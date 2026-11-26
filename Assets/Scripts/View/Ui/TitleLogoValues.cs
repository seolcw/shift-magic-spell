using System;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    /// <summary>
    /// 첫 화면 로고 연출 시각표(초): 뒤섞인 두 줄(SHIFT / MAGIC SPELL)이 게임의 줄 돌리기처럼 한 칸씩 밀려 맞춰지고,
    /// 맞춰지면 초록 빛 → 불덩이가 커서에 맞음 → CLICK TO START. 끝나면 처음부터 다시(글자 안내는 첫 명중 뒤로 계속 보임).
    /// 오프셋 = 단어를 왼쪽으로 돌린 칸 수(0이면 제자리). 위 줄은 왼쪽으로, 아래 줄은 오른쪽으로 민다.
    /// </summary>
    [Serializable]
    public class TitleLogoValues
    {
        public string Top = "SHIFT";
        public string Bottom = "MAGIC SPELL";

        [Tooltip("위 줄이 처음에 왼쪽으로 돌아가 있는 칸 수(SHIFT → FTSHI)")]
        public int TopStart = 3;
        [Tooltip("아래 줄이 처음에 왼쪽으로 돌아가 있는 칸 수(MAGIC SPELL → PELLMAGIC S)")]
        public int BottomStart = 7;

        [Tooltip("시작 후 첫 밀기까지")]
        public float LeadIn = 0.6f;
        [Tooltip("한 번 미는 간격")]
        public float Step = 0.42f;
        [Tooltip("한 칸 미끄러지는 시간(Step보다 짧게)")]
        public float Slide = 0.18f;
        [Tooltip("위 줄이 맞은 뒤 아래 줄을 밀기 시작할 때까지")]
        public float Gap = 0.35f;
        [Tooltip("아래 줄이 맞은 뒤 불덩이를 쏠 때까지")]
        public float ShotDelay = 0.35f;
        public float FlyTime = 0.45f;
        [Tooltip("명중 뒤 맞춰진 제목을 보여 주는 시간(이후 처음부터)")]
        public float Hold = 2.6f;
        [Tooltip("명중 뒤 CLICK TO START가 처음 뜰 때까지")]
        public float PromptDelay = 0.3f;
        [Tooltip("CLICK TO START 깜빡임(초당 켜짐 횟수)")]
        public float PromptBlinkRate = 1.25f;
        [Tooltip("줄이 맞은 순간 별이 반짝이는 시간")]
        public float SparkleTime = 0.6f;

        /// <summary>위 줄을 맞추려면 왼쪽으로 몇 번 밀어야 하나</summary>
        public int TopSteps => Mod(Top.Length - TopStart, Top.Length);

        /// <summary>아래 줄을 맞추려면 오른쪽으로 몇 번 밀어야 하나</summary>
        public int BottomSteps => Mod(BottomStart, Bottom.Length);

        public float TopFrom => LeadIn;
        public float TopSolvedAt => TopFrom + TopSteps * Step;
        public float BottomFrom => TopSolvedAt + Gap;
        public float BottomSolvedAt => BottomFrom + BottomSteps * Step;
        public float ShotAt => BottomSolvedAt + ShotDelay;
        public float HitAt => ShotAt + FlyTime;
        public float Duration => HitAt + Hold;

        /// <summary>한 바퀴 안의 시각(t)에서 위 줄 오프셋(소수 = 미끄러지는 중)</summary>
        public float GetTopOffset(float t)
        {
            return TopStart + GetPushed(t, TopFrom, TopSteps);
        }

        /// <summary>한 바퀴 안의 시각(t)에서 아래 줄 오프셋. 오른쪽으로 밀어서 오프셋이 줄어든다.</summary>
        public float GetBottomOffset(float t)
        {
            return BottomStart - GetPushed(t, BottomFrom, BottomSteps);
        }

        public bool IsTopShifting(float t)
        {
            return t >= TopFrom - Step * 0.5f && t < TopSolvedAt;
        }

        public bool IsBottomShifting(float t)
        {
            return t >= BottomFrom - Step * 0.5f && t < BottomSolvedAt;
        }

        /// <summary>처음 켜진 뒤 흐른 시간(total)으로: 첫 명중 이후에는 계속 깜빡인다</summary>
        public bool IsPromptVisible(float total)
        {
            float since = total - (HitAt + PromptDelay);
            return since >= 0f && Mathf.Repeat(since * PromptBlinkRate, 1f) < 0.6f;
        }

        // from부터 Step마다 한 칸씩, 각 칸은 Slide 동안 부드럽게 미끄러진다
        private float GetPushed(float t, float from, int steps)
        {
            if (t < from || steps <= 0)
            {
                return 0f;
            }

            float k = (t - from) / Step;
            int done = Mathf.FloorToInt(k);
            if (done >= steps)
            {
                return steps;
            }

            float u = Mathf.Clamp01((k - done) * Step / Mathf.Max(0.01f, Slide));
            return done + u * u * (3f - 2f * u);
        }

        private static int Mod(int value, int length)
        {
            return ((value % length) + length) % length;
        }
    }
}
