using System.Linq;
using NUnit.Framework;
using ShiftMagicSpell.Core;

namespace ShiftMagicSpell.Tests
{
    /// <summary>연습 구간(새 판의 첫 단어)과 처음 하는 사람이 버티게 바꾼 규칙.</summary>
    public class TutorialTests
    {
        private const float DT = 1f / 60f;
        private GameSession _game;

        [SetUp]
        public void SetUp()
        {
            _game = TestDataUtils.CreateSession(3);
        }

        private void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += DT)
            {
                _game.Update(DT);
            }
        }

        [Test]
        public void NewRun_FirstWordIsPractice_HandNeverAttacks_HintStaysOn()
        {
            _game.StartNewRun();
            Assert.IsTrue(_game.IsTutorial);
            Assert.AreEqual(0, _game.TutorialStep, "처음 안내는 이동");
            Step(30f);
            Assert.AreEqual(0, _game.Stats.Attacks.Values.Sum(), "연습 동안 손은 공격하지 않는다");
            Assert.AreEqual(_game.Config.PlayerHp, _game.Player.Hp);
            Assert.AreEqual(1f, _game.HintAlpha(), "연습 동안 글자 위치는 계속 보인다");
        }

        [Test]
        public void TutorialSteps_Advance_AndFirstShotEndsPractice()
        {
            _game.StartNewRun();
            PlayerController player = _game.Player;
            player.Press(MoveDir.Right);
            player.Release(MoveDir.Right);
            Assert.AreEqual(1, _game.TutorialStep, "이동했으면 줄 돌리기 안내");

            Step(0.3f);
            player.IsShiftHeld = true;
            player.Press(MoveDir.Up);
            player.Release(MoveDir.Up);
            player.IsShiftHeld = false;
            Assert.AreEqual(2, _game.TutorialStep, "돌렸으면 단어 맞추기 안내");

            TestDataUtils.PlaceWord(_game, 0, _game.Words.Current);
            Assert.AreEqual(3, _game.TutorialStep, "블록이 생기면 반대편 슬롯 안내");
            Assert.IsTrue(_game.IsTutorial, "쏘기 전까지는 연습");

            BoardCell slot = _game.Boards[1].Slots.First(s => s.Block != null);
            player.Board = 1;
            player.R = slot.R;
            player.C = slot.C;
            Assert.IsTrue(player.Fire());
            Assert.IsFalse(_game.IsTutorial, "첫 블록을 쏘면 연습 끝");

            Step(10f);
            Assert.Greater(_game.Stats.Attacks.Values.Sum(), 0, "연습이 끝나면 손이 공격한다");
        }

        [Test]
        public void StartAndDeathRestart_GiveThreeSecondsBeforeFirstAttack()
        {
            // 쓰러짐 화면 시간을 정확히 재므로 사망 타격 멈춤은 끈다(타격감은 HitFeedbackTests에서)
            TestDataUtils.DisableHitStop(_game);
            _game.StartPhase(0);
            Assert.AreEqual(_game.Config.StartFirstAttackDelay, _game.Hand.T, 0.001f);
            _game.IsInvincible = true;
            Step(_game.Config.StartFirstAttackDelay - 0.1f);
            Assert.IsNull(_game.Hand.Attack, "시작 뒤 3초 동안 공격 준비도 하지 않는다");

            _game.IsInvincible = false;
            _game.Player.Hp = 1;
            _game.Player.Invuln = 0f;
            _game.Player.Hurt("TEST");
            Step(1.4f);
            Assert.AreEqual(GameState.Play, _game.State);
            Step(_game.Config.StartFirstAttackDelay - 0.2f);
            Assert.IsNull(_game.Hand.Attack, "쓰러진 뒤 다시 시작해도 3초 여유");
        }

        [Test]
        public void PhaseOne_DoesNotTargetLinedUpLetters()
        {
            Assert.IsFalse(_game.Data.Phases[0].IsTargetingProgressLine, "페이즈 1은 맞춘 줄을 노리는 줄 섞기 가산점 없음");
            Assert.IsTrue(_game.Data.Phases[1].IsTargetingProgressLine);
            Assert.GreaterOrEqual(_game.Config.AttackInterval * _game.Data.Phases[0].IntervalMul, 3f, "페이즈 1 공격 간격 3초 이상");
        }

        [Test]
        public void RefusedRotation_ExplainsWhy()
        {
            _game.StartPhase(0);
            PlayerController player = _game.Player;
            player.Board = 0;
            player.R = _game.Boards[0].ShiftCell.R;
            player.C = _game.Boards[0].ShiftCell.C;
            player.IsShiftHeld = true;
            Assert.IsFalse(player.Rotate(MoveDir.Left));
            StringAssert.Contains("SHIFT KEY", _game.Toast?.Text);
        }
    }
}
