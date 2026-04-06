using EventHorizon.Core;
using EventHorizon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EventHorizon.Example
{
    /// <summary>
    /// Example view that displays a health bar driven by a FloatVariableSO.
    /// Demonstrates reactive data binding with zero direct system references.
    /// </summary>
    public class HealthBarView : UIViewBase
    {
        // [Header("Data")]
        // [SerializeField] private FloatVariableSO _health;
        // [SerializeField] private FloatVariableSO _maxHealth;
        //
        // [Header("UI")]
        // [SerializeField] private Slider _healthSlider;
        // [SerializeField] private TMP_Text _healthLabel;

        public override void Bind()
        {
            base.Bind();
            // BindVariable(_health, OnHealthChanged);
            // BindVariable(_maxHealth, OnMaxHealthChanged);
        }

        private void OnHealthChanged(float value)
        {
            UpdateDisplay();
        }

        private void OnMaxHealthChanged(float value)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            // float current = _health != null ? _health.RuntimeValue : 0f;
            // float max = _maxHealth != null ? _maxHealth.RuntimeValue : 1f;
            //
            // if (_healthSlider != null)
            // {
            //     _healthSlider.maxValue = max;
            //     _healthSlider.value = current;
            // }
            //
            // if (_healthLabel != null)
            // {
            //     _healthLabel.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            // }
        }
    }
}
