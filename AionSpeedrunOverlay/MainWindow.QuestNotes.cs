using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using AionSpeedrunOverlay.Quest;

namespace AionSpeedrunOverlay;

public partial class MainWindow
{
    private void OpenQuestNotes()
    {
        if (isNoteEditorOpen)
        {
            System.Windows.MessageBox.Show(appearanceWindow ?? (Window)this,
                "Save or cancel the note currently being edited in the overlay first.",
                "Finish the current edit", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var catalog = new QuestCatalog(ResolveQuestCatalogPath(), ResolveLevelLessContextPath());
            var window = new QuestNotesWindow(catalog.GetNoteEntries(), questNoteStore, key =>
            {
                if (string.Equals(key, displayedNoteKey, StringComparison.Ordinal))
                    RenderDisplayedNote();
            }) { Owner = appearanceWindow ?? (Window)this };
            window.Title = $"{activeQuestFaction} quest notes";
            window.LibraryTitle.Text = activeQuestFaction == QuestFaction.Elyos
                ? "Elyos notes · Experimental" : "Asmodian quest notes";
            // A modal window prevents another editor from overwriting a stale draft.
            // Dispatcher timers and background recognition continue normally.
            window.ShowDialog();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or JsonException or InvalidOperationException)
        {
            System.Windows.MessageBox.Show(appearanceWindow ?? (Window)this,
                ex.Message, "Could not open quest notes", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
