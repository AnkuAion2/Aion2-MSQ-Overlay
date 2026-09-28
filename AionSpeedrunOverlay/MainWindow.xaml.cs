using AionSpeedrunOverlay.Capture;
using AionSpeedrunOverlay.Board;
using AionSpeedrunOverlay.Models;
using AionSpeedrunOverlay.Overlay;
using AionSpeedrunOverlay.Quest;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace AionSpeedrunOverlay
{
    public partial class MainWindow : Window
    {
        public sealed class NoteDisplayItem
        {
            public int Number { get; init; }
            public string Text { get; init; } = "";
            public bool ShowSeparator { get; init; }
        }

        private static readonly bool DaevanionBoardOverlayEnabled = false;

        // Das Overlay soll in Screenshots und Aufnahmen sichtbar sein.
        private const uint WDA_NONE = 0x00000000;

        // ============================================================
        // SERVICES
        // ============================================================

        private readonly ScreenCaptureService
            screenCaptureService =
                new ScreenCaptureService();


        private QuestRecognitionService?
            questRecognitionService;

        private readonly QuestDisplaySafetyPolicy
            questDisplaySafetyPolicy = new();

        private DaevanionBoardDetectionService?
            daevanionBoardDetectionService;

        private BoardOverlayWindow?
            boardOverlayWindow;


        // ============================================================
        // TIMERS / STATE
        // ============================================================

        private DispatcherTimer?
            questDetectionTimer;

        private static readonly TimeSpan StableQuestRecognitionDelay =
            TimeSpan.FromMilliseconds(350);

        private static readonly TimeSpan ConfirmingQuestRecognitionDelay =
            TimeSpan.FromMilliseconds(125);

        private DispatcherTimer?
            boardDetectionTimer;

        private readonly QuestNoteStore
            questNoteStore =
                new QuestNoteStore();

        private bool
            isQuestRecognitionRunning =
                false;

        private bool
            isQuestRecognitionConfirmationPending =
                false;

        private bool
            isQuestRecognitionShuttingDown =
                false;

        private bool
            isBoardDetectionRunning =
                false;

        private bool
            isDaevanionBoardOpen =
                false;

        private DaevanionBoardCategory?
            pendingBoardCategory;

        private int
            pendingBoardConfirmations =
                0;

        private int
            boardMissingConfirmations =
                0;

        private string
            pendingRecognitionId =
                "";

        private int
            pendingQuestConfirmations =
                0;

        private int
            pendingRecognitionMisses =
                0;

        private string
            displayedRecognitionId =
                "";

        private string
            displayedNoteKey =
                "";

        private List<string>
            displayedDefaultNotes =
                new();

        private bool
            isNoteEditorOpen =
                false;

        private string
            editingNoteKey =
                "";

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public MainWindow() : this(new QuestFactionStore()) { }

        internal MainWindow(QuestFactionStore factionStore)
        {
            questFactionStore = factionStore;
            activeQuestFaction = questFactionStore.Load();
            InitializeComponent();

            Loaded +=
                MainWindow_Loaded;

            Closing += MainWindow_Closing;

            Closed +=
                MainWindow_Closed;
        }


        // ============================================================
        // STARTUP
        // ============================================================

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Left = 35;
                Top = 180;
                InitializeAppearance();

                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                SetWindowDisplayAffinity(hwnd, WDA_NONE);

                InitializeQuestRecognition();
                if (DaevanionBoardOverlayEnabled)
                    InitializeDaevanionBoardDetection();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Startup error");
                DetectionStatusText.Text = "STARTUP ERROR";
            }
        }


        // ============================================================
        // DAEVANION BOARD
        // ============================================================

        private void InitializeDaevanionBoardDetection()
        {
            daevanionBoardDetectionService =
                new DaevanionBoardDetectionService(screenCaptureService);

            boardDetectionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };

            boardDetectionTimer.Tick += BoardDetectionTimer_Tick;
            boardDetectionTimer.Start();
            BoardDetectionTimer_Tick(null, EventArgs.Empty);
        }


        private async void BoardDetectionTimer_Tick(
            object? sender,
            EventArgs e)
        {
            if (isBoardDetectionRunning ||
                daevanionBoardDetectionService == null)
            {
                return;
            }

            isBoardDetectionRunning = true;

            try
            {
                DaevanionBoardDetectionResult result = await Task.Run(
                    daevanionBoardDetectionService.DetectPrimaryScreen);

                if (!result.IsOpen || result.Category == null)
                {
                    pendingBoardCategory = null;
                    pendingBoardConfirmations = 0;

                    if (isDaevanionBoardOpen)
                    {
                        boardMissingConfirmations++;

                        if (boardMissingConfirmations >= 2)
                            DeactivateDaevanionBoardOverlay();
                    }

                    return;
                }

                boardMissingConfirmations = 0;

                if (pendingBoardCategory == result.Category)
                {
                    pendingBoardConfirmations++;
                }
                else
                {
                    pendingBoardCategory = result.Category;
                    pendingBoardConfirmations = 1;
                }

                if (pendingBoardConfirmations < 3)
                    return;

                ActivateDaevanionBoardOverlay(result.Category.Value);
            }
            catch (Exception ex)
            {
                boardDetectionTimer?.Stop();
                boardOverlayWindow?.HideRoute();

                System.Windows.MessageBox.Show(
                    ex.ToString(),
                    "Daevanion Board detection error");
            }
            finally
            {
                isBoardDetectionRunning = false;
            }
        }


        private void ActivateDaevanionBoardOverlay(
            DaevanionBoardCategory category)
        {
            isDaevanionBoardOpen = true;
            questDetectionTimer?.Stop();

            boardOverlayWindow ??= new BoardOverlayWindow();
            boardOverlayWindow.ShowRoute(category);

            // Das kompakte Notizfenster würde links mehrere Board-Nodes
            // verdecken. Während des Boards bleibt daher nur der
            // klickdurchlässige Marker-Layer sichtbar.
            if (IsVisible)
                Hide();
        }


        private void DeactivateDaevanionBoardOverlay()
        {
            isDaevanionBoardOpen = false;
            boardMissingConfirmations = 0;
            boardOverlayWindow?.HideRoute();

            if (!IsVisible)
                Show();

            questDetectionTimer?.Start();
            QuestDetectionTimer_Tick(null, EventArgs.Empty);
        }


        // ============================================================
        // QUEST RECOGNITION
        // ============================================================

        private void InitializeQuestRecognition()
        {
            string questCatalogPath = ResolveQuestCatalogPath();

            string tessdataPath = Path.Combine(
                AppContext.BaseDirectory,
                "tessdata");

            questRecognitionService = new QuestRecognitionService(
                screenCaptureService,
                questCatalogPath,
                tessdataPath,
                ResolveLevelLessContextPath());

            questDetectionTimer = new DispatcherTimer
            {
                Interval = StableQuestRecognitionDelay
            };

            questDetectionTimer.Tick += QuestDetectionTimer_Tick;
            questDetectionTimer.Start();

            // Nicht erst auf den ersten Timer-Tick warten.
            QuestDetectionTimer_Tick(null, EventArgs.Empty);

            DetectionStatusText.Text =
                "QUEST OCR ACTIVE\nLooking for the yellow quest …";

            CurrentQuestText.Text =
                "Quest route";

            SetNoteRows(
                Array.Empty<string>(),
                "Open a tracked quest to show its route note.");

            NoteSourceText.Text = "Waiting for a route quest";
            EditNoteButton.IsEnabled = false;
        }


        private string ResolveQuestCatalogPath()
        {
            if (activeQuestFaction == QuestFaction.Elyos)
                return Path.Combine(AppContext.BaseDirectory, "Data", QuestFactionStore.ElyosCatalogFileName);

            const string catalogFileName =
                "aion2_asmodian_mythic_quests_lvl1-45.json";

            string deployedCatalogPath = Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                catalogFileName);

            if (File.Exists(deployedCatalogPath))
                return deployedCatalogPath;

            string downloadsCatalogPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                catalogFileName);

            if (File.Exists(downloadsCatalogPath))
                return downloadsCatalogPath;

            return Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                "quests.json");
        }


        private string? ResolveLevelLessContextPath()
        {
            if (activeQuestFaction == QuestFaction.Elyos) return null;

            return Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                "level-less-contexts.json");
        }


        private async void QuestDetectionTimer_Tick(
            object? sender,
            EventArgs e)
        {
            if (isQuestRecognitionShuttingDown ||
                isDaevanionBoardOpen ||
                isQuestRecognitionRunning ||
                questRecognitionService == null)
            {
                return;
            }

            // Der Timer ist absichtlich als One-Shot geschaltet: Erst nachdem
            // dieser OCR-Lauf vollständig beendet ist, beginnt die Wartezeit
            // bis zum nächsten Lauf. So können keine Ticks verloren gehen
            // und keine zwei Erkennungen überlappen.
            questDetectionTimer?.Stop();
            isQuestRecognitionRunning = true;
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            questRecognitionCompletion = completion;
            bool continueQuestRecognition = true;

            try
            {
                using QuestRecognitionFrame frame = await Task.Run(
                    questRecognitionService.RecognizePrimaryScreenFrame);
                if (isQuestRecognitionShuttingDown) return;
                QuestRecognitionResult result = frame.Result;

                QuestDisplaySafetyAction displaySafetyAction =
                    questDisplaySafetyPolicy.Observe(
                        result.Match,
                        displayedRecognitionId);

                if (displaySafetyAction == QuestDisplaySafetyAction.Clear &&
                    !isNoteEditorOpen)
                {
                    ClearDisplayedQuestForSafety(
                        result.Match.Quest == null
                            ? "Open a tracked quest to show its route note."
                            : $"{result.Match.Quest.Title} detected; " +
                                "checking the current objective.");
                }

                if (result.Match.Quest == null ||
                    result.Match.RejectionReason == "quest-ambiguous")
                {
                    RegisterPendingRecognitionGap();
                    HoldDisplayedQuest(
                        result.Match.RejectionReason == "level-missing"
                            ? "Open the quest tracker to show its route note."
                            : "Open a tracked quest to show its route note.");

                    string raw = result.RecognizedLines.Count > 0
                        ? string.Join(" | ", result.RecognizedLines)
                        : "no yellow text";

                    DetectionStatusText.Text =
                        $"QUEST OCR | no safe match\n" +
                        $"Lines: {result.YellowLineCandidates}\n" +
                        raw;

                    return;
                }

                if (result.Match.Quest.Steps.Count > 0 &&
                    result.Match.Step == null &&
                    !result.Match.IsQuestObjectiveMatch)
                {
                    RegisterPendingRecognitionGap();
                    if (displaySafetyAction != QuestDisplaySafetyAction.Clear ||
                        isNoteEditorOpen)
                    {
                        HoldDisplayedQuest(
                            "Checking the current quest objective …");
                    }

                    DetectionStatusText.Text =
                        $"QUEST OCR | objective uncertain\n" +
                        $"{result.Match.Quest.Title}\n" +
                        $"Quest: {result.Match.Score:P0}\n" +
                        $"Objective: {result.Match.StepScore:P0}";
                    return;
                }

                string recognitionId =
                    QuestRecognitionIdentity.CreateDisplayId(result.Match);
                pendingRecognitionMisses = 0;

                if (string.Equals(
                        pendingRecognitionId,
                        recognitionId,
                        StringComparison.Ordinal))
                {
                    pendingQuestConfirmations++;
                }
                else
                {
                    pendingRecognitionId = recognitionId;
                    pendingQuestConfirmations = 1;

                    if (!string.Equals(
                            displayedRecognitionId,
                            recognitionId,
                            StringComparison.Ordinal))
                    {
                        HoldDisplayedQuest(
                            "Confirming the new route note …");
                    }
                }

                int requiredConfirmations =
                    QuestRecognitionConfirmationPolicy.GetRequiredConfirmations(
                        result.Match);

                isQuestRecognitionConfirmationPending =
                    !string.Equals(
                        displayedRecognitionId,
                        recognitionId,
                        StringComparison.Ordinal) &&
                    pendingQuestConfirmations < requiredConfirmations;

                DetectionStatusText.Text =
                    $"QUEST OCR | {result.Match.Score:P0}\n" +
                    $"Level: {result.Match.DetectedLevel?.ToString() ?? "?"}" +
                    $"{(result.Match.LevelFilterApplied ? " (Filter)" : "")}\n" +
                    $"{result.Match.RawText}\n" +
                    $"Objective: {result.Match.RawObjectiveText} " +
                    $"({result.Match.StepScore:P0})\n" +
                    $"Confirmation: {pendingQuestConfirmations}/" +
                    $"{requiredConfirmations}";

                if (pendingQuestConfirmations <
                        requiredConfirmations ||
                    string.Equals(
                        displayedRecognitionId,
                        recognitionId,
                        StringComparison.Ordinal))
                {
                    return;
                }

                AcceptConfirmedNote(result.Match, recognitionId);

            }
            catch (Exception ex)
            {
                continueQuestRecognition = false;
                questDetectionTimer?.Stop();
                if (isQuestRecognitionShuttingDown) return;
                DetectionStatusText.Text = "QUEST OCR STOPPED";

                System.Windows.MessageBox.Show(
                    ex.ToString(),
                    "Quest OCR error");
            }
            finally
            {
                isQuestRecognitionRunning = false;

                if (continueQuestRecognition)
                    ScheduleNextQuestRecognition();
                completion.TrySetResult();
            }
        }


        private void ScheduleNextQuestRecognition()
        {
            if (isQuestRecognitionShuttingDown ||
                isDaevanionBoardOpen ||
                questRecognitionService == null ||
                questDetectionTimer == null)
            {
                return;
            }

            questDetectionTimer.Interval =
                isQuestRecognitionConfirmationPending
                    ? ConfirmingQuestRecognitionDelay
                    : StableQuestRecognitionDelay;
            questDetectionTimer.Start();
        }


        private void HoldDisplayedQuest(string message)
        {
            if (string.IsNullOrWhiteSpace(displayedRecognitionId))
            {
                CurrentQuestText.Text = "Quest route";
                SetNoteRows(Array.Empty<string>(), message);
                NoteSourceText.Text = "Waiting for a route quest";
                EditNoteButton.IsEnabled = false;
                return;
            }

            // Ein unsicherer Frame darf die zuletzt bestätigte Quest und
            // Notiz nicht verändern. Der OCR-Status wird separat aktualisiert.
        }


        private void RegisterPendingRecognitionGap()
        {
            if (string.IsNullOrWhiteSpace(pendingRecognitionId))
                return;

            pendingRecognitionMisses++;

            // Ein einzelner schlechter Frame unterbricht die laufende
            // Bestätigung nicht. Erst zwei aufeinanderfolgende Lücken
            // verwerfen den noch unbestätigten Kandidaten.
            if (pendingRecognitionMisses < 2)
                return;

            pendingRecognitionId = "";
            pendingQuestConfirmations = 0;
            pendingRecognitionMisses = 0;
            isQuestRecognitionConfirmationPending = false;
        }


        private void ClearDisplayedQuestForSafety(string message)
        {
            displayedRecognitionId = "";
            displayedNoteKey = "";
            displayedDefaultNotes.Clear();
            CurrentQuestText.Text = "Quest route";
            SetNoteRows(Array.Empty<string>(), message);
            NoteSourceText.Text = "Checking the current objective";
            EditNoteButton.IsEnabled = false;

        }


        private void RenderDisplayedNote()
        {
            if (string.IsNullOrWhiteSpace(displayedNoteKey))
                return;

            bool hasOverride = questNoteStore.TryGetOverride(
                displayedNoteKey,
                out IReadOnlyList<string> customNotes);
            IReadOnlyList<string> effectiveNotes = hasOverride
                ? customNotes
                : displayedDefaultNotes;
            List<string> normalizedNotes =
                QuestNoteStore.NormalizeSteps(effectiveNotes);

            SetNoteRows(
                normalizedNotes,
                hasOverride
                    ? "This objective intentionally has no route note."
                    : "No route note has been added for this confirmed objective yet.");
            NoteSourceText.Text = hasOverride
                ? "Custom note · saved locally"
                : "Default note";
            RenderNoteImages();

        }


        private void SetNoteRows(
            IReadOnlyList<string> notes,
            string emptyMessage)
        {
            List<NoteDisplayItem> items = notes
                .Select((note, index) => new NoteDisplayItem
                {
                    Number = index + 1,
                    Text = note,
                    ShowSeparator = index < notes.Count - 1
                })
                .ToList();

            NoteImagesPanel.Children.Clear();
            clickThrough?.InvalidateInputTargets();
            NoteImagesPanel.Visibility = Visibility.Collapsed;
            QuestNotesList.ItemsSource = items;
            QuestNotesList.Visibility = items.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
            EmptyNotesText.Text = emptyMessage;
            EmptyNotesText.Visibility = items.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }


        private void EditNoteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(displayedNoteKey))
                return;

            editingNoteKey = displayedNoteKey;
            isNoteEditorOpen = true;
            PinNoteForEditing();
            editingImages = questNoteStore.GetImages(editingNoteKey).ToList();
            RenderEditorImages();

            bool hasOverride = questNoteStore.TryGetOverride(
                editingNoteKey,
                out IReadOnlyList<string> customNotes);
            IReadOnlyList<string> effectiveNotes = hasOverride
                ? customNotes
                : displayedDefaultNotes;

            NoteEditorTitleText.Text = CurrentQuestText.Text;
            NoteEditorTextBox.Text = string.Join(
                Environment.NewLine,
                QuestNoteStore.NormalizeSteps(effectiveNotes));
            NotesView.Visibility = Visibility.Collapsed;
            NoteEditorPanel.Visibility = Visibility.Visible;
            EditNoteButton.IsEnabled = false;
            Dispatcher.BeginInvoke(() =>
            {
                NoteEditorTextBox.Focus();
                NoteEditorTextBox.CaretIndex =
                    NoteEditorTextBox.Text.Length;
            });
        }


        private void SaveNoteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(editingNoteKey))
                return;

            try
            {
                questNoteStore.SaveWithImages(
                    editingNoteKey,
                    QuestNoteStore.ParseEditorText(NoteEditorTextBox.Text), editingImages);

                CloseNoteEditor();
                RenderDisplayedNote();
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException)
            {
                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Could not save the route note");
            }
        }


        private void ResetNoteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(editingNoteKey))
                return;

            try
            {
                questNoteStore.RemoveOverride(editingNoteKey);
                CloseNoteEditor();
                RenderDisplayedNote();
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException)
            {
                System.Windows.MessageBox.Show(
                    ex.Message,
                    "Could not restore the default note");
            }
        }


        private void CancelNoteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            CloseNoteEditor();
        }


        private void CloseNoteEditor()
        {
            isNoteEditorOpen = false;
            editingNoteKey = "";
            editingImages.Clear();
            EditorImagesPanel.Children.Clear();
            NoteEditorPanel.Visibility = Visibility.Collapsed;
            UpdateHistoryControls();
            NotesView.Visibility = Visibility.Visible;
            EditNoteButton.IsEnabled =
                !string.IsNullOrWhiteSpace(displayedNoteKey);
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void SpeedrunPanel_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed ||
                FindVisualParent<System.Windows.Controls.Button>(
                    e.OriginalSource as DependencyObject) != null ||
                FindVisualParent<System.Windows.Controls.TextBox>(
                    e.OriginalSource as DependencyObject) != null)
            {
                return;
            }

            DragMove();
        }


        private static T? FindVisualParent<T>(DependencyObject? child)
            where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T match)
                    return match;

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }


        // ============================================================
        // WINDOW CLEANUP
        // ============================================================

        private void MainWindow_Closed(
            object? sender,
            EventArgs e)
        {
            isQuestRecognitionShuttingDown = true;

            questDetectionTimer?.Stop();

            boardDetectionTimer?.Stop();

            boardOverlayWindow?.Close();


            questRecognitionService?.Dispose();

        }


        [DllImport(
            "user32.dll",
            SetLastError = true)]
        private static extern bool
            SetWindowDisplayAffinity(
                IntPtr hWnd,
                uint dwAffinity);
    }
}
