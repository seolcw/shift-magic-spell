using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using ShiftMagicSpell.View;
using UnityEditor;
using UnityEngine;

namespace ShiftMagicSpell.Editor
{
    /// <summary>
    /// 씬을 처음 만들 때 HUD(Hud)와 전체 화면 패널(Screens)을 배치한다. 좌표는 320x200 화면 픽셀(왼쪽 위 원점).
    /// 만든 뒤에는 씬에서 위치·문구·색을 직접 고친다.
    /// </summary>
    public static class HudSetupUtils
    {
        private const float CENTER_X = 160f;

        public static HudView BuildHud()
        {
            ViewColors colors = ViewAssets.Instance.Colors;
            var root = new GameObject("Hud");
            HudView hud = root.AddComponent<HudView>();
            GameObject content = Child(root, "Content", 0f, 0f);

            CountRowView playerHp = Child(content, "PlayerHp", 8f, 6f).AddComponent<CountRowView>();
            SetFields(playerHp, ("_sheet", "HpCell"), ("_fullTag", "Full"), ("_emptyTag", "Empty"), ("_spacingPx", 9f), ("_previewCount", 5));
            PixelText phase = AddText(content, "Phase", "PHASE {0}/{1}", 8f, 17f, colors.Dim, TextAlign.Left);
            CountRowView handHp = Child(content, "HandHp", 312f, 6f).AddComponent<CountRowView>();
            SetFields(handHp, ("_sheet", "HpBar"), ("_fullTag", "Fill"), ("_emptyTag", "Empty"), ("_spacingPx", 4f), ("_isRightAligned", true), ("_previewCount", 20));
            PixelText handLabel = AddText(content, "HandHpLabel", "HAND {0}/{1}", 312f, 17f, colors.Dim, TextAlign.Right);

            GameObject panelObject = Child(content, "WordPanel", CENTER_X, 4f);
            WordPanelView wordPanel = panelObject.AddComponent<WordPanelView>();
            NineSliceSprite panel = Child(panelObject, "Panel", 0f, 0f, true).AddComponent<NineSliceSprite>();
            SpriteRenderer icon = Child(panelObject, "Icon", 0f, 0f, true).AddComponent<SpriteRenderer>();
            icon.sortingOrder = RenderOrderUtils.HUD + 2;
            PixelText word = AddText(panelObject, "Word", "FIRE", 0f, 0f, colors.Text, TextAlign.Left, isLocal: true);
            PixelText progress = AddText(panelObject, "Progress", "0/4", 0f, 0f, colors.Dim, TextAlign.Left, isLocal: true);
            PixelText next = AddText(panelObject, "Next", "NEXT {0}", 0f, 19f, colors.Dim, TextAlign.Center, isLocal: true);
            PrefabSetupUtils.SetRefs(wordPanel, ("_panel", panel), ("_icon", icon), ("_word", word), ("_progress", progress), ("_next", next));

            GameObject promptObject = Child(content, "SlotPrompt", CENTER_X, 175f);
            SlotPromptView prompt = promptObject.AddComponent<SlotPromptView>();
            SpriteRenderer spaceKey = Child(promptObject, "SpaceKey", 0f, 0f, true).AddComponent<SpriteRenderer>();
            spaceKey.sprite = ViewAssets.Instance.Catalog.GetFrame("SpaceKey", "Base", 0).Sprite;
            spaceKey.sortingOrder = RenderOrderUtils.HUD;
            PixelText promptLabel = AddText(promptObject, "Label", "CAST {0}", 0f, 0f, colors.Glow, TextAlign.Left, hasShadow: true, isLocal: true);
            PrefabSetupUtils.SetRefs(prompt, ("_spaceKey", spaceKey), ("_label", promptLabel));
            promptObject.SetActive(false);

            PixelText toast = AddText(content, "Toast", "TOAST", CENTER_X, 191f, colors.Glow, TextAlign.Center, hasShadow: true);
            toast.gameObject.SetActive(false);
            PixelText banner = AddText(content, "Banner", "PHASE 1", CENTER_X, 52f, colors.Text, TextAlign.Center, 2, true);
            banner.gameObject.SetActive(false);

            PrefabSetupUtils.SetRefs(hud,
                ("_content", content),
                ("_playerHp", playerHp),
                ("_phase", phase),
                ("_handHp", handHp),
                ("_handHpLabel", handLabel),
                ("_wordPanel", wordPanel),
                ("_slotPrompt", prompt),
                ("_toast", toast),
                ("_banner", banner));
            BuildGuide(hud);
            return hud;
        }

        /// <summary>화면 아래 안내 줄(연습 단계, 반대편 슬롯 안내). 이미 있는 씬에도 쓸 수 있다(같은 이름은 다시 만든다).</summary>
        public static void BuildGuide(HudView hud)
        {
            GameObject content = hud.transform.Find("Content").gameObject;
            Transform old = content.transform.Find("Guide");
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            PixelText guide = AddText(content, "Guide", "LINE UP THE WORD IN ONE ROW OR COLUMN", CENTER_X, 182f, ViewAssets.Instance.Colors.Glow, TextAlign.Center, hasShadow: true);
            guide.gameObject.SetActive(false);
            PrefabSetupUtils.SetRefs(hud, ("_guide", guide));
        }

        /// <summary>쓰러짐 화면의 원인 줄과 블록 유지 안내</summary>
        /// <summary>엔딩 자막: Screens 아래 Ending(평소 꺼짐) + 가운데 글자(그림자). 문구는 AttackPatterns.Ending.Caption을 코드가 한 글자씩 채운다.</summary>
        public static void BuildEnding(ScreensView screens)
        {
            GameObject ending = Child(screens.gameObject, "Ending", 0f, 0f);
            PixelText caption = AddText(ending, "Caption", string.Empty, CENTER_X, 46f, ViewAssets.Instance.Colors.Text, TextAlign.Center, 1, true, order: RenderOrderUtils.SCREEN_PANEL + 2);
            ending.SetActive(false);
            PrefabSetupUtils.SetRefs(screens, ("_ending", ending), ("_endingCaption", caption));
        }

        public static void BuildDeadCause(ScreensView screens, GameObject dead)
        {
            foreach (string oldName in new[] { "Cause", "Kept" })
            {
                Transform old = dead.transform.Find(oldName);
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }
            }

            ViewColors colors = ViewAssets.Instance.Colors;
            PixelText cause = AddText(dead, "Cause", "HIT BY {0}", CENTER_X, 110f, colors.Text, TextAlign.Center, order: RenderOrderUtils.SCREEN_PANEL + 2);
            AddText(dead, "Kept", "BLOCKS IN YOUR SLOTS ARE KEPT", CENTER_X, 122f, colors.Dim, TextAlign.Center, order: RenderOrderUtils.SCREEN_PANEL + 2);
            PrefabSetupUtils.SetRefs(screens, ("_deadCause", cause));
        }

        public static ScreensView BuildScreens()
        {
            ViewColors colors = ViewAssets.Instance.Colors;
            var root = new GameObject("Screens");
            ScreensView screens = root.AddComponent<ScreensView>();

            GameObject title = Panel(root, "Title", "SHIFT MAGIC SPELL", "CLICK TO START", colors.Text);
            string[] help =
            {
                "MOVE: WASD / ↑↓←→",
                "SHIFT + ↑↓←→: ROTATE YOUR ROW / COLUMN",
                "LINE UP THE WORD IN ONE ROW OR COLUMN",
                "SPACE ON A SLOT: CAST   P: PAUSE   R: RESTART",
            };
            for (int i = 0; i < help.Length; i++)
            {
                AddText(title, $"Help{i + 1}", help[i], CENTER_X, 120f + i * 11f, colors.Dim, TextAlign.Center, order: RenderOrderUtils.SCREEN_PANEL + 2);
            }

            PixelText version = AddText(title, "Version", "VERSION", 316f, 190f, colors.Dim.WithAlpha(0.6f), TextAlign.Right, order: RenderOrderUtils.SCREEN_PANEL + 2);

            GameObject paused = Panel(root, "Paused", "PAUSED", "P: RESUME  R: RESTART PHASE", colors.Text);
            GameObject dead = Panel(root, "Dead", "YOU FELL", "PHASE {0} AGAIN  HAND HP {1}/{2}", colors.Hp);
            BuildDeadCause(screens, dead);
            GameObject win = Panel(root, "Win", "VICTORY!", "YOU SHIFTED THE CURSOR AWAY", colors.Glow);
            PixelText winLine1 = AddText(win, "Line1", "TIME {0}:{1:00}   DEATHS {2}   HITS {3}", CENTER_X, 118f, colors.Dim, TextAlign.Center, order: RenderOrderUtils.SCREEN_PANEL + 2);
            PixelText winLine2 = AddText(win, "Line2", "WORDS {0}   ROTATIONS {1}   COMBOS {2}", CENTER_X, 130f, colors.Dim, TextAlign.Center, order: RenderOrderUtils.SCREEN_PANEL + 2);
            AddText(win, "Line3", "R: PLAY AGAIN", CENTER_X, 142f, colors.Text, TextAlign.Center, order: RenderOrderUtils.SCREEN_PANEL + 2);

            PrefabSetupUtils.SetRefs(screens,
                ("_title", title),
                ("_version", version),
                ("_paused", paused),
                ("_dead", dead),
                ("_deadSubtitle", dead.transform.Find("Subtitle").GetComponent<PixelText>()),
                ("_win", win),
                ("_winLine1", winLine1),
                ("_winLine2", winLine2));
            foreach (GameObject panel in new[] { paused, dead, win })
            {
                panel.SetActive(false);
            }

            BuildTransitionScreens(root, screens);
            return screens;
        }

        /// <summary>
        /// 페이즈 전환 화면: 클리어 글자(덮개 없이 손이 물러나는 게 보이게), 다음 페이즈 배너, FINAL 배너, 번쩍임.
        /// 이미 있는 씬에도 쓸 수 있게 따로 둔다(같은 이름 오브젝트는 지우고 다시 만든다).
        /// </summary>
        public static void BuildTransitionScreens(GameObject root, ScreensView screens)
        {
            ViewColors colors = ViewAssets.Instance.Colors;
            foreach (string oldName in new[] { "PhaseClear", "PhaseIntro", "Final", "Flash" })
            {
                Transform old = root.transform.Find(oldName);
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }
            }

            GameObject phaseClear = Child(root, "PhaseClear", 0f, 0f);
            PixelText phaseClearTitle = AddText(phaseClear, "TitleText", "PHASE {0} CLEAR", CENTER_X, 40f, colors.Ok, TextAlign.Center, 2, true, order: RenderOrderUtils.SCREEN_PANEL + 2);
            GameObject phaseIntro = Banner(root, "PhaseIntro", "PHASE {0}", "THE HAND GETS FASTER", colors.Text);
            GameObject final = Banner(root, "Final", "FINAL: SHIFT", "LINE UP SHIFT!", colors.Glow);
            SpriteRenderer flash = Child(root, "Flash", 0f, 200f).AddComponent<SpriteRenderer>();
            flash.sprite = ViewAssets.Instance.WhitePixel;
            flash.transform.localScale = new Vector3(ViewUtils.SCREEN_W, ViewUtils.SCREEN_H, 1f);
            flash.sortingOrder = RenderOrderUtils.SCREEN_PANEL + 50;
            flash.enabled = false;
            PrefabSetupUtils.SetRefs(screens,
                ("_phaseClear", phaseClear),
                ("_phaseClearTitle", phaseClearTitle),
                ("_phaseIntro", phaseIntro),
                ("_phaseIntroTitle", phaseIntro.transform.Find("TitleText").GetComponent<PixelText>()),
                ("_final", final),
                ("_flash", flash));
            foreach (GameObject panel in new[] { phaseClear, phaseIntro, final })
            {
                panel.SetActive(false);
            }
        }

        // 화면 가운데 판(Ui/Panel 9-slice) + 큰 제목 + 부제목. 판 뒤의 새 판(키보드)이 보이게 덮개는 없다.
        private static GameObject Banner(GameObject parent, string panelName, string titleText, string subtitle, Color titleColor)
        {
            const float width = 208f;
            const float height = 48f;
            GameObject banner = Child(parent, panelName, 0f, 0f);
            NineSliceSprite panel = Child(banner, "Panel", CENTER_X - width * 0.5f, 62f).AddComponent<NineSliceSprite>();
            SetFields(panel, ("_sizePx", new Vector2(width, height)), ("_sortingOrder", RenderOrderUtils.SCREEN_PANEL + 1));
            AddText(banner, "TitleText", titleText, CENTER_X, 70f, titleColor, TextAlign.Center, 2, true, order: RenderOrderUtils.SCREEN_PANEL + 2);
            AddText(banner, "Subtitle", subtitle, CENTER_X, 94f, ViewAssets.Instance.Colors.Dim, TextAlign.Center, order: RenderOrderUtils.SCREEN_PANEL + 2);
            return banner;
        }

        // 화면 전체 덮개 + 큰 제목 + 부제목
        private static GameObject Panel(GameObject parent, string panelName, string titleText, string subtitle, Color titleColor)
        {
            GameObject panel = Child(parent, panelName, 0f, 0f);
            SpriteRenderer overlay = Child(panel, "Overlay", 0f, 200f).AddComponent<SpriteRenderer>();
            overlay.sprite = ViewAssets.Instance.WhitePixel;
            overlay.color = ViewAssets.Instance.Colors.Overlay;
            overlay.sortingOrder = RenderOrderUtils.SCREEN_PANEL;
            overlay.transform.localScale = new Vector3(ViewUtils.SCREEN_W, ViewUtils.SCREEN_H, 1f);
            AddText(panel, "TitleText", titleText, CENTER_X, 70f, titleColor, TextAlign.Center, 2, true, order: RenderOrderUtils.SCREEN_PANEL + 2);
            AddText(panel, "Subtitle", subtitle, CENTER_X, 96f, ViewAssets.Instance.Colors.Dim, TextAlign.Center, order: RenderOrderUtils.SCREEN_PANEL + 2);
            return panel;
        }

        private static PixelText AddText(GameObject parent, string objectName, string text, float x, float y, Color color, TextAlign align, int scale = 1, bool hasShadow = false, bool isLocal = false, int order = RenderOrderUtils.HUD)
        {
            PixelText pixelText = Child(parent, objectName, x, y, isLocal).AddComponent<PixelText>();
            SetFields(pixelText, ("_text", text), ("_color", color), ("_align", (int)align), ("_scale", scale), ("_hasShadow", hasShadow), ("_sortingOrder", order + 1));
            return pixelText;
        }

        /// <summary>자식 오브젝트를 화면 픽셀 위치에 만든다. isLocal이면 부모 기준 픽셀 오프셋(y 아래가 +).</summary>
        private static GameObject Child(GameObject parent, string objectName, float x, float y, bool isLocal = false)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(parent.transform, false);
            if (isLocal)
            {
                child.transform.localPosition = new Vector3(x, -y, 0f) / ViewUtils.PPU;
            }
            else
            {
                child.transform.position = ViewUtils.PixelToWorld(new Vector2(x, y));
            }

            return child;
        }

        public static void SetFields(Object target, params (string Field, object Value)[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, object value) in fields)
            {
                SerializedProperty property = serialized.FindProperty(field);
                switch (value)
                {
                    case string text:
                        property.stringValue = text;
                        break;
                    case float number:
                        property.floatValue = number;
                        break;
                    case int integer when property.propertyType == SerializedPropertyType.Enum:
                        property.enumValueIndex = integer;
                        break;
                    case int integer:
                        property.intValue = integer;
                        break;
                    case bool flag:
                        property.boolValue = flag;
                        break;
                    case Vector2 vector:
                        property.vector2Value = vector;
                        break;
                    case Color color:
                        property.colorValue = color;
                        break;
                    default:
                        Debug.LogError($"[ERROR] HudSetupUtils::SetFields() - Unsupported value. Details: [Field={field}]");
                        break;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
