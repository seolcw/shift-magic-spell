using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 공격 슬롯 칸(Slot 프리팹): 받침(AttackSlot) + 담긴 블록(AttackBlock, 마법 색으로 틴트) + 마법 아이콘.
    /// 블록은 들어올 때 커지며 나타나고, 손에게 맞으면 하얗게 번쩍이고(타격감) 흔들리고 금이 간다. 루트 위치 = 칸 그림(16x20) 왼쪽 아래.
    /// </summary>
    public class SlotView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _base;
        [Tooltip("블록 가운데(커지기·흔들기 기준점)")]
        [SerializeField] private Transform _blockPivot;
        [SerializeField] private SpriteRenderer _block;
        [SerializeField] private SpriteRenderer _icon;
        [SerializeField] private int _baseSortingOrder = RenderOrderUtils.BOARD_BASE;
        [SerializeField] private int _blockSortingOrder = RenderOrderUtils.BLOCK;
        [Tooltip("블록이 커지며 나타나는 시간(초)")]
        [SerializeField] private float _popTime = 0.25f;
        [Tooltip("맞았을 때 흔들리는 폭(px)")]
        [SerializeField] private float _shakePx = 3f;

        private Vector3 _pivotHome;
        private bool _hasPivotHome;

        public void Render(BoardCell slot, bool isActive, float time, AttackPatternsData data)
        {
            ArtCatalog catalog = ViewAssets.Instance.Catalog;
            if (!_hasPivotHome)
            {
                _pivotHome = _blockPivot.localPosition;
                _hasPivotHome = true;
            }

            _base.sprite = catalog.GetFrame("AttackSlot", isActive ? "Active" : "Empty", 0).Sprite;
            _base.sortingOrder = _baseSortingOrder;
            AttackBlock block = slot?.Block;
            _blockPivot.gameObject.SetActive(block != null);
            if (block == null)
            {
                return;
            }

            SpellDef spell = data != null ? data.GetSpellOrNull(block.Word) : null;
            float pop = Mathf.Min(1f, block.Age / _popTime);
            float scale = pop < 1f ? 0.4f + 0.6f * pop + Mathf.Sin(pop * Mathf.PI) * 0.25f : 1f;
            float shake = block.HitT > 0f ? Mathf.Round(Mathf.Sin(time * 70f) * _shakePx * block.HitT / 0.4f) : 0f;
            _blockPivot.localPosition = _pivotHome + new Vector3(shake / ViewUtils.PPU, 0f, 0f);
            _blockPivot.localScale = new Vector3(scale, scale, 1f);
            bool isCracked = data != null && block.Hp < data.Config.BlockHp;
            _block.sprite = isCracked ? catalog.GetFrame("AttackBlock", "Cracked", 0).Sprite : catalog.GetAnimated("AttackBlock", "Idle", block.Age).Sprite;
            _block.color = spell != null ? spell.Color : ViewAssets.Instance.Colors.Glow;
            _block.sortingOrder = _blockSortingOrder;
            _icon.sprite = catalog.GetFrame("SpellIcon", spell?.Icon, 0).Sprite;
            _icon.sortingOrder = _blockSortingOrder + 1;

            // 손에게 맞은 순간 블록과 아이콘이 함께 하얗게
            float flash = block.FlashT > 0f ? 1f : 0f;
            SpriteFlashUtils.Apply(_block, flash);
            SpriteFlashUtils.Apply(_icon, flash);
        }
    }
}
