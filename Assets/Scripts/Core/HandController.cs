using System.Collections.Generic;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>
    /// 손(거대한 마우스 커서). 쉬기 → 이동(이동이 곧 예고) → 예고(빨간 테두리·조준선·그림자) → 공격 → 쉬기.
    /// 위치는 로직 좌표(칸 단위). Anim은 Hand.aseprite 태그 이름(Idle, FlickCharge, FlickRelease, Click, SlamRaise, Hurt) 또는 Stun.
    /// </summary>
    public class HandController
    {
        public const string ANIM_IDLE = "Idle";
        public const string ANIM_STUN = "Stun";
        public const string ANIM_HURT = "Hurt";
        public const string ANIM_FLICK_CHARGE = "FlickCharge";
        public const string ANIM_FLICK_RELEASE = "FlickRelease";
        private const float FLICK_RELEASE_TIME = 0.16f;
        private const float FULL_SLAM_TIME = 0.36f;
        private const float CHARGE_HIT_RADIUS = 0.55f;

        private readonly GameSession _game;
        private readonly List<Vector2> _path = new List<Vector2>();
        private float _pendingStun;
        private float _execT;

        public int Hp;
        public Vector2 Pos;
        public HandState State;
        public float T;
        public string Anim = ANIM_IDLE;
        public float AnimT;
        public Vector2 AnimDir;
        public bool IsCharging;
        public AttackPlan Attack;
        public readonly List<Bullet> Bullets = new List<Bullet>();

        public HandController(GameSession game)
        {
            _game = game;
        }

        public int MaxHp => _game.CurrentPhase.HandHp;
        public float Speed => _game.Config.HandSpeed / GeometryUtils.WEB_CELL_PX * _game.CurrentPhase.SpeedMul;
        public bool HasPath => _path.Count > 0;

        public void Reset()
        {
            Hp = MaxHp;
            Pos = new Vector2(GeometryUtils.BoardOffsetX(0) + GeometryUtils.COLS * 0.5f, -GeometryUtils.MARGIN);
            State = HandState.Idle;
            T = _game.Config.StartFirstAttackDelay;
            Attack = null;
            _path.Clear();
            Bullets.Clear();
            _pendingStun = 0f;
            _execT = 0f;
            IsCharging = false;
            AnimDir = Vector2.zero;
            SetAnim(ANIM_IDLE);
        }

        public void SetAnim(string animName)
        {
            Anim = animName;
            AnimT = 0f;
        }

        /// <summary>마법에 맞으면 잠깐 멈춘다. 공격 동작 중이면 끝난 뒤에 멈춘다.</summary>
        public void Stun(float seconds)
        {
            if (State == HandState.Exec)
            {
                _pendingStun = seconds;
                return;
            }

            Attack = null;
            _path.Clear();
            State = HandState.Stun;
            T = seconds;
            SetAnim(ANIM_STUN);
        }

        /// <summary>진행 중인 예고·공격·소환 탄을 모두 거두고, waitSeconds 뒤에 다음 공격을 고른다(페이즈 전환, 마무리 연출).</summary>
        public void CancelAttack(float waitSeconds)
        {
            Attack = null;
            _path.Clear();
            Bullets.Clear();
            IsCharging = false;
            _pendingStun = 0f;
            State = HandState.Idle;
            T = waitSeconds;
        }

        /// <summary>전환 연출 중에는 공격 없이 동작 시간만 흐른다</summary>
        public void UpdateAnimOnly(float dt)
        {
            AnimT += dt;
        }

        public void Update(float dt)
        {
            AnimT += dt;
            if (Anim == ANIM_FLICK_RELEASE && AnimT > FLICK_RELEASE_TIME)
            {
                SetAnim(ANIM_IDLE);
            }

            UpdateBullets(dt);
            switch (State)
            {
                case HandState.Stun:
                    T -= dt;
                    if (T <= 0f)
                    {
                        State = HandState.Idle;
                        T = 0.3f;
                        SetAnim(ANIM_IDLE);
                    }

                    break;
                case HandState.Idle:
                    UpdateIdle(dt);
                    break;
                case HandState.Travel:
                    if (MoveAlong(dt, Speed))
                    {
                        StartWarn();
                    }

                    break;
                case HandState.Warn:
                    UpdateWarn(dt);
                    break;
                case HandState.Exec:
                    UpdateExec(dt);
                    break;
                default:
                    break;
            }
        }

        private void UpdateIdle(float dt)
        {
            // 쉬는 동안 플레이어 쪽 둘레로 천천히 따라간다
            PlayerController player = _game.Player;
            Vector2 target = PerimeterUtils.Nearest(player.Board, GeometryUtils.CellCenter(player.Board, player.R, player.C));
            SetPath(PerimeterUtils.FindPath(Pos, player.Board, target).Points);
            MoveAlong(dt, Speed * 0.6f);

            // 연습 구간(첫 단어)에는 공격하지 않는다
            if (_game.IsTutorial)
            {
                T = Mathf.Max(T, _game.Config.StartFirstAttackDelay);
                return;
            }

            T -= dt;
            if (T > 0f)
            {
                return;
            }

            AttackPlan attack = _game.Brain.ChooseOrNull();
            if (attack == null)
            {
                T = 0.4f;
                return;
            }

            Attack = attack;
            SetPath(PerimeterUtils.FindPath(Pos, attack.Board, attack.Anchor).Points);
            State = HandState.Travel;
        }

        private void SetPath(List<Vector2> points)
        {
            _path.Clear();
            _path.AddRange(points);
        }

        /// <summary>경로를 따라 speed(칸/초)로 움직인다. 끝까지 갔으면 true.</summary>
        private bool MoveAlong(float dt, float speed)
        {
            float left = speed * dt;
            while (left > 0f && _path.Count > 0)
            {
                Vector2 next = _path[0];
                float dist = Vector2.Distance(Pos, next);
                if (dist <= left)
                {
                    Pos = next;
                    _path.RemoveAt(0);
                    left -= dist;
                }
                else
                {
                    Pos += (next - Pos) / dist * left;
                    left = 0f;
                }
            }

            return _path.Count == 0;
        }

        private void ToIdle(bool isShortWait = false)
        {
            Attack = null;
            IsCharging = false;
            if (_pendingStun > 0f)
            {
                State = HandState.Stun;
                T = _pendingStun;
                _pendingStun = 0f;
                SetAnim(ANIM_STUN);
                return;
            }

            // 플레이어가 있는 판에 블록이 많이 쌓여 있을수록(쏘러 온 것) 더 자주 공격한다
            int stack = _game.Boards[_game.Player.Board].BlockCount();
            State = HandState.Idle;
            T = isShortWait ? 0.3f : _game.Config.AttackInterval * _game.CurrentPhase.IntervalMul * Mathf.Max(0.4f, 1f - _game.Config.BlockAggro * stack);
        }

        // ── 예고 시작: 조준·잠금이 필요한 공격은 여기서 대상을 정하고 공정성을 다시 본다 ──
        private void StartWarn()
        {
            AttackPlan attack = Attack;
            PlayerController player = _game.Player;
            State = HandState.Warn;
            T = attack.Warn;
            if (attack.IsRecheck && player.Board != attack.Board)
            {
                ToIdle(true);
                return;
            }

            switch (attack.Type)
            {
                case AttackType.Aim:
                    if (!LockAim(attack.Warn))
                    {
                        ToIdle(true);
                    }

                    break;
                case AttackType.Click:
                    if (!LockClick(attack.Warn))
                    {
                        ToIdle(true);
                    }

                    break;
                case AttackType.Slot:
                    if (attack.Slot.Block == null)
                    {
                        ToIdle(true);
                        return;
                    }

                    SetPath(new List<Vector2> { attack.Center });
                    break;
                case AttackType.Track:
                    attack.IsLocked = false;
                    T = Def(AttackType.Track).TrackTime;
                    break;
                case AttackType.Drag:
                    {
                        RectInt selection = attack.RectSelection;
                        attack.From = GeometryUtils.CellCenter(attack.Board, selection.y, selection.x);
                        attack.To = GeometryUtils.CellCenter(attack.Board, selection.y + selection.height - 1, selection.x + selection.width - 1);
                        SetPath(new List<Vector2> { attack.From });
                        break;
                    }

                case AttackType.Flick:
                    SetAnim(ANIM_FLICK_CHARGE);
                    AnimDir = attack.Axis == LineAxis.Row ? new Vector2(attack.Sign, 0f) : new Vector2(0f, attack.Sign);
                    break;
                default:
                    break;
            }
        }

        private AttackDef Def(AttackType type)
        {
            return _game.Brain.Def(type);
        }

        private bool LockAim(float time)
        {
            AttackPlan attack = Attack;
            PlayerController player = _game.Player;
            attack.From = Pos;
            attack.To = PerimeterUtils.RayExit(attack.Board, attack.From, GeometryUtils.CellCenter(player.Board, player.R, player.C));
            attack.Cells = _game.Brain.AimCells(_game.Boards[attack.Board], attack.From, attack.To);
            attack.Warn = time;
            return _game.Brain.CanEscape(attack.Board, attack.Cells, time, false);
        }

        private bool LockClick(float time)
        {
            AttackPlan attack = Attack;
            PlayerController player = _game.Player;
            BoardCell cell = _game.Boards[attack.Board].GetCellOrNull(player.R, player.C);
            attack.Cells = new List<BoardCell> { cell };
            attack.Center = GeometryUtils.CellCenter(attack.Board, cell.R, cell.C);
            SetPath(new List<Vector2> { attack.Center });
            return _game.Brain.CanEscape(attack.Board, attack.Cells, time, false);
        }

        private void UpdateWarn(float dt)
        {
            AttackPlan attack = Attack;
            PlayerController player = _game.Player;
            T -= dt;
            if (attack.IsRecheck && player.Board != attack.Board)
            {
                // 플레이어가 반대편으로 넘어가면 판 위 공격은 취소
                ToIdle(true);
                return;
            }

            if (attack.Type == AttackType.Click || attack.Type == AttackType.Slot)
            {
                MoveAlong(dt, Speed * 1.6f);
            }

            if (attack.Type == AttackType.Slot && attack.Slot.Block == null)
            {
                // 플레이어가 먼저 쏴 버리면 취소
                ToIdle(true);
                return;
            }

            if (attack.Type == AttackType.Drag)
            {
                if (_path.Count > 0)
                {
                    if (MoveAlong(dt, Speed * 1.6f))
                    {
                        attack.DragT = T;
                    }
                }
                else
                {
                    float u = attack.DragT > 0.05f ? Mathf.Min(1f, 1f - (T - attack.DragT * 0.25f) / (attack.DragT * 0.75f)) : 1f;
                    Pos = Vector2.Lerp(attack.From, attack.To, Mathf.Max(0f, u));
                }
            }

            if (attack.Type == AttackType.Track && !attack.IsLocked)
            {
                UpdateTrack(dt);
                return;
            }

            if (T <= 0f)
            {
                Fire();
            }
        }

        // 추적 내려찍기: 그림자가 플레이어를 따라다니다가(플레이어보다 느림) 2x2로 잠근다
        private void UpdateTrack(float dt)
        {
            AttackPlan attack = Attack;
            PlayerController player = _game.Player;
            AttackDef def = Def(AttackType.Track);
            Vector2 target = GeometryUtils.CellCenter(player.Board, player.R, player.C) + new Vector2(0.25f, 0.25f);
            float step = def.FollowSpeed / GeometryUtils.WEB_CELL_PX * _game.CurrentPhase.SpeedMul * dt;
            Pos = Vector2.MoveTowards(Pos, target, step);
            if (T > 0f)
            {
                return;
            }

            int r = Mathf.FloorToInt(Pos.y + 0.5f);
            int c = Mathf.FloorToInt(Pos.x - GeometryUtils.BoardOffsetX(attack.Board) + 0.5f);
            attack.Cells = _game.Boards[attack.Board].Area(r - 1, c - 1, 2, 2);
            attack.IsLocked = true;
            T = attack.Warn * def.LockMul;
            if (attack.Cells.Count == 0 || !_game.Brain.CanEscape(attack.Board, attack.Cells, T, false))
            {
                ToIdle(true);
            }
        }

        private bool IsPlayerIn(AttackPlan attack)
        {
            PlayerController player = _game.Player;
            return player.Board == attack.Board && attack.Cells.Exists(cell => cell.Is(player.R, player.C));
        }

        private void CountAttack(AttackPlan attack)
        {
            GameStats.Increment(_game.Stats.Attacks, attack.Type.ToId());
        }

        /// <summary>테스트·디버그용: 예고 없이 그 공격을 바로 실행한다.</summary>
        public void ForceFire(AttackPlan attack)
        {
            Attack = attack;
            State = HandState.Warn;
            Fire();
        }

        private void Fire()
        {
            AttackPlan attack = Attack;
            AttackDef def = Def(attack.Type);
            PlayerController player = _game.Player;
            BoardModel board = _game.Boards[attack.Board];
            switch (attack.Type)
            {
                case AttackType.Flick:
                    FireFlick(attack, def, board);
                    break;
                case AttackType.Summon:
                    {
                        CountAttack(attack);
                        int length = attack.Axis == LineAxis.Row ? GeometryUtils.COLS : GeometryUtils.ROWS;
                        Bullets.Add(new Bullet
                        {
                            Board = attack.Board,
                            Axis = attack.Axis,
                            Index = attack.Index,
                            Pos = attack.End == 0 ? -0.8f : length - 0.2f,
                            Dir = attack.End == 0 ? 1 : -1,
                            Speed = def.BulletSpeed * _game.CurrentPhase.SpeedMul,
                        });
                        ToIdle();
                        break;
                    }

                case AttackType.Full:
                    CountAttack(attack);
                    if (IsPlayerIn(attack))
                    {
                        player.Hurt(attack.HitName);
                    }

                    AddImpact(attack);
                    _game.Feel.Hit(HitKind.Slam, Vector2.down);
                    State = HandState.Exec;
                    _execT = 0f;
                    break;
                case AttackType.Charge:
                    CountAttack(attack);
                    State = HandState.Exec;
                    IsCharging = true;
                    SetPath(new List<Vector2> { PerimeterUtils.LineAnchor(attack.Board, attack.Axis, attack.Index, 1 - attack.End) });
                    break;
                case AttackType.Aim:
                    if (!attack.IsCounted)
                    {
                        CountAttack(attack);
                        attack.IsCounted = true;
                    }

                    _game.Stats.AimDashes++;
                    State = HandState.Exec;
                    IsCharging = true;
                    SetPath(new List<Vector2> { attack.To });
                    break;
                case AttackType.Track:
                    CountAttack(attack);
                    if (IsPlayerIn(attack))
                    {
                        player.Hurt(attack.HitName);
                    }

                    AddImpact(attack);
                    _game.Feel.Hit(HitKind.Slam, Vector2.down);
                    ToIdle();
                    break;
                case AttackType.Slot:
                    FireSlotBreak(attack, def);
                    break;
                case AttackType.Click:
                    FireClick(attack, def);
                    break;
                case AttackType.Drag:
                    CountAttack(attack);
                    if (IsPlayerIn(attack))
                    {
                        player.Hurt(attack.HitName);
                    }

                    AddImpact(attack);
                    ToIdle();
                    break;
                default:
                    ToIdle();
                    break;
            }
        }

        // 줄 섞기: 회전 밀기. 줄 위의 플레이어는 피해를 받고 키와 함께 밀린다(SHIFT 칸 위는 안전).
        private void FireFlick(AttackPlan attack, AttackDef def, BoardModel board)
        {
            CountAttack(attack);
            PlayerController player = _game.Player;
            float step = _game.Config.DominoStep;
            float slide = _game.Config.RotateSlide;
            if (player.Board == attack.Board && !player.Here.IsShift && attack.Cells.Exists(cell => cell.Is(player.R, player.C)))
            {
                player.Hurt(attack.HitName);
                BoardCell to = board.RideLineOrNull(attack.Axis, attack.Index, player.R, player.C, attack.ShiftCount, attack.Sign, step, slide, out SlideAnim ride);
                if (to != null)
                {
                    player.R = to.R;
                    player.C = to.C;
                    player.Slide = ride;
                    player.Dwell = 0f;
                }
            }

            board.RotateLine(attack.Axis, attack.Index, attack.ShiftCount, attack.Sign, step, slide);
            SetAnim(ANIM_FLICK_RELEASE);
            ToIdle();

            // 손이 섞다가 우연히 단어가 맞춰지면 그것도 완성
            _game.OnBoardChanged();
        }

        // 슬롯 깨기: 블록 내구도 -1(흔들림), 0이 되면 블록이 사라진다. 슬롯 위의 플레이어도 맞는다.
        private void FireSlotBreak(AttackPlan attack, AttackDef def)
        {
            CountAttack(attack);
            if (IsPlayerIn(attack))
            {
                _game.Player.Hurt(attack.HitName);
            }

            AttackBlock block = attack.Slot.Block;
            if (block != null)
            {
                block.Hp--;
                block.HitT = 0.4f;
                bool isBroken = block.Hp <= 0;
                HitKind kind = isBroken ? HitKind.BlockBreak : HitKind.BlockChip;
                HitFeelLevel level = _game.Data.HitFeel.Get(kind);
                block.FlashT = Mathf.Max(level.FlashTime, level.HitStop) * _game.Data.HitFeel.FlashScale;
                _game.Feel.Hit(kind, Vector2.down);
                SpellDef spell = _game.Data.GetSpellOrNull(block.Word);
                _game.AddDebris(kind, GeometryUtils.CellCenter(attack.Board, attack.Slot.R, attack.Slot.C), Vector2.down, spell != null ? spell.Color : PaletteUtils.Glow, "block");
                if (isBroken)
                {
                    attack.Slot.Block = null;
                    _game.Fx.Add(FxEvent.AtCell(FxKind.Fizzle, attack.Board, attack.Slot.R, attack.Slot.C, 0.5f));
                    _game.ShowToast($"{block.Word} BLOCK BROKEN");
                    _game.Stats.BlocksLost++;
                }
            }
            else
            {
                _game.Feel.Hit(HitKind.Slam, Vector2.down);
            }

            _game.Fx.Add(new FxEvent { Kind = FxKind.Click, From = attack.Center, T = 0.25f, Duration = 0.25f });
            ToIdle();
        }

        private void FireClick(AttackPlan attack, AttackDef def)
        {
            if (!attack.IsSecond)
            {
                CountAttack(attack);
            }

            if (IsPlayerIn(attack))
            {
                _game.Player.Hurt(attack.HitName);
            }

            _game.Fx.Add(new FxEvent { Kind = FxKind.Click, From = attack.Center, T = 0.25f, Duration = 0.25f });
            if (attack.IsDouble && !attack.IsSecond && _game.Player.Board == attack.Board && LockClick(def.DoubleGap))
            {
                attack.IsSecond = true;
                _game.Stats.DoubleClicks++;
                State = HandState.Warn;
                T = def.DoubleGap;
                attack.Warn = def.DoubleGap;
                return;
            }

            ToIdle();
        }

        private void AddImpact(AttackPlan attack)
        {
            _game.Fx.Add(new FxEvent { Kind = FxKind.Impact, Board = attack.Board, Cells = FxEvent.ToCellList(attack.Cells), T = 0.3f, Duration = 0.3f });
        }

        private void UpdateExec(float dt)
        {
            AttackPlan attack = Attack;
            PlayerController player = _game.Player;
            _execT += dt;
            if (attack.Type == AttackType.Charge || attack.Type == AttackType.Aim)
            {
                AttackDef def = Def(attack.Type);
                float speed = def.ChargeSpeed / GeometryUtils.WEB_CELL_PX * _game.CurrentPhase.SpeedMul;

                // 터널링 방지: 한 프레임을 잘게 나눠서 본체 충돌을 본다
                int substeps = Mathf.Max(1, Mathf.CeilToInt(speed * dt / 0.3f));
                bool hasArrived = false;
                for (int i = 0; i < substeps && !hasArrived; i++)
                {
                    hasArrived = MoveAlong(dt / substeps, speed);
                    Vector2 center = GeometryUtils.CellCenter(player.Board, player.R, player.C);
                    if (player.Board == attack.Board && Vector2.Distance(Pos, center) < CHARGE_HIT_RADIUS)
                    {
                        player.Hurt(attack.HitName);
                    }
                }

                if (!hasArrived)
                {
                    return;
                }

                IsCharging = false;
                if (attack.Type == AttackType.Aim && --attack.Chain > 0 && player.Board == attack.Board && _game.State == GameState.Play)
                {
                    float warn = attack.Warn * def.ChainWarnMul;
                    if (LockAim(warn))
                    {
                        State = HandState.Warn;
                        T = warn;
                        return;
                    }
                }

                ToIdle();
                return;
            }

            // 전체 공격: 가운데를 찍고 돌아온다
            float u = Mathf.Min(1f, _execT / FULL_SLAM_TIME);
            float s = u < 0.4f ? u / 0.4f : 1f - (u - 0.4f) / 0.6f;
            Pos = Vector2.Lerp(attack.Anchor, attack.Center, s);
            if (u >= 1f)
            {
                Pos = attack.Anchor;
                ToIdle();
            }
        }

        private void UpdateBullets(float dt)
        {
            PlayerController player = _game.Player;
            foreach (Bullet bullet in Bullets)
            {
                bullet.Pos += bullet.Dir * bullet.Speed * dt;
                int i = Mathf.FloorToInt(bullet.Pos + 0.5f);
                int r = bullet.Axis == LineAxis.Row ? bullet.Index : i;
                int c = bullet.Axis == LineAxis.Row ? i : bullet.Index;
                BoardCell cell = _game.Boards[bullet.Board].GetCellOrNull(r, c);

                // 플레이어를 맞히면 탄은 거기서 사라진다
                if (cell != null && player.Board == bullet.Board && player.R == r && player.C == c && player.Hurt(AttackType.Summon.ToDisplayName()))
                {
                    bullet.IsDead = true;
                    continue;
                }

                int length = bullet.Axis == LineAxis.Row ? GeometryUtils.COLS : GeometryUtils.ROWS;
                bullet.IsDead = bullet.Pos < -1f || bullet.Pos > length;
            }

            Bullets.RemoveAll(bullet => bullet.IsDead);
        }
    }
}
