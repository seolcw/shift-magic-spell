using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftMagicSpell.Data
{
    /// <summary>
    /// 웹 프로토타입 attackPatterns.json을 그대로 옮긴 수치 묶음. 인스펙터에서 플레이 중에도 조절할 수 있다.
    /// 거리·속도 단위는 웹과 같다(칸 = 50px). 코드에서 칸 단위로 바꿔 쓴다.
    /// </summary>
    [CreateAssetMenu(fileName = "AttackPatterns", menuName = "Shift Magic Spell/Attack Patterns")]
    public class AttackPatternsData : ScriptableObject
    {
        public ConfigValues Config = new ConfigValues();
        public List<SliderDef> Sliders = new List<SliderDef>();
        public List<PhaseDef> Phases = new List<PhaseDef>();
        public string FinisherWord = "SHIFT";
        public List<SpellDef> Spells = new List<SpellDef>();
        public List<AttackDef> Attacks = new List<AttackDef>();

        [Tooltip("타격감: 멈춤·흔들림·번쩍임·넉백·조각·소리 세기 (Unity에서 추가, 웹 JSON에 없음)")]
        public HitFeelValues HitFeel = new HitFeelValues();

        private Dictionary<string, SpellDef> _spellDict;
        private Dictionary<string, AttackDef> _attackDict;

        public SpellDef GetSpellOrNull(string word)
        {
            if (_spellDict == null)
            {
                _spellDict = new Dictionary<string, SpellDef>();
                foreach (SpellDef spell in Spells)
                {
                    _spellDict[spell.Word] = spell;
                }
            }

            return word != null && _spellDict.TryGetValue(word, out SpellDef found) ? found : null;
        }

        public AttackDef GetAttackOrNull(string id)
        {
            if (_attackDict == null)
            {
                _attackDict = new Dictionary<string, AttackDef>();
                foreach (AttackDef attack in Attacks)
                {
                    _attackDict[attack.Id] = attack;
                }
            }

            return id != null && _attackDict.TryGetValue(id, out AttackDef found) ? found : null;
        }

        private void OnValidate()
        {
            // 목록에 항목을 더하거나 이름을 바꾸면 찾기 표를 다시 만든다
            _spellDict = null;
            _attackDict = null;
        }
    }

    [Serializable]
    public class ConfigValues
    {
        public float WarnTime = 0.7f;
        public float HandSpeed = 560f;
        public float AttackInterval = 2.4f;
        public float MoveRepeat = 0.15f;
        public float RotateRepeat = 0.24f;
        public float DominoStep = 0.035f;
        public int PlayerHp = 5;
        public float HitInvuln = 1f;
        public float ShiftCooldown = 2f;
        public float ShiftInvuln = 0.5f;
        public float SpellStun = 0.8f;
        public float SpellTime = 0.9f;
        public int FinisherHp = 5;
        public float DeathHandRecover = 0.5f;
        public float Reaction = 0.25f;
        public float RotateSlide = 0.24f;
        public float HintInterval = 4f;
        public float HintDuration = 1f;
        public float OverflowMul = 0.8f;
        public float ComboWindow = 1.5f;
        public float ComboBonus = 0.5f;
        public int BlockHp = 2;
        public float StoreTime = 0.5f;
        public float BlockAggro = 0.15f;

        // Unity에서 추가(웹 JSON에 없음): 연타해도 이 간격보다 빨리 움직이거나 돌리지 않는다
        public float MoveDelay = 0.1f;
        public float RotateDelay = 0.2f;

        // Unity에서 추가(웹 JSON에 없음): 페이즈 전환 연출
        [Tooltip("손 HP가 0이 된 뒤 손이 흔들리다 물러나는 시간(초). 이 동안 공격 없음, 플레이어 무적")]
        public float PhaseClearTime = 1.5f;
        [Tooltip("다음 페이즈 배너를 보여 주는 시간(초). 새 판은 이때 보인다")]
        public float PhaseIntroTime = 2f;
        [Tooltip("배너가 사라진 뒤 손이 첫 공격을 고르기까지(초)")]
        public float PhaseFirstAttackDelay = 1f;
        [Tooltip("마지막 페이즈에서 SHIFT로 바뀔 때 FINAL 배너 시간(초). 이 동안 손은 공격하지 않음")]
        public float FinisherIntroTime = 1.5f;
        [Tooltip("게임 시작·사망 후 재시작 때 손이 첫 공격을 고르기까지(초)")]
        public float StartFirstAttackDelay = 3f;
    }

    /// <summary>웹 디버그 패널 슬라이더 정의. Unity에서는 인스펙터로 조절하므로 범위 참고용으로 남긴다.</summary>
    [Serializable]
    public class SliderDef
    {
        public string Key;
        public string Label;
        public float Min;
        public float Max;
        public float Step;
    }

    [Serializable]
    public class PhaseDef
    {
        public List<string> Words = new List<string>();
        public List<string> Attacks = new List<string>();
        public int HandHp = 20;
        public float SpeedMul = 1f;
        public float IntervalMul = 1f;
        public float WarnMul = 1f;

        // Unity에서 추가(웹 JSON에 없음)
        [Tooltip("손이 플레이어가 2글자 이상 맞춘 줄을 가로지르는 줄 섞기에 가산점을 주는가(페이즈 1은 끔: 처음 하는 사람이 버티게)")]
        public bool IsTargetingProgressLine = true;
    }

    [Serializable]
    public class SpellDef
    {
        public string Word;
        public string Ko;
        public string Fx;
        public Color Color = Color.white;
        public float Size = 10f;
        public int Count = 1;
        public string Icon;
    }

    /// <summary>
    /// 공격 하나의 수치. 공격마다 쓰는 항목이 달라서, JSON에 없는 항목은 기본값으로 둔다.
    /// 페이즈별 값(ChainMin, ChainMax, DoubleChance)은 0번 = 1페이즈.
    /// </summary>
    [Serializable]
    public class AttackDef
    {
        public string Id;
        public string Name;
        public string Kind;
        public float Base;
        public float Cooldown;
        public float WarnMul = 1f;
        public int ShiftMin = 1;
        public int ShiftMax = 1;
        public float BulletSpeed;
        public float ChargeSpeed;
        public int[] ChainMin = new int[0];
        public int[] ChainMax = new int[0];
        public float ChainWarnMul = 1f;
        public float TrackTime;
        public float FollowSpeed;
        public float LockMul = 1f;
        public float[] DoubleChance = new float[0];
        public float DoubleGap;

        public static int GetForPhaseOrDefault(int[] values, int phase, int fallback)
        {
            return values != null && phase >= 0 && phase < values.Length ? values[phase] : fallback;
        }

        public static float GetForPhaseOrDefault(float[] values, int phase, float fallback)
        {
            return values != null && phase >= 0 && phase < values.Length ? values[phase] : fallback;
        }
    }
}
