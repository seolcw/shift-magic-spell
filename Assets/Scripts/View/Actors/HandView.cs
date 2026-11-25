using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 손(Hand 프리팹). 루트 위치 = 손의 로직 위치(손 그림 가운데). Body는 그림 가운데가 루트에 오도록 놓는다.
    /// 판 위로 올라오면 그림자와 함께 살짝 떠 있고, 줄 섞기는 줄 바깥으로 당겼다가 튕긴다.
    /// 타격감: 마법에 맞으면 하얀 실루엣, 마법이 날아온 방향으로 밀리고 납작해졌다 돌아온다(세기는 AttackPatterns.HitFeel).
    /// </summary>
    public class HandView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private SpriteRenderer _shadow;
        [SerializeField] private int _sortingOrder = RenderOrderUtils.HAND;

        [Header("판 위에서 떠 있는 높이(px)·크기")]
        [SerializeField] private float _liftPx = 3f;
        [SerializeField] private float _bigLiftPx = 9f;
        [Tooltip("추적 내려찍기 때 손 크기")]
        [SerializeField] private float _bigScale = 1.25f;
        [SerializeField] private Vector2 _shadowSize = new Vector2(7f, 3f);
        [SerializeField] private Vector2 _bigShadowSize = new Vector2(14f, 7f);
        [SerializeField, Range(0f, 1f)] private float _shadowAlpha = 0.45f;

        [Header("동작")]
        [Tooltip("줄 섞기 예고 때 당기는 양(칸 비율)")]
        [SerializeField] private float _flickPull = 0.3f;
        [Tooltip("줄 섞기 튕길 때 나가는 양(칸 비율)")]
        [SerializeField] private float _flickPush = 0.45f;
        [Tooltip("맞은 뒤 Hurt 그림과 흔들림을 보여 주는 시간(초)")]
        [SerializeField] private float _hurtTime = 0.35f;

        [Header("페이즈 전환")]
        [Tooltip("손 HP 0 뒤 전환 시간 중 이 비율부터 화면 위로 물러난다(앞부분은 마법을 맞고 흔들림)")]
        [SerializeField, Range(0f, 1f)] private float _retreatStart = 0.6f;
        [Tooltip("물러나는·내려오는 거리(px)")]
        [SerializeField] private float _retreatPx = 120f;
        [SerializeField] private float _clearShakePx = 3f;
        [Tooltip("배너 시간 중 이 비율 안에 다시 내려온다")]
        [SerializeField, Range(0.1f, 1f)] private float _returnRatio = 0.6f;

        private Vector3 _bodyHome;
        private bool _hasBodyHome;

        public void Render(GameSession game)
        {
            bool isVisible = game.State != GameState.Ready;
            gameObject.SetActive(isVisible);
            if (!isVisible)
            {
                return;
            }

            if (!_hasBodyHome)
            {
                _bodyHome = _body.transform.localPosition;
                _hasBodyHome = true;
            }

            ViewColors colors = ViewAssets.Instance.Colors;
            HandController hand = game.Hand;
            AttackPlan attack = hand.Attack;
            bool isBig = attack != null && attack.Type == AttackType.Track;
            bool isOverBoard = !hand.IsCharging && IsOverBoard(hand.Pos);
            float lift = isOverBoard ? (isBig ? _bigLiftPx : _liftPx) : 0f;
            float scale = isOverBoard && isBig ? _bigScale : 1f;
            HitFeedback feel = game.Feel;
            Vector2 animOffset = GetAnimOffset(hand, attack) + GetTransitionOffset(game) + feel.HandKnock.Dir * feel.HandKnock.Strength;

            // 납작해짐: 가로로 늘고 세로로 눌린다. 피벗이 왼쪽 아래라 Body 위치(그림 가운데를 루트에 맞춘 값)도 같은 배율로 맞춘다.
            float squash = feel.HandSquash.Strength;
            float scaleX = scale * (1f + squash);
            float scaleY = scale * (1f - squash);
            transform.localPosition = ViewUtils.PixelToWorld(ViewUtils.ToPixel(hand.Pos));
            _body.transform.localPosition = new Vector3(_bodyHome.x * scaleX, _bodyHome.y * scaleY, _bodyHome.z) + new Vector3(Mathf.Round(animOffset.x), -Mathf.Round(animOffset.y - lift), 0f) / ViewUtils.PPU;
            _body.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            _body.sprite = GetFrame(game, hand, attack).Sprite;
            _body.sortingOrder = _sortingOrder;

            Color tint = Color.white;
            if (hand.IsCharging)
            {
                tint = colors.HandCharge;
            }
            else if (hand.State == HandState.Stun)
            {
                tint = colors.HandStun;
            }

            _body.color = tint;
            SpriteFlashUtils.Apply(_body, feel.HandFlash.IsActive ? 1f : 0f);
            _shadow.enabled = isOverBoard;
            Vector2 shadowSize = isBig ? _bigShadowSize : _shadowSize;
            _shadow.transform.localPosition = new Vector3(0f, -2f / ViewUtils.PPU, 0f);
            _shadow.transform.localScale = new Vector3(shadowSize.x * 2f / 32f, shadowSize.y * 2f / 16f, 1f);
            _shadow.color = new Color(0f, 0f, 0f, _shadowAlpha);
            _shadow.sortingOrder = _sortingOrder - 1;
        }

        // 페이즈 전환: 마법을 맞고 크게 흔들리다 화면 위로 물러났다가(PhaseClear), 배너 동안 다시 내려온다(PhaseIntro). 화면 픽셀(y 아래가 +)
        private Vector2 GetTransitionOffset(GameSession game)
        {
            if (game.State == GameState.PhaseClear)
            {
                float total = Mathf.Max(0.01f, game.Config.PhaseClearTime);
                float elapsed = total - game.StateT;
                float retreatAt = total * _retreatStart;
                if (elapsed < retreatAt)
                {
                    float shake = elapsed > game.Config.SpellTime * 0.5f ? _clearShakePx : 0f;
                    return new Vector2(Mathf.Round(Mathf.Sin(elapsed * 70f) * shake), 0f);
                }

                float u = Mathf.Clamp01((elapsed - retreatAt) / Mathf.Max(0.01f, total - retreatAt));
                return new Vector2(0f, -_retreatPx * u * u);
            }

            if (game.State == GameState.PhaseIntro)
            {
                float total = Mathf.Max(0.01f, game.Config.PhaseIntroTime);
                float u = Mathf.Clamp01((total - game.StateT) / (total * _returnRatio));
                float eased = 1f - (1f - u) * (1f - u);
                return new Vector2(0f, -_retreatPx * (1f - eased));
            }

            return Vector2.zero;
        }

        private Vector2 GetAnimOffset(HandController hand, AttackPlan attack)
        {
            Vector2 offset = Vector2.zero;
            if (hand.Anim == HandController.ANIM_FLICK_CHARGE && attack != null)
            {
                float u = Mathf.Min(1f, hand.AnimT / Mathf.Max(0.05f, attack.Warn));
                offset = -ViewUtils.ToPixelDelta(hand.AnimDir) * (_flickPull * u);
            }
            else if (hand.Anim == HandController.ANIM_FLICK_RELEASE)
            {
                float u = 1f - Mathf.Min(1f, hand.AnimT / 0.16f);
                offset = ViewUtils.ToPixelDelta(hand.AnimDir) * (_flickPush * u);
            }

            if (hand.Anim == HandController.ANIM_HURT && hand.AnimT < _hurtTime)
            {
                float u = 1f - hand.AnimT / _hurtTime;
                // 맞은 직후 좌우로 떨린다(밀리는 양은 타격감 넉백이 따로 더한다)
                offset += new Vector2(Mathf.Sin(hand.AnimT * 90f) * 2f * u, 0f);
            }

            return offset;
        }

        private static ArtFrame GetFrame(GameSession game, HandController hand, AttackPlan attack)
        {
            ArtCatalog catalog = ViewAssets.Instance.Catalog;
            if (hand.Anim == HandController.ANIM_HURT && hand.AnimT < 0.35f)
            {
                return catalog.GetFrame("Hand", "Hurt", 0);
            }

            if (hand.Anim == HandController.ANIM_FLICK_CHARGE || hand.Anim == HandController.ANIM_FLICK_RELEASE)
            {
                return catalog.GetAnimated("Hand", hand.Anim, hand.AnimT, false);
            }

            if (attack != null && hand.State == HandState.Warn)
            {
                switch (attack.Type)
                {
                    case AttackType.Click:
                    case AttackType.Slot:
                    case AttackType.Drag:
                        return catalog.GetAnimated("Hand", "Click", game.Time);
                    case AttackType.Track:
                    case AttackType.Full:
                        return catalog.GetFrame("Hand", "SlamRaise", 0);
                    default:
                        break;
                }
            }

            if (attack != null && hand.State == HandState.Exec && attack.Type == AttackType.Full)
            {
                return catalog.GetFrame("Hand", "SlamHit", 0);
            }

            return catalog.GetAnimated("Hand", "Idle", game.Time);
        }

        private static bool IsOverBoard(Vector2 logic)
        {
            for (int board = 0; board < 2; board++)
            {
                float x = GeometryUtils.BoardOffsetX(board);
                if (logic.x > x && logic.x < x + GeometryUtils.COLS && logic.y > 0f && logic.y < GeometryUtils.ROWS)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
