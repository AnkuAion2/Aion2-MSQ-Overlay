using System;
using System.IO;
using System.Text.Json;

namespace AionSpeedrunOverlay.Quest;

public enum QuestFaction { Asmodian, Elyos }

// A preference for the next launch, deliberately separate from live appearance settings.
public sealed class QuestFactionStore
{
    public const string ElyosCatalogFileName = "aion2_elyos_story_quests_lvl1-45.json";
    private readonly string path;

    public QuestFactionStore(string? path = null)
    {
        this.path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AionSpeedrunOverlay", "quest-faction.json");
    }

    public QuestFaction Load()
    {
        try
        {
            if (!File.Exists(path)) return QuestFaction.Asmodian;
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("faction", out var value)
                && value.ValueKind == JsonValueKind.String
                && value.GetString() == nameof(QuestFaction.Elyos)
                    ? QuestFaction.Elyos : QuestFaction.Asmodian;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return QuestFaction.Asmodian;
        }
    }

    public void Save(QuestFaction faction)
    {
        if (!Enum.IsDefined(faction)) throw new ArgumentOutOfRangeException(nameof(faction));
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(
            new { faction = faction.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, path, true);
    }
}