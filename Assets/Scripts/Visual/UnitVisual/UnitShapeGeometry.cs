using UnityEngine;

namespace Visual
{
    // Geometry in the same [-1,1] coordinates used by UnitShapeSdf.shader.
    public readonly struct UnitShapeGeometry
    {
        public readonly UnitShapeType Shape;
        public readonly float Size, Radius, TriangleSize, Rotation, LineWidth;
        public readonly bool MirrorX, MirrorY;
        public readonly Transform Transform;
        public readonly int SortingLayer, SortingOrder;

        public UnitShapeGeometry(UnitShapeType shape, float size, float radius, float triangleSize,
            float rotation, bool mirrorX, bool mirrorY, float lineWidth, Transform transform,
            int sortingLayer, int sortingOrder)
        {
            Shape = shape; Size = size; Radius = radius; TriangleSize = triangleSize;
            Rotation = rotation; MirrorX = mirrorX; MirrorY = mirrorY; LineWidth = lineWidth;
            Transform = transform; SortingLayer = sortingLayer; SortingOrder = sortingOrder;
        }

        public void Sample(float t, out Vector2 point, out Vector2 normal)
        {
            t = Mathf.Repeat(t, 1f);
            if (Shape == UnitShapeType.Circle)
            {
                float angle = t * Mathf.PI * 2f;
                normal = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                point = normal * Radius;
            }
            else if (Shape == UnitShapeType.Square)
            {
                float h = Radius * 0.89f;
                float side = t * 4f, u = side - Mathf.Floor(side);
                switch ((int)side)
                {
                    case 0: point = Vector2.Lerp(new Vector2(-h,h), new Vector2(h,h),u); normal = Vector2.up; break;
                    case 1: point = Vector2.Lerp(new Vector2(h,h), new Vector2(h,-h),u); normal = Vector2.right; break;
                    case 2: point = Vector2.Lerp(new Vector2(h,-h), new Vector2(-h,-h),u); normal = Vector2.down; break;
                    default: point = Vector2.Lerp(new Vector2(-h,-h), new Vector2(-h,h),u); normal = Vector2.left; break;
                }
            }
            else
            {
                float r = Radius * 1.52f * TriangleSize;
                Vector2 a = new Vector2(0,r), b = new Vector2(-0.8660254f*r,-0.5f*r), c = new Vector2(0.8660254f*r,-0.5f*r);
                float side = t * 3f, u = side - Mathf.Floor(side);
                Vector2 from = side < 1f ? a : side < 2f ? b : c;
                Vector2 to = side < 1f ? b : side < 2f ? c : a;
                point = Vector2.Lerp(from,to,u);
                Vector2 edge = to-from;
                normal = new Vector2(edge.y,-edge.x).normalized;
            }
        }

        public Vector3 ToWorld(Vector2 shapePoint)
        {
            // Invert shader-coordinate rotation, then the mirrors.
            float angle = -Rotation * Mathf.Deg2Rad;
            float s = Mathf.Sin(angle), c = Mathf.Cos(angle);
            Vector2 p = new Vector2(c*shapePoint.x-s*shapePoint.y,s*shapePoint.x+c*shapePoint.y);
            if (MirrorX) p.x = -p.x;
            if (MirrorY) p.y = -p.y;
            return Transform.TransformPoint(new Vector3(p.x,p.y,0f) * (Size * 0.5f));
        }
    }
}
