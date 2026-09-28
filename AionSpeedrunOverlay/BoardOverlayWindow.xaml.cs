using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using AionSpeedrunOverlay.Board;
using AionSpeedrunOverlay.Models;

namespace AionSpeedrunOverlay
{
    public partial class BoardOverlayWindow : Window
    {
        private const int ReferenceWidth = 1920;
        private const int ReferenceHeight = 1080;
        private const double ReferenceFirstCenterX = 320;
        private const double ReferenceFirstCenterY = 140;
        private const double ReferenceCellSpacing = 64;
        private const double ReferenceMarkerSize = 54;

        private const int GwlExStyle = -20;
        private const int WsExTransparent = 0x00000020;
        private const int WsExToolWindow = 0x00000080;
        private const int WsExNoActivate = 0x08000000;
        private const uint WdaNone = 0x00000000;

        private DaevanionBoardCategory? displayedCategory;


        public BoardOverlayWindow()
        {
            InitializeComponent();

            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;

            SourceInitialized += (_, _) => ConfigureOverlayWindow();
        }


        public void ShowRoute(DaevanionBoardCategory category)
        {
            if (displayedCategory != category ||
                BoardMarkerCanvas.Children.Count == 0)
            {
                displayedCategory = category;
                RenderRoute(category);
            }

            if (!IsVisible)
                Show();
        }


        public void HideRoute()
        {
            if (IsVisible)
                Hide();
        }


        private void RenderRoute(DaevanionBoardCategory category)
        {
            BoardMarkerCanvas.Children.Clear();

            double scaleX = Width / ReferenceWidth;
            double scaleY = Height / ReferenceHeight;
            double markerScale = Math.Min(scaleX, scaleY);
            double markerSize = ReferenceMarkerSize * markerScale;

            foreach (DaevanionNode node in
                     DaevanionBoardRoutes.GetRoute(category))
            {
                double centerX =
                    (ReferenceFirstCenterX +
                     (node.Column - 1) * ReferenceCellSpacing) * scaleX;
                double centerY =
                    (ReferenceFirstCenterY +
                     (node.Row - 1) * ReferenceCellSpacing) * scaleY;

                var marker = new System.Windows.Shapes.Rectangle
                {
                    Width = markerSize,
                    Height = markerSize,
                    RadiusX = 7 * markerScale,
                    RadiusY = 7 * markerScale,
                    Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 45, 149)),
                    StrokeThickness = Math.Max(2, 4 * markerScale),
                    Fill = System.Windows.Media.Brushes.Transparent,
                    IsHitTestVisible = false
                };

                System.Windows.Controls.Canvas.SetLeft(
                    marker,
                    centerX - markerSize / 2);
                System.Windows.Controls.Canvas.SetTop(
                    marker,
                    centerY - markerSize / 2);
                BoardMarkerCanvas.Children.Add(marker);
            }
        }


        private void ConfigureOverlayWindow()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GwlExStyle);

            extendedStyle |= WsExTransparent;
            extendedStyle |= WsExToolWindow;
            extendedStyle |= WsExNoActivate;

            SetWindowLong(hwnd, GwlExStyle, extendedStyle);
            SetWindowDisplayAffinity(hwnd, WdaNone);
        }


        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(
            IntPtr hWnd,
            int nIndex,
            int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(
            IntPtr hWnd,
            uint dwAffinity);
    }
}
