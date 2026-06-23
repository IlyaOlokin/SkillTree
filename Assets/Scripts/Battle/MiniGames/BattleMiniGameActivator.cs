using System;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.MiniGames
{
    public sealed class BattleMiniGameActivator : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image timerFill;

        private BattleMiniGameController _controller;
        private BattleMiniGameEventDefinition _definition;
        private float _timeLeft;
        private float _duration;
        private bool _bound;

        public BattleMiniGameEventDefinition Definition => _definition;

        public event Action<BattleMiniGameActivator> Expired;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        public void Bind(
            BattleMiniGameController controller,
            BattleMiniGameEventDefinition definition,
            float activationTime)
        {
            _controller = controller;
            _definition = definition;
            _duration = Mathf.Max(0.01f, activationTime);
            _timeLeft = _duration;
            _bound = true;

            UpdateTimerFill();
        }

        public void Tick(float deltaTime, bool isPaused)
        {
            if (!_bound || isPaused)
            {
                return;
            }

            _timeLeft -= Mathf.Max(0f, deltaTime);
            UpdateTimerFill();

            if (_timeLeft <= 0f)
            {
                _bound = false;
                Expired?.Invoke(this);
            }
        }

        private void HandleClick()
        {
            _controller?.TryActivate(this);
        }

        private void UpdateTimerFill()
        {
            if (timerFill != null)
            {
                timerFill.fillAmount = Mathf.Clamp01(_timeLeft / _duration);
            }
        }
    }
}
