# Privacy and local data

Aion Speedrun Overlay performs OCR locally. The current source contains no
telemetry, analytics, automatic upload, or network-based capture submission.

## Screen reading

While active, the application periodically reads the predefined screen region
needed for quest OCR. Recognition is performed on the local computer. Normal
OCR frames are not saved to disk.

## Local files

Custom notes are stored in:

```text
%LOCALAPPDATA%\AionSpeedrunOverlay\notes-overrides.json
```

Older beta versions may have created the following legacy data:

```text
%LOCALAPPDATA%\AionSpeedrunOverlay\personal-best.json
%LOCALAPPDATA%\AionSpeedrunOverlay\QuestCaptures
```

The current version does not create or update this legacy timer/capture data.
Application updates do not remove existing files. To erase all data created by
the overlay, close it and delete `%LOCALAPPDATA%\AionSpeedrunOverlay`.
