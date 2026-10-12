using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace SkillTree
{
    /// <summary>Animated presentation derived solely from the authored SpriteShape spline.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteShapeController))]
    public sealed class BonusZoneAuraVisual : MonoBehaviour
    {
        [SerializeField] private Material borderMaterial;
        [SerializeField] private Color tint = new Color(0.12f, 0.94f, 0.92f, 1f);
        [SerializeField, Range(0f, 0.3f)] private float fillOpacity = 0.065f;
        [SerializeField, Range(1f, 5f)] private float fillContrast = 2.6f;
        [SerializeField, Range(0.1f, 5f)] private float interiorDotDensity = 1.8f;
        [SerializeField, Range(0.003f, 0.06f)] private float interiorDotRadius = 0.018f;
        [SerializeField, Range(0f, 1f)] private float interiorDotIntensity = 0.5f;
        [SerializeField, Range(0f, 1f)] private float interiorDotSpeed = 0.12f;
        [SerializeField, Min(0.01f)] private float noiseScale = 0.8f;
        [SerializeField] private Vector2 flowVelocity = new Vector2(0.035f, 0.022f);
        [SerializeField, Min(0.001f)] private float contourWidth = 0.018f;
        [SerializeField, Min(0.01f)] private float haloWidth = 0.16f;
        [SerializeField, Range(0f, 4f)] private float contourBrightness = 1.25f;
        [SerializeField, Range(0f, 1f)] private float haloBrightness = 0.22f;
        [SerializeField, Min(0.1f)] private float sparkDensity = 10f;
        [SerializeField] private float sparkSpeed = 0.12f;
        [SerializeField, Range(0f, 3f)] private float sparkIntensity = 0.8f;
        [SerializeField] private float phaseSeed = 1.7f;

        private SpriteShapeController shape;
        private GameObject borderObject;
        private MeshRenderer borderRenderer;
        private Mesh borderMesh;
        private MaterialPropertyBlock properties;
        private MaterialPropertyBlock originalFillProperties;
        private int splineHash;
        private bool dirty;

        private void OnEnable()
        {
            shape = GetComponent<SpriteShapeController>();
            properties = new MaterialPropertyBlock();
            originalFillProperties = new MaterialPropertyBlock();
            shape.spriteShapeRenderer.GetPropertyBlock(originalFillProperties);
            borderObject = new GameObject("Bonus Zone Aura Contour") { hideFlags = HideFlags.HideAndDontSave, layer = gameObject.layer };
            borderObject.transform.SetParent(transform, false);
            borderMesh = new Mesh { name = "BonusZone spline contour", hideFlags = HideFlags.HideAndDontSave };
            borderObject.AddComponent<MeshFilter>().sharedMesh = borderMesh;
            borderRenderer = borderObject.AddComponent<MeshRenderer>();
            borderRenderer.shadowCastingMode = ShadowCastingMode.Off;
            borderRenderer.receiveShadows = false;
            Refresh();
        }

        private void OnValidate() => dirty = true;

        private void Update()
        {
            // Nine-point authored zones are cheap to hash; idle animation stays entirely on the GPU.
            if (shape != null && (dirty || shape.spline.GetHashCode() != splineHash)) Refresh();
        }

        public void Refresh()
        {
            if (shape == null || borderMesh == null) { dirty = true; return; }
            dirty = false;
            splineHash = shape.spline.GetHashCode();
            var spline = shape.spline;
            int count = spline.GetPointCount();
            borderMesh.Clear();
            if (spline.isOpenEnded || count < 3) { Warn("requires a simple closed spline with at least three points"); return; }
            var points = new Vector2[count];
            float perimeter = 0f;
            for (int i = 0; i < count; i++)
            {
                points[i] = spline.GetPosition(i);
                if (spline.GetTangentMode(i) != ShapeTangentMode.Linear) { Warn("currently supports linear spline segments only"); return; }
            }
            for (int i = 0; i < count; i++)
            {
                float length = Vector2.Distance(points[i], points[(i + 1) % count]);
                if (length < 0.0001f) { Warn("contains a duplicate point or zero-length edge"); return; }
                perimeter += length;
                for (int j = i + 2; j < count; j++)
                {
                    if (i == 0 && j == count - 1) continue;
                    if (Intersects(points[i], points[(i + 1) % count], points[j], points[(j + 1) % count]))
                    { Warn("contains intersecting edges"); return; }
                }
            }
            var vertices = new Vector3[(count + 1) * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[count * 6];
            float distance = 0f;
            float width = Mathf.Max(haloWidth, contourWidth * 3f);
            for (int i = 0; i <= count; i++)
            {
                int index = i % count;
                Vector2 incoming = (points[index] - points[(index + count - 1) % count]).normalized;
                Vector2 outgoing = (points[(index + 1) % count] - points[index]).normalized;
                Vector2 normal = new Vector2(-outgoing.y, outgoing.x);
                Vector2 join = new Vector2(-incoming.y, incoming.x) + normal;
                if (join.sqrMagnitude < 0.0001f) join = normal;
                join.Normalize();
                // Bounded joins prevent long spikes at acute corners, independently of winding.
                Vector2 offset = join * Mathf.Min(width / Mathf.Max(0.2f, Vector2.Dot(join, normal)), width * 3f);
                vertices[i * 2] = points[index] - offset;
                vertices[i * 2 + 1] = points[index] + offset;
                uv[i * 2] = new Vector2(distance / perimeter, -width);
                uv[i * 2 + 1] = new Vector2(distance / perimeter, width);
                if (i < count)
                {
                    int v = i * 2, t = i * 6;
                    triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                    triangles[t + 3] = v + 1; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
                    distance += Vector2.Distance(points[index], points[(index + 1) % count]);
                }
            }
            borderMesh.vertices = vertices;
            borderMesh.uv = uv;
            borderMesh.triangles = triangles;
            borderMesh.RecalculateBounds();
            borderRenderer.sharedMaterial = borderMaterial;
            borderRenderer.sortingLayerID = shape.spriteShapeRenderer.sortingLayerID;
            borderRenderer.sortingOrder = shape.spriteShapeRenderer.sortingOrder + 1;
            properties.Clear();
            properties.SetColor("_Tint", tint);
            properties.SetFloat("_Opacity", fillOpacity);
            properties.SetFloat("_FillContrast", fillContrast);
            properties.SetFloat("_DotDensity", interiorDotDensity);
            properties.SetFloat("_DotRadius", interiorDotRadius);
            properties.SetFloat("_DotIntensity", interiorDotIntensity);
            properties.SetFloat("_DotSpeed", interiorDotSpeed);
            properties.SetFloat("_NoiseScale", noiseScale);
            properties.SetVector("_Flow", new Vector4(flowVelocity.x, flowVelocity.y, -flowVelocity.y * 0.7f, flowVelocity.x * 0.8f));
            properties.SetFloat("_Phase", phaseSeed);
            shape.spriteShapeRenderer.SetPropertyBlock(properties);
            properties.SetFloat("_LineWidth", contourWidth);
            properties.SetFloat("_HaloWidth", width);
            properties.SetFloat("_Brightness", contourBrightness);
            properties.SetFloat("_HaloBrightness", haloBrightness);
            properties.SetFloat("_SparkCount", Mathf.Max(1f, Mathf.Round(perimeter * sparkDensity)));
            properties.SetFloat("_SparkSpeed", sparkSpeed / perimeter);
            properties.SetFloat("_SparkIntensity", sparkIntensity);
            borderRenderer.SetPropertyBlock(properties);
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        private static bool Intersects(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float denominator = Cross(b - a, d - c);
            if (Mathf.Abs(denominator) < 0.00001f)
                return Mathf.Abs(Cross(c - a, b - a)) < 0.00001f &&
                       Mathf.Max(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x)) <= Mathf.Min(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x)) &&
                       Mathf.Max(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y)) <= Mathf.Min(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y));
            float u = Cross(c - a, d - c) / denominator;
            float v = Cross(c - a, b - a) / denominator;
            return u >= 0f && u <= 1f && v >= 0f && v <= 1f;
        }
        private void Warn(string message) => Debug.LogWarning("BonusZone aura " + message + ".", this);

        private void OnDisable()
        {
            if (shape != null && originalFillProperties != null) shape.spriteShapeRenderer.SetPropertyBlock(originalFillProperties);
            Release(borderObject);
            Release(borderMesh);
            borderObject = null;
            borderMesh = null;
        }
        private static void Release(Object resource)
        {
            if (resource == null) return;
            if (Application.isPlaying) Destroy(resource); else DestroyImmediate(resource);
        }
    }
}
