# Player Memory Recycling System

## Purpose

Memory Recycler 3D now treats the player as another record source. The game begins as a city-memory restoration prototype, then reveals that the central archive also preserved the recycler's choices, hesitation, mistakes, and ending path.

## Recorded Behaviors

- First memory decision: preserve, delete, or reprocess.
- Total preserve/delete/reprocess counts.
- Puzzle mistake count.
- Per-memory puzzle mistake count.
- Time spent before reaching the ending.
- Time spent near the central archive terminal.

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
- The puzzle panel shows the hint in the existing selected-sentence text area, so the teammate puzzle UI overlap fix is not disturbed.
- Memory assets store `sentencePieces` in correct narrative order, while the runtime puzzle displays those pieces in a stable shuffled order per memory.
- Wrong answers are recorded through `PlayerMemoryLog3D`.
- The first wrong answer gives the memory hint.
- Repeated wrong answers produce archive-style feedback that makes the game feel like it is watching the player struggle.

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

## Memory Data Fields

`MemoryData3D` now includes:

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
- Solve one puzzle incorrectly, then correctly, and verify the ending mentions the mistake count.
- Confirm wrong-answer toast changes after repeated mistakes.
- Confirm puzzle hints appear without overlapping puzzle buttons.
- Choose preserve/delete/reprocess at least once each and verify the ending behavior report.
- Delete one memory and confirm its title is partially masked in archive-style views.
- Reprocess one memory and confirm the ending testimony includes `기록 불일치`.
- Open the archive before the ending and confirm the progress reaction reflects current decision count.
- Stand near the central archive for at least 20 seconds and confirm hesitation text appears.
- Confirm `Manual Visual Y Offset` remains `0.43` in the player script/scene.
