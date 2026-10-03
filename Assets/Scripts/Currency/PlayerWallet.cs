using System;
using UnityEngine;
using Zenject;

namespace CurrencySystem
{
    public sealed class PlayerWallet : ITickable
    {
        private const float DefaultDisplayAnimationDuration = 0.55f;

        private int _gold;
        private int _displayedGold;
        private int _displayStartGold;
        private int _displayTargetGold;
        private float _displayAnimationDuration;
        private float _displayAnimationTimer;
        private bool _isAnimatingDisplay;

        public int Gold => _gold;
        public int DisplayedGold => _displayedGold;
        public bool IsAnimatingDisplay => _isAnimatingDisplay;

        public event Action<int> OnGoldChanged;
        public event Action<int> OnGoldDisplayChanged;

        public void Tick()
        {
            if (Input.GetKeyDown(KeyCode.Y))
                AddGold(1000);

            if (!_isAnimatingDisplay)
                return;

            _displayAnimationTimer += Time.unscaledDeltaTime;
            float duration = Mathf.Max(0.01f, _displayAnimationDuration);
            float t = Mathf.Clamp01(_displayAnimationTimer / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            _displayedGold = Mathf.RoundToInt(Mathf.Lerp(_displayStartGold, _displayTargetGold, easedT));

            if (t >= 1f)
            {
                _displayedGold = _displayTargetGold;
                _isAnimatingDisplay = false;
            }

            OnGoldDisplayChanged?.Invoke(_displayedGold);
        }

        public void AddGold(int amount, bool animateDisplay = true)
        {
            if (amount <= 0)
                return;

            SetGoldInternal(_gold + amount, animateDisplay);
        }

        public bool TrySpendGold(int amount, bool animateDisplay = true)
        {
            if (amount <= 0)
                return false;

            if (_gold < amount)
                return false;

            SetGoldInternal(_gold - amount, animateDisplay);
            return true;
        }

        public void ApplySaveData(int gold)
        {
            SetGoldInstant(gold, false);
        }

        public void ResetToDefaults()
        {
            SetGoldInstant(0, true);
        }

        public void SetGoldInstant(int gold, bool notify)
        {
            _gold = Mathf.Max(0, gold);
            _displayedGold = _gold;
            _displayStartGold = _gold;
            _displayTargetGold = _gold;
            _displayAnimationTimer = 0f;
            _displayAnimationDuration = 0f;
            _isAnimatingDisplay = false;

            if (!notify)
                return;

            OnGoldChanged?.Invoke(_gold);
            OnGoldDisplayChanged?.Invoke(_displayedGold);
        }

        private void SetGoldInternal(int gold, bool animateDisplay)
        {
            int previousGold = _gold;
            _gold = Mathf.Max(0, gold);
            if (_gold == previousGold)
                return;

            OnGoldChanged?.Invoke(_gold);

            if (!animateDisplay)
            {
                SetGoldInstant(_gold, false);
                OnGoldDisplayChanged?.Invoke(_displayedGold);
                return;
            }

            _displayStartGold = _displayedGold;
            _displayTargetGold = _gold;
            _displayAnimationTimer = 0f;
            _displayAnimationDuration = DefaultDisplayAnimationDuration;
            _isAnimatingDisplay = true;
        }
    }
}
