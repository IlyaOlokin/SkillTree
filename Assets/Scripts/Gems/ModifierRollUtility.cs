using System;
using System.Collections.Generic;
using System.Reflection;
using SkillTree;
using UnityEngine;

namespace Gems
{
    public static class ModifierRollUtility
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Dictionary<Type, FieldInfo[]> ModifierContainerFieldsByType = new();

        public static Modifier CreateRolledModifier(Modifier template, float rolledValue)
        {
            if (template == null)
                return null;

            Modifier modifierInstance = UnityEngine.Object.Instantiate(template);
            modifierInstance.name = template.name;
            ApplyRolledValue(modifierInstance, rolledValue);
            return modifierInstance;
        }

        public static bool ApplyRolledValue(Modifier modifier, float rolledValue)
        {
            if (modifier == null)
                return false;

            FieldInfo[] fields = GetModifierContainerFields(modifier.GetType());
            if (fields.Length == 0)
                return false;

            bool changed = false;
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].GetValue(modifier) is not ModifierContainer container)
                    continue;

                fields[i].SetValue(modifier, new ModifierContainer(
                    container.modifierType,
                    container.statType,
                    rolledValue));
                changed = true;
            }

            return changed;
        }

        public static void DestroyModifier(Modifier modifier)
        {
            if (modifier == null)
                return;

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(modifier);
                return;
            }

            UnityEngine.Object.DestroyImmediate(modifier);
        }

        private static FieldInfo[] GetModifierContainerFields(Type modifierType)
        {
            if (modifierType == null)
                return Array.Empty<FieldInfo>();

            if (ModifierContainerFieldsByType.TryGetValue(modifierType, out FieldInfo[] cachedFields))
                return cachedFields;

            List<FieldInfo> fields = new();
            FieldInfo[] typeFields = modifierType.GetFields(FieldFlags);
            for (int i = 0; i < typeFields.Length; i++)
            {
                if (typeFields[i].FieldType == typeof(ModifierContainer))
                    fields.Add(typeFields[i]);
            }

            FieldInfo[] result = fields.ToArray();
            ModifierContainerFieldsByType[modifierType] = result;
            return result;
        }
    }
}
