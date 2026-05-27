using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Hit Taken Grants Vengeance", fileName = "New HitTakenGrantsVengeance")]
    public class HitTakenGrantsVengeance : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float attackProgressLoss = 0.3f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            EffectController effectController = unit.effectController;
            Attacker attacker = unit.attacker;
            if (effectController == null && attacker == null)
            {
                return null;
            }

            float clampedAttackProgressLoss = Mathf.Clamp01(attackProgressLoss);

            void HandleGettingHit(DamageInfo _)
            {
                attacker?.ModifyAttackProgress(-clampedAttackProgressLoss);
                effectController?.AddEffect(() => new Vengeance(1));
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
