using System.Linq;
using NUnit.Framework;
using ShiftMagicSpell.Audio;
using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.Tests
{
    /// <summary>
    /// 타격감 규칙(씬 없이 GameSession만): 멈춤(히트스톱)은 로직만 세우고 입력은 잃지 않는다, 멈춤 간격, 맞은 방향,
    /// 마법이 손에 닿는 순간의 번쩍임·넉백·조각, 블록 번쩍임, 흔들림 배율, 소리 세기.
    /// </summary>
    public class HitFeedbackTests
    {
        private const float DT = 1f / 60f;
        private GameSession _game;

        [SetUp]
        public void SetUp()
        {
            _game = TestDataUtils.CreateSession(3);
        }

        private HitFeelValues Feel => _game.Data.HitFeel;

        private void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += DT)
            {
                _game.Update(DT);
            }
        }

        private void StepUntilNotStopped()
        {
            for (int i = 0; i < 120 && _game.Feel.IsHitStopped; i++)
            {
                _game.Update(DT);
            }
        }

        [Test]
        public void PlayerHurt_StopsLogic_ThenResumes()
        {
            _game.StartPhase(0);
            _game.Player.Invuln = 0f;
            Assert.IsTrue(_game.Player.Hurt("TEST"));
            Assert.IsTrue(_game.Feel.IsHitStopped);
            Assert.AreEqual(Feel.PlayerHurt.HitStop, _game.Feel.HitStopT, 0.001f);

            float time = _game.Time;
            float invuln = _game.Player.Invuln;
            _game.Update(DT);
            Assert.AreEqual(time, _game.Time, "멈춘 동안 로직 시간은 흐르지 않는다");
            Assert.AreEqual(invuln, _game.Player.Invuln, "멈춘 동안 무적 시간도 줄지 않는다");
            Assert.IsTrue(_game.Feel.PlayerFlash.IsActive, "멈춘 동안에도 번쩍임은 보인다");

            Step(Feel.PlayerHurt.HitStop + DT * 2f);
            Assert.IsFalse(_game.Feel.IsHitStopped);
            Assert.Greater(_game.Time, time, "멈춤이 끝나면 다시 흐른다");
        }

        [Test]
        public void MoveDuringHitStop_IsQueued_AndRunsAfter()
        {
            _game.StartPhase(0);
            _game.IsInvincible = true;
            PlayerController player = _game.Player;
            player.Board = 0;
            player.R = 2;
            player.C = 1;
            _game.Feel.RequestHitStop(0.1f, false);

            player.Press(MoveDir.Right);
            player.Release(MoveDir.Right);
            Assert.AreEqual(1, player.C, "멈춘 동안에는 움직이지 않는다");
            Step(0.1f + DT * 3f);
            Assert.AreEqual(2, player.C, "멈춤 중에 누른 이동은 버리지 않고 멈춤이 끝나면 실행한다");
        }

        [Test]
        public void SpaceDuringHitStop_FiresAfterStop()
        {
            _game.StartPhase(0);
            _game.IsTutorialDone = true;
            BoardCell slot = _game.Boards[1].Slots[0];
            slot.Block = new AttackBlock { Word = "FIRE", Hp = 2 };
            PlayerController player = _game.Player;
            player.Board = 1;
            player.R = slot.R;
            player.C = slot.C;
            int handHp = _game.Hand.Hp;
            _game.Feel.RequestHitStop(0.1f, false);

            Assert.IsFalse(player.Fire(), "멈춘 동안에는 바로 쏘지 않는다");
            Assert.IsNotNull(slot.Block);
            Step(0.1f + DT * 3f);
            Assert.IsNull(slot.Block, "멈춤이 끝나면 예약한 발동을 한다");
            Assert.Less(_game.Hand.Hp, handHp);
        }

        [Test]
        public void HitStopGap_SkipsBackToBackStops_ButPriorityStops()
        {
            _game.StartPhase(0);
            _game.Feel.RequestHitStop(0.05f, false);
            StepUntilNotStopped();
            _game.Feel.RequestHitStop(0.05f, false);
            Assert.IsFalse(_game.Feel.IsHitStopped, "멈춤이 끝난 직후의 보통 멈춤은 건너뛴다");
            _game.Feel.RequestHitStop(0.05f, true);
            Assert.IsTrue(_game.Feel.IsHitStopped, "사망·마무리 멈춤은 간격과 상관없이 멈춘다");

            StepUntilNotStopped();
            Step(Feel.HitStopGap + DT * 2f);
            _game.Feel.RequestHitStop(0.05f, false);
            Assert.IsTrue(_game.Feel.IsHitStopped, "간격이 지나면 다시 멈출 수 있다");
        }

        [Test]
        public void OverlappingStops_TakeLonger_NotSum()
        {
            _game.StartPhase(0);
            _game.Feel.RequestHitStop(0.08f, false);
            _game.Feel.RequestHitStop(0.05f, false);
            Assert.AreEqual(0.08f, _game.Feel.HitStopT, 0.001f);
            _game.Feel.RequestHitStop(0.2f, false);
            Assert.AreEqual(0.2f, _game.Feel.HitStopT, 0.001f);
        }

        [Test]
        public void PlayerHurt_PushesAwayFromHand_FlashesEdge_AndLeavesDebris()
        {
            _game.StartPhase(0);
            PlayerController player = _game.Player;
            player.Board = 0;
            player.R = 2;
            player.C = 2;
            Vector2 center = GeometryUtils.CellCenter(0, 2, 2);
            _game.Hand.Pos = center + new Vector2(-3f, 0f);
            player.Invuln = 0f;
            player.Hurt("TEST");

            Assert.Greater(_game.Feel.PlayerKnock.Dir.x, 0.9f, "왼쪽 손에 맞으면 오른쪽으로 밀린다");
            Assert.AreEqual(Feel.PlayerHurt.KnockPx, _game.Feel.PlayerKnock.Amount, 0.001f);
            Assert.IsTrue(_game.Feel.PlayerFlash.IsActive);
            Assert.IsTrue(_game.Feel.PlayerSquash.IsActive);
            Assert.IsTrue(_game.Feel.Edge.IsActive, "화면 테두리가 빨갛게");
            Assert.IsTrue(_game.Feel.Shake.IsActive);
            FxEvent debris = _game.Fx.FirstOrDefault(fx => fx.Kind == FxKind.Debris);
            Assert.IsNotNull(debris);
            Assert.AreEqual(Feel.PlayerHurt.Debris, debris.Count);
            Assert.AreEqual(HitKind.PlayerHurt, _game.Feel.LastKind);
        }

        [Test]
        public void LastHp_UsesDeathLevel()
        {
            _game.StartPhase(0);
            _game.Player.Hp = 1;
            _game.Player.Invuln = 0f;
            _game.Player.Hurt("TEST");
            Assert.AreEqual(GameState.Dead, _game.State);
            Assert.AreEqual(HitKind.PlayerDeath, _game.Feel.LastKind);
            Assert.AreEqual(Feel.PlayerDeath.HitStop, _game.Feel.HitStopT, 0.001f);
        }

        [Test]
        public void SpellImpact_FlashesHand_PushesAwayFromCaster()
        {
            _game.StartPhase(0);
            _game.IsInvincible = true;
            _game.Hand.Hp = 99;
            Vector2 from = _game.Hand.Pos + new Vector2(0f, 4f);
            int serial = _game.Feel.Serial;
            _game.CastSpell("FIRE", from, 1f);
            Assert.AreEqual(serial, _game.Feel.Serial, "시전 순간에는 아직 맞지 않았다");

            Step(_game.Config.SpellTime * 0.55f + DT);
            Assert.AreEqual(serial + 1, _game.Feel.Serial, "마법이 손에 닿는 순간 타격");
            Assert.AreEqual(HitKind.Spell, _game.Feel.LastKind);
            Assert.IsTrue(_game.Feel.HandFlash.IsActive);
            Assert.Less(_game.Feel.HandKnock.Dir.y, -0.9f, "아래에서 날아온 마법에 맞으면 위로 밀린다");
            Assert.AreEqual(HandController.ANIM_HURT, _game.Hand.Anim);
            Assert.IsTrue(_game.Fx.Any(fx => fx.Kind == FxKind.Debris && fx.Style == "hand"));
        }

        [Test]
        public void SpellKind_FollowsMultiplier()
        {
            _game.StartPhase(0);
            _game.IsInvincible = true;
            _game.Hand.Hp = 99;
            (float Mul, HitKind Kind)[] cases = { (1f, HitKind.Spell), (1.5f, HitKind.Combo), (0.8f, HitKind.Overflow) };
            foreach ((float mul, HitKind kind) in cases)
            {
                _game.CastSpell("FIRE", _game.Hand.Pos + Vector2.down, mul);
                Step(_game.Config.SpellTime);
                StepUntilNotStopped();
                Assert.AreEqual(kind, _game.Feel.LastKind, $"배율 {mul}");
            }
        }

        [Test]
        public void TwoSpellsInFlight_BothLand()
        {
            // 콤보는 앞 마법이 닿기 전에 다음 마법을 쏠 수 있다: 두 번 다 손에 맞아야 한다(예전에는 앞 것이 덮였다)
            _game.StartPhase(0);
            _game.IsInvincible = true;
            _game.Hand.Hp = 99;
            int serial = _game.Feel.Serial;
            _game.CastSpell("FIRE", _game.Hand.Pos + Vector2.down, 1f);
            Step(0.1f);
            _game.CastSpell("BOLT", _game.Hand.Pos + Vector2.down, 1.5f);
            Step(_game.Config.SpellTime * 2f);
            Assert.AreEqual(serial + 2, _game.Feel.Serial);
        }

        [Test]
        public void FinisherImpact_IsPriorityStop()
        {
            // 마법이 닿기 0.1초 전에 짧게 멈춰서, 닿는 순간이 멈춤 간격(0.15초) 안에 오게 한다
            _game.StartPhase(_game.Data.Phases.Count - 1);
            _game.CastSpell(_game.Data.FinisherWord, _game.Hand.Pos + Vector2.down, 1f);
            Step(_game.Config.SpellTime * 0.55f - 0.1f);
            _game.Feel.RequestHitStop(0.02f, false);
            StepUntilNotStopped();
            Step(0.12f);
            Assert.AreEqual(HitKind.Finisher, _game.Feel.LastKind);
            Assert.IsTrue(_game.Feel.IsHitStopped, "마무리 SHIFT는 직전 멈춤 간격 안이어도 멈춘다");
        }

        [Test]
        public void SlotHit_FlashesBlock_AndBreakIsStronger()
        {
            _game.StartPhase(0);
            BoardCell slot = _game.Boards[1].Slots[0];
            slot.Block = new AttackBlock { Word = "FIRE", Hp = 2 };
            _game.Player.Board = 0;
            var attack = new AttackPlan { Type = AttackType.Slot, Name = "slot", Board = 1, Slot = slot, Cells = new System.Collections.Generic.List<BoardCell> { slot } };
            _game.Hand.ForceFire(attack);
            Assert.Greater(slot.Block.FlashT, 0f, "금이 가는 순간 블록이 번쩍인다");
            Assert.AreEqual(HitKind.BlockChip, _game.Feel.LastKind);
            Assert.IsTrue(_game.Fx.Any(fx => fx.Kind == FxKind.Debris && fx.Style == "block"));

            _game.Feel.Reset();
            _game.Hand.ForceFire(attack);
            Assert.IsNull(slot.Block);
            Assert.AreEqual(HitKind.BlockBreak, _game.Feel.LastKind);
            Assert.Greater(Feel.BlockBreak.ShakePx, Feel.BlockChip.ShakePx);
        }

        [Test]
        public void ShakeScaleZero_TurnsShakeOff_AndFlashScaleZero_TurnsFlashOff()
        {
            Feel.ShakeScale = 0f;
            Feel.FlashScale = 0f;
            _game.StartPhase(0);
            _game.Player.Invuln = 0f;
            _game.Player.Hurt("TEST");
            Assert.IsFalse(_game.Feel.Shake.IsActive, "흔들림 줄이기: 흔들림 없음");
            Assert.IsFalse(_game.Feel.PlayerFlash.IsActive, "번쩍임 줄이기: 번쩍임 없음");
            Assert.IsTrue(_game.Feel.IsHitStopped, "멈춤은 그대로");
        }

        [Test]
        public void BigHits_SoundLowerOrLouder_ThanSmallHits()
        {
            Assert.Less(Feel.Finisher.SoundPitch, 1f, "마무리는 낮고 묵직하게");
            Assert.Less(Feel.PlayerDeath.SoundPitch, 1f);
            Assert.Less(Feel.Overflow.SoundVolume, Feel.Spell.SoundVolume, "80% 발동은 작게");
            CollectionAssert.Contains(SoundEventWatcher.GetHitKeys(HitKind.Combo), SoundKey.SpellImpact);
            CollectionAssert.Contains(SoundEventWatcher.GetHitKeys(HitKind.PlayerDeath), SoundKey.Death);
        }

        [Test]
        public void SoundWatcher_WeightsTheHitThisFrame()
        {
            _game.StartPhase(0);
            var watcher = new SoundEventWatcher();
            watcher.Bind(_game);
            _game.Player.Invuln = 0f;
            _game.Player.Hp = 1;
            _game.Player.Hurt("TEST");
            watcher.Poll();
            Assert.IsTrue(watcher.TryGetWeight(SoundKey.Death, out float volume, out float pitch));
            Assert.AreEqual(Feel.PlayerDeath.SoundVolume, volume, 0.001f);
            Assert.AreEqual(Feel.PlayerDeath.SoundPitch, pitch, 0.001f);

            watcher.Poll();
            Assert.IsFalse(watcher.TryGetWeight(SoundKey.Death, out _, out _), "다음 프레임에는 세기가 남지 않는다");
        }
    }
}
