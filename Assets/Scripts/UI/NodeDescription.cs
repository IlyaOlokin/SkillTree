using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NodeDescription : MonoBehaviour
{
    [SerializeField] private TMP_Text description;
    [SerializeField] private RectTransform heightOffsetSource;
    private RectTransform selfRectTransform;

    private void Awake()
    {
        selfRectTransform = (RectTransform)transform;
    }

    public void SetText(string text)
    {
        description.text = text;
    }

    public void RefreshLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(selfRectTransform);
    }

    public float GetChildHeightOffset()
    {
        return heightOffsetSource.rect.height;
    }
}
