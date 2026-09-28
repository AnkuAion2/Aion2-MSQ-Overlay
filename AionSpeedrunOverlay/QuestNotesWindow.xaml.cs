using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AionSpeedrunOverlay.Quest;

namespace AionSpeedrunOverlay;

public partial class QuestNotesWindow : Window
{
    private readonly QuestNoteStore store;
    private readonly Action<string> onSaved;
    private readonly List<NoteRow> rows;
    private ICollectionView? view;
    private NoteRow? current;
    private bool changingSelection;
    private bool loadingText;
    private bool restoreDefault;
    private string savedText = "";

    public QuestNotesWindow(IReadOnlyList<QuestNoteEntry> entries, QuestNoteStore store, Action<string> onSaved)
    {
        this.store = store;
        this.onSaved = onSaved;
        rows = entries.Select(e => new NoteRow(e, store)).ToList();
        InitializeComponent();
        view = CollectionViewSource.GetDefaultView(rows);
        view.GroupDescriptions.Add(new PropertyGroupDescription("Entry.GroupTitle"));
        view.Filter = MatchesSearch;
        EntriesList.ItemsSource = view;
        UpdateCount();
        if (rows.Count > 0) EntriesList.SelectedItem = rows[0];
        Closing += OnClosing;
    }

    private bool HasChanges => current != null && (restoreDefault || NoteText.Text != savedText);

    private bool MatchesSearch(object item)
    {
        var row = (NoteRow)item;
        string text = $"{row.Entry.GroupTitle} {row.Entry.StepTitle} {row.Entry.Chapter} {row.Text}";
        return SearchBox.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .All(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private void Search_Changed(object sender, TextChangedEventArgs e)
    {
        if (view == null) return;
        // Filtering never commits or discards the current draft.
        changingSelection = true;
        try
        {
            view.Refresh();
            EntriesList.SelectedItem = current != null && MatchesSearch(current) ? current : null;
        }
        finally { changingSelection = false; }
        UpdateCount();
    }

    private void UpdateCount() => CountText.Text = $"{view!.Cast<object>().Count()} / {rows.Count} entries";

    private void Entry_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (changingSelection || EntriesList.SelectedItem is not NoteRow next || ReferenceEquals(next, current)) return;
        if (!ResolvePendingChanges())
        {
            changingSelection = true;
            EntriesList.SelectedItem = current;
            changingSelection = false;
            return;
        }
        current = next;
        LoadCurrent();
    }

    private void LoadCurrent()
    {
        if (current == null) return;
        current.Reload(store);
        QuestTitle.Text = current.Entry.GroupTitle;
        StepTitle.Text = current.Entry.StepTitle;
        int imageCount = store.GetImages(current.Entry.Key).Count;
        NoteInfo.Text = (current.IsCustom ? "Custom note" : "Default note") +
            (imageCount > 0 ? $" · {imageCount} attached image(s) preserved" : " · No attached images");
        var sharedRows = rows.Where(row => row.Entry.Key == current.Entry.Key).ToList();
        if (sharedRows.Count > 1)
            NoteInfo.Text += "\nShared note: saving or restoring defaults affects all these steps:\n" +
                string.Join("\n", sharedRows.Select(row => row.Entry.StepTitle));
        savedText = current.Text;
        loadingText = true;
        NoteText.Text = savedText;
        loadingText = false;
        restoreDefault = false;
        EditorPanel.IsEnabled = true;
        UpdateButtons();
        StatusText.Text = "Edit the note, then choose Save.";
    }

    private void Note_Changed(object sender, TextChangedEventArgs e)
    {
        if (loadingText || current == null) return;
        if (restoreDefault && NoteText.Text != current.Entry.DefaultText) restoreDefault = false;
        UpdateButtons();
        StatusText.Text = HasChanges ? "Unsaved changes" : "No unsaved changes";
    }

    private void UpdateButtons()
    {
        SaveButton.IsEnabled = HasChanges;
        CancelButton.IsEnabled = HasChanges;
    }

    private bool SaveCurrent()
    {
        if (current == null || !HasChanges) return true;
        string key = current.Entry.Key;
        try
        {
            if (restoreDefault) store.RemoveTextOverride(key);
            else store.SaveOverride(key, QuestNoteStore.ParseEditorText(NoteText.Text));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "Could not save the note", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        foreach (var row in rows.Where(row => row.Entry.Key == key)) row.Reload(store);
        LoadCurrent();
        StatusText.Text = "Saved locally. Existing images were preserved.";
        onSaved(key);
        return true;
    }

    private bool ResolvePendingChanges()
    {
        if (!HasChanges) return true;
        var answer = System.Windows.MessageBox.Show(this,
            "Save changes to the selected note?\nYes: save · No: discard · Cancel: keep editing",
            "Unsaved note", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        return answer == MessageBoxResult.No || answer == MessageBoxResult.Yes && SaveCurrent();
    }

    private void Save_Click(object sender, RoutedEventArgs e) => SaveCurrent();
    private void Cancel_Click(object sender, RoutedEventArgs e) => LoadCurrent();
    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (current == null) return;
        loadingText = true;
        NoteText.Text = current.Entry.DefaultText;
        loadingText = false;
        restoreDefault = true;
        UpdateButtons();
        StatusText.Text = "Default text selected. Choose Save to apply; images will be kept.";
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void OnClosing(object? sender, CancelEventArgs e) => e.Cancel = !ResolvePendingChanges();

    private sealed class NoteRow : INotifyPropertyChanged
    {
        public QuestNoteEntry Entry { get; }
        public string Text { get; private set; } = "";
        public bool IsCustom { get; private set; }
        public string Preview => string.IsNullOrEmpty(Text) ? "No note" : Text.Replace(Environment.NewLine, " · ");
        public event PropertyChangedEventHandler? PropertyChanged;
        public NoteRow(QuestNoteEntry entry, QuestNoteStore store) { Entry = entry; Reload(store); }
        public void Reload(QuestNoteStore store)
        {
            IsCustom = store.TryGetOverride(Entry.Key, out var notes);
            Text = IsCustom ? string.Join(Environment.NewLine, QuestNoteStore.NormalizeSteps(notes)) : Entry.DefaultText;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Preview)));
        }
    }
}
