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
            unit.health.OnHealthChange += DisplayHealthChangeNotification;
        }

        private void DisplayHealthChangeNotification(float deltaHealth)
        {
            var newEffect = Instantiate(unitNotificationEffect, transform.position, Quaternion.identity);
            newEffect.WriteDamage(Mathf.Abs(deltaHealth));
        }
    }
}

