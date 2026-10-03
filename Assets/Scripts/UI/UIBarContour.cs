using UnityEngine;
using UnityEngine.UI;

/// <summary>Supplies atlas-independent coordinates for the UI/Bar Contour shader.</summary>
[AddComponentMenu("UI/Effects/Bar Contour Coordinates")]
[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public sealed class UIBarContour : BaseMeshEffect
{
    protected override void OnEnable()
    {
        base.OnEnable();
        EnableCoordinates();
    }

    protected override void OnCanvasHierarchyChanged()
    {
        base.OnCanvasHierarchyChanged();
        EnableCoordinates();
        if (graphic != null)
            graphic.SetVerticesDirty();
    }

    private void EnableCoordinates()
    {
        if (graphic != null && graphic.canvas != null)
            graphic.canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
            return;

        EnableCoordinates();
        UIVertex vertex = default;
        vh.PopulateUIVertex(ref vertex, 0);
        Vector2 min = vertex.position;
        Vector2 max = min;
        for (int i = 1; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            min = Vector2.Min(min, vertex.position);
            max = Vector2.Max(max, vertex.position);
        }

        Vector2 size = max - min;
        size.x = Mathf.Max(size.x, 0.00001f);
        size.y = Mathf.Max(size.y, 0.00001f);
        // UV1.w: 1 = no fill edge, 2 = right, 3 = left, 4 = top, 5 = bottom.
        // Keep the same edge at fillAmount == 1 to avoid a pop on reaching full.
        float fillEdge = 1f;
        Image image = graphic as Image;
        if (image != null && image.type == Image.Type.Filled)
        {
            if (image.fillMethod == Image.FillMethod.Horizontal)
                fillEdge = image.fillOrigin == (int)Image.OriginHorizontal.Left ? 2f : 3f;
            else if (image.fillMethod == Image.FillMethod.Vertical)
                fillEdge = image.fillOrigin == (int)Image.OriginVertical.Bottom ? 4f : 5f;
        }
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            vertex.uv1 = new Vector4((vertex.position.x - min.x) / size.x,
                (vertex.position.y - min.y) / size.y, size.x / size.y, fillEdge);
            vh.SetUIVertex(vertex, i);
        }
    }
}
