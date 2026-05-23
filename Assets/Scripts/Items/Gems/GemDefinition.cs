using System;
using System.Collections.Generic;
using Items;
using SkillTree;
using UnityEngine;

namespace Gems
{
    [CreateAssetMenu(menuName = "Gems/Gem Definition", fileName = "NewGemDefinition")]
    public class GemDefinition : ItemDefinition
    {
        [SerializeField] private GemKind kind = GemKind.LocalModifiers;
        [SerializeField] private List<Modifier> modifierTemplates = new();
        [SerializeField] private List<GemPowerInfluenceRule> powerInfluenceRules = new();

        public GemKind Kind => kind;
        public IReadOnlyList<Modifier> ModifierTemplates => modifierTemplates;
        public IReadOnlyList<GemPowerInfluenceRule> PowerInfluenceRules =>
            powerInfluenceRules != null ? powerInfluenceRules : Array.Empty<GemPowerInfluenceRule>();

        public GemInstance CreateInstance()
        {
            return GemInstance.Create(this);
        }

        public IReadOnlyList<string> GetBaseDescriptions()
        {
            List<string> descriptions = new();

            if (!string.IsNullOrWhiteSpace(Description))
                descriptions.Add(Description);

            for (int i = 0; i < modifierTemplates.Count; i++)
            {
                Modifier modifier = modifierTemplates[i];
                if (modifier == null)
                    continue;

                descriptions.Add(modifier.GetDescription(ModifierPowerContext.None));
            }

            IReadOnlyList<GemPowerInfluenceRule> influenceRules = PowerInfluenceRules;
            for (int i = 0; i < influenceRules.Count; i++)
            {
                GemPowerInfluenceRule influenceRule = influenceRules[i];
                if (influenceRule == null)
                    continue;

                string description = influenceRule.GetDescription();
                if (!string.IsNullOrWhiteSpace(description))
                    descriptions.Add(description);
            }

            return descriptions;
        }
    }
}
