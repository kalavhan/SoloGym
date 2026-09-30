# Framed pixel content panel

Component 02 provides a charcoal-and-brass frame for live Unity UI content. It reuses the accepted primary button in a separate landscape fixture. The frame is one AutoSprite object; text, counters and controls remain native uGUI children.

## Component contract

Create a [`PixelContentPanel`](../../app/Assets/SoloGym/Scripts/UI/Pixel/PixelContentPanel.cs) beneath an existing UI transform and parent content to its `Content` property:

```csharp
var panel = PixelContentPanel.Create(parent);
((RectTransform)panel.transform).sizeDelta = new Vector2(480, 360);
var action = PixelPrimaryButton.Create(panel.Content, localizedLabel, onClick);
// The caller lays out the action and other children inside panel.Content.
```

- `Background` exposes the nine-sliced frame Image. It is decorative and does not intercept pointer events.
- `Content` is a stretched RectTransform with a RectMask2D. Children outside it are clipped, including pointer hit testing. Keep child focus outlines inside the content area too.
- `SetPadding(left, right, top, bottom)` accepts finite logical-unit insets of at least 32, preserving the authored corner region. Default insets are 40. This does not resize the parent or arrange its children.
- A LayoutElement advertises a 240×144 minimum and 480×360 preferred size. Manual layouts must honor the minimum and provide enough room for their content/padding; a mask is not a text-fitting solution.

The panel does not impose a title, scroll behavior, data model, focus state or modal behavior. Callers supply live headings and arrange their content using native layout controls. A parent CanvasGroup can gate contained controls. Modal click-blocking/backdrop behavior belongs to a later dialog component.

## Artwork

[Source manifest](../../assets/sprites/autosprite/content-panel-r1/manifest.json) records AutoSprite asset `cmuo43v66000511kc7nalx87c`, the prompt, hashes and two credits: one generation plus background removal. Original and transparent PNGs are preserved; the runtime image is byte-identical to the transparent export. No character generation was performed.

[`PixelPanelTextureImporter`](../../app/Assets/SoloGym/Editor/PixelPanelTextureImporter.cs) imports only this asset with a 960×960 metadata crop at (32,32), a 128-pixel border on each side, 400 pixels per unit, point filtering, no mipmaps and no compression. At the default Canvas reference scale, corner regions occupy 32 logical units. Nine-slicing extends the quiet center and straight edges without scaling those corner regions. The flat slate material does not require a repeated tile. Do not switch this non-seamless source to tiled rendering.

The provider image is a pixel-art source with generated texture detail; point filtering is not a guarantee of a uniform native pixel grid at every device scale. Device sizing and final visual approval remain part of integration review.

## Review player

[`PixelPanelGallery`](../../app/Assets/SoloGym/Scripts/UI/Pixel/PixelPanelGallery.cs) shows tall, wide and minimum-size frames, English/Spanish text and a contained primary button. The lower controls change width, text length and locale. The action counter is fictional, memory-only data. The fixture supports landscape windows from 854×480, with safe-area simulation and a minimum 0.75 Canvas scale.

The existing button and new panel build entries share [`PixelReviewBuild`](../../app/Assets/SoloGym/Editor/PixelReviewBuild.cs). It builds one dedicated fixture scene and restores temporary player settings and the previous editor scene setup. The normal app scene registration is unchanged.

From the repository root, build with Unity 6000.3.24f1 and Linux Standalone support:

```bash
mkdir -p artifacts/local
"/home/josue/Unity/Hub/Editor/6000.3.24f1/Editor/Unity" \
  -batchmode -nographics -quit -projectPath "$PWD/app" \
  -executeMethod SoloGym.Editor.PixelPanelBuild.BuildLinux \
  -logFile "$PWD/artifacts/local/panel-build.log"
```

Open the interactive review:

```bash
app/Builds/FantasyPanel/SoloGymPanel.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es
```

For automated verification with a graphics display:

```bash
XDG_CONFIG_HOME="$PWD/artifacts/local/panel-prefs" \
xvfb-run -a -s '-screen 0 1920x1080x24' \
  app/Builds/FantasyPanel/SoloGymPanel.x86_64 \
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 \
  -sologym-locale es -sologym-safe-inset 0 -sologym-smoke \
  -sologym-capture "$PWD/artifacts/visual/ContentPanel/1280x720-es.png" \
  -logFile "$PWD/artifacts/local/panel-player.log"
```

`-sologym-smoke` writes a PNG and sibling JSON report, then exits with the verification status. Capture alone keeps the player open; add `-sologym-quit-after-capture` for capture-only automation.

## Verification

Verified 2026-09-30 with Unity 6000.3.24f1 and the Linux review players. Both the panel and existing button build entries succeeded after sharing the builder.

| Capture / report | Locale | Simulated safe inset | Result |
| --- | --- | --- | --- |
| [1280×720](../../artifacts/visual/ContentPanel/1280x720-es.png) / [report](../../artifacts/visual/ContentPanel/1280x720-es.smoke.json) | ES | 0 px | 147/147 passed |
| [1844×853](../../artifacts/visual/ContentPanel/1844x853-en.png) / [report](../../artifacts/visual/ContentPanel/1844x853-en.smoke.json) | EN | 64 px | 147/147 passed |
| [854×480](../../artifacts/visual/ContentPanel/854x480-es.png) / [report](../../artifacts/visual/ContentPanel/854x480-es.smoke.json) | ES | 32 px | 147/147 passed |
| [Existing button regression report](../../artifacts/visual/ContentPanel/button-regression.smoke.json) | ES | 0 px | 118/118 passed |

The panel smoke path inspects rendered mesh geometry before/after width changes, actual UI raycasts and masking/culling. It also checks child pointer/keyboard activation, parent interaction gates, localized text bounds in wide/narrow layouts, content/safe-area containment, and minimum button targets. Source/export hashes match the manifest and the runtime PNG is identical to the transparent export. Existing application scenes, scene registration and project settings have no committed changes.

All three panel captures were visually inspected for preserved corners, readable localized text and absence of clipping/overlap. The initially clipped narrow paragraph was corrected by giving it more vertical space before these final runs. User visual acceptance is still pending. Linux capture pixels do not establish density-independent touch size; Android/iOS touch, safe areas and physical readability still require device testing.
