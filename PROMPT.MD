# ECHO — MAJOR LEVEL DESIGN, HORROR, GAMEPLAY & POLISH UPGRADE

You are a senior Unity game developer, horror game designer, level designer, lighting artist, audio designer, and gameplay programmer.

I am developing **ECHO**, a 3D sci-fi psychological horror game in **Unity 6.6**. I have an existing project with a space station, rooms, corridors, a monster, a spacecraft, and puzzles.

Your task is to significantly upgrade the EXISTING GAME into a polished, atmospheric, terrifying, and fully playable sci-fi horror experience suitable for a competitive game development hackathon.

**IMPORTANT: Do not unnecessarily rebuild the project from scratch. Inspect the existing project, understand its hierarchy, scripts, prefabs, materials, lighting, interactions, and gameplay flow before modifying anything. Preserve existing working features wherever possible.**

The result must feel like a professionally designed indie horror game, not a collection of disconnected assets.

---

# 1. ADD A SECOND FLOOR TO THE SPACE STATION

Expand the existing space station by adding a complete, accessible second floor.

### Level design requirements
- Create a fully connected second floor with a clear architectural relationship to the existing first floor.
- Include new corridors, rooms, a connecting staircase or ladder, and a central observation area.
- Add a small control room, maintenance section, and an atmospheric upper-level overlooking the lower station.
- Introduce environmental storytelling through damaged equipment, abandoned workstations, broken lights, bloodless signs of an evacuation, and malfunctioning machinery.
- Make the second floor feel larger and more dangerous without making navigation confusing.
- Include shortcuts or alternative routes where appropriate.
- Connect the second floor logically to the main reactor and the spacecraft progression.
- Ensure the player can actually reach, explore, and leave every intended area.
- Add useful environmental landmarks so the player understands where they are.

### Visual design
Use a coherent industrial sci-fi design:
- Dark metallic flooring.
- Heavy modular wall panels.
- Reinforced bulkheads.
- Exposed pipes and cables.
- Emergency lighting.
- Damaged ceiling panels.
- Occasional steam vents and electrical sparks.
- Large structural supports and believable door frames.

Do not fill every empty area with random props. Every object should contribute to the environment, gameplay, or atmosphere.

**The new floor must feel like a natural extension of the original station, not a separate map pasted onto it.**

---

# 2. FIX THE ENTIRE LEVEL GEOMETRY

This is a high-priority requirement.

The existing level must have clean, seamless, professional-looking boundaries.

### Eliminate geometry problems
- No overlapping walls.
- No walls clipping through other walls.
- No duplicate wall surfaces occupying the same space.
- No visible gaps between adjoining wall sections.
- No floating floor sections.
- No floor and ceiling intersections.
- No exposed backsides of wall meshes.
- No objects protruding through doors or corridors unintentionally.
- No visible seams caused by inconsistent wall heights, widths, or positions.
- No light leaking through supposedly solid walls.
- No player-visible gaps at corners, intersections, door frames, or floor transitions.

### Geometry construction
Inspect existing modular room and corridor prefabs.

Create a consistent modular construction system with:
- Standardized wall dimensions.
- Consistent floor heights.
- Proper corner pieces.
- Dedicated door-frame pieces.
- Appropriate wall thickness.
- Correct alignment between floor, walls, and ceiling.
- Properly connected corridor intersections.

When two walls meet, their geometry must be designed to join cleanly instead of being independently placed on top of each other.

Where necessary, replace problematic pieces with properly sized meshes or prefabs.

Use appropriate colliders and ensure the player cannot walk through solid walls.

### Boundary and collision polish
- Ensure all walkable areas have reliable collision.
- Prevent the player from getting stuck on door frames, corners, stairs, or decorative objects.
- Prevent the player from walking through closed doors.
- Ensure the monster cannot pass through walls or closed bulkheads unless that behavior is explicitly designed.
- Check that the second floor connects to the first floor without collision problems.
- Verify that the player cannot accidentally fall through the environment.

Perform a visual and collision inspection of the entire station after the changes.

**Do not hide geometry problems using darkness. Fix the actual geometry.**

---

# 3. COMPLETELY ELIMINATE UNWANTED WALL LIGHTING AND LIGHT BLEED

One of the most important visual problems to fix is unwanted lighting on walls.

I want the environment to be dark and cinematic, but the lighting must remain physically believable and visually controlled.

### Requirements
- Eliminate unwanted bright patches on walls.
- Eliminate flickering or shimmering caused by overlapping surfaces.
- Eliminate visible light leaking through walls, floors, ceilings, and closed doors.
- Avoid bright wall surfaces that appear to glow without a light source.
- Fix incorrect material emission.
- Fix excessive ambient illumination.
- Remove unnecessary light sources from the interiors.
- Prevent lights from illuminating rooms through solid geometry.
- Inspect point lights, spotlights, area-light substitutes, emissive materials, and baked lighting.
- Check for duplicate lights and overlapping lighting volumes.
- Fix shadow artifacts where practical.

If the project uses baked lighting, inspect the lightmaps, UVs, baking settings, and environment geometry.

If the project uses real-time lighting, inspect light ranges, shadow settings, light layers where applicable, and rendering configuration.

Do not simply disable all lighting or all shadows to solve these issues.

**The goal is controlled darkness with intentional illumination, not a completely black environment.**

---

# 4. CREATE A MUCH DARKER, MORE TERRIFYING ATMOSPHERE

Completely improve the lighting and visual atmosphere across the station.

The game should feel like a real abandoned spacecraft where something dangerous is hunting the player.

### Visual direction
Use a combination of:
- Deep shadows.
- Low-intensity cool ambient lighting.
- Desaturated metallic surfaces.
- Dim emergency lights.
- Intermittent red warning lights.
- Occasional cold white or blue illumination.
- Dark corridors with limited visibility.
- Strong contrast between illuminated areas and shadowed spaces.
- Subtle volumetric fog where supported and appropriate.
- Dust or particles visible only when illuminated.
- Light shafts in selected areas.
- Occasional electrical sparks.
- Small amounts of atmospheric steam.

Avoid excessive bloom, oversaturated colors, and constant visual effects.

The player should be able to recognize important doors, interactable objects, and navigational landmarks without making every area bright.

### Flickering red emergency lights
Implement a reusable emergency-light system.

Requirements:
- Red lights flicker irregularly.
- Different lights have different flickering patterns.
- Some lights occasionally switch off for several seconds.
- Some lights briefly pulse brighter during scripted events.
- A few lights can fail permanently as the game progresses.
- Flickering must not look like a synchronized disco effect.
- Light transitions should feel electrical and unsettling.
- Avoid excessive real-time light updates that could hurt performance.

Make the lighting behavior configurable through Inspector settings.

Create a controlled balance between darkness and visibility.

Use lighting to direct attention toward important doors, puzzle clues, dangerous areas, and the reactor.

### Dynamic horror lighting
When the monster approaches:
- Nearby emergency lights may become unstable.
- The lighting can subtly change to signal danger.
- Selected lights may turn red or briefly go dark.
- Lighting changes must be subtle enough to preserve suspense.

Do not reveal the monster's location every time it approaches.

---

# 5. REDESIGN THE MONSTER — MAKE IT MUCH BIGGER AND BETTER

The current monster needs a major upgrade.

Transform it into a frightening, physically imposing sci-fi creature that feels dangerous, intelligent, and memorable.

### Monster appearance
Make the monster significantly larger than the player.

Suggested proportions:
- Approximately 2.5–3 times the player's height, adjusted to suit the existing character model and level.
- Long limbs.
- An unsettling silhouette.
- An unusually elongated torso or neck.
- Large, unnatural hands or claws.
- A distorted head or partially concealed face.
- Damaged, biomechanical, or mutated surface details.
- Subtle glowing features used sparingly.

The monster should look like it belongs in the abandoned space station.

It should feel organic, disturbing, and dangerous rather than like a generic robot or oversized zombie.

If the existing model supports it, improve the materials, proportions, textures, rigging, and animation.

If the existing model is unsuitable, replace it with an available compatible asset only if it can be integrated cleanly into the current project.

Do not leave the monster as a featureless black silhouette.

Its shape should remain readable in low light, while its details are revealed gradually.

### Monster animation
Improve:
- Idle animation.
- Breathing or body movement.
- Walking animation.
- Running animation.
- Turning.
- Searching.
- Attacking.
- Staggering or recovering where appropriate.
- Death or despawn behavior if the game includes it.

Use animation blending and appropriate movement transitions.

Avoid sliding feet, unnatural rotation, teleporting, and visibly snapping between animation states.

### Monster AI
Improve the monster's behavior so it feels like a predator.

Implement or improve these states:

1. **Dormant:** The creature remains hidden or inactive.
2. **Patrol:** It moves through selected corridors and rooms.
3. **Investigate:** It moves toward suspicious sounds or events.
4. **Search:** It checks nearby areas when it loses the player.
5. **Chase:** It pursues the player when it detects them.
6. **Attack:** It attacks when close enough.
7. **Lost target:** It searches the last known player location before returning to patrol.

The monster should not constantly know the player's exact location.

Use sensible sight distance, field of view, obstacles, hearing distance, and investigation delays.

Allow the player to hide or break line of sight using appropriate environmental features if the existing gameplay supports them.

The monster must navigate both floors using the actual level layout.

Use a properly configured NavMesh or another suitable navigation system.

Avoid allowing the monster to:
- Walk through walls.
- Run through closed doors.
- Get permanently stuck at corners.
- Teleport unexpectedly.
- Chase the player through inaccessible geometry.

### Scare design
Create several different scare opportunities:
- A distant silhouette crossing a corridor.
- A brief glimpse of the creature at the end of a hallway.
- Heavy footsteps heard from an adjacent room.
- A creature that appears to have disappeared when the player returns.
- A scripted appearance near a key progression point.
- An occasional close encounter.

Do not use constant jumpscares.

Build tension through anticipation, uncertainty, sound, darkness, and occasional glimpses.

All scripted encounters must be tested to ensure they do not break progression.

---

# 6. ADD HIGH-QUALITY SPATIAL AUDIO

Audio must become a major part of the horror experience.

Use the existing audio system if it is functional. If not, improve it without introducing unnecessary dependencies.

### Monster audio
Add appropriate audio events for:
- Heavy footsteps.
- Distant footsteps.
- Footsteps approaching the player.
- Footsteps moving away.
- Low breathing.
- Unsettling growls.
- Subtle vocalizations.
- Wall or metal impacts where appropriate.
- Chase sounds.
- Attack sounds.
- Detection or investigation sounds.

### Spatial audio
Monster footsteps must sound like they originate from the monster's actual position.

Implement:
- 3D positional audio.
- Distance-based attenuation.
- Appropriate stereo or spatial positioning.
- Slight variation in footstep timing and volume.
- Different sound intensity based on distance.
- Reverberation or environmental effects where supported.
- Occlusion or muffling through walls where practical.

A monster walking on the other side of a bulkhead should sound different from one approaching directly down the corridor.

Do not play every sound at full volume everywhere.

### Space station ambience
Create an atmospheric ambient soundscape featuring:
- Low mechanical humming.
- Ventilation.
- Distant metal creaking.
- Occasional electrical buzzing.
- Intermittent alarm sounds.
- Subtle metallic impacts.
- Reactor rumbling.
- Air circulation and pressure-system noises.
- Occasional distant, unexplained sounds.

Use audio sparingly enough that individual sounds remain meaningful.

### Player and spacecraft audio
Add appropriate sounds for:
- Player footsteps on metal flooring.
- Door opening and closing.
- Buttons and control panels.
- Puzzle interactions.
- Reactor startup.
- Reactor failure.
- Electrical systems activating.
- Spaceship engines.
- Thrusters.
- Cockpit controls.
- Spacecraft movement.
- Successful mission progression.

Use short, satisfying interaction sounds.

Avoid repetitive sounds firing every frame or stacking indefinitely.

If appropriate assets are unavailable, build a clean audio integration system with clearly named AudioClip slots and safe fallbacks. Do not invent nonexistent asset paths or leave the game broken because an audio file is missing.

---

# 7. IMPROVE SPACECRAFT HANDLING

The spacecraft must feel responsive, controllable, and satisfying.

Inspect the existing spacecraft controller before changing it.

### Handling requirements
- Smooth acceleration.
- Predictable deceleration.
- Responsive steering.
- Proper pitch, yaw, and roll controls where applicable.
- Stable camera behavior.
- Clear forward movement.
- Sensible turning speed.
- Appropriate limits on extreme rotation.
- Smooth transitions between idle, acceleration, and braking.
- Proper collision handling.
- No random spinning.
- No uncontrollable drifting.
- No sudden camera snapping.
- No movement caused by conflicting input systems.

If the spacecraft uses Rigidbody physics, tune the physics forces, drag, angular drag, and rotation handling appropriately.

If it uses a custom controller, improve the acceleration curves and movement smoothing.

Do not combine incompatible Rigidbody and transform-based movement systems.

### Controls
Make the controls understandable and accessible.

Use an intuitive control layout:
- WASD or arrow keys for movement, depending on the current design.
- Mouse or another clearly defined input for steering if appropriate.
- Shift for acceleration or boost if suitable.
- Space for a relevant secondary control if appropriate.
- Escape to pause.

These bindings are suggestions; preserve existing controls if they already work well.

Display the controls in a small, readable tutorial or cockpit overlay.

Allow the player to understand how to move without guessing.

### Flight feedback
Add subtle:
- Engine audio.
- Thruster effects.
- Acceleration feedback.
- Camera response to movement.
- Cockpit or HUD feedback where appropriate.

Keep camera effects comfortable and avoid excessive shake.

Test the spacecraft in the actual game scene rather than in isolation.

---

# 8. FIX AND IMPROVE THE PUZZLE NEAR THE FIRST EXIT DOOR

The puzzle near the first door exit must become easier to interact with and more satisfying to solve.

### Interaction problems to fix
- The interaction button must be obvious.
- The player should not need pixel-perfect aiming.
- The interaction prompt must appear at the correct distance.
- The puzzle should be reachable from the intended side.
- The prompt must disappear when the player moves away.
- The interaction must not trigger through walls.
- Repeated button presses must not cause duplicate interactions.
- The UI must not overlap important puzzle elements.
- The player must receive clear feedback after each action.

### Interaction system
Create or improve a reusable interaction system.

Requirements:
- Use a sensible interaction range.
- Detect interactable objects using raycasts or an appropriate alternative.
- Filter interactions by line of sight where necessary.
- Display a clear prompt such as "Press E to Interact."
- Support keyboard and controller input if the current project supports both.
- Provide visual feedback when an object is interactable.
- Ensure only one interaction occurs per intended input.
- Avoid conflicting input handling between the player controller and puzzle interface.

Use Unity's existing Input System if it is already configured. Do not unnecessarily migrate the entire project to a different input framework.

### Improve the first-door puzzle
Retain the original puzzle concept if it is already functional, but improve its presentation and feedback.

Make the puzzle understandable without making it trivial.

Include:
- A clear starting state.
- Readable symbols, indicators, or controls.
- A visible indication of correct and incorrect actions.
- A short success animation or lighting change.
- A satisfying unlock sound.
- A clear indication when the door becomes available.

If the puzzle is a keypad, circuit puzzle, or sequence puzzle, preserve its existing logic unless it is broken.

The player must never be left wondering whether an input registered.

---

# 9. COMPLETELY REVAMP THE REACTOR STARTUP MECHANISM

The reactor startup should become a memorable gameplay sequence rather than a single button press.

Create a short, atmospheric, interactive reactor activation puzzle.

The puzzle should be understandable, visually engaging, and completable in approximately 30–60 seconds.

### Proposed reactor sequence

**Stage 1: Restore auxiliary power**
- The player reaches the reactor control station.
- A terminal indicates that auxiliary power is offline.
- The player interacts with a nearby power-routing panel.

**Stage 2: Solve a small routing puzzle**
- Present a small panel with three or four switches or circuit nodes.
- The player must activate the correct combination or connect the power path.
- Display a small number of readable hints on the panel.
- Provide visual feedback for successful and failed inputs.
- Use illuminated circuit paths, indicators, or connected nodes.
- Avoid requiring random guessing.

**Stage 3: Stabilize the reactor**
- After restoring auxiliary power, the player interacts with the main reactor console.
- A small sequence of controls must be activated in the correct order.
- Show the order through environmental clues or an interface hint.
- Add a short countdown or pressure mechanic only if it enhances the experience without making the puzzle frustrating.

**Stage 4: Start the reactor**
- The reactor begins to activate.
- Lights illuminate progressively.
- Machinery starts moving.
- Ambient sound intensifies.
- The reactor produces a powerful low-frequency rumble.
- The station's emergency lighting changes as power is restored.
- The UI confirms that reactor startup has succeeded.

### Horror integration
Starting the reactor should attract the monster or change its behavior.

Possible effects:
- The reactor's sound draws the monster toward the room.
- Emergency lights begin flickering more intensely.
- A nearby door opens, revealing a new route.
- The creature becomes more aggressive after power is restored.
- A short scripted scare occurs after the startup sequence.

Use these effects carefully. Do not trigger so many events simultaneously that the player cannot understand what is happening.

### Failure handling
- Incorrect inputs should produce clear feedback.
- The player must be able to retry.
- Reset the puzzle only when appropriate.
- Avoid irreversible softlocks.
- Do not allow the reactor to enter contradictory states.
- Prevent duplicate startup animations or repeated audio playback.
- Once activated, the reactor should remain activated unless the game explicitly supports shutdown.

### Visual presentation
Give the reactor a strong visual identity.

Use:
- A large central reactor structure.
- Rotating or animated components where practical.
- Glowing energy elements.
- Heavy mechanical supports.
- Warning indicators.
- Steam or particles.
- Pulsing light.
- A dramatic startup sequence.

Do not rely entirely on particle effects. The reactor's actual geometry, materials, lighting, sound, and animations must work together.

---

# 10. IMPROVE DOORS, PANELS, AND ENVIRONMENTAL INTERACTIONS

Ensure that all important interactable objects follow consistent behavior.

### Doors
- Open and close smoothly.
- Use appropriate audio.
- Have clear locked, unlocked, and open states.
- Do not allow the player to bypass collision by walking through them.
- Use sensible interaction distances.
- Avoid opening through walls or other objects.

### Panels and terminals
- Use readable text.
- Provide immediate input feedback.
- Clearly indicate success, failure, and unavailable actions.
- Avoid cluttered UI.
- Keep the interaction prompt aligned with the actual object.

### Environmental details
Where appropriate, add:
- Broken monitors.
- Flickering displays.
- Damaged wall panels.
- Emergency signage.
- Warning labels.
- Loose cables.
- Pipes and vents.
- Maintenance equipment.
- Abandoned tools.
- Damaged security cameras.

Use a consistent visual style across both floors.

---

# 11. IMPROVE THE PLAYER EXPERIENCE AND HORROR PACING

The player should always understand the immediate objective without knowing everything about the environment.

Create a clear progression structure:

1. Explore the first-floor station.
2. Discover and solve the first exit-door puzzle.
3. Gain access to additional station areas.
4. Reach the second floor.
5. Discover the reactor and its startup requirements.
6. Complete the reactor puzzle.
7. Survive the monster's response.
8. Reach the spacecraft or the next major progression point.
9. Escape or proceed to the intended ending.

Adapt this sequence to the existing game rather than breaking established progression.

### Horror pacing
Use a mix of:
- Quiet exploration.
- Environmental clues.
- Distant audio cues.
- Brief visual encounters.
- Puzzle-solving tension.
- Sudden but fair danger.
- Short periods of relief.

Do not make the monster chase the player continuously from the beginning.

Avoid unnecessary objective markers that reveal every secret.

Use lighting, sound, signage, and environmental storytelling to guide the player naturally.

---

# 12. VISUAL QUALITY AND PERFORMANCE

The final result must look polished while remaining performant.

### Lighting and rendering
- Use the rendering pipeline already configured in the project.
- Do not switch render pipelines without a compelling reason.
- Fix broken materials and missing shaders.
- Ensure metallic materials respond appropriately to the lighting.
- Use emission selectively.
- Configure shadows sensibly.
- Avoid excessive real-time lights.
- Use baked or mixed lighting where appropriate.
- Avoid unnecessary post-processing.
- Use volumetric effects only when supported and affordable.

### Performance
- Avoid creating excessive GameObjects.
- Avoid spawning particles indefinitely.
- Avoid unnecessary Update loops.
- Cache references where appropriate.
- Use object pooling for repeated effects if needed.
- Avoid expensive raycasts every frame when a lower-frequency check is sufficient.
- Optimize monster AI and navigation.
- Avoid excessive audio sources.
- Ensure the game remains responsive during reactor startup and monster encounters.

Target a stable and playable experience on a typical gaming laptop.

---

# 13. UNITY IMPLEMENTATION REQUIREMENTS

Before editing, inspect the project and identify:
- Unity version and rendering pipeline.
- Current scene structure.
- Existing player controller.
- Existing monster prefab and AI.
- Existing spacecraft controller.
- Existing interaction system.
- Existing door and puzzle scripts.
- Current reactor implementation.
- Input configuration.
- Audio assets.
- Materials and shaders.
- Existing lighting setup.
- NavMesh and collision setup.

Then implement the upgrades in a way that fits the existing architecture.

### Code requirements
- Use clean, maintainable C#.
- Avoid duplicate systems.
- Avoid unnecessary third-party dependencies.
- Preserve working functionality.
- Do not create missing references to scripts, scenes, prefabs, materials, or audio files.
- Do not delete assets without confirming they are unused.
- Do not break scene references.
- Use serialized fields for configurable values.
- Add sensible null checks and error handling.
- Prevent repeated input events from triggering duplicate state changes.
- Use coroutines or suitable state-based logic for sequences that unfold over time.
- Keep puzzle states and progression states consistent.
- Avoid hard-coded scene assumptions where possible.
- Ensure scripts compile under Unity 6.6.

If creating new scripts, use descriptive names such as:
- `EmergencyLightFlicker.cs`
- `MonsterAudioController.cs`
- `MonsterAIController.cs`
- `PlayerInteractionController.cs`
- `ReactorStartupController.cs`
- `ReactorCircuitPuzzle.cs`
- `SpacecraftController.cs`

These are suggested names, not a requirement to create duplicate scripts if equivalent systems already exist.

Use the project's existing naming conventions whenever possible.

---

# 14. REQUIRED VALIDATION AND TESTING

Do not stop after creating assets or writing scripts.

Inspect and test the actual result.

### Geometry checklist
- First-floor rooms connect correctly.
- Second-floor rooms connect correctly.
- No overlapping walls.
- No visible geometry gaps.
- No unwanted wall intersections.
- No light leaking through solid geometry.
- No missing colliders.
- No inaccessible rooms that are required for progression.

### Lighting checklist
- The environment is substantially darker and more atmospheric.
- Emergency lights flicker convincingly.
- Important interactables remain readable.
- No unexplained bright patches appear on walls.
- No broken materials or pink shaders remain.
- Lighting changes work during gameplay.

### Monster checklist
- Monster proportions are appropriate.
- Animations play correctly.
- Audio is spatialized.
- AI can navigate the station.
- The monster respects walls and doors.
- Detection, pursuit, searching, and attack states work.
- The monster does not become permanently stuck.
- Scripted scares do not break progression.

### Gameplay checklist
- The first exit-door puzzle is interactable.
- Puzzle controls respond reliably.
- Reactor startup works from beginning to end.
- The reactor cannot enter a broken state.
- The player can reach the second floor.
- Spacecraft controls are responsive.
- The game can reach its intended ending.

### Technical checklist
- Resolve all new compilation errors.
- Fix new runtime errors.
- Check the Unity Console.
- Check for missing references.
- Verify scene and prefab references.
- Check that audio assets load correctly.
- Test the game in Play Mode.
- Test the complete critical gameplay loop.

If a feature cannot be fully tested automatically, clearly identify the remaining manual test instead of claiming it works.

---

# 15. EXECUTION STRATEGY

Work in this order:

**PHASE 1 — AUDIT AND STABILIZATION**
Inspect the project, identify broken geometry, lighting problems, existing systems, and dependencies. Fix the most serious issues first.

**PHASE 2 — LEVEL EXPANSION**
Build the second floor and ensure all architectural connections, colliders, and navigation paths work.

**PHASE 3 — LIGHTING AND ATMOSPHERE**
Remove light leaks, fix wall-lighting problems, establish darker lighting, and implement red emergency-light flickering.

**PHASE 4 — MONSTER**
Improve the creature's visual design, animations, AI, navigation, and spatial audio.

**PHASE 5 — GAMEPLAY**
Fix the first-door interaction system, redesign the reactor startup puzzle, and improve spacecraft handling.

**PHASE 6 — AUDIO AND POLISH**
Add environmental ambience, interaction sounds, mechanical sounds, and carefully timed horror cues.

**PHASE 7 — TESTING**
Run through the complete gameplay loop and fix any regressions introduced by the changes.

Prioritize functionality, visual coherence, and atmosphere over adding unnecessary features.

If time is limited, complete each phase to a usable standard before moving to optional polish.

Do not merely generate a report or a list of suggested changes. Implement the changes in the actual Unity project wherever your tools permit.

If a change requires Unity Editor interaction that you cannot perform directly, explain the precise remaining action and provide the necessary implementation rather than pretending it has already been completed.

---

# FINAL CREATIVE DIRECTION

ECHO should feel like an abandoned spacecraft where the player is never completely certain whether they are alone.

The second floor should make the station feel larger and more interconnected.

The walls should look structurally correct, the boundaries should be seamless, and the lighting should be dark without visual artifacts.

The monster should be physically imposing and genuinely threatening.

Its footsteps should create tension before the player even sees it.

The red emergency lights should make corridors feel dangerous without becoming visually distracting.

The reactor should be a memorable interactive set piece.

The spacecraft should feel responsive and satisfying to control.

Every interaction should be clear, every puzzle should be fair, and the complete experience should feel intentionally designed.

**Final priority: a seamless environment, controlled dark lighting, a terrifying monster, polished audio, satisfying gameplay, and a complete playable experience.**

Inspect first. Implement carefully. Test everything. Preserve the existing project. Do not sacrifice functionality for visual effects.