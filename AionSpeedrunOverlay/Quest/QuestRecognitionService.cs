using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using AionSpeedrunOverlay.Capture;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Tesseract;
using CvRect = OpenCvSharp.Rect;

namespace AionSpeedrunOverlay.Quest
{
    public sealed class QuestRecognitionResult
    {
        public QuestMatch Match { get; init; } = new QuestMatch();
        public IReadOnlyList<string> RecognizedLines { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> RecognizedObjectiveLines { get; init; } = Array.Empty<string>();
        public int YellowLineCandidates { get; init; }
        public int WhiteLineCandidates { get; init; }
        public int OcrTitleLines { get; init; }
        public int OcrObjectiveLines { get; init; }
        public Rectangle ContentBounds { get; init; }
        public IReadOnlyList<Rectangle> TitleCandidateBounds { get; init; } =
            Array.Empty<Rectangle>();
        public IReadOnlyList<Rectangle> ObjectiveCandidateBounds { get; init; } =
            Array.Empty<Rectangle>();
    }


    public sealed class QuestRecognitionFrame : IDisposable
    {
        public QuestRecognitionResult Result { get; }
        public Bitmap QuestRegion { get; }


        public QuestRecognitionFrame(
            QuestRecognitionResult result,
            Bitmap questRegion)
        {
            Result = result;
            QuestRegion = questRegion;
        }


        public void Dispose()
        {
            QuestRegion.Dispose();
        }
    }


    public sealed class QuestRecognitionService : IDisposable
    {
        private const double SearchLeftRatio = 0.855;
        private const double SearchTopRatio = 0.245;
        private const double SearchWidthRatio = 0.14;
        private const double SearchHeightRatio = 0.40;
        private const int MaxTitleOcrLines = 5;
        private const int MaxObjectiveOcrLines = 7;
        private const int ObjectiveWindowHeight = 115;
        private const double MinimumStrongWhiteCoverage = 0.70;

        private static readonly HashSet<string>
            AdaptiveObjectiveFallbackQuestIds = new(
                new[]
                {
                    "quest:2102360",
                    "quest:2102380"
                },
                StringComparer.Ordinal);

        private readonly ScreenCaptureService screenCaptureService;
        private readonly QuestCatalog catalog;
        private readonly TesseractEngine ocrEngine;


        public QuestRecognitionService(
            ScreenCaptureService screenCaptureService,
            string questCatalogPath,
            string tessdataPath,
            string? levelLessContextPath = null)
        {
            this.screenCaptureService = screenCaptureService;
            catalog = new QuestCatalog(
                questCatalogPath,
                levelLessContextPath);

            string trainedDataPath = Path.Combine(tessdataPath, "eng.traineddata");

            if (!File.Exists(trainedDataPath))
            {
                throw new FileNotFoundException(
                    "The English OCR training data was not found.",
                    trainedDataPath);
            }

            ocrEngine = new TesseractEngine(
                tessdataPath,
                "eng",
                EngineMode.LstmOnly);

            ocrEngine.SetVariable(
                "tessedit_char_whitelist",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789[]().-' ");
        }


        public QuestRecognitionResult RecognizePrimaryScreen()
        {
            using QuestRecognitionFrame frame = RecognizePrimaryScreenFrame();
            return frame.Result;
        }


        public QuestRecognitionFrame RecognizePrimaryScreenFrame()
        {
            Bitmap screenshot = CapturePrimaryQuestRegion();

            try
            {
                return new QuestRecognitionFrame(
                    RecognizeQuestRegion(screenshot),
                    screenshot);
            }
            catch
            {
                screenshot.Dispose();
                throw;
            }
        }


        public Bitmap CapturePrimaryQuestRegion()
        {
            Rectangle bounds =
                System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            Rectangle region = CalculateSearchRegion(
                bounds.Width,
                bounds.Height);

            return screenCaptureService.CaptureRegion(
                bounds.Left + region.X,
                bounds.Top + region.Y,
                region.Width,
                region.Height);
        }


        public QuestRecognitionResult RecognizeImageFile(string path)
        {
            using Bitmap screenshot = new Bitmap(path);
            return Recognize(screenshot);
        }


        public QuestRecognitionResult RecognizeQuestRegionImageFile(string path)
        {
            using Bitmap screenshot = new Bitmap(path);
            return RecognizeQuestRegion(screenshot);
        }


        public QuestRecognitionResult Recognize(Bitmap screenshot)
        {
            Rectangle searchRegion = CalculateSearchRegion(
                screenshot.Width,
                screenshot.Height);

            using Mat source = BitmapConverter.ToMat(screenshot);
            using Mat searchImage = new Mat(
                source,
                new CvRect(
                    searchRegion.X,
                    searchRegion.Y,
                    searchRegion.Width,
                    searchRegion.Height));

            return RecognizeSearchImage(searchImage);
        }


        private QuestRecognitionResult RecognizeQuestRegion(Bitmap screenshot)
        {
            using Mat source = BitmapConverter.ToMat(screenshot);
            return RecognizeSearchImage(source);
        }


        private QuestRecognitionResult RecognizeSearchImage(Mat searchImage)
        {

            using Mat bgr = new Mat();
            using Mat hsv = new Mat();
            using Mat yellowMask = new Mat();
            using Mat yellowOcrMask = new Mat();
            using Mat secondaryQuestMask = new Mat();
            using Mat whiteMask = new Mat();
            using Mat whiteOcrMask = new Mat();
            using Mat brightWhiteMask = new Mat();

            Cv2.CvtColor(searchImage, bgr, ColorConversionCodes.BGRA2BGR);
            Cv2.CvtColor(bgr, hsv, ColorConversionCodes.BGR2HSV);

            // Breiter Gold-/Gelbbereich für leicht wechselnde Helligkeit,
            // Bloom und unterschiedliche UI-Kontraste.
            Cv2.InRange(
                hsv,
                new Scalar(10, 65, 125),
                new Scalar(42, 255, 255),
                yellowMask);

            // Die engere Maske lokalisiert die Zeile. Für das eigentliche OCR
            // nehmen wir zusätzlich schwächer gesättigte Kantenglättungs-Pixel
            // mit, damit Ziffern wie 9 nicht zu 0 zerfallen.
            Cv2.InRange(
                hsv,
                new Scalar(7, 28, 115),
                new Scalar(48, 255, 255),
                yellowOcrMask);

            // Optionale/sekundäre Quests werden unter der gelben Hauptquest
            // in Grün/Türkis dargestellt. Deren weiße Ziele dürfen niemals
            // als Schritt der gelben Quest ausgewertet werden.
            Cv2.InRange(
                hsv,
                new Scalar(65, 60, 110),
                new Scalar(95, 255, 255),
                secondaryQuestMask);

            // Die aktuell sichtbaren Questziele stehen direkt unter dem
            // gelben Titel in hellem Weiß/Grau.
            Cv2.InRange(
                hsv,
                new Scalar(0, 0, 150),
                new Scalar(179, 90, 255),
                whiteMask);

            // Kleine UI-Skalierungen zeichnen die weiße Schrift teilweise nur
            // mit etwa 110-160 Helligkeit. Die strengere Maske lokalisiert
            // normale Zeilen; diese zweite Maske dient als OCR- und
            // Lokalisierungs-Fallback direkt unter einem sicher erkannten
            // Questtitel.
            Cv2.InRange(
                hsv,
                new Scalar(0, 0, 105),
                new Scalar(179, 80, 255),
                whiteOcrMask);

            // Auf dem hellblauen, halbtransparenten Questtracker kann der
            // Hintergrund mit der normalen Weißmaske verschmelzen. Eine sehr
            // helle Maske bleibt als reine Lokalisierungshilfe erhalten und
            // wird nur nach einer sicher erkannten Quest ohne Schritt genutzt.
            Cv2.InRange(
                hsv,
                new Scalar(0, 0, 190),
                new Scalar(179, 80, 255),
                brightWhiteMask);

            using Mat morphologyKernel = Cv2.GetStructuringElement(
                MorphShapes.Rect,
                new OpenCvSharp.Size(2, 2));

            Cv2.MorphologyEx(
                yellowMask,
                yellowMask,
                MorphTypes.Close,
                morphologyKernel);

            Cv2.MorphologyEx(
                whiteMask,
                whiteMask,
                MorphTypes.Close,
                morphologyKernel);

            Cv2.MorphologyEx(
                secondaryQuestMask,
                secondaryQuestMask,
                MorphTypes.Close,
                morphologyKernel);

            Cv2.MorphologyEx(
                whiteOcrMask,
                whiteOcrMask,
                MorphTypes.Close,
                morphologyKernel);

            Cv2.MorphologyEx(
                brightWhiteMask,
                brightWhiteMask,
                MorphTypes.Close,
                morphologyKernel);

            List<CvRect> titleCandidates = FindTextLines(yellowMask);
            List<CvRect> secondaryQuestCandidates =
                FindTextLines(secondaryQuestMask);
            List<CvRect> objectiveCandidates = FindTextLines(whiteMask);

            List<CvRect> titleOcrCandidates = titleCandidates
                .OrderBy(line => line.Y)
                .Take(MaxTitleOcrLines)
                .ToList();

            List<RecognizedTextLine> recognizedTitleEntries =
                RecognizeLines(yellowOcrMask, titleOcrCandidates);

            List<string> recognizedTitleLines = recognizedTitleEntries
                .Select(line => line.Text)
                .ToList();

            // Zuerst nur die Quest bestimmen. Erst danach lesen wir die weißen
            // Ziele unmittelbar unter genau dieser Titelzeile. Das spart den
            // Großteil der OCR-Arbeit und verhindert, dass Ziele anderer
            // eingeblendeter Quests den Schritt verfälschen.
            QuestMatch titleMatch = catalog.FindBestMatch(
                recognizedTitleLines,
                Array.Empty<string>());

            // Kleine Levelziffern zerfallen je nach Hintergrund entweder in
            // der breiten oder in der strengen Gelbmaske. Nur wenn der erste
            // Durchlauf keine sichere Quest liefert, lesen wir dieselben
            // Zeilen ein zweites Mal. Der Katalog darf anschließend mehrere
            // Levelvarianten anhand des Questtitels auflösen; widersprüchliche
            // Varianten ohne klaren Titel bleiben abgelehnt.
            if (titleMatch.Quest == null ||
                titleMatch.RejectionReason == "quest-ambiguous")
            {
                List<RecognizedTextLine> fallbackTitleEntries =
                    RecognizeLines(yellowMask, titleOcrCandidates);

                foreach (RecognizedTextLine entry in fallbackTitleEntries)
                {
                    if (recognizedTitleEntries.Any(existing =>
                        string.Equals(
                            existing.Text,
                            entry.Text,
                            StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    recognizedTitleEntries.Add(entry);
                    recognizedTitleLines.Add(entry.Text);
                }

                titleMatch = catalog.FindBestMatch(
                    recognizedTitleLines,
                    Array.Empty<string>());
            }

            List<CvRect> objectiveOcrCandidates = new();
            List<CvRect> dimObjectiveOcrCandidates = new();
            List<CvRect> brightObjectiveOcrCandidates = new();
            bool usedDimObjectiveFallback = false;

            if (titleMatch.Quest != null)
            {
                RecognizedTextLine? matchedTitle = recognizedTitleEntries
                    .FirstOrDefault(line => string.Equals(
                        line.Text,
                        titleMatch.RawText,
                        StringComparison.Ordinal));

                if (matchedTitle != null)
                {
                    // Beide Linienrechtecke enthalten Padding. Bei kleinen
                    // UI-Skalierungen überlappen sich Titel- und Ziel-Bounds
                    // deshalb um einige Pixel, obwohl die Texte getrennt sind.
                    int objectiveTop = matchedTitle.Bounds.Bottom - 14;
                    int objectiveBottom = matchedTitle.Bounds.Bottom +
                        ObjectiveWindowHeight;

                    int nextSecondaryQuestTop = secondaryQuestCandidates
                        .Where(line => line.Y >= matchedTitle.Bounds.Bottom)
                        .Where(line => line.Y <= objectiveBottom)
                        .Select(line => line.Y)
                        .DefaultIfEmpty(int.MaxValue)
                        .Min();

                    if (nextSecondaryQuestTop != int.MaxValue)
                        objectiveBottom = nextSecondaryQuestTop - 1;

                    List<CvRect> strongObjectiveCandidates =
                        objectiveCandidates
                        .Where(line => line.Y >= objectiveTop)
                        .Where(line => line.Y <= objectiveBottom)
                        .OrderBy(line => line.Y)
                        .ToList();
                    dimObjectiveOcrCandidates =
                        FindTextLines(whiteOcrMask)
                        .Where(line => line.Y >= objectiveTop)
                        .Where(line => line.Y <= objectiveBottom)
                        .OrderBy(line => line.Y)
                        .ToList();
                    brightObjectiveOcrCandidates =
                        FindTextLines(brightWhiteMask)
                        .Where(line => line.Y >= objectiveTop)
                        .Where(line => line.Y <= objectiveBottom)
                        .OrderBy(line => line.Y)
                        .ToList();

                    // Wenn der halbtransparente Tracker mit der Weißmaske zu
                    // einer großen Fläche verschmilzt, liefert FindTextLines
                    // gar kein Rechteck. Drei kurze, überlappende Bänder direkt
                    // unter dem sicher zugeordneten Titel decken ein- und
                    // zweizeilige Ziele ab, ohne spätere Quests einzubeziehen.
                    int fallbackTop = Math.Max(
                        0,
                        matchedTitle.Bounds.Bottom - 2);
                    int fallbackBottom = Math.Min(
                        Math.Min(objectiveBottom, searchImage.Height),
                        fallbackTop + 48);
                    List<CvRect> fixedObjectiveBands = new();

                    // Ein am unteren Bildrand abgeschnittenes Ziel bleibt
                    // absichtlich unberuecksichtigt: Ein Fragment darf keinen
                    // Schritt bestaetigen.
                    if (fallbackBottom - fallbackTop >= 32)
                    {
                        for (int bandTop = fallbackTop;
                            bandTop < fallbackBottom;
                            bandTop += 14)
                        {
                            int bandHeight = Math.Min(
                                22,
                                fallbackBottom - bandTop);

                            if (bandHeight >= 10)
                            {
                                fixedObjectiveBands.Add(new CvRect(
                                    0,
                                    bandTop,
                                    searchImage.Width,
                                    bandHeight));
                            }
                        }
                    }

                    brightObjectiveOcrCandidates = fixedObjectiveBands
                        .Concat(brightObjectiveOcrCandidates)
                        .ToList();

                    objectiveOcrCandidates = strongObjectiveCandidates
                        .Take(MaxObjectiveOcrLines)
                        .ToList();

                    if (objectiveOcrCandidates.Count == 0)
                    {
                        objectiveOcrCandidates = dimObjectiveOcrCandidates
                            .Take(MaxObjectiveOcrLines)
                            .ToList();
                        usedDimObjectiveFallback =
                            objectiveOcrCandidates.Count > 0;
                    }
                }
            }

            List<RecognizedTextLine> recognizedObjectiveEntries =
                RecognizeObjectiveLines(
                    whiteMask,
                    whiteOcrMask,
                    objectiveOcrCandidates,
                    usedDimObjectiveFallback);

            List<string> recognizedObjectiveLines = recognizedObjectiveEntries
                .Select(line => line.Text)
                .ToList();

            QuestMatch finalMatch = catalog.FindBestMatch(
                recognizedTitleLines,
                recognizedObjectiveLines);

            // Auf hellen oder bläulichen Hintergründen kann die strenge
            // Weißmaske einzelne Buchstaben vollständig verlieren. Nur wenn
            // die Quest sicher ist, aber kein Schritt gefunden wurde, lesen
            // wir dieselben bereits lokalisierten Zielzeilen zusätzlich mit
            // der breiteren Maske. Ein Fallback darf ausschließlich einen
            // eindeutigen Schritt derselben Quest oder die direkte nächste
            // Questübergabe ergänzen.
            if (finalMatch.Quest != null &&
                finalMatch.Step == null &&
                !finalMatch.IsQuestObjectiveMatch &&
                !usedDimObjectiveFallback &&
                dimObjectiveOcrCandidates.Count > 0)
            {
                List<RecognizedTextLine> dimObjectiveEntries =
                    RecognizeObjectiveLines(
                        whiteMask,
                        whiteOcrMask,
                        dimObjectiveOcrCandidates
                            .Take(MaxObjectiveOcrLines)
                            .ToList(),
                        true);
                List<string> combinedObjectiveLines = recognizedObjectiveLines
                    .Concat(dimObjectiveEntries.Select(line => line.Text))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                QuestMatch dimMatch = catalog.FindBestMatch(
                    recognizedTitleLines,
                    combinedObjectiveLines);

                if (IsSafeObjectiveFallback(finalMatch, dimMatch) &&
                    dimMatch.RejectionReason != "step-ambiguous")
                {
                    finalMatch = dimMatch;
                    recognizedObjectiveEntries = recognizedObjectiveEntries
                        .Concat(dimObjectiveEntries)
                        .GroupBy(
                            line => line.Text,
                            StringComparer.Ordinal)
                        .Select(group => group.First())
                        .ToList();
                    recognizedObjectiveLines = combinedObjectiveLines;
                }
            }

            // Die besonders helle Lokalisierung ist absichtlich der letzte
            // Fallback. Sie darf nur einen eindeutigen Schritt der bereits
            // feststehenden Quest oder deren direkte nächste Übergabe ergänzen
            // und kann daher nie eine beliebige andere Quest auswählen.
            if (finalMatch.Quest != null &&
                finalMatch.Step == null &&
                !finalMatch.IsQuestObjectiveMatch &&
                brightObjectiveOcrCandidates.Count > 0)
            {
                List<RecognizedTextLine> brightObjectiveEntries =
                    RecognizeLines(
                        brightWhiteMask,
                        brightObjectiveOcrCandidates
                            .Take(MaxObjectiveOcrLines)
                            .ToList());
                List<string> brightObjectiveLines = brightObjectiveEntries
                    .Select(line => line.Text)
                    .ToList();
                List<string> wrappedObjectiveLines = brightObjectiveLines
                    .Zip(
                        brightObjectiveLines.Skip(1),
                        (first, second) => $"{first} {second}")
                    .ToList();
                List<string> combinedObjectiveLines = recognizedObjectiveLines
                    .Concat(brightObjectiveLines)
                    .Concat(wrappedObjectiveLines)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                // Manche halbtransparenten Tracker-Hintergründe löschen in
                // allen Farbmasken Teile der weißen Schrift. Eine lokale
                // Graustufen-Binarisierung liest nur dieselben, bereits unter
                // dem sicheren Titel begrenzten Bänder. Die Katalogprüfung
                // unten darf weiterhin ausschließlich die aktuelle Quest
                // oder deren direkten Übergabeschritt bestätigen.
                List<RecognizedTextLine> adaptiveObjectiveEntries =
                    ShouldUseAdaptiveObjectiveFallback(finalMatch)
                        ? RecognizeAdaptiveLines(
                            bgr,
                            brightObjectiveOcrCandidates
                                .Take(MaxObjectiveOcrLines)
                                .ToList())
                        : new List<RecognizedTextLine>();
                List<string> adaptiveObjectiveLines =
                    adaptiveObjectiveEntries
                    .Select(line => line.Text)
                    .ToList();
                List<string> fallbackObjectiveLines = brightObjectiveLines
                    .Concat(wrappedObjectiveLines)
                    .Concat(adaptiveObjectiveLines)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                combinedObjectiveLines = combinedObjectiveLines
                    .Concat(adaptiveObjectiveLines)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                QuestMatch? brightMatch = fallbackObjectiveLines
                    .Select(line => catalog.FindBestMatch(
                        recognizedTitleLines,
                        recognizedObjectiveLines
                            .Append(line)
                            .Distinct(StringComparer.Ordinal)))
                    .Where(candidate =>
                        IsSafeObjectiveFallback(finalMatch, candidate) &&
                        candidate.RejectionReason != "step-ambiguous")
                    .OrderByDescending(candidate => candidate.StepScore)
                    .FirstOrDefault();

                // Auch abgelehnte Fallback-Texte gehören in die manuell
                // gespeicherten Diagnosedaten. So bleibt sichtbar, ob die
                // Lokalisierung oder erst die sichere Zuordnung scheiterte.
                recognizedObjectiveEntries = recognizedObjectiveEntries
                    .Concat(brightObjectiveEntries)
                    .Concat(adaptiveObjectiveEntries)
                    .GroupBy(
                        line => line.Text,
                        StringComparer.Ordinal)
                    .Select(group => group.First())
                    .ToList();
                recognizedObjectiveLines = combinedObjectiveLines;

                if (brightMatch != null)
                {
                    finalMatch = brightMatch;
                }
            }


            return new QuestRecognitionResult
            {
                Match = finalMatch,
                RecognizedLines = recognizedTitleLines,
                RecognizedObjectiveLines = recognizedObjectiveLines,
                YellowLineCandidates = titleCandidates.Count,
                WhiteLineCandidates = objectiveCandidates.Count,
                OcrTitleLines = titleOcrCandidates.Count,
                OcrObjectiveLines = objectiveOcrCandidates.Count,
                TitleCandidateBounds = titleCandidates
                    .Select(ToDrawingRectangle)
                    .ToList(),
                ObjectiveCandidateBounds = objectiveCandidates
                    .Select(ToDrawingRectangle)
                    .ToList(),
                ContentBounds = CalculateContentBounds(
                    titleOcrCandidates,
                    objectiveOcrCandidates,
                    searchImage.Width,
                    searchImage.Height)
            };
        }


        private List<RecognizedTextLine> RecognizeAdaptiveLines(
            Mat source,
            IEnumerable<CvRect> lines)
        {
            List<RecognizedTextLine> recognizedLines = new();

            foreach (CvRect line in lines)
            {
                CvRect textLine = line;

                // Die festen Bänder beginnen am linken Rand und enthalten
                // dort Quest-Icon sowie Alt+1-Hinweis. Für OCR schneiden wir
                // diesen bekannten Präfix ab; erkannte echte Zeilen behalten
                // dagegen ihre präzisen Bounds.
                if (line.X == 0 && line.Width >= source.Width - 2)
                {
                    const int FixedBandTextInset = 34;
                    textLine = new CvRect(
                        FixedBandTextInset,
                        line.Y,
                        line.Width - FixedBandTextInset,
                        line.Height);
                }

                using Mat sourceLine = new Mat(source, textLine);
                using Mat gray = new Mat();
                using Mat binary = new Mat();
                using Mat inverted = new Mat();
                using Mat enlarged = new Mat();

                Cv2.CvtColor(
                    sourceLine,
                    gray,
                    ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(
                    gray,
                    binary,
                    0,
                    255,
                    ThresholdTypes.Binary | ThresholdTypes.Otsu);
                Cv2.BitwiseNot(binary, inverted);
                Cv2.Resize(
                    inverted,
                    enlarged,
                    new OpenCvSharp.Size(),
                    4.0,
                    4.0,
                    InterpolationFlags.Cubic);

                Cv2.ImEncode(".png", enlarged, out byte[] encoded);

                using Pix pix = Pix.LoadFromMemory(encoded);
                using Page page = ocrEngine.Process(
                    pix,
                    PageSegMode.SingleLine);
                string text = page.GetText().Trim();

                if (!string.IsNullOrWhiteSpace(text))
                    recognizedLines.Add(
                        new RecognizedTextLine(textLine, text));
            }

            return recognizedLines;
        }


        private List<RecognizedTextLine> RecognizeLines(
            Mat mask,
            IEnumerable<CvRect> lines)
        {
            List<RecognizedTextLine> recognizedLines = new();

            foreach (CvRect line in lines)
            {
                using Mat lineMask = new Mat(mask, line);
                using Mat ocrImage = new Mat(
                    line.Height,
                    line.Width,
                    MatType.CV_8UC1,
                    Scalar.All(255));

                ocrImage.SetTo(Scalar.All(0), lineMask);

                using Mat enlarged = new Mat();
                Cv2.Resize(
                    ocrImage,
                    enlarged,
                    new OpenCvSharp.Size(),
                    4.0,
                    4.0,
                    InterpolationFlags.Cubic);

                Cv2.ImEncode(".png", enlarged, out byte[] encoded);

                using Pix pix = Pix.LoadFromMemory(encoded);
                using Page page = ocrEngine.Process(
                    pix,
                    PageSegMode.SingleLine);

                string text = page.GetText().Trim();

                if (!string.IsNullOrWhiteSpace(text))
                    recognizedLines.Add(new RecognizedTextLine(line, text));
            }

            return recognizedLines;
        }


        private List<RecognizedTextLine> RecognizeObjectiveLines(
            Mat strongMask,
            Mat dimMask,
            IEnumerable<CvRect> lines,
            bool forceDimMask)
        {
            List<RecognizedTextLine> recognizedLines = new();

            foreach (CvRect line in lines)
            {
                using Mat strongLine = new Mat(strongMask, line);
                using Mat dimLine = new Mat(dimMask, line);
                int dimPixels = Cv2.CountNonZero(dimLine);
                double strongCoverage = dimPixels > 0
                    ? Cv2.CountNonZero(strongLine) / (double)dimPixels
                    : 1.0;
                Mat recognitionMask = forceDimMask ||
                    line.Height <= 15 &&
                    strongCoverage < MinimumStrongWhiteCoverage
                    ? dimMask
                    : strongMask;

                recognizedLines.AddRange(
                    RecognizeLines(recognitionMask, new[] { line }));
            }

            return recognizedLines;
        }


        private static bool IsSafeObjectiveFallback(
            QuestMatch baseline,
            QuestMatch candidate)
        {
            bool resolvesCurrentQuestStep =
                candidate.Quest?.Id == baseline.Quest?.Id &&
                candidate.Quest?.RecognitionKind ==
                    baseline.Quest?.RecognitionKind &&
                candidate.Step != null;

            bool resolvesDirectNextQuestHandoff =
                candidate.IsNextQuestHandoff &&
                candidate.ObservedQuest?.Id == baseline.Quest?.Id &&
                candidate.ObservedQuest?.RecognitionKind ==
                    baseline.Quest?.RecognitionKind &&
                candidate.Quest != null;

            return resolvesCurrentQuestStep ||
                resolvesDirectNextQuestHandoff;
        }


        private static bool ShouldUseAdaptiveObjectiveFallback(
            QuestMatch baseline)
        {
            if (baseline.Quest == null)
                return false;

            string questIdentity = baseline.Quest.RecognitionKind + ":" +
                baseline.Quest.Id;
            return AdaptiveObjectiveFallbackQuestIds.Contains(questIdentity);
        }


        private sealed record RecognizedTextLine(CvRect Bounds, string Text);


        private static Rectangle ToDrawingRectangle(CvRect rectangle)
        {
            return new Rectangle(
                rectangle.X,
                rectangle.Y,
                rectangle.Width,
                rectangle.Height);
        }


        private static Rectangle CalculateContentBounds(
            IEnumerable<CvRect> titleLines,
            IEnumerable<CvRect> objectiveLines,
            int imageWidth,
            int imageHeight)
        {
            List<CvRect> lines = titleLines
                .Concat(objectiveLines)
                .ToList();

            if (lines.Count == 0)
                return Rectangle.Empty;

            CvRect combined = lines.Aggregate(Union);
            const int horizontalPadding = 8;
            const int verticalPadding = 10;
            int left = Math.Max(0, combined.X - horizontalPadding);
            int top = Math.Max(0, combined.Y - verticalPadding);
            int right = Math.Min(
                imageWidth,
                combined.Right + horizontalPadding);
            int bottom = Math.Min(
                imageHeight,
                combined.Bottom + verticalPadding);

            return new Rectangle(
                left,
                top,
                Math.Max(1, right - left),
                Math.Max(1, bottom - top));
        }


        private static Rectangle CalculateSearchRegion(int width, int height)
        {
            int x = (int)Math.Round(width * SearchLeftRatio);
            int y = (int)Math.Round(height * SearchTopRatio);
            int regionWidth = (int)Math.Round(width * SearchWidthRatio);
            int regionHeight = (int)Math.Round(height * SearchHeightRatio);

            x = Math.Clamp(x, 0, width - 1);
            y = Math.Clamp(y, 0, height - 1);
            regionWidth = Math.Clamp(regionWidth, 1, width - x);
            regionHeight = Math.Clamp(regionHeight, 1, height - y);

            return new Rectangle(x, y, regionWidth, regionHeight);
        }


        private static List<CvRect> FindTextLines(Mat mask)
        {
            Cv2.FindContours(
                mask,
                out OpenCvSharp.Point[][] contours,
                out _,
                RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);

            List<CvRect> glyphs = contours
                .Select(Cv2.BoundingRect)
                .Where(rect =>
                    rect.Width is >= 2 and <= 32 &&
                    rect.Height is >= 3 and <= 28)
                .OrderBy(rect => rect.Y + rect.Height / 2.0)
                .ToList();

            List<CvRect> lines = new();

            foreach (CvRect glyph in glyphs)
            {
                int glyphCenter = glyph.Y + glyph.Height / 2;
                int matchingLine = lines.FindIndex(line =>
                    Math.Abs((line.Y + line.Height / 2) - glyphCenter) <= 9);

                if (matchingLine < 0)
                {
                    lines.Add(glyph);
                    continue;
                }

                lines[matchingLine] = Union(lines[matchingLine], glyph);
            }

            const int padding = 4;

            return lines
                .Where(line => line.Width >= 65)
                .Where(line => line.Height >= 7 && line.Height <= 34)
                .Select(line => new CvRect(
                    Math.Max(0, line.X - padding),
                    Math.Max(0, line.Y - padding),
                    Math.Min(mask.Width, line.Right + padding) - Math.Max(0, line.X - padding),
                    Math.Min(mask.Height, line.Bottom + padding) - Math.Max(0, line.Y - padding)))
                .OrderBy(line => line.Y)
                .ToList();
        }


        private static CvRect Union(CvRect left, CvRect right)
        {
            int x = Math.Min(left.X, right.X);
            int y = Math.Min(left.Y, right.Y);
            int rightEdge = Math.Max(left.Right, right.Right);
            int bottom = Math.Max(left.Bottom, right.Bottom);

            return new CvRect(x, y, rightEdge - x, bottom - y);
        }

        public void Dispose()
        {
            ocrEngine.Dispose();
        }
    }
}
