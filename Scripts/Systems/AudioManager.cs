using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SnakeRescue.Core;
using SnakeRescue.Data;
using SnakeRescue.Utils;

namespace SnakeRescue.Systems
{
    /// <summary>
    /// Manages all audio in the game.
    /// Handles music, SFX, fading, pooling of audio sources.
    ///
    /// Rules:
    /// - Never call AudioSource directly from other scripts
    /// - Always go through AudioManager or GameEvents
    /// - Music fades smoothly between tracks
    /// - SFX uses a pool of AudioSources for overlap support
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────
        public static AudioManager Instance { get; private set; }

        // ─── Inspector ────────────────────────────────────────
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSourceA;
        [SerializeField] private AudioSource _musicSourceB;
        [SerializeField] private AudioSource _ambientSource;

        [Header("SFX Pool")]
        [SerializeField] private int _sfxPoolSize = 8;
        [SerializeField] private Transform _sfxPoolParent;

        [Header("Audio Clips — Music")]
        [SerializeField] private AudioClip _musicMainMenu;
        [SerializeField] private AudioClip _musicGameplay;
        [SerializeField] private AudioClip _musicVictory;
        [SerializeField] private AudioClip _musicFailed;

        [Header("Audio Clips — SFX")]
        [SerializeField] private AudioClip _sfxLevelComplete;
        [SerializeField] private AudioClip _sfxLevelFailed;
        [SerializeField] private AudioClip _sfxSnakeDead;
        [SerializeField] private AudioClip _sfxSnakeHiss;
        [SerializeField] private AudioClip _sfxPrincessScream;
        [SerializeField] private AudioClip _sfxPrincessCheer;
        [SerializeField] private AudioClip _sfxRockFall;
        [SerializeField] private AudioClip _sfxBallRoll;
        [SerializeField] private AudioClip _sfxSplash;
        [SerializeField] private AudioClip _sfxFire;
        [SerializeField] private AudioClip _sfxButtonClick;
        [SerializeField] private AudioClip _sfxStarEarned;
        [SerializeField] private AudioClip _sfxChainReaction;

        // ─── Runtime State ────────────────────────────────────
        private List<AudioSource> _sfxPool    = new List<AudioSource>();
        private AudioSource       _activeMusicSource;
        private AudioSource       _inactiveMusicSource;
        private Coroutine         _fadingCoroutine;
        private bool              _isMuted    = false;

        private float _masterVolume = 1f;
        private float _musicVolume  = 0.7f;
        private float _sfxVolume    = 1f;

        // ─── Clip Registry ────────────────────────────────────
        private Dictionary<string, AudioClip> _clipRegistry;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSFXPool();
            InitializeClipRegistry();
            InitializeMusicSources();
        }

        private void OnEnable()
        {
            GameEvents.OnPlaySFX           += PlaySFX;
            GameEvents.OnPlayMusic         += PlayMusic;
            GameEvents.OnStopMusic         += StopMusic;
            GameEvents.OnGameStateChanged  += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPlaySFX           -= PlaySFX;
            GameEvents.OnPlayMusic         -= PlayMusic;
            GameEvents.OnStopMusic         -= StopMusic;
            GameEvents.OnGameStateChanged  -= OnGameStateChanged;
        }

        // ─── Initialization ───────────────────────────────────

        private void InitializeSFXPool()
        {
            if (_sfxPoolParent == null)
            {
                GameObject poolParent = new GameObject("SFX_Pool");
                poolParent.transform.SetParent(transform);
                _sfxPoolParent = poolParent.transform;
            }

            for (int i = 0; i < _sfxPoolSize; i++)
            {
                GameObject obj = new GameObject($"SFX_Source_{i}");
                obj.transform.SetParent(_sfxPoolParent);

                AudioSource source = obj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f; // 2D audio

                _sfxPool.Add(source);
            }
        }

        private void InitializeClipRegistry()
        {
            _clipRegistry = new Dictionary<string, AudioClip>
            {
                // Music
                { "Music_MainMenu",   _musicMainMenu  },
                { "Music_Gameplay",   _musicGameplay  },
                { "Music_Victory",    _musicVictory   },
                { "Music_Failed",     _musicFailed    },

                // SFX
                { "SFX_LevelComplete",  _sfxLevelComplete  },
                { "SFX_LevelFailed",    _sfxLevelFailed    },
                { "SFX_SnakeDead",      _sfxSnakeDead      },
                { "SFX_SnakeHiss",      _sfxSnakeHiss      },
                { "SFX_PrincessScream", _sfxPrincessScream },
                { "SFX_PrincessCheer",  _sfxPrincessCheer  },
                { "SFX_RockFall",       _sfxRockFall       },
                { "SFX_BallRoll",       _sfxBallRoll       },
                { "SFX_Splash",         _sfxSplash         },
                { "SFX_Fire",           _sfxFire           },
                { "SFX_ButtonClick",    _sfxButtonClick    },
                { "SFX_StarEarned",     _sfxStarEarned     },
                { "SFX_ChainReaction",  _sfxChainReaction  },
            };
        }

        private void InitializeMusicSources()
        {
            if (_musicSourceA == null || _musicSourceB == null)
            {
                Debug.LogError(
                    "[AudioManager] Music sources not assigned in Inspector.");
                return;
            }

            _musicSourceA.loop = true;
            _musicSourceB.loop = true;

            _activeMusicSource   = _musicSourceA;
            _inactiveMusicSource = _musicSourceB;
        }

        // ─── Load Settings ────────────────────────────────────

        public void ApplyVolumeSettings(float master, float music, float sfx)
        {
            _masterVolume = master;
            _musicVolume  = music;
            _sfxVolume    = sfx;

            if (_activeMusicSource != null)
                _activeMusicSource.volume = _masterVolume * _musicVolume;

            if (_ambientSource != null)
                _ambientSource.volume = _masterVolume * _sfxVolume * 0.5f;
        }

        // ─── Music ────────────────────────────────────────────

        public void PlayMusic(string clipName)
        {
            if (!_clipRegistry.TryGetValue(clipName, out AudioClip clip))
            {
                Debug.LogWarning(
                    $"[AudioManager] Music clip not found: {clipName}");
                return;
            }

            if (clip == null) return;

            // Already playing the same clip
            if (_activeMusicSource.clip == clip &&
                _activeMusicSource.isPlaying) return;

            if (_fadingCoroutine != null)
                StopCoroutine(_fadingCoroutine);

            _fadingCoroutine = StartCoroutine(
                CrossFadeMusic(clip));
        }

        public void StopMusic()
        {
            if (_fadingCoroutine != null)
                StopCoroutine(_fadingCoroutine);

            _fadingCoroutine = StartCoroutine(FadeOutMusic());
        }

        private IEnumerator CrossFadeMusic(AudioClip newClip)
        {
            float duration     = Constants.AUDIO_FADE_DURATION;
            float startVolume  = _activeMusicSource.volume;
            float targetVolume = _masterVolume * _musicVolume;
            float elapsed      = 0f;

            // Setup inactive source with new clip
            _inactiveMusicSource.clip   = newClip;
            _inactiveMusicSource.volume = 0f;
            _inactiveMusicSource.Play();

            // Cross-fade
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = elapsed / duration;

                _activeMusicSource.volume   = Mathf.Lerp(startVolume, 0f, t);
                _inactiveMusicSource.volume = Mathf.Lerp(0f, targetVolume,  t);

                yield return null;
            }

            // Swap sources
            _activeMusicSource.Stop();
            _activeMusicSource.clip = null;

            AudioSource temp     = _activeMusicSource;
            _activeMusicSource   = _inactiveMusicSource;
            _inactiveMusicSource = temp;

            _activeMusicSource.volume = targetVolume;
        }

        private IEnumerator FadeOutMusic()
        {
            float duration    = Constants.AUDIO_FADE_DURATION;
            float startVolume = _activeMusicSource.volume;
            float elapsed     = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _activeMusicSource.volume =
                    Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            _activeMusicSource.Stop();
            _activeMusicSource.volume = 0f;
        }

        // ─── SFX ──────────────────────────────────────────────

        public void PlaySFX(string clipName)
        {
            if (_isMuted) return;

            if (!_clipRegistry.TryGetValue(clipName, out AudioClip clip))
            {
                Debug.LogWarning(
                    $"[AudioManager] SFX clip not found: {clipName}");
                return;
            }

            if (clip == null) return;

            AudioSource source = GetAvailableSFXSource();
            if (source == null)
            {
                Debug.LogWarning("[AudioManager] No available SFX source.");
                return;
            }

            source.clip   = clip;
            source.volume = _masterVolume * _sfxVolume;
            source.pitch  = UnityEngine.Random.Range(0.95f, 1.05f);
            source.Play();
        }

        public void PlaySFXAtPosition(string clipName, Vector3 worldPosition)
        {
            if (_isMuted) return;

            if (!_clipRegistry.TryGetValue(clipName, out AudioClip clip))
                return;

            if (clip == null) return;

            AudioSource.PlayClipAtPoint(
                clip,
                worldPosition,
                _masterVolume * _sfxVolume);
        }

        private AudioSource GetAvailableSFXSource()
        {
            foreach (AudioSource source in _sfxPool)
            {
                if (!source.isPlaying) return source;
            }

            // All busy — return the first one (oldest sound)
            return _sfxPool.Count > 0 ? _sfxPool[0] : null;
        }

        // ─── Ambient ──────────────────────────────────────────

        public void PlayAmbient(string clipName)
        {
            if (_ambientSource == null) return;

            if (!_clipRegistry.TryGetValue(clipName, out AudioClip clip))
                return;

            if (clip == null) return;

            _ambientSource.clip   = clip;
            _ambientSource.loop   = true;
            _ambientSource.volume = _masterVolume * _sfxVolume * 0.5f;
            _ambientSource.Play();
        }

        public void StopAmbient()
        {
            _ambientSource?.Stop();
        }

        // ─── Mute ─────────────────────────────────────────────

        public void SetMute(bool muted)
        {
            _isMuted = muted;
            AudioListener.volume = muted ? 0f : 1f;
        }

        public void ToggleMute()
        {
            SetMute(!_isMuted);
        }

        // ─── State Response ───────────────────────────────────

        private void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.MainMenu:
                    PlayMusic("Music_MainMenu");
                    break;

                case GameState.Playing:
                    PlayMusic("Music_Gameplay");
                    break;

                case GameState.LevelComplete:
                    PlayMusic("Music_Victory");
                    PlaySFX("SFX_LevelComplete");
                    break;

                case GameState.LevelFailed:
                    PlayMusic("Music_Failed");
                    PlaySFX("SFX_LevelFailed");
                    break;

                case GameState.Paused:
                    // Slightly lower music while paused
                    if (_activeMusicSource != null)
                        _activeMusicSource.volume =
                            _masterVolume * _musicVolume * 0.4f;
                    break;
            }
        }
    }
}
