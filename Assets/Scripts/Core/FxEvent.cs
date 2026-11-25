using System.Collections.Generic;
using UnityEngine;

namespace ShiftMagicSpell.Core
{
    public enum FxKind
    {
        Deny,
        Warp,
        Rotate,
        Match,
        Float,
        Click,
        Store,
        Fizzle,
        Impact,
        Spell,
        Debris,     // 타격감 조각: From = 위치, To = 주로 튀는 방향, Style = player/hand/block
    }

    /// <summary>
    /// 로직이 남기는 연출 기록(웹의 game.fx 항목). View가 읽어서 그리고, T가 0이 되면 사라진다.
    /// 종류마다 쓰는 항목이 다르다. 위치는 로직 좌표(칸 단위).
    /// </summary>
    public class FxEvent
    {
        public FxKind Kind;
        public float T;
        public float Duration;
        public float Delay;
        public float Age;
        public int Board;
        public int R;
        public int C;
        public LineAxis Axis;
        public int Index;
        public int Sign;
        public List<Vector2Int> Cells;  // x = c, y = r
        public Vector2 From;
        public Vector2 To;
        public string Text;
        public string Word;
        public string Style;
        public Color Color = Color.white;
        public int Count = 1;
        public float Size = 10f;
        public float Seed;

        public static FxEvent AtCell(FxKind kind, int board, int r, int c, float time)
        {
            return new FxEvent { Kind = kind, Board = board, R = r, C = c, T = time, Duration = time };
        }

        public static List<Vector2Int> ToCellList(IEnumerable<BoardCell> cells)
        {
            var result = new List<Vector2Int>();
            foreach (BoardCell cell in cells)
            {
                result.Add(new Vector2Int(cell.C, cell.R));
            }

            return result;
        }
    }
}
