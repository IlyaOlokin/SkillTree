Shader "SkillTree/Unit Shape SDF"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _ShapeType ("Shape Type", Float) = 0
        [HDR] _Color ("Glow Color", Color) = (3.2, 1.35, 0.12, 1)
        [HDR] _CoreColor ("Core Color", Color) = (1.0, 0.92, 0.82, 1)
        _FillColor ("Fill Color", Color) = (0.03, 0.024, 0.012, 0.78)

        _ShapeRadius ("Shape Radius", Range(0.2, 0.8)) = 0.48
        _TriangleSize ("Triangle Size", Range(0.45, 1.2)) = 0.72
        _Softness ("Softness", Range(0.001, 0.05)) = 0.008
        _EdgeFade ("Quad Edge Fade", Range(0.001, 0.2)) = 0.05

        _MainLineWidth ("Main Line Width", Range(0.002, 0.12)) = 0.028
        _InnerRingInset ("Inner Ring Inset", Range(0, 0.35)) = 0.12
        _InnerRingWidth ("Inner Ring Width", Range(0.001, 0.08)) = 0.014
        _OuterRingOffset ("Outer Ring Offset", Range(0, 0.35)) = 0.13
        _OuterRingWidth ("Outer Ring Width", Range(0.001, 0.08)) = 0.012

        _GlowWidth ("Glow Width", Range(0.01, 0.6)) = 0.23
        _GlowIntensity ("Glow Intensity", Range(0, 6)) = 1.45
        _LineIntensity ("Line Intensity", Range(0, 8)) = 2.8
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 0.74

        [Header(Painterly Color)]
        _PaintStrength ("Paint Strength", Range(0, 1)) = 1
        _PaintSteps ("Paint Tonal Steps", Range(2, 12)) = 5
        _PaintSize ("Paint Patch Size", Range(0.03, 0.4)) = 0.14
        _PaintVariation ("Paint Patch Variation", Range(0, 1)) = 0.65

        _CenterDotRadius ("Center Dot Radius", Range(0, 0.9)) = 0.035
        _CenterGlowRadius ("Center Glow Radius", Range(0, 0.45)) = 0.22

        _Armor ("Armor", Range(0, 1)) = 0
        _Crit ("Crit", Range(0, 1)) = 0
        _MarkCount ("Armor Mark Count", Range(0, 48)) = 18
        _MarkWidthReferenceCount ("Armor Mark Width Reference Count", Range(1, 48)) = 8
        _MarkWidth ("Armor Mark Width", Range(0.001, 0.25)) = 0.035
        _MarkDistance ("Mark Distance", Range(-0.1, 0.35)) = 0.105
        _MarkLength ("Armor Mark Height", Range(0.001, 0.2)) = 0.065
        _MarkIntensity ("Mark Intensity", Range(0, 6)) = 1.45

        _SpikeCount ("Crit Spike Count", Range(0, 48)) = 8
        _SpikeWidthReferenceCount ("Crit Spike Width Reference Count", Range(1, 48)) = 4
        _SpikeWidth ("Crit Spike Width", Range(0.001, 0.25)) = 0.075
        _SpikeTipWidth ("Spike Tip Width", Range(0.001, 0.12)) = 0.012
        _SpikeLength ("Crit Spike Height", Range(0.001, 0.45)) = 0.2
        _SpikeIntensity ("Spike Intensity", Range(0, 8)) = 2.6

        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.08
        _PulseSpeed ("Pulse Speed", Range(0, 8)) = 1.35
        _ContourSpinSpeed ("Contour Spin Speed", Range(-3, 3)) = 0.08
        _Rotation ("Rotation", Float) = 0
        _MirrorX ("Mirror X", Float) = 0
        _MirrorY ("Mirror Y", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "PainterlyColor.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _ShapeType;
            float4 _Color;
            float4 _CoreColor;
            float4 _FillColor;

            float _ShapeRadius;
            float _TriangleSize;
            float _Softness;
            float _EdgeFade;

            float _MainLineWidth;
            float _InnerRingInset;
            float _InnerRingWidth;
            float _OuterRingOffset;
            float _OuterRingWidth;

            float _GlowWidth;
            float _GlowIntensity;
            float _LineIntensity;
            float _FillAlpha;
            float _PaintStrength;
            float _PaintSteps;
            float _PaintSize;
            float _PaintVariation;

            float _CenterDotRadius;
            float _CenterGlowRadius;

            float _Armor;
            float _Crit;
            float _MarkCount;
            float _MarkWidthReferenceCount;
            float _MarkWidth;
            float _MarkDistance;
            float _MarkLength;
            float _MarkIntensity;

            float _SpikeCount;
            float _SpikeWidthReferenceCount;
            float _SpikeWidth;
            float _SpikeTipWidth;
            float _SpikeLength;
            float _SpikeIntensity;

            float _PulseAmount;
            float _PulseSpeed;
            float _ContourSpinSpeed;
            float _Rotation;
            float _MirrorX;
            float _MirrorY;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            float Cross2(float2 a, float2 b)
            {
                return a.x * b.y - a.y * b.x;
            }

            float SegmentDistance(float2 p, float2 a, float2 b, out float segmentT)
            {
                float2 pa = p - a;
                float2 ba = b - a;
                segmentT = saturate(dot(pa, ba) / max(dot(ba, ba), 0.00001));
                return length(pa - ba * segmentT);
            }

            float TriangleSdf(float2 p, out float contourT)
            {
                float r = _ShapeRadius * 1.52 * _TriangleSize;
                float2 a = float2(0.0, r);
                float2 b = float2(-0.8660254 * r, -0.5 * r);
                float2 c = float2(0.8660254 * r, -0.5 * r);

                float ta;
                float tb;
                float tc;
                float da = SegmentDistance(p, a, b, ta);
                float db = SegmentDistance(p, b, c, tb);
                float dc = SegmentDistance(p, c, a, tc);

                float d = da;
                contourT = ta / 3.0;

                if (db < d)
                {
                    d = db;
                    contourT = (1.0 + tb) / 3.0;
                }

                if (dc < d)
                {
                    d = dc;
                    contourT = (2.0 + tc) / 3.0;
                }

                bool inside = Cross2(b - a, p - a) >= 0.0
                    && Cross2(c - b, p - b) >= 0.0
                    && Cross2(a - c, p - c) >= 0.0;

                return inside ? -d : d;
            }

            float SquareContourT(float2 p, float halfSize)
            {
                float2 c = clamp(p, -halfSize, halfSize);
                float ax = abs(p.x);
                float ay = abs(p.y);
                float t = 0.0;

                if (p.y >= ax)
                {
                    t = (c.x + halfSize) / (2.0 * halfSize) * 0.25;
                }
                else if (p.x >= ay)
                {
                    t = 0.25 + (halfSize - c.y) / (2.0 * halfSize) * 0.25;
                }
                else if (-p.y >= ax)
                {
                    t = 0.5 + (halfSize - c.x) / (2.0 * halfSize) * 0.25;
                }
                else
                {
                    t = 0.75 + (c.y + halfSize) / (2.0 * halfSize) * 0.25;
                }

                return frac(t);
            }

            float SquareSdf(float2 p, out float contourT)
            {
                float halfSize = _ShapeRadius * 0.89;
                float2 q = abs(p) - halfSize;
                contourT = SquareContourT(p, halfSize);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
            }

            float CircleSdf(float2 p, out float contourT)
            {
                contourT = frac(atan2(p.x, p.y) / 6.2831853);
                return length(p) - _ShapeRadius;
            }

            void ShapeSdf(float2 p, out float sd, out float contourT)
            {
                if (_ShapeType < 0.5)
                {
                    sd = CircleSdf(p, contourT);
                }
                else if (_ShapeType < 1.5)
                {
                    sd = SquareSdf(p, contourT);
                }
                else
                {
                    sd = TriangleSdf(p, contourT);
                }
            }

            float CenterTriangleSdf(float2 p, float radius)
            {
                float r = radius * 1.52 * _TriangleSize;
                float2 a = float2(0.0, r);
                float2 b = float2(-0.8660254 * r, -0.5 * r);
                float2 c = float2(0.8660254 * r, -0.5 * r);

                float ta;
                float tb;
                float tc;
                float da = SegmentDistance(p, a, b, ta);
                float db = SegmentDistance(p, b, c, tb);
                float dc = SegmentDistance(p, c, a, tc);
                float d = min(da, min(db, dc));

                bool inside = Cross2(b - a, p - a) >= 0.0
                    && Cross2(c - b, p - b) >= 0.0
                    && Cross2(a - c, p - c) >= 0.0;

                return inside ? -d : d;
            }

            float CenterSquareSdf(float2 p, float radius)
            {
                float halfSize = radius * 0.89;
                float2 q = abs(p) - halfSize;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
            }

            float CenterShapeSdf(float2 p, float radius)
            {
                if (_ShapeType < 0.5)
                {
                    return length(p) - radius;
                }

                if (_ShapeType < 1.5)
                {
                    return CenterSquareSdf(p, radius);
                }

                return CenterTriangleSdf(p, radius);
            }

            float Ring(float signedDistance, float offset, float width, float softness)
            {
                float halfWidth = width * 0.5;
                return 1.0 - smoothstep(halfWidth, halfWidth + softness, abs(signedDistance - offset));
            }

            float PeriodicMask(float contourT, float count, float width, float softness)
            {
                if (count < 0.5)
                {
                    return 0.0;
                }

                float cell = frac(contourT * count);
                float distanceToCenter = min(cell, 1.0 - cell);
                return 1.0 - smoothstep(width, width + softness, distanceToCenter);
            }

            float FixedWidthPeriodicMask(float contourT, float count, float referenceCount, float width)
            {
                float safeReferenceCount = max(referenceCount, 1.0);
                float fixedCellWidth = min(width * max(count, 1.0) / safeReferenceCount, 0.49);
                float fixedCellSoftness = min(0.012 * max(count, 1.0) / safeReferenceCount, 0.49 - fixedCellWidth);
                return PeriodicMask(contourT, count, fixedCellWidth, max(fixedCellSoftness, 0.001));
            }

            float TaperedFixedWidthPeriodicSpike(float contourT, float count, float referenceCount, float radialDistance, float length, float baseWidth, float tipWidth, float softness)
            {
                if (count < 0.5)
                {
                    return 0.0;
                }

                float safeReferenceCount = max(referenceCount, 1.0);
                float countScale = max(count, 1.0) / safeReferenceCount;
                float baseCellWidth = min(baseWidth * countScale, 0.49);
                float tipCellWidth = min(tipWidth * countScale, baseCellWidth);
                float cellSoftness = max(min(0.012 * countScale, 0.49 - baseCellWidth), 0.001);
                float progress = saturate(radialDistance / max(length, 0.0001));
                float width = lerp(baseCellWidth, tipCellWidth, progress);
                float cell = frac(contourT * count);
                float distanceToCenter = min(cell, 1.0 - cell);
                float along = 1.0 - smoothstep(width, width + cellSoftness, distanceToCenter);
                float start = smoothstep(-softness, softness, radialDistance);
                float end = 1.0 - smoothstep(length - softness, length + softness, radialDistance);
                float tipFade = lerp(1.0, 0.32, progress);
                return along * start * end * tipFade;
            }

            float EdgeFade(float2 uv)
            {
                float left = smoothstep(0.0, _EdgeFade, uv.x);
                float right = smoothstep(0.0, _EdgeFade, 1.0 - uv.x);
                float bottom = smoothstep(0.0, _EdgeFade, uv.y);
                float top = smoothstep(0.0, _EdgeFade, 1.0 - uv.y);
                return left * right * bottom * top;
            }

            float2 Rotate(float2 p, float degrees)
            {
                float radians = degrees * 0.01745329252;
                float s = sin(radians);
                float c = cos(radians);
                return float2(p.x * c - p.y * s, p.x * s + p.y * c);
            }

            float AnchoredSpike(float2 p, float2 anchor, float2 normal, float length, float baseWidth, float tipWidth, float softness)
            {
                normal = normalize(normal);
                float2 tangent = float2(-normal.y, normal.x);
                float2 local = p - anchor;
                float along = dot(local, normal);
                float across = dot(local, tangent);
                float progress = saturate(along / max(length, 0.0001));
                float width = lerp(baseWidth, tipWidth, progress);
                float body = 1.0 - smoothstep(width, width + softness, abs(across));
                float start = smoothstep(-softness, softness, along);
                float end = 1.0 - smoothstep(length, length + softness, along);
                return body * start * end * lerp(1.0, 0.32, progress);
            }

            void SquareAnchor(float contourT, out float2 anchor, out float2 normal)
            {
                float h = _ShapeRadius * 0.89;
                float sideT = frac(contourT) * 4.0;
                float side = floor(sideT);
                float u = frac(sideT);

                if (side < 0.5)
                {
                    anchor = lerp(float2(-h, h), float2(h, h), u);
                    normal = float2(0.0, 1.0);
                }
                else if (side < 1.5)
                {
                    anchor = lerp(float2(h, h), float2(h, -h), u);
                    normal = float2(1.0, 0.0);
                }
                else if (side < 2.5)
                {
                    anchor = lerp(float2(h, -h), float2(-h, -h), u);
                    normal = float2(0.0, -1.0);
                }
                else
                {
                    anchor = lerp(float2(-h, -h), float2(-h, h), u);
                    normal = float2(-1.0, 0.0);
                }
            }

            void TriangleAnchor(float contourT, out float2 anchor, out float2 normal)
            {
                float r = _ShapeRadius * 1.52 * _TriangleSize;
                float2 a = float2(0.0, r);
                float2 b = float2(-0.8660254 * r, -0.5 * r);
                float2 c = float2(0.8660254 * r, -0.5 * r);
                float sideT = frac(contourT) * 3.0;
                float side = floor(sideT);
                float u = frac(sideT);
                float2 edge;

                if (side < 0.5)
                {
                    anchor = lerp(a, b, u);
                    edge = b - a;
                }
                else if (side < 1.5)
                {
                    anchor = lerp(b, c, u);
                    edge = c - b;
                }
                else
                {
                    anchor = lerp(c, a, u);
                    edge = a - c;
                }

                normal = normalize(float2(edge.y, -edge.x));
            }

            float SquareSpikes(float2 p, float count, float length, float baseWidth, float tipWidth, float softness)
            {
                if (count < 0.5)
                {
                    return 0.0;
                }

                float safeCount = max(round(count), 1.0);
                float spike = 0.0;

                [unroll]
                for (int spikeIndex = 0; spikeIndex < 48; spikeIndex++)
                {
                    float active = step(spikeIndex + 0.5, safeCount);
                    float2 anchor;
                    float2 normal;
                    SquareAnchor((spikeIndex + 0.5) / safeCount, anchor, normal);
                    spike = max(spike, AnchoredSpike(p, anchor, normal, length, baseWidth, tipWidth, softness) * active);
                }

                return saturate(spike);
            }

            float TriangleSpikes(float2 p, float count, float length, float baseWidth, float tipWidth, float softness)
            {
                if (count < 0.5)
                {
                    return 0.0;
                }

                float safeCount = max(round(count), 1.0);
                float spike = 0.0;

                [unroll]
                for (int spikeIndex = 0; spikeIndex < 48; spikeIndex++)
                {
                    float active = step(spikeIndex + 0.5, safeCount);
                    float2 anchor;
                    float2 normal;
                    TriangleAnchor((spikeIndex + 0.5) / safeCount, anchor, normal);
                    spike = max(spike, AnchoredSpike(p, anchor, normal, length, baseWidth, tipWidth, softness) * active);
                }

                return saturate(spike);
            }

            float PaintPatch(float2 p)
            {
                return SkillTreePaintPatch(p, _PaintSize);
            }

            float PaintEnergy(float value, float patchValue)
            {
                return SkillTreePaintEnergy(value, patchValue, _PaintStrength, _PaintSteps, _PaintVariation);
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                p.x *= _MirrorX > 0.5 ? -1.0 : 1.0;
                p.y *= _MirrorY > 0.5 ? -1.0 : 1.0;
                p = Rotate(p, _Rotation);

                float sd;
                float contourT;
                ShapeSdf(p, sd, contourT);

                float softness = max(_Softness, fwidth(sd) * 1.35);
                float pulse = 1.0 + (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5) * _PulseAmount;
                float animatedT = frac(contourT + _Time.y * _ContourSpinSpeed * 0.035);

                float inside = 1.0 - smoothstep(0.0, softness * 1.5, sd);
                float mainLine = Ring(sd, 0.0, _MainLineWidth, softness);
                float hotLine = Ring(sd, 0.0, _MainLineWidth * 0.42, softness * 0.55);
                float innerRing = Ring(sd, -_InnerRingInset, _InnerRingWidth, softness);
                float outerRing = Ring(sd, _OuterRingOffset, _OuterRingWidth, softness);

                float glow = 1.0 - smoothstep(0.0, _GlowWidth, abs(sd));
                float outerGlow = (1.0 - smoothstep(0.0, _GlowWidth, max(sd, 0.0))) * smoothstep(-0.02, 0.08, sd);

                float markAlong = FixedWidthPeriodicMask(animatedT, _MarkCount, _MarkWidthReferenceCount, _MarkWidth);
                float markRadial = Ring(sd, _MarkDistance, _MarkLength, softness);
                float marks = markAlong * markRadial * lerp(0.65, 1.25, saturate(_Armor));

                float spikeDistance = sd + 0.018;
                float spikeProgress = saturate(spikeDistance / max(_SpikeLength, 0.0001));
                float spikeCore;
                if (_ShapeType < 0.5)
                {
                    spikeCore = TaperedFixedWidthPeriodicSpike(
                        contourT,
                        _SpikeCount,
                        _SpikeWidthReferenceCount,
                        spikeDistance,
                        _SpikeLength,
                        _SpikeWidth,
                        _SpikeTipWidth,
                        softness);
                }
                else if (_ShapeType < 1.5)
                {
                    spikeCore = SquareSpikes(p, _SpikeCount, _SpikeLength, _SpikeWidth, _SpikeTipWidth, softness);
                }
                else
                {
                    spikeCore = TriangleSpikes(p, _SpikeCount, _SpikeLength, _SpikeWidth, _SpikeTipWidth, softness);
                }
                float spikeHot = spikeCore * (1.0 - smoothstep(0.72, 1.0, spikeProgress));
                float spikes = spikeCore * lerp(0.7, 1.35, saturate(_Crit));

                float centerDotDistance = CenterShapeSdf(p, _CenterDotRadius);
                float centerGlowDistance = CenterShapeSdf(p, _CenterGlowRadius);
                float centerDot = 1.0 - smoothstep(0.0, softness, centerDotDistance);
                float centerGlow = smoothstep(0.0, 1.0, saturate(-centerGlowDistance / max(_CenterGlowRadius, 0.0001)));

                float lineEnergy = (mainLine * 1.1 + hotLine * 2.1 + innerRing * 0.7 + outerRing * 0.55) * _LineIntensity;
                float decorEnergy = marks * _MarkIntensity + spikes * _SpikeIntensity + spikeHot * _SpikeIntensity * 0.8;
                float glowEnergy = (glow * 0.36 + outerGlow * 0.52 + centerGlow * 0.5) * _GlowIntensity;
                float centerEnergy = centerDot * 3.0 + centerGlow * 0.35;

                float energy = (lineEnergy + decorEnergy + glowEnergy + centerEnergy) * pulse;
                float edge = EdgeFade(i.uv);

                float3 fill = _FillColor.rgb * inside * _FillAlpha;
                float coloredEnergy = (glowEnergy + decorEnergy * 0.68 + outerRing * _LineIntensity * 0.42 + mainLine * _LineIntensity * 0.22) * pulse;
                float coreEnergy = (hotLine * _LineIntensity * 1.65 + mainLine * _LineIntensity * 0.78
                    + innerRing * _LineIntensity * 0.34 + marks * _MarkIntensity * 0.48
                    + spikes * _SpikeIntensity * 0.54 + centerDot * 3.8 + centerGlow * 0.18) * pulse;
                float patch = PaintPatch(p);
                coloredEnergy = PaintEnergy(coloredEnergy, patch);
                coreEnergy = PaintEnergy(coreEnergy, patch);
                float3 light = (_Color.rgb * coloredEnergy + _CoreColor.rgb * coreEnergy) * i.color.rgb;
                // Quantize the translucent halo too, otherwise blending restores a gradient.
                // Keep coverage of the actual shape, fine lines and marks antialiased.
                float haloAlpha = PaintEnergy(glow * 0.28 + outerGlow * 0.22, patch);
                float alpha = saturate(inside * _FillAlpha + haloAlpha + mainLine + decorEnergy * 0.28 + centerDot);

                return float4(fill + light, alpha * edge);
            }
            ENDCG
        }
    }
}
