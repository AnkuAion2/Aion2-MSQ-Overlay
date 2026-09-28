# Aion Speedrun Overlay

Aion Speedrun Overlay is an experimental Windows overlay that reads the visible
English AION2 quest tracker with local OCR and shows conservative route notes.
It is designed to prefer a missing or delayed note over displaying the wrong
note.

This project is an independent community tool. It is not affiliated with,
endorsed by, or supported by NCSoft. AION and AION2 are trademarks of their
respective owner. Users remain responsible for complying with the rules that
apply to their account and region.

## Features

- Reads the visible AION2 quest tracker using local OCR.
- Shows the currently confirmed quest and objective with compact route notes.
- Supports a separate experimental Elyos Story catalog for levels 1-45; Asmodian remains the default.
- Lets users edit their own notes and attach up to three images per quest step.
- Keeps custom notes, attached images, and appearance settings locally between updates.
- Provides a session history of confirmed steps, with an explicit Live button to
  resume following the latest confirmed step.
- Offers adjustable overlay opacity, size, and colors.
- Uses conservative confirmation rules: no new note is preferable to a wrong
  note.
- Does not read game memory, modify game files, automate input, send telemetry,
  or upload captured screen content.
- Runs as a compact, movable Windows overlay with a taskbar icon and close button.

## Download and run

1. Open the GitHub release and download the attached
   `AionSpeedrunOverlay-<version>-win-x64.zip`. Do not use GitHub's automatic
   "Source code" archives as the application download.
2. Extract the complete ZIP to a normal folder.
3. Start `AionSpeedrunOverlay.exe`. Administrator rights are not required.

Use the pencil button to edit the note for the currently confirmed quest step.
Images can be imported from PNG/JPEG files or pasted using the image button in
the note editor. Save commits the note and its images; Cancel discards pending
changes. Close and reopen the overlay normally; saved custom notes and images
remain available because they are stored separately from the application.

Open settings and choose **Edit all quest notes** to search every quest and step,
edit notes without playing through the story, or restore their default text.
Save commits each edit; existing image attachments are preserved. Steps that
already share a note key are clearly marked because their custom note is shared.

Use the arrow buttons to browse confirmed steps from the current session.
Browsing history or editing a note pauses automatic following. Select Live to
return to the latest confirmed step. Use the settings button to customize the
appearance; changes are saved automatically.

The release is self-contained for 64-bit Windows, so a separate .NET
installation is not required. Most runtime components are bundled into the
single `AionSpeedrunOverlay.exe`; the few adjacent OCR files and folders must
remain beside it. The executable is currently unsigned, which can cause a
Windows SmartScreen warning.

The OCR is currently intended for the English client, the primary monitor, a
16:9 resolution, 100% Windows display scaling, and 100% AION UI scaling. Other
configurations have not been validated comprehensively.

## Quest faction

Choose **Asmodian** or **Elyos · Experimental** in settings. The overlay saves
the selection, waits for the current OCR pass to finish, and restarts
automatically. Selecting the active faction does not restart it. Save or cancel
an open note edit before switching. If automatic restart fails, close and
reopen the overlay manually. Appearance settings still update immediately.

Asmodian remains the default. Its catalog, recognition rules, and existing
notes are unchanged. Elyos loads a separate local catalog containing 89 Story
quests and 269 steps for levels 1-45. It does not load the Asmodian instance
contexts. Elyos notes start empty: use the pencil or **Edit all Elyos quest
notes** to add your own. Elyos note keys are separate from Asmodian keys.

Elyos support is experimental. Catalog texts were imported from English
[Aion2.app detail pages](https://aion2.app/db/quests?race=Light&type=Hero),
but have not been validated against live game screenshots. Matching continues
to use the existing conservative rules; some steps may not be recognized.
There is no runtime connection to the source website.

The optional maintainer script `scripts/import-elyos-quests.py` downloads the
English quest data using Python's standard library and caches source pages
under `artifacts/elyos-import`. It is not part of application startup.

By default, it writes `artifacts/elyos-import/elyos-candidate.json` and a
matching `.provenance.json` file for review. It refuses destinations inside
the application's `Data` directory and refuses to overwrite existing output
files. Use `--output artifacts/elyos-import/elyos-candidate-v2.json` for another
candidate.

An import candidate is raw source data: it does not include our added OCR
variants or local catalog corrections. Compare it with the maintained catalog
and merge only reviewed changes, preserving published quest/step IDs, aliases,
and notes. Do not replace the maintained catalog with the candidate wholesale.
Source attribution does not grant a redistribution license; see
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Recognition behavior

- Quest OCR runs serially; two recognition operations are never intentionally
  run at the same time.
- Stable recognition waits about 350 ms after one OCR pass finishes.
- A genuinely different quest or step that is still awaiting confirmation is
  checked again after about 125 ms.
- Existing matching thresholds and confirmation counts remain conservative.
- A short quest can be skipped if it is not visible for enough observations.
  The overlay does not fill gaps from route order.

## Local data

Local data is stored outside the application folder:

| File under `%LOCALAPPDATA%\AionSpeedrunOverlay` | Contents |
| --- | --- |
| `notes-overrides.json` | Custom note text and embedded image attachments |
| `appearance.json` | Overlay opacity, scale, width, and colors |
| `quest-faction.json` | Selected faction for the next application launch |

Confirmed-step history is kept in memory for the current session only. See
[PRIVACY.md](PRIVACY.md) for details about screen reading and image storage.

Replacing or deleting the extracted application folder does not delete these
files. To remove all local data, close the overlay and delete:

```text
%LOCALAPPDATA%\AionSpeedrunOverlay
```

Older beta versions may have left `personal-best.json` or a `QuestCaptures`
folder in the same local-data folder. This version no longer creates or updates
those files; they can be deleted after the overlay is closed.

## Known limitations

- This is a beta release, not a guarantee that every quest will be recognized.
- OCR depends on the visible quest tracker and supported display/UI scaling.
- Fail-closed recognition can deliberately leave the previous safe note visible
  or show no new note.
- The application has no automatic updater. Download and extract newer releases
  manually; local notes remain in `%LOCALAPPDATA%`.

## Build from source

The source tree includes the Windows WPF application and its OCR regression
runner. Building requires Windows and the .NET 10 SDK, which is different from
running a self-contained release. Run these commands from the repository root:

```powershell
dotnet build AionSpeedrunOverlay.slnx --configuration Release
dotnet run --project AionSpeedrunOverlay/AionSpeedrunOverlay.csproj --configuration Release --no-build
```

The build restores the declared NuGet dependencies. Keep the included `Data` and
`tessdata` files available; the project copies them to the application output.
GitHub's automatic source archives contain source files, not a ready-to-run app.

To create the self-contained Windows x64 release package:

```powershell
.\build-release.ps1
```

The package and its checksum are written to `artifacts`. The script replaces
any existing package output with the same version.

## Optional regression checks

The existing runner checks matching rules, confirmation behavior, saved OCR
crops, and note storage. Run it from the repository root after a Release build:

```powershell
dotnet run --project AionSpeedrunOverlay.QuestSmoke/AionSpeedrunOverlay.QuestSmoke.csproj --configuration Release --no-build -- --summary AionSpeedrunOverlay.QuestSmoke/TestData
```

These are offline regression checks, not a live-game or visual acceptance test.
The runner uses temporary files for its note-storage checks.

## Publishing the source

Use the source files selected by `.gitignore`, not a ZIP of the entire working
folder. Generated builds, local verification artifacts, backups, and personal
settings do not belong in a source upload. Review the selected files before
committing; ignore rules do not remove files that are already tracked.

The quest catalog names Aion2t in its source metadata, and the regression crops
contain game imagery. That source label does not by itself establish Aion2t's
rights in the underlying game data or a requirement to obtain its permission.
The applicable rights and terms for this snapshot have not been established;
this review confirms neither permission nor a prohibition on publication.
See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for the recorded provenance
and the limits of the review. The project MIT license does not grant rights in
third-party content.

## Reporting problems

For problems with a release, open a GitHub issue and include the overlay
version, Windows resolution and display scaling, AION display mode, UI scaling,
client language, and the affected quest/objective. Do not publish screenshots
containing personal information unless you have reviewed them first.

## Licenses

The project license is provided in `LICENSE`. Third-party components and their
licenses are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
