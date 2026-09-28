using System;
using System.Collections.Generic;
using System.Linq;
using AionSpeedrunOverlay.Models;

namespace AionSpeedrunOverlay.Quest;

public sealed record QuestNoteEntry(
    string Key, string GroupTitle, string StepTitle, string Chapter, string DefaultText);

public sealed partial class QuestCatalog
{
    // Snapshots for the editor; callers cannot mutate the recognition definitions.
    public IReadOnlyList<QuestNoteEntry> GetNoteEntries()
    {
        var entries = new List<QuestNoteEntry>();
        foreach (var quest in quests.OrderBy(q => q.RouteIndex ?? int.MaxValue)
                     .Concat(levelLessContexts.OrderBy(q => q.RouteIndex ?? int.MaxValue)))
        {
            string order = quest.RouteIndex is int route ? $"{route:00} · " : "";
            string level = quest.RecommendedLevel is int lv ? $"[Lv. {lv}] " : "";
            string group = order + level + quest.Title;
            Add(null, "General quest note", quest.Notes);
            for (int i = 0; i < quest.Steps.Count; i++)
                Add(quest.Steps[i], $"{i + 1}. {quest.Steps[i].Title}", quest.Steps[i].Notes);

            void Add(QuestStepDefinition? step, string title, IEnumerable<string> notes)
            {
                string key = QuestRecognitionIdentity.CreateNoteKey(new QuestMatch { Quest = quest, Step = step });
                entries.Add(new QuestNoteEntry(key, group, title, quest.Chapter,
                    string.Join(Environment.NewLine, QuestNoteStore.NormalizeSteps(notes))));
            }
        }
        return entries.AsReadOnly();
    }
}
