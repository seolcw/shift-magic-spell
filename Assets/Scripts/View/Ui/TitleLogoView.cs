using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 첫 화면 로고(Screens/Title/Logo): 키캡으로 된 제목 두 줄이 게임의 줄 돌리기처럼 밀려서 맞춰지고,
    /// 맞춰지면 초록 빛·별 → 플레이어의 불덩이가 오른쪽 위 커서에 맞음 → CLICK TO START가 깜빡인다. 시각은 TitleLogoValues.
    /// 자기 즉시 그리기 캔버스(덮개 위 순서)에 그린다. ScreensView가 첫 화면일 때만 Render를 부른다.
    /// </summary>
    public class TitleLogoView : MonoBehaviour
    {
        private const float KEY_W = 16f;
        private const float KEY_H = 20f;
        private const float GLYPH_X = 5f;
        private const float GLYPH_Y = 4f;

        [SerializeField] private PixelCanvas _canvas;
        [SerializeField] private TitleLogoValues _values = new TitleLogoValues();

        [Header("배치 (화면 px, 왼쪽 위 원점)")]
        [Tooltip("위 줄(SHIFT) 윗변")]
        [SerializeField] private float _topY = 22f;
        [Tooltip("위 줄 키캡 배율")]
        [SerializeField] private int _topScale = 2;
        [Tooltip("위 줄 아래와 아래 줄 사이")]
        [SerializeField] private float _rowGap = 6f;
        [SerializeField] private float _promptY = 104f;
        [Tooltip("커서(손) 가운데")]
        [SerializeField] private Vector2 _handCenter = new Vector2(278f, 24f);
        [Tooltip("맞았을 때 커서가 밀려나는 거리(px)")]
        [SerializeField] private float _handKnockPx = 6f;
        [SerializeField] private float _handHurtTime = 0.5f;
        [Tooltip("불덩이 궤적이 위로 휘는 높이(px)")]
        [SerializeField] private float _arcPx = 14f;
        [SerializeField] private float _burstTime = 0.28f;

        private PixelTextDrawer _text;
        private float _loopT;
        private float _totalT;

        public TitleLogoValues Values => _values;

        public float TotalTime => _totalT;

        private static ViewColors Colors => ViewAssets.Instance.Colors;

        private static ArtCatalog Catalog => ViewAssets.Instance.Catalog;

        private float CenterX => ViewUtils.SCREEN_W * 0.5f;

        private void OnEnable()
        {
            _loopT = 0f;
            _totalT = 0f;
        }

        public void Render(float deltaTime)
        {
            if (_canvas == null)
            {
                return;
            }

            _text ??= new PixelTextDrawer(_canvas, Catalog);
            _totalT += deltaTime;
            _loopT = Mathf.Repeat(_loopT + deltaTime, _values.Duration);
            _canvas.BeginFrame();
            Draw(_loopT, _totalT);
            _canvas.EndFrame();
        }

        private void Draw(float t, float total)
        {
            TitleLogoValues v = _values;
            float topW = v.Top.Length * KEY_W * _topScale;
            float topX = Mathf.Round(CenterX - topW * 0.5f);
            float bottomY = _topY + KEY_H * _topScale + _rowGap;
            float bottomW = v.Bottom.Length * KEY_W;
            float bottomX = Mathf.Round(CenterX - bottomW * 0.5f);

            DrawRow(v.Top, topX, _topY, v.GetTopOffset(t), _topScale, t >= v.TopSolvedAt);
            DrawRow(v.Bottom, bottomX, bottomY, v.GetBottomOffset(t), 1, t >= v.BottomSolvedAt);

            // 미는 중인 줄 끝에 SHIFT 화살표(미는 방향)가 깜빡인다
            ArtFrame arrow = Frame("ShiftArrow", "Arrow");
            bool isBlinkOn = Mathf.Repeat(t * 3f, 1f) < 0.5f;
            if (v.IsTopShifting(t) && isBlinkOn)
            {
                _canvas.DrawFrameCentered(arrow, topX - 12f, _topY + KEY_H * _topScale * 0.5f, Color.white, 90f);
            }

            if (v.IsBottomShifting(t) && isBlinkOn)
            {
                _canvas.DrawFrameCentered(arrow, bottomX + bottomW + 12f, bottomY + KEY_H * 0.5f, Color.white, -90f);
            }

            // 플레이어는 아래 줄 왼쪽에 서 있다(위 줄 왼쪽은 밀기 화살표 자리)
            Vector2 player = new Vector2(bottomX - 12f, bottomY + KEY_H * 0.5f);
            _canvas.DrawFrameCentered(Catalog.GetAnimated("Player", "Idle", t), player.x, player.y, Color.white);

            DrawSparkles(t, v.TopSolvedAt, topX, _topY, topW, KEY_H * _topScale);
            DrawSparkles(t, v.BottomSolvedAt, bottomX, bottomY, bottomW, KEY_H);
            DrawHandAndShot(t, new Vector2(CenterX, _topY + 10f));

            if (v.IsPromptVisible(total))
            {
                _text.Draw("CLICK TO START", CenterX, _promptY, Colors.Text, TextAlign.Center, 1f, Color.black);
            }
        }

        // 단어를 offset칸 왼쪽으로 돌린 줄. 경계를 넘는 키는 양쪽 끝에 나눠 그리고 넘어간 만큼 흐리게(잘라 그릴 수 없어서)
        private void DrawRow(string word, float x, float y, float offset, int scale, bool isSolved)
        {
            int count = word.Length;
            float width = KEY_W * scale;
            for (int i = 0; i < count; i++)
            {
                float pos = Mathf.Repeat(i - offset, count);
                if (pos <= count - 1f)
                {
                    DrawKey(word[i], x + pos * width, y, scale, isSolved, 1f);
                    continue;
                }

                float inLeft = pos - (count - 1f);
                DrawKey(word[i], x + (pos - count) * width, y, scale, isSolved, inLeft);
                DrawKey(word[i], x + pos * width, y, scale, isSolved, 1f - inLeft);
            }
        }

        private void DrawKey(char letter, float x, float y, int scale, bool isSolved, float alpha)
        {
            if (alpha <= 0.02f)
            {
                return;
            }

            x = Mathf.Round(x);
            bool isBlank = letter == ' ';
            var tint = new Color(1f, 1f, 1f, alpha);
            _canvas.DrawFrame(Frame("Keycap", isBlank ? "Blank" : "Base"), x, y, tint, scale);
            if (isBlank)
            {
                return;
            }

            if (isSolved)
            {
                _canvas.DrawFrame(Frame("KeyMatched", "Glow"), x, y, tint, scale);
            }

            if (_text.TryGetGlyph(letter, out ArtFrame glyph))
            {
                Color color = isSolved ? Colors.Ok : Colors.Text;
                _canvas.DrawFrame(glyph, x + GLYPH_X * scale, y + (GLYPH_Y + 1f) * scale, Color.black.WithAlpha(alpha * 0.5f), scale);
                _canvas.DrawFrame(glyph, x + GLYPH_X * scale, y + GLYPH_Y * scale, color.WithAlpha(alpha), scale);
            }
        }

        // 줄이 맞은 순간 줄 둘레(글자 위는 피해서)에 별이 반짝인다
        private void DrawSparkles(float t, float solvedAt, float x, float y, float width, float height)
        {
            float local = t - solvedAt;
            if (local < 0f || local >= _values.SparkleTime)
            {
                return;
            }

            Vector2[] spots =
            {
                new Vector2(-6f, -2f), new Vector2(width + 6f, -4f), new Vector2(width * 0.33f, -6f),
                new Vector2(width * 0.66f, height + 4f), new Vector2(-8f, height - 2f), new Vector2(width + 8f, height - 6f),
            };
            for (int i = 0; i < spots.Length; i++)
            {
                float bob = Mathf.Round(Mathf.Sin(i + local * 6f) * 2f);
                _canvas.DrawFrameCentered(Catalog.GetAnimated("Star", "Twinkle", t + i * 0.1f), x + spots[i].x, y + spots[i].y + bob, Color.white);
            }
        }

        // 커서는 오른쪽 위에서 까딱거리고, 불덩이가 날아와 맞으면 밀려나며 아파한다
        private void DrawHandAndShot(float t, Vector2 from)
        {
            TitleLogoValues v = _values;
            float sinceHit = t - v.HitAt;
            bool isHurt = sinceHit >= 0f && sinceHit < _handHurtTime;
            Vector2 hand = _handCenter;
            ArtFrame handFrame;
            if (isHurt)
            {
                handFrame = Frame("Hand", "Hurt");
                hand.x += Mathf.Round(_handKnockPx * (1f - sinceHit / _handHurtTime));
            }
            else
            {
                handFrame = Catalog.GetAnimated("Hand", "Idle", t);
                hand.y += Mathf.Round(Mathf.Sin(t * 3f) * 1.5f);
            }

            _canvas.DrawFrameCentered(handFrame, hand.x, hand.y, Color.white);

            if (t >= v.ShotAt && t < v.HitAt)
            {
                float k = (t - v.ShotAt) / Mathf.Max(0.01f, v.FlyTime);
                Vector2 to = _handCenter + new Vector2(-6f, 4f);
                Vector2 position = Vector2.Lerp(from, to, k) + Vector2.down * (Mathf.Sin(k * Mathf.PI) * _arcPx);
                Vector2 dir = (to - from) + Vector2.down * (Mathf.Cos(k * Mathf.PI) * Mathf.PI * _arcPx);
                float angle = -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                _canvas.DrawFrameCentered(Catalog.GetAnimated("Fireball", "Fly", t), Mathf.Round(position.x), Mathf.Round(position.y), Color.white, angle);
            }

            if (sinceHit >= 0f && sinceHit < _burstTime)
            {
                _canvas.DrawFrameCentered(Catalog.GetAnimated("Explosion", "Burst", sinceHit, false), _handCenter.x - 6f, _handCenter.y + 4f, Color.white);
            }
        }

        private static ArtFrame Frame(string sheet, string tag, int index = 0)
        {
            return Catalog.GetFrame(sheet, tag, index);
        }
    }
}
