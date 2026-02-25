using SkillTree;
using System.Collections;
using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    [SerializeField] protected NodeDescription NodeDescriptionPrefab;
    [SerializeField] private Vector2 descriptionOffset = new Vector2(0f, 80f);
    [SerializeField] private float childHeightOffsetMultiplier = 1f;
    [SerializeField] private float childHeightOffsetPadding = 0f;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Camera worldCamera;

    private NodeDescription NodeDescriptionInstance;
    private RectTransform canvasRectTransform;
    private RectTransform descriptionRectTransform;

    private void Awake()
    {
        canvasRectTransform = (RectTransform)targetCanvas.transform;

        NodeDescriptionInstance = Instantiate(NodeDescriptionPrefab, transform, false);
        descriptionRectTransform = (RectTransform)NodeDescriptionInstance.transform;
        NodeDescriptionInstance.gameObject.SetActive(false);
    }

    public void DisplayNodeDescription(Node node)
    {
        NodeDescriptionInstance.SetText(node.GetDescription());
        
        NodeDescriptionInstance.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        NodeDescriptionInstance.RefreshLayout();
        SetDescriptionPosition(node.transform.position);
    }

    public void HideNodeDescription()
    {
        NodeDescriptionInstance.gameObject.SetActive(false);
    }

    private void SetDescriptionPosition(Vector3 worldPosition)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);
        Camera uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : targetCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                screenPoint,
                uiCamera,
                out Vector2 localPoint))
        {
            float childHeightOffset = NodeDescriptionInstance.GetChildHeightOffset() * childHeightOffsetMultiplier
                + childHeightOffsetPadding;
            Vector2 totalOffset = descriptionOffset + new Vector2(0f, childHeightOffset);
            descriptionRectTransform.anchoredPosition = localPoint + totalOffset;
        }
    }
}
