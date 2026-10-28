# BUILDSPEC: ECHO: Lost Signal

Single source of truth for the 3-person parallel build.
Models: **Gemini 3.8 Flash (Antigravity IDE)** and **NVIDIA Nemotron 3.5 Lightning (free, via Opencode)**. Both read `AGENTS.md`.
Engine: **Unity 6000.6.7f1, URP, Windows**. Branch: **`main`**. Single-level station (no second floor).
Story reference: `docs/STORY.md` (final draft; it overrides the old SPEC.md puzzles and endings).
Agent setup: `AGENT_SETUP.md`. Agent rules: `AGENTS.md`.

> How to claim a task: change `[ ]` to `[~]` and put your name in the Owner column in a tiny commit to `main`. When done, change it to `[x]` in your PR.
> Legend: **G** = Gemini plans/writes logic, **N** = Nemotron does mechanical work, **H** = human only (Unity Editor, scenes, prefabs, `.meta`, git).

---

## 0. Ground rules (read once)

1. **Story is final.** Eyeless monster hunting by sound. Walking, running and crouching feed a noise/alert meter. The old sonar ping (a key press that lit up your surroundings but alerted the monster) is dropped: leave any existing code disabled and do not build on it. The old 2-3-1 / 3-1-2 puzzles, Dying Beacon and Selfish endings are replaced by the story beats in section 2.
2. **Vehicle naming.** The story says "shuttle", "space ship" and "escape pod". In code use: `Shuttle` (the wrecked ship you leave) and `EscapePod` (the powered ship you run to).
3. **Agents cannot use the Unity Editor.** Agents write C#. Humans do scene work, prefab wiring, Inspector assignments, NavMesh bakes, lighting bakes, and anything touching `.unity`, `.prefab` or `.meta`.
4. **Repo state (confirmed).** Git/LFS setup is healthy and the asset cleanup is committed and pushed on `main`.
5. **Level is built at runtime today.** `EchoGame.cs` builds the station when Play starts. Task C-03 (bake it into scenes) is the critical path: World's scene, NavMesh and lighting work cannot start until it lands. The level is a single floor; the second floor is cut.
6. **Team.** Three lane owners plus one spare person who takes the Graphics Mode track (W-10a to W-10f) and QA playthroughs. Time budget: about 6 hours today plus 1-2 hours tomorrow.

---

## 1. Repo layout and ownership

```
Assets/ECHO/
  Scripts/Core/        -> Lane CORE
  Scripts/World/       -> Lane WORLD
  Scripts/Interact/    -> Lane INTERACT
  Scripts/Shared/      -> Contract (Core owns; others read only)
  Scenes/Main.unity                -> CORE
  Scenes/Station_Geometry.unity    -> WORLD
  Scenes/Station_Lighting.unity    -> WORLD
  Scenes/Station_Gameplay.unity    -> WORLD
  Scenes/Test_*.unity              -> whoever created them
  Prefabs/UI/, Data/Audio/         -> INTERACT
docs/  STORY.md  WORKFLOW.md  CONTEXT.md
```

- One owner per scene. Need something in someone else's scene? Ask for a prefab, never edit their scene.
- One Assembly Definition per Scripts folder (Core, World, Interact), all referencing Shared.
- Move or rename assets only inside the Unity Editor so GUIDs survive.
- Source art (PSD, Blender, 4K originals) stays out of the repo.

---

## 2. Story beats (the spine of everything)

All gameplay is driven by one ordered list of beats. A debug menu (F1) jumps to any beat.

| # | Beat | What happens | Lane |
|---|------|--------------|------|
| 1 | Boarding | Cutscene: John and Mission Control | World, Interact |
| 2 | Vent | Find paper with keypad code and map | World, Interact |
| 3 | Keypad | Decompression door opens | Interact |
| 4 | MedBay | Mangled body, "Dear God" | World, Interact |
| 5 | Lunge | Creature dashes, John locks the door | Core, World |
| 6 | HideIntro | Locker (safe) vs table or chair (both kill), banging minigame | Core, Interact, World |
| 7 | Sneak | Walk to the shuttle, noise rules apply | Core |
| 8 | ShuttleWreck | Engine crushed, an escape pod must be powered | World, Interact |
| 9 | ReactorEmpty | Cores missing, map shows spares | World, Interact |
| 10 | CoreHunt | Collect cores, up to 3 more safe hides, the 4th kills | Core, World |
| 11 | PowerOn | Optional Morse note, place cores, power returns, monster alerted | Interact, Core |
| 12 | Chase | Run to the ship | Core, World |
| 13 | Airlock | Door shuts, hatch opens, monster sucked out | World, Core |
| 14 | Ending | Good Ending; Secret Ending if Morse was sent | Interact, World |

Rules to keep configurable (ScriptableObject `GameTuning`): safe hides before death (default 3 after the intro), noise values for walk/run/crouch, alert thresholds, monster hearing radius, monster speed per state, kill range, banging meter fill/decay rates.

Note: the Good Ending never depends on Morse. Morse only unlocks the Secret Ending.

---

## 3. Shared contract (Core pushes this in the first 45 minutes)

World and Interact code against this using stubs until the real systems land. Sketch, adjust as needed:

```csharp
using System;
using UnityEngine;

public enum Beat { Boarding, Vent, Keypad, MedBay, Lunge, HideIntro, Sneak,
                   ShuttleWreck, ReactorEmpty, CoreHunt, PowerOn, Chase, Airlock, Ending }

public static class Progress {
    public static Beat Current;
    public static int CoresFound, SafeHides;   // hide #4 after the intro = death
    public static bool HasMap, HasNote, MorseSent;
    public static void Reset() { Current = Beat.Boarding; CoresFound = 0; SafeHides = 0;
                                 HasMap = HasNote = MorseSent = false; }
}

public static class EchoEvents {
    public static event Action<Beat> BeatChanged;
    public static event Action<Vector3, float> Noise;  // position, loudness
    public static event Action<bool> HideFinished;     // true = survived
    public static void SetBeat(Beat b) { Progress.Current = b; BeatChanged?.Invoke(b); }
    public static void MakeNoise(Vector3 p, float loud) => Noise?.Invoke(p, loud);
    public static void EndHide(bool ok) => HideFinished?.Invoke(ok);
}

public interface IInteractable { string Prompt { get; } void Interact(); }
```

If Domain Reload is disabled in Play Mode Options, call `Progress.Reset()` and clear event subscribers on Play start (`[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`).

Stub rule: until the real system lands, code against `EchoEvents` and `Progress` only. Never reference another lane's classes directly.

---

## 4. LANE CORE (programmer): systems, monster, flow

Branch prefix `core/`. Owns `Main.unity`, `Scripts/Core`, `Scripts/Shared`, merges PRs.

| Status | ID | Task | Model | Owner | Done when |
|--------|----|------|-------|-------|-----------|
| [ ] | C-00 | Confirm cleanup commit is on `main` (skins 2-4, Starter Assets, Energy/Demo removed), add `AGENTS.md`, `docs/` files | H | | Clean `git status`, all laptops pulled |
| [ ] | C-01 | Contract files (section 3), `GameTuning` ScriptableObject, assembly definitions | G | | Compiles, pushed to `main` within 45 min |
| [ ] | C-02 | Beat-jump debug menu (F1) plus cheats: noclip, god mode, set noise, show hearing radius gizmo | G then N | | In Play Mode, F1 jumps to any beat and sets sane Progress values |
| [ ] | C-03a | **Critical path.** Editor script that runs the existing `BuildStation()` in edit mode and saves the result as scenes plus room prefabs, saving procedurally generated materials and textures as assets. Human runs it and commits | G (script), H (run) | | Station exists as saved scene(s) without pressing Play |
| [ ] | C-03b | Split `EchoGame.cs` into `partial class` files (no logic change) | N | | Compiles, behaviour identical |
| [ ] | C-03c | "Built map" flag: skip runtime level build AND skip disabling the scene's cameras and lights | G | | Saved map loads with its own lights and cameras intact |
| [ ] | C-04 | Player noise: walk, run and crouch emit `EchoEvents.MakeNoise` at footstep intervals; loudness from `GameTuning` | G | | Test_Monster arena shows noise events in a debug overlay |
| [ ] | C-05 | MonsterAI state machine: Dormant, Investigate, Chase, Search, Attack, driven only by `EchoEvents.Noise`; NavMeshAgent; hearing radius gizmo | G | | Monster investigates a noise, chases when loud, loses you after Search timeout |
| [ ] | C-06 | EditMode unit tests: noise to alert thresholds, state transitions, hide counter (4th hide kills), beat order | N (after G writes one example) | | Tests green |
| [ ] | C-07 | MedBay Lunge scripted sequence (monster dash, door lock) hooked to beat `Lunge` | G | | Jump to Lunge plays it, then advances to `HideIntro` |
| [ ] | C-08 | Hide system logic: Locker is safe, Table and Chair always kill; `SafeHides` counter, 4th safe hide after the intro = death; raises `HideFinished` | G | | Counter and death verified in Test_Hide |
| [ ] | C-09 | Death and respawn at last checkpoint beat; "caught" overlay hook | G | | Dying returns you to the start of the current beat |
| [ ] | C-10 | Ending controller: Good vs Secret (`MorseSent`), triggers ending screens | G | | Both endings reachable from jump menu |
| [ ] | C-11 | Chase beat logic: monster sprint speed, pursuit to the pod, no free hides | G | | Chase is survivable but tense |
| [ ] | C-12 | Integration owner: merge PRs, run smoke test after every merge (section 8) | H | | Always |

---

## 5. LANE WORLD (map and graphics): scenes, props, cutscenes, Graphics Mode

Branch prefix `world/`. Owns `Station_Geometry`, `Station_Lighting`, `Station_Gameplay`, `Scripts/World`.

| Status | ID | Task | Model | Owner | Done when |
|--------|----|------|-------|-------|-----------|
| [ ] | W-01 | After C-03a: split the baked map into additive scenes (Geometry, Lighting, Gameplay) and make `Main` load them additively. While waiting for C-03a, World does W-03 and W-04 prep and Graphics Mode code | H | | Press Play on Main, whole station loads |
| [ ] | W-02 | Bake NavMesh with the AI Navigation package; verify the monster can reach every room | H | | NavMesh covers all walkable floor |
| [ ] | W-03 | Geometry linter Editor script: flags overlapping or duplicate colliders and renderers (z-fighting, double walls) | G | | Menu item lists offenders; humans fix them |
| [ ] | W-04 | Measure door and ceiling heights, scale the monster to under 85% of both (not 3x the player if that breaks doors); check kill range and agent radius afterwards | H | | Monster fits through every door |
| [ ] | W-05 | Place Gameplay objects as prefabs: vent, keypad, door, mangled body and blood, 3 MedBay hide spots, reactor, radio room, core spawn points matching the map prop | H | | Each has a collider and an `IInteractable` (Interact lane provides the scripts) |
| [ ] | W-06 | Patrol waypoints and test arenas: `Test_Hide`, `Test_Monster` | H | | Arenas load standalone |
| [ ] | W-07 | Emergency-light flicker script (reacts to `EchoEvents.Noise` and `BeatChanged` for tension, not to the monster class) | N | | Flicker visible, parameters in Inspector |
| [ ] | W-08 | Cutscene director: coroutine-based, locks player, moves camera, plays VO, resumes. Timeline optional | G | | `Director.Play(sequence)` works |
| [ ] | W-09 | Stage four cutscenes: Boarding, Lunge camera, ShuttleWreck, Airlock hatch and monster sucked out; plus the asteroid ending | H + G | | Each playable from the jump menu |
| [ ] | W-10a | **Spare person.** `GraphicsMode` ScriptableObject presets (Performance, Balanced, Cinematic) plus runtime switcher, manual only (pause menu and F1) | G | | Switching mode changes settings live |
| [ ] | W-10b | **Spare.** Editor tool "ECHO > Setup Lighting": near-black ambient, global post Volume (tonemapping, bloom, vignette, grain, colour), SSAO feature, reflection probes per room, flashlight spotlight | G, then H runs | | One click gives a moody lit station |
| [ ] | W-10c | **Spare.** Baked GI: Adaptive Probe Volumes or lightmaps, single owner, single commit | H | | Bounce light visible, no leaks on route |
| [ ] | W-10d | **Spare.** Own SSR: a custom URP Renderer Feature written in-house using the RenderGraph API (no third-party code, no licence issue) | G | | Wet-looking floors reflect lights in Cinematic mode |
| [ ] | W-10e | **Spare.** Planar reflections on key corridor floors: reflection camera to low-res RenderTexture plus a floor shader/material | G, then H wires | | Floors in 2-3 key corridors mirror the room |
| [ ] | W-10f | **Spare.** Light shafts: additive cone meshes with fade, fog and light cookies on emergency lights | N | | Visible beams, tuned in Inspector |
| [ ] | W-11 | Fix light bleed at seams visible on the player route only | H | | No visible leaks along the main route |
| [ ] | W-12 | Texture budget pass (1K/2K for most, 4K only for hero assets), resize in place without changing file type so GUIDs hold | H | | `du` of Assets down, no pink materials |

---

## 6. LANE INTERACT (puzzles, audio, UI)

Branch prefix `interact/`. Owns `Scripts/Interact`, UI prefabs, audio data.

| Status | ID | Task | Model | Owner | Done when |
|--------|----|------|-------|-------|-----------|
| [ ] | I-01 | Interaction system: raycast from the camera, "Press E" prompt, one press = one action, uses `IInteractable` | G | | Look at a test cube, prompt appears, E triggers |
| [ ] | I-02 | Pickups: vent paper (sets `HasMap`), map viewer, note (`HasNote`), cores (`CoresFound++`) | N | | Each updates `Progress` |
| [ ] | I-03 | Keypad UI plus door unlock; code from the vent paper | G then N | | Wrong code rejected, right code advances to MedBay |
| [ ] | I-04 | Hiding minigame UI: locked cursor, reticle driven by accumulated mouse movement plus shake, keep it inside the centre ring while a "banging" meter fills; calls `EchoEvents.EndHide` | G | | Success and fail both fire `HideFinished` |
| [ ] | I-05 | Morse radio UI: dot, dash, space buttons, Send, validator; sets `MorseSent` | G (validator), N (UI boilerplate) | | Validator unit tests green |
| [ ] | I-06 | `voicelines.csv` and `sfx.csv` (id, text, file, trigger beat), CSV loader, subtitle display | N | | A line plays and shows subtitle on its beat |
| [ ] | I-07 | Audio manager: ambience per area, monster footsteps (distance-based), interaction sounds | G then N | | Mix is audible and not clipping |
| [ ] | I-08 | One cheap occlusion trick: a single raycast to the monster plus a low-pass filter | N | | Muffled behind walls |
| [ ] | I-09 | Title, pause (includes Graphics Mode selector), death and ending screens | N | | Navigable with mouse and keyboard |
| [ ] | I-10 | Objective hints and pacing text per beat | N | | Hint changes with each beat |

---

## 7. Graphics Mode: "cheap ray tracing" on an ROG Zephyrus

Target machine: Ryzen AI 9, 32 GB LPDDR5X, RTX 5060 laptop GPU, OLED display. Plenty of headroom, but laptops throttle on battery, so this must be a switchable mode.

**Honest note:** hardware ray tracing in Unity is HDRP only, and this project is URP. So we fake it with these techniques, in order of visual payoff per hour:

1. **Baked GI**: Adaptive Probe Volumes (Unity 6) or classic lightmaps plus Light Probes, using the GPU progressive lightmapper. One owner, one commit, because bakes cause scene conflicts. This is the biggest visual jump.
2. **Reflection probes** per room with box projection, at medium resolution. Gives metallic floors and walls believable reflections.
3. **SSAO** (URP renderer feature) for contact darkening in corners.
4. **Post-processing Volume**: Tonemapping (ACES or Neutral), bloom, subtle vignette, film grain, colour adjustments. OLED tip: keep blacks truly black, avoid crushing everything with grey ambient.
5. **Shadows**: soft shadows on, higher shadow resolution and cascade tuning, flashlight spotlight with soft shadows, a few mixed lights per room.
6. **Cheap light shafts / volumetric look**: additive cone meshes with fade, or fog plus light cookies on emergency lights.
7. **Floor reflections** in key corridors: a planar reflection script (second camera to render texture, lowered resolution). Task W-10e.
8. **Screen-space reflections**: URP has no built-in SSR. We write our own renderer feature (W-10d) so there is no third-party licence to worry about. Compatibility risk: Unity 6 renderer features must use the RenderGraph API, so tell the agent the exact Unity version (6000.6.7f1) and have it compile in the Editor, pasting errors back. Do this after baked GI, probes and SSAO work, since they give more look for less risk. If it fights you after about 90 minutes, fall back to probes plus planar reflections.

**Preset design** (a `GraphicsMode` ScriptableObject with a runtime switcher, selectable from pause menu and the F1 menu):

| Mode | Contents |
|------|----------|
| Performance | Baked GI only, no SSAO, no reflection probe updates, low shadow res, no extra post |
| Balanced | Baked GI, SSAO, probes, medium shadows, bloom and tonemapping |
| Cinematic (RTX laptop) | Everything: high shadow res, soft shadows, full post stack, light shafts, planar floor reflections in key rooms |

Selection is manual only (pause menu and F1). Default on first launch: Balanced. The ROG should be set to Cinematic by hand.

**Known gotchas:**
- `EchoGame` disables all cameras and lights on scene load. Task C-03 must land before lighting is visible.
- Any reveal system that toggles renderers on and off stops casting shadows while disabled. If the old pulse reveal stays, test against it. The story no longer needs it as a core mechanic.
- Test on battery once; if frames drop, Balanced must still look good.
- Take before/after screenshots for each step so effort is visible.

---

## 8. Git and testing rules

- `main` is always playable. Short branches (`core/…`, `world/…`, `interact/…`), merge within about 90 minutes, merge `main` into your branch before opening the PR.
- Before every push: `git status --short`. If you see `Library/`, `Builds/` or big unexpected files, stop.
- PR definition of done: Console clean, works in its test scene, reachable from the jump menu, PR description lists every Inspector step.
- Test by playing in the Editor. Build only at milestones, on one laptop, same output folder each time, Mono Development Build, no IL2CPP, no Crunch today.
- The batch-mode compile command fails while the Editor has the project open. While the Editor is open, copy Console errors into the agent instead.
- Unit tests only for pure logic: noise to alert, hide counter, Morse validator, beat order.
- **Smoke test after each merge (3 min):** jump to Keypad, then MedBay, hide in the locker, sneak to the shuttle, jump to CoreHunt.
- Playthroughs at each integration point are done by someone who did not write that code.

---

## 9. Schedule (hours from now; shrink if you have less)

| When | What |
|------|------|
Budget: about 6 hours today, plus 1-2 hours tomorrow for polish and the final build.

| When | What |
|------|------|
| 0:00-0:45 | Core pushes contract and jump menu (C-01, C-02) and the bake script (C-03a). World preps W-03/W-04 and tests the bake. Interact writes CSVs and starts keypad and Morse UI on stubs. Spare writes Graphics Mode code (W-10a, W-10b). Everyone pulls at 0:45. |
| 0:45-1:15 | C-03a run and committed, then World does W-01 and W-02 (scenes and NavMesh). Core does C-03b/c. |
| 1:15-3:00 | Act 1: Boarding to Sneak. Spare: baked GI (W-10c) once the scenes exist. |
| 3:00 | Integration 1: full Act 1 playthrough, fixes |
| 3:00-5:00 | Act 2: ShuttleWreck to Airlock, Morse and Secret Ending. Spare: SSR, planar reflections, light shafts (W-10d to W-10f). |
| 5:00 | Integration 2: full run, first checkpoint build |
| 5:00-6:00 | Fixes, audio mix, Morse tuning, ending polish. Feature freeze at 6:00. |
| Tomorrow, 1-2h | Bug fixes, Graphics Mode polish, final build, tag it, upload the zip outside Git |

**Cut order if late:** table/chair death cutscenes become fade + sound + "caught" overlay; ending staging becomes text cards over VO; later hide spots become closets only; extra lighting polish (planar reflections, SSR); monster animations beyond idle, run and attack.

---

## 10. Using the two models

- **Gemini 3.8 Flash (Antigravity):** task specs, architecture, logic-heavy code (MonsterAI, hide minigame, ending controller, Editor scripts), hard debugging, diff review. It can burn quota on long loops, so keep tasks small.
- **Nemotron 3.5 Lightning (free via Opencode):** mechanical work (pure code moves, boilerplate, CSV loaders, placing repeated pickups, EditMode test copies of an existing example, comments). Treat it as a fast junior: tight spec, one file at a time. Check your host's context limit and rate limits; free tiers throttle.
- **Loop:** Gemini writes the task card and a worked example, then Nemotron repeats it, then the *other* model reviews the diff, then a human reviews the PR.
- One session per task, fresh context each time. Attach only the files the task needs.

**Prompt template:**

```
Read AGENTS.md and docs/STORY.md.
Beat: <e.g. HideIntro> | Lane: <Core/World/Interact> | Task ID: <e.g. C-08>
Use only Progress, EchoEvents and IInteractable across lanes. Edit only: <files>.
New files are fine. Do not touch other lanes' folders.
Done when: <what I see in Play Mode after jumping to this beat>
Return: a diff, plus every Inspector/Editor step I must do.
```

---

## 11. Risks and mitigations

| Risk | Mitigation |
|------|------------|
| First time scripting a game for the programmer | Ask agents to annotate every script with what Unity calls and when; keep MonoBehaviour basics from `AGENTS.md` handy |
| Agent claims it tested in Play Mode | It cannot. Humans verify in the Editor |
| Scene merge conflicts | One owner per scene, additive scenes, prefabs for hand-offs |
| Deprecated Unity APIs from models | API notes in `AGENTS.md` |
| LFS bandwidth | Copy the project between laptops by USB/LAN, not via GitHub; keep `CREDITS.md` for open-source assets |
| Time | Beat-jump menu so nobody replays the game; cut order in section 9 |
