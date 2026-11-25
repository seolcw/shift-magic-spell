using ShiftMagicSpell.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShiftMagicSpell.Editor
{
    /// <summary>
    /// 타격감 준비: 하얀 번쩍임 머티리얼을 만들어 ViewAssets와 프리팹(플레이어·손·슬롯 블록)에 붙이고,
    /// Main 씬의 Hud에 빨간 화면 테두리(HitEdge)를 넣는다. 이미 있으면 그대로 둔다(인스펙터에서 고친 값을 지킨다).
    /// </summary>
    public static class HitFeedbackSetupUtils
    {
        public const string SHADER_NAME = "ShiftMagicSpell/SpriteFlash";
        public const string FLASH_MATERIAL_PATH = ViewAssetSetupUtils.GENERATED_DIR + "/SpriteFlash.mat";

        [MenuItem("Tools/Shift Magic Spell/Add Hit Feedback To Main Scene (keeps edits)")]
        public static void AddToMainScene()
        {
            Material material = SetupFlashMaterial();
            ApplyFlashMaterialToPrefabs(material);

            var scene = EditorSceneManager.OpenScene(SceneSetupUtils.SCENE_PATH, OpenSceneMode.Single);
            GameRenderer renderer = Object.FindAnyObjectByType<GameRenderer>(FindObjectsInactive.Include);
            HudView hud = Object.FindAnyObjectByType<HudView>(FindObjectsInactive.Include);
            HitEdgeView edge = Object.FindAnyObjectByType<HitEdgeView>(FindObjectsInactive.Include);
            if (edge == null && hud != null)
            {
                edge = BuildHitEdge(hud.gameObject);
            }

            if (renderer != null && edge != null)
            {
                PrefabSetupUtils.SetRefs(renderer, ("_hitEdge", edge));
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[INFO] HitFeedbackSetupUtils::AddToMainScene() - Hit feedback ready. Details: [Material={FLASH_MATERIAL_PATH}, Scene={SceneSetupUtils.SCENE_PATH}]");
        }

        /// <summary>번쩍임 머티리얼이 없으면 만들고, ViewAssets.FlashMaterial이 비어 있으면 채운다</summary>
        public static Material SetupFlashMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FLASH_MATERIAL_PATH);
            if (material == null)
            {
                Shader shader = Shader.Find(SHADER_NAME);
                if (shader == null)
                {
                    Debug.LogError($"[ERROR] HitFeedbackSetupUtils::SetupFlashMaterial() - Shader not found. Details: [Shader={SHADER_NAME}]");
                    return null;
                }

                System.IO.Directory.CreateDirectory(ViewAssetSetupUtils.GENERATED_DIR);
                material = new Material(shader) { name = "SpriteFlash" };
                AssetDatabase.CreateAsset(material, FLASH_MATERIAL_PATH);
            }

            ViewAssets assets = AssetDatabase.LoadAssetAtPath<ViewAssets>(ViewAssetSetupUtils.VIEW_ASSETS_PATH);
            if (assets != null && assets.FlashMaterial == null)
            {
                assets.FlashMaterial = material;
                EditorUtility.SetDirty(assets);
            }

            AssetDatabase.SaveAssets();
            return material;
        }

        /// <summary>번쩍이는 렌더러(플레이어·손 몸, 슬롯 블록·아이콘)에 머티리얼을 붙인다</summary>
        public static void ApplyFlashMaterialToPrefabs(Material material)
        {
            if (material == null)
            {
                return;
            }

            SetMaterial(PrefabSetupUtils.PLAYER_PATH, material, "_body");
            SetMaterial(PrefabSetupUtils.HAND_PATH, material, "_body");
            SetMaterial(PrefabSetupUtils.SLOT_PATH, material, "_block", "_icon");
        }

        /// <summary>Hud 아래에 화면 테두리 네 변(흰 점 스프라이트)을 만든다. Hud는 화면 원점에 있다.</summary>
        public static HitEdgeView BuildHitEdge(GameObject hudRoot)
        {
            var root = new GameObject("HitEdge");
            root.transform.SetParent(hudRoot.transform, false);
            root.transform.localPosition = Vector3.zero;
            HitEdgeView view = root.AddComponent<HitEdgeView>();
            Sprite white = ViewAssets.Instance.WhitePixel;
            SpriteRenderer Bar(string name)
            {
                var bar = new GameObject(name);
                bar.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = bar.AddComponent<SpriteRenderer>();
                renderer.sprite = white;
                renderer.enabled = false;
                return renderer;
            }

            PrefabSetupUtils.SetRefs(view, ("_top", Bar("Top")), ("_bottom", Bar("Bottom")), ("_left", Bar("Left")), ("_right", Bar("Right")));
            return view;
        }

        // 프리팹을 열어 컴포넌트의 렌더러 필드들에 머티리얼을 붙이고 저장한다
        private static void SetMaterial(string prefabPath, Material material, params string[] rendererFields)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    var serialized = new SerializedObject(component);
                    foreach (string field in rendererFields)
                    {
                        SerializedProperty property = serialized.FindProperty(field);
                        if (property != null && property.objectReferenceValue is SpriteRenderer renderer)
                        {
                            renderer.sharedMaterial = material;
                        }
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
