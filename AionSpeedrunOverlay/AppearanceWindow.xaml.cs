using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using AionSpeedrunOverlay.Overlay;

namespace AionSpeedrunOverlay;

public partial class AppearanceWindow : Window
{
    public event Action? EditAllNotesRequested;

    private OverlayAppearance appearance;
    private readonly OverlayAppearanceStore store;
    private readonly Action<OverlayAppearance> preview;
    private readonly DispatcherTimer saveTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private bool updatingControls;
    private bool dirty;

    public AppearanceWindow(OverlayAppearance appearance, OverlayAppearanceStore store,
        Action<OverlayAppearance> preview)
    {
        this.appearance = appearance.Normalize();
        this.store = store;
        this.preview = preview;
        InitializeComponent();
        PopulateControls();
        saveTimer.Tick += (_, _) => SavePending();
        Closing += OnClosing;
        Closed += (_, _) => saveTimer.Stop();
    }

    private void PopulateControls()
    {
        updatingControls = true;
        OpacitySlider.Value = appearance.BackgroundOpacity * 100;
        ScaleSlider.Value = appearance.Scale * 100;
        WidthSlider.Value = appearance.Width;
        UpdateLabels();
        updatingControls = false;
    }

    private void UpdateLabels()
    {
        OpacityValue.Text = $"{appearance.BackgroundOpacity * 100:0}%";
        ScaleValue.Text = $"{appearance.Scale * 100:0}%";
        WidthValue.Text = $"{appearance.Width:0}";
        BackgroundHex.Text = appearance.BackgroundColor;
        TextHex.Text = appearance.TextColor;
        AccentHex.Text = appearance.AccentColor;
        BackgroundSwatch.Background = Brush(appearance.BackgroundColor);
        TextSwatch.Background = Brush(appearance.TextColor);
        AccentSwatch.Background = Brush(appearance.AccentColor);
    }

    private static SolidColorBrush Brush(string hex) =>
        new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));

    private void AppearanceSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (updatingControls || !IsInitialized) return;
        appearance = appearance with
        {
            BackgroundOpacity = OpacitySlider.Value / 100,
            Scale = ScaleSlider.Value / 100,
            Width = WidthSlider.Value
        };
        PreviewAndScheduleSave();
    }

    private void ChooseColor_Click(object sender, RoutedEventArgs e)
    {
        string kind = (string)((System.Windows.Controls.Button)sender).Tag;
        string current = kind switch
        {
            "Background" => appearance.BackgroundColor,
            "Text" => appearance.TextColor,
            _ => appearance.AccentColor
        };
        using var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            Color = System.Drawing.ColorTranslator.FromHtml(current)
        };
        if (dialog.ShowDialog(new DialogOwner(new WindowInteropHelper(this).Handle))
            != System.Windows.Forms.DialogResult.OK) return;
        string hex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        appearance = kind switch
        {
            "Background" => appearance with { BackgroundColor = hex },
            "Text" => appearance with { TextColor = hex },
            _ => appearance with { AccentColor = hex }
        };
        PreviewAndScheduleSave();
    }

    private void PreviewAndScheduleSave()
    {
        appearance = appearance.Normalize();
        UpdateLabels();
        preview(appearance);
        dirty = true;
        SaveStatus.Text = "Saving changes…";
        SaveStatus.Foreground = Brush("#94A3B8");
        saveTimer.Stop();
        saveTimer.Start();
    }

    private bool SavePending()
    {
        saveTimer.Stop();
        if (!dirty) return true;
        try
        {
            store.Save(appearance);
            dirty = false;
            SaveStatus.Text = "Changes saved automatically.";
            SaveStatus.Foreground = Brush("#94A3B8");
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SaveStatus.Text = "Could not save. Changes apply for this session only.\n" + ex.Message;
            SaveStatus.Foreground = Brush("#FCA5A5");
            return false;
        }
    }

    private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
    {
        appearance = new();
        PopulateControls();
        PreviewAndScheduleSave();
    }

    private void EditAllNotes_Click(object sender, RoutedEventArgs e) => EditAllNotesRequested?.Invoke();

    private void Done_Click(object sender, RoutedEventArgs e) => Close();

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!SavePending())
        {
            e.Cancel = System.Windows.MessageBox.Show(this,
                "Your appearance could not be saved. Close without saving?",
                "Overlay appearance", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes;
        }
    }

    private sealed class DialogOwner(IntPtr handle) : System.Windows.Forms.IWin32Window
    {
        public IntPtr Handle => handle;
    }
}
