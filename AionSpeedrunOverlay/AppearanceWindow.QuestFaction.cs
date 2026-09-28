using System;
using System.IO;
using System.Windows;
using AionSpeedrunOverlay.Quest;

namespace AionSpeedrunOverlay;

public partial class AppearanceWindow
{
    public Func<bool>? CanChangeFaction { get; set; }
    public event Action? FactionRestartRequested;
    private QuestFactionStore? factionStore;
    private QuestFaction activeFaction;
    private QuestFaction selectedFaction;

    public void ConfigureQuestFaction(QuestFaction active, QuestFactionStore store)
    {
        activeFaction = active;
        factionStore = store;
        selectedFaction = store.Load();
        FactionPanel.Visibility = Visibility.Visible;
        EditAllNotesButton.Content = $"Edit all {active} quest notes";
        UpdateFactionSelection();
    }

    private void SelectAsmodian_Click(object sender, RoutedEventArgs e) => SaveFaction(QuestFaction.Asmodian);
    private void SelectElyos_Click(object sender, RoutedEventArgs e) => SaveFaction(QuestFaction.Elyos);

    private void SaveFaction(QuestFaction faction)
    {
        if (factionStore == null || !(CanChangeFaction?.Invoke() ?? true)) return;
        if (!SavePending()) return;
        try
        {
            factionStore.Save(faction);
            selectedFaction = faction;
            UpdateFactionSelection();
            if (faction != activeFaction) FactionRestartRequested?.Invoke();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "Could not save faction",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateFactionSelection()
    {
        AsmodianFactionButton.Background = Brush(selectedFaction == QuestFaction.Asmodian ? "#285DA5" : "#202D40");
        ElyosFactionButton.Background = Brush(selectedFaction == QuestFaction.Elyos ? "#285DA5" : "#202D40");
        AsmodianFactionButton.Content = selectedFaction == QuestFaction.Asmodian ? "✓ Asmodian" : "Asmodian";
        ElyosFactionButton.Content = selectedFaction == QuestFaction.Elyos ? "✓ Elyos · Experimental" : "Elyos · Experimental";
        FactionStatus.Text = selectedFaction == activeFaction
            ? $"Active: {activeFaction}. Selecting another faction restarts the overlay automatically."
            : $"Active: {activeFaction}. Restarting to switch to {selectedFaction}…";
        FactionStatus.Foreground = Brush(selectedFaction == activeFaction ? "#94A3B8" : "#B5D2FF");
        ElyosNotice.Visibility = selectedFaction == QuestFaction.Elyos || activeFaction == QuestFaction.Elyos
            ? Visibility.Visible : Visibility.Collapsed;
    }
}