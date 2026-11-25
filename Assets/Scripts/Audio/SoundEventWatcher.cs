using System.Collections.Generic;
using ShiftMagicSpell.Core;
using ShiftMagicSpell.Data;

namespace ShiftMagicSpell.Audio
{
    /// <summary>
    /// GameSession을 매 프레임 읽어서 지난 프레임과 달라진 것(새 FxEvent, Stats 카운터, State·손 상태 전환, 플레이어 칸)을
    /// 소리 이벤트로 바꾼다. 게임 코드에 호출을 넣지 않고 바깥에서 관찰하므로 Core를 고치지 않아도 된다.
    /// </summary>
    public class SoundEventWatcher
    {
        // 페이즈 클리어·승리 상태는 마지막 마법을 시전한 프레임에 바로 바뀐다. 팡파르는 마법이 손에 맞는 순간에 내고,
        // 맞는 순간을 못 보면(디버그 건너뛰기 등) 이 시간 뒤에 낸다.
        private const float JINGLE_FALLBACK_TIME = 1.5f;

        private readonly List<SoundKey> _keys = new List<SoundKey>();
        // 이번 프레임 타격 소리의 세기(타격감 HitFeel의 SoundVolume·SoundPitch). 큰 타격은 낮고 크게, 약한 타격은 작게.
        private readonly Dictionary<SoundKey, HitFeelLevel> _weights = new Dictionary<SoundKey, HitFeelLevel>();
        private int _feelSerial;
        private HashSet<FxEvent> _prevFx = new HashSet<FxEvent>();
        private HashSet<FxEvent> _curFx = new HashSet<FxEvent>();

        private GameSession _session;
        private GameStats _stats;
        private GameState _state;
        private TimedText _banner;
        private float _finisherIntroT;
        private HandState _handState;
        private string _handAnim;
        private float _handAnimT;
        private int _playerBoard;
        private int _playerR;
        private int _playerC;
        private int _wordsDone;
        private int _combos;
        private int _hits;
        private SoundKey? _pendingJingle;
        private float _jingleAt;

        public GameSession Session => _session;

        /// <summary>관찰할 판을 바꾼다. 지금 상태를 기준으로 삼으므로 이미 있던 연출에는 소리를 내지 않는다.</summary>
        public void Bind(GameSession session)
        {
            _session = session;
            _pendingJingle = null;
            _prevFx.Clear();
            if (session == null)
            {
                return;
            }

            foreach (FxEvent fx in session.Fx)
            {
                _prevFx.Add(fx);
            }

            _state = session.State;
            _feelSerial = session.Feel.Serial;
            _banner = session.Banner;
            _finisherIntroT = session.FinisherIntroT;
            SnapshotStats();
            SnapshotHand();
            SnapshotPlayer();
        }

        /// <summary>지난 Poll 이후 생긴 소리 이벤트를 돌려준다. 돌려준 목록은 다음 Poll에서 다시 쓴다.</summary>
        public IReadOnlyList<SoundKey> Poll()
        {
            _keys.Clear();
            _weights.Clear();
            if (_session == null)
            {
                return _keys;
            }

            PollHitWeight();

            // 새 판(StartNewRun)은 Stats를 새로 만든다: 카운터 기준만 다시 잡는다
            if (_session.Stats != _stats)
            {
                SnapshotStats();
            }

            bool isStateChanged = PollState();
            bool isPhaseStarted = _session.Banner != null && _session.Banner != _banner;
            _banner = _session.Banner;

            // 마무리 단어(SHIFT)로 바뀌며 FINAL 배너가 뜨는 순간
            bool isFinisherIntro = _session.FinisherIntroT > 0f && _finisherIntroT <= 0f;
            _finisherIntroT = _session.FinisherIntroT;
            if (isFinisherIntro)
            {
                _keys.Add(SoundKey.Combo);
            }
            bool hasWarp = PollFx();
            bool isHurt = PollStats();
            PollHand();
            // 순간이동·페이즈 시작·피격(줄 섞기에 같이 밀림)으로 칸이 바뀐 것은 걸음이 아니다
            PollPlayer(isStateChanged || isPhaseStarted || hasWarp || isHurt);
            PollJingleFallback();
            return _keys;
        }

        /// <summary>이번 프레임에 난 타격 소리의 세기. 없으면 false(보통 세기로 낸다).</summary>
        public bool TryGetWeight(SoundKey key, out float volume, out float pitch)
        {
            if (_weights.TryGetValue(key, out HitFeelLevel level))
            {
                volume = level.SoundVolume;
                pitch = level.SoundPitch;
                return true;
            }

            volume = 1f;
            pitch = 1f;
            return false;
        }

        // 새 타격(HitFeedback.Serial이 바뀜)이 났으면 그 종류의 타격 소리들에 세기를 단다
        private void PollHitWeight()
        {
            HitFeedback feel = _session.Feel;
            if (feel.Serial == _feelSerial)
            {
                return;
            }

            _feelSerial = feel.Serial;
            HitFeelLevel level = _session.Data.HitFeel.Get(feel.LastKind);
            foreach (SoundKey key in GetHitKeys(feel.LastKind))
            {
                _weights[key] = level;
            }
        }

        /// <summary>타격 종류 → 그 순간 나는 소리들</summary>
        public static SoundKey[] GetHitKeys(HitKind kind)
        {
            switch (kind)
            {
                case HitKind.PlayerHurt:
                    return new[] { SoundKey.PlayerHurt };
                case HitKind.PlayerDeath:
                    return new[] { SoundKey.PlayerHurt, SoundKey.Death };
                case HitKind.Spell:
                case HitKind.Combo:
                case HitKind.Overflow:
                case HitKind.Finisher:
                    return new[] { SoundKey.SpellImpact };
                case HitKind.BlockChip:
                case HitKind.BlockBreak:
                    return new[] { SoundKey.BlockBreak, SoundKey.HandClick };
                case HitKind.Slam:
                    return new[] { SoundKey.HandSlam };
                default:
                    return new SoundKey[0];
            }
        }

        private bool PollState()
        {
            GameState state = _session.State;
            if (state == _state)
            {
                return false;
            }

            GameState prev = _state;
            _state = state;
            switch (state)
            {
                case GameState.Play:
                    _pendingJingle = null;
                    if (prev == GameState.Ready)
                    {
                        _keys.Add(SoundKey.Start);
                    }

                    break;
                case GameState.Dead:
                    _keys.Add(SoundKey.Death);
                    break;
                case GameState.PhaseClear:
                    QueueJingle(SoundKey.PhaseClear);
                    break;
                case GameState.PhaseIntro:
                    // 다음 페이즈 배너가 뜨는 순간
                    _keys.Add(SoundKey.Warp);
                    break;
                case GameState.Win:
                    QueueJingle(SoundKey.Win);
                    break;
                default:
                    break;
            }

            return true;
        }

        private void QueueJingle(SoundKey key)
        {
            _pendingJingle = key;
            _jingleAt = _session.Time + JINGLE_FALLBACK_TIME;
        }

        // 새로 생긴 FxEvent(참조로 구분)마다 소리. 같은 종류가 한 프레임에 여럿이면(순간이동 출발·도착 등) 한 번만.
        private bool PollFx()
        {
            bool hasWarp = false;
            bool hasMatch = false;
            _curFx.Clear();
            foreach (FxEvent fx in _session.Fx)
            {
                _curFx.Add(fx);
                if (_prevFx.Contains(fx))
                {
                    continue;
                }

                switch (fx.Kind)
                {
                    case FxKind.Deny:
                        AddOnce(SoundKey.Deny);
                        break;
                    case FxKind.Warp:
                        hasWarp = true;
                        AddOnce(SoundKey.Warp);
                        break;
                    case FxKind.Rotate:
                        AddOnce(SoundKey.Rotate);
                        break;
                    case FxKind.Match:
                        hasMatch = true;
                        break;
                    case FxKind.Store:
                        AddOnce(SoundKey.Store);
                        break;
                    case FxKind.Spell:
                        AddOnce(GetSpellKey(fx.Style));
                        break;
                    case FxKind.Click:
                        AddOnce(SoundKey.HandClick);
                        break;
                    case FxKind.Impact:
                        AddOnce(SoundKey.HandSlam);
                        break;
                    case FxKind.Fizzle:
                        AddOnce(SoundKey.BlockBreak);
                        break;
                    default:
                        break;
                }
            }

            (_prevFx, _curFx) = (_curFx, _prevFx);

            // 단어 완성도 Match 연출을 남기므로, 완성한 프레임이 아닐 때만 "제자리 글자가 늘었다"로 본다
            if (hasMatch && _session.Stats.WordsDone == _wordsDone)
            {
                _keys.Add(SoundKey.LetterMatch);
            }

            return hasWarp;
        }

        /// <summary>attackPatterns.json의 spells.fx 값 → 소리 묶음</summary>
        public static SoundKey GetSpellKey(string style)
        {
            switch (style)
            {
                case "fireball":
                case "dragon":
                case "rocket":
                    return SoundKey.SpellFire;
                case "bolt":
                    return SoundKey.SpellBolt;
                case "beam":
                    return SoundKey.SpellBeam;
                case "shift":
                    return SoundKey.SpellShift;
                default:
                    return SoundKey.SpellMagic;
            }
        }

        /// <summary>카운터 증가를 소리로 바꾼다. 플레이어가 맞았으면 true.</summary>
        private bool PollStats()
        {
            GameStats stats = _session.Stats;
            if (stats.WordsDone > _wordsDone)
            {
                _keys.Add(SoundKey.WordComplete);
            }

            if (stats.Combos > _combos)
            {
                _keys.Add(SoundKey.Combo);
            }

            int hits = CountHits(stats);
            bool isHurt = hits > _hits;
            if (isHurt)
            {
                _keys.Add(SoundKey.PlayerHurt);
            }

            _wordsDone = stats.WordsDone;
            _combos = stats.Combos;
            _hits = hits;
            return isHurt;
        }

        private void PollHand()
        {
            HandController hand = _session.Hand;
            if (hand.State == HandState.Warn && _handState != HandState.Warn)
            {
                _keys.Add(SoundKey.HandWarn);
            }

            // 같은 동작을 다시 걸면 이름은 그대로지만 AnimT가 0으로 돌아간다
            bool isAnimRestarted = hand.Anim != _handAnim || hand.AnimT < _handAnimT;
            if (isAnimRestarted)
            {
                if (hand.Anim == HandController.ANIM_HURT)
                {
                    // 마법이 손에 맞는 순간. 마지막 한 방이면 팡파르도 이때 낸다.
                    _keys.Add(SoundKey.SpellImpact);
                    FlushJingle();
                }
                else if (hand.Anim == HandController.ANIM_FLICK_RELEASE)
                {
                    AddOnce(SoundKey.HandClick);
                }
            }

            SnapshotHand();
        }

        private void PollPlayer(bool isNotStep)
        {
            PlayerController player = _session.Player;
            bool hasMoved = player.Board != _playerBoard || player.R != _playerR || player.C != _playerC;
            if (hasMoved && !isNotStep && _session.State == GameState.Play)
            {
                _keys.Add(SoundKey.Move);
            }

            SnapshotPlayer();
        }

        private void PollJingleFallback()
        {
            if (_pendingJingle.HasValue && _session.Time >= _jingleAt)
            {
                FlushJingle();
            }
        }

        private void FlushJingle()
        {
            if (!_pendingJingle.HasValue)
            {
                return;
            }

            _keys.Add(_pendingJingle.Value);
            _pendingJingle = null;
        }

        private void AddOnce(SoundKey key)
        {
            if (!_keys.Contains(key))
            {
                _keys.Add(key);
            }
        }

        private void SnapshotStats()
        {
            _stats = _session.Stats;
            _wordsDone = _stats.WordsDone;
            _combos = _stats.Combos;
            _hits = CountHits(_stats);
        }

        private void SnapshotHand()
        {
            HandController hand = _session.Hand;
            _handState = hand.State;
            _handAnim = hand.Anim;
            _handAnimT = hand.AnimT;
        }

        private void SnapshotPlayer()
        {
            PlayerController player = _session.Player;
            _playerBoard = player.Board;
            _playerR = player.R;
            _playerC = player.C;
        }

        private static int CountHits(GameStats stats)
        {
            int total = 0;
            foreach (int count in stats.Hits.Values)
            {
                total += count;
            }

            return total;
        }
    }
}
