using UnityEngine;
using Sirenix.OdinInspector;
using System;

namespace TecVooDoo.DontLoseYourHead.Core
{
    [CreateAssetMenu(fileName = "NewIntVariable", menuName = "DLYH/Variables/Int Variable")]
    public class IntVariableSO : ScriptableObject
    {
        [Title("Configuration")]
        [SerializeField] private int _initialValue;

        [Title("Runtime Value")]
        [ReadOnly]
        [ShowInInspector]
        private int _runtimeValue;

        public int Value
        {
            get => _runtimeValue;
            set
            {
                if (_runtimeValue != value)
                {
                    _runtimeValue = value;
                    OnValueChanged?.Invoke(_runtimeValue);
                }
            }
        }

        public event Action<int> OnValueChanged;

        private void OnEnable()
        {
            _runtimeValue = _initialValue;
        }

        public void ResetToInitial()
        {
            Value = _initialValue;
        }

        public void Add(int amount)
        {
            Value += amount;
        }

        public void Subtract(int amount)
        {
            Value -= amount;
        }
    }
}
