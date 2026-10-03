using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SkillTree;
using UnityEditor;
using UnityEngine;

public class SkillTreeAnalyzerWindow : EditorWindow
{
    private enum AnalysisMode
    {
        StatType,
        Modifier
    }

    private sealed class StatRow
    {
        public Node Node;
        public Modifier Modifier;
        public ModifierContainer Container;
        public float EffectiveValue;
    }

    private sealed class ModifierRow
    {
        public Node Node;
        public Modifier Modifier;
        public int Occurrences;
    }

    private sealed class StatTotalsRow
    {
        public StatType StatType;
        public float AddedPositive;
        public readonly HashSet<Node> AddedPositiveNodes = new HashSet<Node>();
        public float AddedNegative;
        public readonly HashSet<Node> AddedNegativeNodes = new HashSet<Node>();
        public float IncreasedPositive;
        public readonly HashSet<Node> IncreasedPositiveNodes = new HashSet<Node>();
        public float IncreasedNegative;
        public readonly HashSet<Node> IncreasedNegativeNodes = new HashSet<Node>();
        public float MorePositive;
        public readonly HashSet<Node> MorePositiveNodes = new HashSet<Node>();
        public float MoreNegative;
        public readonly HashSet<Node> MoreNegativeNodes = new HashSet<Node>();
    }

    private sealed class StatTotalsWindow : EditorWindow
    {
        private List<StatTotalsRow> _rows = new List<StatTotalsRow>();
        private int _totalNodeCount;
        private int _emptyNodeCount;
        private bool _applyNodePower;
        private Vector2 _scroll;

        public static void Open(List<StatTotalsRow> rows, int totalNodeCount, int emptyNodeCount, bool applyNodePower)
        {
            var window = GetWindow<StatTotalsWindow>(true, "All Stat Totals");
            window._rows = rows;
            window._totalNodeCount = totalNodeCount;
            window._emptyNodeCount = emptyNodeCount;
            window._applyNodePower = applyNodePower;
            window.minSize = new Vector2(760f, 420f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("All Stat Totals", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Total nodes in tree", _totalNodeCount.ToString());
                EditorGUILayout.LabelField("Empty nodes", _emptyNodeCount.ToString());
                EditorGUILayout.LabelField("Apply Node Power", _applyNodePower ? "Yes" : "No");
            }

            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("Stat Type", EditorStyles.toolbarButton, GUILayout.Width(180f));
                EditorGUILayout.LabelField("Added +", EditorStyles.toolbarButton, GUILayout.Width(90f));
                EditorGUILayout.LabelField("Nodes", EditorStyles.toolbarButton, GUILayout.Width(50f));
                EditorGUILayout.LabelField("Increased +", EditorStyles.toolbarButton, GUILayout.Width(100f));
                EditorGUILayout.LabelField("Nodes", EditorStyles.toolbarButton, GUILayout.Width(50f));
                EditorGUILayout.LabelField("More +", EditorStyles.toolbarButton, GUILayout.Width(90f));
                EditorGUILayout.LabelField("Nodes", EditorStyles.toolbarButton, GUILayout.Width(50f));
                EditorGUILayout.LabelField("Added -", EditorStyles.toolbarButton, GUILayout.Width(90f));
                EditorGUILayout.LabelField("Nodes", EditorStyles.toolbarButton, GUILayout.Width(50f));
                EditorGUILayout.LabelField("Increased -", EditorStyles.toolbarButton, GUILayout.Width(100f));
                EditorGUILayout.LabelField("Nodes", EditorStyles.toolbarButton, GUILayout.Width(50f));
                EditorGUILayout.LabelField("More -", EditorStyles.toolbarButton, GUILayout.Width(90f));
                EditorGUILayout.LabelField("Nodes", EditorStyles.toolbarButton, GUILayout.Width(50f));
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (StatTotalsRow row in _rows)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(row.StatType.ToString(), GUILayout.Width(180f));
                    EditorGUILayout.LabelField(FormatPositiveValue(row.AddedPositive), GUILayout.Width(90f));
                    EditorGUILayout.LabelField(row.AddedPositiveNodes.Count.ToString(), GUILayout.Width(50f));
                    EditorGUILayout.LabelField(FormatPositivePercent(row.IncreasedPositive), GUILayout.Width(100f));
                    EditorGUILayout.LabelField(row.IncreasedPositiveNodes.Count.ToString(), GUILayout.Width(50f));
                    EditorGUILayout.LabelField(FormatPositivePercent(row.MorePositive), GUILayout.Width(90f));
                    EditorGUILayout.LabelField(row.MorePositiveNodes.Count.ToString(), GUILayout.Width(50f));
                    EditorGUILayout.LabelField(FormatNegativeValue(row.AddedNegative), GUILayout.Width(90f));
                    EditorGUILayout.LabelField(row.AddedNegativeNodes.Count.ToString(), GUILayout.Width(50f));
                    EditorGUILayout.LabelField(FormatNegativePercent(row.IncreasedNegative), GUILayout.Width(100f));
                    EditorGUILayout.LabelField(row.IncreasedNegativeNodes.Count.ToString(), GUILayout.Width(50f));
                    EditorGUILayout.LabelField(FormatNegativePercent(row.MoreNegative), GUILayout.Width(90f));
                    EditorGUILayout.LabelField(row.MoreNegativeNodes.Count.ToString(), GUILayout.Width(50f));
                }
            }

            if (_rows.Count == 0)
                EditorGUILayout.LabelField("No stat modifiers found.");

            EditorGUILayout.EndScrollView();
        }
    }

    private AnalysisMode _mode;
    private StatType _selectedStatType = StatType.Empty;
    private Modifier _selectedModifier;
    private bool _applyNodePower = true;
    private Vector2 _scroll;
    private int _totalNodeCount;
    private int _emptyNodeCount;
    private readonly List<StatRow> _statRows = new List<StatRow>();
    private readonly List<ModifierRow> _modifierRows = new List<ModifierRow>();

    [MenuItem("Tools/Skill Tree Analyzer")]
    public static void OpenWindow()
    {
        var window = GetWindow<SkillTreeAnalyzerWindow>("Skill Tree Analyzer");
        window.minSize = new Vector2(560f, 420f);
    }

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnHierarchyChange()
    {
        Rebuild();
        Repaint();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawFilters();

        EditorGUILayout.Space(6f);

        if (_mode == AnalysisMode.StatType)
        {
            DrawStatSummary();
            DrawStatRows();
        }
        else
        {
            DrawModifierSummary();
            DrawModifierRows();
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUI.BeginChangeCheck();
            _mode = (AnalysisMode)EditorGUILayout.EnumPopup(_mode, EditorStyles.toolbarPopup, GUILayout.Width(120f));
            if (EditorGUI.EndChangeCheck())
                Rebuild();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                Rebuild();

            if (GUILayout.Button("All Stat Totals", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                ShowAllStatTotalsWindow();

            using (new EditorGUI.DisabledScope(GetFoundNodes().Count == 0))
            {
                if (GUILayout.Button("Select Nodes", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                    SelectFoundNodes();
            }
        }
    }

    private void DrawFilters()
    {
        EditorGUILayout.Space(6f);

        if (_mode == AnalysisMode.StatType)
        {
            EditorGUI.BeginChangeCheck();
            _selectedStatType = (StatType)EditorGUILayout.EnumPopup("Stat Type", _selectedStatType);
            _applyNodePower = EditorGUILayout.Toggle("Apply Node Power", _applyNodePower);
            if (EditorGUI.EndChangeCheck())
                Rebuild();

            if (_selectedStatType == StatType.Empty)
                EditorGUILayout.HelpBox("Select a StatType to analyze node modifiers.", MessageType.Info);
        }
        else
        {
            EditorGUI.BeginChangeCheck();
            _selectedModifier = (Modifier)EditorGUILayout.ObjectField("Modifier", _selectedModifier, typeof(Modifier), false);
            if (EditorGUI.EndChangeCheck())
                Rebuild();

            if (_selectedModifier == null)
                EditorGUILayout.HelpBox("Select a Modifier asset to find nodes that contain this exact modifier reference.", MessageType.Info);
        }
    }

    private void DrawStatSummary()
    {
        int nodeCount = GetFoundNodes().Count;
        int modifierCount = _statRows.Count;
        float added = SumStatRows(ModifierType.Added);
        float increased = SumStatRows(ModifierType.Increased);
        float more = SumStatRows(ModifierType.More);

        EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Total nodes in tree", _totalNodeCount.ToString());
            EditorGUILayout.LabelField("Empty nodes", _emptyNodeCount.ToString());
            EditorGUILayout.LabelField("Nodes containing stat", nodeCount.ToString());
            EditorGUILayout.LabelField("Modifier entries", modifierCount.ToString());
            EditorGUILayout.LabelField("Added total", FormatValue(added));
            EditorGUILayout.LabelField("Increased total", FormatPercent(increased));
            EditorGUILayout.LabelField("More total", FormatPercent(more));
        }
    }

    private void DrawModifierSummary()
    {
        int nodeCount = _modifierRows.Count;
        int occurrences = _modifierRows.Sum(row => row.Occurrences);

        EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Total nodes in tree", _totalNodeCount.ToString());
            EditorGUILayout.LabelField("Empty nodes", _emptyNodeCount.ToString());
            EditorGUILayout.LabelField("Nodes containing modifier", nodeCount.ToString());
            EditorGUILayout.LabelField("Modifier occurrences", occurrences.ToString());

            if (_selectedModifier != null)
            {
                string path = AssetDatabase.GetAssetPath(_selectedModifier);
                EditorGUILayout.LabelField("Asset Path", string.IsNullOrEmpty(path) ? "(scene/runtime object)" : path);
            }
        }
    }

    private void DrawStatRows()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Matches", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (StatRow row in _statRows)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(row.Node, typeof(Node), true);
                    EditorGUILayout.ObjectField(row.Modifier, typeof(Modifier), false);
                }

                ModifierContainer container = row.Container;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(container.modifierType.ToString(), GUILayout.Width(90f));
                    EditorGUILayout.LabelField("Raw", FormatByType(container.modifierType, container.value), GUILayout.Width(120f));
                    EditorGUILayout.LabelField("Effective", FormatByType(container.modifierType, row.EffectiveValue), GUILayout.Width(140f));
                    EditorGUILayout.LabelField("Power", row.Node != null ? row.Node.Power.ToString("0.###") : "0");
                }
            }
        }

        if (_statRows.Count == 0)
            EditorGUILayout.LabelField("No matches.");

        EditorGUILayout.EndScrollView();
    }

    private void DrawModifierRows()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Matches", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (ModifierRow row in _modifierRows)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(row.Node, typeof(Node), true);
                    EditorGUILayout.ObjectField(row.Modifier, typeof(Modifier), false);
                    EditorGUILayout.LabelField("Count", row.Occurrences.ToString(), GUILayout.Width(80f));
                }

                DrawModifierContainers(row.Modifier, row.Node);
            }
        }

        if (_modifierRows.Count == 0)
            EditorGUILayout.LabelField("No matches.");

        EditorGUILayout.EndScrollView();
    }

    private void DrawModifierContainers(Modifier modifier, Node node)
    {
        List<ModifierContainer> containers = GetModifierContainers(modifier).ToList();
        if (containers.Count == 0)
            return;

        float multiplier = _applyNodePower && node != null ? node.PowerMultiplier : 1f;

        foreach (ModifierContainer container in containers)
        {
            float effectiveValue = ShouldScaleStat(container.statType) ? container.value * multiplier : container.value;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(container.statType.ToString(), GUILayout.Width(180f));
                EditorGUILayout.LabelField(container.modifierType.ToString(), GUILayout.Width(90f));
                EditorGUILayout.LabelField(FormatByType(container.modifierType, effectiveValue));
            }
        }
    }

    private void Rebuild()
    {
        _statRows.Clear();
        _modifierRows.Clear();

        Node[] nodes = FindSceneNodes();
        _totalNodeCount = nodes.Length;
        _emptyNodeCount = nodes.Count(node => node == null || node.Modifiers == null || node.Modifiers.Count == 0);

        if (_mode == AnalysisMode.StatType)
        {
            if (_selectedStatType == StatType.Empty)
                return;

            foreach (Node node in nodes)
            {
                if (node == null || node.Modifiers == null)
                    continue;

                foreach (Modifier modifier in node.Modifiers)
                {
                    foreach (ModifierContainer container in GetModifierContainers(modifier))
                    {
                        if (container == null || container.statType != _selectedStatType)
                            continue;

                        float multiplier = _applyNodePower && ShouldScaleStat(container.statType)
                            ? node.PowerMultiplier
                            : 1f;

                        _statRows.Add(new StatRow
                        {
                            Node = node,
                            Modifier = modifier,
                            Container = container,
                            EffectiveValue = container.value * multiplier
                        });
                    }
                }
            }
        }
        else
        {
            if (_selectedModifier == null)
                return;

            foreach (Node node in nodes)
            {
                if (node == null || node.Modifiers == null)
                    continue;

                int occurrences = node.Modifiers.Count(modifier => modifier == _selectedModifier);
                if (occurrences <= 0)
                    continue;

                _modifierRows.Add(new ModifierRow
                {
                    Node = node,
                    Modifier = _selectedModifier,
                    Occurrences = occurrences
                });
            }
        }
    }

    private static Node[] FindSceneNodes()
    {
        return Resources.FindObjectsOfTypeAll<Node>()
            .Where(node => node != null
                           && node.gameObject.scene.IsValid()
                           && node.gameObject.scene.isLoaded
                           && !EditorUtility.IsPersistent(node))
            .OrderBy(node => node.gameObject.scene.path)
            .ThenBy(GetHierarchyPath)
            .ToArray();
    }

    private static IEnumerable<ModifierContainer> GetModifierContainers(Modifier modifier)
    {
        if (modifier == null)
            yield break;

        if (modifier is BaseModifier baseModifier)
        {
            if (baseModifier.modifierContainer != null)
                yield return baseModifier.modifierContainer;

            yield break;
        }

        foreach (FieldInfo field in GetSerializedFields(modifier.GetType()))
        {
            object value = field.GetValue(modifier);
            if (value == null)
                continue;

            if (value is ModifierContainer container)
            {
                yield return container;
                continue;
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                foreach (object item in enumerable)
                {
                    if (item is ModifierContainer itemContainer)
                        yield return itemContainer;
                }
            }
        }
    }

    private static IEnumerable<FieldInfo> GetSerializedFields(Type type)
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        for (Type current = type; current != null && current != typeof(Modifier); current = current.BaseType)
        {
            foreach (FieldInfo field in current.GetFields(Flags))
            {
                if (field.IsStatic || field.IsNotSerialized)
                    continue;

                bool isSerialized = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
                if (isSerialized)
                    yield return field;
            }
        }
    }

    private HashSet<Node> GetFoundNodes()
    {
        if (_mode == AnalysisMode.StatType)
            return new HashSet<Node>(_statRows.Select(row => row.Node).Where(node => node != null));

        return new HashSet<Node>(_modifierRows.Select(row => row.Node).Where(node => node != null));
    }

    private void SelectFoundNodes()
    {
        Selection.objects = GetFoundNodes()
            .Where(node => node != null)
            .Select(node => node.gameObject)
            .Cast<UnityEngine.Object>()
            .ToArray();
    }

    private void ShowAllStatTotalsWindow()
    {
        Node[] nodes = FindSceneNodes();
        var totals = new Dictionary<StatType, StatTotalsRow>();

        foreach (Node node in nodes)
        {
            if (node == null || node.Modifiers == null)
                continue;

            foreach (Modifier modifier in node.Modifiers)
            {
                foreach (ModifierContainer container in GetModifierContainers(modifier))
                {
                    if (container == null || container.statType == StatType.Empty)
                        continue;

                    float multiplier = _applyNodePower && ShouldScaleStat(container.statType)
                        ? node.PowerMultiplier
                        : 1f;
                    float effectiveValue = container.value * multiplier;

                    if (!totals.TryGetValue(container.statType, out StatTotalsRow row))
                    {
                        row = new StatTotalsRow { StatType = container.statType };
                        totals.Add(container.statType, row);
                    }

                    AddToTotals(row, node, container.modifierType, effectiveValue);
                }
            }
        }

        List<StatTotalsRow> rows = Enum.GetValues(typeof(StatType))
            .Cast<StatType>()
            .Where(statType => statType != StatType.Empty)
            .Select(statType => totals.TryGetValue(statType, out StatTotalsRow row)
                ? row
                : new StatTotalsRow { StatType = statType })
            .ToList();

        int emptyNodeCount = nodes.Count(node => node == null || node.Modifiers == null || node.Modifiers.Count == 0);
        StatTotalsWindow.Open(rows, nodes.Length, emptyNodeCount, _applyNodePower);
    }

    private static void AddToTotals(StatTotalsRow row, Node node, ModifierType modifierType, float value)
    {
        switch (modifierType)
        {
            case ModifierType.Added:
                AddSignedValue(value, node, ref row.AddedPositive, row.AddedPositiveNodes, ref row.AddedNegative, row.AddedNegativeNodes);
                break;
            case ModifierType.Increased:
                AddSignedValue(value, node, ref row.IncreasedPositive, row.IncreasedPositiveNodes, ref row.IncreasedNegative, row.IncreasedNegativeNodes);
                break;
            case ModifierType.More:
                AddSignedValue(value, node, ref row.MorePositive, row.MorePositiveNodes, ref row.MoreNegative, row.MoreNegativeNodes);
                break;
        }
    }

    private static void AddSignedValue(
        float value,
        Node node,
        ref float positiveTotal,
        HashSet<Node> positiveNodes,
        ref float negativeTotal,
        HashSet<Node> negativeNodes)
    {
        if (value > 0f)
        {
            positiveTotal += value;
            positiveNodes.Add(node);
        }
        else if (value < 0f)
        {
            negativeTotal += value;
            negativeNodes.Add(node);
        }
    }

    private static bool ShouldScaleStat(StatType statType)
    {
        return statType != StatType.BarrierDamageTypeMask
               && statType != StatType.LifeStealTypeMask;
    }

    private float SumStatRows(ModifierType modifierType)
    {
        return _statRows
            .Where(row => row.Container != null && row.Container.modifierType == modifierType)
            .Sum(row => row.EffectiveValue);
    }

    private static string GetHierarchyPath(Node node)
    {
        if (node == null)
            return string.Empty;

        var names = new Stack<string>();
        Transform current = node.transform;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }

    private static string FormatByType(ModifierType modifierType, float value)
    {
        return modifierType == ModifierType.Added ? FormatValue(value) : FormatPercent(value);
    }

    private static string FormatValue(float value)
    {
        return value.ToString("+0.###;-0.###;0");
    }

    private static string FormatPercent(float value)
    {
        return (value * 100f).ToString("+0.###;-0.###;0") + "%";
    }

    private static string FormatPositiveValue(float value)
    {
        return Mathf.Approximately(value, 0f) ? "0" : FormatValue(value);
    }

    private static string FormatNegativeValue(float value)
    {
        return Mathf.Approximately(value, 0f) ? "0" : FormatValue(value);
    }

    private static string FormatPositivePercent(float value)
    {
        return Mathf.Approximately(value, 0f) ? "0%" : FormatPercent(value);
    }

    private static string FormatNegativePercent(float value)
    {
        return Mathf.Approximately(value, 0f) ? "0%" : FormatPercent(value);
    }
}
