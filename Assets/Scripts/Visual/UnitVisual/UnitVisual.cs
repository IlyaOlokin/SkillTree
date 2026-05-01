using Battle;
using LocalizationSupport;
using TooltipSystem;
using UnityEngine;
using Zenject;

namespace Visual
{
    public class UnitVisual : MonoBehaviour
    {
        [Inject] private TooltipUI _tooltipUI;
        [SerializeField] private Unit unit;
        [SerializeField] private UnitNotificationEffect unitNotificationEffect;
        [SerializeField] private UnitVisualEffectsController effectsController;
        [SerializeField] private UnitVisualHitEffectController hitEffectController;
        [SerializeField] private UnitVisualAttackAnimationController attackAnimationController = new UnitVisualAttackAnimationController();

        void Awake()
        {
            AssignBattleCameraToWorldCanvases();
            attackAnimationController?.Initialize(transform, gameObject);
            effectsController?.Initialize(_tooltipUI);
            hitEffectController?.Initialize();

            
            unit.health.OnHealthChangedDelta += DisplayHealthChangedNotification;
            unit.OnGettingHit += DisplayGettingHitEffect;
            unit.OnAttack += DisplayAttackAnimation;
            unit.OnEvade += DisplayEvadeNotification;
            unit.OnBlock += DisplayBlockNotification;
            
        }

        private void OnDestroy()
        {
            if (unit != null)
            {
                unit.OnGettingHit -= DisplayGettingHitEffect;
                unit.OnAttack -= DisplayAttackAnimation;
                unit.OnEvade -= DisplayEvadeNotification;
                unit.OnBlock -= DisplayBlockNotification;
            }

            if (unit != null && unit.health != null)
            {
                unit.health.OnHealthChangedDelta -= DisplayHealthChangedNotification;
            }

            effectsController?.ClearAllEffectIcons();
            hitEffectController?.Dispose();
            attackAnimationController?.Dispose();
        }

        private void OnDisable()
        {
            attackAnimationController?.Dispose();
        }

        private void Update()
        {
            effectsController?.UpdateEffectIcons(unit);
        }

        private void DisplayHealthChangedNotification(float deltaHealth)
        {
            if (deltaHealth == 0f) return;
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
            newEffect.WriteMessage(GameLocalization.Get("combat.notification.evade", "Evade"));
        }

        private void DisplayBlockNotification()
        {
            var newEffect = Instantiate(unitNotificationEffect, transform.position, Quaternion.identity);
            newEffect.WriteMessage(GameLocalization.Get("combat.notification.block", "Block"));
        }

        private void DisplayGettingHitEffect(DamageInfo damageInfo)
        {
            hitEffectController?.PlayHitEffect(damageInfo);
        }

        private void DisplayAttackAnimation(ITarget target)
        {
            attackAnimationController?.PlayAttackAnimation(target);
        }

        public void PlayAttackAnimation(Vector2 direction)
        {
            attackAnimationController?.PlayAttackAnimation(direction);
        }

        private void AssignBattleCameraToWorldCanvases()
        {
            GameObject battleCameraObject = GameObject.FindWithTag("BattleCamera");
            if (battleCameraObject == null)
            {
                return;
            }

            Camera battleCamera = battleCameraObject.GetComponent<Camera>();
            if (battleCamera == null)
            {
                return;
            }

            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas.renderMode != RenderMode.WorldSpace)
                {
                    continue;
                }

                canvas.worldCamera = battleCamera;
            }
        }
    }
}
