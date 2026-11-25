using ShiftMagicSpell.Core;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 타격감 화면 테두리(Hud 아래 HitEdge 오브젝트): 플레이어가 맞으면 화면 가장자리가 잠깐 빨갛게 물들고,
    /// HP가 1 남으면 은은하게 계속 뛴다. 네 변은 흰 점 스프라이트를 늘려 그린다(화면 쪽이라 흔들리지 않음).
    /// 부모(Hud)는 화면 원점(왼쪽 아래 0,0)에 있다고 보고 320x200 화면 픽셀로 배치한다.
    /// </summary>
    public class HitEdgeView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _top;
        [SerializeField] private SpriteRenderer _bottom;
        [SerializeField] private SpriteRenderer _left;
        [SerializeField] private SpriteRenderer _right;
        [Tooltip("화면 글자(HUD)보다 아래에 그려서 글자를 가리지 않게")]
        [SerializeField] private int _sortingOrder = RenderOrderUtils.HUD - 10;

        [Header("맞았을 때 (시간은 AttackPatterns.HitFeel.EdgeTime)")]
        [SerializeField] private float _hitThicknessPx = 6f;
        [SerializeField, Range(0f, 1f)] private float _hitAlpha = 0.55f;

        [Header("HP 1일 때")]
        [SerializeField] private bool _isLowHpPulseOn = true;
        [SerializeField] private float _lowHpThicknessPx = 2f;
        [SerializeField, Range(0f, 1f)] private float _lowHpAlpha = 0.35f;
        [Tooltip("뛰는 빠르기(초당)")]
        [SerializeField] private float _lowHpRate = 1.5f;

        public void Render(GameSession game)
        {
            float hit = game.Feel.Edge.Strength;
            bool isLowHp = _isLowHpPulseOn && game.State == GameState.Play && game.Player.Hp == 1;
            float low = isLowHp ? 0.5f + 0.5f * Mathf.Sin(game.Time * _lowHpRate * Mathf.PI * 2f) : 0f;
            float thickness = Mathf.Round(Mathf.Max(hit * _hitThicknessPx, isLowHp ? _lowHpThicknessPx : 0f));
            float alpha = Mathf.Max(hit * _hitAlpha, low * _lowHpAlpha);
            bool isVisible = thickness >= 1f && alpha > 0.01f;
            Color color = ViewAssets.Instance.Colors.Warn.WithAlpha(alpha);
            float w = ViewUtils.SCREEN_W;
            float h = ViewUtils.SCREEN_H;
            SetBar(_top, isVisible, 0f, 0f, w, thickness, color);
            SetBar(_bottom, isVisible, 0f, h - thickness, w, thickness, color);
            SetBar(_left, isVisible, 0f, thickness, thickness, h - thickness * 2f, color);
            SetBar(_right, isVisible, w - thickness, thickness, thickness, h - thickness * 2f, color);
        }

        // 화면 픽셀 사각형(왼쪽 위 원점) → 흰 점 스프라이트(피벗 왼쪽 아래)를 늘려서
        private void SetBar(SpriteRenderer bar, bool isVisible, float x, float y, float width, float height, Color color)
        {
            if (bar == null)
            {
                return;
            }

            bar.enabled = isVisible && width > 0f && height > 0f;
            if (!bar.enabled)
            {
                return;
            }

            bar.sprite ??= ViewAssets.Instance.WhitePixel;
            bar.color = color;
            bar.sortingOrder = _sortingOrder;
            bar.transform.localPosition = new Vector3(x / ViewUtils.PPU, (ViewUtils.SCREEN_H - y - height) / ViewUtils.PPU, 0f);
            bar.transform.localScale = new Vector3(width, height, 1f);
        }
    }
}
