using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>
    /// 타격감 상태: 멈춤(히트스톱), 방향 있는 화면 흔들림, 맞은 대상의 번쩍임·납작해짐·밀림, 빨간 테두리.
    /// 세기는 AttackPatternsData.HitFeel(상황별). 값은 여기서 정하고 그림은 View가 읽어서 그린다.
    /// 멈춤 중에도 이 값들은 실제 시간으로 흐른다(GameSession.Update가 로직보다 먼저 Update를 부른다).
    /// </summary>
    public class HitFeedback
    {
        private readonly GameSession _game;
        private float _gapT;

        public readonly HitPulse Shake = new HitPulse();
        public readonly HitPulse PlayerFlash = new HitPulse();
        public readonly HitPulse PlayerSquash = new HitPulse();
        public readonly HitPulse PlayerKnock = new HitPulse();
        public readonly HitPulse HandFlash = new HitPulse();
        public readonly HitPulse HandSquash = new HitPulse();
        public readonly HitPulse HandKnock = new HitPulse();
        public readonly HitPulse Edge = new HitPulse();

        public HitFeedback(GameSession game)
        {
            _game = game;
        }

        /// <summary>남은 멈춤 시간(초)</summary>
        public float HitStopT { get; private set; }

        public bool IsHitStopped => HitStopT > 0f;

        /// <summary>마지막 타격 종류와 번호(소리처럼 바깥에서 "새 타격이 났나"를 볼 때)</summary>
        public HitKind LastKind { get; private set; }
        public int Serial { get; private set; }

        private HitFeelValues Values => _game.Data.HitFeel;

        public void Reset()
        {
            HitStopT = 0f;
            _gapT = 0f;
            Shake.Clear();
            PlayerFlash.Clear();
            PlayerSquash.Clear();
            PlayerKnock.Clear();
            HandFlash.Clear();
            HandSquash.Clear();
            HandKnock.Clear();
            Edge.Clear();
        }

        /// <summary>실제 시간으로 흐른다(멈춤 중에도)</summary>
        public void Update(float dt)
        {
            Shake.Update(dt);
            PlayerFlash.Update(dt);
            PlayerSquash.Update(dt);
            PlayerKnock.Update(dt);
            HandFlash.Update(dt);
            HandSquash.Update(dt);
            HandKnock.Update(dt);
            Edge.Update(dt);
            if (HitStopT > 0f)
            {
                HitStopT = Mathf.Max(0f, HitStopT - dt);
                if (HitStopT <= 0f)
                {
                    _gapT = Values.HitStopGap;
                }

                return;
            }

            _gapT = Mathf.Max(0f, _gapT - dt);
        }

        /// <summary>
        /// 타격 연출을 건다. dir = 맞은 쪽이 밀리는 방향(로직 좌표, 길이 1). 대상(플레이어·손)은 종류로 정해진다.
        /// 블록의 번쩍임은 블록에 붙어 있어서 부르는 쪽(HandController)이 건다.
        /// </summary>
        public void Hit(HitKind kind, Vector2 dir)
        {
            HitFeelValues values = Values;
            HitFeelLevel level = values.Get(kind);
            RequestHitStop(level.HitStop, HitFeelValues.IsPriority(kind));
            Shake.StartIfStronger(level.ShakeTime, level.ShakePx * values.ShakeScale, dir);
            // 번쩍임은 멈춤이 끝날 때까지는 이어지게(멈춘 화면에서 하얀 대상이 보이도록)
            float flashTime = level.FlashTime > 0f ? Mathf.Max(level.FlashTime, level.HitStop) * values.FlashScale : 0f;
            switch (kind)
            {
                case HitKind.PlayerHurt:
                case HitKind.PlayerDeath:
                    PlayerFlash.Start(flashTime, 1f, dir);
                    PlayerSquash.Start(level.ShakeTime, level.Squash, dir);
                    PlayerKnock.Start(level.ShakeTime, level.KnockPx, dir);
                    Edge.Start(values.EdgeTime * (kind == HitKind.PlayerDeath ? 2f : 1f), 1f, dir);
                    break;
                case HitKind.Spell:
                case HitKind.Combo:
                case HitKind.Overflow:
                case HitKind.Finisher:
                    HandFlash.Start(flashTime, 1f, dir);
                    HandSquash.Start(level.ShakeTime, level.Squash, dir);
                    HandKnock.Start(level.ShakeTime, level.KnockPx, dir);
                    break;
                default:
                    break;
            }

            LastKind = kind;
            Serial++;
        }

        /// <summary>
        /// 멈춤 요청. 이미 멈춰 있으면 남은 시간과 새 시간 중 긴 쪽(겹쳐도 더해지지 않음).
        /// 멈춤이 끝난 직후(HitStopGap) 다시 멈추려 하면 건너뛴다. 사망·마무리(isPriority)는 예외.
        /// </summary>
        public void RequestHitStop(float seconds, bool isPriority)
        {
            if (seconds <= 0f)
            {
                return;
            }

            if (HitStopT <= 0f && _gapT > 0f && !isPriority)
            {
                return;
            }

            HitStopT = Mathf.Max(HitStopT, seconds);
        }
    }
}
