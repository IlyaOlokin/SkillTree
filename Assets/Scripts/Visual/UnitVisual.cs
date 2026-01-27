using Battle;
using UnityEngine;

namespace Visual
{
    public class UnitVisual : MonoBehaviour
    {
        [SerializeField] private Unit unit;
        [SerializeField] private UnitNotificationEffect unitNotificationEffect;
        void Awake()
        {
            unit.health.OnHealthChangedDelta += DisplayHealthChangedNotification;
        }

        private void DisplayHealthChangedNotification(float deltaHealth)
        {
            var newEffect = Instantiate(unitNotificationEffect, transform.position, Quaternion.identity);
            newEffect.WriteDamage(Mathf.Abs(deltaHealth));
        }
    }
}

