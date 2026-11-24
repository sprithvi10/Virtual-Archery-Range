# Virtual Archery Range

A single-player archery app developed in Unity for **DTI6300 — Foundations of Immersive Virtual Reality**. Shoot from a fixed position in a forest range, practise at different distances, or compete against the clock.

## Controls

### VR controllers

Use a headset with two tracked controllers. The following controls use the project's default grip and trigger bindings.

| Action | Control |
| --- | --- |
| Select a menu button | Point the controller's UI ray at the button and press the trigger. |
| Hold the bow | Reach for the handle and hold the grip button. |
| Draw the string | Bring the other controller near the centre of the string, hold its trigger, and pull backward. |
| Shoot | Aim while holding the bow, then release the drawing hand's trigger. |
| Drop the bow | Release the grip button. |
| Change practice distance | Point at the distance slider, hold the trigger, and drag it. |

Arrows load automatically during play. The nearby **power bar** shows draw strength. Pulling farther produces a faster arrow and a flatter trajectory. Aim by aligning the bow and arrow with the target.

### Desktop preview

When testing in the Unity Editor, click the **Game** view before using keyboard shortcuts.

| Action | Control |
| --- | --- |
| Start Practice from the main menu | `1` |
| Start Competitive from the main menu | `2` |
| Aim | Hold the right mouse button and move the mouse. |
| Draw / increase power | Hold `Space`. |
| Shoot | Release `Space`. |
| End Practice and view results | `E` |
| Retry the current mode | `R` |
| Return to the main menu | `Esc` |
| Use buttons or the practice distance slider | Left-click buttons or drag the slider. |

## Game modes

### Practice

- Shoot at one target with **unlimited arrows** and **no timer**.
- Use the practice menu slider to adjust the target distance from **6 to 12 metres** during the session.
- Review live score, hits, misses, accuracy, and arrows still in flight.
- Select **End Practice** to finish the session and view the results.
- Select **Try Again** to start another session, or **Return to Menu** to choose a mode.

Practice is an open session for experimenting with aim, power, and distance.

### Competitive

- Reach **at least 50 points** within a round with a **60-second time limit** and **12 arrows**.
- The target starts at **6 metres** and can progress to **9 metres**, then **12 metres**.
- Land **three consecutive hits worth 6 or more points each** to unlock the next distance.
- A hit worth fewer than 6 points or a miss resets the precision streak.
- A final score of 50 or more points wins the round. A final score below 50 is a loss.

Watch the timer, arrows remaining, score, current range, and precision streak on the competitive panel.

## Scoring

Both modes award points according to the target ring hit.

| Target ring | Points |
| --- | ---: |
| Gold / centre | 10 |
| Red | 8 |
| Blue | 6 |
| Black | 4 |
| White / outer ring | 2 |
| Miss | 0 |

## Practice results

After ending practice, the results screen shows the final score, arrows fired, hits, misses, and accuracy. Accuracy measures the percentage of shots that hit the target.

The shot diagram groups recorded target impacts by distance.

| Diagram marker | Meaning |
| --- | --- |
| White dots | Recorded target impact positions at the displayed distance. |
| `+` | Average impact position for that distance. |
| Circle | RMS spread, shown for groups with at least three plotted impacts. A smaller spread indicates a tighter group. |

Each diagram describes the plotted impacts at its displayed distance. The session statistics cover the whole practice session. Use the grouping and centre offset to decide how to adjust your aim on the next attempt.
