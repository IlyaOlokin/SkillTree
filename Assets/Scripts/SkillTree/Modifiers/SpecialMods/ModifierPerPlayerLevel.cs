using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Modifier Container Per Player Level", fileName = "New ModifierContainerPerPlayerLevel")]
    public class ModifierPerPlayerLevel : Modifier
    {
        [SerializeField] private ModifierContainer modifierContainer;
        [SerializeField, Min(1)] private int levelsPerModifier = 1;

        public override bool IsApplicable(Unit unit)
        {
            return GetModifierCount(unit) > 0;
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            if (unit is not PlayerUnit playerUnit || playerUnit.UnitLevel == null)
            {
                return null;
            }

            int cachedModifierCount = GetModifierCount(unit);

            void HandleLevelChanged()
            {
                int currentModifierCount = GetModifierCount(unit);
                if (currentModifierCount == cachedModifierCount)
                {
                    return;
                }

                cachedModifierCount = currentModifierCount;
                unit.RequestModRecalculation();
            }

            return new DelegateModifierRuntimeBinding(
                () => playerUnit.UnitLevel.OnExpChanged += HandleLevelChanged,
                () => playerUnit.UnitLevel.OnExpChanged -= HandleLevelChanged);
        }

        public override void ApplyEffect(Unit unit)
        {
            if (modifierContainer == null)
            {
                return;
            }

            int modifierCount = GetModifierCount(unit);
            if (modifierCount <= 0)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer * modifierCount);
        }

        public override void ApplyEffect(Unit unit, ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return;
            }

            int modifierCount = GetModifierCount(unit);
            if (modifierCount <= 0)
            {
                return;
            }

            unit.BaseUnitModifiers.ChangeModifierValue(powerContext.Scale(modifierContainer) * modifierCount);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            string levelIntervalDescription = GetLevelIntervalDescription();
            if (modifierContainer == null)
            {
                return GameLocalization.FormatModifier(
                    "modifier.modifierPerPlayerLevel.noModifier",
                    "Applies modifier per [[0]]",
                    levelIntervalDescription);
            }

            return GameLocalization.FormatModifier(
                "modifier.modifierPerPlayerLevel.withModifier",
                "Adds '[[0]]' per [[1]]",
                powerContext.Scale(modifierContainer).GetDescription(),
                levelIntervalDescription);
        }

        private void OnValidate()
        {
            levelsPerModifier = Mathf.Max(1, levelsPerModifier);
        }

        private int GetModifierCount(Unit unit)
        {
            int playerLevel = GetPlayerLevel(unit);
            if (playerLevel <= 0)
            {
                return 0;
            }

            return playerLevel / Mathf.Max(1, levelsPerModifier);
        }

        private string GetLevelIntervalDescription()
        {
            if (levelsPerModifier <= 1)
            {
                return GameLocalization.GetModifier(
                    "modifier.modifierPerPlayerLevel.level.single",
                    "Player Level");
            }

            return GameLocalization.FormatModifier(
                "modifier.modifierPerPlayerLevel.level.multi",
                "[[0]] Player Levels",
                levelsPerModifier);
        }

        private static int GetPlayerLevel(Unit unit)
        {
            return unit is PlayerUnit playerUnit && playerUnit.UnitLevel != null
                ? Mathf.Max(1, playerUnit.UnitLevel.Level)
                : 0;
        }
    }
}
