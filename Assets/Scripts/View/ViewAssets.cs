using System;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 화면 쪽 공용 설정 하나: 스프라이트 목록, 공용 스프라이트(흰 점·그림자·테두리), 화면 배치, 색.
    /// `Assets/Resources/ViewAssets.asset` 하나만 두고 인스펙터에서 고친다. 어디서나 Instance로 읽는다(에디터 미리보기 포함).
    /// </summary>
    [CreateAssetMenu(fileName = "ViewAssets", menuName = "Shift Magic Spell/View Assets")]
    public class ViewAssets : ScriptableObject
    {
        public const string RESOURCE_NAME = "ViewAssets";

        private static ViewAssets _instance;

        [Header("스프라이트")]
        public ArtCatalog Catalog;
        [Tooltip("1x1 흰 점(사각형·선·덮개를 그릴 때 늘려서 씀). 피벗 왼쪽 아래")]
        public Sprite WhitePixel;
        [Tooltip("손 그림자 타원. 피벗 가운데")]
        public Sprite Shadow;
        [Tooltip("플레이어 테두리(18x18). 피벗 왼쪽 아래")]
        public Sprite Outline;
        [Tooltip("타격감 하얀 번쩍임 머티리얼(ShiftMagicSpell/SpriteFlash 셰이더). 플레이어·손·공격 블록에 쓴다")]
        public Material FlashMaterial;

        [Header("화면 배치 (320x200 픽셀, 왼쪽 위 원점)")]
        public ScreenLayoutValues Layout = new ScreenLayoutValues();

        [Header("색")]
        public ViewColors Colors = new ViewColors();

        public static ViewAssets Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ViewAssets>(RESOURCE_NAME);
                    if (_instance == null)
                    {
                        Debug.LogError($"[ERROR] ViewAssets::Instance - Missing resource. Details: [Name={RESOURCE_NAME}]");
                    }
                }

                return _instance;
            }
        }
    }
}
