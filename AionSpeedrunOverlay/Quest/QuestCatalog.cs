using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using AionSpeedrunOverlay.Models;

namespace AionSpeedrunOverlay.Quest
{
    public sealed class QuestMatch
    {
        public QuestDefinition? Quest { get; init; }
        public QuestDefinition? ObservedQuest { get; init; }
        public QuestStepDefinition? Step { get; init; }
        public string RawText { get; init; } = "";
        public double Score { get; init; }
        public string RawObjectiveText { get; init; } = "";
        public double StepScore { get; init; }
        public int? DetectedLevel { get; init; }
        public bool LevelFilterApplied { get; init; }
        public double RunnerUpScore { get; init; }
        public bool IsAmbiguous { get; init; }
        public bool IsQuestObjectiveMatch { get; init; }
        public bool IsNextQuestHandoff { get; init; }
        public bool IsAdjacentLevelRecovery { get; init; }
        public string RejectionReason { get; init; } = "";
    }


    public static class QuestRecognitionIdentity
    {
        public static QuestDefinition? GetVisibleQuest(QuestMatch match)
        {
            return match.ObservedQuest ?? match.Quest;
        }


        public static string CreateDisplayId(QuestMatch match)
        {
            QuestDefinition? visibleQuest = GetVisibleQuest(match);

            if (visibleQuest == null)
                return "";

            string prefix = visibleQuest.RecognitionKind + ":" +
                visibleQuest.Id + ":";

            if (match.IsNextQuestHandoff && match.Quest != null)
            {
                return prefix + "handoff:" +
                    match.Quest.RecognitionKind + ":" + match.Quest.Id;
            }

            return prefix + (match.Step?.Id ?? "quest");
        }


        public static string CreateNoteKey(QuestMatch match)
        {
            // A handoff is inferred from an objective while the old quest
            // title is still visible. It must not select the next quest's
            // note before that next title is actually recognized.
            if (match.Quest == null || match.IsNextQuestHandoff)
                return "";

            return match.Quest.RecognitionKind + ":" + match.Quest.Id + ":" +
                (match.Step?.Id ?? "quest");
        }
    }


    public sealed partial class QuestCatalog
    {
        private const double QuestMatchThreshold = 0.72;
        private const double StepMatchThreshold = 0.75;
        private const double MinimumMatchMargin = 0.08;
        private const double MinimumObjectiveContextMargin = 0.05;
        private const double LevelLessQuestMatchThreshold = 0.90;
        private const double LevelLessMinimumMatchMargin = 0.10;
        private const double AdjacentLevelRecoveryThreshold = 0.95;
        private const double AdjacentLevelRecoveryMargin = 0.12;
        private const double NextQuestHandoffThreshold = 0.82;

        private static readonly HashSet<string> ObjectiveActionWords = new(
            new[]
            {
                "accept", "ask", "awaken", "bid", "bind", "carry", "check", "collect",
                "complete", "defeat", "deliver", "discuss", "enter", "escape",
                "examine", "find",
                "fly", "gather", "go", "hear", "hide", "infiltrate",
                "investigate", "join", "learn", "meet", "obtain", "prepare",
                "remove", "report", "rescue", "return", "search", "share", "sleep",
                "talk", "travel", "use", "visit"
            },
            StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ObjectiveIdentityStopWords = new(
            new[]
            {
                "a", "an", "and", "as", "at", "be", "for", "from", "in",
                "is", "may", "of", "on", "the", "to", "via", "what",
                "where", "who", "with"
            },
            StringComparer.OrdinalIgnoreCase);

        private readonly List<QuestDefinition> quests;
        private readonly List<QuestDefinition> levelLessContexts;

        public QuestCatalog(string path, string? levelLessContextPath = null)
        {
            quests = LoadDefinitions(path, "The quest catalog was not found.");
            levelLessContexts = !string.IsNullOrWhiteSpace(levelLessContextPath) &&
                File.Exists(levelLessContextPath)
                ? LoadDefinitions(
                    levelLessContextPath,
                    "The level-less context file was not found.")
                : new List<QuestDefinition>();

            if (quests.Count == 0)
                throw new InvalidOperationException("The quest catalog does not contain any quests.");
        }


        private static List<QuestDefinition> LoadDefinitions(
            string path,
            string missingMessage)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(missingMessage, path);

            string json = File.ReadAllText(path);
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };
            using JsonDocument document = JsonDocument.Parse(json);

            return document.RootElement.ValueKind == JsonValueKind.Array
                ? JsonSerializer.Deserialize<List<QuestDefinition>>(json, options)
                    ?? new List<QuestDefinition>()
                : JsonSerializer.Deserialize<QuestCatalogDocument>(json, options)?.Quests
                    ?? new List<QuestDefinition>();
        }


        public QuestMatch FindBestMatch(
            IEnumerable<string> recognizedTitleLines,
            IEnumerable<string> recognizedObjectiveLines)
        {
            List<string> titleLines = recognizedTitleLines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            List<string> objectiveLines = ExpandWrappedLines(
                recognizedObjectiveLines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList());

            List<int> detectedLevels = DetectRecommendedLevels(titleLines);
            bool levelFilterApplied = detectedLevels.Count > 0;
            List<QuestDefinition> candidates = levelFilterApplied
                ? quests
                    .Where(quest =>
                        quest.RecommendedLevel.HasValue &&
                        detectedLevels.Contains(
                            quest.RecommendedLevel.Value))
                    .ToList()
                : quests
                    .Where(quest => quest.AllowWithoutLevel)
                    .Concat(levelLessContexts)
                    .ToList();

            if (candidates.Count == 0)
            {
                return new QuestMatch
                {
                    DetectedLevel = detectedLevels.FirstOrDefault() is int level &&
                        level > 0
                            ? level
                            : null,
                    RejectionReason = levelFilterApplied
                        ? "level-without-candidates"
                        : "level-missing"
                };
            }

            double requiredQuestThreshold = levelFilterApplied
                ? QuestMatchThreshold
                : LevelLessQuestMatchThreshold;
            double requiredMatchMargin = levelFilterApplied
                ? MinimumMatchMargin
                : LevelLessMinimumMatchMargin;

            (QuestDefinition? bestQuest,
                string bestRawText,
                double bestScore,
                double runnerUpScore) =
                FindBestQuest(titleLines, candidates);
            bool adjacentLevelRecovered = false;

            // Eine einzelne Levelziffer zerfaellt in der kleinen Aion-Schrift
            // gelegentlich von 35 zu 36. Das Level-Gate bleibt verbindlich;
            // nur ein katalogweit eindeutiger Titel mit mindestens 95 % darf
            // genau eine benachbarte Stufe korrigieren. Fehlendes Level oder
            // groessere Abweichungen erhalten keinerlei Fallback.
            if (levelFilterApplied &&
                detectedLevels.Count == 1 &&
                (bestQuest == null || bestScore < requiredQuestThreshold))
            {
                (QuestDefinition? recoveredQuest,
                    string recoveredRawText,
                    double recoveredScore,
                    double recoveredRunnerUpScore) =
                    FindBestQuest(
                        titleLines.Where(line =>
                            DetectRecommendedLevels(new[] { line }).Count > 0),
                        quests.Where(quest =>
                            quest.RecommendedLevel.HasValue));

                if (recoveredQuest?.RecommendedLevel is int recoveredLevel &&
                    Math.Abs(recoveredLevel - detectedLevels[0]) == 1 &&
                    recoveredScore >= AdjacentLevelRecoveryThreshold &&
                    recoveredScore - recoveredRunnerUpScore >=
                        AdjacentLevelRecoveryMargin)
                {
                    bestQuest = recoveredQuest;
                    bestRawText = recoveredRawText;
                    bestScore = recoveredScore;
                    runnerUpScore = recoveredRunnerUpScore;
                    candidates = new List<QuestDefinition> { recoveredQuest };
                    adjacentLevelRecovered = true;
                }
            }

            int? detectedLevel = levelFilterApplied
                ? adjacentLevelRecovered
                    ? detectedLevels[0]
                    : bestQuest?.RecommendedLevel ?? detectedLevels[0]
                : null;
            bool contextResolvedAmbiguity = false;

            // Wenn mehrere Quests desselben Levels denselben sichtbaren
            // Titelanfang haben, entscheidet ein starker Treffer auf die
            // darunterstehende Aufgabe. Beispiel: "Daybreak S..." kann sowohl
            // "Daybreak Society" als auch "Daybreak Society Rescue Operation"
            // meinen; "Distract the Black Claw soldiers" ist dagegen eindeutig.
            if (objectiveLines.Count > 0)
            {
                (QuestDefinition? contextualQuest,
                    string contextualRawText,
                    double contextualTitleScore,
                    double contextualCombinedScore,
                    double contextualRunnerUpScore) =
                    FindBestQuestUsingObjectiveContext(
                        titleLines,
                        objectiveLines,
                        candidates,
                        requiredQuestThreshold);

                if (contextualQuest != null &&
                    contextualCombinedScore - contextualRunnerUpScore >=
                        Math.Min(
                            requiredMatchMargin,
                            MinimumObjectiveContextMargin))
                {
                    bestQuest = contextualQuest;
                    bestRawText = contextualRawText;
                    bestScore = contextualTitleScore;
                    contextResolvedAmbiguity = true;
                }
            }

            if (bestScore < requiredQuestThreshold || bestQuest == null)
            {
                return new QuestMatch
                {
                    RawText = bestRawText,
                    Score = bestScore,
                    RunnerUpScore = runnerUpScore,
                    DetectedLevel = detectedLevel,
                    LevelFilterApplied = levelFilterApplied,
                    IsAdjacentLevelRecovery = adjacentLevelRecovered,
                    RejectionReason = "quest-low-confidence"
                };
            }

            bool isAmbiguous =
                !contextResolvedAmbiguity &&
                bestScore - runnerUpScore < requiredMatchMargin;

            if (isAmbiguous)
            {
                return new QuestMatch
                {
                    Quest = bestQuest,
                    RawText = bestRawText,
                    Score = bestScore,
                    RunnerUpScore = runnerUpScore,
                    DetectedLevel = detectedLevel,
                    LevelFilterApplied = levelFilterApplied,
                    IsAmbiguous = true,
                    IsAdjacentLevelRecovery = adjacentLevelRecovered,
                    RejectionReason = "quest-ambiguous"
                };
            }

            Dictionary<QuestStepDefinition, (
                double Score,
                string RawText,
                bool ExactTitleMatch,
                bool ExactAliasMatch,
                double TitleScore,
                string TitleRawText,
                bool ActionAlignedTitleMatch,
                double AliasScore,
                string AliasRawText)>
                stepScores = new();
            string bestRawObjectiveText = "";
            double bestStepScore = 0;

            List<string> objectiveLineList = objectiveLines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
            List<string> primaryObjectiveTitleEvidence =
                BuildPrimaryObjectiveTitleEvidence(objectiveLineList);

            void AddStepEvidence(
                QuestStepDefinition step,
                string rawText,
                string alias,
                bool isTitle,
                bool exactTitleAliasOnly)
            {
                if (!isTitle &&
                    !HaveCompatibleObjectiveIdentity(rawText, alias))
                {
                    return;
                }

                double score = CalculateStepMatchScore(rawText, alias);
                if (isTitle)
                {
                    score = AdjustTitleScoreForSpecificity(
                        rawText,
                        alias,
                        score);
                }
                if (exactTitleAliasOnly)
                {
                    score = HasExactNormalizedObjectiveText(rawText, alias)
                        ? 1.0
                        : 0;
                }
                bool exactTitleMatch = isTitle &&
                    IsExactObjectiveTextMatch(rawText, alias, score);
                bool exactAliasMatch = !isTitle &&
                    IsExactObjectiveTextMatch(rawText, alias, score);

                stepScores.TryGetValue(step, out var current);
                double bestEvidenceScore = Math.Max(current.Score, score);
                string bestEvidenceRawText = score > current.Score
                    ? rawText
                    : current.RawText;
                double aliasScore = !isTitle
                    ? Math.Max(current.AliasScore, score)
                    : current.AliasScore;
                string aliasRawText = !isTitle && score > current.AliasScore
                    ? rawText
                    : current.AliasRawText;
                double titleScore = isTitle
                    ? Math.Max(current.TitleScore, score)
                    : current.TitleScore;
                string titleRawText = isTitle && score > current.TitleScore
                    ? rawText
                    : current.TitleRawText;
                bool actionAlignedTitleMatch = isTitle &&
                    score >= StepMatchThreshold &&
                    HaveMatchingObjectiveActions(rawText, alias);

                stepScores[step] = (
                    bestEvidenceScore,
                    bestEvidenceRawText,
                    current.ExactTitleMatch || exactTitleMatch,
                    current.ExactAliasMatch || exactAliasMatch,
                    titleScore,
                    titleRawText,
                    current.ActionAlignedTitleMatch || actionAlignedTitleMatch,
                    aliasScore,
                    aliasRawText);
            }

            foreach (QuestStepDefinition step in bestQuest.Steps)
            {
                foreach (string rawText in primaryObjectiveTitleEvidence)
                {
                    AddStepEvidence(
                        step,
                        rawText,
                        step.Title,
                        true,
                        false);

                    foreach (string titleAlias in step.TitleAliases.Where(
                        title => !string.IsNullOrWhiteSpace(title)))
                    {
                        // Step title aliases encode observed OCR forms. Unlike
                        // canonical titles they are exact-only, otherwise a
                        // short alias such as "... Qual" can impersonate the
                        // equally prefixed Nornir objective.
                        AddStepEvidence(
                            step,
                            rawText,
                            titleAlias,
                            true,
                            true);
                    }
                }

                foreach (string rawText in objectiveLineList)
                {
                    foreach (string alias in step.ObjectiveAliases.Where(alias =>
                        !string.IsNullOrWhiteSpace(alias)))
                    {
                        AddStepEvidence(
                            step,
                            rawText,
                            alias,
                            false,
                            false);
                    }
                }
            }

            double bestTitleScore = stepScores.Count > 0
                ? stepScores.Max(entry => entry.Value.TitleScore)
                : 0;
            double runnerUpTitleScore = stepScores
                .Select(entry => entry.Value.TitleScore)
                .OrderByDescending(score => score)
                .Skip(1)
                .FirstOrDefault();
            bool hasDistinctTitleEvidence =
                bestTitleScore >= StepMatchThreshold &&
                bestTitleScore - runnerUpTitleScore >= MinimumMatchMargin;

            var orderedSteps = stepScores
                .OrderByDescending(entry => entry.Value.ExactTitleMatch)
                .ThenByDescending(entry =>
                    entry.Value.ExactAliasMatch &&
                    entry.Value.ActionAlignedTitleMatch)
                .ThenByDescending(entry =>
                    entry.Value.ActionAlignedTitleMatch)
                .ThenByDescending(entry =>
                    hasDistinctTitleEvidence &&
                    entry.Value.TitleScore == bestTitleScore)
                .ThenByDescending(entry => entry.Value.TitleScore)
                .ThenByDescending(entry => entry.Value.AliasScore)
                .ThenByDescending(entry => entry.Value.Score)
                .ToList();
            QuestStepDefinition? bestStep = orderedSteps.Count > 0
                ? orderedSteps[0].Key
                : null;
            bestStepScore = orderedSteps.Count > 0
                ? orderedSteps[0].Value.Score
                : 0;
            bool bestStepHasDistinctTitleEvidence = orderedSteps.Count > 0 &&
                hasDistinctTitleEvidence &&
                orderedSteps[0].Value.TitleScore == bestTitleScore;
            bool bestStepHasActionAlignedTitleEvidence =
                orderedSteps.Count > 0 &&
                orderedSteps[0].Value.ActionAlignedTitleMatch;
            bestRawObjectiveText = orderedSteps.Count > 0
                ? (orderedSteps[0].Value.ExactTitleMatch ||
                    bestStepHasActionAlignedTitleEvidence ||
                    bestStepHasDistinctTitleEvidence) &&
                    !string.IsNullOrWhiteSpace(
                        orderedSteps[0].Value.TitleRawText)
                        ? orderedSteps[0].Value.TitleRawText
                        : orderedSteps[0].Value.AliasScore >=
                            StepMatchThreshold &&
                    !string.IsNullOrWhiteSpace(
                        orderedSteps[0].Value.AliasRawText)
                        ? orderedSteps[0].Value.AliasRawText
                        : orderedSteps[0].Value.RawText
                : "";
            bool duplicateExactTitles = orderedSteps.Count > 1 &&
                orderedSteps[0].Value.TitleScore >= StepMatchThreshold &&
                orderedSteps[1].Value.TitleScore >= StepMatchThreshold &&
                string.Equals(
                    Normalize(orderedSteps[0].Key.Title),
                    Normalize(orderedSteps[1].Key.Title),
                    StringComparison.Ordinal);
            int bestEvidenceTier = orderedSteps.Count > 0
                ? orderedSteps[0].Value.ExactTitleMatch
                    ? 3
                    : bestStepHasActionAlignedTitleEvidence
                        ? 2
                        : bestStepHasDistinctTitleEvidence
                            ? 1
                            : 0
                : 0;
            int runnerUpEvidenceTier = orderedSteps.Count > 1
                ? orderedSteps[1].Value.ExactTitleMatch
                    ? 3
                    : orderedSteps[1].Value.ActionAlignedTitleMatch
                        ? 2
                        : hasDistinctTitleEvidence &&
                            orderedSteps[1].Value.TitleScore == bestTitleScore
                                ? 1
                                : 0
                : 0;
            double comparisonScore = duplicateExactTitles
                ? orderedSteps[0].Value.AliasScore
                : bestStepScore;
            double runnerUpStepScore = orderedSteps.Count > 1 &&
                runnerUpEvidenceTier == bestEvidenceTier
                ? duplicateExactTitles
                    ? orderedSteps[1].Value.AliasScore
                    : orderedSteps[1].Value.Score
                : 0;
            bool stepAmbiguous = duplicateExactTitles
                ? comparisonScore < StepMatchThreshold ||
                    comparisonScore - runnerUpStepScore < MinimumMatchMargin
                : bestStepScore >= StepMatchThreshold &&
                    bestStepScore - runnerUpStepScore < MinimumMatchMargin &&
                    !(bestStepScore >= 0.99 && runnerUpStepScore < 0.99) &&
                    !(orderedSteps[0].Value.ExactAliasMatch &&
                        !orderedSteps.Skip(1).Any(entry =>
                            entry.Value.ExactAliasMatch)) &&
                    !(orderedSteps[0].Value.AliasScore >= 0.99 &&
                        orderedSteps.Skip(1).All(entry =>
                            entry.Value.AliasScore < 0.99));

            List<string> questOnlyObjectiveAliases = bestQuest.ObjectiveAliases
                .Where(alias => !bestQuest.Steps.Any(step =>
                    step.ObjectiveAliases
                        .Concat(step.TitleAliases.Prepend(step.Title))
                        .Any(stepName => string.Equals(
                            Normalize(alias),
                            Normalize(stepName),
                            StringComparison.Ordinal))))
                .ToList();
            double questObjectiveScore = FindBestQuestObjectiveAliasScore(
                questOnlyObjectiveAliases,
                objectiveLines);
            bool bestStepHasExactTitle = orderedSteps.Count > 0 &&
                orderedSteps[0].Value.ExactTitleMatch;
            bool distinctTitleBeatsQuestObjective =
                (bestStepHasActionAlignedTitleEvidence ||
                    bestStepHasDistinctTitleEvidence) &&
                bestTitleScore >= questObjectiveScore;
            bool questObjectiveBlocksStep =
                questObjectiveScore >= StepMatchThreshold &&
                !bestStepHasExactTitle &&
                !distinctTitleBeatsQuestObjective &&
                (bestStep == null ||
                    bestStepScore < questObjectiveScore + MinimumMatchMargin);

            if (questObjectiveBlocksStep)
            {
                bestStep = null;
                stepAmbiguous = false;
                bestStepScore = questObjectiveScore;
                bestRawObjectiveText = FindBestAliasRawText(
                    questOnlyObjectiveAliases,
                    objectiveLines);
            }

            (QuestDefinition? handoffQuest,
                string handoffRawText,
                double handoffScore) = FindNextQuestHandoff(
                    bestQuest,
                    objectiveLines);
            bool currentStepIsSafe =
                bestStep != null &&
                bestStepScore >= StepMatchThreshold &&
                !stepAmbiguous;
            bool handoffBeatsCurrentStep =
                !currentStepIsSafe ||
                !bestStepHasExactTitle &&
                handoffScore >= bestStepScore + MinimumMatchMargin;

            if (handoffQuest != null &&
                handoffScore >= NextQuestHandoffThreshold &&
                handoffBeatsCurrentStep)
            {
                return new QuestMatch
                {
                    Quest = handoffQuest,
                    ObservedQuest = bestQuest,
                    RawText = bestRawText,
                    Score = bestScore,
                    RunnerUpScore = runnerUpScore,
                    RawObjectiveText = handoffRawText,
                    StepScore = handoffScore,
                    DetectedLevel = detectedLevel,
                    LevelFilterApplied = levelFilterApplied,
                    IsQuestObjectiveMatch = true,
                    IsNextQuestHandoff = true,
                    IsAdjacentLevelRecovery = adjacentLevelRecovered
                };
            }

            return new QuestMatch
            {
                Quest = bestQuest,
                Step = bestStepScore >= StepMatchThreshold && !stepAmbiguous
                    ? bestStep
                    : null,
                RawText = bestRawText,
                Score = bestScore,
                RunnerUpScore = runnerUpScore,
                RawObjectiveText = bestRawObjectiveText,
                StepScore = bestStepScore,
                DetectedLevel = detectedLevel,
                LevelFilterApplied = levelFilterApplied,
                IsAmbiguous = stepAmbiguous,
                IsQuestObjectiveMatch = questObjectiveBlocksStep,
                IsAdjacentLevelRecovery = adjacentLevelRecovered,
                RejectionReason = stepAmbiguous
                        ? "step-ambiguous"
                        : ""
            };
        }


        private static double FindBestAliasScore(
            IEnumerable<string> aliases,
            IEnumerable<string> rawLines)
        {
            double bestScore = 0;

            foreach (string rawLine in rawLines)
            {
                foreach (string alias in aliases.Where(value =>
                    !string.IsNullOrWhiteSpace(value)))
                {
                    bestScore = Math.Max(
                        bestScore,
                        CalculateStepMatchScore(rawLine, alias));
                }
            }

            return bestScore;
        }


        private static double FindBestQuestObjectiveAliasScore(
            IEnumerable<string> aliases,
            IEnumerable<string> rawLines)
        {
            double bestScore = 0;

            foreach (string alias in aliases)
            {
                string normalizedAlias = Normalize(alias);

                if (normalizedAlias.Length == 0)
                    continue;

                foreach (string rawLine in rawLines)
                {
                    string actionAlignedRawLine = TrimToObjectiveAction(rawLine);
                    if (!HaveCompatibleObjectiveIdentity(
                        actionAlignedRawLine,
                        alias))
                    {
                        continue;
                    }

                    string normalizedRawLine = Normalize(actionAlignedRawLine);
                    double lengthCoverage = Math.Min(
                            normalizedRawLine.Length,
                            normalizedAlias.Length) /
                        (double)Math.Max(
                            normalizedRawLine.Length,
                            normalizedAlias.Length);

                    // A short generic fragment such as "Talk to Nemon" must
                    // not impersonate a quest-level handoff such as
                    // "Talk to Nemon at Shadow Hall". Truncated but still
                    // identifying handoffs remain eligible.
                    if (lengthCoverage < 0.60)
                        continue;

                    bestScore = Math.Max(
                        bestScore,
                        CalculateStepMatchScore(actionAlignedRawLine, alias));
                }
            }

            return bestScore;
        }


        private (QuestDefinition? Quest, string RawText, double Score)
            FindNextQuestHandoff(
                QuestDefinition currentQuest,
                IReadOnlyList<string> objectiveLines)
        {
            if (!currentQuest.RouteIndex.HasValue ||
                objectiveLines.Count == 0)
            {
                return (null, "", 0);
            }

            int? nextRouteIndex = quests
                .Where(quest =>
                    quest.RouteIndex > currentQuest.RouteIndex &&
                    string.Equals(
                        quest.RecognitionKind,
                        currentQuest.RecognitionKind,
                        StringComparison.Ordinal))
                .Select(quest => quest.RouteIndex)
                .Where(routeIndex => routeIndex.HasValue)
                .Select(routeIndex => routeIndex!.Value)
                .DefaultIfEmpty()
                .Min();

            if (!nextRouteIndex.HasValue || nextRouteIndex.Value == 0)
                return (null, "", 0);

            List<(QuestDefinition Quest, string RawText, double Score)> scored =
                quests
                    .Where(quest =>
                        quest.RouteIndex == nextRouteIndex &&
                        string.Equals(
                            quest.RecognitionKind,
                            currentQuest.RecognitionKind,
                            StringComparison.Ordinal))
                    .Select(quest =>
                    {
                        double score = FindBestQuestObjectiveAliasScore(
                            quest.ObjectiveAliases,
                            objectiveLines);
                        string rawText = FindBestAliasRawText(
                            quest.ObjectiveAliases,
                            objectiveLines);
                        return (Quest: quest, RawText: rawText, Score: score);
                    })
                    .OrderByDescending(candidate => candidate.Score)
                    .ToList();

            if (scored.Count == 0 ||
                scored[0].Score < NextQuestHandoffThreshold)
            {
                return (null, "", 0);
            }

            double runnerUpScore = scored.Count > 1
                ? scored[1].Score
                : 0;

            if (scored[0].Score - runnerUpScore < MinimumMatchMargin)
                return (null, "", 0);

            return scored[0];
        }


        private static string TrimToObjectiveAction(string rawText)
        {
            Match? actionMatch = Regex.Matches(
                    rawText,
                    @"[A-Za-z]+",
                    RegexOptions.CultureInvariant)
                .Cast<Match>()
                .FirstOrDefault(match =>
                    ObjectiveActionWords.Contains(match.Value));

            return actionMatch != null
                ? rawText[actionMatch.Index..]
                : rawText;
        }


        private static List<string> BuildPrimaryObjectiveTitleEvidence(
            IReadOnlyList<string> objectiveLines)
        {
            List<string> evidence = new();

            if (objectiveLines.Count == 0)
                return evidence;

            string combined = TrimToObjectiveAction(objectiveLines[0]);
            evidence.Add(combined);

            for (int index = 1; index < objectiveLines.Count; index++)
            {
                string continuation = objectiveLines[index].Trim();

                // A new action starts a subordinate objective/alias. It must
                // never outrank the actual objective title above it.
                if (FindObjectiveAction(continuation) != null)
                    break;

                combined = $"{combined} {continuation}".Trim();
                evidence.Add(combined);
            }

            return evidence;
        }


        private static string FindBestAliasRawText(
            IEnumerable<string> aliases,
            IEnumerable<string> rawLines)
        {
            string bestRawText = "";
            double bestScore = 0;

            foreach (string rawLine in rawLines)
            {
                foreach (string alias in aliases.Where(value =>
                    !string.IsNullOrWhiteSpace(value)))
                {
                    double score = CalculateStepMatchScore(rawLine, alias);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestRawText = rawLine;
                    }
                }
            }

            return bestRawText;
        }


        private static List<string> ExpandWrappedLines(
            IReadOnlyList<string> lines)
        {
            List<string> expanded = lines.ToList();

            for (int start = 0; start < lines.Count; start++)
            {
                string combined = lines[start];

                for (int offset = 1;
                     offset <= 2 && start + offset < lines.Count;
                     offset++)
                {
                    combined += " " + lines[start + offset];
                    expanded.Add(combined);
                }
            }

            return expanded;
        }


        private static (
            QuestDefinition? Quest,
            string RawText,
            double TitleScore,
            double CombinedScore,
            double RunnerUpCombinedScore) FindBestQuestUsingObjectiveContext(
                IEnumerable<string> titleLines,
                IEnumerable<string> objectiveLines,
                IEnumerable<QuestDefinition> candidates,
                double questMatchThreshold)
        {
            QuestDefinition? bestQuest = null;
            string bestRawText = "";
            double bestTitleScore = 0;
            double bestCombinedScore = 0;
            double runnerUpCombinedScore = 0;

            foreach (QuestDefinition quest in candidates)
            {
                (QuestDefinition? titleQuest,
                    string rawText,
                    double titleScore,
                    _) =
                    FindBestQuest(titleLines, new[] { quest });

                if (titleQuest == null || titleScore < questMatchThreshold)
                    continue;

                double objectiveScore = FindBestObjectiveContextScore(
                    quest,
                    objectiveLines);

                if (objectiveScore < StepMatchThreshold)
                    continue;

                double combinedScore =
                    titleScore * 0.55 + objectiveScore * 0.45;

                if (combinedScore <= bestCombinedScore)
                {
                    runnerUpCombinedScore = Math.Max(
                        runnerUpCombinedScore,
                        combinedScore);
                    continue;
                }

                runnerUpCombinedScore = bestCombinedScore;
                bestQuest = quest;
                bestRawText = rawText;
                bestTitleScore = titleScore;
                bestCombinedScore = combinedScore;
            }

            return (
                bestQuest,
                bestRawText,
                bestTitleScore,
                bestCombinedScore,
                runnerUpCombinedScore);
        }


        private static double FindBestObjectiveContextScore(
            QuestDefinition quest,
            IEnumerable<string> objectiveLines)
        {
            IEnumerable<string> knownObjectives = quest.ObjectiveAliases
                .Concat(quest.Steps.SelectMany(step =>
                    step.ObjectiveAliases.Concat(
                        step.TitleAliases.Prepend(step.Title))))
                .Where(value => !string.IsNullOrWhiteSpace(value));

            double bestScore = 0;

            foreach (string rawText in objectiveLines)
            {
                foreach (string knownObjective in knownObjectives)
                {
                    bestScore = Math.Max(
                        bestScore,
                        CalculateStepMatchScore(rawText, knownObjective));
                }
            }

            return bestScore;
        }


        private static (
            QuestDefinition? Quest,
            string RawText,
            double Score,
            double RunnerUpScore) FindBestQuest(
                IEnumerable<string> titleLines,
                IEnumerable<QuestDefinition> candidates)
        {
            Dictionary<QuestDefinition, (double Score, string RawText)>
                scores = new();

            foreach (string rawText in titleLines)
            {
                if (Normalize(rawText).Length < 4)
                    continue;

                foreach (QuestDefinition quest in candidates)
                {
                    IEnumerable<string> names = quest.Aliases
                        .Append(quest.Title)
                        .Where(name => !string.IsNullOrWhiteSpace(name));

                    foreach (string name in names)
                    {
                        double score = CalculateMatchScore(rawText, name);

                        if (!scores.TryGetValue(quest, out var current) ||
                            score > current.Score)
                        {
                            scores[quest] = (score, rawText);
                        }
                    }
                }
            }

            var ordered = scores
                .OrderByDescending(entry => entry.Value.Score)
                .ToList();

            return ordered.Count == 0
                ? (null, "", 0, 0)
                : (
                    ordered[0].Key,
                    ordered[0].Value.RawText,
                    ordered[0].Value.Score,
                    ordered.Count > 1 ? ordered[1].Value.Score : 0);
        }


        private static List<int> DetectRecommendedLevels(
            IEnumerable<string> titleLines)
        {
            List<int> levels = new();

            foreach (string line in titleLines)
            {
                foreach (Match match in Regex.Matches(
                    line,
                    @"(?:\[\s*)?[lLiI][vVwWyY]\s*\.?\s*(?<level>\d{1,2})(?!\d)(?:\s*\])?",
                    RegexOptions.CultureInvariant))
                {
                    if (int.TryParse(
                            match.Groups["level"].Value,
                            out int level) &&
                        level is >= 1 and <= 99 &&
                        !levels.Contains(level))
                    {
                        levels.Add(level);
                    }
                }
            }

            return levels;
        }


        private static double CalculateMatchScore(string rawText, string knownText)
        {
            string normalizedText = Normalize(rawText);
            string normalizedKnownText = Normalize(knownText);

            if (normalizedText.Length < 4 || normalizedKnownText.Length < 4)
                return 0;

            double score = Math.Max(
                Similarity(normalizedText, normalizedKnownText),
                BestContainedSimilarity(normalizedText, normalizedKnownText));

            score = Math.Max(
                score,
                TruncatedTitlePrefixScore(rawText, knownText));

            foreach (string token in rawText.Split(
                new[] { ' ', '\t', '[', ']', '(', ')', '.', ':', '-' },
                StringSplitOptions.RemoveEmptyEntries))
            {
                string normalizedToken = Normalize(token);

                if (normalizedToken.Length >= 4)
                {
                    score = Math.Max(
                        score,
                        Similarity(normalizedToken, normalizedKnownText));

                    // Aion kürzt lange Titel mit "...". Tesseract liefert
                    // dann beispielsweise nur "Contaminat". Ein ausreichend
                    // langer Token am Anfang des bekannten Titels ist ein
                    // starkes, aber nicht pauschal perfektes Signal.
                    if (normalizedToken.Length >= 7 &&
                        normalizedKnownText.StartsWith(
                            normalizedToken,
                            StringComparison.Ordinal))
                    {
                        double prefixCoverage = normalizedToken.Length /
                            (double)normalizedKnownText.Length;

                        score = Math.Max(
                            score,
                            Math.Min(0.97, 0.88 + 0.10 * prefixCoverage));
                    }
                }
            }

            if (normalizedText.Contains(normalizedKnownText, StringComparison.Ordinal) ||
                normalizedKnownText.Contains(normalizedText, StringComparison.Ordinal))
            {
                int shorter = Math.Min(normalizedText.Length, normalizedKnownText.Length);

                if (shorter >= 6)
                    score = Math.Max(score, 0.92);
            }

            return score;
        }


        private static double CalculateStepMatchScore(
            string rawText,
            string knownText)
        {
            // Sehr kurze OCR-Fragmente wie "Talk to" oder "Go to" tragen
            // keine Identität. Ein Containment-Treffer darauf darf niemals
            // einen konkreten Questschritt und damit eine Notiz auswählen.
            if (Normalize(rawText).Length < 8)
                return 0;

            double score = CalculateMatchScore(rawText, knownText);
            string? rawAction = FindObjectiveAction(rawText);
            string? knownAction = FindObjectiveAction(knownText);

            // Gleiche Ortsnamen sind bei Questschritten häufig. "Talk to ..."
            // darf deshalb niemals allein wegen "Aldelle Village" als
            // "Return to ..." akzeptiert werden. Im Zweifel zeigen wir keine
            // Notiz statt einer Notiz für die falsche Aktion.
            if (rawAction != null && knownAction != null &&
                !string.Equals(
                    rawAction,
                    knownAction,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Math.Min(score, StepMatchThreshold - 0.01);
            }

            return score;
        }


        private static bool IsExactObjectiveTextMatch(
            string rawText,
            string stepTitle,
            double score)
        {
            if (score < 0.99)
                return false;

            return HasExactNormalizedObjectiveText(rawText, stepTitle);
        }


        private static bool HasExactNormalizedObjectiveText(
            string rawText,
            string knownText)
        {

            string normalizedRawText = Normalize(
                TrimToObjectiveAction(rawText));
            string normalizedKnownText = Normalize(knownText);

            return normalizedKnownText.Length >= 8 &&
                string.Equals(
                    normalizedRawText,
                    normalizedKnownText,
                    StringComparison.Ordinal);
        }


        private static string? FindObjectiveAction(string text)
        {
            return Regex.Matches(
                    text,
                    @"[A-Za-z]+",
                    RegexOptions.CultureInvariant)
                .Select(match => match.Value)
                .FirstOrDefault(ObjectiveActionWords.Contains);
        }


        private static bool HaveMatchingObjectiveActions(
            string rawText,
            string knownText)
        {
            string? rawAction = FindObjectiveAction(rawText);
            string? knownAction = FindObjectiveAction(knownText);

            return rawAction != null &&
                knownAction != null &&
                string.Equals(
                    rawAction,
                    knownAction,
                    StringComparison.OrdinalIgnoreCase);
        }


        private static bool HaveCompatibleObjectiveIdentity(
            string rawText,
            string knownText)
        {
            string? rawAction = FindObjectiveAction(rawText);
            string? knownAction = FindObjectiveAction(knownText);

            // Ein einzelner Ortsname wie "Campsite" darf niemals eine
            // handlungsgebundene Aliaszeile wie "Talk to Nemon at ..."
            // bestaetigen. ExpandWrappedLines liefert fuer echte Umbrueche
            // zusaetzlich die vollstaendige Zeile inklusive Aktion.
            if (knownAction != null && rawAction == null)
                return false;

            if (rawAction == null || knownAction == null)
                return true;

            if (!string.Equals(
                rawAction,
                knownAction,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            List<string> rawIdentity = GetObjectiveIdentityTokens(rawText);
            List<string> knownIdentity = GetObjectiveIdentityTokens(knownText);

            if (rawIdentity.Count == 0 || knownIdentity.Count == 0)
                return false;

            if (!AreSimilarIdentityTokens(rawIdentity[0], knownIdentity[0]))
                return false;

            IReadOnlyList<string> knownQualifiers = knownIdentity.Skip(1).ToList();
            IReadOnlyList<string> rawQualifiers = rawIdentity.Skip(1).ToList();

            // A generic alias such as "Talk to Nemon" cannot identify a
            // different location-qualified handoff safely.
            if (knownQualifiers.Count == 0)
                return rawQualifiers.Count == 0;

            int matchedQualifiers = knownQualifiers.Count(knownToken =>
                rawQualifiers.Any(rawToken =>
                    AreSimilarIdentityTokens(rawToken, knownToken)));

            return matchedQualifiers >= Math.Ceiling(
                knownQualifiers.Count * 0.5);
        }


        private static List<string> GetObjectiveIdentityTokens(string text)
        {
            string? action = FindObjectiveAction(text);

            if (action == null)
                return new List<string>();

            int actionIndex = text.IndexOf(
                action,
                StringComparison.OrdinalIgnoreCase);
            string actionTail = actionIndex >= 0
                ? text[(actionIndex + action.Length)..]
                : text;

            return Regex.Matches(actionTail, @"[A-Za-z]+")
                .Select(match => match.Value.ToLowerInvariant())
                .Where(token => token.Length >= 3)
                .Where(token => !ObjectiveActionWords.Contains(token))
                .Where(token => !ObjectiveIdentityStopWords.Contains(token))
                .ToList();
        }


        private static bool AreSimilarIdentityTokens(
            string left,
            string right)
        {
            return Similarity(Normalize(left), Normalize(right)) >= 0.68;
        }


        private static double AdjustTitleScoreForSpecificity(
            string rawText,
            string knownTitle,
            double score)
        {
            List<string> rawIdentity = GetLooseTitleIdentityTokens(rawText);
            List<string> knownIdentity = GetLooseTitleIdentityTokens(knownTitle);

            if (rawIdentity.Count <= knownIdentity.Count ||
                knownIdentity.Count == 0)
            {
                return score;
            }

            bool knownTitleIsShorterPrefix = knownIdentity
                .Select((token, index) => new { token, index })
                .All(entry => entry.index < rawIdentity.Count &&
                    AreSimilarIdentityTokens(
                        rawIdentity[entry.index],
                        entry.token));

            // "Find the Young Elim" is not the same objective as
            // "Find the Young Elim survivors". Extra trailing identity words
            // must prevent the shorter title from becoming a safe match.
            return knownTitleIsShorterPrefix
                ? Math.Min(
                    score,
                    StepMatchThreshold - MinimumMatchMargin - 0.01)
                : score;
        }


        private static List<string> GetLooseTitleIdentityTokens(string text)
        {
            List<string> tokens = Regex.Matches(text, @"[A-Za-z]+")
                .Select(match => match.Value.ToLowerInvariant())
                .Where(token => token.Length >= 3)
                .Where(token => !ObjectiveIdentityStopWords.Contains(token))
                .ToList();

            if (tokens.Count > 0 && ObjectiveActionWords.Any(action =>
                Similarity(Normalize(tokens[0]), Normalize(action)) >= 0.55))
            {
                tokens.RemoveAt(0);
            }

            return tokens;
        }


        private static double TruncatedTitlePrefixScore(
            string recognizedText,
            string knownTitle)
        {
            List<string> recognizedWords = Regex.Matches(
                    recognizedText,
                    @"[A-Za-z]+",
                    RegexOptions.CultureInvariant)
                .Select(match => Normalize(match.Value))
                .Where(word => word.Length > 0)
                .ToList();

            List<string> knownWords = Regex.Matches(
                    knownTitle,
                    @"[A-Za-z]+",
                    RegexOptions.CultureInvariant)
                .Select(match => Normalize(match.Value))
                .Where(word => word.Length > 0)
                .ToList();

            if (recognizedWords.Count == 0 || knownWords.Count == 0)
                return 0;

            string normalizedKnownTitle = string.Concat(knownWords);
            int minimumPrefixLength = Math.Max(
                7,
                knownWords[0].Length + (knownWords.Count > 1 ? 3 : 0));
            double bestScore = 0;

            for (int start = 0; start < recognizedWords.Count; start++)
            {
                string prefix = "";

                for (int end = start;
                     end < recognizedWords.Count && end < start + knownWords.Count + 1;
                     end++)
                {
                    prefix += recognizedWords[end];

                    if (!normalizedKnownTitle.StartsWith(
                            prefix,
                            StringComparison.Ordinal))
                    {
                        break;
                    }

                    if (prefix.Length < minimumPrefixLength)
                        continue;

                    double coverage = prefix.Length /
                        (double)normalizedKnownTitle.Length;

                    bestScore = Math.Max(
                        bestScore,
                        Math.Min(0.98, 0.90 + 0.08 * coverage));
                }
            }

            return bestScore;
        }


        private static string Normalize(string value)
        {
            char[] characters = value
                .ToLowerInvariant()
                .Where(char.IsLetter)
                .ToArray();

            return new string(characters);
        }


        private static double Similarity(string left, string right)
        {
            if (left.Length == 0 || right.Length == 0)
                return 0;

            int distance = LevenshteinDistance(left, right);
            return 1.0 - distance / (double)Math.Max(left.Length, right.Length);
        }


        private static double BestContainedSimilarity(
            string recognizedText,
            string knownName)
        {
            if (recognizedText.Length <= knownName.Length)
                return Similarity(recognizedText, knownName);

            double best = 0;
            int minimumLength = Math.Max(4, knownName.Length - 3);
            int maximumLength = Math.Min(
                recognizedText.Length,
                knownName.Length + 3);

            for (int length = minimumLength; length <= maximumLength; length++)
            {
                for (int start = 0;
                     start + length <= recognizedText.Length;
                     start++)
                {
                    string window = recognizedText.Substring(start, length);
                    best = Math.Max(best, Similarity(window, knownName));
                }
            }

            return best;
        }


        private static int LevenshteinDistance(string left, string right)
        {
            int[] previous = Enumerable.Range(0, right.Length + 1).ToArray();
            int[] current = new int[right.Length + 1];

            for (int i = 1; i <= left.Length; i++)
            {
                current[0] = i;

                for (int j = 1; j <= right.Length; j++)
                {
                    int substitutionCost = left[i - 1] == right[j - 1] ? 0 : 1;

                    current[j] = Math.Min(
                        Math.Min(current[j - 1] + 1, previous[j] + 1),
                        previous[j - 1] + substitutionCost);
                }

                (previous, current) = (current, previous);
            }

            return previous[right.Length];
        }
    }
}
