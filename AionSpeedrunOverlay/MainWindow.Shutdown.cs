using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AionSpeedrunOverlay;

public partial class MainWindow
{
    private TaskCompletionSource? questRecognitionCompletion;
    private bool waitingForQuestShutdown;

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        // Prevent new passes before checking the current one. Both this handler
        // and the OCR continuation run on the WPF dispatcher.
        isQuestRecognitionShuttingDown = true;
        questDetectionTimer?.Stop();

        if (waitingForQuestShutdown)
        {
            e.Cancel = true;
            return;
        }

        Task? completion = questRecognitionCompletion?.Task;
        if (completion == null || completion.IsCompleted) return;

        // Keep the window/dispatcher alive until the entire pass has unwound,
        // including Page/Pix/frame disposal. Never interrupt native Tesseract.
        e.Cancel = true;
        waitingForQuestShutdown = true;
        await completion;
        waitingForQuestShutdown = false;
        Close();
    }
}
