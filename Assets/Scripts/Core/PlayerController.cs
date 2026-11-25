using System.Collections.Generic;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>
    /// 플레이어: 칸 이동(누르고 있으면 반복), SHIFT 칸 순간이동, 피격.
    /// 입력은 Press/Release로 받는다(키보드 읽기는 Input 쪽).
    /// </summary>
    public class PlayerController
    {
        private readonly GameSession _game;
        private readonly List<MoveDir> _heldDirs = new List<MoveDir>();
        private float _repeatT;

        // 동작 사이 최소 간격(쉬는 시간). 쉬는 동안 누른 입력은 하나만 예약해 두었다가 끝나면 실행한다.
        private float _restT;
        private bool _hasQueuedInput;
        private MoveDir _queuedDir;
        private bool _isQueuedRotate;

        // 타격 멈춤(히트스톱) 중에 누른 Space: 멈춤이 끝나면 발동한다
        private bool _isFireQueued;

        public int Board;
        public int R;
        public int C;
        public int Hp;
        public float Invuln;
        public float ShiftCd;
        public float Dwell;
        public float BoardTime;
        public float SinceMove;
        public Vector2Int LastDir;   // 웹과 같이 (dr, dc): x = 행 변화, y = 열 변화
        public SlideAnim Slide;
        public bool IsShiftHeld;

        public PlayerController(GameSession game)
        {
            _game = game;
            Reset();
        }

        public void Reset()
        {
            Board = 0;
            R = 2;
            C = 2;
            Hp = _game.Config.PlayerHp;
            Invuln = 0f;
            ShiftCd = 0f;
            Dwell = 0f;
            BoardTime = 0f;
            LastDir = Vector2Int.zero;
            SinceMove = 99f;
            _repeatT = 0f;
            _restT = 0f;
            _hasQueuedInput = false;
            _isFireQueued = false;
            Slide = null;
        }

        public BoardCell Here => _game.Boards[Board].GetCellOrNull(R, C);

        public void Press(MoveDir dir)
        {
            if (_heldDirs.Contains(dir))
            {
                return;
            }

            _heldDirs.Add(dir);
            Act(dir);
        }

        public void Release(MoveDir dir)
        {
            _heldDirs.Remove(dir);
        }

        public void ReleaseAll()
        {
            _heldDirs.Clear();
            IsShiftHeld = false;
            _hasQueuedInput = false;
            _isFireQueued = false;
        }

        public void Update(float dt)
        {
            Invuln = Mathf.Max(0f, Invuln - dt);
            ShiftCd = Mathf.Max(0f, ShiftCd - dt);
            Dwell += dt;
            SinceMove += dt;
            BoardTime += dt;
            if (Slide != null)
            {
                Slide.T += dt;
                if (Slide.IsDone)
                {
                    Slide = null;
                }
            }

            if (_isFireQueued)
            {
                _isFireQueued = false;
                Fire();
            }

            _restT = Mathf.Max(0f, _restT - dt);
            if (_hasQueuedInput && _restT <= 0f)
            {
                _hasQueuedInput = false;
                Perform(_queuedDir, _isQueuedRotate);
                return;
            }

            if (_heldDirs.Count > 0)
            {
                _repeatT -= dt;
                if (_repeatT <= 0f && _restT <= 0f)
                {
                    Act(_heldDirs[_heldDirs.Count - 1]);
                }
            }
        }

        /// <summary>Space: 슬롯 칸에 서 있으면 담긴 블록을 발동. 타격 멈춤 중이면 예약했다가 멈춤이 끝나면 발동한다.</summary>
        public bool Fire()
        {
            BoardCell here = Here;
            if (_game.State != GameState.Play || !here.IsSlot)
            {
                return false;
            }

            if (_game.Feel.IsHitStopped)
            {
                _isFireQueued = true;
                return false;
            }

            if (here.Block == null)
            {
                _game.Fx.Add(FxEvent.AtCell(FxKind.Deny, Board, R, C, 0.2f));
                _game.ShowToast("NO BLOCK IN THIS SLOT");
                return false;
            }

            return _game.FireSlot(_game.Boards[Board], here);
        }

        public bool TryMove(MoveDir dir)
        {
            if (_game.State != GameState.Play)
            {
                return false;
            }

            Vector2Int delta = GeometryUtils.ToDelta(dir);
            if (_game.Boards[Board].GetCellOrNull(R + delta.y, C + delta.x) == null)
            {
                return false;
            }

            R += delta.y;
            C += delta.x;
            _game.Stats.Moves++;
            LastDir = new Vector2Int(delta.y, delta.x);
            SinceMove = 0f;
            Dwell = 0f;
            Slide = null;
            EnterCell();
            return true;
        }

        public bool Hurt(string source)
        {
            if (_game.IsInvincible || Invuln > 0f || _game.State != GameState.Play)
            {
                return false;
            }

            Hp--;
            Invuln = _game.Config.HitInvuln;
            _game.OnPlayerHurt(source);
            return true;
        }

        // Shift를 누른 채 방향키 = 서 있는 줄 돌리기(←→ 행, ↑↓ 열). Shift 없이 = 한 칸 이동. 누르고 있으면 반복.
        private void Act(MoveDir dir)
        {
            // 쉬는 중이거나 타격 멈춤(히트스톱) 중이면 버리지 않고 마지막 입력 하나만 예약한다(입력이 씹힌 느낌이 나지 않게)
            if (_restT > 0f || _game.Feel.IsHitStopped)
            {
                _hasQueuedInput = true;
                _queuedDir = dir;
                _isQueuedRotate = IsShiftHeld;
                return;
            }

            Perform(dir, IsShiftHeld);
        }

        private void Perform(MoveDir dir, bool isRotate)
        {
            ConfigValues config = _game.Config;
            if (isRotate)
            {
                _repeatT = config.RotateRepeat;
                if (Rotate(dir))
                {
                    _restT = config.RotateDelay;
                }

                return;
            }

            _repeatT = config.MoveRepeat;
            if (TryMove(dir))
            {
                _restT = config.MoveDelay;
            }
        }

        /// <summary>플레이어 Shift: 서 있는 행(←→) 또는 열(↑↓)을 그 방향으로 한 칸 돌린다. 플레이어는 칸에 그대로 있고 키만 지나간다.</summary>
        public bool Rotate(MoveDir dir)
        {
            if (_game.State != GameState.Play)
            {
                return false;
            }

            BoardModel board = _game.Boards[Board];
            BoardCell here = Here;
            Vector2Int delta = GeometryUtils.ToDelta(dir);
            LineAxis axis = delta.x != 0 ? LineAxis.Row : LineAxis.Col;
            int index = axis == LineAxis.Row ? R : C;
            int sign = delta.x != 0 ? delta.x : delta.y;
            if (here.IsShift || here.IsSlot || !board.CanRotate(axis, index))
            {
                // 왜 안 되는지 알려 준다(SHIFT 칸·슬롯은 줄에서 빠지는 고정 칸)
                _game.Fx.Add(FxEvent.AtCell(FxKind.Deny, Board, R, C, 0.2f));
                _game.ShowToast(here.IsShift ? "CAN'T SHIFT ON A SHIFT KEY - STEP OFF" : here.IsSlot ? "CAN'T SHIFT ON A SLOT - STEP ONTO THE KEYS" : "THIS LINE CAN'T ROTATE");
                return false;
            }

            board.RotateLine(axis, index, 1, sign, _game.Config.DominoStep, _game.Config.RotateSlide);
            _game.Fx.Add(new FxEvent { Kind = FxKind.Rotate, Board = Board, Axis = axis, Index = index, Sign = sign, T = 0.45f, Duration = 0.45f });
            _game.Stats.Rotations++;
            Dwell = 0f;
            _game.OnBoardChanged();
            return true;
        }

        // SHIFT 칸을 밟으면 반대편 판의 SHIFT 칸으로 순간이동(쿨다운 중이면 그냥 서 있는다)
        private void EnterCell()
        {
            BoardCell here = Here;
            if (!here.IsShift || ShiftCd > 0f)
            {
                return;
            }

            BoardModel other = _game.Boards[1 - Board];
            BoardCell dest = other.ShiftCell;
            _game.Fx.Add(FxEvent.AtCell(FxKind.Warp, Board, R, C, 0.4f));
            Board = other.Index;
            R = dest.R;
            C = dest.C;
            ShiftCd = _game.Config.ShiftCooldown;
            Invuln = Mathf.Max(Invuln, _game.Config.ShiftInvuln);
            BoardTime = 0f;
            Dwell = 0f;
            _game.Stats.Teleports++;
            _game.Fx.Add(FxEvent.AtCell(FxKind.Warp, Board, R, C, 0.4f));
        }
    }
}
