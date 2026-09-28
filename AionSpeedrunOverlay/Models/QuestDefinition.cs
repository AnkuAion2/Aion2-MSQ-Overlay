using System.Collections.Generic;

namespace AionSpeedrunOverlay.Models
{
    public sealed class QuestCatalogDocument
    {
        public int SchemaVersion { get; init; }

        public List<QuestDefinition> Quests { get; init; } = new();
    }


    public sealed class QuestDefinition
    {
        public string Id { get; init; } = "";

        public int? RecommendedLevel { get; init; }

        public string RecognitionKind { get; init; } = "quest";

        public bool AllowWithoutLevel { get; init; }

        public string Chapter { get; init; } = "";

        public string Title { get; init; } = "";

        public List<string> Aliases { get; init; } = new();

        public List<string> ObjectiveAliases { get; init; } = new();

        public List<string> Notes { get; init; } = new();

        public List<QuestStepDefinition> Steps { get; init; } = new();

        public int? RouteIndex { get; init; }
    }


    public sealed class QuestStepDefinition
    {
        public string Id { get; init; } = "";

        public string Title { get; init; } = "";

        public List<string> TitleAliases { get; init; } = new();

        public List<string> ObjectiveAliases { get; init; } = new();

        public List<string> Notes { get; init; } = new();
    }
}
