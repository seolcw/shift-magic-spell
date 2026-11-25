using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ShiftMagicSpell.Audio;
using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.Tests
{
    /// <summary>엔딩 연출 규칙(씬 없이): 단계 순서, 건너뛰기, 자막 글자, 단계마다 소리, 재시작, 금 갈 때 흔들림.</summary>
    public class EndingTests
    {
        private const float DT = 1f / 60f;
        private GameSession _game;

        [SetUp]
        public void SetUp()
        {
            _game = TestDataUtils.CreateSession(5);

            // 단계 시각을 정확히 재므로 마무리 명중의 타격 멈춤은 끈다
            TestDataUtils.DisableHitStop(_game);
        }

        private EndingValues Ending => _game.Data.Ending;

        private void CastFinisher()
        {
            _game.StartPhase(_game.Data.Phases.Count - 1);
            _game.CastSpell(_game.Data.FinisherWord, _game.Hand.Pos + Vector2.down * 3f, 1f);
            Assert.AreEqual(GameState.Win, _game.State);
        }

        [Test]
        public void Stages_FollowTime_InOrder()
        {
            CastFinisher();
            var seen = new List<EndingStage> { _game.EndingStage };
            for (float t = 0f; t < Ending.EndAt + 0.5f; t += DT)
            {
                _game.Update(DT);
                if (_game.EndingStage != seen[seen.Count - 1])
                {
                    seen.Add(_game.EndingStage);
                }
            }

            CollectionAssert.AreEqual(new[]
            {
                EndingStage.Impact, EndingStage.Crack1, EndingStage.Crack2, EndingStage.Hourglass, EndingStage.Beam,
                EndingStage.Arrow, EndingStage.Zip, EndingStage.Caption, EndingStage.Done,
            }, seen);
            Assert.IsTrue(_game.IsEndingDone);
        }

        [Test]
        public void Skip_JumpsStraightToResults()
        {
            CastFinisher();
            for (float t = 0f; t < Ending.Crack2At + 0.1f; t += DT)
            {
                _game.Update(DT);
            }

            Assert.AreEqual(EndingStage.Crack2, _game.EndingStage);
            _game.SkipEnding();
            Assert.IsTrue(_game.IsEndingDone);
            Assert.AreEqual(EndingStage.Done, _game.EndingStage);
            Assert.AreEqual(0, _game.EndingCaptionLength, "건너뛰면 자막도 없이 결과 화면");
        }

        [Test]
        public void Skip_DoesNothing_OutsideEnding()
        {
            _game.StartPhase(0);
            float stateT = _game.StateT;
            _game.SkipEnding();
            Assert.AreEqual(GameState.Play, _game.State);
            Assert.AreEqual(stateT, _game.StateT);
            Assert.AreEqual(EndingStage.None, _game.EndingStage);
        }

        [Test]
        public void Caption_TypesOneLetterAtATime()
        {
            Assert.AreEqual(0, Ending.GetCaptionLength(Ending.CaptionAt - 0.01f));
            Assert.AreEqual(1, Ending.GetCaptionLength(Ending.CaptionAt));
            Assert.AreEqual(4, Ending.GetCaptionLength(Ending.CaptionAt + Ending.CaptionCharTime * 3f + 0.001f));
            Assert.AreEqual(Ending.Caption.Length, Ending.GetCaptionLength(Ending.EndAt));
            Assert.LessOrEqual(Ending.CaptionAt + Ending.CaptionCharTime * Ending.Caption.Length, Ending.EndAt, "자막이 다 찍힌 뒤에 결과 화면");
        }

        [Test]
        public void EachStage_PlaysItsSound_AndCaptionTicks()
        {
            CastFinisher();
            var watcher = new SoundEventWatcher();
            watcher.Bind(_game);
            var played = new List<SoundKey>();
            for (float t = 0f; t < Ending.EndAt + 0.5f; t += DT)
            {
                _game.Update(DT);
                played.AddRange(watcher.Poll());
            }

            SoundKey[] stageKeys = { SoundKey.EndingCrack, SoundKey.EndingCrackBig, SoundKey.EndingPop1, SoundKey.EndingPop2, SoundKey.EndingPop3, SoundKey.EndingZip };
            CollectionAssert.AreEqual(stageKeys, played.Where(key => stageKeys.Contains(key)).ToList(), "단계마다 한 번씩, 순서대로");
            Assert.AreEqual(Ending.Caption.Length, played.Count(key => key == SoundKey.EndingType), "자막 글자마다 한 번");
        }

        [Test]
        public void Crack_ShakesScreen()
        {
            CastFinisher();
            for (float t = 0f; t < Ending.CrackAt + DT && _game.EndingStage != EndingStage.Crack1; t += DT)
            {
                _game.Update(DT);
            }

            for (int i = 0; i < 3 && _game.EndingStage != EndingStage.Crack1; i++)
            {
                _game.Update(DT);
            }

            Assert.AreEqual(EndingStage.Crack1, _game.EndingStage);
            Assert.IsTrue(_game.Feel.Shake.IsActive, "금이 가는 순간 화면이 툭 흔들린다");
        }

        [Test]
        public void RestartDuringEnding_StartsNewRun()
        {
            CastFinisher();
            for (float t = 0f; t < Ending.BeamAt; t += DT)
            {
                _game.Update(DT);
            }

            _game.RestartPhase();
            Assert.AreEqual(GameState.Play, _game.State);
            Assert.AreEqual(0, _game.Phase);
            Assert.AreEqual(EndingStage.None, _game.EndingStage);
        }

        [Test]
        public void EveryStageSprite_ExistsInCatalog()
        {
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/Data/ArtCatalog.asset");
            Assert.IsNotNull(catalog);
            foreach (string tag in new[] { "Crack", "Hourglass", "Beam", "Arrow" })
            {
                Assert.IsNotNull(catalog.GetFrame("CursorShift", tag, 0).Sprite, tag);
            }

            Assert.IsNotNull(catalog.GetFrame("Poof", "Poof", 0).Sprite);
        }
    }
}
