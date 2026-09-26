# Layered UI and customizable avatars

Recorded 2026-09-25 following the user's request to separate backgrounds, containers and controls, and to compose a tintable character from interchangeable parts. This is the implementation plan; the existing windows have not yet been migrated and the avatar assets have not been produced. WIN-010's visual target still awaits approval.

## Current implementation and correction

The app already uses Unity 6000.3.24f1 and uGUI 2.0.0. Buttons, inputs, state, validation and most labels are code. However, `HomeScreen`, `WelcomeScreen`, `OnboardingScreen` and `PrivateProfileScreen` use full-screen reference artwork, cropped panel sections and text-removal plates for much of their presentation. That couples layout and styling to generated images. Some windows also load different source artwork per locale. The Home avatar is a static illustration.

Retain the approved references and visual style, but replace those runtime presentation dependencies. A reference image specifies the desired appearance; it must not become the window's control surface. Keep the existing state controllers and authentication behavior while migrating rendering. This change does not add storage, complete consent policy or grant access through the production onboarding gate.

## UI composition

| Layer | Implementation | Changes without new art |
| --- | --- | --- |
| Environment | Background-only texture, optional separate foreground/effects | Crop, placement, dimming and motion settings |
| Containers | Shared `SystemPanel` geometry: angular corners, translucent fill, borders and glow; small reusable decorative textures only where needed | Width, height, theme colors, padding and border style |
| Controls | Reusable buttons, inputs, toggles, choice groups, tabs, progress bars and scrolling lists | Label, state, value, item count, layout and interaction |
| Content | Real localized text and bound data; separate icons and provider marks | EN/ES, errors, measurements, rewards and account state |
| Character | Independent avatar renderer fed by an appearance recipe | Supported body, skin, face, hair and equipped items |

Use the existing uGUI stack. Panels can use custom `MaskableGraphic` geometry; a detailed ornamental frame can use a small sliced sprite that preserves its corners while the center stretches. Unity documents native text and sliced/filled images in [uGUI visual components](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/UIVisualComponents.html). This supports reusable artwork without rendering a complete image for every button.

Introduce a shared `SystemTheme` ScriptableObject for palette, typography, spacing, borders, glow and interaction states. Package controls as reusable components/prefabs. A screen assembles these controls and binds its state controller; it does not duplicate their drawing logic. Component names here are proposed, not existing APIs.

Use one background and icon set for both languages. Layout must account for safe areas, different aspect ratios and text length. Reference pixel coordinates guide the approved baseline; anchored layouts, wrapping and scrolling handle other sizes. Do not distort glyphs to force every translation into a fixed image rectangle. Keep textures decorative and non-interactive; actual controls own touch targets and focus.

AI artwork remains useful for backgrounds, concepts, illustrations and authored character assets. Changing a label, panel dimension, accent color or input state must not need image generation. Do not add new full-window source textures, painted labels or clean-plate patches to the replacement UI.

## Migration before the next window

| Step | Scope | Completion evidence |
| --- | --- | --- |
| UI-01 | Shared theme, panel and control kit; identify/extract usable background-only art from retained sources | One component gallery shows resize, color and text changes without regenerating assets; this is an engineering preview, not a new visual target |
| UI-02 | Migrate WIN-009 Private Fitness Profile as the first complete window | EN/ES baseline comparisons; live measurement entry, unit conversion, notices and Back keep their current behavior |
| UI-03 | Migrate WIN-006/007, then WIN-002 | Reuse the kit; preserve drafts, consent choices and authentication behavior; inspect relevant whole-screen states |
| UI-04 | Migrate WIN-001 | Separate background, panels and data; retain the accepted static portrait as an explicitly temporary illustration until the modular avatar is approved |
| UI-05 | Implement WIN-010 using the shared kit after its target is approved | Whole-window comparison and a focused flow check, with no flattened-screen runtime dependency |

If existing art does not contain the obscured environment, create or repair the background once as a separate asset and present any visible design change. Do not reconstruct backgrounds on each UI edit. Preserve the retained target PNGs and hashes; do not replace them with the implementation capture.

For each migrated window, assemble the complete screen first, then check the retained baseline and a small set of meaningful interactions. Include EN/ES and a narrow/tall viewport to catch layout coupling. Record visible differences rather than claiming mathematical pixel identity. No per-button render cycle and no APK in this iteration. Existing implementation reports remain historical evidence until new captures exist.

## Avatar: tintable base with interchangeable parts

Use a neutral, shaded base with a skin-only mask. A palette/material changes exposed skin while preserving shading. Hair, eyes, teeth, clothes and equipment have separate color regions. A whole-image tint would also recolor those parts and is unsuitable.

Author semantic parts that can share a rig and registration:

- Head and neck, torso and pelvis.
- Left/right upper arms, forearms and hands; thighs, lower legs and feet.
- Eyes, eyebrows and mouth; expression variants where visible.
- Front and back hair, with a shared hairstyle choice and separate color.
- Clothing and equipment attached to their corresponding body parts/sockets.
- Separate ground shadow and optional cosmetic effects.

These are authoring parts, not necessarily one texture or draw call each. Pack compatible pieces into atlases. All pieces need matching scale, pivots, attachment points, canvas registration, direction and animation timing. Equipment declares supported body fits and covered body/hair regions. Helmets choose a fitted hairstyle or deliberately hide covered hair.

Layer order must change with the direction and pose: a far glove can sit behind the torso while the near glove is in front. Do not draw the entire body and then every item on top. Facial details can use a simpler combined face sprite for distant gameplay, provided the same selected appearance is represented.

Store IDs and palette choices, not a generated image per outfit:

```text
AppearanceRecipe
  base / body fit / head
  skin palette
  hair style / hair palette
  eyes / eye palette / eyebrows / mouth
  equipped items by slot
```

System, Equipment, Tower and Gym all consume that recipe. Portrait and world views can use different asset detail levels and poses. Male/female presentation and supported body builds use the same appearance and action conventions; items still need an authored fit for every body variant they support. Body weight and height do not automatically choose the avatar's shape.

## Prove motion before expanding the sprite library

A single front-facing base is enough for a customization preview, but not for isometric walking, turns and boxing. Keep the earlier modular 3D authoring/export approach as a candidate alongside a layered 2D rig. The requested runtime result is interchangeable sprite layers; authoring method and final runtime implementation remain pending the proof. Do not generate independent AI images for every frame or outfit combination.

Start with one base, two skin colors, two hairstyles, one torso item and paired gloves. Show idle, walking and one punch in two directions that expose overlap problems. If that works, extend the proof to male/female presentation, representative body fits and the proposed eight directions before producing the library. This smaller first step does not waive the broader fit/motion requirements in the asset contract.

Unity's optional 2D Animation package provides sprite libraries and resolvers for swapping parts; its skeletal swapping requires matching skeletons. See [Unity Sprite Swap](https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/SpriteSwapIntro.html). The package is not installed in this project, and no rig, masks or modular avatar are implemented yet. Evaluate it during the proof instead of selecting a package or promising compatible art from concept images alone.

The machine-readable proposal is [avatar-contract.json](../../data/art/avatar-contract.json). Base, customization, gear and motion remain separately reviewable batches. The shared UI refactor can proceed without generating the character library.
