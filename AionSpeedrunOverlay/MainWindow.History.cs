using System;
using System.Collections.Generic;
using System.Windows;
using AionSpeedrunOverlay.Quest;

namespace AionSpeedrunOverlay;

public partial class MainWindow
{
    private sealed record HistoryEntry(string RecognitionId, string Title, string NoteKey, string[] Notes);
    private readonly List<HistoryEntry> noteHistory = new();
    private int historyIndex = -1;
    private bool followLive = true;
    private int historySeenCount;

    // Called only after the existing OCR confirmation gate has accepted a new state.
    private void AcceptConfirmedNote(QuestMatch match, string recognitionId)
    {
        var quest = QuestRecognitionIdentity.GetVisibleQuest(match)!;
        var notes = match.IsNextQuestHandoff
            ? new List<string>()
            : match.Step?.Notes.Count > 0 ? match.Step.Notes
            : match.IsQuestObjectiveMatch || match.Quest!.Steps.Count == 0
                ? match.Quest!.Notes : new List<string>();
        noteHistory.Add(new HistoryEntry(
            recognitionId,
            match.Step != null ? $"{quest.Title} · {match.Step.Title}" : quest.Title,
            QuestRecognitionIdentity.CreateNoteKey(match),
            QuestNoteStore.NormalizeSteps(notes).ToArray()));

        // displayedRecognitionId continues to mean latest confirmed OCR state,
        // never the older state chosen for viewing.
        displayedRecognitionId = recognitionId;
        if (followLive && !isNoteEditorOpen)
        {
            historyIndex = noteHistory.Count - 1;
            historySeenCount = noteHistory.Count;
            ShowHistoryNote();
        }
        UpdateHistoryControls();
    }

    private void ShowHistoryNote()
    {
        if (isNoteEditorOpen || historyIndex < 0) return;
        var entry = noteHistory[historyIndex];
        displayedNoteKey = entry.NoteKey;
        displayedDefaultNotes = new List<string>(entry.Notes);
        CurrentQuestText.Text = entry.Title;
        CurrentQuestText.ToolTip = entry.Title;
        EditNoteButton.IsEnabled = !string.IsNullOrEmpty(entry.NoteKey);
        if (string.IsNullOrEmpty(entry.NoteKey))
        {
            SetNoteRows(Array.Empty<string>(), "No route note has been added for this objective yet.");
            NoteSourceText.Text = "Route note";
        }
        else RenderDisplayedNote();
    }

    private void PreviousNote_Click(object sender, RoutedEventArgs e)
    {
        if (isNoteEditorOpen || historyIndex <= 0) return;
        followLive = false;
        historySeenCount = noteHistory.Count;
        historyIndex--;
        ShowHistoryNote();
        UpdateHistoryControls();
    }

    private void NextNote_Click(object sender, RoutedEventArgs e)
    {
        if (isNoteEditorOpen || historyIndex < 0 || historyIndex >= noteHistory.Count - 1) return;
        historyIndex++;
        historySeenCount = noteHistory.Count;
        // Only the explicit Live button resumes automatic following.
        ShowHistoryNote();
        UpdateHistoryControls();
    }

    private void ReturnToLive_Click(object sender, RoutedEventArgs e)
    {
        if (isNoteEditorOpen || noteHistory.Count == 0) return;
        followLive = true;
        historyIndex = noteHistory.Count - 1;
        historySeenCount = noteHistory.Count;
        ShowHistoryNote();
        UpdateHistoryControls();
    }

    private void PinNoteForEditing()
    {
        followLive = false;
        historySeenCount = noteHistory.Count;
        UpdateHistoryControls();
    }

    private void UpdateHistoryControls()
    {
        PreviousNoteButton.IsEnabled = !isNoteEditorOpen && historyIndex > 0;
        NextNoteButton.IsEnabled = !isNoteEditorOpen && historyIndex >= 0 && historyIndex < noteHistory.Count - 1;
        ReturnToLiveButton.IsEnabled = !isNoteEditorOpen && noteHistory.Count > 0 && !followLive;
        HistoryStatusText.Text = noteHistory.Count == 0 ? "Live · waiting"
            : followLive ? "Live"
            : $"History · {historyIndex + 1}/{noteHistory.Count}" +
                (noteHistory.Count > historySeenCount ? " · New step detected" : "");
        HistoryStatusText.ToolTip = noteHistory.Count == 0 ? "Confirmed steps appear here during this session."
            : "Latest confirmed: " + noteHistory[^1].Title;
    }
}
