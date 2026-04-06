using EventHorizon.Core;
using EventHorizon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EventHorizon.Example
{
    /// <summary>
    /// Worked example: a settings panel driven entirely by VariableSO bindings.
    /// Data flows IN through variables, user actions flow OUT through SetValue.
    /// Zero direct references to SoundModuleSO or any other system.
    /// </summary>
    public class SettingsView : UIViewBase
    {
        [Header("Data")]
        [SerializeField] private BoolVariableSO _musicEnabled;
        [SerializeField] private BoolVariableSO _sfxEnabled;
        [SerializeField] private BoolVariableSO _hapticsEnabled;
        [SerializeField] private StringVariableSO _colorTheme;

        [Header("UI")]
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private Toggle _hapticsToggle;
        [SerializeField] private TMP_Dropdown _themeDropdown;

        public override void Bind()
        {
            base.Bind();
            BindVariable(_musicEnabled, OnMusicChanged);
            BindVariable(_sfxEnabled, OnSFXChanged);
            BindVariable(_hapticsEnabled, OnHapticsChanged);
            BindVariable(_colorTheme, OnThemeChanged);

            if (_musicToggle != null)
                _musicToggle.onValueChanged.AddListener(OnMusicToggled);

            if (_sfxToggle != null)
                _sfxToggle.onValueChanged.AddListener(OnSFXToggled);

            if (_hapticsToggle != null)
                _hapticsToggle.onValueChanged.AddListener(OnHapticsToggled);

            if (_themeDropdown != null)
                _themeDropdown.onValueChanged.AddListener(OnThemeSelected);
        }

        public override void Unbind()
        {
            if (_musicToggle != null)
                _musicToggle.onValueChanged.RemoveListener(OnMusicToggled);

            if (_sfxToggle != null)
                _sfxToggle.onValueChanged.RemoveListener(OnSFXToggled);

            if (_hapticsToggle != null)
                _hapticsToggle.onValueChanged.RemoveListener(OnHapticsToggled);

            if (_themeDropdown != null)
                _themeDropdown.onValueChanged.RemoveListener(OnThemeSelected);
        }

        private void OnMusicChanged(bool value)
        {
            if (_musicToggle != null)
                _musicToggle.SetIsOnWithoutNotify(value);
        }

        private void OnSFXChanged(bool value)
        {
            if (_sfxToggle != null)
                _sfxToggle.SetIsOnWithoutNotify(value);
        }

        private void OnHapticsChanged(bool value)
        {
            if (_hapticsToggle != null)
                _hapticsToggle.SetIsOnWithoutNotify(value);
        }

        private void OnThemeChanged(string value)
        {
            if (_themeDropdown == null) return;

            for (int i = 0; i < _themeDropdown.options.Count; i++)
            {
                if (_themeDropdown.options[i].text == value)
                {
                    _themeDropdown.SetValueWithoutNotify(i);
                    return;
                }
            }
        }

        private void OnMusicToggled(bool value)
        {
            if (_musicEnabled != null)
                _musicEnabled.SetValue(value);
        }

        private void OnSFXToggled(bool value)
        {
            if (_sfxEnabled != null)
                _sfxEnabled.SetValue(value);
        }

        private void OnHapticsToggled(bool value)
        {
            if (_hapticsEnabled != null)
                _hapticsEnabled.SetValue(value);
        }

        private void OnThemeSelected(int index)
        {
            if (_colorTheme != null && _themeDropdown != null && index < _themeDropdown.options.Count)
            {
                _colorTheme.SetValue(_themeDropdown.options[index].text);
            }
        }
    }
}
