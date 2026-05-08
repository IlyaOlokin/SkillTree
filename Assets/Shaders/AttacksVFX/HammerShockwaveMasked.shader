Shader "SkillTree/VFX/HammerShockwaveMasked"
{
    Properties
    {
        [NoScaleOffset] [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}

        [NoScaleOffset] _BottomMask ("Bottom Mask", 2D) = "white" {}
        [NoScaleOffset] _TopMask ("Top Mask", 2D) = "white" {}
        [NoScaleOffset] _NoiseTex ("Noise", 2D) = "gray" {}

        [HDR]_BottomColor ("Bottom Color", Color) = (0.62, 0.62, 0.62, 1)
        [HDR]_TopColor ("Top Color", Color) = (1, 1, 1, 1)
        [HDR]_PhysicalColor ("Physical Color", Color) = (1.377358, 1.377358, 1.377358, 1)
        [HDR]_FireColor ("Fire Color", Color) = (3.207547, 0.3372529, 0, 1)
        [HDR]_ColdColor ("Cold Color", Color) = (0.2616589, 1.23218, 3.698113, 1)
        [HDR]_LightningColor ("Lightning Color", Color) = (3.773585, 2.050125, 0.1957992, 1)
        _DominantBaseDamageType ("Dominant Base Damage Type", Float) = 0
        _Life ("Life 0..1", Range(0, 1)) = 0

        _StartRadius ("Start Radius", Range(0.01, 0.8)) = 0.16
        _EndRadius ("End Radius", Range(0.01, 0.8)) = 0.34

        _BottomMaskRadius ("Bottom Mask Radius", Range(0.01, 0.8)) = 0.34
        _TopMaskRadius ("Top Mask Radius", Range(0.01, 0.8)) = 0.34

        _BottomThicknessStart ("Bottom Thickness Start", Range(0.1, 4.0)) = 0.7
        _BottomThicknessEnd ("Bottom Thickness End", Range(0.1, 4.0)) = 1.45
        _TopThicknessStart ("Top Thickness Start", Range(0.1, 4.0)) = 0.8
        _TopThicknessEnd ("Top Thickness End", Range(0.1, 4.0)) = 1.1
        _TopOuterSpikeStart ("Top Outer Spike Start", Range(0.1, 6.0)) = 0.95
        _TopOuterSpikeEnd ("Top Outer Spike End", Range(0.1, 6.0)) = 2.15
        _TopInnerSpikeStart ("Top Inner Spike Start", Range(0.1, 6.0)) = 0.8
        _TopInnerSpikeEnd ("Top Inner Spike End", Range(0.1, 6.0)) = 1.5

        _BottomThreshold ("Bottom Threshold", Range(0, 1)) = 0.22
        _TopThreshold ("Top Threshold", Range(0, 1)) = 0.16
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.2)) = 0.035

        _NoiseTiling ("Noise Tiling", Range(0.1, 8.0)) = 2.0
        _NoiseFlow ("Noise Flow", Range(0, 8.0)) = 1.25
        _BottomNoiseAmount ("Bottom Noise Amount", Range(0, 1)) = 0.09
        _TopNoiseAmount ("Top Noise Amount", Range(0, 1)) = 0.16
        _RadialWobble ("Radial Wobble", Range(0, 0.15)) = 0.018
        _AngularWobble ("Angular Wobble", Range(0, 0.12)) = 0.012
        _TopSpikeJitter ("Top Spike Jitter", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "SpriteForward"
            Tags { "LightMode" = "Universal2D" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BottomMask);
            SAMPLER(sampler_BottomMask);
            TEXTURE2D(_TopMask);
            SAMPLER(sampler_TopMask);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _BottomColor;
                half4 _TopColor;
                half4 _PhysicalColor;
                half4 _FireColor;
                half4 _ColdColor;
                half4 _LightningColor;
                float _DominantBaseDamageType;
                float _Life;
                float _StartRadius;
                float _EndRadius;
                float _BottomMaskRadius;
                float _TopMaskRadius;
                float _BottomThicknessStart;
                float _BottomThicknessEnd;
                float _TopThicknessStart;
                float _TopThicknessEnd;
                float _TopOuterSpikeStart;
                float _TopOuterSpikeEnd;
                float _TopInnerSpikeStart;
                float _TopInnerSpikeEnd;
                float _BottomThreshold;
                float _TopThreshold;
                float _EdgeSoftness;
                float _NoiseTiling;
                float _NoiseFlow;
                float _BottomNoiseAmount;
                float _TopNoiseAmount;
                float _RadialWobble;
                float _AngularWobble;
                float _TopSpikeJitter;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half4 color        : COLOR;
            };

            float EaseOutCubic(float t)
            {
                float inv = 1.0 - t;
                return 1.0 - inv * inv * inv;
            }

            float Luma(float3 color)
            {
                return dot(color, float3(0.299, 0.587, 0.114));
            }

            float SafeSampleMask(Texture2D tex, SamplerState texSampler, float2 uv)
            {
                float inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
                return Luma(SAMPLE_TEXTURE2D(tex, texSampler, uv).rgb) * inside;
            }

            float SampleNoise(float2 uv)
            {
                return Luma(SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, frac(uv)).rgb);
            }

            float3 GetDominantDamageColor(float dominantBaseDamageType)
            {
                if (dominantBaseDamageType < 0.5)
                {
                    return float3(-1.0, -1.0, -1.0);
                }

                if (dominantBaseDamageType < 1.5)
                {
                    return _PhysicalColor.rgb;
                }

                if (dominantBaseDamageType < 2.5)
                {
                    return _FireColor.rgb;
                }

                if (dominantBaseDamageType < 3.5)
                {
                    return _ColdColor.rgb;
                }

                return _LightningColor.rgb;
            }

            void ResolveLayerColors(out float3 bottomLayerColor, out float3 topLayerColor)
            {
                float3 dominantDamageColor = GetDominantDamageColor(_DominantBaseDamageType);
                if (dominantDamageColor.x < 0.0)
                {
                    bottomLayerColor = _BottomColor.rgb;
                    topLayerColor = _TopColor.rgb;
                    return;
                }

                float bottomLuma = max(Luma(_BottomColor.rgb), 0.0001);
                float topLuma = max(Luma(_TopColor.rgb), 0.0001);
                float maxLayerLuma = max(bottomLuma, topLuma);

                bottomLayerColor = dominantDamageColor * (bottomLuma / maxLayerLuma);
                topLayerColor = dominantDamageColor * (topLuma / maxLayerLuma);
            }

            float2 BuildLayerUv(
                float outputRadius,
                float2 radialDir,
                float2 tangentDir,
                float targetRadius,
                float sourceRadius,
                float innerScale,
                float outerScale,
                float angularShift,
                float radialOffset)
            {
                float signedDelta = outputRadius - targetRadius;
                float radialScale = signedDelta >= 0.0 ? max(outerScale, 0.0001) : max(innerScale, 0.0001);
                float sampleRadius = sourceRadius + signedDelta / radialScale + radialOffset;

                float c = cos(angularShift);
                float s = sin(angularShift);
                float2 rotatedDir = float2(
                    radialDir.x * c - radialDir.y * s,
                    radialDir.x * s + radialDir.y * c
                );

                float tangentPush = angularShift * 0.35;
                float2 shiftedDir = normalize(rotatedDir + tangentDir * tangentPush);
                return shiftedDir * sampleRadius + 0.5;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 bottomLayerColor;
                float3 topLayerColor;
                ResolveLayerColors(bottomLayerColor, topLayerColor);

                float life = saturate(_Life);
                float easedLife = EaseOutCubic(life);
                float lifeAlpha = smoothstep(0.0, 0.06, life) * (1.0 - smoothstep(0.82, 1.0, life));

                float spriteAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;

                float2 centeredUv = input.uv - 0.5;
                float outputRadius = length(centeredUv);
                float2 radialDir = outputRadius > 0.0001 ? centeredUv / outputRadius : float2(1.0, 0.0);
                float2 tangentDir = float2(-radialDir.y, radialDir.x);
                float angle = atan2(radialDir.y, radialDir.x);

                float baseRadius = lerp(_StartRadius, _EndRadius, easedLife);

                float noiseA = SampleNoise(input.uv * _NoiseTiling + float2(life * _NoiseFlow, life * _NoiseFlow * 0.61));
                float noiseB = SampleNoise(input.uv * (_NoiseTiling * 1.37) + float2(-life * _NoiseFlow * 0.41, life * _NoiseFlow * 0.77));
                float noiseC = SampleNoise(input.uv * (_NoiseTiling * 0.73) + float2(life * _NoiseFlow * 0.28, -life * _NoiseFlow * 0.33));

                float angularPulse = sin(angle * 14.0 + life * 18.0 + noiseA * 6.2831);
                float radialPulse = sin(angle * 9.0 - life * 15.0 + noiseB * 6.2831);

                float bottomThickness = lerp(_BottomThicknessStart, _BottomThicknessEnd, easedLife);
                float topThickness = lerp(_TopThicknessStart, _TopThicknessEnd, easedLife);
                float topOuterSpike = lerp(_TopOuterSpikeStart, _TopOuterSpikeEnd, easedLife);
                float topInnerSpike = lerp(_TopInnerSpikeStart, _TopInnerSpikeEnd, easedLife);

                float bottomInnerScale = bottomThickness * (1.0 + radialPulse * _BottomNoiseAmount * 0.25);
                float bottomOuterScale = bottomThickness * (1.0 + angularPulse * _BottomNoiseAmount * 0.35);
                float topInnerScale = lerp(topThickness, topInnerSpike, 0.7) * (1.0 + radialPulse * _TopNoiseAmount * 0.2);
                float topOuterScale = lerp(topThickness, topOuterSpike, 0.78) * (1.0 + angularPulse * _TopSpikeJitter * 0.45 + (noiseC - 0.5) * _TopNoiseAmount);

                float bottomAngularShift = angularPulse * _AngularWobble * 0.4 + (noiseB - 0.5) * _AngularWobble * 0.25;
                float topAngularShift = angularPulse * _AngularWobble + (noiseC - 0.5) * _AngularWobble * 0.55;

                float bottomRadialOffset = (noiseA - 0.5) * _RadialWobble * 0.35;
                float topRadialOffset = (noiseB - 0.5) * _RadialWobble + angularPulse * _RadialWobble * 0.25;

                float2 bottomUv = BuildLayerUv(
                    outputRadius,
                    radialDir,
                    tangentDir,
                    baseRadius,
                    _BottomMaskRadius,
                    bottomInnerScale,
                    bottomOuterScale,
                    bottomAngularShift,
                    bottomRadialOffset
                );

                float2 topUv = BuildLayerUv(
                    outputRadius,
                    radialDir,
                    tangentDir,
                    baseRadius,
                    _TopMaskRadius,
                    topInnerScale,
                    topOuterScale,
                    topAngularShift,
                    topRadialOffset
                );

                float bottomMask = SafeSampleMask(_BottomMask, sampler_BottomMask, bottomUv);
                float topMask = SafeSampleMask(_TopMask, sampler_TopMask, topUv);

                float bottomField = bottomMask + (noiseB - 0.5) * _BottomNoiseAmount;
                float topField = topMask + (noiseC - 0.5) * _TopNoiseAmount;

                float soft = max(_EdgeSoftness, 0.001);
                float bottomAlpha = smoothstep(_BottomThreshold - soft, _BottomThreshold + soft, bottomField);
                float topAlpha = smoothstep(_TopThreshold - soft, _TopThreshold + soft, topField);

                float topEdgePulse = 1.0 + angularPulse * _TopSpikeJitter * 0.18;
                topAlpha *= topEdgePulse;

                bottomAlpha *= lifeAlpha;
                topAlpha *= lifeAlpha;

                float finalAlpha = saturate(max(bottomAlpha, topAlpha) * spriteAlpha * input.color.a);
                if (finalAlpha <= 0.0001)
                {
                    discard;
                }

                float3 combinedColor = (bottomLayerColor * bottomAlpha + topLayerColor * topAlpha) / max(bottomAlpha + topAlpha, 0.0001);
                combinedColor *= input.color.rgb;

                return half4(combinedColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
