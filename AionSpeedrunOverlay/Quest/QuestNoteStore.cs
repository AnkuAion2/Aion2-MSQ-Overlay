using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AionSpeedrunOverlay.Quest
{
    public sealed class QuestNoteStore
    {
        private const int CurrentFormatVersion = 1;

        private readonly string storagePath;
        private readonly object sync = new();
        private Dictionary<string, List<string>> overrides;
        private Dictionary<string, List<string>> images = new(StringComparer.Ordinal);


        public QuestNoteStore(string? storagePath = null)
        {
            this.storagePath = storagePath ?? Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "AionSpeedrunOverlay",
                "notes-overrides.json");

            overrides = Load();
        }


        public bool TryGetOverride(
            string noteKey,
            out IReadOnlyList<string> notes)
        {
            lock (sync)
            {
                if (overrides.TryGetValue(noteKey, out List<string>? stored))
                {
                    notes = stored.ToList();
                    return true;
                }

                notes = Array.Empty<string>();
                return false;
            }
        }


        public IReadOnlyList<string> GetImages(string key)
        {
            lock (sync) return images.TryGetValue(key, out var values) ? values.ToArray() : Array.Empty<string>();
        }

        public void SaveWithImages(string key, IEnumerable<string> notes, IEnumerable<string> imageData)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Missing note key.");
            var selected = imageData.ToList();
            if (selected.Count > 3 || selected.Any(data => data.Length > 2800000))
                throw new IOException("Maximum three images, 2 MB each after resizing.");
            lock (sync)
            {
                var updated = CloneOverrides();
                updated[key] = NormalizeSteps(notes);
                var updatedImages = images.ToDictionary(entry => entry.Key, entry => entry.Value.ToList(), StringComparer.Ordinal);
                if (selected.Count == 0) updatedImages.Remove(key);
                else updatedImages[key] = selected;
                Persist(updated, updatedImages);
                overrides = updated;
                images = updatedImages;
            }
        }

        public void SaveOverride(
            string noteKey,
            IEnumerable<string> notes)
        {
            if (string.IsNullOrWhiteSpace(noteKey))
                throw new ArgumentException("The note key is missing.", nameof(noteKey));

            List<string> normalized = NormalizeSteps(notes);

            lock (sync)
            {
                Dictionary<string, List<string>> updated = CloneOverrides();
                updated[noteKey] = normalized;
                Persist(updated);
                overrides = updated;
            }
        }


        // The catalog editor resets text independently of existing image attachments.
        public void RemoveTextOverride(string noteKey)
        {
            if (string.IsNullOrWhiteSpace(noteKey)) return;
            lock (sync)
            {
                if (!overrides.ContainsKey(noteKey)) return;
                var updated = CloneOverrides();
                updated.Remove(noteKey);
                Persist(updated);
                overrides = updated;
            }
        }


        public void RemoveOverride(string noteKey)
        {
            if (string.IsNullOrWhiteSpace(noteKey))
                return;

            lock (sync)
            {
                if (!overrides.ContainsKey(noteKey) && !images.ContainsKey(noteKey))
                    return;

                Dictionary<string, List<string>> updated = CloneOverrides();
                updated.Remove(noteKey);
                var updatedImages = images.ToDictionary(entry => entry.Key, entry => entry.Value.ToList(), StringComparer.Ordinal);
                updatedImages.Remove(noteKey);
                Persist(updated, updatedImages);
                images = updatedImages;
                overrides = updated;
            }
        }


        public static List<string> ParseEditorText(string text)
        {
            return NormalizeSteps(new[] { text });
        }


        public static List<string> NormalizeSteps(IEnumerable<string> values)
        {
            List<string> rawSteps = new();

            foreach (string value in values)
            {
                foreach (string line in value
                    .Replace("\r", "", StringComparison.Ordinal)
                    .Split(new[] { '\n', '>' }, StringSplitOptions.None))
                {
                    string normalized = line.Trim();

                    if (normalized.Length > 0)
                        rawSteps.Add(normalized);
                }
            }

            bool isSequentialNumberedList = rawSteps.Count >= 2 &&
                rawSteps.Select(TryReadNumberedStep)
                    .Select((match, index) =>
                        match.Success &&
                        int.TryParse(match.Groups[1].Value, out int number) &&
                        number == index + 1)
                    .All(matches => matches);

            return rawSteps
                .Select(step => isSequentialNumberedList
                    ? TryReadNumberedStep(step).Groups[2].Value.Trim()
                    : Regex.Replace(
                            step,
                            @"^\s*\d{1,3}\s*[.)-]\s+",
                            "",
                            RegexOptions.CultureInvariant)
                        .Trim())
                .Where(step => step.Length > 0)
                .ToList();
        }


        private static Match TryReadNumberedStep(string step)
        {
            return Regex.Match(
                step,
                @"^\s*(\d{1,3})\s*[.)-]?\s+(.+)$",
                RegexOptions.CultureInvariant);
        }


        public static string FormatForDisplay(IEnumerable<string> notes)
        {
            List<string> normalized = NormalizeSteps(notes);

            return string.Join(
                Environment.NewLine,
                normalized.Select((note, index) => $"{index + 1}. {note}"));
        }


        private Dictionary<string, List<string>> Load()
        {
            try
            {
                if (!File.Exists(storagePath))
                    return new Dictionary<string, List<string>>(
                        StringComparer.Ordinal);

                QuestNoteDocument? document =
                    JsonSerializer.Deserialize<QuestNoteDocument>(
                        File.ReadAllText(storagePath));

                if (document?.FormatVersion != CurrentFormatVersion)
                {
                    return new Dictionary<string, List<string>>(
                        StringComparer.Ordinal);
                }

                images = document.Images ?? new(StringComparer.Ordinal);
                return document.Notes.ToDictionary(
                    entry => entry.Key,
                    entry => NormalizeSteps(entry.Value),
                    StringComparer.Ordinal);
            }
            catch (IOException)
            {
                return new Dictionary<string, List<string>>(
                    StringComparer.Ordinal);
            }
            catch (UnauthorizedAccessException)
            {
                return new Dictionary<string, List<string>>(
                    StringComparer.Ordinal);
            }
            catch (JsonException)
            {
                return new Dictionary<string, List<string>>(
                    StringComparer.Ordinal);
            }
        }


        private Dictionary<string, List<string>> CloneOverrides()
        {
            return overrides.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.ToList(),
                StringComparer.Ordinal);
        }


        private void Persist(Dictionary<string, List<string>> updated, Dictionary<string, List<string>>? updatedImages = null)
        {
            string? directory = Path.GetDirectoryName(storagePath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string temporaryPath = storagePath + ".tmp";
            string json = JsonSerializer.Serialize(
                new QuestNoteDocument
                {
                    FormatVersion = CurrentFormatVersion,
                    Notes = updated,
                    Images = updatedImages ?? images
                },
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, storagePath, true);
        }


        private sealed class QuestNoteDocument
        {
            public Dictionary<string, List<string>> Images { get; init; } = new(StringComparer.Ordinal);
            public int FormatVersion { get; init; }

            public Dictionary<string, List<string>> Notes { get; init; } =
                new(StringComparer.Ordinal);
        }
    }
}
