# Development Metrics

Baseline snapshot: **2026-11-08**  
Unity version: **6000.2.8f1**  
Measurements below describe the current working tree.

## Current baseline

| Metric | Count | Counting rule |
| --- | ---: | --- |
| Narrative event assets | 5 | `GameEventDefinition` assets in `Assets/Resources/Data/Events`. |
| Event stages | 6 | `GameEventStage` entries across those event assets. |
| Event decision choices | 16 | `GameChoiceDefinition` entries (`choiceId`) in event assets. |
| Dialogue assets | 22 | `DialogueDefinition` assets in `Assets/Resources/Data/Conversations`. |
| Dialogue response options | 45 | `DialogueResponse` entries (`playerResponse`) across dialogue assets. |
| Total selectable options | 61 | Event decision choices plus dialogue response options. These categories are also reported separately above. |
| Production C# files | 12 | All `.cs` files under `Assets/Scripts`. |
| Production C# lines | 1,540 physical / 1,343 active | Physical counts every line; active excludes blank and comment-only lines. Inline comments are included. |
| Test C# files | 1 | All `.cs` files under `Assets/Tests`. |
| Test C# lines | 285 physical / 252 active | Same counting rule as production C#. |
| Automated Edit Mode cases | 9 | `[Test]` methods in `GameStateTests.cs`; the project developer reported 8 passed on the first run. The failing missing-round case used a choice fixture without required result text; the fixture is corrected and that case needs a rerun. |

## Recounting rules

- Recount event and dialogue option totals from their Unity assets when content changes.
- Recount C# files and lines from `.cs` source only; do not include `.meta`, `.asmdef`, generated project files, or package code.
- Report physical and active C# lines together so the counting method remains clear.
