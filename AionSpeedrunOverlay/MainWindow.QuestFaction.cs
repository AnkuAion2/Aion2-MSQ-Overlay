using AionSpeedrunOverlay.Quest;

namespace AionSpeedrunOverlay;

public partial class MainWindow
{
    private readonly QuestFactionStore questFactionStore;
    // Frozen before startup: saving a preference never swaps an in-flight OCR catalog.
    private readonly QuestFaction activeQuestFaction;
}