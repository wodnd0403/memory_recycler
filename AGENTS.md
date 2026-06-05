# AGENTS.md

Memory Recycler 3D 저장소에서 Codex와 다른 AI 에이전트가 안전하게 작업하기 위한 규칙입니다.

## Project Overview

- 프로젝트명: Memory Recycler 3D
- 엔진/버전: Unity `6000.3.12f1` (`ProjectSettings/ProjectVersion.txt` 기준)
- 렌더 파이프라인: Unity 6 URP 기반
- 장르/형태: 3D 폐도시 탐험 프로토타입
- 핵심 루프: 기억 구체 수집 -> 문장 복원 퍼즐 -> 보존/삭제/재가공 선택 -> 중앙 아카이브 엔딩
- 메인 씬: `Assets/MemoryRecycler3D/Scenes/Prototype3D.unity`

## Important Files

- `Assets/MemoryRecycler3D/Scripts/Memory/MemoryManager3D.cs`
- `Assets/MemoryRecycler3D/Scripts/UI/UIManager3D.cs`
- `Assets/MemoryRecycler3D/Scripts/World/ArchiveTerminal3D.cs`
- `Assets/MemoryRecycler3D/Scripts/Player/ThirdPersonPlayer3D.cs`
- `Assets/MemoryRecycler3D/Scripts/Player/OrbitCamera3D.cs`
- `Assets/MemoryRecycler3D/Scripts/World/PlayBoundary3D.cs`
- `Assets/MemoryRecycler3D/Scripts/Editor/`
- `Assets/MemoryRecycler3D/Scenes/Prototype3D.unity`

## Required Workflow

1. 작업 전 반드시 `git status`를 확인한다.
2. 현재 브랜치를 확인하고, `main` 브랜치에서 직접 큰 기능 개발을 하지 않는다.
3. 작업 전 관련 파일을 먼저 분석하고, 수정 계획을 사용자에게 제안한다.
4. 발표 안정성을 최우선으로 둔다. 대규모 리팩토링보다 최소 수정과 회귀 방지를 우선한다.
5. 기존 동작을 바꾸는 경우 재현 절차와 예상 영향을 먼저 설명한다.
6. 작업 후 반드시 `git diff --stat`과 `git diff`를 확인한다.
7. 커밋 전에 stage할 파일 목록을 사용자에게 보여준다.
8. 사용자가 명시적으로 요청한 경우에만 커밋한다.
9. 사용자가 명시적으로 요청하기 전까지 push하지 않는다.
10. 커밋 메시지는 한국어로 작성한다.

## Unity Scene Safety

- 사용자가 승인하기 전까지 `Assets/MemoryRecycler3D/Scenes/Prototype3D.unity`를 수정하지 않는다.
- `Prototype3D.unity` 변경이 필요한 경우 먼저 이유, 예상 영향, 되돌릴 수 있는 방법을 설명한다.
- Unity 씬 파일 변경과 C# 코드 변경을 한 커밋에 과하게 섞지 않는다.
- 씬 변경이 필요한 작업은 가능한 한 작은 단위로 나누고, 변경 파일 목록을 명확히 남긴다.
- 씬 직렬화 변경은 diff가 매우 커질 수 있으므로 단순 저장만으로 생긴 변경인지 주의해서 확인한다.

## Editor Automation Safety

`Assets/MemoryRecycler3D/Scripts/Editor/`에는 씬 생성, 정리, 맵 구성 자동화 메뉴가 있다.

사용자가 명시적으로 요청하기 전까지 다음 계열 기능을 실행하지 않는다.

- destructive scene build
- cleanup
- regenerate
- rebuild
- full scene reconstruction
- prototype scene destructive build

특히 아래 메뉴는 임의로 실행하지 않는다.

- `Tools/Memory Recycler 3D/Advanced/Build Prototype Scene (Destructive)`
- `Tools/Memory Recycler 3D/Advanced/Clean Prototype Scene`
- 씬 오브젝트를 대량 생성/삭제/재배치하는 Editor 자동화

`MemoryRecycler3DSceneBuilder.cs`, `MR_ProposalMapCompositionPass.cs`, 기타 SceneBuilder/Editor 자동화 스크립트는 사용자가 승인하기 전까지 수정하지 않는다.

## Files Not To Commit

다음 파일과 폴더는 커밋하지 않는다.

- `Library/`
- `Temp/`
- `Logs/`
- `obj/`
- `UserSettings/`
- 자동 생성된 `*.csproj`
- 자동 생성된 `*.sln`
- 임시 파일, 캐시 파일, 로컬 IDE 설정

`.gitignore`가 있어도 커밋 전 `git status`와 stage 목록을 직접 확인한다.

## Development Priorities

1. 발표 5~10분 루프가 막히지 않게 유지한다.
2. 새 기능보다 저장/이어하기, 퍼즐 재진입, 엔딩 진입, 맵 경계, 카메라/이동 안정성을 우선한다.
3. 퍼즐, 엔딩, UI 변경은 `MemoryManager3D.cs`, `UIManager3D.cs`, `ArchiveTerminal3D.cs`의 현재 흐름을 먼저 이해한 뒤 최소 범위로 수정한다.
4. 맵/충돌 문제는 `PlayBoundary3D.cs`, `ThirdPersonPlayer3D.cs`, `OrbitCamera3D.cs`, 씬 배치 사이의 영향을 함께 검토한다.
5. 개인 엔딩, 등장인물 연출, 퍼즐 재미 보강은 발표 안정성 확인 후 단계적으로 진행한다.

## Verification Checklist

작업 후 가능한 범위에서 다음을 확인한다.

- 새 게임 시작 가능
- 이어하기/저장 상태 유지
- WASD 이동, Shift 달리기, 점프
- E 상호작용으로 기억 구체 수집
- 기억 카드에서 복원 퍼즐 진입
- 퍼즐 닫기 후 아카이브에서 다시 진행 가능
- 보존/삭제/재가공 선택 반영
- 처리 완료 기억 3개 이상에서 중앙 아카이브 엔딩 진입
- 엔딩 후 타이틀로/새 게임/종료 흐름
- 맵 밖 이탈 또는 낙하 시 안전 복귀

Unity 실행 검증을 하지 못한 경우, 최종 보고에 검증하지 못한 항목을 명확히 적는다.
