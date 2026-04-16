using System;
using System.Collections.Generic;
using System.Text;
using Battle;
using LocalizationSupport;
using TooltipSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace SkillTree
{
    public abstract class Modifier : ScriptableObject
    {
        [field: SerializeField]
        public List<ModifierPriority> Priorities { get; private set; } = new List<ModifierPriority>()
        {
            ModifierPriority.PreAttribute
        };

        public virtual void OnCollected(){}
        public virtual void OnReset(){}
        public virtual IModifierRuntimeBinding CreateRuntimeBinding(Unit unit) => null;
        
        public virtual bool IsApplicable(Unit unit) => true;
        public virtual bool IsInPriority(ModifierPriority priority) => Priorities.Contains(priority);

        public virtual void ApplyEffect(Unit unit) { }
        // Attack modifiers must mutate only damageInfo.BaseUnitModifiers snapshot.
        // Do not mutate damageInfo.Owner.BaseUnitModifiers here.
        public virtual void ApplyEffect(DamageInfo damageInfo) { }

        public virtual string GetDescription()
        {
            return GameLocalization.Get("modifier.emptyDescription", "Empty description");
        }

        public void SetPriorities(List<ModifierPriority> priorities)
        {
            Priorities = priorities;
        }
    }

    [Serializable]
    public class ModifierContainer
    {
        public ModifierType modifierType;
        public StatType statType;
        public float value;

        public ModifierContainer(ModifierType modifierType, StatType statType, float value)
        {
            this.modifierType = modifierType;
            this.statType = statType;
            this.value = value;
        }

        public static ModifierContainer operator *(ModifierContainer src, float value)
        {
            return new ModifierContainer(
                src.modifierType,
                src.statType,
                src.value * value);
        }

        public string GetDescription()
        {
            var forcePercentForAdded = StatTypeDisplayRules.IsPercentStat(statType);
            string formattedStatName = StatTypeTooltipFormatter.Format(statType);

            switch (modifierType)
            {
                case ModifierType.Added:
                    string addedValue = $"+{(forcePercentForAdded ? value * 100f : value)}{(forcePercentForAdded ? "%" : string.Empty)}";
                    return GameLocalization.Format(
                        "modifier.container.added",
                        "[[0]] to [[1]]",
                        addedValue,
                        formattedStatName.Replace("Added", string.Empty));
                case ModifierType.Increased:
                    return GameLocalization.Format(
                        value < 0 ? "modifier.container.decreased" : "modifier.container.increased",
                        value < 0 ? "[[0]]% Decreased [[1]]" : "[[0]]% Increased [[1]]",
                        Mathf.Abs(value * 100f),
                        formattedStatName);
                case ModifierType.More:
                    return GameLocalization.Format(
                        value < 0 ? "modifier.container.less" : "modifier.container.more",
                        value < 0 ? "[[0]]% Less [[1]]" : "[[0]]% More [[1]]",
                        Mathf.Abs(value * 100f),
                        formattedStatName);
            }

            return string.Empty;
        }
    }
}
