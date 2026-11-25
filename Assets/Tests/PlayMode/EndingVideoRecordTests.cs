using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using ShiftMagicSpell.Audio;
using ShiftMagicSpell.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ShiftMagicSpell.Tests
{
    /// <summary>
    /// 엔딩 영상 녹화(평소 테스트에서는 건너뜀, 이름으로 지정할 때만): 30fps 고정으로 마지막 페이즈에서 SHIFT를 맞춰
    /// 엔딩이 끝나고 결과 화면이 2초 보일 때까지 매 프레임 PNG를 남기고, 실제로 낸 소리(클립·크기·높이)를 프레임 번호와 함께 적는다.
    /// 결과: Logs/Video/ending/frame-0000.png…, sounds.tsv, music.tsv. 영상 합치기는 ArtSource/Scripts 밖 도구(ffmpeg)로.
    /// </summary>
    public class EndingVideoRecordTests
    {
        public const string OUTPUT_DIR = "Logs/Video/ending";
        private const int FPS = 30;
        private const float LEAD_IN_SECONDS = 0.6f;
        private const float RESULTS_SECONDS = 2f;

        [UnityTest, Explicit("영상 녹화용. 이름으로 지정해 실행한다")]
        public IEnumerator RecordEndingFrames()
        {
            if (Directory.Exists(OUTPUT_DIR))
            {
                Directory.Delete(OUTPUT_DIR, true);
            }

            Directory.CreateDirectory(OUTPUT_DIR);
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            GameSession game = Object.FindFirstObjectByType<GameRunner>().Session;
            SoundManager sound = SoundManager.Instance;
            Assert.IsNotNull(sound, "씬에 SoundManager가 있어야 소리를 적을 수 있다");

            // 실제 플레이처럼: 마지막 페이즈, 손 HP 5 → 목표가 SHIFT로 바뀐 뒤 SHIFT를 한 줄로 맞춘다
            game.StartNewRun();
            game.StartPhase(game.Data.Phases.Count - 1);
            game.IsTutorialDone = true;
            game.IsInvincible = true;
            game.Hand.Hp = game.Config.FinisherHp;
            game.Words.Refresh();
            game.FinisherIntroT = 0f;
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            var sounds = new StringBuilder();
            int frame = 0;
            void OnPlayed(SoundKey key, AudioClip clip, float volume, float pitch)
            {
#if UNITY_EDITOR
                string path = clip != null ? UnityEditor.AssetDatabase.GetAssetPath(clip) : string.Empty;
#else
                string path = string.Empty;
#endif
                sounds.AppendLine(string.Join("\t", frame.ToString(CultureInfo.InvariantCulture), key.ToString(), path,
                    volume.ToString("0.###", CultureInfo.InvariantCulture), pitch.ToString("0.###", CultureInfo.InvariantCulture)));
            }

            sound.OnSoundPlayed += OnPlayed;
            Time.captureFramerate = FPS;
            try
            {
                int leadFrames = Mathf.RoundToInt(LEAD_IN_SECONDS * FPS);
                for (; frame < leadFrames; frame++)
                {
                    yield return null;
                    ScreenCaptureUtils.CaptureTo(OUTPUT_DIR, $"frame-{frame:0000}");
                }

                TestDataUtils.PlaceWord(game, 0, game.Data.FinisherWord);
                Assert.AreEqual(GameState.Win, game.State, "SHIFT를 맞추면 승리 상태(엔딩 시작)");
                float end = game.Data.Ending.EndAt + RESULTS_SECONDS;
                for (; frame < 60 * FPS && game.StateT < end; frame++)
                {
                    yield return null;
                    ScreenCaptureUtils.CaptureTo(OUTPUT_DIR, $"frame-{frame:0000}");
                }
            }
            finally
            {
                Time.captureFramerate = 0;
                sound.OnSoundPlayed -= OnPlayed;
            }

            File.WriteAllText(Path.Combine(OUTPUT_DIR, "sounds.tsv"), sounds.ToString());
#if UNITY_EDITOR
            string music = sound.Library != null && sound.Library.Music != null ? UnityEditor.AssetDatabase.GetAssetPath(sound.Library.Music) : string.Empty;
            float musicVolume = sound.Library != null ? sound.Library.MusicVolume : 0f;
            File.WriteAllText(Path.Combine(OUTPUT_DIR, "music.tsv"), $"{music}\t{musicVolume.ToString("0.###", CultureInfo.InvariantCulture)}\t{FPS}\t{frame}\n");
#endif
            Assert.IsTrue(game.IsEndingDone);
            Assert.Greater(frame, FPS * 5);
        }
    }
}
