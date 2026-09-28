using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace AionSpeedrunOverlay.Capture
{
    public class ScreenCaptureService
    {
        /// <summary>
        /// Nimmt einen beliebigen rechteckigen Bereich des Bildschirms auf.
        /// </summary>
        public Bitmap CaptureRegion(
            int x,
            int y,
            int width,
            int height)
        {
            Bitmap bitmap = new Bitmap(
                width,
                height,
                PixelFormat.Format32bppArgb);

            using Graphics graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(
                x,
                y,
                0,
                0,
                new Size(width, height),
                CopyPixelOperation.SourceCopy);

            return bitmap;
        }

    }
}