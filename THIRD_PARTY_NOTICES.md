# Third-party notices

The release contains the following third-party components. Their respective
licenses apply to those components; the Aion Speedrun Overlay project license
does not replace them.

| Component | Version/source | License |
| --- | --- | --- |
| OpenCvSharp4 and OpenCvSharp4.Extensions | 4.13.0.20260627 | Apache-2.0 |
| OpenCvSharp4.runtime.win / OpenCV native runtime | 4.13.0.20260627 | Apache-2.0 |
| Tesseract .NET wrapper | 5.2.0 | Apache-2.0 |
| Tesseract OCR native runtime | bundled by Tesseract 5.2.0 | Apache-2.0 |
| English Tesseract trained data | Tesseract tessdata | Apache-2.0 |
| Leptonica native runtime | bundled by Tesseract 5.2.0 | Leptonica two-clause license |

Project pages:

- OpenCvSharp: https://github.com/shimat/opencvsharp
- OpenCV: https://github.com/opencv/opencv
- Tesseract .NET wrapper: https://github.com/charlesw/tesseract
- Tesseract OCR: https://github.com/tesseract-ocr/tesseract
- Tesseract trained data: https://github.com/tesseract-ocr/tessdata
- Leptonica: https://github.com/DanBloomberg/leptonica

Copies of the applicable license texts are included in the `licenses` folder.
The optional OpenCV FFmpeg video-I/O plugin is removed from the public release
because this application does not use video capture or video writing.


## Game data and imagery

The MIT license in `LICENSE` covers the project's original code. No ownership
or MIT licensing of third-party game content is claimed here.

### Recorded provenance

| Material in this source tree | Recorded provenance and status |
| --- | --- |
| `AionSpeedrunOverlay/Data/aion2_asmodian_mythic_quests_lvl1-45.json` | The supplied file names Aion2t quest-detail pages as its source and records a game-client date of 2026-08-19. The source label was already present when the file was first inspected for integration on 2026-08-24. The file's original creation process and the accuracy of that label have not been independently established. |
| AionSpeedrunOverlay/Data/aion2_elyos_story_quests_lvl1-45.json | Imported from public English Elyos Story detail pages on Aion2.app on 2026-09-28 (local date). Contains quest names, levels, objectives, and steps, with per-quest source URLs; no story prose, rewards, images, or route notes were imported. No live-client validation or separate redistribution grant is claimed. |
| `AionSpeedrunOverlay/Data/level-less-contexts.json` and `AionSpeedrunOverlay/Data/quests.json` | Additional game names/objectives and route configuration. Their presence does not establish ownership or licensing of the underlying game content. |
| `AionSpeedrunOverlay.QuestSmoke/TestData/*.png` | 62 cropped game screenshots used as OCR regression fixtures. Individual capture-author credits and specific redistribution grants are not recorded in this tree. |
| `TitleTemplateBase64` in `AionSpeedrunOverlay/Board/DaevanionBoardDetectionService.cs` | An embedded game-UI template in the disabled board detector. No separate permission record was found in this tree. |

The overlay reads the local quest catalog. It does not connect to Aion2t or
Aion2.app to retrieve quest data at runtime.

### Rights and review limits

Reviewed on 2026-09-27:

- [Aion2t](https://aion2t.com/) describes itself as independent of NCSoft and
  states that its game data comes via the official NCSOFT Aion 2 API. Its
  historical [quest-database address](https://aion2t.com/db/quests) now redirects
  to [Aion2.app](https://aion2.app/db/quests). These are the sites' statements,
  not independent verification of how this project's catalog was created.
- Section 3 of [Aion2t's terms](https://aion2t.com/terms) and
  [Aion2.app's terms](https://aion2.app/terms) states restrictions on copying
  and redistributing site content without permission. Section 4 attributes
  game assets, names, and imagery to NCSoft. Those statements alone do not
  establish Aion2t's ownership of the underlying quest data or determine how
  the terms apply to this particular catalog.
- Rights in underlying content and possible rights in a compiled database are
  separate questions. For example, German law provides for database protection
  under specified conditions in [section 87a UrhG](https://www.gesetze-im-internet.de/urhg/__87a.html)
  and addresses reuse in [section 87b UrhG](https://www.gesetze-im-internet.de/urhg/__87b.html).
  This review has not established whether such protection applies to the
  relevant collection or whether this catalog implicates those rights.
- The historical NCSoft API terms governing the original acquisition and reuse
  of these data have not been verified. No conclusion about redistribution
  follows merely from the fact that data were once accessible through an API.


### Application artwork

`AionSpeedrunOverlay/Assets/aion-penguin.png` was generated with OpenAI image
generation on 2026-09-28 at the project owner's request. It combines a penguin,
fantasy wings, and AION Overlay lettering. The PNG retains its C2PA generation
provenance. `aion-penguin.ico` contains resized versions of the same artwork.
This is custom, unofficial application artwork, not an official NCSoft logo
or evidence of NCSoft endorsement. No rights in the AION name or third-party
game branding are granted by the project's code license.