using System.Linq;
using NUnit.Framework;
using ShiftMagicSpell.Core;
using UnityEngine;

namespace ShiftMagicSpell.Tests
{
    /// <summary>페이즈 전환·마무리 연출 규칙: 전환 중 공격 없음·무적, 배너 뒤 첫 공격 여유, FINAL 동안 손 대기.</summary>
    public class PhaseTransitionTests
    {
        private const float DT = 1f / 60f;
        private GameSession _game;

        [SetUp]
        public void SetUp()
        {
            _game = TestDataUtils.CreateSession(7);

            // 전환 시간을 정확히 재므로 마법이 손에 맞는 순간의 타격 멈춤은 끈다(타격감은 HitFeedbackTests에서)
            TestDataUtils.DisableHitStop(_game);
        }

        private void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += DT)
            {
                _game.Update(DT);
            }
        }

        private int AttackCount()
        {
            return _game.Stats.Attacks.Values.Sum();
        }

        [Test]
        public void PhaseClear_CancelsAttacks_ThenIntro_ThenFirstAttackWaits()
        {
            _game.StartPhase(0);

            // 손이 공격을 고르고 소환 탄이 날아다닐 만큼 진행(그동안 쓰러지지 않게 잠깐 무적)
            _game.IsInvincible = true;
            Step(6f);
            _game.IsInvincible = false;
            _game.Hand.Hp = 3;
            _game.CastSpell("FIRE", Vector2.zero, 1f);
            Assert.AreEqual(GameState.PhaseClear, _game.State);
            Assert.IsNull(_game.Hand.Attack, "진행 중인 공격은 거둔다");
            Assert.AreEqual(0, _game.Hand.Bullets.Count, "날아다니던 탄도 거둔다");

            int attacks = AttackCount();
            int playerHp = _game.Player.Hp;
            Assert.IsFalse(_game.Player.Hurt("test"), "전환 중에는 무적");

            Step(_game.Config.PhaseClearTime);
            Assert.AreEqual(GameState.PhaseIntro, _game.State, "물러난 뒤 다음 페이즈 배너");
            Assert.AreEqual(1, _game.Phase);
            Assert.IsFalse(_game.Player.Hurt("test"), "배너 중에도 무적");

            Step(_game.Config.PhaseIntroTime);
            Assert.AreEqual(GameState.Play, _game.State);
            float hintStart = _game.HintT0;
            Assert.That(_game.Time - hintStart, Is.LessThan(0.1f), "배너가 끝나면 새 단어를 바로 한 번 깜빡인다");

            // 첫 공격 전 여유: 이 시간 안에는 예고(Travel/Warn)도 시작하지 않는다
            Step(_game.Config.PhaseFirstAttackDelay - 0.1f);
            Assert.IsNull(_game.Hand.Attack, "배너 뒤 첫 공격 전 여유");
            Assert.AreEqual(attacks, AttackCount(), "전환 내내 공격 없음");
            Assert.AreEqual(playerHp, _game.Player.Hp);
        }

        [Test]
        public void LastPhaseFinisher_ShowsFinal_AndHandHolds()
        {
            _game.StartPhase(2);
            _game.IsInvincible = true;
            Step(4f);
            _game.Hand.Hp = 7;
            int attacks = AttackCount();
            _game.CastSpell("FIRE", Vector2.zero, 1f);
            Assert.AreEqual("SHIFT", _game.Words.Current);
            Assert.AreEqual(_game.Config.FinisherIntroTime, _game.FinisherIntroT, 0.001f, "FINAL 배너 시작");
            Assert.IsNull(_game.Hand.Attack);
            Assert.AreEqual(0, _game.Hand.Bullets.Count);

            Step(_game.Config.FinisherIntroTime + _game.Config.PhaseFirstAttackDelay - 0.1f);
            Assert.AreEqual(0f, _game.FinisherIntroT, "배너는 정해진 시간 뒤 사라진다");
            Assert.AreEqual(attacks, AttackCount(), "FINAL 배너와 여유 시간 동안 손은 공격하지 않는다");
            Assert.AreEqual(GameState.Play, _game.State, "플레이어는 계속 움직일 수 있다");
        }
    }
}
