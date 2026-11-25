# Shift Magic Spell 인수 문서 (2026-11-25 갱신)

새 컴퓨터·새 세션에서 이어서 작업할 때 **이 파일을 가장 먼저** 읽는다. 그다음 `Docs/DESIGN.md`(기획 기준 문서)를 읽는다.

GitHub Game Off 2026(주제 SHIFT) 출품작. 마감은 한국시간 **12월 2일 06:37**(PST 12/1 13:37).

## 1. 지금 상태 한눈에
- **이름:** **Shift Magic Spell**(예전 이름 Finger Snap Shift). 11/20에 문서, 웹 프로토타입(`shift-magic-spell-prototype.html`), Unity 제품명, GitHub 저장소(`seolcw/shift-magic-spell`) 이름을 바꿨다. 에셋 생성 스크립트 안의 팔레트 파일 이름(`FingerSnapShift.gpl`)은 그대로 둔다.
- **기획:** 5차 규칙 + 공격 슬롯 + 단어 위치 점멸까지 확정. `Docs/DESIGN.md`가 기준이다.
- **웹 프로토타입:** 5차 규칙이 모두 들어가 있고 플레이 가능하다. 규칙 검사 20개 통과, 봇이 처음부터 끝까지 이긴다. **사람이 직접 해 본 난이도 검증은 아직 안 했다.**
- **도트 에셋:** 기존 9개 + 5차 추가 22개 + 엔딩 2개(`Hand/CursorShift`, `Fx/Poof`). Unity에서 그대로 임포트해 씬·프리팹에서 쓰고 있다.
- **Unity 프로젝트:** 이 저장소가 Unity 프로젝트 루트다(Unity 6000.0.58f2). `feature/unity-prototype`에 처음부터 승리까지 플레이 가능한 버전이 있다: 5차 규칙 전부, 효과음·배경음, 페이즈 전환 연출, 연습 구간, 960x600 커스텀 WebGL 템플릿. PlayMode 테스트(봇 플레이 포함)가 있다. 무엇을 어디서 고치는지는 `Docs/UNITY-STRUCTURE.md`, Unity에서 바꾼 수치는 `Docs/DESIGN.md` 7장. 11/21에 PR #1·#2로 `main`에 병합됐다.
- **타격감 (11/25):** 맞는 순간 멈춤(히트스톱, 멈춘 동안 입력은 예약)·하얀 번쩍임·방향 흔들림·넉백·납작해짐·조각·빨간 화면 테두리·소리 세기. 브랜치 `feature/hit-feedback`, PR로 올라가 있다(병합 전).
- **엔딩 (11/25):** SHIFT 명중 뒤 거대한 손이 금 가고 → 모래시계 → 텍스트 커서 I → 작은 화살표로 바뀌어 달아나는 약 7초 연출 + 효과음 Crack·Pop·Zip(jsfxr). Space·Enter·클릭으로 건너뛰기. 브랜치 `feature/ending-cursor-shift`(hit-feedback 위에 쌓음), PR로 올라가 있다(병합 전). **병합 순서: hit-feedback → ending.**
- **테스트:** PlayMode 74개 전부 통과(11/25). 엔딩 영상은 녹화 테스트(Explicit)로 만들 수 있다(4장).
- **사용자 안내:** 무언가 고치기 전에 계획(브랜치 이름 포함)을 먼저 보여 주고 확인받는다. 커밋은 작업 단위로 나눈다.

## 2. 게임 요약 (자세한 건 DESIGN.md)
거대한 마우스 커서(손)가 키보드 2개 위의 플레이어를 공격한다. 플레이어는 공격을 피하면서
1. **Shift+방향키로 서 있는 행·열을 한 칸씩 돌려**(Loopover식 순환) 목표 공격 단어(FIRE, MAGIC…)를 한 줄로 맞춘다.
2. 완성한 단어는 **반대편 판의 공격 슬롯**에 블록으로 들어간다. 그 슬롯에 가서 **Space**로 발동하면 단어별 마법이 커서를 친다. 1.5초 안에 연달아 쏘면 콤보, 슬롯이 꽉 차 있으면 80%로 바로 발동.
3. 3페이즈를 지나 손 HP가 5 이하가 되면 마지막 단어 **SHIFT**로 끝낸다.
- 손은 글자를 바꾸지 않는다. 피해 공격 위주 + 가끔 줄 섞기 + 슬롯 깨기.
- 단어 글자의 위치는 4초마다 1초만 깜빡여 보여 준다(도망다니면서 찾게).
- 왼쪽·오른쪽 판은 파란 SHIFT 칸을 밟아 순간이동한다.

## 3. 폴더
| 경로 | 내용 | Unity 빌드 포함 |
| --- | --- | --- |
| `HANDOVER.md` | 이 문서 | X |
| `README.md` | 영어 소개(Game Off 방문자용): 게임 설명, 조작, 빌드, 크레딧, 라이선스 | X |
| `Docs/DESIGN.md` | **기획 기준 문서** (5차 + 공격 슬롯 + 점멸 + 에셋 목록 + 일정) | X |
| `Docs/claude-personal-work-rules.md` | **작업 규칙.** 새 세션도 따른다(한국어, 수정 전 계획 확인, 커밋은 요청할 때만, AI 표시 없음 등). 새 Unity 프로젝트의 `CLAUDE.md`에 붙여 넣을 내용 | X |
| `Docs/HANDOVER-README.md` | 11/19 첫 인수 문서. **예전 규칙(3·4차)과 예전 경로 기준이라 참고만** | X |
| `Assets/Art/` | 도트 에셋 `.aseprite` 33개 (Tiles, Hand, Player, Fx, Ui, Font) | 참조되면 포함 |
| `ArtSource/Scripts/` | 에셋 생성 Lua: `build_assets.lua`(기존), `build_assets_v5.lua`(5차 추가), `build_assets_ending.lua`(엔딩), 미리보기 스크립트 3개, 공용 규칙 `fss_lib.lua` | X |
| `ArtSource/Preview/` | `asset-sheet*.png`, `game-preview*.png` 확인 그림 | X |
| `ArtSource/Export/` | `.aseprite`를 가로 시트 PNG + JSON으로 내보낸 백업 | X |
| `WebPrototype/` | 웹 프로토타입. `shift-magic-spell-prototype.html`을 브라우저로 열면 바로 플레이 | X |
| `WebPrototype/src/` | 원본: `game.js`(Unity 구조와 같은 모듈), `attackPatterns.json`(모든 수치·단어·마법), `layoutRules.js`, `layouts.json`, 봇 `simtest.js`, 검사 `verify-rules.js` | X |

## 4. 다른 컴퓨터에서 확인하는 법
- **웹 프로토타입:** `WebPrototype/shift-magic-spell-prototype.html`을 브라우저로 연다(서버 필요 없음). 아래 디버그 패널에 수치 슬라이더, 무적, 페이즈 건너뛰기가 있다.
- **Node.js가 있으면** (`WebPrototype` 폴더에서):
  ```
  node src/build.js          # src → html 한 장으로 합치기
  node src/verify-rules.js   # 규칙 검사 20개 (모두 PASS여야 함)
  node src/simtest.js 4 dodge 0.6   # 봇 4판, 0.6초마다 행동
  ```
- **Unity 테스트**(에디터를 닫고): `Unity.exe -batchmode -projectPath <이 폴더> -runTests -testPlatform PlayMode -testResults results.xml -logFile test.log`. 캡처는 `Logs/Screens/`.
- **엔딩 영상:** `-testFilter ShiftMagicSpell.Tests.EndingVideoRecordTests.RecordEndingFrames`로 녹화 테스트만 돌리면 `Logs/Video/ending/`에 30fps 프레임과 실제로 낸 소리 기록(`sounds.tsv`, `music.tsv`)이 남는다. ffmpeg로 프레임을 3배(nearest)로 키우고, 소리를 기록된 프레임 시각·크기·높이로 섞어 MP4로 만든다(11/25 영상은 이 방법으로 만듦).
- **11/25 작업 PC:** Unity 6000.0.58f2(WebGL 모듈은 아직 없음: 웹 빌드 전에 Hub에서 추가), Aseprite(Steam), ffmpeg 9.1(winget `Gyan.FFmpeg`). 효과음은 npm `jsfxr` 1.4.1로 만든다(설정은 `Assets/Audio/Sfx/README.md`, 기존 Click.wav를 같은 바이트로 다시 만들어 확인함).
- **Aseprite가 있으면** 에셋을 다시 만들 수 있다: `Aseprite.exe -b --script-param root=<이 폴더> --script ArtSource/Scripts/build_assets.lua`(기존 9개), `build_assets_v5.lua`(5차 22개), 미리보기는 `build_preview.lua`·`build_preview_v5.lua`. 이 컴퓨터에서는 Steam 설치본 `C:\Program Files (x86)\Steam\steamapps\common\Aseprite\Aseprite.exe`(1.3.18)를 썼다. 다른 컴퓨터에서는 경로가 다를 수 있다. Aseprite에서 손으로 고친 파일은 스크립트로 다시 만들면 덮어써지니 주의.

## 5. 남은 일 (순서대로)
1. ~~이름 변경~~ (11/20 완료), ~~Unity 이식~~ (11/21 병합), ~~사운드~~ (11/20), 타격감·엔딩(11/25, PR 병합 대기)
2. **PR 병합:** `feature/hit-feedback` → `main` 먼저, 그다음 `feature/ending-cursor-shift`(병합 후 대상을 `main`으로 바꾸거나 이어서 병합). 병합은 사용자가 요청할 때만.
3. **WebGL 빌드 → itch.io Draft 업로드**로 웹 동작 확인(빌드 PC에 WebGL 모듈 필요, 업로드는 사용자가 직접). 기획 일정상 11/24 예정이었다.
4. 사람 플레이 테스트로 난이도 맞추기(단어 길이, 공격 간격, 점멸 주기, 줄 돌리기 반복 속도, 타격 멈춤·흔들림 세기).
5. 아직 안 정한 것(DESIGN.md 8장): 단어 거꾸로 인정 여부, 난이도 프리셋, 플레이어 정체, 색맹 대응. 설정 화면이 생기면 "흔들림·번쩍임 줄이기"(`HitFeel.ShakeScale`·`FlashScale`)를 연결.
6. 아직 없는 에셋: 슬롯 깨기 전용 손 동작, 플레이어 순간이동·사망 모션, 타이틀 화면 그림.
7. 엔딩 아이디어 중 남은 것(11/25 제안): 둘로 나뉜 키보드가 하나로 합쳐짐, 플레이어 정체 공개(텍스트 커서가 THANKS FOR PLAYING을 침), 카메라가 빠지며 책상 위 고양이가 보이는 반전.

## 6. 새 세션에 줄 요청 (복사해서 사용)
```
이 폴더의 HANDOVER.md와 Docs/DESIGN.md를 먼저 읽고,
Docs/claude-personal-work-rules.md 규칙을 따라서 Shift Magic Spell 작업을 이어서 해 줘.
- 기획은 Docs/DESIGN.md가 기준이고, 로직은 WebPrototype/src/game.js와 attackPatterns.json을 참고해.
- 그림은 Assets/Art/의 .aseprite를 그대로 써. 목록은 DESIGN.md 9장.
- 무언가 고치기 전에 이해한 내용과 계획(브랜치 이름 포함)을 먼저 보여 주고 확인받아.
```
