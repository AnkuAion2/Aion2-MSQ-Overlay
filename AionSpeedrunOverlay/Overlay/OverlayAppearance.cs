using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AionSpeedrunOverlay.Overlay;

public sealed record OverlayAppearance
{
    public double BackgroundOpacity { get; init; } = 92.0 / 255;
    public double Scale { get; init; } = 1;
    public double Width { get; init; } = 430;
    public string BackgroundColor { get; init; } = "#0B1119";
    public string TextColor { get; init; } = "#E2E8F0";
    public string AccentColor { get; init; } = "#448CFF";

    public OverlayAppearance Normalize() => this with
    {
        BackgroundOpacity = Clamp(BackgroundOpacity, 0, 1, 92.0 / 255),
        Scale = Clamp(Scale, .8, 1.5, 1),
        Width = Clamp(Width, 360, 720, 430),
        BackgroundColor = NormalizeColor(BackgroundColor, "#0B1119"),
        TextColor = NormalizeColor(TextColor, "#E2E8F0"),
        AccentColor = NormalizeColor(AccentColor, "#448CFF")
    };

    private static double Clamp(double value, double min, double max, double fallback) =>
        double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;

    private static string NormalizeColor(string? color, string fallback) =>
        color != null && Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$")
            ? color.ToUpperInvariant() : fallback;
}

public sealed class OverlayAppearanceStore
{
    private readonly string path;

    public OverlayAppearanceStore(string? path = null)
    {
        this.path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AionSpeedrunOverlay", "appearance.json");
    }

    public OverlayAppearance Load()
    {
        try
        {
            if (!File.Exists(path)) return new();
            return (JsonSerializer.Deserialize<OverlayAppearance>(File.ReadAllText(path)) ?? new()).Normalize();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }

    public void Save(OverlayAppearance appearance)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        string temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(
            appearance.Normalize(), new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, path, true);
    }
}
