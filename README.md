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
- Lets users edit their own notes and keeps them locally between updates.
- Uses conservative confirmation rules: no new note is preferable to a wrong
  note.
- Does not read game memory, modify game files, automate input, send telemetry,
  or upload captured screen content.
- Runs as a compact, movable Windows overlay with a close button.

## Download and run

1. Open the GitHub release and download the attached
   `AionSpeedrunOverlay-<version>-win-x64.zip`. Do not use GitHub's automatic
   "Source code" archives as the application download.
2. Extract the complete ZIP to a normal folder.
3. Start `AionSpeedrunOverlay.exe`. Administrator rights are not required.

Use the pencil button to edit the note for the currently confirmed quest step.
Close and reopen the overlay normally; custom notes remain available because
they are stored separately from the extracted application.

The release is self-contained for 64-bit Windows, so a separate .NET
installation is not required. The executable is currently unsigned, which can
cause a Windows SmartScreen warning.

The OCR is currently intended for the English client, the primary monitor, a
16:9 resolution, 100% Windows display scaling, and 100% AION UI scaling. Other
configurations have not been validated comprehensively.

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

Custom notes are stored outside the application folder:

```text
%LOCALAPPDATA%\AionSpeedrunOverlay\notes-overrides.json
```

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

## Source availability

This repository distributes pre-built Windows releases. The application source
code is not published in this repository. GitHub's automatically generated
`Source code` archives contain only the public repository documents and are not
application downloads.

For problems with a release, open a GitHub issue and include the overlay
version, Windows resolution and display scaling, AION display mode, UI scaling,
client language, and the affected quest/objective. Do not publish screenshots
containing personal information unless you have reviewed them first.

## Licenses

The project license is provided in `LICENSE`. Third-party components and their
licenses are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
