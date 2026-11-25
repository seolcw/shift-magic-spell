using System.Collections.Generic;
using ShiftMagicSpell.Data;
using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>
    /// 게임 한 판의 상태와 진행(웹의 Game 클래스에서 렌더를 뺀 부분). MonoBehaviour 없이 Update(dt)로 돌아간다.
    /// </summary>
    public class GameSession
    {
        public readonly AttackPatternsData Data;
        public readonly BoardModel[] Boards;
        public readonly PlayerController Player;
        public readonly HandController Hand;
        public readonly HandBrain Brain;
        public readonly WordSystem Words;
        public readonly GameRandom Random;
        public readonly List<FxEvent> Fx = new List<FxEvent>();

        /// <summary>타격감(멈춤·흔들림·번쩍임·넉백·빨간 테두리). 화면 흔들림도 여기 있다.</summary>
        public readonly HitFeedback Feel;

        public GameStats Stats = new GameStats();
        public int Phase;
        public GameState State = GameState.Ready;
        public float Time;
        public float StateT;
        public TimedText Toast;
        public TimedText Banner;
        public bool IsInvincible;
        public bool IsPaused;

        // 마무리 단어(SHIFT)로 바뀐 직후 FINAL 배너를 보여 주는 남은 시간
        public float FinisherIntroT;

        // 연습 구간(새 판의 첫 단어): 첫 블록을 쏘면 끝난다. 새 판(StartNewRun)에서만 켠다.
        public bool IsTutorialDone = true;

        // 마지막으로 맞은 공격 이름(사망 화면 원인)
        public string LastHitSource = string.Empty;

        // 단어 위치 힌트 점멸 기준 시각(새 단어가 나오면 다시 센다)
        public float HintT0;

        // 마지막으로 슬롯을 발동한 판·시각·콤보 단계(같은 판을 comboWindow초 안에 연달아 쏘면 콤보)
        public int LastFireBoard = -1;
        public float LastFireTime;
        public int LastFireCombo;

        // 날아가는 중인 마법들. 손에 닿는 순간 타격감을 건다. 콤보는 앞 마법이 닿기 전에 다음 마법을 쏠 수 있어서 목록으로 둔다.
        private readonly List<PendingSpellHit> _pendingSpellHits = new List<PendingSpellHit>();

        private class PendingSpellHit
        {
            public float T;
            public HitKind Kind;
            public Vector2 From;
            public Color Color;
        }

        public GameSession(AttackPatternsData data, BoardLayoutData layout, int seed)
        {
            Data = data;
            Feel = new HitFeedback(this);
            Random = new GameRandom(seed);
            Boards = new[] { new BoardModel(0, layout), new BoardModel(1, layout) };
            Player = new PlayerController(this);
            Hand = new HandController(this);
            Brain = new HandBrain(this);
            Words = new WordSystem(this);
        }

        public ConfigValues Config => Data.Config;
        public PhaseDef CurrentPhase => Data.Phases[Phase];
        public bool IsLastPhase => Phase == Data.Phases.Count - 1;

        /// <summary>연습 구간: 손이 공격하지 않고, 단어 힌트가 계속 켜져 있고, 안내가 차례로 나온다</summary>
        public bool IsTutorial => Phase == 0 && !IsTutorialDone;

        /// <summary>연습 안내 단계: 0 이동, 1 Shift+방향키로 줄 돌리기, 2 단어를 한 줄로, 3 반대편 슬롯에서 Space</summary>
        public int TutorialStep
        {
            get
            {
                if (Stats.Moves == 0)
                {
                    return 0;
                }

                if (Stats.Rotations == 0)
                {
                    return 1;
                }

                return Stats.Stored == 0 ? 2 : 3;
            }
        }

        /// <summary>0~1: 단어 위치 힌트 세기. hintInterval초마다 hintDuration초 동안 두 번 깜빡이고, 나머지 시간에는 0.</summary>
        public float HintAlpha()
        {
            if (Words.Current == null)
            {
                return 0f;
            }

            // 연습 구간에는 글자 위치를 계속 보여 준다
            if (IsTutorial)
            {
                return 1f;
            }

            float local = Mathf.Repeat(Time - HintT0, Config.HintInterval);
            if (local > Config.HintDuration)
            {
                return 0f;
            }

            return Mathf.Sin(Mathf.Repeat(local / Config.HintDuration * 2f, 1f) * Mathf.PI);
        }

        /// <summary>페이즈 시작(판도 시작 배치로). handHp를 주면 손 HP를 그 값으로 시작한다(사망 후 재시작).</summary>
        public void StartPhase(int phase, int? handHp = null)
        {
            Phase = phase;
            foreach (BoardModel board in Boards)
            {
                board.Reset();
            }

            Player.Reset();
            Hand.Reset();
            if (handHp.HasValue)
            {
                Hand.Hp = handHp.Value;
            }

            Brain.Reset();
            Words.Reset();
            Words.Refresh();
            Fx.Clear();
            LastFireBoard = -1;
            _pendingSpellHits.Clear();
            Feel.Reset();
            FinisherIntroT = 0f;
            State = GameState.Play;
            IsPaused = false;
            Banner = new TimedText($"PHASE {phase + 1}", 1.4f);
        }

        /// <summary>처음부터 새 판(첫 화면·승리 화면에서). 기록도 새로 센다.</summary>
        public void StartNewRun()
        {
            Stats = new GameStats();
            IsTutorialDone = false;
            StartPhase(0);
        }

        /// <summary>R: 현재 페이즈를 처음부터(손 HP도 가득). 승리 화면에서는 1페이즈부터 새 판.</summary>
        public void RestartPhase()
        {
            if (State == GameState.Win)
            {
                StartNewRun();
                return;
            }

            StartPhase(Phase);
        }

        /// <summary>디버그: 다음 페이즈로 건너뛴다(마지막 페이즈면 승리).</summary>
        public void SkipPhase()
        {
            if (!IsLastPhase)
            {
                StartPhase(Phase + 1);
                return;
            }

            State = GameState.Win;
            StateT = 0f;
        }

        /// <summary>사망 후 재시작할 손 HP: 잃은 만큼의 deathHandRecover(절반)만 회복(올림)</summary>
        public int GetRecoveredHandHp()
        {
            int maxHp = Hand.MaxHp;
            return Mathf.CeilToInt(Hand.Hp + (maxHp - Hand.Hp) * Config.DeathHandRecover);
        }

        public void ShowToast(string text)
        {
            Toast = new TimedText(text, 1.8f);
        }

        public void OnPlayerHurt(string source)
        {
            GameStats.Increment(Stats.Hits, source);
            LastHitSource = source;
            Vector2 center = GeometryUtils.CellCenter(Player.Board, Player.R, Player.C);
            Fx.Add(new FxEvent { Kind = FxKind.Float, From = center - new Vector2(0f, 0.4f), Text = "-1", Color = PaletteUtils.Hp, T = 0.9f, Duration = 0.9f });

            // 타격감: 손이 있는 쪽에서 맞아 반대쪽으로 밀린다. 마지막 HP면 더 세게.
            HitKind kind = Player.Hp <= 0 ? HitKind.PlayerDeath : HitKind.PlayerHurt;
            Vector2 away = GetDirection(Hand.Pos, center, Vector2.down);
            Feel.Hit(kind, away);
            AddDebris(kind, center, away, PaletteUtils.Player, "player");
            if (Player.Hp <= 0)
            {
                State = GameState.Dead;
                StateT = 1.3f;
                Stats.Deaths++;
            }
        }

        /// <summary>판이 바뀔 때마다(플레이어·손의 줄 돌리기) 부른다. 손이 섞다가 우연히 맞춰진 단어도 완성으로 친다.</summary>
        public void OnBoardChanged()
        {
            // 줄 섞기에 맞아 쓰러진 같은 프레임에 단어가 맞춰져도 쓰러짐 상태를 덮지 않게
            if (State != GameState.Play)
            {
                return;
            }

            Words.Check();
        }

        /// <summary>
        /// 단어 완성. 보통 단어는 바로 쏘지 않고 공격 블록이 되어 반대편 판의 빈 슬롯에 들어간다(양쪽 판을 오가게 만드는 장치).
        /// 반대편 슬롯이 꽉 찼으면 바로 발동하되 피해는 overflowMul. 마무리 단어는 슬롯을 거치지 않고 바로 발동.
        /// </summary>
        public void OnWordComplete(string word, AlignResult align)
        {
            Stats.WordsDone++;
            Vector2 from = CenterOf(align.Board, align.Cells);
            Fx.Add(new FxEvent { Kind = FxKind.Match, Board = align.Board, Cells = FxEvent.ToCellList(align.Cells), T = 0.6f, Duration = 0.6f });
            if (word == Data.FinisherWord)
            {
                CastSpell(word, from, 1f);
                return;
            }

            BoardModel target = Boards[1 - align.Board];
            BoardCell slot = target.EmptySlotOrNull();
            if (slot == null)
            {
                ShowToast("SLOTS FULL: CAST NOW (80%)");
                CastSpell(word, from, Config.OverflowMul);
                return;
            }

            slot.Block = new AttackBlock { Word = word, Hp = Config.BlockHp };
            Stats.Stored++;
            SpellDef spell = Data.GetSpellOrNull(word);
            Fx.Add(new FxEvent
            {
                Kind = FxKind.Store,
                Word = word,
                Color = spell != null ? spell.Color : PaletteUtils.Glow,
                From = from,
                To = GeometryUtils.CellCenter(target.Index, slot.R, slot.C),
                T = Config.StoreTime,
                Duration = Config.StoreTime,
            });
            ShowToast($"{word} → {(target.Index == 0 ? "LEFT" : "RIGHT")} SLOT");
        }

        /// <summary>슬롯에서 Space: 블록 발동. 같은 판 슬롯을 comboWindow초 안에 연달아 쏘면 콤보 보너스.</summary>
        public bool FireSlot(BoardModel board, BoardCell slot)
        {
            AttackBlock block = slot.Block;
            if (block == null || State != GameState.Play)
            {
                return false;
            }

            slot.Block = null;
            int combo = IsComboOpen(board.Index) ? LastFireCombo + 1 : 0;
            LastFireBoard = board.Index;
            LastFireTime = Time;
            LastFireCombo = combo;
            if (combo > 0)
            {
                ShowToast($"COMBO ×{combo + 1}! +{Mathf.RoundToInt(Config.ComboBonus * combo * 100f)}%");
                Stats.Combos++;
            }

            CastSpell(block.Word, GeometryUtils.CellCenter(board.Index, slot.R, slot.C), 1f + Config.ComboBonus * combo);
            if (IsTutorial)
            {
                // 첫 블록을 쏘면 연습이 끝나고 손 공격이 시작된다(마법에 맞은 멈춤이 끝난 뒤)
                IsTutorialDone = true;
                ShowToast("PRACTICE OVER - THE HAND FIGHTS BACK!");
            }

            return true;
        }

        /// <summary>그 판 슬롯을 지금 쏘면 콤보가 되는가</summary>
        public bool IsComboOpen(int board)
        {
            return LastFireBoard == board && Time - LastFireTime <= Config.ComboWindow;
        }

        /// <summary>
        /// 마법 발동: 피해 = 글자 수 × 배율(반올림, 최소 1). 손은 맞은 뒤 잠깐 멈춘다. 준 피해를 돌려준다.
        /// 타격감 세기는 배율로 정한다: 마무리 SHIFT, 1보다 크면 콤보, 1보다 작으면 넘침(80%), 1이면 보통.
        /// </summary>
        public int CastSpell(string word, Vector2 from, float multiplier)
        {
            bool isFinisher = word == Data.FinisherWord;
            HitKind hitKind = isFinisher ? HitKind.Finisher : multiplier > 1f ? HitKind.Combo : multiplier < 1f ? HitKind.Overflow : HitKind.Spell;
            SpellDef spell = Data.GetSpellOrNull(word);
            // 웹(Math.round)과 같이 .5는 올린다
            int damage = Mathf.Max(1, Mathf.FloorToInt(word.Length * multiplier + 0.5f));

            // 마지막 페이즈에서는 마무리 단어로만 쓰러진다: 다른 단어는 손 HP를 1 아래로 못 내린다
            if (IsLastPhase && !isFinisher)
            {
                damage = Mathf.Min(damage, Mathf.Max(0, Hand.Hp - 1));
            }

            Hand.Hp = isFinisher ? 0 : Mathf.Max(0, Hand.Hp - damage);
            Stats.Casts++;
            float duration = Config.SpellTime;
            Color color = spell != null ? spell.Color : PaletteUtils.Glow;
            Fx.Add(new FxEvent
            {
                Kind = FxKind.Spell,
                Style = spell != null ? spell.Fx : "beam",
                Word = word,
                Color = color,
                Count = spell != null ? Mathf.Max(1, spell.Count) : 1,
                Size = spell != null ? spell.Size : 10f,
                From = from,
                T = duration,
                Duration = duration,
                Seed = Random.Range(0f, 1000f),
            });
            Fx.Add(new FxEvent
            {
                Kind = FxKind.Float,
                From = Hand.Pos - new Vector2(0f, 1f),
                Text = isFinisher ? "SHIFT!" : $"{word} -{damage}",
                Color = color,
                T = 1.3f + duration * 0.55f,
                Duration = 1.3f,
                Delay = duration * 0.55f,
            });
            _pendingSpellHits.Add(new PendingSpellHit { T = duration * 0.55f, Kind = hitKind, From = from, Color = color });
            if (Hand.Hp <= 0)
            {
                // 한 판 끝: 진행 중인 예고·탄·공격은 모두 거둔다(전환·승리 화면 동안 공격 없음, 플레이어는 상태가 Play가 아니라 무적)
                Hand.CancelAttack(0f);
                State = IsLastPhase ? GameState.Win : GameState.PhaseClear;
                StateT = IsLastPhase ? 0f : Config.PhaseClearTime;
                return damage;
            }

            Hand.Stun(Config.SpellStun + duration * 0.5f);
            Words.Refresh();
            return damage;
        }

        public void Update(float dt)
        {
            if (IsPaused)
            {
                return;
            }

            // 타격감은 실제 시간으로 흐르고, 멈춤(히트스톱) 동안에는 로직 전체(시간·연출·손·플레이어)를 세운다.
            // 멈춘 동안 누른 입력은 PlayerController가 예약해 두었다가 멈춤이 끝나면 실행한다.
            Feel.Update(dt);
            if (Feel.IsHitStopped)
            {
                return;
            }

            Time += dt;
            UpdateFx(dt);
            if (Toast != null && (Toast.T -= dt) <= 0f)
            {
                Toast = null;
            }

            if (Banner != null && (Banner.T -= dt) <= 0f)
            {
                Banner = null;
            }

            UpdatePendingSpellHits(dt);

            switch (State)
            {
                case GameState.Win:
                    StateT += dt;
                    break;
                case GameState.Dead:
                    // 사망: 현재 페이즈 처음부터(판도 시작 배치로), 손 HP는 잃은 만큼의 절반만 회복. 슬롯에 모아 둔 블록은 남긴다.
                    StateT -= dt;
                    if (StateT <= 0f)
                    {
                        AttackBlock[][] kept = SaveBlocks();
                        StartPhase(Phase, GetRecoveredHandHp());
                        RestoreBlocks(kept);
                    }

                    break;
                case GameState.PhaseClear:
                    // 손이 흔들리다 화면 위로 물러난다(그림은 HandView) → 다음 페이즈 배너
                    StateT -= dt;
                    Hand.UpdateAnimOnly(dt);
                    UpdateBoards(dt);
                    if (StateT <= 0f)
                    {
                        StartPhase(Phase + 1);
                        State = GameState.PhaseIntro;
                        StateT = Config.PhaseIntroTime;
                        Banner = null;
                    }

                    break;
                case GameState.PhaseIntro:
                    // 새 판을 보여 주며 배너. 끝나면 새 단어를 한 번 깜빡이고, 첫 공격 전 여유를 둔다
                    StateT -= dt;
                    Hand.UpdateAnimOnly(dt);
                    UpdateBoards(dt);
                    if (StateT <= 0f)
                    {
                        State = GameState.Play;
                        Hand.CancelAttack(Config.PhaseFirstAttackDelay);
                        HintT0 = Time;
                    }

                    break;
                case GameState.Play:
                    UpdatePlay(dt);
                    break;
                default:
                    break;
            }
        }

        /// <summary>마지막 페이즈에서 목표가 마무리 단어로 바뀐 순간: FINAL 배너, 손은 잠깐 공격하지 않는다</summary>
        public void OnFinisherStarted()
        {
            FinisherIntroT = Config.FinisherIntroTime;
            Hand.CancelAttack(Config.FinisherIntroTime + Config.PhaseFirstAttackDelay);
        }

        /// <summary>
        /// 타격감 조각 연출을 남긴다. style: player(분홍 조각), hand(흰 조각 + 마법 색 별), block(블록 조각).
        /// dir = 조각이 주로 튀는 방향(맞은 쪽이 밀리는 방향).
        /// </summary>
        public void AddDebris(HitKind kind, Vector2 position, Vector2 dir, Color color, string style)
        {
            int count = Data.HitFeel.Get(kind).Debris;
            if (count <= 0)
            {
                return;
            }

            const float DEBRIS_TIME = 0.5f;
            Fx.Add(new FxEvent
            {
                Kind = FxKind.Debris,
                From = position,
                To = dir,
                Color = color,
                Count = count,
                Style = style,
                Seed = Random.Range(0f, 1000f),
                T = DEBRIS_TIME,
                Duration = DEBRIS_TIME,
            });
        }

        /// <summary>from → to 방향(길이 1). 두 점이 거의 같으면 fallback.</summary>
        public static Vector2 GetDirection(Vector2 from, Vector2 to, Vector2 fallback)
        {
            Vector2 delta = to - from;
            return delta.sqrMagnitude > 0.0001f ? delta.normalized : fallback;
        }

        // 마법이 손에 닿는 순간: 손이 움찔하고(Hurt), 날아온 방향으로 밀리며 번쩍이고 조각이 튄다
        private void UpdatePendingSpellHits(float dt)
        {
            for (int i = _pendingSpellHits.Count - 1; i >= 0; i--)
            {
                PendingSpellHit hit = _pendingSpellHits[i];
                hit.T -= dt;
                if (hit.T > 0f)
                {
                    continue;
                }

                _pendingSpellHits.RemoveAt(i);
                Vector2 push = GetDirection(hit.From, Hand.Pos, Vector2.up);
                Hand.SetAnim(HandController.ANIM_HURT);
                Feel.Hit(hit.Kind, push);
                AddDebris(hit.Kind, Hand.Pos, push, hit.Color, "hand");
            }
        }

        private void UpdateBoards(float dt)
        {
            foreach (BoardModel board in Boards)
            {
                board.Update(dt);
            }
        }

        private void UpdatePlay(float dt)
        {
            Stats.PlayTime += dt;
            FinisherIntroT = Mathf.Max(0f, FinisherIntroT - dt);
            UpdateBoards(dt);

            Brain.Update(dt);
            Player.Update(dt);
            if (State == GameState.Play)
            {
                Hand.Update(dt);
            }
        }

        private void UpdateFx(float dt)
        {
            for (int i = Fx.Count - 1; i >= 0; i--)
            {
                FxEvent fx = Fx[i];
                fx.T -= dt;
                fx.Age += dt;
                if (fx.T <= 0f)
                {
                    Fx.RemoveAt(i);
                }
            }
        }

        private AttackBlock[][] SaveBlocks()
        {
            var kept = new AttackBlock[Boards.Length][];
            for (int b = 0; b < Boards.Length; b++)
            {
                kept[b] = new AttackBlock[Boards[b].Slots.Count];
                for (int i = 0; i < Boards[b].Slots.Count; i++)
                {
                    kept[b][i] = Boards[b].Slots[i].Block;
                }
            }

            return kept;
        }

        private void RestoreBlocks(AttackBlock[][] kept)
        {
            for (int b = 0; b < Boards.Length; b++)
            {
                for (int i = 0; i < Boards[b].Slots.Count && i < kept[b].Length; i++)
                {
                    Boards[b].Slots[i].Block = kept[b][i];
                }
            }
        }

        private static Vector2 CenterOf(int board, List<BoardCell> cells)
        {
            Vector2 sum = Vector2.zero;
            foreach (BoardCell cell in cells)
            {
                sum += GeometryUtils.CellCenter(board, cell.R, cell.C);
            }

            return cells.Count > 0 ? sum / cells.Count : sum;
        }
    }
}
