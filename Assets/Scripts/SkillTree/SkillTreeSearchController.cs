using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LocalizationSupport;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace SkillTree
{
    public class SkillTreeSearchController : MonoBehaviour
    {
        private sealed class SearchEntry
        {
            public Node Node;
            public string SearchText;
        }

        [Inject] private MainSkillTree _skillTree;
        [Inject] private SkillTreeNodeHighlightService _highlightService;

        [Header("Input")]
        [SerializeField] private TMP_InputField searchInputField;
        [SerializeField] private bool createInputFieldIfMissing;
        [SerializeField] private Vector2 inputFieldSize = new(420f, 40f);
        [SerializeField] private Vector2 inputFieldTopOffset = new(0f, -18f);

        [Header("Search")]
        [SerializeField] private float debounceSeconds = 0.1f;
        [SerializeField] private int searchNodesPerFrame = 128;

        private readonly List<SearchEntry> _entries = new();
        private readonly Dictionary<Node, SearchEntry> _entriesByNode = new();
        private readonly List<string> _terms = new();

        private Coroutine _debounceCoroutine;
        private Coroutine _searchCoroutine;
        private string _pendingQuery = string.Empty;
        private int _searchVersion;

        private void Start()
        {
            EnsureInputField();
            RebuildIndex();

            if (searchInputField != null)
            {
                searchInputField.onValueChanged.AddListener(HandleSearchTextChanged);
                if (!string.IsNullOrWhiteSpace(searchInputField.text))
                    ScheduleSearch(searchInputField.text);
            }

            if (_skillTree != null)
                _skillTree.OnAnyNodeChanged += RefreshNodeIndex;
        }

        private void OnDestroy()
        {
            if (searchInputField != null)
                searchInputField.onValueChanged.RemoveListener(HandleSearchTextChanged);

            if (_skillTree != null)
            {
                _skillTree.OnAnyNodeChanged -= RefreshNodeIndex;
            }

            _highlightService?.ClearHighlights(SkillTreeNodeHighlightLayer.Search);
        }

        private void HandleSearchTextChanged(string query)
        {
            ScheduleSearch(query);
        }

        private void ScheduleSearch(string query)
        {
            _pendingQuery = query ?? string.Empty;

            if (_debounceCoroutine != null)
                StopCoroutine(_debounceCoroutine);

            _debounceCoroutine = StartCoroutine(DebouncedSearch());
        }

        private IEnumerator DebouncedSearch()
        {
            if (debounceSeconds > 0f)
                yield return new WaitForSecondsRealtime(debounceSeconds);

            _debounceCoroutine = null;
            StartSearch(_pendingQuery);
        }

        private void StartSearch(string query)
        {
            if (_searchCoroutine != null)
                StopCoroutine(_searchCoroutine);

            _searchVersion++;
            _searchCoroutine = StartCoroutine(SearchRoutine(query ?? string.Empty, _searchVersion));
        }

        private IEnumerator SearchRoutine(string query, int version)
        {
            ParseTerms(query, _terms);

            if (_terms.Count == 0)
            {
                _highlightService?.ClearHighlights(SkillTreeNodeHighlightLayer.Search);
                _searchCoroutine = null;
                yield break;
            }

            HashSet<Node> matches = new();
            int processedThisFrame = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (version != _searchVersion)
                    yield break;

                SearchEntry entry = _entries[i];
                if (entry.Node != null && IsMatch(entry.SearchText, _terms))
                    matches.Add(entry.Node);

                processedThisFrame++;
                if (searchNodesPerFrame > 0 && processedThisFrame >= searchNodesPerFrame)
                {
                    processedThisFrame = 0;
                    yield return null;
                }
            }

            _highlightService?.SetHighlights(SkillTreeNodeHighlightLayer.Search, matches);
            _searchCoroutine = null;
        }

        private void RebuildIndex()
        {
            _entries.Clear();
            _entriesByNode.Clear();

            if (_skillTree == null)
                return;

            foreach (Node node in _skillTree.EnumerateNodes())
            {
                if (node == null)
                    continue;

                SearchEntry entry = new SearchEntry
                {
                    Node = node,
                    SearchText = BuildNodeSearchText(node)
                };
                _entries.Add(entry);
                _entriesByNode[node] = entry;
            }
        }

        private void RefreshNodeIndex(Node node)
        {
            if (node == null)
                return;

            if (!_entriesByNode.TryGetValue(node, out SearchEntry entry))
            {
                entry = new SearchEntry { Node = node };
                _entries.Add(entry);
                _entriesByNode[node] = entry;
            }

            entry.SearchText = BuildNodeSearchText(node);

            if (searchInputField != null && !string.IsNullOrWhiteSpace(searchInputField.text))
                ScheduleSearch(searchInputField.text);
        }

        private static string BuildNodeSearchText(Node node)
        {
            StringBuilder builder = new StringBuilder(256);

            if (node.ShouldShowTooltipTitle())
                AppendSearchText(builder, node.GetTooltipTitle());

            IReadOnlyList<string> descriptions = node.GetTooltipDescriptions();
            if (descriptions != null)
            {
                for (int i = 0; i < descriptions.Count; i++)
                    AppendSearchText(builder, descriptions[i]);
            }

            return NormalizeForSearch(builder.ToString());
        }

        private static void AppendSearchText(StringBuilder builder, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (builder.Length > 0)
                builder.Append('\n');

            builder.Append(text);
        }

        private static void ParseTerms(string query, List<string> terms)
        {
            terms.Clear();

            if (string.IsNullOrWhiteSpace(query))
                return;

            string[] rawTerms = query.Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < rawTerms.Length; i++)
            {
                string term = NormalizeForSearch(rawTerms[i]);
                if (!string.IsNullOrWhiteSpace(term) && !terms.Contains(term))
                    terms.Add(term);
            }
        }

        private static bool IsMatch(string searchText, IReadOnlyList<string> terms)
        {
            if (string.IsNullOrEmpty(searchText))
                return false;

            for (int i = 0; i < terms.Count; i++)
            {
                if (searchText.IndexOf(terms[i], StringComparison.Ordinal) < 0)
                    return false;
            }

            return true;
        }

        private static string NormalizeForSearch(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            StringBuilder builder = new StringBuilder(text.Length);
            bool insideTag = false;

            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                if (character == '<')
                {
                    insideTag = true;
                    continue;
                }

                if (character == '>')
                {
                    insideTag = false;
                    continue;
                }

                if (!insideTag)
                    builder.Append(char.ToLowerInvariant(character));
            }

            return builder.ToString().Trim();
        }

        private void EnsureInputField()
        {
            if (searchInputField != null || !createInputFieldIfMissing)
                return;

            Canvas canvas = CreateSearchCanvas();
            searchInputField = CreateSearchInputField(canvas.transform);
        }

        private Canvas CreateSearchCanvas()
        {
            GameObject canvasObject = new GameObject(
                "SkillTreeSearchCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (EventSystem.current == null)
            {
                GameObject eventSystemObject = new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule));
                DontDestroyOnLoad(eventSystemObject);
            }

            return canvas;
        }

        private TMP_InputField CreateSearchInputField(Transform parent)
        {
            GameObject root = new GameObject(
                "SkillTreeSearchInput",
                typeof(RectTransform),
                typeof(Image),
                typeof(TMP_InputField));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = inputFieldTopOffset;
            rootRect.sizeDelta = inputFieldSize;

            Image background = root.GetComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.06f, 0.86f);

            GameObject textArea = new GameObject(
                "Text Area",
                typeof(RectTransform),
                typeof(RectMask2D));
            textArea.transform.SetParent(root.transform, false);

            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(14f, 5f);
            textAreaRect.offsetMax = new Vector2(-14f, -5f);

            TextMeshProUGUI placeholder = CreateInputText(
                "Placeholder",
                textArea.transform,
                GameLocalization.GetGameUI("skillTree.search.placeholder", "Search nodes..."),
                new Color(1f, 1f, 1f, 0.38f));
            placeholder.fontStyle = FontStyles.Italic;

            TextMeshProUGUI text = CreateInputText(
                "Text",
                textArea.transform,
                string.Empty,
                new Color(1f, 1f, 1f, 0.95f));

            TMP_InputField inputField = root.GetComponent<TMP_InputField>();
            inputField.textViewport = textAreaRect;
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.characterLimit = 80;
            inputField.selectionColor = new Color(1f, 0.8f, 0.2f, 0.45f);
            inputField.caretColor = new Color(1f, 0.85f, 0.25f, 1f);

            return inputField;
        }

        private static TextMeshProUGUI CreateInputText(string name, Transform parent, string text, Color color)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.color = color;
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            return label;
        }

    }
}
