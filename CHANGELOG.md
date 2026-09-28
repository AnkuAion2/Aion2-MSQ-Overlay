# Changelog

## 0.1.0-beta.8

- Added a searchable quest-note library with text editing and default-text
  restore while preserving attached images.
- Added experimental Elyos support: 89 Story quests and 269 steps for levels
  1-45, empty default notes, and separate note keys.
- Added 19 Elyos-only text-recognition variants. These were checked with
  synthetic text cases, not validated as an in-game OCR accuracy improvement.
- Added faction selection with automatic restart after the current OCR pass
  finishes. Open note edits must be saved or cancelled first.
- Updated settings and quest-library styling, including custom scrollbars.
- Added taskbar visibility and the penguin app icon and settings logo.
- Removed unused mapping, legacy speedrun components and dependencies.
- Preserved the existing Asmodian catalog and shared recognition rules during
  the Elyos extension.
- Made the maintainer import write a separate review candidate, refusing
  application-data destinations and existing output files.

## 0.1.0-beta.7

- Removed the start/pause button, elapsed-time display, and personal-best
  display.
- Removed active timer tracking and automatic start-screen detection.
- Removed the manual screenshot button and its active capture path.
- Kept the quest/step display, route notes, note editor, close button, and Quest
  OCR behavior unchanged.

## 0.1.0-beta.6

- Added strictly serial adaptive Quest OCR scheduling.
- Kept the normal post-recognition delay at about 350 ms.
- Added a temporary 125 ms delay while a different quest or step is awaiting
  the existing confirmation policy.
- Preserved matching thresholds, confirmation counts, fail-closed behavior,
  level recovery, handoff handling, and note selection.
- Updated regression expectations for catalog objectives that are now explicit
  steps of the currently visible quest instead of inferred next-quest handoffs.
- Added public release packaging, privacy documentation, and release hygiene.
