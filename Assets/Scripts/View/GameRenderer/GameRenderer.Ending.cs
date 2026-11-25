using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.View
{
    public partial class GameRenderer
    {
        [Header("엔딩 연출 (단계 시각은 AttackPatterns.Ending, 커서 그림은 HandView)")]
        [Tooltip("커서 모양이 바뀌는 순간 펑 연기를 보여 주는 시간(초)")]
        [SerializeField] private float _endingPoofTime = 0.26f;
        [Tooltip("달아나는 화살표 뒤 잔상 개수와 간격(px)")]
        [SerializeField] private int _zipTrailCount = 3;
        [SerializeField] private float _zipTrailGapPx = 12f;

        /// <summary>엔딩: 커서 모양이 바뀔 때마다 펑 연기, 화살표가 달아날 때 잔상</summary>
        private void DrawEndingFx()
        {
            EndingStage stage = _game.EndingStage;
            if (stage < EndingStage.Hourglass || stage >= EndingStage.Done)
            {
                return;
            }

            EndingValues ending = _game.Data.Ending;
            float t = _game.StateT;
            Vector2 center = ToPixel(_game.Hand.Pos);
            foreach (float at in new[] { ending.HourglassAt, ending.BeamAt, ending.ArrowAt })
            {
                float local = t - at;
                if (local >= 0f && local < _endingPoofTime)
                {
                    _canvas.DrawFrameCentered(Animated("Poof", "Poof", local, false), center.x, center.y, Color.white);
                }
            }

            if (stage != EndingStage.Zip)
            {
                return;
            }

            float u = Mathf.Clamp01((t - ending.ZipAt) / Mathf.Max(0.01f, ending.ZipTime));
            float travelled = _hand.ZipDistancePx * u * u;
            Vector2 dir = _hand.GetZipDir();
            ArtFrame arrow = Frame("CursorShift", "Arrow");
            for (int i = 1; i <= _zipTrailCount; i++)
            {
                float back = travelled - i * _zipTrailGapPx;
                if (back <= 0f)
                {
                    continue;
                }

                Vector2 position = center + dir * back;
                _canvas.DrawFrameCentered(arrow, Mathf.Round(position.x), Mathf.Round(position.y), Color.white.WithAlpha(0.5f / i));
            }
        }
    }
}
