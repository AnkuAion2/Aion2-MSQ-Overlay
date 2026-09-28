using System;
using System.Windows;
using System.Windows.Media;
using AionSpeedrunOverlay.Overlay;

namespace AionSpeedrunOverlay;

public partial class MainWindow
{
    private readonly OverlayAppearanceStore appearanceStore = new();
    private OverlayAppearance appearance = new();
    private AppearanceWindow? appearanceWindow;
    private SelectiveClickThrough? clickThrough;

    private void InitializeAppearance()
    {
        appearance = appearanceStore.Load();
        ApplyAppearance(appearance);
        UpdateLayout();
        clickThrough ??= new SelectiveClickThrough(this, SpeedrunPanel, NoteEditorPanel);
    }

    private void MoveOverlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton != System.Windows.Input.MouseButton.Left) return;
        e.Handled = true;
        DragMove();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (appearanceWindow != null)
        {
            appearanceWindow.Activate();
            return;
        }

        appearanceWindow = new AppearanceWindow(appearance, appearanceStore, value =>
        {
            appearance = value;
            ApplyAppearance(value);
        }) { Owner = this };
        appearanceWindow.ConfigureQuestFaction(activeQuestFaction, questFactionStore);
        appearanceWindow.CanChangeFaction = CanRestartForFaction;
        appearanceWindow.FactionRestartRequested += RestartForFaction;
        appearanceWindow.EditAllNotesRequested += OpenQuestNotes;
        appearanceWindow.Closed += (_, _) => appearanceWindow = null;
        appearanceWindow.Show();
    }

    private void ApplyAppearance(OverlayAppearance value)
    {
        value = value.Normalize();
        // Scale the layout, including wrapping and hit targets, rather than only painting it larger.
        SpeedrunPanel.LayoutTransform = new ScaleTransform(value.Scale, value.Scale);
        MinWidth = 360 * value.Scale;
        MinHeight = 132 * value.Scale;
        Width = value.Width * value.Scale;

        SetAppearanceBrush("PanelBackground", value.BackgroundColor, value.BackgroundOpacity);
        SetAppearanceBrush("NotesBackground",
            value.BackgroundColor == "#0B1119" ? "#131C27" : value.BackgroundColor,
            value.BackgroundOpacity * 36 / 92);
        SetAppearanceBrush("NumberBackground",
            value.BackgroundColor == "#0B1119" ? "#1E2938" : value.BackgroundColor,
            value.BackgroundOpacity * 51 / 92);

        bool defaultText = value.TextColor == "#E2E8F0";
        SetAppearanceBrush("PrimaryText", value.TextColor);
        SetAppearanceBrush("SecondaryText", defaultText ? "#CBD5E1" : value.TextColor);
        SetAppearanceBrush("MutedText", defaultText ? "#64748B" : value.TextColor);
        SetAppearanceBrush("ButtonText", defaultText ? "#F1F5F9" : value.TextColor);
        SetAppearanceBrush("IconText", defaultText ? "#DDE7F1" : value.TextColor);

        bool defaultAccent = value.AccentColor == "#448CFF";
        SetAppearanceBrush("AccentBackground", value.AccentColor, 176.0 / 255);
        SetAppearanceBrush("AccentBorder", defaultAccent ? "#75AAFF" : value.AccentColor, 204.0 / 255);
        SetAppearanceBrush("PanelBorder", defaultAccent ? "#64748B" : value.AccentColor, 89.0 / 255);
        SetAppearanceBrush("NotesBorder", defaultAccent ? "#475569" : value.AccentColor, 61.0 / 255);
        SetAppearanceBrush("NumberBorder", defaultAccent ? "#5B687A" : value.AccentColor, 128.0 / 255);
        SetAppearanceBrush("SeparatorBrush", defaultAccent ? "#475569" : value.AccentColor, 53.0 / 255);
        SetAppearanceBrush("EditorBorder", defaultAccent ? "#5B8FD9" : value.AccentColor, 121.0 / 255);
        SetAppearanceBrush("EditorTitle", defaultAccent ? "#BAE6FD" : value.AccentColor);
    }

    private void SetAppearanceBrush(string key, string hex, double opacity = 1)
    {
        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
        color.A = (byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        Resources[key] = brush;
    }
}
