using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>
    /// 타격 한 번에 걸리는 짧은 연출 값(번쩍임, 납작해짐, 밀림, 화면 흔들림, 빨간 테두리).
    /// T가 Duration에서 0으로 줄어든다. 세기(Amount)와 방향(Dir)은 걸 때 정한다. 방향은 로직 좌표(y 아래가 +).
    /// </summary>
    public class HitPulse
    {
        public float T;
        public float Duration;
        public float Amount;
        public Vector2 Dir;

        public bool IsActive => T > 0f;

        /// <summary>1(막 걸림) → 0(끝)</summary>
        public float Remaining => Duration > 0f ? Mathf.Clamp01(T / Duration) : 0f;

        /// <summary>지금 세기(줄어드는 곡선 적용): Amount × 남은 비율²</summary>
        public float Strength => Amount * Remaining * Remaining;

        public void Start(float duration, float amount, Vector2 dir)
        {
            if (duration <= 0f || amount <= 0f)
            {
                return;
            }

            T = duration;
            Duration = duration;
            Amount = amount;
            Dir = dir;
        }

        /// <summary>지금 걸린 것보다 셀 때만 새로 건다(약한 타격이 센 타격을 덮지 않게)</summary>
        public void StartIfStronger(float duration, float amount, Vector2 dir)
        {
            if (amount >= Strength)
            {
                Start(duration, amount, dir);
            }
        }

        public void Update(float dt)
        {
            T = Mathf.Max(0f, T - dt);
        }

        public void Clear()
        {
            T = 0f;
            Duration = 0f;
            Amount = 0f;
            Dir = Vector2.zero;
        }
    }
}
