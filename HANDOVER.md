# Shift Magic Spell 인수 문서 (2026-11-20)

새 컴퓨터·새 세션에서 이어서 작업할 때 **이 파일을 가장 먼저** 읽는다. 그다음 `Docs/DESIGN.md`(기획 기준 문서)를 읽는다.

GitHub Game Off 2026(주제 SHIFT) 출품작. 마감은 한국시간 **12월 2일 06:37**(PST 12/1 13:37).

## 1. 지금 상태 한눈에
- **이름:** **Shift Magic Spell**(예전 이름 Finger Snap Shift). 11/20에 문서, 웹 프로토타입(`shift-magic-spell-prototype.html`), Unity 제품명, GitHub 저장소(`seolcw/shift-magic-spell`) 이름을 바꿨다. 에셋 생성 스크립트 안의 팔레트 파일 이름(`FingerSnapShift.gpl`)은 그대로 둔다.
- **기획:** 5차 규칙 + 공격 슬롯 + 단어 위치 점멸까지 확정. `Docs/DESIGN.md`가 기준이다.
- **웹 프로토타입:** 5차 규칙이 모두 들어가 있고 플레이 가능하다. 규칙 검사 20개 통과, 봇이 처음부터 끝까지 이긴다. **사람이 직접 해 본 난이도 검증은 아직 안 했다.**
- **도트 에셋:** 기존 9개 + 5차 추가 22개. Unity에서 그대로 임포트해 씬·프리팹에서 쓰고 있다.
- **Unity 프로젝트:** 이 저장소가 Unity 프로젝트 루트다(Unity 6000.0.58f2). `feature/unity-prototype`에 처음부터 승리까지 플레이 가능한 버전이 있다: 5차 규칙 전부, 효과음·배경음, 페이즈 전환 연출, 연습 구간, 960x600 커스텀 WebGL 템플릿. PlayMode 테스트(봇 플레이 포함)가 있다. 무엇을 어디서 고치는지는 `Docs/UNITY-STRUCTURE.md`, Unity에서 바꾼 수치는 `Docs/DESIGN.md` 7장. 11/21에 PR #1·#2로 `main`에 병합됐다.
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
| `Assets/Art/` | 도트 에셋 `.aseprite` 31개 (Tiles, Hand, Player, Fx, Ui, Font) | 참조되면 포함 |
| `ArtSource/Scripts/` | 에셋 생성 Lua: `build_assets.lua`(기존), `build_assets_v5.lua`(5차 추가), 미리보기 스크립트 2개, 공용 규칙 `fss_lib.lua` | X |
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
- **Aseprite가 있으면** 에셋을 다시 만들 수 있다: `Aseprite.exe -b --script-param root=<이 폴더> --script ArtSource/Scripts/build_assets.lua`(기존 9개), `build_assets_v5.lua`(5차 22개), 미리보기는 `build_preview.lua`·`build_preview_v5.lua`. 이 컴퓨터에서는 Steam 설치본 `C:\Program Files (x86)\Steam\steamapps\common\Aseprite\Aseprite.exe`(1.3.18)를 썼다. 다른 컴퓨터에서는 경로가 다를 수 있다. Aseprite에서 손으로 고친 파일은 스크립트로 다시 만들면 덮어써지니 주의.

## 5. 남은 일 (순서대로)
1. ~~이름 변경~~ (11/20 완료: Shift Magic Spell)
2. **Unity 이식** (Unity 6000.0.58f2, 2D Built-in, WebGL 960x600, 320x200 3배 확대), 브랜치 `feature/unity-prototype`:
   - 처음부터 끝까지 플레이 가능하게 만든 뒤 Play Mode로 확인하고, WebGL 빌드 zip(루트에 `index.html`)을 만들어 itch.io Draft 업로드로 웹 동작을 확인한다(업로드는 사용자가 직접).
   - 수치는 ScriptableObject로(`attackPatterns.json` 구조 그대로), 판 배치는 `layouts.json`·`layoutRules.js`의 마스크·슬롯 위치 그대로.
   - 패키지: Input System(키는 물리 위치로 읽음), Aseprite Importer(`com.unity.2d.aseprite`), 2D Pixel Perfect.
3. 사람 플레이 테스트로 난이도 맞추기(단어 길이, 공격 간격, 점멸 주기, 줄 돌리기 반복 속도).
4. 아직 안 정한 것(DESIGN.md 8장): 튜토리얼, 단어 거꾸로 인정 여부, 난이도 프리셋, 플레이어 정체, 결과 화면, 색맹 대응.
5. 아직 없는 에셋: 슬롯 깨기 전용 손 동작, 플레이어 순간이동·사망 모션, 타이틀·결과 화면. (사운드는 11/20에 추가됨)
6. 타격감(11/25, 브랜치 `feature/hit-feedback`): 멈춤·하얀 번쩍임·방향 흔들림·넉백·납작해짐·조각·빨간 테두리·소리 세기. 수치는 `Docs/DESIGN.md` 7장. 처음 한 번 **Tools > Shift Magic Spell > Add Hit Feedback To Main Scene (keeps edits)** 를 실행해 번쩍임 머티리얼·화면 테두리를 씬과 프리팹에 넣는다.

## 6. 새 세션에 줄 요청 (복사해서 사용)
```
이 폴더의 HANDOVER.md와 Docs/DESIGN.md를 먼저 읽고,
Docs/claude-personal-work-rules.md 규칙을 따라서 Shift Magic Spell 작업을 이어서 해 줘.
- 기획은 Docs/DESIGN.md가 기준이고, 로직은 WebPrototype/src/game.js와 attackPatterns.json을 참고해.
- 그림은 Assets/Art/의 .aseprite를 그대로 써. 목록은 DESIGN.md 9장.
- 무언가 고치기 전에 이해한 내용과 계획(브랜치 이름 포함)을 먼저 보여 주고 확인받아.
```
