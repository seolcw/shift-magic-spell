using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.Tests
{
    /// <summary>테스트용: 에셋(수치·배치)을 읽어 씬 없이 GameSession을 만든다. 수치는 복사본이라 테스트가 에셋을 바꾸지 않는다.</summary>
    public static class TestDataUtils
    {
        public static AttackPatternsData LoadAttackPatterns()
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<AttackPatternsData>("Assets/Data/AttackPatterns.asset");
            return Object.Instantiate(asset);
#else
            return null;
#endif
        }

        public static BoardLayoutData LoadBoardLayouts()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<BoardLayoutData>("Assets/Data/BoardLayouts.asset");
#else
            return null;
#endif
        }

        public static GameSession CreateSession(int seed = 1)
        {
            return new GameSession(LoadAttackPatterns(), LoadBoardLayouts(), seed);
        }

        /// <summary>
        /// 타격 멈춤(히트스톱)을 끈다. 전환·재시작처럼 정해진 시간을 재는 테스트용(멈춤 동안은 로직 시간이 흐르지 않아서).
        /// 수치는 복사본이라 에셋은 바뀌지 않는다.
        /// </summary>
        public static void DisableHitStop(GameSession game)
        {
            foreach (HitKind kind in System.Enum.GetValues(typeof(HitKind)))
            {
                game.Data.HitFeel.Get(kind).HitStop = 0f;
            }
        }

        /// <summary>판 b의 1행 앞쪽에 단어 글자를 키 바꿔치기로 놓고 단어 판정을 돌린다(웹 verify-rules의 place)</summary>
        public static void PlaceWord(GameSession game, int boardIndex, string word)
        {
            BoardModel board = game.Boards[boardIndex];
            var row = board.LineCells(LineAxis.Row, 1);
            row.RemoveAll(cell => cell.IsShift);
            for (int i = 0; i < word.Length; i++)
            {
                BoardCell from = null;
                foreach (BoardCell cell in board.KeyCells())
                {
                    if (cell.Key.Letter == word[i])
                    {
                        from = cell;
                    }
                }

                (from.Key, row[i].Key) = (row[i].Key, from.Key);
            }

            board.MarkChanged();
            game.OnBoardChanged();
        }

        public static string RowString(BoardModel board, int row)
        {
            var builder = new System.Text.StringBuilder();
            foreach (BoardCell cell in board.LineCells(LineAxis.Row, row))
            {
                if (!cell.IsShift)
                {
                    builder.Append(cell.Key.HasLetter ? cell.Key.Letter : '_');
                }
            }

            return builder.ToString();
        }

        public static string ColString(BoardModel board, int col)
        {
            var builder = new System.Text.StringBuilder();
            foreach (BoardCell cell in board.LineCells(LineAxis.Col, col))
            {
                if (!cell.IsShift)
                {
                    builder.Append(cell.Key.HasLetter ? cell.Key.Letter : '_');
                }
            }

            return builder.ToString();
        }
    }
}
