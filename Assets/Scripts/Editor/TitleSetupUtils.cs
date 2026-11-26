using ShiftMagicSpell.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShiftMagicSpell.Editor
{
    /// <summary>
    /// 첫 화면을 키캡 로고 연출로 바꾼다(Main 씬 Screens/Title 아래 Logo). 이미 Logo가 있으면 그대로 둔다(인스펙터에서 고친 값을 지킨다).
    /// 배치 모드: -executeMethod ShiftMagicSpell.Editor.TitleSetupUtils.AddTitleLogoToMainScene
    /// </summary>
    public static class TitleSetupUtils
    {
        [MenuItem("Tools/Shift Magic Spell/Add Title Logo To Main Scene (keeps edits)")]
        public static void AddTitleLogoToMainScene()
        {
            var scene = EditorSceneManager.OpenScene(SceneSetupUtils.SCENE_PATH, OpenSceneMode.Single);
            ScreensView screens = Object.FindAnyObjectByType<ScreensView>(FindObjectsInactive.Include);
            Transform title = screens != null ? screens.transform.Find("Title") : null;
            if (title == null)
            {
                Debug.LogError($"[ERROR] TitleSetupUtils::AddTitleLogoToMainScene() - Screens/Title not found. Details: [Scene={SceneSetupUtils.SCENE_PATH}]");
                return;
            }

            if (title.Find("Logo") == null)
            {
                HudSetupUtils.BuildTitleLogo(screens, title.gameObject);
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[INFO] TitleSetupUtils::AddTitleLogoToMainScene() - Title logo ready. Details: [Scene={SceneSetupUtils.SCENE_PATH}]");
        }
    }
}
