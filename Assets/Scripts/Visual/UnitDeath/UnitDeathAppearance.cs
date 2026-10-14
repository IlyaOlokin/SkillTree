using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Visual.UnitDeath
{
    /// <summary>Value-only copy of the rendered body. No source unit or transform survives capture.</summary>
    public sealed class UnitDeathAppearance
    {
        private readonly struct Property
        {
            public readonly int Id;
            public readonly ShaderPropertyType Type;
            public Property(int id, ShaderPropertyType type) { Id = id; Type = type; }
        }

        private static readonly Dictionary<Shader, Property[]> Properties = new();
        private readonly Property[] properties;
        private readonly Vector4[] values;
        public readonly Matrix4x4 LocalToWorld, WorldToLocal;
        public readonly UnitShapeGeometry Geometry;
        public readonly int Layer;
        public readonly float SourceTime, Diameter;
        public readonly Color Glow, Core;

        private UnitDeathAppearance(UnitShapeView source, Material material, MaterialPropertyBlock overrides)
        {
            Geometry = source.GetGeometry();
            // GetGeometry contains a Transform: explicitly replace it with a value-only record.
            var geometry = Geometry;
            Geometry = new UnitShapeGeometry(geometry.Shape, geometry.Size, geometry.Radius,
                geometry.TriangleSize, geometry.Rotation, geometry.MirrorX, geometry.MirrorY,
                geometry.LineWidth, null, geometry.SortingLayer, geometry.SortingOrder);
            LocalToWorld = source.transform.localToWorldMatrix;
            WorldToLocal = LocalToWorld.inverse;
            Layer = source.gameObject.layer;
            SourceTime = Time.time;
            if (!Properties.TryGetValue(material.shader, out properties))
            {
                var definitions = new List<Property>();
                for (int i = 0; i < material.shader.GetPropertyCount(); i++)
                {
                    var type = material.shader.GetPropertyType(i);
                    if (type == ShaderPropertyType.Texture || type == ShaderPropertyType.Int) continue;
                    definitions.Add(new Property(material.shader.GetPropertyNameId(i), type));
                }
                properties = definitions.ToArray();
                Properties.Add(material.shader, properties);
            }
            values = new Vector4[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                var p = properties[i];
                bool overridden = overrides.HasProperty(p.Id);
                if (p.Type == ShaderPropertyType.Color)
                    values[i] = overridden ? (Vector4)overrides.GetColor(p.Id) : (Vector4)material.GetColor(p.Id);
                else if (p.Type == ShaderPropertyType.Vector)
                    values[i] = overridden ? overrides.GetVector(p.Id) : material.GetVector(p.Id);
                else
                    values[i] = new Vector4(overridden ? overrides.GetFloat(p.Id) : material.GetFloat(p.Id), 0, 0, 0);
            }
            Glow = ColorValue("_Color", source.GlowColor);
            Core = ColorValue("_CoreColor", source.CoreColor);
            // Visible body bounds, including polygon geometry and nonuniform world scaling.
            Bounds bounds = new Bounds(ToWorld(Vector2.zero), Vector3.zero);
            for (int i = 0; i < 48; i++)
            {
                Geometry.Sample(i / 48f, out Vector2 point, out _);
                bounds.Encapsulate(ToWorld(point));
            }
            Diameter = Mathf.Max(0.01f, Mathf.Max(bounds.size.x, bounds.size.y));
        }

        public static UnitDeathAppearance Capture(UnitShapeView source, MaterialPropertyBlock scratch)
        {
            if (source == null) return null;
            var renderer = source.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null ||
                renderer.sharedMaterial.shader.name != "SkillTree/Unit Shape SDF") return null;
            scratch.Clear();
            renderer.GetPropertyBlock(scratch);
            return new UnitDeathAppearance(source, renderer.sharedMaterial, scratch);
        }

        public void Apply(MaterialPropertyBlock target)
        {
            target.Clear();
            for (int i = 0; i < properties.Length; i++)
            {
                var p = properties[i];
                if (p.Type == ShaderPropertyType.Color) target.SetColor(p.Id, (Color)values[i]);
                else if (p.Type == ShaderPropertyType.Vector) target.SetVector(p.Id, values[i]);
                else target.SetFloat(p.Id, values[i].x);
            }
            target.SetFloat("_DeathSourceTime", SourceTime);
        }

        private Color ColorValue(string name, Color fallback)
        {
            int id = Shader.PropertyToID(name);
            for (int i = 0; i < properties.Length; i++)
                if (properties[i].Id == id) return (Color)values[i];
            return fallback;
        }

        public Vector3 ToWorld(Vector2 canonicalPoint)
        {
            float angle = -Geometry.Rotation * Mathf.Deg2Rad;
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            Vector2 p = new(c * canonicalPoint.x - s * canonicalPoint.y,
                s * canonicalPoint.x + c * canonicalPoint.y);
            if (Geometry.MirrorX) p.x = -p.x;
            if (Geometry.MirrorY) p.y = -p.y;
            return LocalToWorld.MultiplyPoint3x4(new Vector3(p.x, p.y, 0) * (Geometry.Size * 0.5f));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearCache() => Properties.Clear();
    }
}
