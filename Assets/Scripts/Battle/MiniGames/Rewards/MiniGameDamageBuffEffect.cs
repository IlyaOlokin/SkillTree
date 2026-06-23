using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle.MiniGames
{
    public sealed class MiniGameDamageBuffEffect : BaseEffect
    {
        private readonly float _moreDamage;
        private BaseModifier _modifier;

        public override bool IsStackable { get; set; } = false;

        public MiniGameDamageBuffEffect(float duration, float moreDamage)
        {
            Duration = Mathf.Max(0f, duration);
            _moreDamage = Mathf.Max(0f, moreDamage);
        }

        public override void OnApply(Unit unit)
        {
            if (unit == null || _moreDamage <= 0f)
            {
                return;
            }

            _modifier = ScriptableObject.CreateInstance<BaseModifier>();
            _modifier.modifierContainer = new ModifierContainer(ModifierType.More, StatType.Damage, _moreDamage);
            unit.AddOuterModifier(_modifier);
        }

        public override void OnRemove(Unit unit)
        {
            if (unit == null || _modifier == null)
            {
                return;
            }

            unit.RemoveOuterModifier(_modifier);
            _modifier = null;
        }

        protected override string GetDescriptionId()
        {
            return "miniGameDamageBuff";
        }
    }
}
