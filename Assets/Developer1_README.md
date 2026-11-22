# Virtual Archery Range Developer 1

For Unity 6000.6.10f1, Universal 3D / URP, and XR Interaction Toolkit 3.6.1.
Uses two tracked controllers. No eye tracking or gaze input is required.
Your existing project name, Virtual Archery Range, is fine.

## Integrated Developer 2 game

Open **Assets/Scenes/ArcheryRange.unity** and press **Play**. This is the first enabled
build scene; the existing sample and Developer 1 test scenes remain available.
Use the main menu to choose Practice or Competitive.

- **Practice:** three fixed lanes at 6 m, 9 m and 12 m, unlimited arrows, no timer,
  the original tutorial and immediate feedback. Choose **End Practice** on the
  right-hand session panel to see final results.
- **Competitive:** one central target, 60 seconds, 12 arrows, and at least 50 points
  to win by default. Three consecutive blue-ring-or-better hits (6+ points each)
  earn the next distance: 6 m, then 9 m, then 12 m. A weak hit or miss resets the
  current streak; once a distance is earned, a later already-airborne miss cannot revoke it.
  All airborne arrows must land before the target slides back. Shooting and the timer
  pause during that slide. A round-ending shot never starts another target move.
- **Scoring:** gold 10, red 8, blue 6, black 4, white 2. Ground, backstop and flight
  timeouts are misses. Accuracy is hits divided by resolved shots; no shots means 0%.
- **Results:** final points, accuracy, targets hit, misses, arrows fired and competitive
  win/loss, plus the range reached. **Try Again** restarts the same mode;
  **Return to Menu** resets the session.
  Ending practice, exhausting arrows or reaching the time limit first disables shooting,
  then waits for all launched arrows to resolve before displaying the final statistics.

Without a headset, click the Game view: **1** selects Practice, **2** selects Competitive,
hold **right mouse** to aim, hold/release **Space** to draw/shoot, **E** ends Practice,
**Esc** returns to the menu, and **R** retries from Results. These keys replace the
P shortcut in the Developer 1 test scene. The bow is hidden on the desktop preview's
menu/results screens so it cannot obscure their text; VR bow rendering is unchanged.

**Left-click** the menu, session and results buttons. Mouse movement alone does not aim:
hold **right mouse** while moving. Keep the Game view focused and floating Editor
windows out of the way while testing.

The desktop **aim guide** follows the bow's direction, contracts as you draw and turns
gold at full strength. Its meter shows draw amount, not a predicted impact point:
arrows still drop. It hides on menus/results, while finishing airborne shots and while
the target moves, and is disabled in headset play and player builds. It cannot intercept clicks.

The **Cedar Hollow** woodland setting uses shared low-poly trees, layered ridges, warm
afternoon lighting, a timber shelter, fencing and a framed backstop. Practice has lane
guides and distance signs; Competitive replaces them with a single precision-course track.

Developer 2 files:

| File | Purpose |
| --- | --- |
| Assets/Scripts/Gameplay/GameManager.cs | Menu, mode and results lifecycle; existing Practice integration |
| Assets/Scripts/Gameplay/Target.cs | Circular five-ring scoring and hit flash through IArrowHitReceiver |
| Assets/Scripts/Gameplay/ScoreManager.cs | Bow shot tracking, pending shots, points, hits, misses and accuracy |
| Assets/Scripts/Gameplay/ChallengeManager.cs | Timer, arrow budget, finishing state and win threshold |
| Assets/Scripts/Gameplay/DifficultyManager.cs | Precision streaks, earned target slides and mode-specific layouts |
| Assets/Scripts/Gameplay/ResultsManager.cs | Frozen final session statistics |
| Assets/Scripts/Gameplay/GameUI.cs | Main menu, live scoreboard, draw meter and practice-panel visibility |
| Assets/Scripts/Gameplay/ResultsUI.cs | Results board, retry and menu controls |
| Assets/Scripts/Gameplay/DesktopRangeControls.cs | Mode-aware Editor keyboard shortcuts |
| Assets/Scripts/Gameplay/DesktopAimReticle.cs | Noninteractive Editor-only direction guide and draw feedback |
| Assets/Editor/Developer2/Developer2Setup.cs | Range/prefab generation and scene validation |
| Assets/Editor/Developer2/RangePresentationArt.cs | Shared low-poly woodland scenery, mode decoration and lighting |
| Assets/Prefabs/Target.prefab | Circular solid hit face, scoring rings and stand |
| Assets/UI/GameUI.prefab, Assets/UI/ResultsUI.prefab | World-space menus, scoreboard and results |
| Assets/UI/DesktopAimReticle.prefab | Desktop-only overlay sight, draw meter and hint |
| Assets/Materials/Developer2/ | Range and target materials |

To adjust the rules, select **Range Managers** in ArcheryRange: edit ChallengeManager's
Round Duration, Arrow Limit and Winning Score, or DifficultyManager's Hits To Advance,
Minimum Qualifying Points and Transition Duration. The course uses Distances 6/9/12 m
to match its static signs and track markers. The menu and HUD read the configured rules.

**Archery > Developer 2 > Create Range and Game Assets** creates missing Developer 2
assets and opens the range. Existing gameplay prefabs are preserved.
**Upgrade Range Presentation** rebuilds only the generated scenery, reconnects the two
mode decorations and adds a missing desktop reticle; it also restores the default
precision-course settings. Save modified scenes when prompted before switching.
**Validate Current Scene** checks references, circular target colliders, mode decoration,
noninteractive reticle, both UI input paths and the single VR rig/event system.
The Developer 1 runtime scripts, prefabs and VRTesting scene are reused rather than replaced.

### Integrated Unity verification

The installed Unity 6000.6.10f1 Editor compiled the updated game and ran a temporary
Play Mode scenario with real BowController releases and Arrow physics collisions.
All 42 checkpoints passed: fixed Practice lanes, single-target Competitive, blue-hit
qualification, weak-hit/miss streak resets, earned 6/9/12 m progression, smooth movement,
timer/shooting pause, waiting for another airborne arrow, cancel/restart isolation,
last-arrow completion without another slide, timeout results and reticle state/draw feedback.
A twelve-shot round produced 98 points, 11 hits, one miss and a win at the 12 m stage.
No runtime errors or exceptions occurred during that scenario. Practice, Competitive,
the moving target, expert range and results rendering were inspected after shader warmup.
Repeating the presentation upgrade and scene validation also passed.
Native mouse input started Practice and Competitive with the new overlay present;
right-drag changed camera orientation and a real Space hold/release registered one shot.
The temporary verification scripts were removed afterward.
This verifies the Editor preview, not physical headset tracking, controller-ray buttons,
comfort, or device builds; those still need testing on the intended VR hardware.

### Desktop mouse interaction fix

The original UI only had TrackedDeviceGraphicRaycaster, which accepts XR pointer data
but ignores ordinary mouse pointers. GameUI, ResultsUI and PracticeUI now also have a
standard GraphicRaycaster; their asset generators include it too. GameUI and ResultsUI
disable the mouse raycasters together with their hidden canvases to prevent invisible
panels intercepting clicks. XRUIInputModule and the tracked-device raycasters remain.
The teammate's runtime aiming, bow and VR controller code is unchanged.

Verified in the actual Game view with native mouse input: Start Practice, End Practice,
Try Again, results Return to Menu, Start Competitive and live HUD Return to Menu.
Right-drag changed the rendered camera rotation by about 38 degrees; a real Space
hold/release registered one shot. These checks exercise input dispatch, unlike the
original smoke scenario's direct button-event invocations.



## Import and run

1. Extract the ZIP. In Unity, choose **Assets > Import Package > Custom Package**.
   Select the included **VirtualArchery_Developer1.unitypackage** and import all items.
   Alternatively, merge the supplied Assets folder into your project's Assets folder.
   Use only one import method. Do not replace your whole Assets folder.
2. Open **Window > Package Management > Package Manager**. Select **XR Interaction Toolkit**.
   Under **Samples**, import **Starter Assets** for version 3.6.1. Wait for Unity to finish compiling.
   Importing the package itself and importing its sample assets are separate steps.
3. Choose **Archery > Developer 1 > Create Assets and Test Scene**.
   This creates the prefabs, materials and VRTesting scene using your installed package references.
   The scene files are generated inside Unity, rather than shipping guessed package GUIDs.
4. The setup opens **Assets/Scenes/VRTesting.unity**. Press **Play**.
   Practice starts automatically in this test scene.
5. Choose **Archery > Developer 1 > Validate Current Scene** to check required references.

The setup creates missing assets. It keeps existing prefabs and an existing VRTesting scene.
If you already made one of these files, check its references before using it with this code.
To deliberately rebuild a generated asset, move that asset out of its generated path first,
then run setup again. The setup does not change ArcheryRange.unity, Packages or ProjectSettings.

## Controls

### With a headset

- Reach for the bow handle and hold either controller's **grip** button.
- Put the other controller near the centre of the string, within about 20 cm.
- Hold that controller's **trigger**, pull backward, aim, then release the trigger.
- The arrow loads automatically. Pulling farther gives a faster shot.
- Release the grip to drop the bow; it returns to its starting stand position.
- Point at the practice panel with the controller's UI ray to use its buttons.
- **End Practice** stops new shots and waits for airborne arrows to land or time out.
  The statistics remain visible and Start Practice begins a fresh session.

The controller's tracked orientation determines the bow's aim. A trigger release with less
than 6 cm of draw cancels the shot. The default maximum draw is 50 cm.

### Editor preview without a headset

Click the Game view first. The test scene's DesktopPracticePreview enables itself only in
the Editor while no XR device is active. It is inactive in a built VR application.

- Hold **right mouse** and move the mouse to aim.
- Hold **Space** to draw; release **Space** to shoot.
- **E** ends practice; **P** starts another session.
- Space charges to full draw in approximately one second.

This preview checks gameplay flow. It does not replace headset/controller testing.
Remove or disable DesktopPracticePreview when using Unity's XR Device Simulator, because
the simulator may not report itself as an active XR display.

## Included files

| File | Purpose |
| --- | --- |
| Assets/Scripts/Player/BowController.cs | Grab, draw, release, automatic arrows, sound and controller vibration |
| Assets/Scripts/Player/Arrow.cs | Rigidbody flight, swept hit detection, sticking and flight timeout |
| Assets/Scripts/Player/ArrowHitInfo.cs | Shared target-hit interface and hit data |
| Assets/Scripts/Player/ArrowTargetRelay.cs | Optional Inspector connection for your teammate's target |
| Assets/Scripts/Player/VRPlayerSetup.cs | Binds controller transforms to bows and provides XR interaction/UI managers |
| Assets/Scripts/Player/PracticeTestTarget.cs | Feedback target used only by the test scene |
| Assets/Scripts/Gameplay/PracticeManager.cs | Unlimited, untimed practice and hit/miss statistics |
| Assets/Scripts/Gameplay/TutorialManager.cs | Grab, draw, shoot, observe feedback and try an adjustment |
| Assets/Scripts/Gameplay/PracticeUI.cs | Practice controls, statistics and draw meter |
| Assets/Scripts/Gameplay/DesktopPracticePreview.cs | Keyboard preview in the Editor |
| Assets/Editor/Developer1/Developer1Setup.cs | Creates and connects all Developer 1 assets |
| Assets/Audio/Developer1/ | Short generated release and test-hit sounds |

PracticeManager and TutorialManager are in Scripts/Gameplay because your assignment table
assigns those particular files to Developer 1. Your teammate keeps the other gameplay files.
All runtime scripts use the namespace **VirtualArchery**.

## Assets created by the setup command

- Assets/Prefabs/VRPlayer.prefab
- Assets/Prefabs/Bow.prefab
- Assets/Prefabs/Arrow.prefab
- Assets/UI/PracticeUI.prefab
- Assets/Scenes/VRTesting.unity
- Assets/Materials/Developer1/*.mat

VRPlayer is copied from XRI's imported Starter Assets and configured for a stationary player.
Locomotion and gaze components are disabled on this copy. It includes simple visible hands.
The imported Starter Assets remain unchanged and must stay in the project because the rig
uses their controller prefabs, scripts and input actions.

The bow and arrow models use Unity primitives and line renderers. No external model pack
is needed. The test target is created directly in VRTesting; it does not create or replace
Developer 2's Target.prefab or Target.cs. Arrow roots are at their tips and face local +Z.

## Add this to Developer 2's main scene

1. Open **Assets/Scenes/ArcheryRange.unity**.
2. Add **VRPlayer**, **Bow** and **PracticeUI** prefabs. Use one active XR Origin/camera rig.
3. Put VRPlayer at the shooting position, with its local +Z facing the targets.
   Put Bow within reach, for example 1.15 m above the floor and 0.55 m ahead of the player.
4. On PracticeUI's **PracticeManager** and **TutorialManager**, assign the scene's Bow.
   They can find the only active bow automatically, but explicit references are clearer.
5. Set PracticeManager's **Auto Start** OFF in the main scene. The test scene uses ON.
6. Connect the main menu's Practice button to **PracticeManager.StartPractice()**.
7. Connect PracticeManager's **On Practice Ended** event to your teammate's results or menu method.
   That method can read **practice.Summary** for shots, hits, misses and accuracy.

The PracticeUI prefab contains the practice manager and tutorial manager itself.
Do not also add duplicate instances of those managers elsewhere in the same scene.
If the main scene already has an EventSystem, use exactly one input module:
**XR UI Input Module**. Remove Standalone Input Module or Input System UI Input Module
from that EventSystem. A world-space canvas needs Tracked Device Graphic Raycaster,
which the supplied PracticeUI already includes.

## Target connection

An arrow recognises a target through **IArrowHitReceiver**, not a custom tag or layer.
Put the receiver on the hit collider's object or one of its parents. Use a solid collider,
not a trigger. All ordinary objects, the ground and flight timeouts count as misses.
Each arrow reports a result once, even if several collision checks see the same hit.

Developer 2 can add the interface and this method to their existing Target class:

```csharp
using UnityEngine;
using VirtualArchery;

// Add IArrowHitReceiver to your existing Target class declaration:
// public class Target : MonoBehaviour, IArrowHitReceiver

public void OnArrowHit(ArrowHitInfo hit)
{
    Vector3 localHit = transform.InverseTransformPoint(hit.Point);
    // Use localHit.x and localHit.y to calculate ring accuracy on a target facing local Z.
    // Call your own scoring/feedback method here.
}
```

**Alternative:** add **ArrowTargetRelay** to the target root. Its **On Hit (Vector3)**
Inspector event reports the world-space hit point; connect it to your teammate's public
hit/scoring method that accepts a Vector3. Choose one route to prevent double scoring.
Do not also award points from OnCollisionEnter for the same arrow.

Your teammate's scoring method name is not assumed. No fake Target.cs or ScoreManager.cs
is included, so these files compile before their implementation exists.

## Events for competitive mode and results

```csharp
// Starting competitive mode:
practice.CancelPractice();                 // Stop practice without emitting its results.
bow.SetShootingEnabled(true);

// Subscribe in your own component's OnEnable; unsubscribe in OnDisable:
bow.ShotFired += HandleShot;                // HandleShot(Arrow arrow)
bow.ShotResolved += HandleResolved;         // HandleResolved(ArrowHitInfo hit)

// When no more arrows may be fired:
bow.SetShootingEnabled(false);              // Also cancels a partially drawn shot.
```

Developer 2 owns competitive limits, timer, ring points, progression and win/loss results.
Use ShotFired for arrow counts and ShotResolved for hits/misses. For a final challenge
result, wait for launched arrows to resolve before calculating the final statistics.
ArrowHitInfo includes Arrow, Collider, Point, Normal and IsTarget.

PracticeSummary contains shots, hits, misses and Accuracy (0-100).
During practice, accuracy excludes arrows still in flight. End Practice waits for them,
so the final summary contains a result for every shot. Maximum flight time defaults to
8 seconds; embedded arrows are removed after 15 seconds to avoid accumulating objects.
Practice does not award ring points; Developer 2's target and score manager own those.

## Headset setup

- Keep the OpenXR plug-in enabled for the platform you are building for.
- In **Project Settings > XR Plug-in Management > OpenXR**, enable the interaction profile
  for each controller type you will actually test, then resolve Project Validation errors.
- Use **Input System Package (New)** or **Both** for Active Input Handling if Unity requests it.
- Keep Packages/manifest.json, Packages/packages-lock.json, ProjectSettings, Assets and
  asset .meta files in Git. Both developers need the same package versions and samples.
- On a Mac, use the Editor preview for local checks. For a standalone headset such as Quest,
  build to its Android target with the headset's required build support and device setup.
  PC VR testing requires a supported desktop OS/runtime. Merely enabling OpenXR does not
  make every headset available for live Play Mode on macOS.

Headsets with two tracked grip/trigger controllers are the intended input. Controllerless
headsets or hand-only input need a different interaction implementation.

## Quick checks in Unity

1. Setup opens a scene with a bow, target, ground and practice panel.
2. In Play Mode, draw and release; stronger draw should produce a flatter, faster shot.
3. An arrow touching the test target flashes it and adds one hit. Ground or timeout adds one miss.
4. Firing several arrows and ending practice waits for their results before showing the summary.
5. Starting another practice resets the statistics and tutorial.
6. Releasing the bow while drawing cancels the shot and returns the bow to its stand.
7. Test holding with both left and right hands on the actual headset. Check drawing near the face.
8. Test the world-space UI with a controller ray; check that the End Practice button works.

## Verification completed before packaging

- All supplied C# files passed syntax parsing.
- The actual PracticeManager and TutorialManager were compiled and exercised in a lightweight
  C# harness: 8 tests passed for pending shots, duplicate callbacks, session reset, unlimited
  practice, disabling, tutorial progression and repeated End calls.
- XRI API names and Starter Assets structure were checked against the 3.6.1 package source.

The Unity Editor and a VR headset were not available in the authoring workspace. Unity
import/compilation, prefab generation, physics and headset behaviour still require the
checks above in your installed Editor. The included scene validator checks references
after Unity creates the assets.

## Reference documentation

- https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.6/manual/samples-starter-assets.html
- https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.6/manual/xr-ui-input-module.html
- https://docs.unity3d.com/6000.0/Documentation/Manual/choose-collision-detection-mode.html
