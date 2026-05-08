using SkillTree;
using UnityEngine;

namespace Gems
{
    public static class GemModifierUtility
    {
        public static Modifier CreateRuntimeModifier(Modifier template)
        {
            if (template == null)
                return null;

            Modifier modifierInstance = Object.Instantiate(template);
            modifierInstance.name = template.name;
            return modifierInstance;
        }

        public static void DestroyRuntimeModifier(Modifier modifier)
        {
            if (modifier == null)
                return;

            if (Application.isPlaying)
            {
                Object.Destroy(modifier);
                return;
            }

            Object.DestroyImmediate(modifier);
        }
    }
}
