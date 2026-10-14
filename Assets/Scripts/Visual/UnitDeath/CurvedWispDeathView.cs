using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Visual.UnitDeath
{
    public sealed class CurvedWispDeathView : UnitDeathView
    {
        private const int RibbonSegments = 24;
        private const float SurfaceExtent = 2.6f;
        private struct Fleck
        {
            public Vector3 Origin, Velocity;
            public float Size, Angle, Spin, Delay, Lifetime;
            public bool Square;
        }
        private Mesh surfaceMesh, ribbonMesh;
        private MeshRenderer surface, ribbon;
        private MaterialPropertyBlock surfaceBlock;
        private MaterialPropertyBlock ribbonBlock;
        private readonly List<Vector3> vertices = new(110);
        private readonly List<Vector2> uv = new(110);
        private readonly List<Vector2> kinds = new(110);
        private readonly List<Color> colors = new(110);
        private readonly List<int> indices = new(240);
        private readonly Fleck[] flecks = new Fleck[12];
        private UnitDeathAppearance appearance;
        private Vector3 origin, control1, control2, end;
        private float width, trailLength;
        private int fragmentCount;
        private Color trailColor;

        public override void Begin(UnitDeathAppearance snapshot, DeathVisualProfile profile, int seed, Camera camera)
        {
            appearance = snapshot;
            EnsureMeshes();
            gameObject.layer = snapshot.Layer;
            surface.gameObject.layer = ribbon.gameObject.layer = snapshot.Layer;
            surface.sharedMaterial = profile.surfaceMaterial;
            ribbon.sharedMaterial = profile.ribbonMaterial;
            surface.sortingLayerID = ribbon.sortingLayerID = snapshot.Geometry.SortingLayer;
            surface.sortingOrder = snapshot.Geometry.SortingOrder;
            ribbon.sortingOrder = snapshot.Geometry.SortingOrder + 1;
            snapshot.Apply(surfaceBlock);
            snapshot.Apply(ribbonBlock);

            var random = new System.Random(seed);
            float Next(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
            float d = snapshot.Diameter;
            float side = random.Next(2) == 0 ? -1 : 1;
            origin = snapshot.LocalToWorld.MultiplyPoint3x4(Vector3.zero);
            end = origin + new Vector3(side * Next(profile.sidewaysRange.x, profile.sidewaysRange.y),
                Next(profile.riseRange.x, profile.riseRange.y), 0) * d;
            control1 = origin + new Vector3(-side * Next(0.1f, 0.24f), Next(0.18f, 0.3f), 0) * d;
            control2 = origin + new Vector3(side * Next(0.2f, 0.43f), Next(0.42f, 0.65f), 0) * d;
            end = ClampToBattle(end, camera);
            control1 = ClampToBattle(control1, camera);
            control2 = ClampToBattle(control2, camera);
            width = Mathf.Max(0.001f, profile.ribbonWidth) * d * Next(0.85f, 1.15f);
            trailLength = Mathf.Clamp(profile.trailLength, 0.1f, 0.7f);
            trailColor = Color.white;
            surfaceBlock.SetFloat("_DeathSeed", Next(0, 100));
            surfaceBlock.SetFloat("_DeathGapCount", random.Next(Mathf.Clamp(profile.gapRange.x, 1, 16),
                Mathf.Clamp(Mathf.Max(profile.gapRange.x, profile.gapRange.y), 1, 16) + 1));
            fragmentCount = random.Next(Mathf.Clamp(profile.fragmentRange.x, 0, 12),
                Mathf.Clamp(Mathf.Max(profile.fragmentRange.x, profile.fragmentRange.y), 0, 12) + 1);
            for (int i = 0; i < fragmentCount; i++)
            {
                snapshot.Geometry.Sample((i + Next(0.05f, 0.95f)) / Mathf.Max(1, fragmentCount),
                    out Vector2 point, out Vector2 normal);
                Vector3 start = snapshot.ToWorld(point);
                Vector3 direction = (snapshot.ToWorld(point + normal * 0.1f) - start).normalized;
                flecks[i] = new Fleck {
                    Origin = start, Velocity = direction * d * Next(0.06f, 0.18f),
                    Size = d * Next(0.012f, 0.026f), Angle = Next(0, 360), Spin = Next(-120, 120),
                    Delay = Next(0.025f, 0.15f), Lifetime = Next(0.32f, 0.58f), Square = random.Next(2) == 0
                };
            }

            var positions = new Vector3[4];
            Vector2[] corners = { new(-1,-1), new(-1,1), new(1,1), new(1,-1) };
            var texcoords = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                Vector2 point = corners[i] * SurfaceExtent;
                positions[i] = transform.InverseTransformPoint(snapshot.LocalToWorld.MultiplyPoint3x4(
                    new Vector3(point.x, point.y, 0) * snapshot.Geometry.Size * 0.5f));
                texcoords[i] = (point + Vector2.one) * 0.5f;
            }
            surfaceMesh.vertices = positions;
            surfaceMesh.uv = texcoords;
            surfaceMesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            surfaceMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            surfaceMesh.RecalculateBounds();
            ribbon.SetPropertyBlock(ribbonBlock);
            RenderAt(0);
        }

        private static Vector3 ClampToBattle(Vector3 point, Camera camera)
        {
            if (camera == null) return point;
            Vector3 viewport = camera.WorldToViewportPoint(point);
            viewport.x = Mathf.Clamp(viewport.x, 0.07f, 0.93f);
            viewport.y = Mathf.Clamp(viewport.y, 0.12f, 0.9f);
            return camera.ViewportToWorldPoint(viewport);
        }

        private Vector3 Path(float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1 - t;
            return u*u*u*origin + 3*u*u*t*control1 + 3*u*t*t*control2 + t*t*t*end;
        }

        public override void RenderAt(float normalizedTime)
        {
            if (appearance == null) return;
            float t = Mathf.Clamp01(normalizedTime);
            if (t >= 1) { Clear(); return; }
            float movement = 1 - Mathf.Pow(1 - t, 3);
            Vector3 head = Path(movement);
            Vector3 offset = appearance.WorldToLocal.MultiplyPoint3x4(head) * (2 / appearance.Geometry.Size);
            surfaceBlock.SetVector("_DeathCoreOffset", new Vector4(offset.x, offset.y, 0, 0));
            surfaceBlock.SetFloat("_DeathProgress", t);
            surface.SetPropertyBlock(surfaceBlock);
            vertices.Clear(); uv.Clear(); kinds.Clear(); colors.Clear(); indices.Clear();

            float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.62f, 1, t));
            float tail = Mathf.Max(0, movement - trailLength * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.72f, 1, t))));
            for (int i = 0; i <= RibbonSegments; i++)
            {
                float along = i / (float)RibbonSegments;
                float p = Mathf.Lerp(tail, movement, along);
                Vector3 position = Path(p);
                Vector3 direction = (Path(Mathf.Min(1, p + 0.005f)) - Path(Mathf.Max(0, p - 0.005f))).normalized;
                Vector3 normal = new(-direction.y, direction.x, 0);
                float halfWidth = width * Mathf.Pow(along, 0.65f) * fade;
                Color color = trailColor;
                color.a = fade * Mathf.SmoothStep(0, 1, along) * Mathf.Clamp01(t * 16);
                AddVertex(position - normal * halfWidth, new Vector2(0, along), 2, color);
                AddVertex(position + normal * halfWidth, new Vector2(1, along), 2, color);
                if (i > 0)
                {
                    int a = i * 2;
                    indices.Add(a-2); indices.Add(a-1); indices.Add(a);
                    indices.Add(a); indices.Add(a-1); indices.Add(a+1);
                }
            }
            for (int i = 0; i < fragmentCount; i++)
            {
                var f = flecks[i];
                float progress = (t - f.Delay) / f.Lifetime;
                if (progress < 0 || progress >= 1) continue;
                float opacity = Mathf.Clamp01(progress * 8) * (1 - progress) * fade;
                float radius = f.Size * (1 - progress * 0.65f);
                Vector3 position = f.Origin + f.Velocity * progress;
                float a = (f.Angle + f.Spin * progress) * Mathf.Deg2Rad;
                Vector3 right = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * radius;
                Vector3 up = new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0) * radius;
                Color color = Color.white; color.a = opacity;
                int start = vertices.Count;
                AddVertex(position-right-up, new Vector2(0,0), f.Square ? 1 : 0, color);
                AddVertex(position-right+up, new Vector2(0,1), f.Square ? 1 : 0, color);
                AddVertex(position+right+up, new Vector2(1,1), f.Square ? 1 : 0, color);
                AddVertex(position+right-up, new Vector2(1,0), f.Square ? 1 : 0, color);
                indices.Add(start); indices.Add(start+1); indices.Add(start+2);
                indices.Add(start); indices.Add(start+2); indices.Add(start+3);
            }
            ribbonMesh.Clear(false);
            ribbonMesh.SetVertices(vertices); ribbonMesh.SetUVs(0, uv); ribbonMesh.SetUVs(1, kinds);
            ribbonMesh.SetColors(colors); ribbonMesh.SetTriangles(indices, 0);
            ribbonMesh.RecalculateBounds();
        }

        private void AddVertex(Vector3 world, Vector2 texcoord, float kind, Color color)
        {
            vertices.Add(transform.InverseTransformPoint(world));
            uv.Add(texcoord); kinds.Add(new Vector2(kind, 0)); colors.Add(color);
        }

        private void EnsureMeshes()
        {
            if (surfaceMesh != null) return;
            surfaceBlock = new MaterialPropertyBlock();
            ribbonBlock = new MaterialPropertyBlock();
            surfaceMesh = new Mesh { name = "Death Surface", hideFlags = HideFlags.DontSave };
            ribbonMesh = new Mesh { name = "Death Ribbon and Flecks", hideFlags = HideFlags.DontSave };
            ribbonMesh.MarkDynamic();
            surface = CreateRenderer("Procedural Body and Core", surfaceMesh);
            ribbon = CreateRenderer("Procedural Ribbon and Flecks", ribbonMesh);
        }

        private MeshRenderer CreateRenderer(string name, Mesh mesh)
        {
            var child = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            child.transform.SetParent(transform, false);
            child.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }

        public override void Clear()
        {
            appearance = null;
            if (ribbonMesh != null) ribbonMesh.Clear(false);
            base.Clear();
        }

        private void OnDestroy()
        {
            if (surfaceMesh != null) Destroy(surfaceMesh);
            if (ribbonMesh != null) Destroy(ribbonMesh);
        }
    }
}
