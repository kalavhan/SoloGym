# Repeatable character production for SoloGym

Research date: 2026-09-26. Current direction: the user approved investigating a repeatable source pipeline, then rejected the generic live-3D mannequin produced by the engineering proof. The desired visible result is the original stylish, illustrated **2.5D character**, with convincing body variation and authored hair. The live-3D recommendation below records the earlier investigation; it is superseded as the presentation target by this correction.

## Art-direction correction after the first source proof

The shared-source experiment demonstrates registered geometry and equipment, but does not meet the game's visual target. It has a generic human base, simple flat materials, one limited muscle morph, no fatness control, procedural hair and unfinished equipment. Structural checks do not make that model an acceptable replacement for the illustrated character. The runtime experiment is now opt-in with `-sologym-avatar-renderer source3d`; the existing cutout renderer remains the default while its replacement is developed. The original cutout defects are not fixed by this rollback.

The user selected seven discrete body presets: **skinny, normal, chubby, fat, skinny muscular, muscular, fat muscular**. These require authored silhouettes and fitted clothing. Skin tone remains independent. Do not approximate all seven by stretching one drawing or exposing the experimental model's single muscle morph as if it covered these options.

For the 2.5D route, use one coherent illustrated character master per supported fit and view, registered in a layered authoring source. Derive layers, deformation meshes, equipment masks and exports from that source. A 3D rig may provide repeatable pose, depth, camera and garment guides; its raw model must not become the visible character merely because it is easy to export.

The next visual proof must establish full-body silhouette, recognizable face, deliberate stylish hair, muscle/body definition and a fitted outfit in the studio and intended elevated gameplay view. Textures/cosmetic skins are supported, but cannot replace missing silhouette, hair or body-shape authoring. Choosing 2.5D lets the accepted illustration guide the visible result while the pipeline supplies consistent registration.

The engineering sources under `design/character-source` are retained as technical experiments and optional authoring guides. They are not approved character art.

The user wants character correctness first, Sims-style customization, and a later elevated gameplay camera. The existing game art direction remains the visual target.

## Finding

Use a conventional character-production pipeline with AI assistance: **one editable character source, one versioned rig, fitted customization, scripted export, and visual verification in the game**. The sources reviewed do not establish a universal Astra-specific sprite standard. A project asset contract supplies the standard that every tool and agent must follow.

For SoloGym, the recommended next proof is a **stylized 3D master in Blender, rendered live in Unity**, with the same character used in the studio and gameplay camera. Painted textures, controlled shading and silhouettes should carry the established illustrated appearance. A 3D master can also produce sprite atlases if that route proves better after visual and device testing. This is an engineering recommendation based on the requested customization and viewpoints.

OpenAI's Astra game-development example follows generated concepts with an editable Blender model, runtime export and in-game review. It demonstrates a useful workflow, not a guarantee of production-quality humanoid anatomy or clothing. [OpenAI: Building games with Astra](https://developers.openai.com/blog/how-to-build-games-with-astra)

OpenAI's image-generation documentation explicitly describes limitations in recurring visual consistency and precise composition. Better prompts and references can improve candidates, but they cannot serve as exact joint coordinates or interchangeable geometry. [OpenAI image generation guide](https://developers.openai.com/api/docs/guides/image-generation)

## What failed in the current proof

The current source pieces contain hollow joint caps, overlapping neck anatomy, inconsistent garment perspective, and a pelvis assembled from a cropped pants section. Runtime offsets can align bounding boxes without repairing these drawings. Passing attachment and interaction checks does not establish anatomical or visual correctness.

The repository already proposes a shared 3D source in [asset production and animation sources](asset-production-and-animation-sources.md). However, [avatar-contract.json](../../data/art/avatar-contract.json) still labels the proposed skeleton `not_authored`, and `production_ready` is false. The active proof uses independently assembled 2D pieces. The proposed source pipeline was never completed.

## Available approaches

| Approach | Where it fits | Main limitation |
| --- | --- | --- |
| Layered 2D master with skinning | Painted character with limited viewpoints and discrete customization | Each new direction needs coherent artwork; changing silhouette and limb overlap requires corrective authoring. |
| **Shared 3D master rendered in Unity** | Body/face controls, fitted outfits, studio rotation and elevated gameplay | Needs good topology, weights, garment corrections, deliberate stylized shading and mobile profiling. |
| Shared 3D master rendered to sprites | Fixed camera directions and a sprite-based runtime | Export volume grows with fits, directions, actions and equipment; layered output still needs depth/occlusion handling. |

If the 2D route is selected, use one registered layered source and a real skinning mesh. Unity Sprite Swap requires identical skeletons for swapping sprites under skeletal animation. This formalizes skeleton compatibility; it does not repair incompatible source anatomy. [Unity Sprite Swap](https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/SpriteSwapIntro.html)

For 3D, Unity can reuse an Avatar across files with the same bone structure. Its documentation also requires checking the mapping and pose even when automatic mapping succeeds. A green importer result is therefore a technical check, not visual acceptance. [Unity humanoid import](https://docs.unity3d.com/6000.3/Documentation/Manual/ConfiguringtheAvatar.html)

## Responsibility of each tool

| Tool or artifact | Responsibility |
| --- | --- |
| Astra/ChatGPT acting through development tools | Maintain the specification; write Blender scripts, Unity importers and validators; run exports and capture comparisons; inspect results and propose corrections. |
| Image generation | Concept art, reference exploration, palette and texture proposals. Generated anatomy and placement remain candidates until checked against the master. |
| Blender source | Authoritative body geometry, skeleton, rest pose, weights, UVs, shape controls, fitted gear and animation. |
| Unity | Runtime appearance recipe, animation, skin/material controls, equipment visibility, actual studio/gameplay rendering and device measurements. |
| Versioned contract and review fixtures | Keep asset compatibility explicit and make future work reproducible across people, agents and sessions. |

Blender exposes scene editing through Python, including background script execution. OpenAI also documents Astra using that route to generate views and export editable scenes. A dedicated Blender connector can be convenient, but the pipeline can use saved scripts and the Blender executable. [Blender Python scripting](https://docs.blender.org/api/main/info_tips_and_tricks.html), [OpenAI: Architectural visualization with Astra](https://developers.openai.com/blog/architectural-visualization-with-astra)

The first body should come from deliberate modeling or a suitable existing source with recorded provenance. An AI-generated mesh may be a starting point, but topology, face quality, skinning and garment fit still need inspection and correction. The workflow does not assume a polished human character can be produced in one prompt.

### Where ControlNet and reference adapters help

ControlNet provides structural conditioning such as pose, edges and depth. IP-Adapter adds reference-image conditioning and can be combined with structural controls. These are useful for keeping concept candidates closer to a chosen pose and identity. [ControlNet repository](https://github.com/lllyasviel/ControlNet), [IP-Adapter repository](https://github.com/tencent-ailab/IP-Adapter)

The original IP-Adapter paper explicitly distinguishes resemblance from highly consistent subject reproduction. Its findings should not be generalized to every subsequent variant. [IP-Adapter paper, sections 4.4–5](https://arxiv.org/html/2308.06721v1)

For this project, the engineering implication is that conditioning does not establish shared topology, UVs, bone weights, sockets, garment clearance or frame registration. If tested later, feed renders and masks from the master into a pinned workflow, then use the result for supervised surface or concept work. Do not independently regenerate each limb, outfit or animation frame and assume compatibility. Adding this toolchain is optional; it is not required for the first correctness proof.

## Proposed production contract

Extend the existing contract when the first master exists; do not mark placeholder IDs as production assets. These are proposed SoloGym rules, not claims that an external tool enforces them automatically.

| Concern | Required record or invariant |
| --- | --- |
| Master | Editable source path, source hash/revision, source provenance and pinned tool versions. |
| Skeleton | Bone names and hierarchy, bind/rest pose, units, axes, root/ground anchor and named equipment sockets. |
| Body and face | Stable base topology; named shape controls with supported ranges; explicit supported fit families. |
| Equipment | Skeleton compatibility, slot, fit support, material regions, coverage masks, and corrective shapes/weights where needed. Sharing a skeleton alone does not make clothing follow body morphs. |
| Appearance | One recipe of stable IDs and control values shared by studio and gameplay; skin/hair tint regions exclude eyes and clothing. |
| Rendering | Versioned studio/gameplay cameras, lighting and material settings; compare both engines if assets are rendered offline. |
| Export | Saved scripts and import settings; no unrecorded per-piece scale/offset repair in runtime code. Changes to registration belong in the source and contract. |
| Sprite branch | Fixed canvas, pixels per unit, pivot, trim offset, ground anchor, action/frame timing, direction and matching occlusion passes. |
| Evidence | Source revision, recipe, camera, pose/frame, software version and actual runtime captures for every reviewed result. |

A new outfit should be fitted to the master, animated with the shared clips, exported by the same script and checked in the same fixtures. A new base or skeleton revision needs explicit compatibility work. It should never silently stretch old equipment to fit.

The first automated checks should validate meaningful failure modes: missing/incompatible bones, invalid weights, missing fit support, material-region mistakes, missing clips, inconsistent export metadata, and failed imports. Visual review must separately inspect anatomy, garment penetration, silhouette, facial identity, lighting and motion. Screenshot differences flag changes for inspection; they do not prove an attractive or anatomically correct result.

## Small proof before expanding the catalogue

Build one character with one body-shape control at minimum/middle/maximum, two skin palettes, two hairstyles, one fitted top and paired gloves. Keep the current menu presentation during this proof.

Capture the same appearance recipe in full-body and face studio views, front/side/back views, and the elevated gameplay view. Exercise idle, walk and jab with equipment on and off. Show the bare base on a plain background so clothing and effects cannot hide joint problems.

Acceptance requires continuous neck/shoulder/elbow/hip/knee anatomy, stable facial and hair alignment, correct near/far occlusion, no visible clothing penetration or gaps in the proof matrix, consistent ground contact, and matching appearance in both runtime views. Inspect motion as well as stills. Expand options only after this small combination works. A successful proof is bounded evidence, not a promise that untested combinations will fit.

If live 3D holds the art style and the chosen phone performance budget, continue with it. If sprite output is preferred, export eight directions from this same master and measure atlas cost and layer occlusion before adding more fits or items. This changes the order of the earlier proposal: prove the shared character and two runtime cameras first, then evaluate a sprite export branch if useful.

## Current status

At the time of this investigation, research and the recommendation were recorded without integrating a 3D source or new renderer. The character still needed visual correction. `BodyMaster-v2.png` and `AvatarRegisteredMesh.cs` were unintegrated experiments, not accepted production assets or evidence that the selected approach worked. The unused `BodyMaster-v2.png` runtime copy was later removed during the AutoSprite cleanup; the image is preserved in checkpoint commit `19fea4d362818b9df5aa234c0e725d33b5e294c7`, and its [generation prompt](../../design/prompts/character-correctness/body-master-v2.txt) remains available. `AvatarRegisteredMesh.cs` is still used by the retained illustrated proof renderer.

Unity 6000.3.24f1 is available. Blender was not found on PATH or the common local paths checked; installation and a source asset remain prerequisites for a Blender proof. No tools were installed during this investigation. Some Blender manual pages could only be read through official search-index excerpts because direct opens returned HTTP 402; the Unity and OpenAI sources and adapter repositories were directly accessible.
