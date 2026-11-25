using System;
using UnityEngine;

namespace ShiftMagicSpell.Data
{
    /// <summary>타격감 연출을 거는 상황. 상황마다 세기(HitFeelLevel)가 다르다.</summary>
    public enum HitKind
    {
        PlayerHurt,
        PlayerDeath,
        Spell,
        Combo,
        Overflow,
        Finisher,
        BlockChip,
        BlockBreak,
        Slam,
    }

    /// <summary>타격 한 번의 세기. 0이면 그 효과는 쓰지 않는다.</summary>
    [Serializable]
    public class HitFeelLevel
    {
        [Tooltip("멈춤(초). 로직만 멈추고 흔들림·번쩍임·소리는 계속된다")]
        public float HitStop;
        [Tooltip("화면 흔들림 최대 이동(px). 맞은 방향으로 한 번 밀렸다가 줄어든다")]
        public float ShakePx;
        [Tooltip("화면 흔들림 시간(초)")]
        public float ShakeTime;
        [Tooltip("맞은 대상이 하얀 실루엣이 되는 시간(초)")]
        public float FlashTime;
        [Tooltip("맞은 대상이 납작해지는 정도(0~1). 0.25면 가로 1.25배, 세로 0.75배에서 돌아온다")]
        [Range(0f, 1f)] public float Squash;
        [Tooltip("맞은 대상이 밀리는 거리(px)")]
        public float KnockPx;
        [Tooltip("맞은 자리에서 튀는 조각 수")]
        public int Debris;
        [Tooltip("타격 소리 크기 배율")]
        public float SoundVolume = 1f;
        [Tooltip("타격 소리 높이 배율. 1보다 작으면 낮고 묵직하다")]
        public float SoundPitch = 1f;

        public HitFeelLevel()
        {
        }

        public HitFeelLevel(float hitStop, float shakePx, float shakeTime, float flashTime, float squash, float knockPx, int debris, float soundVolume, float soundPitch)
        {
            HitStop = hitStop;
            ShakePx = shakePx;
            ShakeTime = shakeTime;
            FlashTime = flashTime;
            Squash = squash;
            KnockPx = knockPx;
            Debris = debris;
            SoundVolume = soundVolume;
            SoundPitch = soundPitch;
        }
    }

    /// <summary>
    /// 타격감 수치(Unity에서 추가, 웹 JSON에 없음). 상황별 세기와 공용 설정.
    /// 웹 데이터 가져오기(Import Prototype Data)는 Config만 덮어쓰므로 여기 값은 지켜진다.
    /// </summary>
    [Serializable]
    public class HitFeelValues
    {
        [Header("플레이어가 맞음")]
        public HitFeelLevel PlayerHurt = new HitFeelLevel(0.08f, 3f, 0.25f, 0.06f, 0.25f, 2f, 6, 1f, 1f);
        [Tooltip("마지막 HP를 잃은 타격")]
        public HitFeelLevel PlayerDeath = new HitFeelLevel(0.25f, 4f, 0.45f, 0.12f, 0.35f, 3f, 12, 1f, 0.85f);

        [Header("커서(손)가 마법에 맞음")]
        public HitFeelLevel Spell = new HitFeelLevel(0.05f, 2f, 0.25f, 0.06f, 0.2f, 4f, 6, 1f, 1f);
        [Tooltip("같은 판 슬롯을 콤보 시간 안에 연달아 쏜 두 번째 이후")]
        public HitFeelLevel Combo = new HitFeelLevel(0.1f, 3f, 0.3f, 0.1f, 0.3f, 6f, 10, 1f, 1.12f);
        [Tooltip("반대편 슬롯이 꽉 차서 80%로 바로 발동")]
        public HitFeelLevel Overflow = new HitFeelLevel(0f, 1f, 0.15f, 0.05f, 0.1f, 2f, 3, 0.7f, 1.1f);
        [Tooltip("마무리 SHIFT")]
        public HitFeelLevel Finisher = new HitFeelLevel(0.3f, 4f, 0.6f, 0.2f, 0.4f, 8f, 16, 1f, 0.8f);

        [Header("공격 블록이 손에 맞음")]
        [Tooltip("금이 감(내구도가 남음)")]
        public HitFeelLevel BlockChip = new HitFeelLevel(0.04f, 1f, 0.15f, 0.06f, 0f, 0f, 4, 0.8f, 1.1f);
        [Tooltip("깨져서 사라짐")]
        public HitFeelLevel BlockBreak = new HitFeelLevel(0.06f, 2f, 0.2f, 0.08f, 0f, 0f, 8, 1f, 0.9f);

        [Header("손이 판을 내려침(플레이어를 맞히지 않아도)")]
        public HitFeelLevel Slam = new HitFeelLevel(0f, 2f, 0.2f, 0f, 0f, 0f, 0, 1f, 1f);

        [Header("공용")]
        [Tooltip("멈춤이 끝난 뒤 이 시간 안의 다음 멈춤은 건너뛴다(사망·마무리는 예외). 연달아 멈추면 조작이 끊긴 느낌이 난다")]
        public float HitStopGap = 0.15f;
        [Tooltip("흔들림 전체 배율. 0이면 흔들림 없음(설정 화면의 '흔들림 줄이기'용)")]
        [Range(0f, 1f)] public float ShakeScale = 1f;
        [Tooltip("하얀 번쩍임 전체 배율. 0이면 번쩍임 없음")]
        [Range(0f, 1f)] public float FlashScale = 1f;
        [Tooltip("플레이어가 맞으면 화면 테두리가 빨갛게 물드는 시간(초)")]
        public float EdgeTime = 0.25f;

        public HitFeelLevel Get(HitKind kind)
        {
            switch (kind)
            {
                case HitKind.PlayerHurt:
                    return PlayerHurt;
                case HitKind.PlayerDeath:
                    return PlayerDeath;
                case HitKind.Spell:
                    return Spell;
                case HitKind.Combo:
                    return Combo;
                case HitKind.Overflow:
                    return Overflow;
                case HitKind.Finisher:
                    return Finisher;
                case HitKind.BlockChip:
                    return BlockChip;
                case HitKind.BlockBreak:
                    return BlockBreak;
                case HitKind.Slam:
                    return Slam;
                default:
                    return Slam;
            }
        }

        /// <summary>사망·마무리처럼 멈춤 간격을 무시하고 꼭 멈추는 타격</summary>
        public static bool IsPriority(HitKind kind)
        {
            return kind == HitKind.PlayerDeath || kind == HitKind.Finisher;
        }
    }
}
