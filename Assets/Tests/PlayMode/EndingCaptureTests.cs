using System.Collections;
using NUnit.Framework;
using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ShiftMagicSpell.Tests
{
    /// <summary>엔딩 연출을 단계마다 PNG로 남긴다(Logs/Screens/ending-*): 금, 펑 모래시계, I, 화살표, 달아남, 자막, 결과 화면.</summary>
    public class EndingCaptureTests
    {
        [UnityTest]
        public IEnumerator CaptureEndingStages()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            GameSession game = Object.FindFirstObjectByType<GameRunner>().Session;
            EndingValues ending = game.Data.Ending;
            game.StartPhase(game.Data.Phases.Count - 1);
            game.IsTutorialDone = true;
            yield return null;
            game.CastSpell(game.Data.FinisherWord, GeometryUtils.CellCenter(0, 3, 3), 1f);

            (string Name, float At)[] shots =
            {
                ("ending-1-crack2", ending.Crack2At + 0.2f),
                ("ending-2-hourglass-poof", ending.HourglassAt + 0.08f),
                ("ending-3-hourglass", ending.HourglassAt + 0.5f),
                ("ending-4-beam", ending.BeamAt + 0.3f),
                ("ending-5-arrow", ending.ArrowAt + 0.5f),
                ("ending-6-zip", ending.ZipAt + ending.ZipTime * 0.6f),
                ("ending-7-caption", ending.EndAt - 0.3f),
                ("ending-8-results", ending.EndAt + 0.5f),
            };
            foreach ((string name, float at) in shots)
            {
                for (int i = 0; i < 1200 && game.StateT < at; i++)
                {
                    yield return null;
                }

                ScreenCaptureUtils.Capture(name);
            }

            Assert.IsTrue(game.IsEndingDone);
        }

        [UnityTest]
        public IEnumerator SkipInScene_ShowsResults()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            GameSession game = Object.FindFirstObjectByType<GameRunner>().Session;
            game.StartPhase(game.Data.Phases.Count - 1);
            game.IsTutorialDone = true;
            yield return null;
            game.CastSpell(game.Data.FinisherWord, GeometryUtils.CellCenter(0, 3, 3), 1f);
            for (int i = 0; i < 600 && game.EndingStage != EndingStage.Hourglass; i++)
            {
                yield return null;
            }

            game.SkipEnding();
            for (int i = 0; i < 30; i++)
            {
                yield return null;
            }

            Assert.IsTrue(game.IsEndingDone);
            ScreenCaptureUtils.Capture("ending-9-skipped");
        }
    }
}
