Shader "SkillTree/Barrier Rings SDF"
{
    Properties
    {
        [HDR] _Color ("Active Color", Color) = (0.35, 2.25, 3.1, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (0.2, 1.55, 3.4, 1)
        _InactiveColor ("Inactive Color", Color) = (0.16, 0.34, 0.42, 0.34)

        _ActiveCount ("Active Count", Range(0, 32)) = 5
        _MaxCount ("Max Count", Range(0, 32)) = 8
        _BrokenProgress ("Broken Progress", Range(0, 32)) = 3
        _SegmentDirection ("Segment Direction", Float) = 1
        _OuterRadius ("Outer Radius", Range(0.05, 0.95)) = 0.78
        _InnerRadius ("Inner Radius", Range(0, 0.85)) = 0.26
        _RingThicknessFraction ("Ring Thickness Fraction", Range(0.05, 1.35)) = 0.9
        _RingGap ("Ring Gap / Overlap", Range(-0.08, 0.08)) = -0.01
        _SegmentsPerLayer ("Segments Per Layer", Range(1, 16)) = 5
        _SegmentGap ("Segment Gap", Range(0, 0.35)) = 0.06
        _InnerFade ("Inner Fade", Range(0.001, 1)) = 0.32
        _GlowWidth ("Glow Width", Range(0.001, 0.3)) = 0.035
        _GlowIntensity ("Glow Intensity", Range(0, 4)) = 1
        _ActiveAlpha ("Active Alpha", Range(0, 1)) = 0.34
        _GlowAlpha ("Glow Alpha", Range(0, 1)) = 0.18

        [Header(Painterly Color)]
        _PaintStrength ("Paint Strength", Range(0, 1)) = 1
        _PaintSteps ("Paint Tonal Steps", Range(2, 12)) = 5
        _PaintSize ("Paint Patch Size", Range(0.001, 0.4)) = 0.14
        _PaintVariation ("Paint Patch Variation", Range(0, 1)) = 0.65

        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.08
        _PulseSpeed ("Pulse Speed", Range(0, 8)) = 1.1
        _Rotation ("Rotation", Float) = 0
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

            float4 _Color;
            float4 _GlowColor;
            float4 _InactiveColor;
            float _ActiveCount;
            float _MaxCount;
            float _BrokenProgress;
            float _SegmentDirection;
            float _OuterRadius;
            float _InnerRadius;
            float _RingThicknessFraction;
            float _RingGap;
            float _SegmentsPerLayer;
            float _SegmentGap;
            float _InnerFade;
            float _GlowWidth;
            float _GlowIntensity;
            float _ActiveAlpha;
            float _GlowAlpha;
            float _PaintStrength;
            float _PaintSteps;
            float _PaintSize;
            float _PaintVariation;
            float _PulseAmount;
            float _PulseSpeed;
            float _Rotation;

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
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            float2 Rotate(float2 p, float degrees)
            {
                float radians = degrees * 0.01745329252;
                float s = sin(radians);
                float c = cos(radians);
                return float2(p.x * c - p.y * s, p.x * s + p.y * c);
            }

            float RingMask(float radius, float innerRadius, float outerRadius, float innerFade)
            {
                float outerEdge = 1.0 - step(outerRadius, radius);
                float fadeStart = max(innerRadius - innerFade, 0.0);
                float innerEdge = innerRadius <= 0.0001
                    ? 1.0
                    : max(step(innerRadius, radius), smoothstep(fadeStart, innerRadius, radius));
                return outerEdge * innerEdge;
            }

            float RingGlow(float radius, float innerRadius, float outerRadius, float glowWidth)
            {
                float centerRadius = (innerRadius + outerRadius) * 0.5;
                float halfWidth = max((outerRadius - innerRadius) * 0.5, 0.0001);
                float distanceToRing = abs(radius - centerRadius) - halfWidth;
                return 1.0 - smoothstep(0.0, glowWidth, max(distanceToRing, 0.0));
            }

            float EdgeFade(float2 uv)
            {
                float fade = 0.025;
                float left = smoothstep(0.0, fade, uv.x);
                float right = smoothstep(0.0, fade, 1.0 - uv.x);
                float bottom = smoothstep(0.0, fade, uv.y);
                float top = smoothstep(0.0, fade, 1.0 - uv.y);
                return left * right * bottom * top;
            }

            float SegmentMask(float2 p, float segmentIndex, float segmentCount, float layerRotationOffset)
            {
                if (segmentCount < 1.5)
                {
                    return 1.0;
                }

                float angle = frac(atan2(p.x, p.y) / 6.2831853 + 0.5 / segmentCount + layerRotationOffset);
                float center = (segmentIndex + 0.5) / segmentCount;
                float distanceToCenter = abs(frac(angle - center + 0.5) - 0.5) * segmentCount;
                float halfWidth = max(0.5 - _SegmentGap * 0.5, 0.02);
                return 1.0 - smoothstep(halfWidth, halfWidth + 0.01, distanceToCenter);
            }

            float4 frag(v2f i) : SV_Target
            {
                float maxCount = round(max(_MaxCount, 0.0));
                if (maxCount < 0.5)
                {
                    return float4(0.0, 0.0, 0.0, 0.0);
                }

                float activeCount = clamp(round(_ActiveCount), 0.0, maxCount);
                float inactiveCount = maxCount - activeCount;
                float2 p = Rotate(i.uv * 2.0 - 1.0, _Rotation);
                float radius = length(p);
                float outerRadius = max(_OuterRadius, 0.01);
                float innerLimit = min(_InnerRadius, outerRadius - 0.01);
                float span = max(outerRadius - innerLimit, 0.01);
                float segmentsPerLayer = clamp(round(_SegmentsPerLayer), 1.0, 16.0);
                float layerCount = ceil(maxCount / segmentsPerLayer);
                float outerLayerSegments = maxCount - floor((maxCount - 0.001) / segmentsPerLayer) * segmentsPerLayer;
                float layerSlotWidth = span / layerCount;
                float gap = clamp(_RingGap, -layerSlotWidth * 0.7, layerSlotWidth * 0.45);
                float ringWidth = max(layerSlotWidth * clamp(_RingThicknessFraction, 0.05, 1.35) - gap, 0.001);
                float innerFade = max(_InnerFade, 0.001);
                float pulse = 1.0 + (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5) * _PulseAmount;

                float3 color = 0.0;
                float alpha = 0.0;

                [unroll]
                for (int ringIndex = 0; ringIndex < 32; ringIndex++)
                {
                    float activeSlot = step(ringIndex + 0.5, maxCount);
                    float slotIndex = ringIndex;
                    float isOuterLayer = 1.0 - step(outerLayerSegments, slotIndex);
                    float adjustedSlotIndex = max(slotIndex - outerLayerSegments, 0.0);
                    float innerLayerIndex = floor(adjustedSlotIndex / segmentsPerLayer);
                    float layerIndex = isOuterLayer * 0.0 + (1.0 - isOuterLayer) * (innerLayerIndex + 1.0);
                    float segmentBaseIndex = adjustedSlotIndex - innerLayerIndex * segmentsPerLayer;
                    float segmentIndex = isOuterLayer * slotIndex + (1.0 - isOuterLayer) * segmentBaseIndex;
                    float segmentCount = isOuterLayer * outerLayerSegments + (1.0 - isOuterLayer) * segmentsPerLayer;
                    float layerStartIndex = (1.0 - isOuterLayer) * (outerLayerSegments + innerLayerIndex * segmentsPerLayer);
                    float brokenInLayer = clamp(_BrokenProgress - layerStartIndex, 0.0, segmentCount);
                    float direction = _SegmentDirection < 0.0 ? -1.0 : 1.0;
                    float layerRotationOffset = segmentCount < 1.5 ? 0.0 : direction * brokenInLayer / segmentCount;
                    float visibleGap = max(gap, 0.0);
                    float ringOuter = outerRadius - layerSlotWidth * layerIndex - visibleGap * 0.5;
                    float ringInner = max(ringOuter - ringWidth, 0.0);
                    float segment = SegmentMask(p, segmentIndex, segmentCount, layerRotationOffset);
                    float ring = RingMask(radius, ringInner, ringOuter, innerFade) * segment * activeSlot;
                    float isActive = step(inactiveCount + 0.5, ringIndex + 1.0);
                    float inactive = 1.0 - isActive;
                    float glow = RingGlow(radius, ringInner, ringOuter, _GlowWidth) * ring * isActive;

                    float activeEnergy = ring * isActive * pulse;
                    float inactiveEnergy = ring * inactive;
                    color += _Color.rgb * activeEnergy * _ActiveAlpha;
                    color += _GlowColor.rgb * glow * _GlowIntensity * pulse * _GlowAlpha;
                    color += _InactiveColor.rgb * inactiveEnergy * _InactiveColor.a;
                    alpha = max(alpha, ring * (isActive * _ActiveAlpha + inactive * _InactiveColor.a));
                    alpha = max(alpha, glow * _GlowAlpha);
                }

                // Style the combined layers once, keeping their charge/segment masks.
                // Local coordinates keep patches attached to the rotating barrier.
                if (_PaintStrength > 0.0)
                {
                    float patchValue = SkillTreePaintPatch(p, _PaintSize);
                    color = SkillTreePaintColor(color, patchValue, _PaintStrength, _PaintSteps, _PaintVariation);
                    alpha = SkillTreePaintEnergy(alpha, patchValue, _PaintStrength, _PaintSteps, _PaintVariation);
                }

                return float4(color * i.color.rgb, saturate(alpha) * EdgeFade(i.uv));
            }
            ENDCG
        }
    }
}
