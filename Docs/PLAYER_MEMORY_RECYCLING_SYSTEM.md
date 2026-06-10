# Player Memory Recycling System

## Purpose

Memory Recycler 3D now treats the player as another record source. The game begins as a city-memory restoration prototype, then reveals that the central archive also preserved the recycler's choices, hesitation, mistakes, and ending path.

## Recorded Behaviors

- First memory decision: preserve, delete, or reprocess.
- Total preserve/delete/reprocess counts.
- Puzzle mistake count.
- Per-memory puzzle mistake count.
- Per-memory restore abandonment count.
- Per-memory restoration time, measured while the restore puzzle is open.
- Total memory instability, derived from mistakes and abandoned restores.
- Time spent before reaching the ending.
- Time spent near the central archive terminal.

## Memory State Rules

The core loop separates memory state into three levels:

- Collected: the player interacted with a blue memory orb and added it to the logbook.
- Restored: the player solved the sentence puzzle successfully.
- Processed: the player chose preserve, delete, or reprocess.

Closing UI does not restore or process a memory. The central archive ending condition counts only processed memories, which means a memory must be restored and then explicitly decided.

## Ending Use

The central archive appends a "수거원 행동 기록" section to the ending. It can report lines such as:

- `수거원은 첫 번째 기억 "..."을 삭제했다.`
- `그는 같은 폐도시 안에서 문장을 3번이나 잘못 복원했다.`
- `그는 아픈 기억 "..."을 남기는 쪽을 택했다.`
- `아카이브는 그의 망설임까지 보존했다.`
- `아카이브는 그가 한 가지 원칙이 아니라, 매번 다른 죄책감으로 판단했다는 사실을 보존했다.`

The ending also appends per-memory archive testimony. Each memory can now provide decision-specific text, so the same recovered memory can become evidence, silence, or a softened but inconsistent replacement depending on the player's decision.

## Decision Presentation

- Preserve: original restored text remains as testimony.
- Delete: memory titles are partially masked and ending testimony begins with `증언 없음`.
- Reprocess: the restored text is shown as a memory-specific softened/reworked version, with a `기록 불일치` warning.

## Current Memory Arc

`MR3D_001` through `MR3D_008` now form one short narrative arc instead of isolated temporary memories.

- `MR3D_001`: the archive registers the recycler by number, not name.
- `MR3D_002`: Mia's blue umbrella reframes a discarded object as waiting.
- `MR3D_003`: the public announcement repeats safety while omitting disappeared names.
- `MR3D_004`: an unnamed citizen waits for a memory that is never returned.
- `MR3D_005`: a family photo reveals that a happy image was already edited.
- `MR3D_006`: the recycler's empty file reveals that the player is also a collection target.
- `MR3D_007`: the archive admits it edits truth into sentences the city can endure.
- `MR3D_008`: the final backup turns the last choice toward the recycler, not only the city.

## Puzzle Feedback

- Each `MemoryData3D` asset can define a short `puzzleHint`.
- Each memory can choose a `puzzleMode`: `Sequence`, `LensAlign`, `LensOcclude`, or `Stillness`.
- The puzzle panel shows the hint in the existing selected-sentence text area, so the teammate puzzle UI overlap fix is not disturbed.
- Memory assets store `sentencePieces` in correct narrative order, while the runtime puzzle displays those pieces in a stable shuffled order per memory.
- Wrong answers are recorded through `PlayerMemoryLog3D`.
- The puzzle panel shows memory stability: `안정`, `흔들림`, or `붕괴 직전`.
- Wrong answers increase memory instability and change feedback from unstable sentence text to archive classification text.
- Closing an unfinished puzzle leaves the memory collected but unrestored, records one restore abandonment, and shows an interruption toast.

## Memory Lens Puzzle

The lens puzzle now treats `Tab` as a memory lens overlay instead of a large logbook panel. Special memories spawn a runtime World Space Canvas echo near their memory orb. The echo billboards toward `Camera.main`, stays visible while the memory is unrestored, and becomes clearer while the lens overlay is active.

- `MR3D_002` uses `LensAlign`: align the world-space blue umbrella memory echo with the lens center marker for 0.7 seconds.
- `MR3D_003` uses `LensOcclude`: align the erased announcement echo with the center marker while the marker is also aimed at a city structure.
- `MR3D_006` uses `Stillness`: keep the empty-file echo inside the lens center and avoid movement/input for 5 seconds.

The old sequence puzzle remains the fallback for normal memories and for missing lens targets. Special lens puzzles no longer rely on the large puzzle panel as the primary interaction surface.

Lens success restores the memory through the same `MarkRestored` path as the sequence puzzle. `PlayerMemoryLog3D` records the solve method as `solvedBy`, for example `LensAlign`, `LensOcclude`, `Stillness`, or `Sequence`.

### Lens Controls and Feedback (Presentation Pass)

- `Tab` toggles the memory lens overlay. When a special memory is collected but not yet restored, the lens auto-targets the next pending special record.
- While the lens is on, the targeted world echo brightens (alpha `1.0`); while off it dims (alpha `0.42`), so the lens ON/OFF state reads clearly.
- The lens overlay status now always shows Korean guidance with a live center-distance readout, e.g. `렌즈 정렬: 0.0 / 0.7초 ... (중앙 거리 24px)`. The earlier English placeholder strings that overwrote the Korean feedback were removed.
- Stillness feedback shows `정지 상태 유지: x.x / 5.0초` and resets to a Korean "stop moving" message on disturbance.
- The HUD objective adds a Tab-lens tutorial line (`Tab으로 기억 렌즈를 켜고, 월드에 떠 있는 기억 잔상을 도시와 겹쳐 복원하세요.` and `특수 기억은 Tab 기억 렌즈로 복원`) only while a pending special memory exists, so first-time players learn the lens without manual setup.

### World Echo Lifecycle (Show / Hide Rules)

`MemoryLensEcho3D` visibility is driven purely by memory state (collected / restored / processed), never by interaction:

- `decision != Unchosen` (processed) → echo hidden.
- `restored == true` and not processed → echo shows a dim "복원 완료 / 처리 대기" state (title gets `· 복원 완료`, body shows the solved text, hint points to the memory card decision).
- otherwise (unrestored) → echo shows the active memory afterimage. Pressing `E` to collect the orb does **not** hide it.
- A non-destructive distance cull (`MaxVisibleDistance`) only toggles the canvas; it recovers when the player returns.

Previous bug: the echo called `canvas.gameObject.SetActive(false)` on itself, which stopped its own `Update()` and made it disappear permanently after the first out-of-range frame (and around collection/cinematic). The fix toggles `canvas.enabled` instead and keeps the GameObject alive, so the echo can always re-evaluate and recover.

State changes are also pushed explicitly via `MemoryLensEcho3D.NotifyStateChanged(memory, reason)` from collect, restore, and decision points, which emit `[MR3D LensEcho]` logs:

- `[MR3D LensEcho] keep visible on interact id=MR3D_002 restored=False decision=Unchosen`
- `[MR3D LensEcho] restored, show 복원 완료/처리 대기 id=MR3D_002 restored=True decision=Unchosen`
- `[MR3D LensEcho] hide after processed id=MR3D_002 decision=Preserve`

Restoration does not process the memory: processed counts only rise on an explicit preserve/delete/reprocess decision, so the 3-processed ending gate is unaffected.

### Runtime Debugging

The puzzle UI writes concise runtime logs with the `[MR3D PuzzleMode]` prefix.

- Opening a puzzle logs memory id, title, `puzzleMode`, restored state, decision state, and open source.
- Special modes log branch entry, such as `Enter LensAlign branch`.
- Lens modes log whether a world-space echo target was found.
- Already-restored special memories log that the puzzle was skipped and include the saved `solvedBy` value.

If a special puzzle appears to behave like a normal sequence puzzle, start a New Game or reset saved progress first. Previously restored memories skip puzzle UI by design.

The first pass uses `puzzleHint` plus code-defined lens success text. It does not add separate `lensHint`, `lensSuccessText`, or `lensRequiredHoldSeconds` serialized fields yet.

## Unfinished Memory Flow

- HUD displays `복원 대기 기억 N개` when collected memories have not been restored.
- The archive logbook marks memories as `[복원 대기]`, `[선택 대기]`, or `[처리 완료]`.
- The central archive terminal warns when unrestored memories are still making noise near the archive.
- `다음 처리할 기억 열기` prioritizes unrestored memories, then restored but undecided memories.
- When there is nothing actionable, the button changes to `처리할 기억 없음` and becomes disabled.

## Decision Preview

The restored memory card includes a short preview before the player chooses:

- Preserve keeps original testimony and preserves the wound.
- Delete turns testimony into a blank and prevents the memory from speaking in the ending.
- Reprocess makes the sentence easier to endure but leaves a record mismatch.

## Archive Progress Reactions

The central archive panel now adds an "아카이브 진행 반응" section. It reacts to:

- no decisions yet,
- partial progress toward the ending requirement,
- ending condition completion,
- first decision pattern,
- accumulated puzzle mistakes.

This keeps the archive useful before the ending and reinforces that the system is already classifying the player.

## Ending Behavior Sentence

The final line of `[수거원 행동 기록]` is selected from the current player behavior:

- archive stay of at least 20 seconds,
- 3 or more puzzle mistakes,
- preserve/delete/reprocess majority,
- balanced choices,
- default common line when no stronger pattern exists.

The ending body is displayed inside a scroll area, with fixed footer buttons for title, new game, and quit. The behavior report uses the short summary version so it stays readable during a presentation.

## Central Archive Terminal Visual

The central archive is the climax location (lens puzzles + self-record interrogation), so it gets a runtime "memory interrogation lens/terminal" dressing instead of looking like a placeholder cube.

- Built by `ArchiveTerminalVisual3D`, auto-spawned by `MR3D_Bootstrap` after scene load. No scene edit is required; pressing Play shows it. `Prototype3D.unity` is unchanged.
- It finds the existing `ArchiveTerminal3D` and builds decoration in world space around it (front faces the player approach on the −Z side): two vertical pillars, a top frame beam, a base beam, a dark lens backing, glowing teal accent lines, and a pulsing teal point light for dark-city readability.
- A world-space label canvas billboards to the camera with `CENTRAL ARCHIVE`, `기억 심문 터미널`, and `처리 완료 3개 이상 접속 가능`. The panel/emission/light brighten while the Tab lens overlay is active.
- All decoration primitives have their colliders removed, so player movement / grounding / physics are untouched. It does not add or modify any `ArchiveTerminal3D` trigger behavior.
- Materials are created at runtime (URP/Lit with emission, falling back to Standard), so no external assets are downloaded.
- Duplicate-guarded by a static instance + `FindFirstObjectByType`, and logs `[MR3D ArchiveVisual]` on build.

## Archive Self-Record Interrogation

Reaching the ending now runs a short interrogation before the ending screen, realizing the earlier `solvedBy` TODO. The central archive asks the recycler to recall their own actions, then folds the result into the ending.

- Entry: `ArchiveTerminal3D` → `UIManager3D.ShowEnding()` builds 2–3 questions from `PlayerMemoryLog3D`. If no question can be built, it falls back directly to `RenderEnding(...)`.
- Questions:
  - Q1 — `수거원이 처음으로 처리한 기억은 무엇입니까?` (correct = the first decided memory, from `firstDecisionMemoryId`).
  - Q2 — `수거원이 처음 선택한 처리 방식은 무엇입니까?` (correct = `firstDecision`: 보존/삭제/재가공, plus a `기억나지 않는다` decoy).
  - Q3 — lens-aware, only if a memory was actually solved by the lens (`solvedBy` ∈ {`LensAlign`, `LensOcclude`, `Stillness`}). Prompt varies: 도시와 겹쳐 복원 / 가려야 보이는 문장 / 멈춰서 열기.
- Option labels reuse the decision masking rules: preserved memories show the original title, deleted memories are masked via `ObscureTitle`, reprocessed memories are tagged `(재정리본)`. Correctness is tracked by id/flag, not by the displayed (masked) string.
- Options are shuffled with a stable per-memory seed so the correct slot is not always first.
- Result is appended to the ending as `[아카이브 자기기록 심문]`:
  - all correct → `수거원은 도시의 기억뿐 아니라 자신의 선택도 복원했다.`
  - majority correct → `... 자신이 한 선택의 일부만 기억했다.`
  - otherwise → `... 자신의 선택은 끝내 복원하지 못했다.`
  - common closing line → `아카이브는 도시의 기억뿐 아니라, 수거원이 기억을 복원한 방식까지 보존했다.`
- The interrogation panel blocks gameplay input and keeps the cursor unlocked (added to `IsAnyMajorPanelOpen`/`CloseAllMajorPanels`). Any answer always advances, so there is no soft-lock. Runtime logs use the `[MR3D Interrogation]` prefix (`begin`, `complete`).
- The ending gate is unchanged: the interrogation only runs after 3+ memories are processed; `ArchiveTerminal3D` still blocks early access.

## Completion Criteria (Presentation Build)

The prototype is considered presentation-complete when, from `Assets/MemoryRecycler3D/Scenes/Prototype3D.unity`, pressing Play allows a full start-to-ending run with no manual Editor setup:

1. Scene is pre-wired: one `UIManager3D`, `MemoryManager3D`, `GameState3D`, `ArchiveTerminal3D`, and 8 `MemoryObject3D` orbs referencing `MR3D_001`–`MR3D_008`. World echoes and the player memory log are created at runtime (`MemoryObject3D.Start` → `MemoryLensEcho3D.EnsureFor`, `MR3D_Bootstrap` → `PlayerMemoryLog3D.Ensure`), so no scene baking of those is required.
2. `MR3D_002` (LensAlign), `MR3D_003` (LensOcclude), `MR3D_006` (Stillness) run as world-space lens puzzles, each with a `Sequence` fallback if a lens anchor is missing.
3. Special-puzzle success sets `restored = true` only; processing (preserve/delete/reprocess) still requires an explicit decision, so the 3-processed ending gate is intact.
4. Ending runs the archive interrogation (or falls back) and reflects `PlayerMemoryLog3D` plus the lens solve method.
5. `dotnet build Assembly-CSharp.csproj` succeeds with 0 errors.

## Known Limitations / Presentation Talking Points

- The lens overlay center marker is screen-space; the puzzle is "aim the world echo into the center frame", not a literal silhouette overlap. Frame it as the lens "locking onto" the memory.
- `LensOcclude` needs a city structure behind the screen center; in open areas, point toward a building/archive. The Sequence fallback covers worst cases.
- Interrogation distractors come from the player's own collected memories, so with very few decided memories an option set can be small (it still includes `기억나지 않는다`).
- Per-memory serialized lens fields (`lensHint`, `lensSuccessText`, `lensRequiredHoldSeconds`) are still code-defined, not asset-authored.

## Memory Data Fields

`MemoryData3D` now includes:

- `puzzleMode`
- `puzzleHint`
- `preserveTestimony`
- `deleteTestimony`
- `reprocessedText`
- `reprocessWarning`

## Safety Notes

- Player movement, `manualVisualYOffset`, CharacterController size, Ground Probe, and Ground Snap are intentionally untouched.
- Runtime capsule auto-fit remains disabled.
- Runtime visual auto-grounding remains disabled.
- The feature is attached through existing memory, puzzle, archive, and ending flow points.

## QA Checklist

- Start a new game and confirm the player memory log resets.
- Collect a memory, open its puzzle, close it without solving, and confirm processed count does not increase.
- Reopen the same memory from the archive logbook and confirm it is still restorable.
- Solve one puzzle incorrectly, then correctly, and verify the ending mentions the mistake count.
- Confirm wrong-answer toast changes after repeated mistakes.
- Confirm puzzle hints appear without overlapping puzzle buttons.
- Confirm `MR3D_002` restores when the lens is aligned with the memory/world anchor.
- Confirm `MR3D_003` restores through the simplified occlusion/center alignment.
- Confirm `MR3D_006` restores after 5 seconds of stillness, and resets when input occurs.
- Confirm special puzzle success records `solvedBy` in `PlayerMemoryLog3D`.
- Confirm memory stability changes after wrong answers or abandoned restores.
- Choose preserve/delete/reprocess at least once each and verify the ending behavior report.
- Confirm the restored card shows preserve/delete/reprocess result previews before choosing.
- Delete one memory and confirm its title is partially masked in archive-style views.
- Reprocess one memory and confirm the ending testimony includes `기록 불일치`.
- Open the archive before the ending and confirm the progress reaction reflects current decision count.
- Confirm unfinished memories appear as `[복원 대기]` and do not unlock the ending.
- Confirm the ending body scrolls and does not overlap the footer buttons.
- Stand near the central archive for at least 20 seconds and confirm hesitation text appears.
- Confirm the HUD shows the Tab-lens tutorial line while a collected special memory is still unrestored, and that it disappears once restored.
- Confirm the lens overlay status is Korean with a live `(중앙 거리 NNpx)` readout (no English placeholder text).
- Process 3+ memories, enter the central archive, and confirm the interrogation appears before the ending screen.
- Answer all interrogation questions correctly and confirm the ending shows `자기기록 일치: N / N` with the "자신의 선택도 복원했다" line.
- Answer incorrectly and confirm the ending shows the mismatch line instead, and that the run still reaches the ending screen (no soft-lock).
- Solve `MR3D_002`/`003`/`006` with the lens (not the fallback) and confirm a lens-specific Q3 appears in the interrogation.
- Confirm the interrogation panel keeps the cursor visible and clickable, and that `[MR3D Interrogation]` logs `begin`/`complete`.
- Confirm the `MR3D_002`/`003`/`006` world echoes are visible while unrestored, and that pressing `E` to collect the orb does NOT hide the echo (`[MR3D LensEcho] keep visible on interact`).
- Confirm the echo stays visible throughout the lens puzzle, then switches to the dim "복원 완료 / 처리 대기" state on restore, and hides only after a preserve/delete/reprocess decision (`hide after processed`).
- Walk far from a special memory and back, and confirm the echo reappears (no permanent disappearance).
- Confirm the central archive shows the lens/terminal dressing (pillars, frame, glowing lines, `CENTRAL ARCHIVE` label) on Play with no Editor menu, and that it brightens with the Tab lens.
- Confirm the archive decoration does not block movement (walk through/around it) and the player grounding is unchanged.
- Confirm `Manual Visual Y Offset` remains `0.43` in the player script/scene.
