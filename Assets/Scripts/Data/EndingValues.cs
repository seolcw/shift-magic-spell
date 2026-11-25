using System;
using UnityEngine;

namespace ShiftMagicSpell.Data
{
    /// <summary>엔딩 연출 단계. 마무리 SHIFT를 쏜 순간(승리 상태 시작)부터 시간이 흐르며 차례로 넘어간다.</summary>
    public enum EndingStage
    {
        None,       // 승리 상태가 아님
        Impact,     // SHIFT가 날아가 맞고 손이 흔들림(타격감 그대로)
        Crack1,     // 손에 금이 감
        Crack2,     // 금이 더 크게 감
        Hourglass,  // 펑! 모래시계(로딩 커서)
        Beam,       // 펑! 텍스트 커서 I
        Arrow,      // 펑! 작은 화살표 커서가 두리번거림
        Zip,        // 화살표가 화면 밖으로 휙
        Caption,    // 자막이 한 글자씩
        Done,       // 결과 화면
    }

    /// <summary>
    /// 엔딩 연출 수치(Unity에서 추가): "거대한 손은 고장 난 커서였다". 시각은 마무리 SHIFT를 쏜 순간부터 잰 초.
    /// 멈춤(히트스톱) 동안은 흐르지 않는다. Space·Enter·클릭으로 건너뛰면 바로 Done.
    /// </summary>
    [Serializable]
    public class EndingValues
    {
        [Tooltip("손에 첫 금이 가는 시각")]
        public float CrackAt = 1.4f;
        [Tooltip("금이 더 크게 가는 시각")]
        public float Crack2At = 1.8f;
        [Tooltip("펑! 모래시계로 바뀌는 시각")]
        public float HourglassAt = 2.2f;
        [Tooltip("펑! 텍스트 커서 I로 바뀌는 시각")]
        public float BeamAt = 3f;
        [Tooltip("펑! 작은 화살표로 바뀌는 시각")]
        public float ArrowAt = 3.7f;
        [Tooltip("화살표가 화면 밖으로 달아나기 시작하는 시각")]
        public float ZipAt = 5f;
        [Tooltip("달아나는 데 걸리는 시간")]
        public float ZipTime = 0.4f;
        [Tooltip("자막이 찍히기 시작하는 시각")]
        public float CaptionAt = 5.4f;
        [Tooltip("자막 한 글자가 찍히는 간격(초)")]
        public float CaptionCharTime = 0.05f;
        [Tooltip("엔딩이 끝나고 결과 화면으로 넘어가는 시각")]
        public float EndAt = 7.4f;
        [Tooltip("자막(5x7 도트 글꼴: 영어 대문자·숫자·기호)")]
        public string Caption = "IT WAS JUST A STUCK CURSOR.";

        /// <summary>승리 상태 시간 t에서의 단계</summary>
        public EndingStage GetStage(float t)
        {
            if (t >= EndAt)
            {
                return EndingStage.Done;
            }

            if (t >= CaptionAt)
            {
                return EndingStage.Caption;
            }

            if (t >= ZipAt)
            {
                return EndingStage.Zip;
            }

            if (t >= ArrowAt)
            {
                return EndingStage.Arrow;
            }

            if (t >= BeamAt)
            {
                return EndingStage.Beam;
            }

            if (t >= HourglassAt)
            {
                return EndingStage.Hourglass;
            }

            if (t >= Crack2At)
            {
                return EndingStage.Crack2;
            }

            return t >= CrackAt ? EndingStage.Crack1 : EndingStage.Impact;
        }

        /// <summary>단계가 시작되는 시각</summary>
        public float GetStageStart(EndingStage stage)
        {
            switch (stage)
            {
                case EndingStage.Crack1:
                    return CrackAt;
                case EndingStage.Crack2:
                    return Crack2At;
                case EndingStage.Hourglass:
                    return HourglassAt;
                case EndingStage.Beam:
                    return BeamAt;
                case EndingStage.Arrow:
                    return ArrowAt;
                case EndingStage.Zip:
                    return ZipAt;
                case EndingStage.Caption:
                    return CaptionAt;
                case EndingStage.Done:
                    return EndAt;
                default:
                    return 0f;
            }
        }

        /// <summary>시간 t까지 찍힌 자막 글자 수</summary>
        public int GetCaptionLength(float t)
        {
            if (t < CaptionAt || string.IsNullOrEmpty(Caption))
            {
                return 0;
            }

            int count = Mathf.FloorToInt((t - CaptionAt) / Mathf.Max(0.001f, CaptionCharTime)) + 1;
            return Mathf.Clamp(count, 0, Caption.Length);
        }
    }
}
