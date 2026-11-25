using System;
using System.Collections.Generic;
using ShiftMagicSpell.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftMagicSpell.Audio
{
    /// <summary>
    /// 게임 소리 담당. 씬의 GameRunner.Session을 매 프레임 관찰(SoundEventWatcher)해서 효과음을 내고,
    /// 첫 화면에서 게임이 시작되면 배경음을 루프로 튼다(WebGL은 사용자 입력 전에는 소리가 막힘). M: 음소거 토글.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        private const int SFX_VOICE_COUNT = 10;
        private const float RUNNER_SEARCH_INTERVAL = 1f;
        private const string MUTE_PREF_KEY = "ShiftMagicSpell.SoundMuted";

        [SerializeField] private SoundLibrary _library;
        [Tooltip("비워 두면 실행 중에 씬에서 찾는다(프리팹이 씬 오브젝트를 참조할 수 없으므로)")]
        [SerializeField] private GameRunner _runner;
        [SerializeField] private AudioSource _musicSource;

        private readonly SoundEventWatcher _watcher = new SoundEventWatcher();
        private readonly Dictionary<SoundKey, float> _lastPlayTimes = new Dictionary<SoundKey, float>();
        private AudioSource[] _sfxSources;
        private int _nextVoice;
        private float _runnerSearchT;
        private bool _isManualBind;

        public static SoundManager Instance { get; private set; }

        /// <summary>소리 재생을 요청할 때마다(클립이 없거나 음소거여도) 불린다. 테스트와 디버그용.</summary>
        public event Action<SoundKey> OnSoundRequested;

        public SoundLibrary Library => _library;
        public bool IsMuted { get; private set; }
        /// <summary>배경음을 틀었는가(오디오가 꺼진 배치 모드에서도 판단할 수 있게 재생 요청 기준)</summary>
        public bool HasMusicStarted { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
            }

            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _sfxSources = new AudioSource[SFX_VOICE_COUNT];
            for (int i = 0; i < SFX_VOICE_COUNT; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _sfxSources[i] = source;
            }

            SetMuted(PlayerPrefs.GetInt(MUTE_PREF_KEY, 0) == 1, false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>관찰할 판을 직접 정한다(테스트용). null이면 다시 씬의 GameRunner를 찾는다.</summary>
        public void Bind(GameSession session)
        {
            _isManualBind = session != null;
            _watcher.Bind(session);
        }

        /// <summary>효과음 하나를 낸다. 같은 이벤트를 MinInterval 안에 다시 부르면 무시한다.</summary>
        public void Play(SoundKey key)
        {
            Play(key, 1f, 1f);
        }

        /// <summary>세기를 바꿔 낸다(타격감: 큰 타격은 낮고 크게, 약한 타격은 작게). 크기는 1을 넘지 않는다.</summary>
        public void Play(SoundKey key, float volumeMul, float pitchMul)
        {
            OnSoundRequested?.Invoke(key);
            if (_library == null || !_library.TryGetEntry(key, out SoundEntry entry))
            {
                return;
            }

            float now = Time.unscaledTime;
            if (entry.MinInterval > 0f && _lastPlayTimes.TryGetValue(key, out float last) && now - last < entry.MinInterval)
            {
                return;
            }

            _lastPlayTimes[key] = now;
            AudioSource source = _sfxSources[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _sfxSources.Length;
            source.clip = entry.Clip;
            source.volume = Mathf.Clamp01(entry.Volume * _library.SfxVolume * volumeMul);
            source.pitch = entry.Pitch * pitchMul * (1f + UnityEngine.Random.Range(-entry.PitchJitter, entry.PitchJitter));
            source.Play();
        }

        public void SetMuted(bool isMuted)
        {
            SetMuted(isMuted, true);
        }

        // GameRunner.Update(로직 진행)가 끝난 뒤에 읽어야 이번 프레임의 변화가 다 보인다
        private void LateUpdate()
        {
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                SetMuted(!IsMuted);
                _watcher.Session?.ShowToast(IsMuted ? "SOUND OFF (M)" : "SOUND ON (M)");
            }

            BindRunnerIfNeeded();
            if (_watcher.Session == null)
            {
                return;
            }

            foreach (SoundKey key in _watcher.Poll())
            {
                _watcher.TryGetWeight(key, out float volume, out float pitch);
                Play(key, volume, pitch);
            }

            if (_watcher.Session.State != GameState.Ready)
            {
                StartMusicIfNeeded();
            }
        }

        private void BindRunnerIfNeeded()
        {
            if (_isManualBind)
            {
                return;
            }

            if (_runner == null)
            {
                _runnerSearchT -= Time.unscaledDeltaTime;
                if (_runnerSearchT > 0f)
                {
                    return;
                }

                _runnerSearchT = RUNNER_SEARCH_INTERVAL;
                _runner = FindAnyObjectByType<GameRunner>();
            }

            GameSession session = _runner != null ? _runner.Session : null;
            if (session != _watcher.Session)
            {
                _watcher.Bind(session);
            }
        }

        private void StartMusicIfNeeded()
        {
            if (HasMusicStarted || _library == null || _library.Music == null)
            {
                return;
            }

            HasMusicStarted = true;
            _musicSource.clip = _library.Music;
            _musicSource.volume = _library.MusicVolume;
            _musicSource.Play();
        }

        private void SetMuted(bool isMuted, bool shouldSave)
        {
            IsMuted = isMuted;
            AudioListener.volume = isMuted ? 0f : 1f;
            if (shouldSave)
            {
                PlayerPrefs.SetInt(MUTE_PREF_KEY, isMuted ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
