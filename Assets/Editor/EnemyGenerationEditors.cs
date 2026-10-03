using System;
using System.Collections.Generic;
using Battle;
using SkillTree;
using UnityEditor;
using UnityEngine;

internal readonly struct EnemyWeightEditorField
{
    public EnemyWeightEditorField(string propertyPath, string label, string section = null)
    {
        PropertyPath = propertyPath;
        Label = label;
        Section = section;
    }

    public string PropertyPath { get; }
    public string Label { get; }
    public string Section { get; }
}

internal static class EnemyWeightEditorGUI
{
    public static void DrawBalancedWeightGroup(
        SerializedObject serializedObject,
        string title,
        IReadOnlyList<EnemyWeightEditorField> definitions)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(title, GetSumLabel(serializedObject, definitions), EditorStyles.boldLabel);

            var properties = new List<SerializedProperty>(definitions.Count);
            for (int i = 0; i < definitions.Count; i++)
                properties.Add(serializedObject.FindProperty(definitions[i].PropertyPath));

            string currentSection = null;
            for (int i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                var property = properties[i];
                if (property == null)
                    continue;

                if (!string.IsNullOrEmpty(definition.Section) && definition.Section != currentSection)
                {
                    currentSection = definition.Section;
                    EditorGUILayout.Space(2f);
                    EditorGUILayout.LabelField(currentSection, EditorStyles.miniBoldLabel);
                }

                EditorGUI.BeginChangeCheck();
                float newValue = DrawSlider(definition.Label, property);
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

        float targetValue = Mathf.Clamp01(changedValue);
        float remainingBudget = 1f - targetValue;
        float otherSum = 0f;

        for (int i = 0; i < properties.Count; i++)
        {
            if (i == changedIndex || properties[i] == null)
                continue;

            otherSum += Mathf.Clamp01(properties[i].floatValue);
        }

        properties[changedIndex].floatValue = targetValue;

        if (remainingBudget <= 0f)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                if (i != changedIndex && properties[i] != null)
                    properties[i].floatValue = 0f;
            }

            return;
        }

        if (otherSum <= 0.0001f)
        {
            int otherCount = 0;
            for (int i = 0; i < properties.Count; i++)
            {
                if (i != changedIndex && properties[i] != null)
                    otherCount++;
            }

            float evenWeight = otherCount > 0 ? remainingBudget / otherCount : 0f;
            for (int i = 0; i < properties.Count; i++)
            {
                if (i != changedIndex && properties[i] != null)
                    properties[i].floatValue = evenWeight;
            }

            return;
        }

        float scale = remainingBudget / otherSum;
        for (int i = 0; i < properties.Count; i++)
        {
            if (i != changedIndex && properties[i] != null)
                properties[i].floatValue = Mathf.Clamp01(properties[i].floatValue * scale);
        }
    }

    private static float DrawSlider(string label, SerializedProperty property)
    {
        bool previousShowMixedValue = EditorGUI.showMixedValue;
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        float value = EditorGUILayout.Slider(label, property.floatValue, 0f, 1f);
        EditorGUI.showMixedValue = previousShowMixedValue;
        return value;
    }

    private static string GetSumLabel(SerializedObject serializedObject, IReadOnlyList<EnemyWeightEditorField> definitions)
    {
        float sum = 0f;
        for (int i = 0; i < definitions.Count; i++)
        {
            var property = serializedObject.FindProperty(definitions[i].PropertyPath);
            if (property == null)
                continue;

            if (property.hasMultipleDifferentValues)
                return "Sum: Mixed";

            sum += Mathf.Clamp01(property.floatValue);
        }

        return $"Sum: {sum:0.###}";
    }
}

[CustomEditor(typeof(EnemyCoreProfile))]
[CanEditMultipleObjects]
public class EnemyCoreProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EnemyWeightEditorGUI.DrawBalancedWeightGroup(
            serializedObject,
            "Category Weights",
            new[]
            {
                new EnemyWeightEditorField("healthWeight", "Health"),
                new EnemyWeightEditorField("offenceWeight", "Offence"),
                new EnemyWeightEditorField("defenceWeight", "Defence"),
                new EnemyWeightEditorField("utilityWeight", "Utility")
            });

        EditorGUILayout.PropertyField(serializedObject.FindProperty("extraModifiers"), true);

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(EnemyAttackSpeedModule))]
[CanEditMultipleObjects]
public class EnemyAttackSpeedModuleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("baseAttackSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("powerMultiplier"));

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(EnemyAttackModule))]
[CanEditMultipleObjects]
public class EnemyAttackModuleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EnemyWeightEditorGUI.DrawBalancedWeightGroup(
            serializedObject,
            "Attack Distribution",
            new[]
            {
                new EnemyWeightEditorField("physical", "Physical Damage"),
                new EnemyWeightEditorField("fire", "Fire Damage"),
                new EnemyWeightEditorField("cold", "Cold Damage"),
                new EnemyWeightEditorField("lightning", "Lightning Damage"),
                new EnemyWeightEditorField("light", "Light Damage"),
                new EnemyWeightEditorField("dark", "Darkness Damage")
            });

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(EnemyDefenceModule))]
[CanEditMultipleObjects]
public class EnemyDefenceModuleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EnemyWeightEditorGUI.DrawBalancedWeightGroup(
            serializedObject,
            "Defence Distribution",
            new[]
            {
                new EnemyWeightEditorField("weights.armor", "Armor"),
                new EnemyWeightEditorField("weights.evasion", "Evasion"),
                new EnemyWeightEditorField("weights.barrierCapacity", "Barrier Capacity"),
                new EnemyWeightEditorField("weights.barrierCount", "Barrier Count"),
                new EnemyWeightEditorField("weights.healthRegeneration", "Health Regeneration"),
                new EnemyWeightEditorField("weights.blockChance", "Block Chance"),
                new EnemyWeightEditorField("weights.elementalResistance", "Elemental Resistance"),
                new EnemyWeightEditorField("weights.fireResistance", "Fire Resistance"),
                new EnemyWeightEditorField("weights.coldResistance", "Cold Resistance"),
                new EnemyWeightEditorField("weights.lightningResistance", "Lightning Resistance"),
                new EnemyWeightEditorField("weights.mysticCleanse", "Mystic Cleanse")
            });

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(EnemyUtilityModule))]
[CanEditMultipleObjects]
public class EnemyUtilityModuleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EnemyWeightEditorGUI.DrawBalancedWeightGroup(
            serializedObject,
            "Utility Distribution",
            new[]
            {
                new EnemyWeightEditorField("critChance", "Crit Chance", "Critical"),
                new EnemyWeightEditorField("critBonus", "Crit Damage Bonus", "Critical"),
                new EnemyWeightEditorField("physical.power", "Power", "Bleed"),
                new EnemyWeightEditorField("physical.mitigation", "Mitigation", "Bleed"),
                new EnemyWeightEditorField("physical.chance", "Chance", "Bleed"),
                new EnemyWeightEditorField("fire.power", "Power", "Ignite"),
                new EnemyWeightEditorField("fire.mitigation", "Mitigation", "Ignite"),
                new EnemyWeightEditorField("fire.chance", "Chance", "Ignite"),
                new EnemyWeightEditorField("cold.power", "Power", "Chill"),
                new EnemyWeightEditorField("cold.mitigation", "Mitigation", "Chill"),
                new EnemyWeightEditorField("cold.chance", "Chance", "Chill"),
                new EnemyWeightEditorField("lightning.power", "Power", "Overcharge"),
                new EnemyWeightEditorField("lightning.mitigation", "Mitigation", "Overcharge"),
                new EnemyWeightEditorField("lightning.chance", "Chance", "Overcharge")
            });

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(EnemyConfigDatabase))]
public class EnemyConfigDatabaseEditor : Editor
{
    private List<EnemySpawnData> _previewWave;
    private int _previewLevel = 1;
    private int _previewWaveIndex = 1;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wave Preview", EditorStyles.boldLabel);
        _previewLevel = EditorGUILayout.IntField("Level", Mathf.Max(1, _previewLevel));
        _previewWaveIndex = EditorGUILayout.IntField("Wave Index", Mathf.Max(1, _previewWaveIndex));

        if (GUILayout.Button("Generate Preview Wave"))
            GeneratePreviewWave();

        if (_previewWave != null)
            DrawPreviewWave();
    }

    private void GeneratePreviewWave()
    {
        ClearPreviewWave();

        var database = (EnemyConfigDatabase)target;
        var factory = new EnemyFactory(database);
        var waveFactory = new WaveFactory(factory, database);
        int wavesInLevel = Mathf.Max(1, database.WavesToUnlockNextLevel);
        int waveIndex = Mathf.Clamp(_previewWaveIndex, 1, wavesInLevel);
        var context = new WaveContext(_previewLevel, waveIndex, wavesInLevel);

        if (database.BossBalance != null && database.BossBalance.TryGetRule(context, out var bossRule))
        {
            context = new WaveContext(
                _previewLevel,
                waveIndex,
                wavesInLevel,
                true,
                bossRule.BossCount,
                bossRule.TotalEnemiesInWave,
                bossRule.MaxBossAffixes);
        }

        _previewWave = waveFactory.CreateWave(context);
    }

    private void DrawPreviewWave()
    {
        if (_previewWave.Count == 0)
        {
            EditorGUILayout.HelpBox("No enemies generated. Check module pools and boss definitions.", MessageType.Warning);
            return;
        }

        for (int i = 0; i < _previewWave.Count; i++)
        {
            var spawnData = _previewWave[i];
            if (spawnData == null)
                continue;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    $"Enemy {i + 1}",
                    $"{spawnData.Rarity} | weight {spawnData.Definition?.WaveWeight:0.##} | power {spawnData.Power:0.##} | exp {spawnData.ExperienceReward:0.##}",
                    EditorStyles.boldLabel);

                DrawGeneratedDefinition(spawnData.Definition);
                DrawRolledAffixes(spawnData.Affixes);
                DrawFinalStats(spawnData.Modifiers);
            }
        }
    }

    private static void DrawGeneratedDefinition(GeneratedEnemyDefinition definition)
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Core", definition?.CoreProfile, typeof(EnemyCoreProfile), false);
            EditorGUILayout.ObjectField("Attack Speed", definition?.AttackSpeedModule, typeof(EnemyAttackSpeedModule), false);
            EditorGUILayout.ObjectField("Attack", definition?.AttackModule, typeof(EnemyAttackModule), false);
            EditorGUILayout.ObjectField("Defence", definition?.DefenceModule, typeof(EnemyDefenceModule), false);
            EditorGUILayout.ObjectField("Utility", definition?.UtilityModule, typeof(EnemyUtilityModule), false);
            EditorGUILayout.EnumPopup("Weapon", definition != null ? definition.WeaponType : WeaponType.Sword);
        }
    }

    private static void DrawRolledAffixes(IReadOnlyList<EnemyAffix> affixes)
    {
        EditorGUILayout.LabelField("Rolled Affixes", EditorStyles.miniBoldLabel);

        if (affixes == null || affixes.Count == 0)
        {
            EditorGUILayout.LabelField("No affixes rolled.");
            return;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            for (int i = 0; i < affixes.Count; i++)
            {
                EnemyAffix affix = affixes[i];
                EditorGUILayout.ObjectField(
                    GetAffixLabel(i, affix),
                    affix,
                    typeof(EnemyAffix),
                    false);
            }
        }
    }

    private static string GetAffixLabel(int index, EnemyAffix affix)
    {
        if (affix == null)
            return $"Affix {index + 1}";

        string key = string.IsNullOrWhiteSpace(affix.affixName)
            ? affix.name
            : affix.affixName;

        if (affix.moreExperience <= 0.0001f)
            return $"{index + 1}. {key}";

        return $"{index + 1}. {key} (+{affix.moreExperience * 100f:0.#}% exp)";
    }

    private static void DrawFinalStats(BaseInnateModifiers innateModifiers)
    {
        if (innateModifiers == null)
            return;

        var previewModifiers = new BaseUnitModifiers();
        foreach (var modifier in innateModifiers.baseModifiers)
            previewModifiers.ChangeModifierValue(modifier);

        StatCalculator.MergeDamageModifiers(previewModifiers);
        StatCalculator.MergeDefenceModifiers(previewModifiers);
        StatCalculator.MergeAilmentModifiers(previewModifiers);

        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            previewModifiers.SetStatValue(statType, StatCalculator.GetStat(previewModifiers, statType));

        EditorGUILayout.LabelField("Final Stats (with rolled affixes)", EditorStyles.miniBoldLabel);
        bool drewAny = false;
        float maximumHealth = previewModifiers.GetStatValue(StatType.MaximumHealth);
        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            if (statType == StatType.Empty)
                continue;

            float value = previewModifiers.GetStatValue(statType);
            if (Mathf.Abs(value) <= 0.0001f)
                continue;

            drewAny = true;
            EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(statType.ToString()), FormatStatValue(statType, value, maximumHealth));
        }

        if (!drewAny)
            EditorGUILayout.LabelField("No final stats generated.");
    }

    private static string FormatStatValue(StatType statType, float value, float maximumHealth = 0f)
    {
        if (statType == StatType.BarrierCount)
            return Mathf.FloorToInt(value + 0.5f).ToString();

        if (statType == StatType.HealthRegenerationPerSecond && maximumHealth > 0.0001f)
            return $"{value:0.##}/s ({value / maximumHealth * 100f:0.##}% HP/s)";

        if (StatTypeDisplayRules.IsPercentStat(statType))
            return $"{value * 100f:0.##}%";

        return value.ToString("0.##");
    }

    private void ClearPreviewWave()
    {
        if (_previewWave == null)
            return;

        for (int i = 0; i < _previewWave.Count; i++)
        {
            if (_previewWave[i]?.Modifiers != null)
                DestroyImmediate(_previewWave[i].Modifiers);
        }

        _previewWave = null;
    }

    private void OnDisable()
    {
        ClearPreviewWave();
    }
}
