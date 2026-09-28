# Privacy and local data

Aion Speedrun Overlay performs OCR locally. The application contains no
telemetry, analytics, automatic upload, or network-based capture submission.

## Screen reading

While active, the application periodically reads the predefined screen region
on the primary monitor needed for quest OCR. It reads the visible pixels in that
region, which may belong to another application when the game is not visible.
Recognition is performed on the local computer. Normal OCR frames are not saved
to disk.

## Notes and attached images

Custom note text and image attachments are stored together in:

```text
%LOCALAPPDATA%\AionSpeedrunOverlay\notes-overrides.json
```

Images are read only when the user chooses a file or presses the paste-image
button in the note editor. The clipboard is not monitored automatically.
Imported images are converted to PNG, resized when needed, and embedded as
Base64 data in the JSON file. Saving a note commits its text and attachments;
Cancel discards pending edits. Source image files are not modified.

This local file is not encrypted. Base64 is an encoding, not encryption. It can
contain the full visible content of images the user attached. Review it before
sharing or submitting it with a bug report. Resetting a note to its default
in the overlay editor removes its custom text and attachments from the current
saved document. The quest library's **Restore default text** action preserves images.

## Appearance settings and history

Overlay opacity, scale, width, and colors are saved in:

```text
%LOCALAPPDATA%\AionSpeedrunOverlay\appearance.json
```

The faction selected for the next launch is saved locally in
%LOCALAPPDATA%\AionSpeedrunOverlay\quest-faction.json.
Asmodian and Elyos notes share the notes file but use distinct keys.

The history of confirmed quest steps is held in memory for the current session
and is not saved as a history log. Temporary `.tmp` files may exist while local
settings are being written.

## Legacy files and removal

Older beta versions may have created:

```text
%LOCALAPPDATA%\AionSpeedrunOverlay\personal-best.json
%LOCALAPPDATA%\AionSpeedrunOverlay\QuestCaptures
```

The current version does not create or update this legacy timer/capture data.
Replacing or deleting the application folder does not delete local data.
To erase the overlay's saved local data, close it and delete
`%LOCALAPPDATA%\AionSpeedrunOverlay`. This does not remove original image files
elsewhere on the computer or copies the user has shared or backed up.
