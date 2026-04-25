using System;
using System.Collections.Generic;
using Battle;
using SkillTree;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyArchetype))]
public class EnemyArchetypeEditor : Editor
{
    private readonly EnemyStatPackageBuilder _builder = new();
    private bool _showBudgetPreview = true;
    private bool _showStatPreview = true;

    private readonly struct WeightFieldDefinition
    {
        public WeightFieldDefinition(string propertyPath, string label, string section = null)
        {
            PropertyPath = propertyPath;
            Label = label;
            Section = section;
        }

        public string PropertyPath { get; }
        public string Label { get; }
        public string Section { get; }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSpawnRules();
        DrawBalancedWeightGroup(
            "Category Weights",
            new[]
            {
                new WeightFieldDefinition("healthWeight", "Health"),
                new WeightFieldDefinition("offenceWeight", "Offence"),
                new WeightFieldDefinition("defenceWeight", "Defence"),
                new WeightFieldDefinition("utilityWeight", "Utility")
            });

        DrawBalancedWeightGroup(
            "Health Distribution",
            new[]
            {
                new WeightFieldDefinition("health.maxHealth", "Max Health")
            });

        DrawBalancedWeightGroup(
            "Offence Distribution",
            new[]
            {
                new WeightFieldDefinition("offence.physical", "Physical Damage"),
                new WeightFieldDefinition("offence.fire", "Fire Damage"),
                new WeightFieldDefinition("offence.cold", "Cold Damage"),
                new WeightFieldDefinition("offence.lightning", "Lightning Damage"),
                new WeightFieldDefinition("offence.light", "Light Damage"),
                new WeightFieldDefinition("offence.dark", "Darkness Damage"),
                new WeightFieldDefinition("offence.critChance", "Crit Chance"),
                new WeightFieldDefinition("offence.critBonus", "Crit Bonus")
            });

        DrawBalancedWeightGroup(
            "Defence Distribution",
            new[]
            {
                new WeightFieldDefinition("defence.armor", "Armor"),
                new WeightFieldDefinition("defence.evasion", "Evasion"),
                new WeightFieldDefinition("defence.barrierCapacity", "Barrier Capacity"),
                new WeightFieldDefinition("defence.barrierCount", "Barrier Count"),
                new WeightFieldDefinition("defence.blockChance", "Block Chance"),
                new WeightFieldDefinition("defence.elementalResistance", "Elemental Resistance"),
                new WeightFieldDefinition("defence.fireResistance", "Fire Resistance"),
                new WeightFieldDefinition("defence.coldResistance", "Cold Resistance"),
                new WeightFieldDefinition("defence.lightningResistance", "Lightning Resistance"),
                new WeightFieldDefinition("defence.mysticCleanse", "Mystic Cleanse")
            });

        DrawBalancedWeightGroup(
            "Utility Distribution",
            new[]
            {
                new WeightFieldDefinition("utility.physical.power", "Power", "Bleed"),
                new WeightFieldDefinition("utility.physical.mitigation", "Mitigation", "Bleed"),
                new WeightFieldDefinition("utility.physical.chance", "Chance", "Bleed"),
                new WeightFieldDefinition("utility.fire.power", "Power", "Ignite"),
                new WeightFieldDefinition("utility.fire.mitigation", "Mitigation", "Ignite"),
                new WeightFieldDefinition("utility.fire.chance", "Chance", "Ignite"),
                new WeightFieldDefinition("utility.cold.power", "Power", "Chill"),
                new WeightFieldDefinition("utility.cold.mitigation", "Mitigation", "Chill"),
                new WeightFieldDefinition("utility.cold.chance", "Chance", "Chill"),
                new WeightFieldDefinition("utility.lightning.power", "Power", "Overcharge"),
                new WeightFieldDefinition("utility.lightning.mitigation", "Mitigation", "Overcharge"),
                new WeightFieldDefinition("utility.lightning.chance", "Chance", "Overcharge")
            });

        DrawGeneralProperties();

        serializedObject.ApplyModifiedProperties();

        DrawPreview((EnemyArchetype)target);
    }

    private void DrawSpawnRules()
    {
        EditorGUILayout.LabelField("Spawn Rules", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("minLevel"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxLevel"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("allowedRarities"), true);
        EditorGUILayout.Space();
    }

    private void DrawGeneralProperties()
    {
        EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("powerMultiplier"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("baseAttackSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("extraModifiers"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("possibleAffixes"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("gemDropTable"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("previewDatabaseOverride"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("previewPower"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("previewRarity"));
    }

    private void DrawBalancedWeightGroup(string title, IReadOnlyList<WeightFieldDefinition> definitions)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(title, $"Sum: {GetSum(definitions):0.###}", EditorStyles.boldLabel);

            if (definitions.Count == 1)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    var onlyProperty = serializedObject.FindProperty(definitions[0].PropertyPath);
                    EditorGUILayout.Slider(definitions[0].Label, onlyProperty.floatValue, 0f, 1f);
                }

                return;
            }

            var properties = new List<SerializedProperty>(definitions.Count);
            for (int i = 0; i < definitions.Count; i++)
                properties.Add(serializedObject.FindProperty(definitions[i].PropertyPath));

            string currentSection = null;
            for (int i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (!string.IsNullOrEmpty(definition.Section) && definition.Section != currentSection)
                {
                    currentSection = definition.Section;
                    EditorGUILayout.Space(2f);
                    EditorGUILayout.LabelField(currentSection, EditorStyles.miniBoldLabel);
                }

                EditorGUI.BeginChangeCheck();
                float newValue = EditorGUILayout.Slider(definition.Label, properties[i].floatValue, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                    RebalanceGroup(properties, i, newValue);
            }
        }

        EditorGUILayout.Space();
    }

    private static void RebalanceGroup(IReadOnlyList<SerializedProperty> properties, int changedIndex, float changedValue)
    {
        if (properties == null || properties.Count == 0)
            return;

        if (properties.Count == 1)
        {
            properties[0].floatValue = 1f;
            return;
        }

        float targetValue = Mathf.Clamp01(changedValue);
        float remainingBudget = 1f - targetValue;
        float otherSum = 0f;

        for (int i = 0; i < properties.Count; i++)
        {
            if (i == changedIndex)
                continue;

            otherSum += Mathf.Clamp01(properties[i].floatValue);
        }

        properties[changedIndex].floatValue = targetValue;

        if (remainingBudget <= 0f)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                if (i == changedIndex)
                    continue;

                properties[i].floatValue = 0f;
            }

            return;
        }

        if (otherSum <= 0.0001f)
        {
            float evenWeight = remainingBudget / (properties.Count - 1);
            for (int i = 0; i < properties.Count; i++)
            {
                if (i == changedIndex)
                    continue;

                properties[i].floatValue = evenWeight;
            }

            return;
        }

        float scale = remainingBudget / otherSum;
        for (int i = 0; i < properties.Count; i++)
        {
            if (i == changedIndex)
                continue;

            properties[i].floatValue = Mathf.Clamp01(properties[i].floatValue * scale);
        }
    }

    private float GetSum(IReadOnlyList<WeightFieldDefinition> definitions)
    {
        float sum = 0f;
        for (int i = 0; i < definitions.Count; i++)
        {
            var property = serializedObject.FindProperty(definitions[i].PropertyPath);
            if (property != null)
                sum += Mathf.Clamp01(property.floatValue);
        }

        return sum;
    }

    private void DrawPreview(EnemyArchetype archetype)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Archetype Preview", EditorStyles.boldLabel);

        if (!TryResolveDatabase(archetype, out var database, out var matchedDatabases))
        {
            EditorGUILayout.HelpBox(
                "Preview config database not found. Assign Preview Database Override or add this archetype to an EnemyConfigDatabase.",
                MessageType.Info);
            return;
        }

        float basePower = archetype.PreviewPower;
        float previewPower = EnemyPowerCalculator.Calculate(basePower, archetype.PreviewRarity, archetype, false);
        float totalCategoryWeight = archetype.GetTotalCategoryWeight();

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Preview Database", database, typeof(EnemyConfigDatabase), false);
            EditorGUILayout.ObjectField("Stat Budget Config", database != null ? database.StatBudgetConfig : null, typeof(EnemyStatBudgetConfig), false);
            EditorGUILayout.FloatField("Base Power", basePower);
            EditorGUILayout.FloatField("Preview Power", previewPower);
        }

        if (database != null && database.StatBudgetConfig == null)
        {
            EditorGUILayout.HelpBox("Preview uses default budget rules because no EnemyStatBudgetConfig is assigned in the database.", MessageType.None);
        }

        if (matchedDatabases.Count > 1 && archetype.PreviewDatabaseOverride == null)
        {
            EditorGUILayout.HelpBox(
                "Multiple EnemyConfigDatabase assets reference this archetype. Preview uses the first match unless Preview Database Override is assigned.",
                MessageType.Warning);
        }

        if (totalCategoryWeight <= 0f)
        {
            EditorGUILayout.HelpBox("Category weights sum to zero. Preview stats cannot be generated.", MessageType.Warning);
            return;
        }

        EditorGUILayout.HelpBox(
            "Preview is deterministic: no random power variance, no random affixes. Global database modifiers and rarity scaling are included.",
            MessageType.None);

        var previewModifiers = BuildPreviewModifiers(archetype, database, basePower);

        _showBudgetPreview = EditorGUILayout.Foldout(_showBudgetPreview, "Budget Breakdown", true);
        if (_showBudgetPreview)
        {
            DrawBudgetCategory(
                "Health",
                archetype.healthWeight,
                totalCategoryWeight,
                previewPower,
                archetype.AddHealthEntries,
                database != null ? database.StatBudgetConfig : null,
                previewModifiers);

            DrawBudgetCategory(
                "Offence",
                archetype.offenceWeight,
                totalCategoryWeight,
                previewPower,
                archetype.AddOffenceEntries,
                database != null ? database.StatBudgetConfig : null,
                previewModifiers);

            DrawBudgetCategory(
                "Defence",
                archetype.defenceWeight,
                totalCategoryWeight,
                previewPower,
                archetype.AddDefenceEntries,
                database != null ? database.StatBudgetConfig : null,
                previewModifiers);

            DrawBudgetCategory(
                "Utility",
                archetype.utilityWeight,
                totalCategoryWeight,
                previewPower,
                archetype.AddUtilityEntries,
                database != null ? database.StatBudgetConfig : null,
                previewModifiers);
        }

        _showStatPreview = EditorGUILayout.Foldout(_showStatPreview, "Final Stats", true);
        if (_showStatPreview)
            DrawFinalStats(previewModifiers);
    }

    private BaseUnitModifiers BuildPreviewModifiers(EnemyArchetype archetype, EnemyConfigDatabase database, float basePower)
    {
        var previewModifiers = new BaseUnitModifiers();
        EnemySpawnData previewSpawn = null;

        try
        {
            previewSpawn = _builder.Build(
                basePower,
                basePower,
                archetype,
                archetype.PreviewRarity,
                database != null ? database.StatBudgetConfig : null,
                0,
                false);

            if (database?.GlobalModifiers != null)
                previewSpawn.Modifiers.AddRange(database.GlobalModifiers);

            foreach (var modifier in previewSpawn.Modifiers.baseModifiers)
                previewModifiers.ChangeModifierValue(modifier);

            StatCalculator.MergeDamageModifiers(previewModifiers);
            StatCalculator.MergeDefenceModifiers(previewModifiers);
            StatCalculator.MergeAilmentModifiers(previewModifiers);

            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
                previewModifiers.SetStatValue(statType, StatCalculator.GetStat(previewModifiers, statType));
        }
        finally
        {
            if (previewSpawn?.Modifiers != null)
                DestroyImmediate(previewSpawn.Modifiers);
        }

        return previewModifiers;
    }

    private void DrawBudgetCategory(
        string categoryName,
        float categoryWeight,
        float totalCategoryWeight,
        float previewPower,
        Action<List<EnemyStatWeightEntry>> addEntries,
        EnemyStatBudgetConfig statBudgetConfig,
        BaseUnitModifiers previewModifiers)
    {
        float normalizedCategoryWeight = totalCategoryWeight > 0f
            ? Mathf.Max(0f, categoryWeight) / totalCategoryWeight
            : 0f;
        float categoryBudget = previewPower * normalizedCategoryWeight;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(
                categoryName,
                $"{categoryWeight:0.##} weight | {normalizedCategoryWeight * 100f:0.#}% | {categoryBudget:0.##} budget",
                EditorStyles.boldLabel);

            var entries = new List<EnemyStatWeightEntry>();
            addEntries?.Invoke(entries);

            if (entries.Count == 0)
            {
                EditorGUILayout.LabelField("No weighted stats.");
                return;
            }

            float totalStatWeight = 0f;
            for (int i = 0; i < entries.Count; i++)
                totalStatWeight += Mathf.Max(0f, entries[i].Weight);

            if (totalStatWeight <= 0f)
            {
                EditorGUILayout.LabelField("All stat weights are zero.");
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                float normalizedStatWeight = entry.Weight / totalStatWeight;
                float statBudget = categoryBudget * normalizedStatWeight;
                float allocationRatio = normalizedCategoryWeight * normalizedStatWeight;
                float finalValue = previewModifiers.GetStatValue(entry.StatType);
                EnemyStatBudgetRule rule = statBudgetConfig != null
                    ? statBudgetConfig.GetRule(entry.StatType)
                    : EnemyStatBudgetRuleDefaults.Get(entry.StatType);

                EditorGUILayout.LabelField(
                    Nicify(entry.StatType),
                    $"{entry.Weight:0.##} w | {normalizedStatWeight * 100f:0.#}% in category | {allocationRatio * 100f:0.#}% total | {statBudget:0.##} budget | {FormatRule(rule)} | {FormatStatValue(entry.StatType, finalValue)}");
            }
        }
    }

    private void DrawFinalStats(BaseUnitModifiers previewModifiers)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            bool drewAny = false;

            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                if (statType == StatType.Empty)
                    continue;

                float value = previewModifiers.GetStatValue(statType);
                if (Mathf.Abs(value) <= 0.0001f)
                    continue;

                drewAny = true;
                EditorGUILayout.LabelField(Nicify(statType), FormatStatValue(statType, value));
            }

            if (!drewAny)
                EditorGUILayout.LabelField("No final stats generated.");
        }
    }

    private static string FormatRule(EnemyStatBudgetRule rule)
    {
        return rule.scaling switch
        {
            EnemyBudgetScaling.Linear => $"Linear x{rule.multiplier:0.###}",
            EnemyBudgetScaling.Power => $"Power e{rule.exponent:0.###} x{rule.multiplier:0.###}",
            EnemyBudgetScaling.SoftCap => $"SoftCap {rule.maxValue:0.###}",
            EnemyBudgetScaling.Step => $"Step {rule.stepSize:0.###}",
            EnemyBudgetScaling.AllocationLinearCap => $"AllocationCap x{rule.multiplier:0.###} max {rule.maxValue:0.###}",
            _ => rule.scaling.ToString()
        };
    }

    private static string FormatStatValue(StatType statType, float value)
    {
        if (statType == StatType.BarrierCount)
            return Mathf.RoundToInt(value).ToString();

        if (StatTypeDisplayRules.IsPercentStat(statType))
            return $"{value * 100f:0.##}%";

        return value.ToString("0.##");
    }

    private static string Nicify(StatType statType)
    {
        return ObjectNames.NicifyVariableName(statType.ToString());
    }

    private static bool TryResolveDatabase(
        EnemyArchetype archetype,
        out EnemyConfigDatabase database,
        out List<EnemyConfigDatabase> matchedDatabases)
    {
        matchedDatabases = new List<EnemyConfigDatabase>();

        if (archetype.PreviewDatabaseOverride != null)
        {
            database = archetype.PreviewDatabaseOverride;
            matchedDatabases.Add(database);
            return true;
        }

        string[] guids = AssetDatabase.FindAssets("t:EnemyConfigDatabase");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var candidate = AssetDatabase.LoadAssetAtPath<EnemyConfigDatabase>(path);
            if (candidate == null || candidate.archetypes == null)
                continue;

            if (candidate.archetypes.Contains(archetype))
                matchedDatabases.Add(candidate);
        }

        database = matchedDatabases.Count > 0 ? matchedDatabases[0] : null;
        return database != null;
    }
}
