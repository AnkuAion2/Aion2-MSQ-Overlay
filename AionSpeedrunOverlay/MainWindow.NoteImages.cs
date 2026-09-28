using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Button = System.Windows.Controls.Button;
using Image = System.Windows.Controls.Image;

namespace AionSpeedrunOverlay;

public partial class MainWindow
{
    private List<string> editingImages = new();
    private Window? noteImageViewer;
    // Bounded FIFO: retain at most twelve small, frozen previews, never full-size images.
    private readonly Dictionary<string, BitmapSource> noteThumbnails = new(StringComparer.Ordinal);
    private readonly Queue<string> thumbnailOrder = new();

    private BitmapSource GetNoteThumbnail(string data)
    {
        if (noteThumbnails.TryGetValue(data, out var cached)) return cached;
        using var stream = new MemoryStream(Convert.FromBase64String(data));
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
        int width = decoder.Frames[0].PixelWidth, height = decoder.Frames[0].PixelHeight;
        stream.Position = 0;
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        if (width >= height) image.DecodePixelWidth = Math.Min(256, width);
        else image.DecodePixelHeight = Math.Min(256, height);
        image.EndInit();
        image.Freeze();
        if (noteThumbnails.Count >= 12) noteThumbnails.Remove(thumbnailOrder.Dequeue());
        noteThumbnails.Add(data, image);
        thumbnailOrder.Enqueue(data);
        return image;
    }

    private void OpenNoteImage(string data)
    {
        try { ShowNoteImage(ReadNoteImage(data)); }
        catch (Exception ex) when (ex is FormatException or IOException or NotSupportedException or ArgumentException)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "Could not open image");
        }
    }

    private static BitmapSource ReadNoteImage(string data)
    {
        byte[] bytes = Convert.FromBase64String(data);
        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static string EncodeNoteImage(BitmapSource source)
    {
        if ((long)source.PixelWidth * source.PixelHeight > 40000000)
            throw new IOException("Image is too large (maximum 40 megapixels).");
        double scale = Math.Min(1, 1600.0 / Math.Max(source.PixelWidth, source.PixelHeight));
        BitmapSource image = scale < 1 ? new TransformedBitmap(source, new ScaleTransform(scale, scale)) : source;
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        if (stream.Length > 2 * 1024 * 1024)
            throw new IOException("Image is still larger than 2 MB after resizing. Please crop it first.");
        return Convert.ToBase64String(stream.ToArray());
    }

    private void AddNoteImage_Click(object sender, RoutedEventArgs e)
    {
        ImportNoteImage(() =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg", Multiselect = false };
            if (dialog.ShowDialog(this) != true) return null;
            var file = new FileInfo(dialog.FileName);
            if (file.Length > 10 * 1024 * 1024) throw new IOException("Choose an image smaller than 10 MB.");
            using var stream = File.OpenRead(dialog.FileName);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            if (decoder is not PngBitmapDecoder && decoder is not JpegBitmapDecoder)
                throw new IOException("Only PNG and JPEG images are supported.");
            return decoder.Frames[0];
        });
    }

    private void PasteNoteImage_Click(object sender, RoutedEventArgs e) =>
        ImportNoteImage(() => System.Windows.Clipboard.GetImage()
            ?? throw new IOException("The clipboard does not contain an image."));

    private void ImportNoteImage(Func<BitmapSource?> getImage)
    {
        if (!isNoteEditorOpen) return;
        try
        {
            if (editingImages.Count >= 3) throw new IOException("You can attach up to three images per step.");
            var image = getImage();
            if (image == null) return;
            editingImages.Add(EncodeNoteImage(image));
            RenderEditorImages();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException
            or ArgumentException or InvalidOperationException or COMException)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "Could not add image");
        }
    }

    private void RenderNoteImages()
    {
        NoteImagesPanel.Children.Clear();
        foreach (string data in questNoteStore.GetImages(displayedNoteKey).Take(3))
            NoteImagesPanel.Children.Add(CreateImageTile(data, false));
        clickThrough?.InvalidateInputTargets();
        NoteImagesPanel.Visibility = NoteImagesPanel.Children.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        if (NoteImagesPanel.Children.Count > 0 && QuestNotesList.Visibility == Visibility.Collapsed)
            EmptyNotesText.Visibility = Visibility.Collapsed;
    }

    private void RenderEditorImages()
    {
        EditorImagesPanel.Children.Clear();
        foreach (string data in editingImages.ToArray())
            EditorImagesPanel.Children.Add(CreateImageTile(data, true));
        clickThrough?.InvalidateInputTargets();
        AddNoteImageButton.IsEnabled = PasteNoteImageButton.IsEnabled = editingImages.Count < 3;
    }

    private FrameworkElement CreateImageTile(string data, bool editable)
    {
        var tile = new StackPanel { Margin = new Thickness(3) };
        try
        {
            var source = GetNoteThumbnail(data);
            var button = new Button {
                Style = (Style)FindResource("QuietButton"), Padding = new Thickness(3),
                Content = new Image { Source = source, Width = 82, Height = 54, Stretch = Stretch.Uniform },
                ToolTip = "Open image"
            };
            button.Click += (_, _) => OpenNoteImage(data);
            tile.Children.Add(button);
        }
        catch (Exception ex) when (ex is FormatException or IOException or NotSupportedException or ArgumentException)
        {
            tile.Children.Add(new TextBlock { Text = "Image unavailable", FontSize = 10, Foreground = System.Windows.Media.Brushes.White });
        }
        if (editable)
        {
            var remove = new Button { Content = "Remove", Style = (Style)FindResource("QuietButton"), Margin = new Thickness(0,3,0,0) };
            remove.Click += (_, _) => { editingImages.Remove(data); RenderEditorImages(); };
            tile.Children.Add(remove);
        }
        return tile;
    }

    private void ShowNoteImage(BitmapSource source)
    {
        noteImageViewer?.Close();
        noteImageViewer = new Window {
            Owner = this, Title = "Note image", Width = Math.Min(1000, SystemParameters.WorkArea.Width * .8),
            Height = Math.Min(750, SystemParameters.WorkArea.Height * .8),
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Topmost = true,
            Background = System.Windows.Media.Brushes.Black,
            Content = new Image { Source = source, Stretch = Stretch.Uniform, Margin = new Thickness(12) }
        };
        var viewer = noteImageViewer;
        viewer.Closed += (_, _) =>
        {
            viewer.Content = null;
            if (ReferenceEquals(noteImageViewer, viewer)) noteImageViewer = null;
        };
        viewer.Show();
    }
}
