using UnityEngine;
using UnityEngine.UI;

// Geometry keeps both action icons legible without font glyph dependencies.
public sealed class BattlePlaybackIcon : MaskableGraphic
{
    [SerializeField] private bool showPlay;
    public bool ShowPlay
    {
        get => showPlay;
        set
        {
            if (showPlay == value) return;
            showPlay = value;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (showPlay)
        {
            vh.AddVert(new Vector3(r.xMin, r.yMin), color, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin, r.yMax), color, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax, r.center.y), color, Vector2.zero);
            vh.AddTriangle(0, 1, 2);
        }
        else
        {
            AddBar(vh, r.xMin, r.xMin + r.width * 0.32f, r);
            AddBar(vh, r.xMax - r.width * 0.32f, r.xMax, r);
        }
    }

    private void AddBar(VertexHelper vh, float left, float right, Rect r)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(left, r.yMin), color, Vector2.zero);
        vh.AddVert(new Vector3(left, r.yMax), color, Vector2.zero);
        vh.AddVert(new Vector3(right, r.yMax), color, Vector2.zero);
        vh.AddVert(new Vector3(right, r.yMin), color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
