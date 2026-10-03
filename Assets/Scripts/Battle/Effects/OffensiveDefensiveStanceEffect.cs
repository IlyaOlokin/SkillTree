using System.Collections.Generic;
using LocalizationSupport;
using SkillTree;

namespace Battle
{
    public class OffensiveDefensiveStanceEffect : BaseEffect
    {
        public OffensiveDefensiveStanceModifier Source { get; }
        public bool IsDefensive { get; private set; }
        private int _count;
        private float _damageBonus;
        private float _defenceBonus;
        private Unit _owner;
        public override bool IsStackable { get; set; } = false;
        public override bool CanDisplayMultipleIcons => true;
        public override EffectVisualType VisualType => IsDefensive ? EffectVisualType.DefensiveStance : EffectVisualType.OffensiveStance;
        private int Threshold => IsDefensive ? Source.HitsToOffence : Source.AttacksToDefence;

        public OffensiveDefensiveStanceEffect(OffensiveDefensiveStanceModifier source) { Source = source; }
        public void SetBonuses(float damage, float defence) { _damageBonus = damage; _defenceBonus = defence; }
        public override void OnApply(Unit unit)
        {
            _owner = unit;
            unit.OnAttackCompleted += AttackCompleted;
            unit.OnGettingHit += GettingHit;
        }
        public override void OnRemove(Unit unit)
        {
            unit.OnAttackCompleted -= AttackCompleted;
            unit.OnGettingHit -= GettingHit;
            _owner = null;
        }
        public override bool IsReadyToBeRemoved(Unit unit)
        {
            foreach (var modifier in unit.GetAllModifiers())
                if (ReferenceEquals(modifier.Modifier, Source)) return false;
            return true;
        }
        private void AttackCompleted(ITarget target) { if (!IsDefensive) Advance(); }
        private void GettingHit(DamageInfo damage) { if (IsDefensive) Advance(); }
        private void Advance()
        {
            if (++_count < Threshold) return;
            _count = 0;
            IsDefensive = !IsDefensive;
            // Keep the current attack's persistent stat buckets intact.
            var owner = _owner;
            AttackProcessor.RunAfterCurrentAttack(() => { if (_owner == owner && owner != null) owner.RequestModRecalculation(); });
        }
        public override string GetIconText(IReadOnlyList<ActiveEffect> effects) => $"{_count}/{Threshold}";
        public override float GetIconTimerProgress(IReadOnlyList<ActiveEffect> effects) => 1f;
        protected override string GetDescriptionId() => IsDefensive ? "defensiveStance" : "offensiveStance";
        public override IReadOnlyList<string> GetTooltipDescriptions() => new[]
        {
            GameLocalization.GetContent($"effect.{GetDescriptionId()}.name", IsDefensive ? "Defence" : "Offence"),
            IsDefensive
                ? GameLocalization.FormatContent("effect.defensiveStance.description", "[[0]]% increased Armor and Evasion. Incoming attacks: [[1]]/[[2]]. Evades do not count; blocked and fully absorbed attacks count. At [[2]], return to Offence.", _defenceBonus * 100f, _count, Threshold)
                : GameLocalization.FormatContent("effect.offensiveStance.description", "[[0]]% increased Damage. Completed attacks: [[1]]/[[2]], including misses. At [[2]], switch to Defence.", _damageBonus * 100f, _count, Threshold)
        };
    }
}
