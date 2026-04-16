using Battle;
using UnityEngine;
using Zenject;

namespace Visual
{
    public class PlayerTargetIndicatorView : MonoBehaviour
    {
        [SerializeField] private Unit unit;
        [SerializeField] private GameObject indicatorObject;

        [Inject] private AttackResolver _attackResolver;

        private void Awake()
        {
            RefreshIndicator(_attackResolver != null ? _attackResolver.UnitObject : null);
        }

        private void OnEnable()
        {
            if (_attackResolver != null)
            {
                _attackResolver.OnCurrentTargetChanged += RefreshIndicator;
            }

            RefreshIndicator(_attackResolver != null ? _attackResolver.UnitObject : null);
        }

        private void OnDisable()
        {
            if (_attackResolver != null)
            {
                _attackResolver.OnCurrentTargetChanged -= RefreshIndicator;
            }

            SetIndicatorActive(false);
        }

        private void RefreshIndicator(Unit currentTarget)
        {
            SetIndicatorActive(unit != null && currentTarget == unit);
        }

        private void SetIndicatorActive(bool isActive)
        {
            if (indicatorObject != null)
            {
                indicatorObject.SetActive(isActive);
            }
        }
    }
}
