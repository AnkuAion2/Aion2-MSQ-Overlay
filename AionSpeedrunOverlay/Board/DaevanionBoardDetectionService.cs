using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using AionSpeedrunOverlay.Capture;
using AionSpeedrunOverlay.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using DrawingRectangle = System.Drawing.Rectangle;
using CvRectangle = OpenCvSharp.Rect;

namespace AionSpeedrunOverlay.Board
{
    public sealed class DaevanionBoardDetectionResult
    {
        public bool IsOpen { get; init; }

        public DaevanionBoardCategory? Category { get; init; }

        public double Score { get; init; }

        public double CenterStartGoldRatio { get; init; }

        public double NodePanelRatio { get; init; }

        public double TitleMatch { get; init; }
    }


    public sealed class DaevanionBoardDetectionService
    {
        private const int ReferenceWidth = 1920;
        private const int ReferenceHeight = 1080;
        private const int ReferenceTabY = 87;
        private const int ReferenceTabHeight = 19;
        private const int ReferenceTabWidth = 160;
        private const double MinimumReferenceScore = 500;
        private const double MinimumCenterStartGoldRatio = 0.06;
        private const double MinimumNodePanelRatio = 0.03;
        private const double MinimumTitleMatch = 0.55;

        private const string TitleTemplateBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAM0AAAAtCAAAAAAok7A3AAAC8ElEQVRoBd3BAW4b2wFFMZ79L/r2jcZOYlt1WiToh0rm/0m+Wl5UnlheU55ZXlKembyiPLW8" +
            "ojy1vKI8M3lFeWbyivLE5CXlq5GXlC9GXlM+G/lHLH8o7ybHyD9i5M/kzQgjlufmh/xdc+SP5M2EEZbn5hf5m+bIH8ltxMix/BsjlyF/1eTP5DYyclkuy2dD" +
            "LkP+puUP5TYyuYwcy2dDHob8xvJu+WL51fLE8h/Kw4gJ8xCTT4bcRi5zybs5wshtcswlDyNz5Bi5zJHbyBz5nTyMmJg3MfloyG2IeZOHoaEhDyPmTY7R3GII" +
            "8ybHaG75jTwMmRxz5Jh8NOQ2ZIQhx8gxGnKZGGEIc8kQI0yYI8wlQ34jlzViuYxcJh8NuQ2ZXIYYuYyGXCYmlyHmCENGjBxzxBxhyPdyLCOWy8hl8tGQ25DJ" +
            "w4iRh9HIMWLyMGLIMWTE5GGIIceQ74XFiOUycox8NOQ25IeRIQ+jIUx+GmLkMmRk5GGOjFyGfC9vRt6MHJNPhtyGPMxD5lcNMXKbW4xchoyM3IaMXIZ8L29G" +
            "LMcII58MuQ0xl4zMrzJi5JiHRoxchoyM3IaMXIZ8L+9GLEyOyWdDbkOGHCNDfhoyOYYcI0YuQ0aG3EZGLkO+l3dDLCZMvhhyGxpyGRny09AIQy4jRi5DRobc" +
            "RkYuQ76XH0ZYLCNfDbmNDLmMDPnFaHIMuYwYuQwZGXIbGbkM+V5+GsJcclk+GPIwZMhlxJCf5pJjyGXEyGXIiCEPEyOXId/LL4Ycy8PkoyGXIYYcQwx5WI45" +
            "cgw5hhi5DBkxR44RI5ch38uv5ojlmHw25BjCHDFHGMLIMeQyR8yRIZchI8wRI4ZchnwvH81P+Wp+ymVuGWHe5DLkYW4ZjTwMGTnmTRh5GPK9fDEPeWJ+yLsh" +
            "x+Qyl9wm74Yc0+RhjiZv5shl8jBHvpdnlueW/9bkfyXPLC8pzywvKc8sLynPLC8pzywvKc8sLynPLC8pzywvKc8sL+lfFuprPU8xKfkAAAAASUVORK5CYII=";

        private static readonly Lazy<Mat> TitleTemplate =
            new(CreateTitleTemplate);

        private static readonly DrawingRectangle HeaderArea =
            new(0, 0, 960, 108);

        private static readonly DrawingRectangle TitleArea =
            new(17, 15, 205, 45);

        private static readonly DrawingRectangle CenterStartArea =
            new(730, 548, 76, 80);

        private static readonly DrawingRectangle NodePanelArea =
            new(1540, 105, 180, 60);

        private static readonly (DaevanionBoardCategory Category, int CenterX)[] Tabs =
        {
            (DaevanionBoardCategory.Nezekan, 330),
            (DaevanionBoardCategory.Zikel, 510),
            (DaevanionBoardCategory.Vaizel, 690),
            (DaevanionBoardCategory.Triniel, 870)
        };

        private readonly ScreenCaptureService screenCaptureService;


        public DaevanionBoardDetectionService(
            ScreenCaptureService screenCaptureService)
        {
            this.screenCaptureService = screenCaptureService;
        }


        public DaevanionBoardDetectionResult DetectPrimaryScreen()
        {
            DrawingRectangle bounds =
                System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            double scaleX = bounds.Width / (double)ReferenceWidth;
            double scaleY = bounds.Height / (double)ReferenceHeight;

            using Bitmap header = CaptureScaledRegion(
                bounds,
                HeaderArea,
                scaleX,
                scaleY);
            double titleMatch = GetTitleMatch(header, scaleX, scaleY);

            if (titleMatch < MinimumTitleMatch)
                return RejectedByTitle(titleMatch);

            using Bitmap centerStart = CaptureScaledRegion(
                bounds,
                CenterStartArea,
                scaleX,
                scaleY);
            using Bitmap nodePanel = CaptureScaledRegion(
                bounds,
                NodePanelArea,
                scaleX,
                scaleY);

            return Analyze(
                header,
                centerStart,
                nodePanel,
                scaleX,
                scaleY,
                titleMatch);
        }


        public DaevanionBoardDetectionResult DetectImageFile(string path)
        {
            using Bitmap fullScreenshot = new(path);
            double scaleX = fullScreenshot.Width / (double)ReferenceWidth;
            double scaleY = fullScreenshot.Height / (double)ReferenceHeight;

            using Bitmap header = CloneScaledRegion(
                fullScreenshot,
                HeaderArea,
                scaleX,
                scaleY);
            double titleMatch = GetTitleMatch(header, scaleX, scaleY);

            if (titleMatch < MinimumTitleMatch)
                return RejectedByTitle(titleMatch);

            using Bitmap centerStart = CloneScaledRegion(
                fullScreenshot,
                CenterStartArea,
                scaleX,
                scaleY);
            using Bitmap nodePanel = CloneScaledRegion(
                fullScreenshot,
                NodePanelArea,
                scaleX,
                scaleY);

            return Analyze(
                header,
                centerStart,
                nodePanel,
                scaleX,
                scaleY,
                titleMatch);
        }


        private DaevanionBoardDetectionResult Analyze(
            Bitmap header,
            Bitmap centerStart,
            Bitmap nodePanel,
            double scaleX,
            double scaleY,
            double titleMatch)
        {
            List<(DaevanionBoardCategory Category, double Score)> scores =
                GetTabScores(header, scaleX, scaleY);
            var ordered = scores
                .OrderByDescending(entry => entry.Score)
                .ToArray();

            double scaledMinimumScore =
                MinimumReferenceScore * scaleX * scaleY;
            bool dominantTab =
                ordered[1].Score <= ordered[0].Score * 0.35;
            bool validTab =
                ordered[0].Score >= scaledMinimumScore && dominantTab;

            double centerStartGoldRatio = MaskRatio(
                centerStart,
                new Scalar(10, 35, 80),
                new Scalar(42, 255, 255));
            double nodePanelRatio = MaskRatio(
                nodePanel,
                new Scalar(80, 10, 80),
                new Scalar(140, 255, 255));

            bool isOpen =
                validTab &&
                titleMatch >= MinimumTitleMatch &&
                centerStartGoldRatio >= MinimumCenterStartGoldRatio &&
                nodePanelRatio >= MinimumNodePanelRatio;

            return new DaevanionBoardDetectionResult
            {
                IsOpen = isOpen,
                Category = isOpen ? ordered[0].Category : null,
                Score = ordered[0].Score,
                CenterStartGoldRatio = centerStartGoldRatio,
                NodePanelRatio = nodePanelRatio,
                TitleMatch = titleMatch
            };
        }


        private static DaevanionBoardDetectionResult RejectedByTitle(
            double titleMatch)
        {
            return new DaevanionBoardDetectionResult
            {
                IsOpen = false,
                Category = null,
                TitleMatch = titleMatch
            };
        }


        private static double GetTitleMatch(
            Bitmap header,
            double scaleX,
            double scaleY)
        {
            using Mat source = BitmapConverter.ToMat(header);
            using Mat gray = new();
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGRA2GRAY);

            DrawingRectangle scaledTitle = Scale(
                TitleArea,
                scaleX,
                scaleY);
            using Mat crop = new(
                gray,
                new CvRectangle(
                    scaledTitle.X,
                    scaledTitle.Y,
                    scaledTitle.Width,
                    scaledTitle.Height));
            using Mat normalized = new();
            using Mat binary = new();
            using Mat match = new();
            Cv2.Resize(crop, normalized, TitleTemplate.Value.Size());
            Cv2.Threshold(
                normalized,
                binary,
                100,
                255,
                ThresholdTypes.Binary);
            Cv2.MatchTemplate(
                binary,
                TitleTemplate.Value,
                match,
                TemplateMatchModes.CCoeffNormed);

            return match.At<float>(0, 0);
        }


        private static Mat CreateTitleTemplate()
        {
            byte[] bytes = Convert.FromBase64String(TitleTemplateBase64);
            return Cv2.ImDecode(bytes, ImreadModes.Grayscale);
        }


        private static List<(DaevanionBoardCategory Category, double Score)>
            GetTabScores(
                Bitmap header,
                double scaleX,
                double scaleY)
        {
            using Mat source = BitmapConverter.ToMat(header);
            using Mat bgr = new();
            using Mat hsv = new();
            Cv2.CvtColor(source, bgr, ColorConversionCodes.BGRA2BGR);
            Cv2.CvtColor(bgr, hsv, ColorConversionCodes.BGR2HSV);

            List<(DaevanionBoardCategory Category, double Score)> scores =
                new();

            foreach ((DaevanionBoardCategory category, int centerX) in Tabs)
            {
                int width = Math.Max(
                    1,
                    (int)Math.Round(ReferenceTabWidth * scaleX));
                int height = Math.Max(
                    1,
                    (int)Math.Round(ReferenceTabHeight * scaleY));
                int x = (int)Math.Round(
                    (centerX - ReferenceTabWidth / 2.0) * scaleX);
                int y = (int)Math.Round(ReferenceTabY * scaleY);

                x = Math.Clamp(x, 0, hsv.Width - 1);
                y = Math.Clamp(y, 0, hsv.Height - 1);
                width = Math.Clamp(width, 1, hsv.Width - x);
                height = Math.Clamp(height, 1, hsv.Height - y);

                using Mat tab = new(
                    hsv,
                    new CvRectangle(x, y, width, height));
                using Mat activeGlowMask = new();
                Cv2.InRange(
                    tab,
                    new Scalar(85, 10, 85),
                    new Scalar(140, 255, 255),
                    activeGlowMask);

                scores.Add((category, Cv2.CountNonZero(activeGlowMask)));
            }

            return scores;
        }


        private static double MaskRatio(
            Bitmap bitmap,
            Scalar lower,
            Scalar upper)
        {
            using Mat source = BitmapConverter.ToMat(bitmap);
            using Mat bgr = new();
            using Mat hsv = new();
            using Mat mask = new();
            Cv2.CvtColor(source, bgr, ColorConversionCodes.BGRA2BGR);
            Cv2.CvtColor(bgr, hsv, ColorConversionCodes.BGR2HSV);
            Cv2.InRange(hsv, lower, upper, mask);

            return Cv2.CountNonZero(mask) /
                   (double)(mask.Width * mask.Height);
        }


        private Bitmap CaptureScaledRegion(
            DrawingRectangle screenBounds,
            DrawingRectangle referenceArea,
            double scaleX,
            double scaleY)
        {
            DrawingRectangle area = Scale(
                referenceArea,
                scaleX,
                scaleY);

            return screenCaptureService.CaptureRegion(
                screenBounds.Left + area.X,
                screenBounds.Top + area.Y,
                area.Width,
                area.Height);
        }


        private static Bitmap CloneScaledRegion(
            Bitmap screenshot,
            DrawingRectangle referenceArea,
            double scaleX,
            double scaleY)
        {
            DrawingRectangle area = Scale(
                referenceArea,
                scaleX,
                scaleY);

            return screenshot.Clone(area, PixelFormat.Format32bppArgb);
        }


        private static DrawingRectangle Scale(
            DrawingRectangle area,
            double scaleX,
            double scaleY)
        {
            return new DrawingRectangle(
                (int)Math.Round(area.X * scaleX),
                (int)Math.Round(area.Y * scaleY),
                Math.Max(1, (int)Math.Round(area.Width * scaleX)),
                Math.Max(1, (int)Math.Round(area.Height * scaleY)));
        }
    }
}
