using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 맞은 대상을 하얀 실루엣으로 번쩍이게 한다(SpriteFlash 셰이더의 _Flash 값). 머티리얼은 ViewAssets.FlashMaterial.
    /// 렌더러에 머티리얼이 안 붙어 있으면 여기서 붙인다(프리팹에는 에디터 도구가 미리 붙여 둔다).
    /// </summary>
    public static class SpriteFlashUtils
    {
        private static readonly int FLASH_ID = Shader.PropertyToID("_Flash");
        private static readonly int FLASH_COLOR_ID = Shader.PropertyToID("_FlashColor");
        private static MaterialPropertyBlock _block;

        /// <summary>amount 0 = 원래 그림, 1 = 완전히 흰색(알파는 그대로)</summary>
        public static void Apply(SpriteRenderer renderer, float amount)
        {
            Apply(renderer, amount, Color.white);
        }

        public static void Apply(SpriteRenderer renderer, float amount, Color flashColor)
        {
            Material material = ViewAssets.Instance.FlashMaterial;
            if (renderer == null || material == null)
            {
                return;
            }

            if (renderer.sharedMaterial != material)
            {
                renderer.sharedMaterial = material;
            }

            _block ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_block);
            _block.SetFloat(FLASH_ID, amount);
            _block.SetColor(FLASH_COLOR_ID, flashColor);
            renderer.SetPropertyBlock(_block);
        }
    }
}
