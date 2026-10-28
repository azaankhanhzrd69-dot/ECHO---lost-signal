# AGENTS.md: rules for every AI agent in this repo

Applies to Gemini 3.8 Flash (Antigravity) and Nemotron 3.5 Lightning alike. Read this and `docs/STORY.md` before every task.

## Project
ECHO: Lost Signal is a first-person horror game in Unity (URP, Windows). The monster is eyeless and hunts by sound. Walking, running and crouching make noise. Story beats and tasks live in `BUILDSPEC.md`. The story in `docs/STORY.md` is final.

Unity version: `6000.6.7f1`. Render pipeline: URP. Branch: `main`. Level is a single floor.
Hiding: Locker is safe; Table and Chair always kill. The old sonar ping is dropped; do not build on it.

## Hard rules (never break)
1. **Never edit** `.unity`, `.prefab`, `.meta`, `.asset` or anything under `Library/`, `Temp/`, `ProjectSettings/` by hand. If a scene or Inspector change is needed, list it for the human.
2. **Never delete** assets. Never run `git push`, `git reset --hard`, `git clean`, `filter-repo`, or force flags. Humans commit and push.
3. **Edit only the files named in the task.** New files are fine, in your lane's folder. Do not touch another lane's folder or scene.
4. **Across lanes use only** `Progress`, `EchoEvents` and `IInteractable` (in `Scripts/Shared`). Do not reference another lane's classes.
5. **Do not claim you tested in Play Mode.** You cannot. Say what a human should check.
6. **Do not invent story.** Use `docs/STORY.md`, `voicelines.csv`, `sfx.csv`. Do not change beats or endings.
7. **Return a diff, not whole files,** and end with a checklist of every Unity Editor / Inspector step the human must do (components to add, fields to assign, layers, tags).

## Lanes and folders
- CORE: `Assets/ECHO/Scripts/Core`, `Scripts/Shared` (shared is read-only for non-Core)
- WORLD: `Assets/ECHO/Scripts/World`
- INTERACT: `Assets/ECHO/Scripts/Interact`

## Unity 6 API notes
- `Rigidbody.velocity` is now `linearVelocity`; `angularDrag`/`drag` are `angularDamping`/`linearDamping`.
- `FindObjectOfType` is deprecated: use `FindFirstObjectByType` or `FindAnyObjectByType`. Never call `Find*` in `Update`; prefer `[SerializeField]` references.
- Cache `GetComponent` results. Multiply movement by `Time.deltaTime`. Physics in `FixedUpdate`, cameras in `LateUpdate`.
- Do not `new` a MonoBehaviour. Use ScriptableObjects for tunable data and coroutines for timed sequences.
- URP, not Built-in: use URP shaders and the `UnityEngine.Rendering.Universal` namespace.
- If Domain Reload is off in Play Mode Options, reset static state on Play start with `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` and unsubscribe events in `OnDisable`.
- Custom URP Renderer Features must use the RenderGraph API (`RecordRenderGraph`), not the legacy `Execute`/`OnCameraSetup` path. If a URP API doesn't compile, say so and paste the error rather than guessing.
- Wrap Editor-only code in `#if UNITY_EDITOR` or put it under an `Editor/` folder.

## Compile check
When the Unity Editor is **closed**:
```
Unity -batchmode -nographics -quit -projectPath . -logFile -
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -logFile -
```
When the Editor is **open**, batch mode fails because the project is locked. Ask the human to paste the Console errors instead.

## Code style
Small classes, one responsibility, namespaces `Echo.Core`, `Echo.World`, `Echo.Interact`. Public tunables come from `GameTuning` ScriptableObject, not magic numbers. Short comments explaining which Unity callback runs when (the programmer is new to Unity).

## Reviewer agent checklist
Check the diff for: violated hard rules; direct cross-lane references; Find calls in Update; deprecated APIs; missing unsubscribe in `OnDisable`; hard-coded numbers that belong in `GameTuning`; missing Inspector instructions.
