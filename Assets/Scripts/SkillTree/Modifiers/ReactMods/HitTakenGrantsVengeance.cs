using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Hit Taken Grants Vengeance", fileName = "New HitTakenGrantsVengeance")]
    public class HitTakenGrantsVengeance : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float attackProgressLoss = 0.3f;

        public override bool IsInPriority(ModifierPriority priority) => priority == ModifierPriority.PreAttribute;

        public override void ApplyEffect(Unit unit)
        {
            // Preserve the fixed penalty while combining it with every other hit-taken contribution.
            unit.BaseUnitModifiers.ChangeModifierValue(new ModifierContainer(
                ModifierType.Added, StatType.AttackProgressOnHitTaken, -Mathf.Clamp01(attackProgressLoss)));
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            EffectController effectController = unit.effectController;
            if (effectController == null)
            {
                return null;
            }

            void HandleGettingHit(DamageInfo _)
            {
                effectController.AddEffect(() => new Vengeance(1));
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnGettingHit += HandleGettingHit,
                () => unit.OnGettingHit -= HandleGettingHit);
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.hitTakenGrantsVengeance.description",
                "When you are hit: lose [[0]]% Attack Progress and gain {vengeance|Vengeance}.",
                attackProgressLoss * 100f);
        }
    }
}
