#ifndef SKILLTREE_PAINTERLY_COLOR_INCLUDED
#define SKILLTREE_PAINTERLY_COLOR_INCLUDED

float SkillTreePaintHash(float2 cell)
{
    float3 h = frac(float3(cell.x, cell.y, cell.x) * 0.1031);
    h += dot(h, h.yzx + 33.33);
    return frac((h.x + h.y) * h.z);
}

float SkillTreePaintPatch(float2 p, float size)
{
    float2 q = float2(p.x + p.y * 0.55, p.y - p.x * 0.25) / max(size, 0.001);
    q += float2(sin(q.y * 0.73), sin(q.x * 0.61)) * 0.32;
    float triangleIndex = step(frac(q.y), frac(q.x));
    return SkillTreePaintHash(floor(q) + triangleIndex * float2(19.19, 47.47));
}

float SkillTreePaintEnergy(float value, float patchValue, float strength, float steps, float variation)
{
    steps = max(round(steps), 2.0);
    float varied = max(value, 0.0) * exp2((patchValue - 0.5) * variation);
    float band = floor(log2(1.0 + varied) * steps + 0.5);
    return lerp(value, exp2(band / steps) - 1.0, saturate(strength));
}

// Scale RGB together to retain the damage tint and HDR headroom.
float3 SkillTreePaintColor(float3 color, float patchValue, float strength, float steps, float variation)
{
    float peak = max(max(color.r, color.g), color.b);
    float painted = SkillTreePaintEnergy(peak, patchValue, strength, steps, variation);
    return color * (painted / max(peak, 0.00001));
}

// Process the texture BEFORE particle tint / lifetime alpha from VFX Graph.
void PainterlyTexture_float(float4 Texel, float2 UV, float Strength, float Steps,
    float PatchSize, float Variation, out float4 Painted)
{
    float patchValue = SkillTreePaintPatch(UV * 2.0 - 1.0, PatchSize);
    Painted.rgb = SkillTreePaintColor(Texel.rgb, patchValue, Strength, Steps, Variation);
    Painted.a = saturate(SkillTreePaintEnergy(Texel.a, patchValue, Strength, Steps, Variation));
}

// VFX planar primitives supply white mesh vertex colors. Bind the actual
// per-particle color and alpha explicitly through the graph's input slots.
void ParticleTint_float(float4 Texel, float3 Tint, float Opacity,
    out float4 RGBA, out float3 RGB, out float A)
{
    RGBA = Texel * float4(Tint, Opacity);
    RGB = RGBA.rgb;
    A = RGBA.a;
}

#endif
