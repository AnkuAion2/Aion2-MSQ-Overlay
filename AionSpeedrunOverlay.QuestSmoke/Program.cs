using AionSpeedrunOverlay.Capture;
using AionSpeedrunOverlay.Models;
using AionSpeedrunOverlay.Quest;
using System.Drawing;

bool summaryOnly = args.Contains("--summary", StringComparer.Ordinal);
string[] inputPaths = args
    .Where(arg => !string.Equals(
        arg,
        "--summary",
        StringComparison.Ordinal))
    .ToArray();

if (inputPaths.Length == 0)
{
    Console.Error.WriteLine(
        "Mindestens einen vollständigen Screenshotpfad übergeben.");
    return 2;
}

string repositoryRoot = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
string projectRoot = Path.Combine(repositoryRoot, "AionSpeedrunOverlay");
string catalogPath = Path.Combine(
    projectRoot,
    "Data",
    "aion2_asmodian_mythic_quests_lvl1-45.json");
string tessdataPath = Path.Combine(projectRoot, "tessdata");
string levelLessContextPath = Path.Combine(
    projectRoot,
    "Data",
    "level-less-contexts.json");

using QuestRecognitionService service = new(
    new ScreenCaptureService(),
    catalogPath,
    tessdataPath,
    levelLessContextPath);

QuestCatalog catalog = new(catalogPath, levelLessContextPath);
QuestMatch missingLevel = catalog.FindBestMatch(
    new[] { "Creeping Shadow" },
    new[] { "Search for traces of survivors" });
QuestMatch wrongLevel = catalog.FindBestMatch(
    new[] { "[Lv. 40] Creeping Shadow" },
    new[] { "Search for traces of survivors" });
QuestMatch levelLessTutorial = catalog.FindBestMatch(
    new[] { "Ishalgen" },
    new[] { "Find the Red Fafnir" });
QuestMatch instanceContext = catalog.FindBestMatch(
    new[] { "The Being Beneath the Lake" },
    new[] { "Dive into the water" });
QuestMatch argitStep = catalog.FindBestMatch(
    new[] { "[Lv. 6] Signs of a Cras..." },
    new[] { "Talk to Argit at Aldelle Village" });
QuestMatch actionConflict = catalog.FindBestMatch(
    new[] { "[Lv. 6] Signs of a Cras..." },
    new[] { "Talk to an unknown person at Aldelle Village" });
QuestMatch exactStepTitle = catalog.FindBestMatch(
    new[] { "[Lv. 6] Signs of a Cras..." },
    new[] { "Report the situation to Pelleir", "Talk to Pelleir" });
QuestMatch deeperInstanceStep = catalog.FindBestMatch(
    new[] { "The Being Beneath the Lake" },
    new[] { "Dive and move deeper into the cave" });
QuestMatch incompleteObjective = catalog.FindBestMatch(
    new[] { "[Lv. 5] The Being Beneath the Lake" },
    new[] { "Talk to" });
QuestMatch deeperInstanceAlias = catalog.FindBestMatch(
    new[] { "The Being Beneath the Lake" },
    new[] { "Move deeper into the cave" });
QuestMatch approachLakeStep = catalog.FindBestMatch(
    new[] { "The Being Beneath the Lake" },
    new[] { "Approach the lake" });
QuestMatch expressQuestObjective = catalog.FindBestMatch(
    new[] { "[Lv.12] Express Daeva Delivery" },
    new[] { "Talk to Ulgorn at the Temporary Investigation Base" });
QuestMatch expressLaterStep = catalog.FindBestMatch(
    new[] { "[Lv.12] Express Daeva Delivery" },
    new[] { "Talk to Ulgorn at Quai Campsite" });
QuestMatch creepingMoslanStep = catalog.FindBestMatch(
    new[] { "[Lv.21] Creeping Shadow" },
    new[] { "Find out where Urd is", "Go to Moslan Forest" });
QuestMatch creepingAmbiguousTitle = catalog.FindBestMatch(
    new[] { "[Lv.21] Creeping Shadow" },
    new[] { "Find out where Urd is" });
QuestMatch recoveredLevelVariant = catalog.FindBestMatch(
    new[]
    {
        "[Lv.2] Express Daeva Delivery",
        "[Lv.12] Express Daeva Delivery"
    },
    new[] { "Talk to Ulgorn at Quai Campsite" });
QuestMatch urgentPursuitHandoff = catalog.FindBestMatch(
    new[] { "[Lv.13] Urgent Pursuit" },
    new[] { "Talk to Asahr at Dream Crossroad" });
QuestMatch newLeadFinalStep = catalog.FindBestMatch(
    new[] { "[Lv.18] A New Lead" },
    new[]
    {
        "Talk to Menrik at Fang Hideout",
        "Talk to Baba at the Quai Campsite"
    });
QuestMatch creepingCollapsedChasm = catalog.FindBestMatch(
    new[] { "[Lv.21] Creeping Shadow" },
    new[] { "Talk to Nemon at the Collapsed Chasm" });
QuestMatch childrenElimRestStep = catalog.FindBestMatch(
    new[] { "[Lv.21] Children of the Elim" },
    new[] { "Talk to Nemon at Elim's Rest" });
QuestMatch disturbedNemonStep = catalog.FindBestMatch(
    new[] { "[Lv.22] Disturbed Balance" },
    new[] { "Talk to Nemon at Odar's Shade" });
QuestMatch voiceInsideEasternAltar = catalog.FindBestMatch(
    new[] { "[Lv.25] Voice of Rage" },
    new[] { "Find the Nornir inside the Eastern Altar" });
QuestMatch eyeMenrikNorthTent = catalog.FindBestMatch(
    new[] { "[Lv.32] Eye of the Red Tower" },
    new[] { "Talk to Menrik at the North Laborer's Tent" });
QuestMatch survivorsNeutralizeTypo = catalog.FindBestMatch(
    new[] { "[Lv.32] The Survivors" },
    new[] { "Neutrallze the Watchtower" });
QuestMatch rescueDeliverTypo = catalog.FindBestMatch(
    new[] { "[Lv.32] Rescue Operation" },
    new[] { "Collect supplies to deltyer to the Hidden Cave" });
QuestMatch scriptureUlgornHandoff = catalog.FindBestMatch(
    new[] { "[Lv.33] Scripture in the Ruins" },
    new[] { "Talk to Ulgorn at Graverobber Campsite" });
QuestMatch impendingFissureStep = catalog.FindBestMatch(
    new[] { "[Lv.34] Impending Threat" },
    new[] { "Talk to Nemon at the Fissure Cave" });
QuestMatch visibleTrapAdaptiveStep = catalog.FindBestMatch(
    new[] { "[Lv.34] Visible Trap" },
    new[]
    {
        "att 4 Talk to Nemen at the Shulak Street Stall",
        "att 1 Talk to Nemon at the Shulak Street Stall"
    });
QuestMatch crashHerbalistObserved = catalog.FindBestMatch(
    new[] { "[Lv. 11] Crash Site Rendezvous" },
    new[]
    {
        "az 1 Alnd the herbalist and gather",
        "neutrallzer materials",
        "Obtaln the sediment (2)5)"
    });
QuestMatch maliceDeliverObserved = catalog.FindBestMatch(
    new[] { "[Lv. 18] Malice Laid Bare" },
    new[]
    {
        "aaz-i1 Deltver the transformation",
        "Talk to Menrik"
    });
QuestMatch crossroadWatcherTent = catalog.FindBestMatch(
    new[] { "[Lv. 20] Crossroad" },
    new[] { "Talk to Nemon at the Watcher's Tent" });
QuestMatch enemyMannikikiStep = catalog.FindBestMatch(
    new[] { "[Lv.35] Enemy Territory" },
    new[] { "Talk to Mannikiki at the Steel Hammer Workcamp" });
QuestMatch lakeCliffStep = catalog.FindBestMatch(
    new[] { "[Lv.37] Lake of Confrontation" },
    new[] { "Talk to Nemon on the cliff above Idun's Lake" });
QuestMatch adjacentLevelRecovery = catalog.FindBestMatch(
    new[] { "[Lv.36] Enemy Territory" },
    new[] { "Deliver supplies to the Steel Hammer Workcamp" });
QuestMatch adjacentTruncatedLevelRecovery = catalog.FindBestMatch(
    new[] { "I[Lv. 36] Enemy Territ." },
    Array.Empty<string>());
QuestMatch mirrorCrobakhi = catalog.FindBestMatch(
    new[] { "[Lv.37] Mirror of Greed" },
    new[] { "Talk to Crobakhi at the Steel Hammer Merchants HQ" });
QuestMatch locationOnlyConflict = catalog.FindBestMatch(
    new[] { "[Lv.33] Scripture in the Ruins" },
    new[] { "Graverobber Campsite" });

if (missingLevel.Quest != null ||
    wrongLevel.Quest != null ||
    levelLessTutorial.Quest?.Id != "2100010" ||
    levelLessTutorial.Step?.Id != "find-the-red-fafnir" ||
    instanceContext.Quest?.RecognitionKind != "instance" ||
    instanceContext.Step?.Id != "dive-into-the-water" ||
    argitStep.Step?.Id != "talk-to-argit-at-aldelle-village" ||
    actionConflict.Step != null ||
    exactStepTitle.Step?.Id != "report-the-situation-to-pelleir" ||
    deeperInstanceStep.Step?.Id != "move-deeper-into-the-cave" ||
    incompleteObjective.Step != null ||
    deeperInstanceAlias.Step?.Id != "move-deeper-into-the-cave" ||
    approachLakeStep.Step?.Id != "approach-the-lake" ||
    expressQuestObjective.Quest?.Id != "2102050" ||
    expressQuestObjective.Step != null ||
    !expressQuestObjective.IsQuestObjectiveMatch ||
    !string.IsNullOrWhiteSpace(expressQuestObjective.RejectionReason) ||
    expressLaterStep.Step?.Id != "talk-to-ulgorn-at-quai-campsite" ||
    creepingMoslanStep.Step?.Id != "find-out-where-urd-is-moslan" ||
    creepingAmbiguousTitle.Step != null ||
    creepingAmbiguousTitle.RejectionReason != "step-ambiguous" ||
    recoveredLevelVariant.DetectedLevel != 12 ||
    recoveredLevelVariant.Quest?.Id != "2102050" ||
    urgentPursuitHandoff.Quest?.Id != "2102070" ||
    urgentPursuitHandoff.Step != null ||
    !urgentPursuitHandoff.IsNextQuestHandoff ||
    QuestRecognitionIdentity.CreateNoteKey(urgentPursuitHandoff) != "" ||
    newLeadFinalStep.Quest?.Id != "2102110" ||
    newLeadFinalStep.Step?.Id != "talk-to-menrik-at-fang-hideout" ||
    newLeadFinalStep.IsNextQuestHandoff ||
    creepingCollapsedChasm.Step?.Id !=
        "talk-to-nemon-at-the-collapsed-chasm" ||
    creepingCollapsedChasm.IsQuestObjectiveMatch ||
    childrenElimRestStep.Quest?.Id != "2102170" ||
    childrenElimRestStep.Step?.Id !=
        "talk-to-nemon-at-the-elims-rest" ||
    childrenElimRestStep.IsNextQuestHandoff ||
    disturbedNemonStep.Quest?.Id != "2102190" ||
    disturbedNemonStep.Step?.Id !=
        "talk-to-nemon-at-odars-shade" ||
    disturbedNemonStep.IsNextQuestHandoff ||
    voiceInsideEasternAltar.Step?.Id !=
        "find-the-nornir-inside-the-eastern-altar" ||
    voiceInsideEasternAltar.IsQuestObjectiveMatch ||
    eyeMenrikNorthTent.Step?.Id !=
        "talk-to-menrik-at-the-north-laborers-tent" ||
    eyeMenrikNorthTent.IsQuestObjectiveMatch ||
    survivorsNeutralizeTypo.Step?.Id !=
        "neutralize-the-watchtower-camp" ||
    rescueDeliverTypo.Step?.Id !=
        "collect-supplies-to-deliver-to-the-hidden-cave" ||
    scriptureUlgornHandoff.Quest?.Id != "2102370" ||
    !scriptureUlgornHandoff.IsNextQuestHandoff ||
    impendingFissureStep.Quest?.Id != "2102380" ||
    impendingFissureStep.Step?.Id !=
        "talk-to-nemon-at-the-fissure-cave" ||
    impendingFissureStep.IsNextQuestHandoff ||
    visibleTrapAdaptiveStep.Quest?.Id != "2102390" ||
    visibleTrapAdaptiveStep.Step?.Id !=
        "talk-to-nemon-at-the-shulak-street-stall" ||
    crashHerbalistObserved.Quest?.Id != "2102030" ||
    crashHerbalistObserved.Step?.Id !=
        "find-the-herbalist-and-gather-neutralizer-materials" ||
    maliceDeliverObserved.Quest?.Id != "2102115" ||
    maliceDeliverObserved.Step?.Id !=
        "deliver-the-transformation-materials" ||
    crossroadWatcherTent.Quest?.Id != "2102150" ||
    crossroadWatcherTent.Step?.Id !=
        "talk-to-nemon-at-the-watchers-tent" ||
    crossroadWatcherTent.IsNextQuestHandoff ||
    enemyMannikikiStep.Quest?.Id != "2102450" ||
    enemyMannikikiStep.Step?.Id !=
        "deliver-supplies-to-the-steel-hammer-workcamp" ||
    enemyMannikikiStep.IsNextQuestHandoff ||
    lakeCliffStep.Quest?.Id != "2102575" ||
    lakeCliffStep.Step?.Id !=
        "talk-to-nemon-on-the-cliff-above-iduns-lake" ||
    lakeCliffStep.IsNextQuestHandoff ||
    adjacentLevelRecovery.Quest?.Id != "2102450" ||
    adjacentLevelRecovery.Step?.Id !=
        "deliver-supplies-to-the-steel-hammer-workcamp" ||
    !adjacentLevelRecovery.IsAdjacentLevelRecovery ||
    adjacentLevelRecovery.DetectedLevel != 36 ||
    adjacentTruncatedLevelRecovery.Quest?.Id != "2102450" ||
    !adjacentTruncatedLevelRecovery.IsAdjacentLevelRecovery ||
    mirrorCrobakhi.Step?.Id !=
        "talk-to-crobakhi-at-the-steel-hammer-merchants-hq" ||
    locationOnlyConflict.Step != null ||
    locationOnlyConflict.IsNextQuestHandoff)
{
    Console.Error.WriteLine(
        "Sicherheitsprüfung fehlgeschlagen: Quest wurde ohne passenden " +
        "Levelkontext akzeptiert.");
    Console.Error.WriteLine(
        $"missing={missingLevel.Quest?.Id ?? "-"}/" +
        $"{missingLevel.RejectionReason}; wrong={wrongLevel.Quest?.Id ?? "-"}; " +
        $"tutorial={levelLessTutorial.Quest?.Id ?? "-"}/" +
        $"{levelLessTutorial.Step?.Id ?? "-"}; instance=" +
        $"{instanceContext.Quest?.RecognitionKind ?? "-"}/" +
        $"{instanceContext.Step?.Id ?? "-"}; argit=" +
        $"{argitStep.Step?.Id ?? "-"}; conflict=" +
        $"{actionConflict.Step?.Id ?? "-"}; exact=" +
        $"{exactStepTitle.Step?.Id ?? "-"}; deeper=" +
        $"{deeperInstanceStep.Step?.Id ?? "-"}; incomplete=" +
        $"{incompleteObjective.Step?.Id ?? "-"}; deeper-alias=" +
        $"{deeperInstanceAlias.Step?.Id ?? "-"}; approach=" +
        $"{approachLakeStep.Step?.Id ?? "-"}; express-root=" +
        $"{expressQuestObjective.Step?.Id ?? "-"}/" +
        $"root={expressQuestObjective.IsQuestObjectiveMatch}/" +
        $"{expressQuestObjective.RejectionReason}; express-later=" +
        $"{expressLaterStep.Step?.Id ?? "-"}; creeping=" +
        $"{creepingMoslanStep.Step?.Id ?? "-"}; creeping-title=" +
        $"{creepingAmbiguousTitle.RejectionReason}; recovered-level=" +
        $"{recoveredLevelVariant.DetectedLevel?.ToString() ?? "-"}; " +
        $"urgent-handoff={urgentPursuitHandoff.Quest?.Id ?? "-"}/" +
        $"{urgentPursuitHandoff.IsNextQuestHandoff}; " +
        $"new-lead-step={newLeadFinalStep.Quest?.Id ?? "-"}/" +
        $"{newLeadFinalStep.Step?.Id ?? "-"}; " +
        $"creeping-collapsed={creepingCollapsedChasm.Step?.Id ?? "-"}/" +
        $"root={creepingCollapsedChasm.IsQuestObjectiveMatch}; " +
        $"children-elim-rest={childrenElimRestStep.Quest?.Id ?? "-"}/" +
        $"{childrenElimRestStep.Step?.Id ?? "-"}; " +
        $"disturbed-nemon={disturbedNemonStep.Quest?.Id ?? "-"}/" +
        $"{disturbedNemonStep.Step?.Id ?? "-"}; " +
        $"voice-inside={voiceInsideEasternAltar.Step?.Id ?? "-"}/" +
        $"root={voiceInsideEasternAltar.IsQuestObjectiveMatch}; " +
        $"eye-menrik-north={eyeMenrikNorthTent.Step?.Id ?? "-"}; " +
        $"survivors-typo={survivorsNeutralizeTypo.Step?.Id ?? "-"}; " +
        $"rescue-typo={rescueDeliverTypo.Step?.Id ?? "-"}; " +
        $"scripture-handoff={scriptureUlgornHandoff.Quest?.Id ?? "-"}/" +
        $"{scriptureUlgornHandoff.IsNextQuestHandoff}; " +
        $"fissure-step={impendingFissureStep.Quest?.Id ?? "-"}/" +
        $"{impendingFissureStep.Step?.Id ?? "-"}; " +
        $"mannikiki-step={enemyMannikikiStep.Quest?.Id ?? "-"}/" +
        $"{enemyMannikikiStep.Step?.Id ?? "-"}; " +
        $"cliff-step={lakeCliffStep.Quest?.Id ?? "-"}/" +
        $"{lakeCliffStep.Step?.Id ?? "-"}; " +
        $"level-recovery={adjacentLevelRecovery.Quest?.Id ?? "-"}/" +
        $"{adjacentLevelRecovery.Step?.Id ?? "-"}/" +
        $"{adjacentLevelRecovery.IsAdjacentLevelRecovery}; " +
        $"truncated-level-recovery=" +
        $"{adjacentTruncatedLevelRecovery.Quest?.Id ?? "-"}/" +
        $"{adjacentTruncatedLevelRecovery.Score:P0}/" +
        $"{adjacentTruncatedLevelRecovery.RunnerUpScore:P0}/" +
        $"{adjacentTruncatedLevelRecovery.IsAdjacentLevelRecovery}; " +
        $"mirror-crobakhi={mirrorCrobakhi.Step?.Id ?? "-"}; " +
        $"crash-herbalist={crashHerbalistObserved.Quest?.Id ?? "-"}/" +
        $"{crashHerbalistObserved.Step?.Id ?? "-"}/" +
        $"{crashHerbalistObserved.StepScore:P0}/" +
        $"{crashHerbalistObserved.RejectionReason}; " +
        $"malice-deliver={maliceDeliverObserved.Quest?.Id ?? "-"}/" +
        $"{maliceDeliverObserved.Step?.Id ?? "-"}/" +
        $"{maliceDeliverObserved.StepScore:P0}/" +
        $"{maliceDeliverObserved.RejectionReason}; " +
        $"crossroad-watcher={crossroadWatcherTent.Quest?.Id ?? "-"}/" +
        $"{crossroadWatcherTent.Step?.Id ?? "-"}/" +
        $"{crossroadWatcherTent.IsNextQuestHandoff}; " +
        $"location-only={locationOnlyConflict.Step?.Id ?? "-"}/" +
        $"{locationOnlyConflict.IsNextQuestHandoff}");
    return 3;
}

Console.WriteLine(
    "Sicherheitsprüfung: Level-Gate sowie explizite Tutorial-/Instanzkontexte korrekt");

QuestStepDefinition safetyStepA = new()
{
    Id = "step-a",
    Title = "Step A"
};
QuestDefinition safetyQuestA = new()
{
    Id = "quest-a",
    RecognitionKind = "quest",
    Steps = new() { safetyStepA }
};
QuestStepDefinition safetyStepB = new()
{
    Id = "step-b",
    Title = "Step B"
};
QuestStepDefinition safetyStepA2 = new()
{
    Id = "step-a-2",
    Title = "Step A 2"
};
QuestDefinition safetyQuestB = new()
{
    Id = "quest-b",
    RecognitionKind = "quest",
    Steps = new() { safetyStepB }
};
QuestMatch unresolvedSafetyMatch = new()
{
    Quest = safetyQuestA
};
QuestMatch resolvedSafetyMatch = new()
{
    Quest = safetyQuestA,
    Step = safetyStepA
};
QuestMatch changedQuestSafetyMatch = new()
{
    Quest = safetyQuestB,
    Step = safetyStepB
};
QuestMatch changedStepSafetyMatch = new()
{
    Quest = safetyQuestA,
    Step = safetyStepA2
};
QuestDisplaySafetyPolicy displaySafetyPolicy = new();
string displayedSafetyId = "quest:quest-a:step-a";

if (displaySafetyPolicy.Observe(
        unresolvedSafetyMatch,
        displayedSafetyId) != QuestDisplaySafetyAction.Hold ||
    displaySafetyPolicy.Observe(
        unresolvedSafetyMatch,
        displayedSafetyId) != QuestDisplaySafetyAction.Hold ||
    displaySafetyPolicy.Observe(
        unresolvedSafetyMatch,
        displayedSafetyId) != QuestDisplaySafetyAction.Hold)
{
    Console.Error.WriteLine(
        "Display safety test failed: persistent unresolved objectives " +
        "must keep the last confirmed note stable.");
    return 8;
}

displaySafetyPolicy.Reset();
if (displaySafetyPolicy.Observe(
        unresolvedSafetyMatch,
        displayedSafetyId) != QuestDisplaySafetyAction.Hold ||
    displaySafetyPolicy.Observe(
        resolvedSafetyMatch,
        displayedSafetyId) != QuestDisplaySafetyAction.Hold ||
    displaySafetyPolicy.Observe(
        unresolvedSafetyMatch,
        displayedSafetyId) != QuestDisplaySafetyAction.Hold ||
    displaySafetyPolicy.Observe(
        unresolvedSafetyMatch,
        displayedSafetyId) != QuestDisplaySafetyAction.Hold ||
    displaySafetyPolicy.Observe(
        changedStepSafetyMatch,
        displayedSafetyId) != QuestDisplaySafetyAction.Hold ||
    displaySafetyPolicy.Observe(
        changedQuestSafetyMatch,
        displayedSafetyId) != QuestDisplaySafetyAction.Hold)
{
    Console.Error.WriteLine(
        "Display safety test failed: transient OCR loss or quest change " +
        "was handled incorrectly.");
    return 8;
}

Console.WriteLine(
    "Display safety check: confirmed notes remain stable across OCR gaps");

QuestMatch strongConfirmationStep = new()
{
    Quest = safetyQuestA,
    Step = safetyStepA,
    Score = 1.0,
    StepScore = 0.96,
    LevelFilterApplied = true
};
QuestMatch weakConfirmationStep = new()
{
    Quest = safetyQuestA,
    Step = safetyStepA,
    Score = 1.0,
    StepScore = 0.93,
    LevelFilterApplied = true
};
QuestMatch strongQuestOnlyConfirmation = new()
{
    Quest = safetyQuestA,
    Score = 0.95,
    LevelFilterApplied = true,
    IsQuestObjectiveMatch = true
};

if (QuestRecognitionConfirmationPolicy.GetRequiredConfirmations(
        strongConfirmationStep) != 3 ||
    QuestRecognitionConfirmationPolicy.GetRequiredConfirmations(
        weakConfirmationStep) != 4 ||
    QuestRecognitionConfirmationPolicy.GetRequiredConfirmations(
        strongQuestOnlyConfirmation) != 2)
{
    Console.Error.WriteLine(
        "Recognition confirmation policy failed: step changes were not " +
        "protected against a short-lived false objective.");
    return 8;
}

Console.WriteLine(
    "Confirmation policy: strong steps require 3 frames; weaker steps 4");

string regressionDataRoot = Path.Combine(
    repositoryRoot,
    "AionSpeedrunOverlay.QuestSmoke",
    "TestData");
string[] newLeadRegressionFiles =
{
    Path.Combine(
        regressionDataRoot,
        "a-new-lead-secondary-quests-1.png"),
    Path.Combine(
        regressionDataRoot,
        "a-new-lead-secondary-quests-2.png")
};

foreach (string regressionFile in newLeadRegressionFiles)
{
    if (!File.Exists(regressionFile))
    {
        Console.Error.WriteLine(
            $"Regressionstest-Datei fehlt: {regressionFile}");
        return 7;
    }

    QuestRecognitionResult regressionResult =
        service.RecognizeQuestRegionImageFile(regressionFile);

    if (regressionResult.Match.Quest?.Id != "2102110" ||
        regressionResult.Match.Step?.Id !=
            "talk-to-menrik-at-fang-hideout" ||
        regressionResult.Match.IsNextQuestHandoff ||
        regressionResult.RecognizedObjectiveLines.Any(line =>
            line.Contains("Baba", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Tomgus", StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine(
            "Regressionstest fehlgeschlagen: Ein grünes Questziel wurde " +
            "dem Hauptquest-Handoff zugeordnet.");
        Console.Error.WriteLine(
            $"{Path.GetFileName(regressionFile)}: quest=" +
            $"{regressionResult.Match.Quest?.Id ?? "-"}; step=" +
            $"{regressionResult.Match.Step?.Id ?? "-"}; handoff=" +
            $"{regressionResult.Match.IsNextQuestHandoff}; objectives=" +
            $"{string.Join(" | ", regressionResult.RecognizedObjectiveLines)}");
        return 7;
    }
}

string urgentRegressionFile = Path.Combine(
    regressionDataRoot,
    "urgent-pursuit-asahr-handoff.png");

if (!File.Exists(urgentRegressionFile))
{
    Console.Error.WriteLine(
        $"Regressionstest-Datei fehlt: {urgentRegressionFile}");
    return 7;
}

QuestRecognitionResult urgentRegressionResult =
    service.RecognizeQuestRegionImageFile(urgentRegressionFile);

bool urgentCurrentQuestRoot =
    urgentRegressionResult.Match.Quest?.Id == "2102065";
bool urgentNextQuestHandoff =
    urgentRegressionResult.Match.Quest?.Id == "2102070" &&
    urgentRegressionResult.Match.IsNextQuestHandoff;

if (urgentRegressionResult.Match.Step != null ||
    !urgentCurrentQuestRoot && !urgentNextQuestHandoff)
{
    Console.Error.WriteLine(
        "Regressionstest fehlgeschlagen: Asahr-Handoff war unsicher: " +
        $"quest={urgentRegressionResult.Match.Quest?.Id ?? "-"}, " +
        $"step={urgentRegressionResult.Match.Step?.Id ?? "-"}, " +
        $"handoff={urgentRegressionResult.Match.IsNextQuestHandoff}.");
    return 7;
}

Console.WriteLine(
    "Reale Sicherheits-Captures: grüne Nebenquests und Asahr-Handoff korrekt");

(string FileName,
    string QuestId,
    string? StepId,
    string RejectionReason,
    bool IsQuestObjectiveMatch)[] realRegressionCases =
{
    (
        "finding-nemon-uncover.png",
        "2102010",
        "uncover-who-is-behind-the-ishalgen-commission",
        "",
        false),
    (
        "contaminated-talk-niror.png",
        "2102040",
        "talk-to-niror-at-the-temporary-investigation-base",
        "",
        false),
    (
        "brigade-prison-escape.png",
        "2101090",
        "escape-from-azrakar-brigade-prison-compound",
        "",
        false),
    (
        "express-quai-campsite.png",
        "2102050",
        "talk-to-ulgorn-at-quai-campsite",
        "",
        false),
    (
        "express-temporary-base-handoff.png",
        "2102050",
        null,
        "",
        true),
    (
        "empyrean-find-ollen.png",
        "2102105",
        "find-ruins-administrator-ollen",
        "",
        false),
    (
        "imminent-join-ulgorn.png",
        "2102140",
        "join-ulgorn",
        "",
        false),
    (
        "creeping-moslan.png",
        "2102160",
        "find-out-where-urd-is-moslan",
        "",
        false),
    (
        "creeping-odars-shade.png",
        "2102160",
        "find-out-where-urd-is-odars-shade",
        "",
        false),
    (
        "brigade-prison-ambiguous-title.png",
        "2101090",
        null,
        "quest-ambiguous",
        false),
    (
        "live-children-find-young-elim-survivors.png",
        "2102170",
        "find-the-young-elim-survivors",
        "",
        false),
    (
        "live-creeping-collapsed-chasm.png",
        "2102160",
        "talk-to-nemon-at-the-collapsed-chasm",
        "",
        false),
    (
        "live-disturbed-nemon-handoff.png",
        "2102190",
        "talk-to-nemon-at-odars-shade",
        "",
        false),
    (
        "live-voice-rage-inside-eastern-altar.png",
        "2102210",
        "find-the-nornir-inside-the-eastern-altar",
        "",
        false),
    (
        "live-striving-varron-nornir-assembly.png",
        "2102230",
        "talk-to-varron-at-nornir-assembly",
        "",
        false),
    (
        "live-collective-restore-fire-altar.png",
        "2102240",
        "restore-the-fire-altar",
        "",
        false),
    (
        "live-land-rifts-nemon-water-altar.png",
        "2102250",
        "talk-to-nemon-at-the-water-altar",
        "",
        false),
    (
        "live-dying-light-grisilla-healing-spring.png",
        "2102255",
        "talk-to-grisilla-at-healing-spring",
        "",
        false),
    (
        "live-past-revealed-meet-urd.png",
        "2102270",
        "meet-urd-before-the-altar",
        "",
        false),
    (
        "live-bitter-join-menrik-with-secondary.png",
        "2102300",
        "join-menrik-at-briskwind-shelter",
        "",
        false),
    (
        "live-bitter-title-only.png",
        "2102300",
        null,
        "",
        false),
    (
        "live-eye-menrik-north-tent.png",
        "2102310",
        "talk-to-menrik-at-the-north-laborers-tent",
        "",
        false),
    (
        "live-survivors-neutralize-watchtower.png",
        "2102320",
        "neutralize-the-watchtower-camp",
        "",
        false),
    (
        "live-rescue-collect-hidden-cave.png",
        "2102330",
        "collect-supplies-to-deliver-to-the-hidden-cave",
        "",
        false)
};

foreach (var regressionCase in realRegressionCases)
{
    string regressionPath = Path.Combine(
        regressionDataRoot,
        regressionCase.FileName);

    if (!File.Exists(regressionPath))
    {
        Console.Error.WriteLine(
            $"Regression test file is missing: {regressionPath}");
        return 7;
    }

    QuestRecognitionResult result =
        service.RecognizeQuestRegionImageFile(regressionPath);

    if (result.Match.Quest?.Id != regressionCase.QuestId ||
        result.Match.Step?.Id != regressionCase.StepId ||
        !string.Equals(
            result.Match.RejectionReason,
            regressionCase.RejectionReason,
            StringComparison.Ordinal) ||
        result.Match.IsQuestObjectiveMatch !=
            regressionCase.IsQuestObjectiveMatch)
    {
        Console.Error.WriteLine(
            $"Real capture regression failed for " +
            $"{regressionCase.FileName}: quest=" +
            $"{result.Match.Quest?.Id ?? "-"}, step=" +
            $"{result.Match.Step?.Id ?? "-"}, rejection=" +
            $"{result.Match.RejectionReason}, questObjective=" +
            $"{result.Match.IsQuestObjectiveMatch}.");
        return 7;
    }
}

Console.WriteLine(
    "Real level 1-32 regression captures: all expected decisions passed");

(string FileName,
    string QuestId,
    string? StepId,
    bool IsNextQuestHandoff,
    bool IsAdjacentLevelRecovery)[] level33To37RegressionCases =
{
    (
        "live-scripture-ulgorn-handoff.png",
        "2102370",
        null,
        true,
        false),
    (
        "live-impending-fissure-handoff.png",
        "2102380",
        "talk-to-nemon-at-the-fissure-cave",
        false,
        false),
    (
        "live-enemy-deliver-level-misread.png",
        "2102450",
        "deliver-supplies-to-the-steel-hammer-workcamp",
        false,
        true),
    (
        "live-enemy-mannikiki-handoff-a.png",
        "2102450",
        "deliver-supplies-to-the-steel-hammer-workcamp",
        false,
        false),
    (
        "live-enemy-mannikiki-handoff-b.png",
        "2102450",
        "deliver-supplies-to-the-steel-hammer-workcamp",
        false,
        false),
    (
        "live-lake-nemon-cliff-handoff.png",
        "2102575",
        "talk-to-nemon-on-the-cliff-above-iduns-lake",
        false,
        false),
    (
        "live-mirror-crobakhi.png",
        "2102590",
        "talk-to-crobakhi-at-the-steel-hammer-merchants-hq",
        false,
        false)
};

foreach (var regressionCase in level33To37RegressionCases)
{
    string regressionPath = Path.Combine(
        regressionDataRoot,
        regressionCase.FileName);
    QuestRecognitionResult result =
        service.RecognizeQuestRegionImageFile(regressionPath);

    if (result.Match.Quest?.Id != regressionCase.QuestId ||
        result.Match.Step?.Id != regressionCase.StepId ||
        result.Match.IsNextQuestHandoff !=
            regressionCase.IsNextQuestHandoff ||
        result.Match.IsAdjacentLevelRecovery !=
            regressionCase.IsAdjacentLevelRecovery ||
        !string.IsNullOrWhiteSpace(result.Match.RejectionReason))
    {
        Console.Error.WriteLine(
            $"Level 33-37 regression failed for " +
            $"{regressionCase.FileName}: quest=" +
            $"{result.Match.Quest?.Id ?? "-"}, step=" +
            $"{result.Match.Step?.Id ?? "-"}, handoff=" +
            $"{result.Match.IsNextQuestHandoff}, levelRecovery=" +
            $"{result.Match.IsAdjacentLevelRecovery}, rejection=" +
            $"{result.Match.RejectionReason}.");
        return 7;
    }
}

Console.WriteLine(
    "Real level 33-37 transitions: all expected decisions passed");

(string FileName,
    string QuestId,
    string? ObservedQuestId,
    string? StepId,
    bool IsNextQuestHandoff)[] manualCaptureRegressionCases =
{
    ("manual-20260911-crash-site-herbalist.png", "2102030", null,
        "find-the-herbalist-and-gather-neutralizer-materials", false),
    ("manual-20260911-crash-site-ulgorn-a.png", "2102030", null,
        "talk-to-ulgorn-at-galoriks-inspection-area", false),
    ("manual-20260911-crash-site-ulgorn-b.png", "2102030", null,
        "talk-to-ulgorn-at-galoriks-inspection-area", false),
    ("manual-20260911-malice-deliver-a.png", "2102115", null,
        "deliver-the-transformation-materials", false),
    ("manual-20260911-malice-deliver-b.png", "2102115", null,
        "deliver-the-transformation-materials", false),
    ("manual-20260909-bitter-menrik.png", "2102300", null,
        "join-menrik-at-briskwind-shelter", false),
    ("manual-20260909-eye-menrik.png", "2102310", null,
        "talk-to-menrik-at-the-north-laborers-tent", false),
    ("manual-20260909-rescue-hidden-cave.png", "2102330", null,
        "find-out-what-is-currently-happening-in-the-hidden-cave", false),
    ("manual-20260910-last-refuge-base-a.png", "2102350", null,
        "investigate-the-marauder-base-near-the-entrance-of-the-ruins", false),
    ("manual-20260910-last-refuge-base-b.png", "2102350", null,
        "investigate-the-marauder-base-near-the-entrance-of-the-ruins", false),
    ("manual-20260910-last-refuge-intel.png", "2102350", null,
        "gather-intel-about-the-archon-from-the-graverobber-campsite", false),
    ("manual-20260910-scripture-ulgorn-handoff.png", "2102370",
        "2102360", null, true),
    ("manual-20260910-impending-fissure-handoff.png", "2102380", null,
        "talk-to-nemon-at-the-fissure-cave", false),
    ("manual-20260910-visible-trap-stall.png", "2102390", null,
        "talk-to-nemon-at-the-shulak-street-stall", false),
    ("manual-20260910-destruction-entrance.png", "2102400", null,
        "investigate-the-destruction-archon-underground-fortress-entrance",
        false),
    ("manual-20260910-destruction-stall-good.png", "2102400", null,
        "talk-to-nemon-at-the-shulak-street-stall", false),
    ("manual-20260910-destruction-stall-a.png", "2102400", null,
        "talk-to-nemon-at-the-shulak-street-stall", false),
    ("manual-20260910-destruction-stall-b.png", "2102400", null,
        "talk-to-nemon-at-the-shulak-street-stall", false),
    ("manual-20260910-enemy-mannikiki-handoff.png", "2102450", null,
        "deliver-supplies-to-the-steel-hammer-workcamp", false),
    ("manual-20260910-powder-clear-path.png", "2102460", null,
        "clear-the-path-to-the-shuttered-workstation", false),
    ("manual-20260910-powder-survivors.png", "2102460", null,
        "find-items-for-the-surviving-workers", false),
    ("manual-20260910-lake-cliff-handoff-a.png", "2102575", null,
        "talk-to-nemon-on-the-cliff-above-iduns-lake", false),
    ("manual-20260910-lake-cliff-handoff-b.png", "2102575", null,
        "talk-to-nemon-on-the-cliff-above-iduns-lake", false)
};

foreach (var regressionCase in manualCaptureRegressionCases)
{
    string regressionPath = Path.Combine(
        regressionDataRoot,
        regressionCase.FileName);
    QuestRecognitionResult result =
        service.RecognizeQuestRegionImageFile(regressionPath);

    if (result.Match.Quest?.Id != regressionCase.QuestId ||
        result.Match.ObservedQuest?.Id != regressionCase.ObservedQuestId ||
        result.Match.Step?.Id != regressionCase.StepId ||
        result.Match.IsNextQuestHandoff !=
            regressionCase.IsNextQuestHandoff ||
        !string.IsNullOrWhiteSpace(result.Match.RejectionReason))
    {
        Console.Error.WriteLine(
            $"Manual capture regression failed for " +
            $"{regressionCase.FileName}: quest=" +
            $"{result.Match.Quest?.Id ?? "-"}, observed=" +
            $"{result.Match.ObservedQuest?.Id ?? "-"}, step=" +
            $"{result.Match.Step?.Id ?? "-"}, handoff=" +
            $"{result.Match.IsNextQuestHandoff}, rejection=" +
            $"{result.Match.RejectionReason}.");
        return 7;
    }
}

Console.WriteLine(
    "Manual capture regressions: all 23 expected decisions passed");

(string FileName, string QuestId, string StepId)[]
    level40To45RegressionCases =
{
    (
        "live-healing-lakiba.png",
        "2102612",
        "talk-to-lakiba-at-amuntas-hideout"),
    (
        "live-symbol-amunta-tomb.png",
        "2102620",
        "talk-to-amunta-at-zemurrus-tomb"),
    (
        "live-breaking-bodyguard-vambraces.png",
        "2102670",
        "prepare-to-infiltrate-muqakas-quarters-materials"),
    (
        "live-farewells-quai-baba.png",
        "2102781",
        "bid-farewell-at-the-quai-campsite")
};

foreach (var regressionCase in level40To45RegressionCases)
{
    string regressionPath = Path.Combine(
        regressionDataRoot,
        regressionCase.FileName);
    QuestRecognitionResult result =
        service.RecognizeQuestRegionImageFile(regressionPath);

    if (result.Match.Quest?.Id != regressionCase.QuestId ||
        result.Match.Step?.Id != regressionCase.StepId ||
        !string.IsNullOrWhiteSpace(result.Match.RejectionReason))
    {
        Console.Error.WriteLine(
            $"Level 40-45 regression failed for " +
            $"{regressionCase.FileName}: quest=" +
            $"{result.Match.Quest?.Id ?? "-"}, step=" +
            $"{result.Match.Step?.Id ?? "-"}, rejection=" +
            $"{result.Match.RejectionReason}.");
        return 7;
    }
}

string nornirNegativePath = Path.Combine(
    regressionDataRoot,
    "live-farewells-nornir-not-quai.png");
QuestRecognitionResult nornirNegative =
    service.RecognizeQuestRegionImageFile(nornirNegativePath);

if (nornirNegative.Match.Step?.Id ==
    "bid-farewell-at-the-quai-campsite")
{
    Console.Error.WriteLine(
        "Level 40-45 safety regression failed: the Nornir objective was " +
        "misidentified as the similarly prefixed Quai objective.");
    return 7;
}

Console.WriteLine(
    "Real level 40-45 transitions: all expected decisions passed");

string noteStoreTestRoot = Path.Combine(
    Path.GetTempPath(),
    "AionSpeedrunOverlay-QuestSmoke-" + Guid.NewGuid().ToString("N"));

try
{
    Directory.CreateDirectory(noteStoreTestRoot);

    string noteStorePath = Path.Combine(
        noteStoreTestRoot,
        "notes-overrides.json");
    const string sampleNoteKey = "quest:2102160:search-for-traces";
    QuestNoteStore noteStore = new(noteStorePath);
    noteStore.SaveOverride(
        sampleNoteKey,
        QuestNoteStore.ParseEditorText(
            "1 gehe zu XY\n2. sprich mit XY\n3) mach dies > 4 mach jenes"));

    QuestNoteStore reloadedNoteStore = new(noteStorePath);

    if (!reloadedNoteStore.TryGetOverride(
            sampleNoteKey,
            out IReadOnlyList<string> reloadedNotes) ||
        reloadedNotes.Count != 4 ||
        reloadedNotes[0] != "gehe zu XY" ||
        reloadedNotes[3] != "mach jenes" ||
        QuestNoteStore.ParseEditorText("3 km nach Norden").Single() !=
            "3 km nach Norden" ||
        QuestNoteStore.FormatForDisplay(reloadedNotes) !=
            "1. gehe zu XY" + Environment.NewLine +
            "2. sprich mit XY" + Environment.NewLine +
            "3. mach dies" + Environment.NewLine +
            "4. mach jenes")
    {
        Console.Error.WriteLine(
            "Notizspeicher-Prüfung fehlgeschlagen: Formatierung oder " +
            "Neustart-Laden.");
        return 8;
    }

    reloadedNoteStore.SaveOverride(sampleNoteKey, Array.Empty<string>());
    QuestNoteStore emptyNoteStore = new(noteStorePath);

    if (!emptyNoteStore.TryGetOverride(
            sampleNoteKey,
            out IReadOnlyList<string> emptyNotes) ||
        emptyNotes.Count != 0)
    {
        Console.Error.WriteLine(
            "Notizspeicher-Prüfung fehlgeschlagen: leerer Override.");
        return 8;
    }

    emptyNoteStore.RemoveOverride(sampleNoteKey);

    if (new QuestNoteStore(noteStorePath).TryGetOverride(
        sampleNoteKey,
        out _))
    {
        Console.Error.WriteLine(
            "Notizspeicher-Prüfung fehlgeschlagen: Standard zurücksetzen.");
        return 8;
    }
}
finally
{
    if (Directory.Exists(noteStoreTestRoot))
        Directory.Delete(noteStoreTestRoot, true);
}

Console.WriteLine(
    "Notizspeicher-Prüfung: nummerierte Schritte und Neustart-Laden korrekt");

int rejected = 0;
int questMatches = 0;
int stepMatches = 0;
Dictionary<string, int> rejectionCounts =
    new(StringComparer.Ordinal);
List<string> imagePaths = inputPaths
    .SelectMany(path => Directory.Exists(path)
        ? Directory.EnumerateFiles(path, "*.png")
        : new[] { path })
    .ToList();

foreach (string imagePath in imagePaths)
{
    using Bitmap probe = new(imagePath);
    QuestRecognitionResult result = probe.Width >= 1000
        ? service.RecognizeImageFile(imagePath)
        : service.RecognizeQuestRegionImageFile(imagePath);
    QuestMatch match = result.Match;

    if (match.Quest != null && match.RejectionReason != "quest-ambiguous")
        questMatches++;

    if (match.Step != null)
        stepMatches++;

    string rejection = string.IsNullOrWhiteSpace(match.RejectionReason)
        ? "none"
        : match.RejectionReason;
    rejectionCounts[rejection] = rejectionCounts.GetValueOrDefault(rejection) + 1;

    if (!summaryOnly)
    {
        Console.WriteLine(Path.GetFileName(imagePath));
        Console.WriteLine(
            $"  Kandidaten: gelb {result.YellowLineCandidates}, " +
            $"weiß {result.WhiteLineCandidates}, " +
            $"Titel-OCR {result.OcrTitleLines}, " +
            $"Ziel-OCR {result.OcrObjectiveLines}");
        Console.WriteLine(
            $"  Gelb-Bounds: {string.Join(" | ", result.TitleCandidateBounds)}");
        Console.WriteLine(
            $"  Weiß-Bounds: {string.Join(" | ", result.ObjectiveCandidateBounds)}");
        Console.WriteLine($"  Titel-OCR: {string.Join(" | ", result.RecognizedLines)}");
        Console.WriteLine($"  Ziel-OCR:  {string.Join(" | ", result.RecognizedObjectiveLines)}");
        Console.WriteLine(
            $"  Level: {match.DetectedLevel?.ToString() ?? "-"}; " +
            $"Quest: {match.Quest?.Title ?? "KEINE"}; " +
            $"Schritt: {match.Step?.Title ?? "KEINER"}");
        Console.WriteLine(
            $"  Scores: Quest {match.Score:P0}, Zweiter {match.RunnerUpScore:P0}, " +
            $"Schritt {match.StepScore:P0}; Grund: " +
            $"{match.RejectionReason}");
    }

    if (match.Quest == null || match.Step == null)
        rejected++;
}

Console.WriteLine($"Unsichere/abgelehnte Bilder: {rejected}/{imagePaths.Count}");
Console.WriteLine($"Quest erkannt: {questMatches}/{imagePaths.Count}");
Console.WriteLine($"Quest und Schritt erkannt: {stepMatches}/{imagePaths.Count}");

foreach (var entry in rejectionCounts.OrderByDescending(entry => entry.Value))
    Console.WriteLine($"  {entry.Key}: {entry.Value}");

return 0;
