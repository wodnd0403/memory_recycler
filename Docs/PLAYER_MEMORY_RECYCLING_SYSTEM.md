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

## Decision Presentation

- Preserve: original restored text remains as testimony.
- Delete: memory titles are partially masked and ending testimony becomes `증언 없음`.
- Reprocess: the restored text is shown as softened/reworked, with a `기록 불일치` warning.

## Safety Notes

- Player movement, `manualVisualYOffset`, CharacterController size, Ground Probe, and Ground Snap are intentionally untouched.
- Runtime capsule auto-fit remains disabled.
- Runtime visual auto-grounding remains disabled.
- The feature is attached through existing memory, puzzle, archive, and ending flow points.

## QA Checklist

- Start a new game and confirm the player memory log resets.
- Solve one puzzle incorrectly, then correctly, and verify the ending mentions the mistake count.
- Choose preserve/delete/reprocess at least once each and verify the ending behavior report.
- Delete one memory and confirm its title is partially masked in archive-style views.
- Reprocess one memory and confirm the ending testimony includes `기록 불일치`.
- Stand near the central archive for at least 20 seconds and confirm hesitation text appears.
- Confirm `Manual Visual Y Offset` remains `0.43` in the player script/scene.
