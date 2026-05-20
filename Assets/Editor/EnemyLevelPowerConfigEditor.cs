using Battle;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyLevelPowerConfig))]
public class EnemyLevelPowerConfigEditor : Editor
{
    private const float MinimumPower = 0.01f;

    private SerializedProperty _levelPowers;
    private bool _showRawList = true;
    private bool _showChart = true;

    private int _levelCount = 40;
    private int _rangeStart = 1;
    private int _rangeEnd = 40;
    private float _flatAmount = 5f;
    private float _multiplyFactor = 1.1f;
    private float _setValue = 10f;
    private float _curveStartPower = 12f;
    private float _curveEndPower = 175f;
    private float _curveExponent = 1.35f;
    private float _interpolateStartPower = 12f;
    private float _interpolateEndPower = 175f;
    private int _smoothIterations = 1;
    private float _smoothStrength = 0.5f;
    private bool _preserveSmoothEndpoints = true;
    private float _roundStep = 1f;

    private void OnEnable()
    {
        _levelPowers = serializedObject.FindProperty("levelPowers");
        _levelCount = Mathf.Max(1, _levelPowers != null ? _levelPowers.arraySize : 1);
        _rangeEnd = _levelCount;
        SyncToolDefaultsFromList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (_levelPowers == null)
        {
            EditorGUILayout.HelpBox("levelPowers property was not found.", MessageType.Error);
            return;
        }

        DrawSummary();
        DrawChart();
        DrawSizeTools();
        DrawRangeTools();
        DrawCurveTools();
        DrawSmoothingTools();
        DrawCleanupTools();
        DrawRawList();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSummary()
    {
        int count = _levelPowers.arraySize;
        if (count <= 0)
        {
            EditorGUILayout.HelpBox("The list is empty. Use Resize to add levels.", MessageType.Warning);
            return;
        }

        float first = GetPower(0);
        float last = GetPower(count - 1);
        float min = first;
        float max = first;
        float largestJump = 0f;
        int largestJumpLevel = 1;

        for (int i = 0; i < count; i++)
        {
            float value = GetPower(i);
            min = Mathf.Min(min, value);
            max = Mathf.Max(max, value);

            if (i <= 0)
                continue;

            float jump = Mathf.Abs(value - GetPower(i - 1));
            if (jump > largestJump)
            {
                largestJump = jump;
                largestJumpLevel = i + 1;
            }
        }

        float totalGrowth = last - first;
        float averageStep = count > 1 ? totalGrowth / (count - 1) : 0f;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Power Summary", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Levels", count);
                EditorGUILayout.FloatField("First Level Power", first);
                EditorGUILayout.FloatField("Last Level Power", last);
                EditorGUILayout.FloatField("Total Growth", totalGrowth);
                EditorGUILayout.FloatField("Average Step", averageStep);
                EditorGUILayout.LabelField("Min / Max", $"{min:0.###} / {max:0.###}");
                EditorGUILayout.LabelField("Largest Jump", $"{largestJump:0.###} before level {largestJumpLevel}");
            }
        }
    }

    private void DrawChart()
    {
        _showChart = EditorGUILayout.Foldout(_showChart, "Power Chart", true);
        if (!_showChart)
            return;

        if (_levelPowers.arraySize <= 0)
            return;

        Rect rect = GUILayoutUtility.GetRect(10f, 120f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.13f));

        float min = GetPower(0);
        float max = min;
        for (int i = 1; i < _levelPowers.arraySize; i++)
        {
            float value = GetPower(i);
            min = Mathf.Min(min, value);
            max = Mathf.Max(max, value);
        }

        float range = Mathf.Max(0.0001f, max - min);
        Handles.BeginGUI();
        Handles.color = new Color(0.25f, 0.25f, 0.25f);
        for (int i = 1; i < 4; i++)
        {
            float y = Mathf.Lerp(rect.yMin, rect.yMax, i / 4f);
            Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMax, y));
        }

        Handles.color = new Color(0.3f, 0.75f, 1f);
        for (int i = 1; i < _levelPowers.arraySize; i++)
        {
            Vector2 previous = GetChartPoint(rect, i - 1, min, range);
            Vector2 current = GetChartPoint(rect, i, min, range);
            Handles.DrawAAPolyLine(2.5f, previous, current);
        }
        Handles.EndGUI();
    }

    private Vector2 GetChartPoint(Rect rect, int index, float min, float range)
    {
        float t = _levelPowers.arraySize <= 1 ? 0f : index / (float)(_levelPowers.arraySize - 1);
        float normalizedPower = (GetPower(index) - min) / range;
        return new Vector2(
            Mathf.Lerp(rect.xMin + 4f, rect.xMax - 4f, t),
            Mathf.Lerp(rect.yMax - 4f, rect.yMin + 4f, normalizedPower));
    }

    private void DrawSizeTools()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("List Size", EditorStyles.boldLabel);
            _levelCount = Mathf.Max(1, EditorGUILayout.IntField("Level Count", _levelCount));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Resize Keep Values"))
                    ApplyOperation("Resize level powers", () => ResizeList(_levelCount));

                if (GUILayout.Button("Append 1 Level"))
                    ApplyOperation("Append level power", () => ResizeList(_levelPowers.arraySize + 1));
            }
        }
    }

    private void DrawRangeTools()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Range Operations", EditorStyles.boldLabel);
            DrawRangeFields();

            _flatAmount = EditorGUILayout.FloatField("Add Power", _flatAmount);
            if (GUILayout.Button("Add To Range"))
                ApplyOperation("Add power to level range", () => ForEachInRange(i => SetPower(i, GetPower(i) + _flatAmount)));

            _multiplyFactor = EditorGUILayout.FloatField("Multiply Factor", _multiplyFactor);
            if (GUILayout.Button("Multiply Range"))
                ApplyOperation("Multiply level power range", () => ForEachInRange(i => SetPower(i, GetPower(i) * _multiplyFactor)));

            _setValue = Mathf.Max(MinimumPower, EditorGUILayout.FloatField("Set Value", _setValue));
            if (GUILayout.Button("Set Range Value"))
                ApplyOperation("Set level power range", () => ForEachInRange(i => SetPower(i, _setValue)));
        }
    }

    private void DrawCurveTools()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Growth Curves", EditorStyles.boldLabel);
            DrawRangeFields();

            _curveStartPower = Mathf.Max(MinimumPower, EditorGUILayout.FloatField("Start Power", _curveStartPower));
            _curveEndPower = Mathf.Max(MinimumPower, EditorGUILayout.FloatField("End Power", _curveEndPower));
            _curveExponent = Mathf.Max(0.01f, EditorGUILayout.FloatField("Growth Exponent", _curveExponent));
            if (GUILayout.Button("Generate Exponential Range"))
                ApplyOperation("Generate level power curve", GenerateCurveRange);

            _interpolateStartPower = Mathf.Max(MinimumPower, EditorGUILayout.FloatField("Linear Start", _interpolateStartPower));
            _interpolateEndPower = Mathf.Max(MinimumPower, EditorGUILayout.FloatField("Linear End", _interpolateEndPower));
            if (GUILayout.Button("Linear Interpolate Range"))
                ApplyOperation("Interpolate level power range", GenerateLinearRange);
        }
    }

    private void DrawSmoothingTools()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Smoothing", EditorStyles.boldLabel);
            DrawRangeFields();

            _smoothIterations = Mathf.Clamp(EditorGUILayout.IntField("Iterations", _smoothIterations), 1, 20);
            _smoothStrength = EditorGUILayout.Slider("Strength", _smoothStrength, 0f, 1f);
            _preserveSmoothEndpoints = EditorGUILayout.Toggle("Preserve Endpoints", _preserveSmoothEndpoints);

            using (new EditorGUI.DisabledScope(GetRangeLength() < 3))
            {
                if (GUILayout.Button("Smooth Range"))
                    ApplyOperation("Smooth level power range", SmoothRange);
            }
        }
    }

    private void DrawCleanupTools()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Cleanup", EditorStyles.boldLabel);
            DrawRangeFields();

            _roundStep = Mathf.Max(0.01f, EditorGUILayout.FloatField("Round Step", _roundStep));
            if (GUILayout.Button("Round Range"))
                ApplyOperation("Round level power range", () => ForEachInRange(i => SetPower(i, Mathf.Round(GetPower(i) / _roundStep) * _roundStep)));

            if (GUILayout.Button("Clamp Range To Minimum"))
                ApplyOperation("Clamp level power range", () => ForEachInRange(i => SetPower(i, GetPower(i))));
        }
    }

    private void DrawRawList()
    {
        _showRawList = EditorGUILayout.Foldout(_showRawList, "Raw Level Powers", true);
        if (!_showRawList)
            return;

        EditorGUILayout.PropertyField(_levelPowers, true);
    }

    private void DrawRangeFields()
    {
        int count = Mathf.Max(1, _levelPowers.arraySize);
        using (new EditorGUILayout.HorizontalScope())
        {
            _rangeStart = Mathf.Clamp(EditorGUILayout.IntField("From Level", _rangeStart), 1, count);
            _rangeEnd = Mathf.Clamp(EditorGUILayout.IntField("To Level", _rangeEnd), 1, count);
        }

        if (_rangeEnd < _rangeStart)
            _rangeEnd = _rangeStart;
    }

    private void ApplyOperation(string undoName, System.Action operation)
    {
        serializedObject.Update();
        Undo.RecordObject(target, undoName);
        operation?.Invoke();
        ClampAllPowers();
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        SyncToolDefaultsFromList();
    }

    private void ResizeList(int targetSize)
    {
        int size = Mathf.Max(1, targetSize);
        int oldSize = _levelPowers.arraySize;
        float lastValue = oldSize > 0 ? GetPower(oldSize - 1) : MinimumPower;

        _levelPowers.arraySize = size;
        for (int i = oldSize; i < size; i++)
            SetPower(i, lastValue);

        _levelCount = size;
        _rangeStart = Mathf.Clamp(_rangeStart, 1, size);
        _rangeEnd = Mathf.Clamp(_rangeEnd, _rangeStart, size);
    }

    private void GenerateCurveRange()
    {
        int startIndex;
        int endIndex;
        if (!TryGetRange(out startIndex, out endIndex))
            return;

        int length = endIndex - startIndex + 1;
        for (int i = 0; i < length; i++)
        {
            float t = length <= 1 ? 1f : i / (float)(length - 1);
            float curved = Mathf.Pow(t, _curveExponent);
            SetPower(startIndex + i, Mathf.Lerp(_curveStartPower, _curveEndPower, curved));
        }
    }

    private void GenerateLinearRange()
    {
        int startIndex;
        int endIndex;
        if (!TryGetRange(out startIndex, out endIndex))
            return;

        int length = endIndex - startIndex + 1;
        for (int i = 0; i < length; i++)
        {
            float t = length <= 1 ? 1f : i / (float)(length - 1);
            SetPower(startIndex + i, Mathf.Lerp(_interpolateStartPower, _interpolateEndPower, t));
        }
    }

    private void SmoothRange()
    {
        int startIndex;
        int endIndex;
        if (!TryGetRange(out startIndex, out endIndex))
            return;

        int length = endIndex - startIndex + 1;
        if (length < 3)
            return;

        float[] values = new float[length];
        for (int i = 0; i < length; i++)
            values[i] = GetPower(startIndex + i);

        float[] buffer = new float[length];
        for (int iteration = 0; iteration < _smoothIterations; iteration++)
        {
            for (int i = 0; i < length; i++)
            {
                if (_preserveSmoothEndpoints && (i == 0 || i == length - 1))
                {
                    buffer[i] = values[i];
                    continue;
                }

                int previous = Mathf.Max(0, i - 1);
                int next = Mathf.Min(length - 1, i + 1);
                float average = (values[previous] + values[i] + values[next]) / 3f;
                buffer[i] = Mathf.Lerp(values[i], average, _smoothStrength);
            }

            for (int i = 0; i < length; i++)
                values[i] = buffer[i];
        }

        for (int i = 0; i < length; i++)
            SetPower(startIndex + i, values[i]);
    }

    private void ForEachInRange(System.Action<int> action)
    {
        int startIndex;
        int endIndex;
        if (!TryGetRange(out startIndex, out endIndex))
            return;

        for (int i = startIndex; i <= endIndex; i++)
            action?.Invoke(i);
    }

    private bool TryGetRange(out int startIndex, out int endIndex)
    {
        startIndex = 0;
        endIndex = -1;

        int count = _levelPowers.arraySize;
        if (count <= 0)
            return false;

        int startLevel = Mathf.Clamp(_rangeStart, 1, count);
        int endLevel = Mathf.Clamp(_rangeEnd, startLevel, count);
        startIndex = startLevel - 1;
        endIndex = endLevel - 1;
        return true;
    }

    private int GetRangeLength()
    {
        int startIndex;
        int endIndex;
        return TryGetRange(out startIndex, out endIndex) ? endIndex - startIndex + 1 : 0;
    }

    private float GetPower(int index)
    {
        return _levelPowers.GetArrayElementAtIndex(index).floatValue;
    }

    private void SetPower(int index, float value)
    {
        _levelPowers.GetArrayElementAtIndex(index).floatValue = Mathf.Max(MinimumPower, value);
    }

    private void ClampAllPowers()
    {
        for (int i = 0; i < _levelPowers.arraySize; i++)
            SetPower(i, GetPower(i));
    }

    private void SyncToolDefaultsFromList()
    {
        int count = _levelPowers != null ? _levelPowers.arraySize : 0;
        if (count <= 0)
            return;

        _levelCount = count;
        _rangeStart = Mathf.Clamp(_rangeStart, 1, count);
        _rangeEnd = Mathf.Clamp(_rangeEnd, _rangeStart, count);

        float first = GetPower(0);
        float last = GetPower(count - 1);
        _curveStartPower = first;
        _curveEndPower = last;
        _interpolateStartPower = first;
        _interpolateEndPower = last;
    }
}
