# SoloGym asset production and exercise demonstration sources

Updated 2026-09-25. The user accepted the initial manhua System style and requested a retained rendering in every window proposal for matching during implementation. WIN-001 has English and Spanish v2 proposal renders awaiting approval. This document proposes how to produce reusable parts, with separate approval for each stage; proposal images do not approve production assets, select an engine, purchase content, or implement an app window. “Character stripes” is interpreted as character **sprites**.

## Work in two separate asset collections

**Game art** includes environments, avatars, wearable equipment, enemies, combat animations, icons and window frames. It follows the accepted indigo/cyan/violet art direction.

**Exercise demonstrations** teach a real movement. They require correct technique, a match to the exact exercise variant and equipment, English/Spanish explanations, and permission to display the source media. A punch used in tower combat is not automatically an appropriate boxing exercise demonstration. The training catalogue can exist before a media supplier is selected; unresolved demonstration records must remain visibly unresolved.

## Repeat this approval sequence for each window

The window milestone document determines order. Complete the current window before starting the next. Reuse accepted assets by version, and seek approval for changes to them. A stage marked not applicable needs a recorded reason; it is not silently skipped.

| Stage | Produce for review | Approval needed before continuing |
| --- | --- | --- |
| 0. Window brief and rendered proposal | Purpose, entry/exit actions, data fields, teen/adult differences, EN/ES copy, orientation, important states and asset list, plus English and Spanish renders of the principal state. Retain immutable versioned references with fixture/canvas metadata and checksums. Identify reusable accepted assets. | **Stop for approval of the brief and exact rendered target before producing separate assets.** |
| 1. Empty background | Background only: architecture, lighting, floor/horizon and camera framing. No character, text, panels, controls or baked UI. Include crop guides for target phone sizes. | **Stop for background approval.** |
| 2. Environment props | Separate transparent foreground/background props, interaction markers and optional atmospheric effects. Show normal and reduced-motion proposals. | **Stop for prop approval.** |
| 3. Base character or enemy | Clean reference sheet, proportions, front/back/side and intended gameplay views; neutral athletic base clothing; no equipment. Reuse the shared avatar if already accepted. | **Stop for base character approval.** |
| 4. Customization | Skin palette, separate skin masks, hair shapes/colors, body-size and muscle variants; headgear compatibility examples. Show variation on the same poses and lighting. | **Stop for customization approval.** |
| 5. Wearable equipment | Item concept, silhouette, rarity accent, equipped view, slots, attachment points, fit variants, visibility masks and inventory icon. Show more than one build. | **Stop for gear approval.** |
| 6. Motion and sprite proof | Idle/movement/action loops, turn changes, equipment staying attached, foot contact, frame registration, alpha edges and occlusion. Compare an unequipped and equipped version. | **Stop for animation/export approval.** |
| 7. Window components | Frames, separators, controls, icon states, typography and colors as separate reusable components. Keep text editable and outside images. | **Stop for UI component approval.** |
| 8. Complete static window | Composite approved production pieces with real sample data and exact EN/ES copy. Include normal, empty, error, busy and relevant teen/recovery states. Compare the asset composite against the approved Stage 0 proposal at its recorded baseline with overlays and difference images. | **Stop for final window approval. Any changed target requires user approval and a new retained revision; never silently replace the proposal.** |
| 9. Window implementation | Build only that accepted window and its approved interactions when its milestone reaches implementation. Compare actual phone captures against the approved composite at the same canvas, locale, fixture and state; retain overlay/difference evidence. | Accept the working window before the next window milestone. |

Each review should identify the window ID, stage, asset version, exact file and requested decision. Approval applies to that version and stage. A revised background invalidates dependent composites until checked again. Silence is not acceptance. G0 proposal previews are part of the brief; they do not start or approve Stage 1 background production.

Follow the [visual-reference contract](visual-reference-contract.md) for immutable reference storage, baseline matching and responsive reviews. Keep the approved proposal visible throughout production. Its flattened illustration is a composition target, not proof that its character layers, gear or animation already work. English and Spanish use the same appearance, equipment and fixture. Production must retain editable text and separately approved assets; different phone sizes or larger text receive explicit responsive review rather than stretching a screenshot. Stages 1–9 retain their individual stop points even when a complete proposal image already exists.

## Character customization that also works during movement

Use one character appearance record across System, Equipment, Tower and the Interdimensional Gym. An item is an equipped attachment, not a different flattened portrait for every outfit. Body measurements stay in the private fitness profile. Appearance uses chosen visual controls; height and weight alone do not identify body composition or muscularity, and do not determine game power. Male and female presentation options use the same action identifiers and equipment rules. Skin and hair choices are independent of those options.

### Production approaches

The following comparison is a SoloGym engineering proposal. It is not a claim that an image generator or library supplies a finished character system.

| Approach | Strengths for SoloGym | Work and constraints |
| --- | --- | --- |
| Layered 2D art on a skeletal rig | Strong illustrated style; swap clothes/hair in slots; tint designated surfaces; efficient for portraits and limited viewpoints. | Each direction needs drawn parts, suitable deformations and ordering. Large turns, body-size changes and boxing foreshortening require substantial corrective art. A single front-facing cutout cannot supply convincing isometric movement. |
| Modular 3D source character, rendered into 2D sprite layers/sheets | Consistent eight-direction views; one animation source; gear follows a skeleton; controlled lighting and camera; repeatable export of masks, shadows and item passes. | Requires modeling, rigging, gear fitting and cleanup. Exported sprites support only the body variants actually rendered. Layered rendering needs correct depth/occlusion; memory and download sizes grow with directions, clips and fits. |
| Modular 3D source rendered live in the eventual app | Continuous body adjustments and rotations; fewer pre-rendered combinations. Can use a stylized appearance. | Requires a later runtime/rendering decision and phone performance proof. It may not match the illustrated reference without deliberate shader and art work. |

**Recommended source pipeline:** approved AI-assisted concept art → cleaned model/reference sheets → a shared modular 3D authoring rig → a small eight-direction 2D sprite proof. Decide between rendered sprites and live 3D only after that proof measures visual fit, customization coverage and device cost. This keeps a reusable source if the initial sprite route proves too restrictive. Layered 2D remains a reasonable alternative for menu portraits, but those portraits must depict the same selected equipment.

Blender provides modeling, rigging, animation, rendering and scripting; its current developer documentation includes shape-key and weighting tools. These capabilities support the proposed authoring approach, without deciding the app engine. [Blender features](https://www.blender.org/features/), [Blender rigging and shape keys](https://developer.blender.org/docs/release_notes/4.5/animation_rigging/).

### Required attachment and appearance rules

| Concern | Proposed rule |
| --- | --- |
| Skeleton and actions | Maintain versioned skeleton, bind pose, scale and action IDs. Share `idle`, `walk`, `run`, `jab`, `hook`, `uppercut`, `special`, `hit`, `defeat`, `celebrate` and `wave` identifiers; exact clips and timings await combat design. Different fit families may need retargeted/corrected motion while preserving those IDs. |
| Equipment slots | Start with head, torso, legs, hands, feet, back and aura. Boxer gloves/gauntlets are paired equipment with left/right attachments. Reserve additional slots rather than promising all of them at launch. |
| Skin color | Tint only explicitly marked skin material or mask regions. Preserve authored highlights/shadows, and test the entire palette in the scene lighting. Hair, eyes, clothes, teeth and equipment are excluded. Do not tint a whole flattened character image. |
| Hair | Separate front/back hair where needed. A head item declares `show`, `hide`, or a named fitted hair variant. Never depend on a helmet simply covering all hairstyles by chance. |
| Gear fitting | Each item declares supported fit families and skeleton versions. Clothing uses weights and corrective shapes; rigid items use named sockets plus fit-specific transforms. Unsupported fits are rejected during content validation, not stretched arbitrarily at runtime. |
| Body controls | Size and muscle definition are separate controls in the source model. A sprite release must expose a finite approved set of combinations, or clearly map its controls to those supported combinations. Continuous-looking sliders must not imply unrendered combinations exist. |
| Direction and overlap | Use direction- and frame-specific ordering or depth-aware composition. A far arm, glove or cape may be behind the torso while the near part is in front. A global “body, then every item” sequence is insufficient. |
| Hidden surfaces | Equipment declares body/hair coverage masks. A torso item may hide covered base-clothing sections; gloves hide covered hands. Overlapping equipment also needs compatibility rules. Depth/occlusion passes must be produced from identical camera/pose/fit settings. |
| Rarity | Basic, Rare, Ultra Rare and Premium change cosmetic material/detail/effects. Rarity is an item attribute, not a skeleton or body type. Premium does not grant fitness XP or combat advantage. |
| Same character everywhere | A stable appearance recipe stores body, skin, hair and item IDs. Menu portraits and game sprites are derived from this recipe. Do not infer appearance from a gameplay stat. |

A conventional 2D rig supports slots, selectable attachments, tinting and keyed draw order; this is why the alternative can support equipment, although direction-specific art remains necessary. Spine is an example of that capability, not a selected tool or license. [Spine slots](https://eu.esotericsoftware.com/spine-slots), [Spine runtime skins](https://eu.esotericsoftware.com/spine-runtime-skins).

### Sprite/export proof and acceptance

First prove one male and one female base, a small/large build pair, a light/dark skin pair, two substantially different hairstyles, one helmet, gloves, torso clothing and footwear. Include idle, walking, turning and at least one punch. Reuse the same neutral lighting, camera and source rig. Check the largest hair, widest torso and longest glove at maximum motion to catch clipping.

For the 2D route, export synchronized base, equipment, shadow and any required depth/mask passes. Store direction, frame index, frame duration, clip ID, loop rule, canvas size, crop rectangle, trim offset, pivot, ground/foot anchor, fit family, skeleton version and source hash. Transparent PNG masters should have consistent alpha treatment, no colored fringe and atlas padding appropriate to the selected importer. A cropped sprite must preserve its original registration metadata so the character does not wobble. Ground shadows remain separate from auras and scenery.

Keep eight camera directions as the initial proof target, not a final asset-count commitment. Do not mirror asymmetric gloves, logos or hairstyles unless explicitly authored for mirroring. Aim direction, actual world movement, collisions and hit events belong to gameplay data, not pixels. A punch's gameplay impact marker must be synchronized with its authored action, rather than guessed from arbitrary generated frames.

Layering avoids baking every full outfit, but does not remove the cost of each item/fit/action/direction. For illustration, 2 presentation bases × 4 body presets × 8 directions × 8 clips × 12 frames already equals **6,144 base frames** before gear. Color masks can avoid separate frame sets for each skin color. Hair geometry and body shapes still need appropriate assets. Track estimated atlas memory/download size before expanding the catalogue.

The initial data contract is [avatar-contract.json](../../data/art/avatar-contract.json). Its records are placeholders with no rendered or licensed source assets; all production and fit proof states remain pending.

## Online generation and an eventual local workflow

Built-in image generation is already available and produced the accepted style concepts. It is suitable for proposed backgrounds, reference sheets, isolated prop concepts, icons and targeted revisions. A generated sheet must be inspected and cleaned; repeated prompts do not guarantee identical anatomy, equipment placement or animation timing.

An eventual local ComfyUI workflow could preserve the selected model/checkpoint, reference images, masks, conditioning inputs, prompts, seeds and workflow version. This helps repeat production and batch variants. It still requires a controlled rig/render pipeline for animation consistency. ComfyUI supports reusable node workflows, JSON workflow storage, local execution and model components; its own code license does not determine the licenses of checkpoints, adapters, custom nodes or outputs. [Official ComfyUI repository](https://github.com/Comfy-Org/ComfyUI).

Before choosing a local pipeline, use the separate machine inventory to confirm installed tools, GPU/VRAM, disk space and model files. Do not assume that a tool's presence proves the model's commercial or training rights. Record each checkpoint, LoRA, adapter and node version and its own license. For example, Stability's current licensing distinguishes covered Core Models and commercial revenue thresholds; this does not license every model called “Stable Diffusion.” [Stability AI licensing](https://stability.ai/license).

Start with references and controlled inputs. Train a SoloGym style LoRA only if testing shows a real benefit and the training collection is approved original/owned or otherwise explicitly licensed for that use. Keep the training permission separate from permission to display media. Do not train on vendor exercise videos, Mixamo output, unrelated manhua panels or downloaded “free” assets without the necessary rights. Do not install tools, download weights, buy subscriptions, train models or generate production art during this planning/data task.

## Exercise demonstration sources checked on 2026-09-25

These are five distinct options, including two different ExerciseDB offerings. An open-source API repository does not establish permission to reuse the hosted media. No supplier is selected and no third-party assets have been imported.

| Source | Verified offer and important license conditions | SoloGym assessment |
| --- | --- | --- |
| **wger** | Project code is AGPL-3.0-or-later. Current README says exercise/ingredient data uses Creative Commons licenses per entry; documentation separately identifies initial data as CC BY-SA 3.0 and notes image sources. Inspect each exercise, image and video license/author/source. CC BY-SA permits commercial reuse with attribution and appropriate ShareAlike obligations for adaptations. [Project license breakdown](https://github.com/wger-project/wger/blob/master/README.md), [documentation](https://wger.readthedocs.io/en/stable/), [CC BY-SA 3.0 terms](https://creativecommons.org/licenses/by-sa/3.0/). | Good first candidate for a small audited, reusable demonstration set. Coverage and technique quality need inspection. Keep licensed data/media separate from our original catalogue and proprietary assets; do not assume all entries have one license. |
| **MuscleWiki API** | Paid plans include commercial fitness-app use. Provider documentation offers exercise videos and EN/ES text; non-English access depends on plan. Terms allow text caching up to 30 days, images up to 24 hours, videos only transient buffering. No offline media or rehosting; preserve video branding and required legal credit. AI training needs written permission. Actual response headers can be stricter than the maximum cache allowance. [FAQ](https://api.musclewiki.com/faq), [controlling API terms, updated August 12, 2026](https://api.musclewiki.com/api-terms). | Strong candidate if streamed demonstrations are acceptable. Design an honest offline/unavailable state and never promise downloaded videos. Map provider IDs to our IDs rather than copying its dataset into the repository. |
| **ExerciseDB API / AscendAPI on RapidAPI** | Its own documentation links the RapidAPI product. Subscription grants limited rights during the subscription. Terms prohibit persistent storage of text/media and allow only temporary operational caching up to one hour; bulk downloading/scraping is prohibited and use rights stop when subscription ends. [Provider documentation](https://exercisedb.notion.site/Table-of-Contents-1a6983b728ca80b69e85c5c74133220e), [provider API terms](https://exercisedb.notion.site/ExerciseDB-API-Terms-of-Use-226983b728ca8090bf7be79564e4b356). | Consider only for a deliberately online integration. These API terms do not support building our permanent local catalogue by importing responses. Confirm the exact contracted product rather than assuming every “ExerciseDB” listing shares terms. |
| **ExerciseDB.io / EDB Exercise Intelligence purchased package** | Terms dated August 23, 2026 grant a purchased package a nonexclusive, nontransferable license for a licensed mobile/web/desktop product. Includes JSON and exercise GIFs, with resolution/metadata depending on package. GIFs may be displayed only together with exercise data. Raw-file redistribution, sublicensing, standalone libraries and competing APIs/content packages are forbidden. Ownership stays with the supplier; no AI-training grant is stated. [Package terms](https://exercisedb.io/terms). | Potential alternative when a purchased file package fits the product. Confirm app bundling, CDN delivery, backups, translation, attribution and exact product scope before purchase; do not infer missing permissions from a download button. |
| **Adobe Mixamo** | Adobe permits royalty-free characters/animations in commercial games and other finished projects; auto-rigging/library support is for bipedal humanoids with proportion/mesh restrictions. Its additional terms prohibit using service content/output to create, train, test or improve AI/ML systems. [Adobe FAQ](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html), [Mixamo additional terms](https://wwwimages2.adobe.com/content/dam/cc/en/legal/servicetou/Mixamo-Addl-Terms-en_US-20210623.pdf). | Useful candidate for game locomotion/emotes or reference motion after rig testing. It is not an exercise-technique certification. Keep raw vendor assets out of a public repository pending redistribution review; do not feed its motion or renders into model training. |

**Proposed selection order:** first audit a few wger records for the exact launch exercises. Compare their quality and rights with a MuscleWiki streaming demonstration in a later supplier evaluation. Evaluate a purchased package only if offline delivery becomes a requirement. Keep the game character motion decision separate from exercise demonstrations. There is no need to choose a provider to author the original exercise taxonomy and generation rules now.

## Media intake and provenance contract

Every demonstration or art asset needs a stable internal ID. Store source permission separately from clinical/editorial review. A link to a supplier page is not itself a cleared license.

| Field group | Required information |
| --- | --- |
| Identity | Internal asset ID; exercise or action ID; variant; provider name; provider record ID; source page; original author/rightsholder. |
| Rights | License name/version and URL; reviewed date; applicable purchase/subscription agreement reference; commercial display, modification, redistribution, offline storage and AI-training permissions, each `allowed`, `prohibited` or `unverified`; required attribution; expiry/cache limits. |
| Editorial match | Exercise/equipment/load-position match; camera angle; visible movement range; correct setup; instruction/cue language; captions; reviewer and review date; teen suitability where applicable. |
| Technical | Format, dimensions, duration, frame rate, loop points, audio/caption presence, thumbnail, delivery mode, source checksum where local storage is permitted, provider availability state. |
| Changes | Original versus adapted; change history; translation/adaptation author; derived asset IDs; any ShareAlike requirements. |
| Approval | `proposed`, `rights_verified`, `technique_reviewed`, `visual_approved`, `export_verified`, `released`, or `retired`; record actor, date, version and evidence for each completed check. |

A paid API's transient response belongs in its permitted cache, not the permanent source catalogue. Our catalogue stores original exercise IDs, original reviewed instructions and source mappings permitted by the agreement. Do not store signed streaming URLs or API credentials in data files, logs or committed assets. If rights are unknown, use a named placeholder with no media URL. If a provider removes a demonstration, the window should still offer approved text cues and an unavailable-media message; the generator must not substitute a mechanically different exercise to hide the missing video.
