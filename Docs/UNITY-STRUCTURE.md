# Unity 구조: 무엇을 어디서 고치나 (2026-11-20)

원칙: **모양·위치·문구·수치처럼 고정된 것은 씬·프리팹·에셋에 두고 에디터에서 고친다.** 런타임에 만드는 것은 개수나 모양이 매번 바뀌어서 미리 둘 수 없는 것뿐이다.

## 1. 에디터에서 고치는 것

| 고치고 싶은 것 | 어디서 |
| --- | --- |
| 게임 수치(예고 시간, 손 속도, 공격 간격, HP, 콤보, 이동·줄 돌리기 쉬는 시간 `MoveDelay`·`RotateDelay` 등), 페이즈별 단어·공격, 마법 색·크기 | `Assets/Data/AttackPatterns.asset` (플레이 중에도 바꾸면 바로 반영) |
| 시작 배치·마스크·슬롯 위치 | `Assets/Data/BoardLayouts.asset` (마스크를 바꾸면 Board_Left/Right에서 ⋮ > **Rebuild Cells**) |
| 판 위치·칸 크기, 색(글자·경고·힌트·덮개 등), 공용 스프라이트 | `Assets/Resources/ViewAssets.asset` |
| 키 모양·글자 위치·힌트 바탕·맞음 테 | `Assets/Prefabs/Board/Key.prefab` |
| 빈 홈, SHIFT 칸(쿨다운 덮개), 공격 슬롯(블록·아이콘·흔들림) | `Assets/Prefabs/Board/KeyHome`, `ShiftKey`, `Slot` |
| 플레이어(서 있는 높이, 깜빡임, 테두리) | `Assets/Prefabs/Actors/Player.prefab` |
| 손(그림 위치, 떠 있는 높이, 그림자, 줄 섞기 당김·튕김, 맞았을 때 튕김) | `Assets/Prefabs/Actors/Hand.prefab` |
| 예고 칸, 소환 탄, 떠오르는 글자 모양 | `Assets/Prefabs/Fx/WarningCell`, `Bullet`, `FloatText` |
| 화면 글자(HP 칸, 페이즈, 손 HP, 목표 단어 판, 슬롯 안내, 토스트, 배너)의 위치·문구·색 | `Main` 씬 > `Hud` > `Content` 아래 오브젝트 |
| 시작·일시정지·쓰러짐·클리어·승리 화면 문구·배치 | `Main` 씬 > `Screens` 아래 패널(평소에는 Title만 켜져 있음. 고칠 때 잠깐 켜기) |
| 타격감 세기(멈춤·흔들림·번쩍임·납작해짐·넉백·조각 수·소리 크기와 높이), 상황별(플레이어 피격·사망, 마법·콤보·80%·마무리, 블록 금·깨짐, 내려치기), 흔들림·번쩍임 전체 배율 | `Assets/Data/AttackPatterns.asset` > `Hit Feel` |
| 흔들림 진동 빠르기·옆 떨림, 조각 퍼짐·속도·중력, 마법이 날아가는 비율·궤적 배율, 연출 프리팹 연결 | `Main` 씬 > `Game` > `GameRenderer` |
| 빨간 화면 테두리(두께·투명도, HP 1일 때 뛰는 빠르기) | `Main` 씬 > `Hud` > `HitEdge` |
| 엔딩 단계 시각·자막 문구 | `Assets/Data/AttackPatterns.asset` > `Ending` |
| 엔딩 커서 떨림·깜빡임·두리번·달아나는 방향 | `Assets/Prefabs/Actors/Hand.prefab` (HandView, 엔딩 항목) |
| 엔딩 펑 연기 시간·달아나는 잔상 | `Main` 씬 > `Game` > `GameRenderer` (엔딩 연출) |
| 엔딩 자막 위치·색 | `Main` 씬 > `Screens` > `Ending` |
| 하얀 번쩍임 머티리얼 | `Assets/Art/Generated/SpriteFlash.mat`(셰이더 `Assets/Shaders/SpriteFlash.shader`), `ViewAssets`의 `Flash Material` |
| 카메라(320x200, PPU 16, 픽셀 퍼펙트) | `Main` 씬 > `Main Camera` |

- 글자(`PixelText`)는 5x7 도트 글꼴이라 **영어 대문자·숫자·기호**만 나온다(`0-9 + - ! ? : / % × → ← ↑ ↓ ⇧`, 마침표). 문구의 `{0}`, `{1}`은 코드가 값을 채우는 자리다(툴팁에 무엇이 들어가는지 적혀 있음).
- 앞뒤 순서(Sorting Order) 기본값은 `RenderOrderUtils`에 있고, 각 컴포넌트·프리팹의 인스펙터 값으로 바꿀 수 있다.

## 2. 런타임에 만드는 것 (그리고 이유)

| 무엇 | 왜 미리 둘 수 없나 | 모양은 어디서 |
| --- | --- | --- |
| 키의 글자·위치 | 줄 돌리기로 키가 칸 사이를 계속 옮겨 다님(키 오브젝트 자체는 씬에 있고 위치·글자만 바뀜) | Key 프리팹 |
| 예고 칸, 소환 탄, 떠오르는 글자 | 개수·위치가 공격마다 다름 → 프리팹을 풀에서 꺼내 씀 | Fx 프리팹 |
| 줄 돌리기 잔상·화살표, 단어 자리 점선, 조준선, 드래그 사각형, 순간이동 고리, 맞춤 번쩍임 | 모양(길이·각도·크기)이 매 프레임 계산됨 → 즉시 그리기 캔버스(`Canvas_*`) | 색은 ViewAssets |
| 마법 연출(불덩이, 번개, 광선 등) | 궤적·입자 수가 마법마다·프레임마다 다름 | 스프라이트는 `Assets/Art/Fx`, 색·크기·개수는 AttackPatterns의 spells, 비율은 GameRenderer |
| 타격감 조각, 번쩍임 값, 넉백·납작해짐 | 맞을 때마다 위치·방향이 다름. 값은 `GameSession.Feel`(HitFeedback)이 정하고 View가 읽음 | 세기는 AttackPatterns의 Hit Feel, 퍼짐은 GameRenderer |
| 글자·9-slice 조각·HP 칸 스프라이트 | 문구·길이·칸 수에 따라 바뀜(숨은 자식으로 만들고 씬에 저장하지 않음) | PixelText / NineSliceSprite / CountRowView 인스펙터 |

## 3. Tools > Shift Magic Spell 메뉴

| 메뉴 | 하는 일 | 덮어쓰나 |
| --- | --- | --- |
| Apply Aseprite Import Settings | `.aseprite` 임포트 설정(PPU 16, 점 필터, 왼쪽 아래 피벗) | 임포트 설정만 |
| Build Art Catalog | `.aseprite` 태그·프레임 시간을 읽어 `ArtCatalog` 갱신(그림을 고친 뒤 실행) | 목록만(직접 고칠 값 없음) |
| Create View Assets (keeps edits) | 흰 점·그림자·테두리 PNG와 ViewAssets가 없으면 만듦 | 아니오 |
| Import Prototype Data (overwrites asset values) | 웹 JSON으로 수치·배치 에셋을 덮어씀(확인 창) | **예** |
| Recreate Main Scene (overwrites scene edits) | 씬을 처음 상태로 다시 만듦(확인 창). 수치 에셋·프리팹은 있으면 그대로 | **씬만** |
| Add Ending To Main Scene (keeps edits) | Screens 아래에 엔딩 자막(Ending)이 없으면 만든다. 배치 모드 `EndingSetupUtils.SetupAll`은 임포트 설정·스프라이트 목록·소리 목록도 함께 갱신 | 씬은 없을 때만 추가 |
| Add Hit Feedback To Main Scene (keeps edits) | 번쩍임 머티리얼을 만들어 ViewAssets와 프리팹(플레이어·손·슬롯)에 붙이고, Hud에 HitEdge가 없으면 만든다 | 프리팹 머티리얼만, 씬은 없을 때만 추가 |
| Build WebGL | WebGL 빌드(Gzip, Fallback 끔, 960x600, 스플래시 끔) | 아니오 |

## 4. 코드 구조

- `Assets/Scripts/Core`: 순수 로직(MonoBehaviour 없음, 칸 단위 좌표 = 웹 px ÷ 50). 화면과 무관하게 테스트한다.
- `Assets/Scripts/View`: `GameRenderer`(매 프레임 반영) → `Board/`(BoardView, KeyView…), `Actors/`(PlayerView, HandView), `Ui/`(PixelText, HudView, ScreensView…), `Fx/`(PrefabPool, WarningCellView), `PixelCanvas`(즉시 그리기).
- `Assets/Scripts/Editor`: 위 메뉴와 처음 한 번 씬·프리팹을 만드는 도구(`SceneSetupUtils`, `PrefabSetupUtils`, `HudSetupUtils`).
