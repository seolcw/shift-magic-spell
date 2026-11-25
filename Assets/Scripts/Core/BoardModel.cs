using System.Collections.Generic;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>키보드 한 판: 마스크 + 칸마다 키 하나 + 공격 슬롯 2칸.</summary>
    public class BoardModel
    {
        public readonly int Index;
        public int Version { get; private set; }
        public int ResetCount { get; private set; }
        public BoardCell ShiftCell { get; private set; }
        public List<BoardCell> Slots { get; } = new List<BoardCell>();
        public List<BoardLine> Lines { get; } = new List<BoardLine>();

        private readonly string[] _mask;
        private readonly string[] _layout;
        private readonly Vector2Int[] _slotDefs;
        private readonly BoardCell[,] _cells = new BoardCell[GeometryUtils.ROWS, GeometryUtils.COLS];
        private readonly List<BoardCell> _allCells = new List<BoardCell>();

        public BoardModel(int index, BoardLayoutData layoutData)
        {
            Index = index;
            _mask = layoutData.GetMask(index);
            _layout = layoutData.GetLayout(index);
            _slotDefs = layoutData.GetSlots(index);
            Reset();
        }

        /// <summary>시작 배치로 되돌린다. 슬롯의 블록도 비운다.</summary>
        public void Reset()
        {
            _allCells.Clear();
            Slots.Clear();
            Lines.Clear();
            Version = 0;
            ResetCount++;
            int keyId = 0;
            for (int r = 0; r < GeometryUtils.ROWS; r++)
            {
                for (int c = 0; c < GeometryUtils.COLS; c++)
                {
                    char maskChar = _mask[r][c];
                    if (maskChar == '.')
                    {
                        _cells[r, c] = null;
                        continue;
                    }

                    bool isShift = maskChar == 'S';
                    var cell = new BoardCell(r, c, isShift, false);
                    if (isShift)
                    {
                        ShiftCell = cell;
                    }
                    else
                    {
                        char layoutChar = _layout[r][c];
                        cell.Key = new KeyTile(keyId++, layoutChar >= 'A' && layoutChar <= 'Z' ? layoutChar : '\0');
                    }

                    _cells[r, c] = cell;
                    _allCells.Add(cell);
                }
            }

            foreach (Vector2Int slotDef in _slotDefs)
            {
                var slot = new BoardCell(slotDef.y, slotDef.x, false, true);
                Slots.Add(slot);
                _allCells.Add(slot);
            }

            BuildLines();
        }

        public IReadOnlyList<BoardCell> AllCells => _allCells;

        public BoardCell GetCellOrNull(int r, int c)
        {
            if (r >= 0 && r < GeometryUtils.ROWS && c >= 0 && c < GeometryUtils.COLS)
            {
                return _cells[r, c];
            }

            foreach (BoardCell slot in Slots)
            {
                if (slot.Is(r, c))
                {
                    return slot;
                }
            }

            return null;
        }

        public char LetterAt(int r, int c)
        {
            BoardCell cell = GetCellOrNull(r, c);
            return cell?.Key != null ? cell.Key.Letter : '\0';
        }

        public IEnumerable<BoardCell> KeyCells()
        {
            foreach (BoardCell cell in _allCells)
            {
                if (cell.Key != null)
                {
                    yield return cell;
                }
            }
        }

        /// <summary>그 행·열의 칸(SHIFT 칸 포함, 슬롯 제외)</summary>
        public List<BoardCell> LineCells(LineAxis axis, int index)
        {
            var result = new List<BoardCell>();
            int length = axis == LineAxis.Row ? GeometryUtils.COLS : GeometryUtils.ROWS;

            // 슬롯 칸(판 바깥 열 -1, 7)의 열처럼 판 밖 줄은 빈 줄
            int indexLimit = axis == LineAxis.Row ? GeometryUtils.ROWS : GeometryUtils.COLS;
            if (index < 0 || index >= indexLimit)
            {
                return result;
            }

            for (int i = 0; i < length; i++)
            {
                BoardCell cell = axis == LineAxis.Row ? _cells[index, i] : _cells[i, index];
                if (cell != null)
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        public List<BoardCell> Area(int r0, int c0, int height, int width)
        {
            var result = new List<BoardCell>();
            for (int r = r0; r < r0 + height; r++)
            {
                for (int c = c0; c < c0 + width; c++)
                {
                    BoardCell cell = GetCellOrNull(r, c);
                    if (cell != null)
                    {
                        result.Add(cell);
                    }
                }
            }

            return result;
        }

        /// <summary>(r, c)에서 걸어서 가는 걸음 수. 칸 번호(GeometryUtils.CellKey) → 걸음 수.</summary>
        public Dictionary<int, int> DistFrom(int r, int c)
        {
            var dist = new Dictionary<int, int> { [GeometryUtils.CellKey(r, c)] = 0 };
            var queue = new Queue<BoardCell>();
            queue.Enqueue(GetCellOrNull(r, c) ?? new BoardCell(r, c, false, false));
            while (queue.Count > 0)
            {
                BoardCell current = queue.Dequeue();
                int currentDist = dist[GeometryUtils.CellKey(current.R, current.C)];
                foreach (MoveDir dir in AllDirs)
                {
                    Vector2Int delta = GeometryUtils.ToDelta(dir);
                    BoardCell next = GetCellOrNull(current.R + delta.y, current.C + delta.x);
                    if (next != null && !dist.ContainsKey(GeometryUtils.CellKey(next.R, next.C)))
                    {
                        dist[GeometryUtils.CellKey(next.R, next.C)] = currentDist + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            return dist;
        }

        public static readonly MoveDir[] AllDirs = { MoveDir.Up, MoveDir.Down, MoveDir.Left, MoveDir.Right };

        /// <summary>키를 줄 돌리기가 아닌 방법으로 직접 옮겼을 때(테스트 등) 부른다. 단어 판정 캐시를 무효로 한다.</summary>
        public void MarkChanged()
        {
            Version++;
        }

        public BoardCell EmptySlotOrNull()
        {
            return Slots.Find(slot => slot.Block == null);
        }

        public int BlockCount()
        {
            return Slots.FindAll(slot => slot.Block != null).Count;
        }

        /// <summary>이 판에서 단어가 가장 많이 맞춰진 자리</summary>
        public AlignResult Align(string word)
        {
            AlignResult result = LayoutUtils.BestAlign(Lines, LetterAt, word);
            result.Board = Index;
            return result;
        }

        public bool CanRotate(LineAxis axis, int index)
        {
            return MovableCells(axis, index).Count >= 2;
        }

        /// <summary>
        /// 줄 돌리기(Shift): SHIFT 칸은 고정, 나머지 키가 sign(+1 = 오른쪽/아래) 방향으로 count칸 순환한다.
        /// 미는 쪽 끝부터 step초 간격으로 도미노처럼 미끄러진다.
        /// </summary>
        public bool RotateLine(LineAxis axis, int index, int count, int sign, float step, float duration)
        {
            List<BoardCell> cells = MovableCells(axis, index);
            int length = cells.Count;
            if (length < 2)
            {
                return false;
            }

            var oldKeys = new KeyTile[length];
            for (int i = 0; i < length; i++)
            {
                oldKeys[i] = cells[i].Key;
            }

            Vector2 unit = axis == LineAxis.Row ? new Vector2(sign, 0f) : new Vector2(0f, sign);
            for (int i = 0; i < length; i++)
            {
                int raw = i + sign * count;
                int j = ((raw % length) + length) % length;
                bool isWrap = raw < 0 || raw >= length;
                float delay = (sign > 0 ? i : length - 1 - i) * step;
                KeyTile key = oldKeys[i];
                cells[j].Key = key;
                key.Slide = SlideAnim.Create(cells[i], cells[j], unit, isWrap, delay, duration, SlideAnim.GetOffset(key.Slide));
            }

            Version++;
            return true;
        }

        /// <summary>그 줄 위에 서 있던 것(플레이어)이 같이 밀려 도착하는 칸과 연출. 줄 밖이면 null.</summary>
        public BoardCell RideLineOrNull(LineAxis axis, int index, int r, int c, int count, int sign, float step, float duration, out SlideAnim slide)
        {
            slide = null;
            List<BoardCell> cells = MovableCells(axis, index);
            int length = cells.Count;
            int i = cells.FindIndex(cell => cell.Is(r, c));
            if (i < 0 || length < 2)
            {
                return null;
            }

            int raw = i + sign * count;
            BoardCell to = cells[((raw % length) + length) % length];
            Vector2 unit = axis == LineAxis.Row ? new Vector2(sign, 0f) : new Vector2(0f, sign);
            float delay = (sign > 0 ? i : length - 1 - i) * step;
            slide = SlideAnim.Create(cells[i], to, unit, raw < 0 || raw >= length, delay, duration, Vector3.zero);
            return to;
        }

        private List<BoardCell> MovableCells(LineAxis axis, int index)
        {
            List<BoardCell> cells = LineCells(axis, index);
            cells.RemoveAll(cell => cell.IsShift);
            return cells;
        }

        public void Update(float dt)
        {
            foreach (BoardCell cell in _allCells)
            {
                KeyTile key = cell.Key;
                if (key?.Slide != null)
                {
                    key.Slide.T += dt;
                    if (key.Slide.IsDone)
                    {
                        key.Slide = null;
                    }
                }

                if (cell.Block != null)
                {
                    cell.Block.Age += dt;
                    cell.Block.HitT = Mathf.Max(0f, cell.Block.HitT - dt);
                    cell.Block.FlashT = Mathf.Max(0f, cell.Block.FlashT - dt);
                }
            }
        }

        private void BuildLines()
        {
            for (int r = 0; r < GeometryUtils.ROWS; r++)
            {
                AddLine(LineAxis.Row, r);
            }

            for (int c = 0; c < GeometryUtils.COLS; c++)
            {
                AddLine(LineAxis.Col, c);
            }
        }

        private void AddLine(LineAxis axis, int index)
        {
            var line = new BoardLine { Axis = axis, Index = index };
            foreach (BoardCell cell in LineCells(axis, index))
            {
                if (!cell.IsShift)
                {
                    line.Cells.Add(cell);
                }
            }

            if (line.Cells.Count >= 2)
            {
                Lines.Add(line);
            }
        }
    }
}
