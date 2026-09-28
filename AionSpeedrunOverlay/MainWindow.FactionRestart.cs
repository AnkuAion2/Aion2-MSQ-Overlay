using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace AionSpeedrunOverlay;

public partial class MainWindow
{
    private bool restartingForFaction;
    private Func<ProcessStartInfo, Process?> launchRestart = Process.Start;

    private bool CanRestartForFaction()
    {
        if (restartingForFaction || isQuestRecognitionShuttingDown) return false;
        if (!isNoteEditorOpen) return true;
        System.Windows.MessageBox.Show(appearanceWindow ?? (Window)this,
            "Save or cancel the note being edited before switching factions.",
            "Finish the current edit", MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }

    private async void RestartForFaction() => await RestartForFactionAsync();

    private async Task RestartForFactionAsync()
    {
        if (!CanRestartForFaction()) return;
        restartingForFaction = true;
        isQuestRecognitionShuttingDown = true;
        questDetectionTimer?.Stop();
        IsEnabled = false;
        if (appearanceWindow != null) appearanceWindow.IsEnabled = false;
        try
        {
            // Use the same completion barrier as normal shutdown. No native OCR is interrupted.
            Task? completion = questRecognitionCompletion?.Task;
            if (completion != null) await completion;
            if (!IsLoaded) return; // The user/application closed while waiting.
            string executable = Environment.ProcessPath
                ?? throw new InvalidOperationException("Could not locate the application executable.");
            var start = new ProcessStartInfo(executable) { UseShellExecute = false, WorkingDirectory = AppContext.BaseDirectory };
            if (string.Equals(Path.GetFileNameWithoutExtension(executable), "dotnet", StringComparison.OrdinalIgnoreCase))
                start.ArgumentList.Add(typeof(App).Assembly.Location);
            using var process = launchRestart(start)
                ?? throw new InvalidOperationException("The replacement application could not be started.");
            Close(); // Existing Closed handler disposes the engine and companion window.
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            isQuestRecognitionShuttingDown = false;
            questDetectionTimer?.Start();
            if (appearanceWindow != null) appearanceWindow.FactionStatus.Text = "Automatic restart failed. Close and reopen the overlay to apply the saved faction.";
            System.Windows.MessageBox.Show(appearanceWindow ?? (Window)this,
                "Automatic restart failed. Your faction choice is saved; close and reopen the overlay to apply it." + Environment.NewLine + Environment.NewLine + ex.Message,
                "Could not restart", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            restartingForFaction = false;
            if (IsLoaded)
            {
                IsEnabled = true;
                if (appearanceWindow != null) appearanceWindow.IsEnabled = true;
            }
        }
    }
}