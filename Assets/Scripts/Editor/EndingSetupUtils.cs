using ShiftMagicSpell.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShiftMagicSpell.Editor
{
    /// <summary>
    /// 엔딩 연출 준비: 새 그림(CursorShift, Poof)의 임포트 설정·스프라이트 목록, 새 효과음(Crack, Pop, Zip)의 소리 목록,
    /// Main 씬 Screens 아래 엔딩 자막. 이미 있으면 그대로 둔다(인스펙터에서 고친 값을 지킨다).
    /// 배치 모드: -executeMethod ShiftMagicSpell.Editor.EndingSetupUtils.SetupAll
    /// </summary>
    public static class EndingSetupUtils
    {
        /// <summary>그림·소리 목록을 갱신하고 씬에 엔딩 자막을 넣는다</summary>
        public static void SetupAll()
        {
            AsepriteImportUtils.ApplyAll();
            ArtCatalogBuildUtils.Build();
            AudioSetupUtils.SetupAudio();
            AddEndingToMainScene();
        }

        [MenuItem("Tools/Shift Magic Spell/Add Ending To Main Scene (keeps edits)")]
        public static void AddEndingToMainScene()
        {
            var scene = EditorSceneManager.OpenScene(SceneSetupUtils.SCENE_PATH, OpenSceneMode.Single);
            ScreensView screens = Object.FindAnyObjectByType<ScreensView>(FindObjectsInactive.Include);
            if (screens == null)
            {
                Debug.LogError($"[ERROR] EndingSetupUtils::AddEndingToMainScene() - ScreensView not found. Details: [Scene={SceneSetupUtils.SCENE_PATH}]");
                return;
            }

            if (screens.transform.Find("Ending") == null)
            {
                HudSetupUtils.BuildEnding(screens);
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[INFO] EndingSetupUtils::AddEndingToMainScene() - Ending caption ready. Details: [Scene={SceneSetupUtils.SCENE_PATH}]");
        }
    }
}
