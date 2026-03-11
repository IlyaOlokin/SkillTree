using Battle;
using UnityEngine;

namespace Visual
{
    public class UnitVisual : MonoBehaviour
    {
        [SerializeField] private Unit unit;
        [SerializeField] private UnitNotificationEffect unitNotificationEffect;
        [SerializeField] private UnitVisualEffectsController effectsController;
        [SerializeField] private UnitVisualHitEffectController hitEffectController;

        void Awake()
        {
            hitEffectController?.Initialize();

            
            unit.health.OnHealthChangedDelta += DisplayHealthChangedNotification;
            unit.OnHit += DisplayHitEffect;
            unit.OnEvade += DisplayEvadeNotification;
            unit.OnBlock += DisplayBlockNotification;
            
        }

        private void OnDestroy()
        {
            if (unit != null)
            {
                unit.OnHit -= DisplayHitEffect;
                unit.OnEvade -= DisplayEvadeNotification;
                unit.OnBlock -= DisplayBlockNotification;
            }

            if (unit != null && unit.health != null)
            {
                unit.health.OnHealthChangedDelta -= DisplayHealthChangedNotification;
            }

            effectsController?.ClearAllEffectIcons();
            hitEffectController?.Dispose();
        }

        private void Update()
        {
            effectsController?.UpdateEffectIcons(unit);
        }

        private void DisplayHealthChangedNotification(float deltaHealth)
        {
            var newEffect = Instantiate(unitNotificationEffect, transform.position, Quaternion.identity);
            if (deltaHealth > 0f)
            {
                newEffect.WriteDamage(Mathf.Abs(deltaHealth));
            }
            else if (deltaHealth < 0f)
            {
                newEffect.WriteHeal(Mathf.Abs(deltaHealth));
            }
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

        private void DisplayHitEffect(DamageInstance damageInstance)
        {
            hitEffectController?.PlayHitEffect(damageInstance);
        }
    }
}
