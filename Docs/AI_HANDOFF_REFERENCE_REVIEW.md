# AI 인수인계: Docs/temp 레퍼런스 분석 작업

> **이 문서만 읽으면 다음 AI가 바로 이어서 작업할 수 있어야 한다.**
> 매 배치 종료 시, 그리고 세션 중단 전에 반드시 최신화한다.

## 1. 현재 작업 목적

`Docs/temp`의 이전 차시 최종보고서/PPT 레퍼런스 46개를 분석해
Memory Recycler 3D **최종보고서/발표 PPT** 제작에 참고할 자료를 선별한다.
산출물은 `Docs/REFERENCE_REVIEW_FOR_FINAL_REPORT.md`에 누적 정리한다.

## 2. 현재 브랜치 / git 상태

- 브랜치: `main` (origin/main과 동기화된 상태에서 시작, 커밋 `261bc44`)
- 작업 시작 시 작업 트리 clean
- 이 작업은 **Docs 문서 3개만 생성/수정/커밋**한다. 게임 코드/씬/에셋 금지.
- **push 금지** (이 작업에서는 커밋만)

## 3. 분석 대상

- 폴더: `Docs/temp/` (46개 파일: pptx 39, hwp 3, docx 4)
- 전체 목록과 상태: `Docs/REFERENCE_REVIEW_PROGRESS.md` (단일 진실 소스)

## 4. 진행 상황

- 이미 분석한 파일: 1~25 (배치 1~3 완료. 19/21.hwp만 파싱 실패)
- 다음 분석 대상: 번호 26 (`26.docx`)부터 — 남은 파일 21개
- A급 후보: 7.pptx (제안서 vs 최종 비교 프레임 + 달성도 체크리스트; 25.pptx가 그 제안서 짝)
- B급 후보: 2, 4, 5, 6, 8, 9, 11, 12, 13, 14, 17, 20, 22, 24, 25 (9/24는 Unity 자료, 17은 MR3D 렌즈와 컨셉 동형)
- 제외 후보: 1, 3, 10, 15, 16, 18, 23
- 파싱 실패: 19.hwp, 21.hwp (OLE 바이너리 — 수동 열람 필요)
- 파싱 실패: 없음 (hwp 3개는 실패 예상 → 파일명/크기로 추정 기록 예정)
- 주의: Python stdout이 cp949라 한글이 깨짐 → 스크립트에
  `sys.stdout.reconfigure(encoding="utf-8", errors="replace")` 필수 (이미 반영됨)

## 5. 판단 기준

- **A급**: 목차 흐름이 완결적(개요→기획→구현→시연→회고)이고, 기술 설명과 시연 구성의
  밀도/배분이 Memory Recycler 3D(내러티브 퍼즐 탐험 게임)에 직접 응용 가능한 자료
- **B급**: 일부 섹션(예: 트러블슈팅 정리법, 데모 슬라이드 배치)만 참고할 자료
- **제외**: 주제가 동떨어지거나(비게임/단순 과제), 내용이 빈약하거나, 중복 파일
- 게임 장르가 비슷하거나(3D 탐험/퍼즐/내러티브), 발표 흐름이 좋은 자료를 우선

## 6. 분석 방법 (도구)

- pptx/docx는 ZIP+XML이므로 Python으로 텍스트 추출:
  - 추출 스크립트: `C:\Users\sdjsd\AppData\Local\Temp\mr3d_extract.py`
    (없으면 아래 "스크립트 재생성" 참고 — pptx: `ppt/slides/slide*.xml`의 `<a:t>`,
    docx: `word/document.xml`의 `<w:t>` 추출, 슬라이드당 220자/총 6000자 제한)
  - 실행: `python C:/Users/sdjsd/AppData/Local/Temp/mr3d_extract.py "Docs/temp/1.pptx"`
- hwp는 OLE 바이너리라 파싱 불가 예상 → `Failed to Parse` 처리, 파일명/크기로 추정만
- **원본 파일 절대 수정 금지** (읽기 전용)

### 스크립트 재생성 (없을 때)
Python 3 + zipfile + 정규식으로 `<a:t>`(pptx) / `<w:t>`(docx) 텍스트 런을 추출해
슬라이드 번호별로 출력하는 단순 스크립트를 OS temp에 새로 만들면 된다.
저장소 안에 만들지 말 것.

## 7. 다음 AI가 바로 실행할 명령어

```bash
cd c:/Users/sdjsd/Desktop/OTT-Project/memory_recycler
git status --short
git branch --show-current
ls Docs/temp/
# 진행표에서 Not Started인 다음 파일 확인 후:
python C:/Users/sdjsd/AppData/Local/Temp/mr3d_extract.py "Docs/temp/<다음파일>"
```

## 8. 다음에 해야 할 작업

1. `Docs/REFERENCE_REVIEW_PROGRESS.md`에서 `Not Started`인 가장 앞 번호부터 8~10개 분석
2. 각 파일: 슬라이드 수/주제/목차 흐름 파악 → A/B/제외 등급 + 한 줄 요약
3. 배치 종료 시 세 문서 갱신:
   - `REFERENCE_REVIEW_PROGRESS.md` (상태/등급/요약)
   - `REFERENCE_REVIEW_FOR_FINAL_REPORT.md` (배치 기록 + A/B/제외 누적)
   - `AI_HANDOFF_REFERENCE_REVIEW.md` (이 문서의 4번 진행 상황 + 배치 로그)
4. Docs 문서 3개만 `git add` 후 커밋 (`docs: 레퍼런스 분석 N차 배치 정리`)
5. 모든 파일이 Done/Skipped/Failed to Parse가 되면 최종 구조 제안 작성

## 9. 주의사항

- 한 번에 끝내려 하지 말고 배치(8~10개) 단위로 진행, 배치마다 커밋
- `최종보고서 .pptx`는 파일명 끝에 공백이 있음 — 따옴표 필수
- `32 (1).pptx` vs `32.pptx` 중복 여부 비교 필요
- 요약은 구조 설명만, 원문 문장 복사 금지 (표절 방지 원칙은 REFERENCE_REVIEW_FOR_FINAL_REPORT.md 참조)
- 커밋 전 `git diff --stat`과 `git status --short`로 Docs만 변경됐는지 확인

## 10. 절대 하면 안 되는 작업

- `Docs/temp/` 원본 파일 수정/이동/삭제
- push, force push, `git reset`, `git clean`, 강제 checkout
- 게임 코드/씬/에셋 수정 및 커밋
- 레퍼런스 문장/이미지/코드/표 그대로 복사

## 11. 이어받기 프롬프트 (세션 중단 시 다음 AI에게 전달)

```text
이전 AI가 Docs/temp 레퍼런스 분석을 진행하다가 중단되었습니다.
먼저 아래 파일을 읽고 이어서 작업하세요.

1. Docs/AI_HANDOFF_REFERENCE_REVIEW.md
2. Docs/REFERENCE_REVIEW_PROGRESS.md
3. Docs/REFERENCE_REVIEW_FOR_FINAL_REPORT.md

해야 할 일:
- REFERENCE_REVIEW_PROGRESS.md에서 Not Started 상태인 다음 파일부터 분석
- 한 번에 최대 8~10개만 분석
- 배치 완료 후 세 문서 갱신
- Docs 문서만 커밋
- push 금지
- 원본 레퍼런스 파일 수정 금지
```
