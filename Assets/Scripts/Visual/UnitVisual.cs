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
            unit.OnEvade += DisplayEvadeNotification;
            unit.OnBlock += DisplayBlockNotification;
        }

        private void OnDestroy()
        {
            if (unit != null && unit.health != null)
            {
                unit.health.OnHealthChangedDelta -= DisplayHealthChangedNotification;
                unit.OnEvade -= DisplayEvadeNotification;
                unit.OnBlock -= DisplayBlockNotification;
            }
        }

        private void DisplayHealthChangedNotification(float deltaHealth)
        {
            var newEffect = Instantiate(unitNotificationEffect, transform.position, Quaternion.identity);
            
            if (deltaHealth > 0) newEffect.WriteDamage(Mathf.Abs(deltaHealth));
            else if (deltaHealth < 0) newEffect.WriteHeal(Mathf.Abs(deltaHealth));
        }
        
        private void DisplayEvadeNotification()
        {
            var newEffect = Instantiate(unitNotificationEffect, transform.position, Quaternion.identity);
            
            newEffect.WriteMessage("Evade");
        }
        
        private void DisplayBlockNotification()
        {
            var newEffect = Instantiate(unitNotificationEffect, transform.position, Quaternion.identity);
            
            newEffect.WriteMessage("Block");
        }
    }
}

