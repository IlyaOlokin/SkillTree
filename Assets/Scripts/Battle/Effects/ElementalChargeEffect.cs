using System.Collections.Generic;
using System.Globalization;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public abstract class ElementalChargeEffect : BaseEffect
    {
        public const float AddedDamagePerStack = 10f;

        private readonly ModifierContainer _attackBonus;
        private Unit _owner;
        private bool _attackPrepared;

        public long Stacks { get; private set; }
        public override bool IsStackable { get; set; } = true;

        protected ElementalChargeEffect(StatType damageStat, int stacks)
        {
            Stacks = Mathf.Max(0, stacks);
            _attackBonus = new ModifierContainer(ModifierType.Added, damageStat, 0f);
        }

        public override bool CanApply(Unit owner)
        {
            return owner != null && Stacks > 0;
        }

        public override void OnApply(Unit unit)
        {
            _owner = unit;
            unit.OnAttackPrepared += PrepareAttack;
            unit.OnAttackCompleted += CompleteAttack;
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            if (newEffect is ElementalChargeEffect charges && charges.GetType() == GetType())
            {
                Stacks += charges.Stacks;
            }
        }

        public override bool IsReadyToBeRemoved(Unit unit) => Stacks <= 0;

        public override void OnRemove(Unit unit)
        {
            unit.OnAttackPrepared -= PrepareAttack;
            unit.OnAttackCompleted -= CompleteAttack;
            _attackPrepared = false;
            _owner = null;
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return Stacks.ToString(CultureInfo.InvariantCulture);
        }

        private void PrepareAttack(DamageInfo damageInfo)
        {
            if (_owner == null || Stacks <= 0 || damageInfo?.BaseUnitModifiers == null) return;

            // Change the attack snapshot only, avoiding deferred unit-stat recalculation.
            _attackBonus.value = AddedDamagePerStack * Stacks;
            damageInfo.BaseUnitModifiers.ChangeModifierValue(_attackBonus);
            _attackPrepared = true;
        }

        private void CompleteAttack(ITarget target)
        {
            // A buff first granted during this attack waits for the next prepared attack.
            if (!_attackPrepared || _owner == null) return;
            _attackPrepared = false;
            Stacks--;
            if (Stacks > 0) return;

            EffectController controller = _owner.effectController;
            ActiveEffect active = controller.Effects.Find(e => ReferenceEquals(e.Effect, this));
            controller.RemoveEffect(active);
        }
    }
}
