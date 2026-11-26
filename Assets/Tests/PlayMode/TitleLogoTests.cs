using System.Collections;
using NUnit.Framework;
using ShiftMagicSpell.Core;
using ShiftMagicSpell.View;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ShiftMagicSpell.Tests
{
    /// <summary>
    /// 첫 화면 키캡 로고: 시각표 규칙(뒤섞여 시작 → 차례로 맞춰짐 → 명중 뒤 CLICK TO START)과,
    /// Main 씬에서 실제로 그려지는지(단계마다 PNG: Logs/Screens/title-*).
    /// </summary>
    public class TitleLogoTests
    {
        private static bool IsSolved(float offset, int length)
        {
            float rest = Mathf.Repeat(offset, length);
            return rest < 0.001f || rest > length - 0.001f;
        }

        [Test]
        public void Rows_StartScrambled_ThenSolveInOrder()
        {
            var v = new TitleLogoValues();
            Assert.IsFalse(IsSolved(v.GetTopOffset(0f), v.Top.Length), "위 줄은 뒤섞여 시작");
            Assert.IsFalse(IsSolved(v.GetBottomOffset(0f), v.Bottom.Length), "아래 줄도 뒤섞여 시작");

            Assert.IsTrue(IsSolved(v.GetTopOffset(v.TopSolvedAt), v.Top.Length), "위 줄이 먼저 맞춰짐");
            Assert.IsFalse(IsSolved(v.GetBottomOffset(v.TopSolvedAt), v.Bottom.Length), "그때 아래 줄은 아직");
            Assert.IsTrue(IsSolved(v.GetBottomOffset(v.BottomSolvedAt), v.Bottom.Length), "그다음 아래 줄");
            Assert.Less(v.BottomSolvedAt, v.ShotAt);
            Assert.Less(v.ShotAt, v.HitAt);
            Assert.Less(v.HitAt, v.Duration);
        }

        [Test]
        public void Rows_MoveOneKeyPerPress_WithoutJumping()
        {
            var v = new TitleLogoValues();
            float previous = v.GetTopOffset(0f);
            for (float t = 0f; t <= v.Duration; t += 1f / 120f)
            {
                float offset = v.GetTopOffset(t);
                Assert.LessOrEqual(Mathf.Abs(offset - previous), 0.25f, $"한 프레임에 크게 튀지 않음 (t={t:0.00})");
                Assert.GreaterOrEqual(offset, previous - 0.0001f, "위 줄은 한 방향(왼쪽)으로만");
                previous = offset;
            }
        }

        [Test]
        public void Prompt_ShowsOnlyAfterTheFirstHit_ThenKeepsBlinking()
        {
            var v = new TitleLogoValues();
            for (float t = 0f; t < v.HitAt; t += 0.05f)
            {
                Assert.IsFalse(v.IsPromptVisible(t), $"명중 전에는 안 보임 (t={t:0.00})");
            }

            int visibleInFirstLoop = 0;
            int visibleInSecondLoop = 0;
            for (float t = v.HitAt; t < v.Duration * 2f; t += 0.05f)
            {
                if (v.IsPromptVisible(t))
                {
                    if (t < v.Duration)
                    {
                        visibleInFirstLoop++;
                    }
                    else
                    {
                        visibleInSecondLoop++;
                    }
                }
            }

            Assert.Greater(visibleInFirstLoop, 0);
            Assert.Greater(visibleInSecondLoop, 0, "두 번째 바퀴(줄이 다시 뒤섞일 때)에도 계속 깜빡임");
        }

        [UnityTest]
        public IEnumerator TitleScene_DrawsLogo_AndCapturesSteps()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            GameSession game = Object.FindFirstObjectByType<GameRunner>().Session;
            Assert.AreEqual(GameState.Ready, game.State, "씬은 첫 화면으로 시작");
            TitleLogoView logo = Object.FindFirstObjectByType<TitleLogoView>();
            Assert.IsNotNull(logo, "Screens/Title/Logo가 씬에 있어야 함(Tools > Add Title Logo To Main Scene)");
            PixelCanvas canvas = logo.GetComponent<PixelCanvas>();
            TitleLogoValues v = logo.Values;

            (string Name, float At)[] shots =
            {
                ("title-1-scrambled", 0.3f),
                ("title-2-shift-top", v.TopFrom + v.Step + v.Slide * 0.5f),
                ("title-3-shift-bottom", v.BottomFrom + v.Step * 2f + v.Slide * 0.5f),
                ("title-4-shot", v.ShotAt + v.FlyTime * 0.5f),
                ("title-5-hit", v.HitAt + 0.07f),
                ("title-6-prompt", v.HitAt + v.PromptDelay + 0.1f),
            };
            Time.captureFramerate = 30;
            try
            {
                foreach ((string name, float at) in shots)
                {
                    for (int i = 0; i < 600 && logo.TotalTime < at; i++)
                    {
                        yield return null;
                    }

                    Assert.Greater(canvas.DrawCount, 20, $"{name}: 로고가 그려져야 함");
                    ScreenCaptureUtils.Capture(name);
                }
            }
            finally
            {
                Time.captureFramerate = 0;
            }

            Assert.AreEqual(GameState.Ready, game.State, "로고 연출은 게임을 시작하지 않는다");
        }
    }
}
