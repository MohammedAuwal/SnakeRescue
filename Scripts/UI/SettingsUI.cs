using UnityEngine;
using UnityEngine.UI;
using SnakeRescue.Systems;
using SnakeRescue.Data;
using SnakeRescue.Core;

namespace SnakeRescue.UI
{
    /// <summary>
    /// Controls the Settings Panel.
    ///
    /// Sliders:
    /// - Master Volume
    /// - Music Volume
    /// - SFX Volume
    ///
    /// Toggles:
    /// - Vibration
    /// - Notifications (placeholder)
    ///
    /// Saves changes immediately to PlayerData.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        // ─── References ───────────────────────────────────────
        [Header("Sliders")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Toggles")]
        [SerializeField] private Toggle _vibrationToggle;
        [SerializeField] private Toggle _notificationsToggle;

        [Header("Buttons")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _resetButton;

        // ─── State ────────────────────────────────────────────
        private bool _isUpdating = false;

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            SetupListeners();
            LoadSettings();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        // ─── Setup ────────────────────────────────────────────

        private void SetupListeners()
        {
            _masterSlider?.onValueChanged.AddListener(OnMasterChanged);
            _musicSlider?.onValueChanged.AddListener(OnMusicChanged);
            _sfxSlider?.onValueChanged.AddListener(OnSFXChanged);

            _vibrationToggle?.onValueChanged.AddListener(OnVibrationChanged);
            _notificationsToggle?.onValueChanged.AddListener(OnNotificationsChanged);

            _closeButton?.onClick.AddListener(CloseSettings);
            _resetButton?.onClick.AddListener(ResetSettings);
        }

        // ─── Load Settings ────────────────────────────────────

        private void LoadSettings()
        {
            _isUpdating = true;

            PlayerData data = SaveSystem.Instance?.CurrentData;

            if (data != null)
            {
                if (_masterSlider != null) _masterSlider.value = data.MasterVolume;
                if (_musicSlider != null)  _musicSlider.value  = data.MusicVolume;
                if (_sfxSlider != null)    _sfxSlider.value    = data.SFXVolume;
                if (_vibrationToggle != null) _vibrationToggle.isOn  = data.Vibration;
                if (_notificationsToggle != null) _notificationsToggle.isOn = data.Notifications;
            }
            else
            {
                // Defaults
                if (_masterSlider != null) _masterSlider.value = 1.0f;
                if (_musicSlider != null)  _musicSlider.value  = 0.7f;
                if (_sfxSlider != null)    _sfxSlider.value    = 1.0f;
                if (_vibrationToggle != null) _vibrationToggle.isOn  = true;
            }

            _isUpdating = false;
        }

        // ─── Value Changes ────────────────────────────────────

        private void OnMasterChanged(float value)
        {
            if (_isUpdating) return;
            SaveSettings();
            UpdateAudioManager();
        }

        private void OnMusicChanged(float value)
        {
            if (_isUpdating) return;
            SaveSettings();
            UpdateAudioManager();
        }

        private void OnSFXChanged(float value)
        {
            if (_isUpdating) return;
            SaveSettings();
            UpdateAudioManager();
        }

        private void OnVibrationChanged(bool value)
        {
            if (_isUpdating) return;
            SaveSettings();
        }

        private void OnNotificationsChanged(bool value)
        {
            if (_isUpdating) return;
            SaveSettings();
        }

        // ─── Save & Apply ─────────────────────────────────────

        private void SaveSettings()
        {
            if (SaveSystem.Instance == null) return;

            SaveSystem.Instance.UpdateSettings(
                _masterSlider?.value ?? 1f,
                _musicSlider?.value  ?? 0.7f,
                _sfxSlider?.value    ?? 1f,
                _vibrationToggle?.isOn ?? true
            );
        }

        private void UpdateAudioManager()
        {
            AudioManager.Instance?.ApplyVolumeSettings(
                _masterSlider?.value ?? 1f,
                _musicSlider?.value  ?? 0.7f,
                _sfxSlider?.value    ?? 1f
            );
        }

        // ─── Buttons ──────────────────────────────────────────

        private void CloseSettings()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
            gameObject.SetActive(false);
        }

        private void ResetSettings()
        {
            GameEvents.TriggerPlaySFX("SFX_ButtonClick");
            _isUpdating = true;

            if (_masterSlider != null) _masterSlider.value = 1.0f;
            if (_musicSlider != null)  _musicSlider.value  = 0.7f;
            if (_sfxSlider != null)    _sfxSlider.value    = 1.0f;
            if (_vibrationToggle != null) _vibrationToggle.isOn  = true;

            _isUpdating = false;
            SaveSettings();
            UpdateAudioManager();
        }

        // ─── Cleanup ──────────────────────────────────────────

        private void OnDestroy()
        {
            _masterSlider?.onValueChanged.RemoveListener(OnMasterChanged);
            _musicSlider?.onValueChanged.RemoveListener(OnMusicChanged);
            _sfxSlider?.onValueChanged.RemoveListener(OnSFXChanged);
        }
    }
}
