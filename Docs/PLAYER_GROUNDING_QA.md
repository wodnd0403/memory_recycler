# Memory Recycler 3D 플레이어 접지 QA

작성일: 2026-06-05

## 변경 목적

발표 시연 중 플레이어가 지면 위를 자연스럽게 걷는 느낌을 강화한다.

특히 다음 문제를 줄이는 것이 목적이다.

- 플레이어가 평지 또는 낮은 턱 위에서 묘하게 떠 보이는 문제
- 일부 폐도시 오브젝트나 지형에 발이 파묻혀 보이는 문제
- 경사면, 계단, 잔해 주변에서 `CharacterController.isGrounded` 판정만으로 접지감이 불안정한 문제

## 원인 요약

현재 플레이어 이동은 `ThirdPersonPlayer3D`의 `CharacterController.Move()` 기반이다.

기존 구조는 `controller.isGrounded`와 `CollisionFlags.Below`에 크게 의존했다. 이 방식은 단순 평면에서는 충분하지만, 폐도시 맵처럼 MeshCollider, 낮은 턱, 얇은 장식, 경사면이 섞인 환경에서는 지면과 캡슐 사이에 작은 틈이 생기거나 반대로 비주얼 모델이 낮게 보일 수 있다.

또한 Mixamo 외부 모델은 `autoGroundExternalVisual`로 캡슐 바닥에 맞춰지는데, 발 본 위치와 애니메이션 포즈가 매 프레임 달라지면 visual offset이 누적되어 떠 보임/파묻힘이 반복될 수 있다.

## 수정한 방식

`ThirdPersonPlayer3D`에 Ground Probe 기반 보정을 추가했다.

- 캡슐 하단 기준으로 짧은 `SphereCast`를 수행한다.
- Trigger Collider는 무시한다.
- 플레이어 자신과 `MR3D_PlayerVisual` 레이어는 무시한다.
- 너무 가파른 표면은 지면으로 보지 않는다.
- 지면과의 간격이 작을 때만 아래 방향으로 부드럽게 snap한다.
- 점프 직후에는 일정 시간 ground snap을 막아 점프를 방해하지 않는다.
- 외부 Mixamo 모델의 visual Y 보정은 기준 위치 주변으로 clamp해 누적 드리프트를 줄인다.

`PlayBoundary3D` 기본값도 발표 맵 기준으로 조정했다.

- `radius`: `320` -> `125`
- `floorY`: `-25` -> `-8`
- `ceilingY`: `220` -> `120`

이 값은 씬에 `PlayBoundary3D`가 직접 배치되어 있지 않고 런타임 부트스트랩이 생성하는 경우 적용된다.

## Inspector 튜닝 값

`Player_Recycler`의 `ThirdPersonPlayer3D`에서 확인한다.

| 값 | 역할 | 기본값 |
| --- | --- | --- |
| `enableGroundProbe` | Ground Probe 사용 여부 | `true` |
| `groundProbeMask` | 지면 탐지 레이어 | `Everything` |
| `groundProbeRadius` | 캡슐 하단 SphereCast 반경 | `0.24` |
| `groundProbeDistance` | 아래 방향 탐지 거리 | `0.55` |
| `groundSnapMaxDistance` | snap 허용 최대 거리 | `0.32` |
| `groundSnapSpeed` | 아래 방향 보정 속도 | `18` |
| `groundProbeSlopeLimit` | 지면으로 인정할 최대 경사 | `50` |
| `groundStickVelocity` | grounded 상태 유지용 하강 속도 | `-3.5` |
| `jumpGroundProbeGraceTime` | 점프 직후 snap 차단 시간 | `0.16` |
| `visualGroundClampRange` | 외부 모델 Y 보정 허용 범위 | `0.09` |

문제가 남을 때는 다음 순서로 조정한다.

1. 떠 보임이 남으면 `groundSnapMaxDistance`를 소폭 올린다.
2. 낮은 턱에서 덜컥거리면 `groundSnapSpeed`를 소폭 낮추거나 `groundProbeRadius`를 줄인다.
3. 발이 파묻히면 `externalFootGroundOffset`을 낮추거나 `visualGroundClampRange`를 줄인다.
4. 경사면에서 부자연스러우면 `groundProbeSlopeLimit`을 `CharacterController.slopeLimit` 근처로 맞춘다.

## Play Mode QA 체크리스트

### 시작과 기본 이동

- [ ] 새 게임 시작 후 플레이어가 시작 위치에 정상 배치된다.
- [ ] 정지 상태에서 발이 지면 위에 자연스럽게 놓인다.
- [ ] WASD 이동 중 발이 지속적으로 떠 보이지 않는다.
- [ ] Shift 달리기 중 모델이 지면과 과하게 벌어지지 않는다.
- [ ] 점프 후 착지 시 발이 지면에 부드럽게 붙는다.

### 지형별 확인

- [ ] 중앙 아카이브 광장 평지에서 접지감이 안정적이다.
- [ ] 주거 구역 잔해 주변에서 허공 충돌이나 발 파묻힘이 없다.
- [ ] 학교/행정 구역의 낮은 턱과 계단형 오브젝트에서 이동이 끊기지 않는다.
- [ ] 경사면 또는 비스듬한 폐허 조각 위에서 튀거나 미끄러지는 느낌이 과하지 않다.
- [ ] 바닥처럼 보이는 곳에 Collider가 없어서 떨어지는 구간이 없는지 확인한다.
- [ ] 보이지 않는 Collider 때문에 허공에서 막히는 지점이 없는지 확인한다.

### 발표 루프

- [ ] 기억 구체 3개 이상 회수 가능하다.
- [ ] 기억 카드에서 복원 퍼즐에 진입 가능하다.
- [ ] 퍼즐을 닫은 뒤 아카이브에서 다시 처리할 기억을 열 수 있다.
- [ ] 보존/삭제/재가공 선택이 정상 기록된다.
- [ ] 처리 3개 이상 후 중앙 아카이브 엔딩에 진입 가능하다.
- [ ] ESC 메뉴, BGM 옵션, 종료 버튼이 정상 동작한다.
- [ ] 맵 밖으로 멀리 이탈하거나 낙하하면 안전 위치로 복귀한다.

## 되돌릴 커밋 단위

문제가 생기면 다음 단위로 되돌린다.

- `fix: 발표용 플레이어 이동 안정성 보강`
  - `PlayBoundary3D` 기본 복귀 범위 조정만 되돌린다.
- `fix: 플레이어 지면 접지 보정 추가`
  - Ground Probe, Ground Snap, 외부 비주얼 clamp를 되돌린다.

문서 커밋과 코드 커밋은 분리되어 있으므로 기능 되돌림 시 문서는 남겨둘 수 있다.

## NavMesh 사용 범위

현재 플레이어를 `NavMeshAgent`로 바꾸지 않는다.

NavMesh는 다음 용도로만 참고한다.

- NPC 이동을 추가할 때 걷기 가능 영역을 만들기
- 발표 맵의 걸을 수 있는 표면을 시각적으로 검증하기
- 바닥처럼 보이지만 실제로 걸을 수 없는 구역을 찾아내기

NavMesh Bake가 필요할 경우에는 먼저 별도 브랜치 또는 깨끗한 작업 트리에서 진행한다. Bake 결과는 `Prototype3D.unity`와 NavMeshData asset/meta를 만들 수 있으므로, 코드 커밋과 반드시 분리한다.

이번 접지 안정화는 `CharacterController + Ground Probe + Ground Snap` 방식으로 해결한다.
