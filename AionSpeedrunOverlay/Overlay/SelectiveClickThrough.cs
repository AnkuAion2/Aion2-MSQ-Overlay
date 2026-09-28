using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using System.Windows.Interop;
using System.Windows.Media;
using Rectangle = System.Windows.Shapes.Rectangle;
using Brushes = System.Windows.Media.Brushes;
using System.Windows.Threading;

namespace AionSpeedrunOverlay.Overlay;

/// <summary>
/// Keeps the original WPF controls interactive in a native window region.
/// A passive, non-activating owned window paints the rest of the same live visual.
/// Unlike HTTRANSPARENT, layered WS_EX_TRANSPARENT also passes input across processes.
/// No polling, global input hooks or synthetic mouse events are needed.
/// </summary>
internal sealed class SelectiveClickThrough : IDisposable
{
    private readonly Window owner;
    private readonly FrameworkElement content;
    private readonly FrameworkElement editor;
    private readonly Window display;
    private readonly Rectangle surface;
    private readonly VisualBrush liveVisual;
    private readonly IntPtr ownerHandle;
    private IntPtr displayHandle;
    private NativeRect lastBounds;
    private Rect[] lastInput = [];
    private readonly List<FrameworkElement> inputTargets = new();
    private readonly List<Rect> inputBuffer = new();
    private bool targetsDirty = true;
    private bool geometryDirty = true;

    // Call when controls are added/removed, not for visibility or position changes.
    public void InvalidateInputTargets()
    {
        targetsDirty = true;
        geometryDirty = true;
        Schedule();
    }

    private void FindInputTargets(DependencyObject node)
    {
        if (node is FrameworkElement target &&
            (ReferenceEquals(target, editor) || target is ButtonBase))
        {
            inputTargets.Add(target);
            return;
        }
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            FindInputTargets(VisualTreeHelper.GetChild(node, i));
    }
    private bool hasLayout;
    private bool updating;
    private bool disposed;
    private DispatcherOperation? pending;

    public SelectiveClickThrough(Window owner, FrameworkElement content, FrameworkElement editor)
    {
        this.owner = owner;
        this.content = content;
        this.editor = editor;
        ownerHandle = new WindowInteropHelper(owner).Handle;
        if (ownerHandle == IntPtr.Zero)
            throw new InvalidOperationException("Create click-through after the overlay window is loaded.");

        liveVisual = new VisualBrush(content) { Stretch = Stretch.Fill, AutoLayoutContent = false, ViewboxUnits = BrushMappingMode.Absolute };
        surface = new Rectangle { Fill = liveVisual, IsHitTestVisible = false };
        display = new Window
        {
            Title = "Aion overlay display",
            Owner = owner,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ShowInTaskbar = false,
            ShowActivated = false,
            Focusable = false,
            Topmost = owner.Topmost,
            Left = owner.Left,
            Top = owner.Top,
            Width = Math.Max(1, owner.ActualWidth),
            Height = Math.Max(1, owner.ActualHeight),
            Content = surface
        };
        display.SourceInitialized += (_, _) =>
        {
            displayHandle = new WindowInteropHelper(display).Handle;
            int style = GetWindowLong(displayHandle, -20);
            SetWindowLong(displayHandle, -20, style | 0x20 | 0x08000000 | 0x80);
        };

        owner.LayoutUpdated += OnLayoutUpdated;
        owner.LocationChanged += OnLayoutUpdated;
        owner.SizeChanged += OnSizeChanged;
        owner.IsVisibleChanged += OnVisibilityChanged;
        owner.Closed += OnClosed;
        try
        {
            Synchronize();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private void OnLayoutUpdated(object? sender, EventArgs e) { geometryDirty = true; Schedule(); }
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) { geometryDirty = true; Schedule(); }
    private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e) => Schedule();
    private void OnClosed(object? sender, EventArgs e) => Dispose();

    private void Schedule()
    {
        if (disposed || updating || pending?.Status == DispatcherOperationStatus.Pending) return;
        pending = owner.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(Synchronize));
    }

    private void Synchronize()
    {
        if (disposed || updating) return;
        updating = true;
        try
        {
            if (!owner.IsVisible || owner.WindowState == WindowState.Minimized)
            {
                display.Hide();
                return;
            }
            if (!GetWindowRect(ownerHandle, out NativeRect bounds))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            if (!geometryDirty && hasLayout && bounds.Equals(lastBounds) && display.IsVisible) return;
            if (targetsDirty)
            {
                inputTargets.Clear();
                FindInputTargets(content);
                targetsDirty = false;
            }
            var input = inputBuffer;
            input.Clear();
            foreach (var target in inputTargets) CollectInput(target, bounds, input);
            geometryDirty = false;
            var rectangles = input;
            bool changed = !hasLayout || !bounds.Equals(lastBounds) || !rectangles.SequenceEqual(lastInput);
            if (!changed && display.IsVisible) return;

            // Position before showing to keep the companion on the owner's monitor/DPI.
            display.Left = owner.Left;
            display.Top = owner.Top;
            display.Width = Math.Max(1, owner.ActualWidth);
            display.Height = Math.Max(1, owner.ActualHeight);
            if (!display.IsVisible) display.Show();
            SetWindowPos(displayHandle, IntPtr.Zero, bounds.Left, bounds.Top,
                bounds.Right - bounds.Left, bounds.Bottom - bounds.Top, 0x0010 | 0x0004);

            var source = (HwndSource)PresentationSource.FromVisual(display)!;
            Matrix fromDevice = source.CompositionTarget.TransformFromDevice;
            var outside = new GeometryGroup { FillRule = FillRule.EvenOdd };
            outside.Children.Add(new RectangleGeometry(new Rect(
                new System.Windows.Point(), fromDevice.Transform(new System.Windows.Point(
                    bounds.Right - bounds.Left, bounds.Bottom - bounds.Top)))));
            foreach (Rect rect in rectangles)
            {
                outside.Children.Add(new RectangleGeometry(new Rect(
                    fromDevice.Transform(rect.TopLeft), fromDevice.Transform(rect.BottomRight))));
            }
            outside.Freeze();
            liveVisual.Viewbox = content.LayoutTransform.TransformBounds(
                new Rect(new System.Windows.Point(), content.RenderSize));
            surface.Clip = outside;
            SetInputRegion(rectangles);
            lastBounds = bounds;
            lastInput = rectangles.ToArray();
            hasLayout = true;
        }
        finally
        {
            updating = false;
        }
    }

    private void CollectInput(DependencyObject node, NativeRect bounds, List<Rect> input)
    {
        if (node is UIElement element && !element.IsVisible) return;
        // The complete editor remains usable (text selection, caret and scrolling).
        // Stop descending here to avoid overlapping regions in the complementary visual clip.
        if (node is FrameworkElement target &&
            (ReferenceEquals(target, editor) || target is ButtonBase))
        {
            var topLeft = target.PointToScreen(new System.Windows.Point());
            var bottomRight = target.PointToScreen(new System.Windows.Point(target.ActualWidth, target.ActualHeight));
            int left = (int)Math.Floor(topLeft.X) - bounds.Left;
            int top = (int)Math.Floor(topLeft.Y) - bounds.Top;
            int right = (int)Math.Ceiling(bottomRight.X) - bounds.Left;
            int bottom = (int)Math.Ceiling(bottomRight.Y) - bounds.Top;
            if (right > left && bottom > top)
                input.Add(new Rect(left, top, right - left, bottom - top));
            return;
        }
    }

    private void SetInputRegion(IEnumerable<Rect> rectangles)
    {
        IntPtr region = CreateRectRgn(0, 0, 0, 0);
        if (region == IntPtr.Zero) throw new Win32Exception();
        try
        {
            foreach (Rect rect in rectangles)
            {
                IntPtr part = CreateRectRgn((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);
                if (part == IntPtr.Zero) throw new Win32Exception();
                try
                {
                    if (CombineRgn(region, region, part, 2) == 0) throw new Win32Exception();
                }
                finally { DeleteObject(part); }
            }
            if (SetWindowRgn(ownerHandle, region, true) == 0) throw new Win32Exception();
            region = IntPtr.Zero; // Successful SetWindowRgn transfers ownership to Windows.
        }
        finally
        {
            if (region != IntPtr.Zero) DeleteObject(region);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        pending?.Abort();
        owner.LayoutUpdated -= OnLayoutUpdated;
        owner.LocationChanged -= OnLayoutUpdated;
        owner.SizeChanged -= OnSizeChanged;
        owner.IsVisibleChanged -= OnVisibilityChanged;
        owner.Closed -= OnClosed;
        SetWindowRgn(ownerHandle, IntPtr.Zero, true);
        liveVisual.Visual = null;
        display.Close();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hwnd, IntPtr region, bool redraw);
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);
    [DllImport("gdi32.dll")]
    private static extern int CombineRgn(IntPtr destination, IntPtr first, IntPtr second, int mode);
    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr value);
}
