using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 플레이어(Player 프리팹). 루트 위치 = 서 있는 칸 그림(16x20)의 왼쪽 아래. 몸은 Body 자식(칸보다 띄우는 높이는 Body의 위치).
    /// 피격 무적 동안 깜빡이고, Shift를 누르고 있으면 하늘색 테, 디버그 무적이면 노란 테.
    /// 타격감: 맞는 순간 하얀 실루엣, 밀린 방향으로 넉백, 납작해졌다 돌아옴(세기는 AttackPatterns.HitFeel).
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        // 몸 그림 너비(px). 피벗이 왼쪽 아래라 납작해질 때 가운데를 기준으로 맞춘다.
        private const float BODY_W = 16f;

        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private SpriteRenderer _shiftOutline;
        [SerializeField] private SpriteRenderer _debugOutline;
        [SerializeField] private int _sortingOrder = RenderOrderUtils.PLAYER;
        [Tooltip("무적 동안 깜빡이는 빠르기(초당)")]
        [SerializeField] private float _blinkRate = 16f;
        [SerializeField, Range(0f, 1f)] private float _blinkAlpha = 0.35f;
        [Tooltip("맞은 뒤 Hurt 그림을 보여 주는 시간(초)")]
        [SerializeField] private float _hurtShowTime = 0.35f;

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

            ArtCatalog catalog = ViewAssets.Instance.Catalog;
            ViewColors colors = ViewAssets.Instance.Colors;
            PlayerController player = game.Player;
            HitFeedback feel = game.Feel;
            Vector3 offset = SlideAnim.GetOffset(player.Slide);
            Vector2 topLeft = ViewUtils.CellTopLeft(player.Board, player.R, player.C) + ViewUtils.ToPixelDelta(offset);
            transform.localPosition = ViewUtils.TopLeftToWorld(topLeft, ViewUtils.CellH);

            // 넉백(밀린 방향, 화면 px) + 납작해짐(가로로 늘고 세로로 눌림, 발은 칸에 붙인 채)
            Vector2 knock = feel.PlayerKnock.Dir * feel.PlayerKnock.Strength;
            float squash = feel.PlayerSquash.Strength;
            float scaleX = 1f + squash;
            float scaleY = 1f - squash;
            float centerFix = -(scaleX - 1f) * BODY_W * 0.5f;
            _body.transform.localPosition = _bodyHome + new Vector3(Mathf.Round(knock.x + centerFix), -Mathf.Round(knock.y), 0f) / ViewUtils.PPU;
            _body.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            // 번쩍이는 동안에는 무적 깜빡임을 쉬고 완전히 하얗게
            bool isFlashing = feel.PlayerFlash.IsActive;
            bool isBlinking = !isFlashing && player.Invuln > 0f && Mathf.FloorToInt(game.Time * _blinkRate) % 2 == 0;
            bool isHurt = player.Invuln > game.Config.HitInvuln - _hurtShowTime && player.Hp < game.Config.PlayerHp && game.Stats.Hits.Count > 0;
            _body.sprite = isHurt ? catalog.GetFrame("Player", "Hurt", 0).Sprite : catalog.GetAnimated("Player", "Idle", game.Time).Sprite;
            _body.color = new Color(1f, 1f, 1f, offset.z * (isBlinking ? _blinkAlpha : 1f));
            _body.sortingOrder = _sortingOrder;
            SpriteFlashUtils.Apply(_body, isFlashing ? 1f : 0f);

            _shiftOutline.enabled = player.IsShiftHeld;
            _shiftOutline.color = colors.ShiftHi;
            _shiftOutline.sortingOrder = _sortingOrder + 1;
            _debugOutline.enabled = game.IsInvincible;
            _debugOutline.color = colors.Glow;
            _debugOutline.sortingOrder = _sortingOrder + 1;
        }
    }
}
