using System.Collections;
using NUnit.Framework;
using ShiftMagicSpell.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ShiftMagicSpell.Tests
{
    /// <summary>
    /// 타격감이 실제 화면에 어떻게 보이는지 PNG로 남긴다(Logs/Screens/hit-*): 플레이어 피격, 마법·콤보 명중, 블록 금,
    /// 마무리 SHIFT, HP 1 테두리. 맞는 순간(멈춤 중)과 조금 뒤(흔들림·조각)를 찍는다. 예외 없이 그려지는지도 확인.
    /// </summary>
    public class HitFeedbackCaptureTests
    {
        [UnityTest]
        public IEnumerator CaptureHitMoments()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            GameSession game = Object.FindFirstObjectByType<GameRunner>().Session;

            // 1. 플레이어 피격: 왼쪽에서 손에 맞아 오른쪽으로 밀린다
            game.StartPhase(0);
            game.IsTutorialDone = true;
            yield return Frames(3);
            PlayerController player = game.Player;
            player.Board = 0;
            player.R = 2;
            player.C = 3;
            game.Hand.Pos = GeometryUtils.CellCenter(0, 2, 3) + new Vector2(-2.5f, -0.5f);
            player.Invuln = 0f;
            player.Hurt("CAPTURE");
            yield return null;
            Assert.IsTrue(game.Feel.IsHitStopped, "맞은 다음 프레임은 멈춤 중");
            ScreenCaptureUtils.Capture("hit-player-1stop");
            yield return Frames(5);
            ScreenCaptureUtils.Capture("hit-player-2after");

            // 2. 마법 명중과 콤보 명중: 손이 맞는 프레임(멈춤 중)과 조금 뒤
            foreach ((string name, float multiplier) in new[] { ("spell", 1f), ("combo", 1.5f) })
            {
                game.StartPhase(0);
                game.IsTutorialDone = true;
                game.IsInvincible = true;
                game.Hand.Hp = 999;
                yield return Frames(3);
                int serial = game.Feel.Serial;
                game.CastSpell("FIRE", GeometryUtils.CellCenter(0, 3, 3), multiplier);
                yield return WaitUntilHit(game, serial);
                ScreenCaptureUtils.Capture($"hit-{name}-1stop");
                yield return Frames(6);
                ScreenCaptureUtils.Capture($"hit-{name}-2after");
            }

            // 3. 블록 금: 오른쪽 위 슬롯의 블록이 손에 맞는다
            game.StartPhase(0);
            game.IsTutorialDone = true;
            game.IsInvincible = true;
            BoardCell slot = game.Boards[1].Slots[0];
            slot.Block = new AttackBlock { Word = "FIRE", Hp = 2, Age = 1f };
            yield return Frames(3);
            var attack = new AttackPlan
            {
                Type = AttackType.Slot,
                Name = "slot",
                Board = 1,
                Slot = slot,
                Cells = new System.Collections.Generic.List<BoardCell> { slot },
                Center = GeometryUtils.CellCenter(1, slot.R, slot.C),
            };
            game.Hand.ForceFire(attack);
            yield return null;
            ScreenCaptureUtils.Capture("hit-block-1stop");
            yield return Frames(6);
            ScreenCaptureUtils.Capture("hit-block-2after");

            // 4. 마무리 SHIFT 명중
            game.StartPhase(game.Data.Phases.Count - 1);
            game.IsTutorialDone = true;
            yield return Frames(3);
            int finisherSerial = game.Feel.Serial;
            game.CastSpell(game.Data.FinisherWord, GeometryUtils.CellCenter(0, 3, 3), 1f);
            yield return WaitUntilHit(game, finisherSerial);
            ScreenCaptureUtils.Capture("hit-finisher-1stop");
            yield return Frames(8);
            ScreenCaptureUtils.Capture("hit-finisher-2after");

            // 5. HP 1: 화면 테두리가 은은하게 뛴다
            game.StartPhase(0);
            game.IsTutorialDone = true;
            game.IsInvincible = true;
            game.Player.Hp = 1;
            yield return Frames(20);
            ScreenCaptureUtils.Capture("hit-lowhp-edge");

            Assert.Pass("captured to Logs/Screens/hit-*");
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        // 마법이 손에 닿을 때까지(타격 번호가 바뀔 때까지) 기다린다. 멈춤 중에는 게임 시간이 서므로 프레임으로 센다.
        private static IEnumerator WaitUntilHit(GameSession game, int serial)
        {
            for (int i = 0; i < 300 && game.Feel.Serial == serial; i++)
            {
                yield return null;
            }

            Assert.AreNotEqual(serial, game.Feel.Serial, "마법이 손에 닿아야 함");
        }
    }
}
