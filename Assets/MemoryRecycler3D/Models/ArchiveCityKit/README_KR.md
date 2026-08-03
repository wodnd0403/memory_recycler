# Memory Recycler 3D - Archive City Kit

레퍼런스의 폐허가 된 brutalist sci-fi 도시 분위기를 기준으로 제작한 절차 생성 3D 모델 세트입니다.
`Prototype3D.unity` 및 기존 프리팹에는 배치하지 않았습니다.

## 생성 모델

| 모델 | 용도 | 기준 크기 (W x D x H) |
|---|---|---|
| `CentralMemoryArchive.obj` | 중앙 랜드마크 아카이브 | 약 50 x 32 x 42m |
| `RuinedLowrise_A.obj` | 저층 폐건물 | 약 13.1 x 10.2 x 10.9m |
| `RuinedMidrise_B.obj` | 중층 폐건물 | 약 10 x 9.8 x 19.6m |
| `RuinedTower_C.obj` | 원거리 타워형 폐건물 | 약 12 x 11.2 x 37m |
| `MemoryStreetLamp_A.obj` | 청록 발광 가로등 | 약 2.1 x 0.8 x 5.8m |

모든 모델은 Y-up, `1 unit = 1m`, 바닥 중앙 피벗, 정면 `-Z` 기준입니다.

## Material Slot

- `MR_Concrete`: 콘크리트 `#34383A`
- `MR_ConcreteDark`: 어두운 손상 콘크리트
- `MR_DarkMetal`: 암흑색 금속 `#161B1D`
- `MR_SecondaryMetal`: 보조 금속 `#252C2F`
- `MR_WindowDark`: 꺼진 창문
- `MR_CyanEmission`: 주 발광 `#35E6E6`
- `MR_DimCyan`: 약한 발광 `#197D82`
- `MR_Rust`: 녹 강조색 `#684B3B`
- `MR_Debris`: 콘크리트 파편

Unity에서 `MR_CyanEmission`과 `MR_DimCyan`은 URP/Lit 머티리얼로 바꾸고 Emission을 켜야 실제 발광과 Bloom이 표시됩니다.

## 아카이브 그룹

`MainTower`, `LeftWing`, `RightWing`, `Entrance`, `ArchiveSymbol`, `Windows`,
`CyanLights`, `Pipes`, `ExteriorPanels`, `DamageParts`, `Debris`로 분리되어 있습니다.
복잡한 MeshCollider 하나 대신 그룹별 단순 BoxCollider를 구성할 수 있습니다.

## 다시 생성

프로젝트 루트에서:

```powershell
python Tools/ModelGeneration/generate_archive_city_kit.py
```

미리보기까지 생성하려면 Pillow가 설치된 Python으로 다음을 실행합니다.

```powershell
python Tools/ModelGeneration/generate_archive_city_kit.py --preview
```

## Blender / FBX Export

Blender가 설치된 환경에서:

```powershell
& blender.exe --background --python Tools/Blender/create_central_archive.py -- --blender-export --preview
```

실행하면 다음 파일을 생성합니다.

- `SourceAssets/Blender/ArchiveCityKit.blend`
- `Assets/MemoryRecycler3D/Models/ArchiveCityKit/Generated/*.fbx`

현재 저장소 환경에서는 Blender 실행 파일을 찾지 못했으므로 OBJ/MTL만 실제 검증했습니다.

## Unity Import

프로젝트를 열면 `Generated` 폴더의 OBJ와 MTL이 자동으로 임포트됩니다. 모델 Inspector에서 Scale Factor가 `1`인지 확인합니다. 씬 적용 전에는 Project 창의 모델 Preview에서 방향, 머티리얼 슬롯, 바운드를 확인합니다.

## 런타임 폐허 도시 확장

`ArchiveCityBackdropExpansion3D`가 중앙 아카이브를 기준으로 반경 86m, 132m, 180m의 폐건물 링을 생성합니다.
배치 전 각 모델의 실제 바운드를 검사하고 최소 6m 여유를 확보하며, 겹치는 후보는 각도와 반경을 바꿔 다시 배치합니다.
도시 지면은 480m 크기, 플레이 경계는 반경 225m로 확장되며 모두 `Prototype3D` 실행 중에만 생성됩니다.

- 기존 씬 파일과 플레이어 이동 시스템은 수정하지 않습니다.
- 중앙 아카이브와 모든 교체·확장 건물에는 단순 BoxCollider를 사용합니다.
- 가로등에는 기둥 중심의 CapsuleCollider를 사용해 플레이어가 통과하지 않도록 합니다.
- 저장 위치가 건물 내부라면 실행 시 안전 복귀 지점으로 이동시킵니다.
- 중·원거리 건물은 그림자 생성을 끄고 모델을 반복 사용해 렌더링 부담을 줄였습니다.
- 확장 도시만 되돌리려면 `ArchiveCityBackdropExpansion3D.ExpandedCityEnabled`를 `false`로 변경합니다.
