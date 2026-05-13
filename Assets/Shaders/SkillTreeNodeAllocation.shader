Shader "UI/SkillTree/Node Allocation"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Progress ("Progress", Range(0,1)) = 0

        [HDR] _CoreColor ("Core HDR Color", Color) = (0.12,0.55,1.6,1)
        [HDR] _WispColor ("Wisp HDR Color", Color) = (0.2,1.25,3.5,1)
        [HDR] _BurstColor ("Burst HDR Color", Color) = (0.75,2.1,6.0,1)
        _DimColor ("Inactive Dim Color", Color) = (0.18,0.2,0.22,1)

        _EmissionIntensity ("Emission Intensity", Range(0,12)) = 4.5
        _EffectRadius ("Effect Radius", Range(0.05,0.9)) = 0.42
        _WispSharpness ("Wisp Sharpness", Range(2,32)) = 14
        _HaloSize ("Halo Size", Range(0.05,0.8)) = 0.34
        _RayLength ("Ray Length", Range(0,1.2)) = 0.55
        _SparkDensity ("Spark Density", Range(4,64)) = 24

        _NoiseTex ("Optional Noise Mask", 2D) = "gray" {}
        _NoiseInfluence ("Noise Influence", Range(0,1)) = 0.25

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            float4 _NoiseTex_ST;
            fixed4 _Color;
            float _Progress;
            float4 _CoreColor;
            float4 _WispColor;
            float4 _BurstColor;
            float4 _DimColor;
            float _EmissionIntensity;
            float _EffectRadius;
            float _WispSharpness;
            float _HaloSize;
            float _RayLength;
            float _SparkDensity;
            float _NoiseInfluence;
            float4 _ClipRect;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float Phase(float p, float startValue, float peakValue, float endValue)
            {
                return smoothstep(startValue, peakValue, p) * (1.0 - smoothstep(peakValue, endValue, p));
            }

            float StarRay(float angle01, float d, float p, float rayCount)
            {
                float ray = angle01 * rayCount;
                float rayId = floor(ray);
                float lane = frac(ray);
                float lengthRandom = lerp(0.62, 1.18, Hash11(rayId + 3.17));
                float widthRandom = lerp(18.0, 38.0, Hash11(rayId + 11.43));
                float brightnessRandom = lerp(0.55, 1.25, Hash11(rayId + 27.91));
                float startRandom = lerp(-0.035, 0.055, Hash11(rayId + 41.29));
                float centerLine = abs(lane - 0.5) * 2.0;
                float burstShape = pow(saturate(1.0 - centerLine), widthRandom);
                float collapseShape = pow(saturate(1.0 - centerLine), widthRandom * 0.42);
                float rayStart = 0.08 + startRandom;
                float rayMid = 0.2 + startRandom;
                float rayEnd = 0.22 + _RayLength * (0.32 + 0.58 * lengthRandom);
                float rayFade = 0.08 + 0.14 * lengthRandom;
                float burstHead = smoothstep(rayStart, rayMid, d) * (1.0 - smoothstep(rayEnd, rayEnd + rayFade, d));
                float burst = Phase(p, 0.48, 0.68, 0.9);

                float collapse = smoothstep(0.52, 1.0, p);
                float collapseLife = smoothstep(0.54, 0.68, p) * (1.0 - smoothstep(0.998, 1.0, p));
                float streakHead = lerp(rayEnd * 0.9, 0.055 + startRandom * 0.12, collapse);
                float streakTail = lerp(rayEnd + rayFade * 0.65, 0.34 + startRandom * 0.28, collapse);
                float streakBand = smoothstep(streakHead, streakHead + 0.08, d) * (1.0 - smoothstep(streakTail, streakTail + rayFade * 0.65, d));
                float inwardGradient = 1.0 - smoothstep(streakHead, streakTail, d);
                float inwardStreak = streakBand * lerp(0.55, 1.25, inwardGradient);
                float hotTip = 1.0 - smoothstep(0.0, 0.085 + rayFade * 0.24, abs(d - streakHead));
                float collapseHead = (inwardStreak + hotTip * 0.9) * lerp(1.0, 1.45, collapse);

                return (burstShape * burstHead * burst + collapseShape * collapseHead * collapseLife) * brightnessRandom;
            }

            float EnergyWisp(float angle01, float d, float p)
            {
                float curl = sin((angle01 * 6.2831853 * 5.0) + d * 18.0 - p * 9.0);
                float curl2 = sin((angle01 * 6.2831853 * 9.0) - d * 11.0 + p * 6.0);
                float strand = pow(saturate((curl * 0.65 + curl2 * 0.35) * 0.5 + 0.5), _WispSharpness);
                float body = (1.0 - smoothstep(_EffectRadius * 0.3, _EffectRadius * 1.35, d));
                float hollowCore = smoothstep(0.13, 0.28, d);
                float life = Phase(p, 0.18, 0.58, 0.96) + smoothstep(0.82, 1.0, p) * 0.28;
                return strand * body * hollowCore * life;
            }

            float Spark(float angle01, float d, float p)
            {
                float laneId = floor(angle01 * _SparkDensity);
                float lane = frac(angle01 * _SparkDensity);
                float seed = Hash11(laneId + 7.13);
                float seed2 = Hash11(laneId + 19.71);
                float width = lerp(0.04, 0.14, seed2);
                float radialTarget = lerp(0.28, 0.78, seed);

                float attract = Phase(p, 0.08, 0.34, 0.56);
                float burst = Phase(p, 0.5, 0.72, 0.98);
                float travel = lerp(radialTarget, 0.2, saturate(p * 2.1));
                float burstTravel = lerp(0.18, radialTarget + 0.22, saturate((p - 0.55) / 0.35));

                float attractDot = (1.0 - smoothstep(0.0, width, abs(d - travel))) * (1.0 - smoothstep(0.0, 0.18, abs(lane - 0.5)));
                float burstDot = (1.0 - smoothstep(0.0, width * 0.8, abs(d - burstTravel))) * (1.0 - smoothstep(0.0, 0.12, abs(lane - 0.5)));

                return attractDot * attract * 0.8 + burstDot * burst;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float p = saturate(_Progress);
                float4 tex = tex2D(_MainTex, i.uv) * i.color;

                float2 centered = i.uv * 2.0 - 1.0;
                float d = length(centered);
                float angle = atan2(centered.y, centered.x);
                float angle01 = frac(angle / 6.2831853 + 0.5);

                float noise = tex2D(_NoiseTex, TRANSFORM_TEX(i.uv, _NoiseTex)).r;
                noise = lerp(1.0, lerp(0.72, 1.28, noise), _NoiseInfluence);
                float visibilityIn = smoothstep(0.001, 0.035, p);
                float visibilityOut = 1.0 - smoothstep(0.99, 1.0, p);
                float visibility = visibilityIn * visibilityOut;
                float collapse = smoothstep(0.52, 1.0, p);
                float effectScale = lerp(1.0, 0.26, collapse);
                float collapsedD = d / max(effectScale, 0.001);
                float rayD = d / lerp(1.0, 0.62, collapse);
                float collapseCharge = Phase(p, 0.52, 0.82, 1.0);

                float prep = 1.0 - smoothstep(0.0, 0.18, p);
                float fill = smoothstep(0.22, 0.68, p);
                float integration = Phase(p, 0.45, 0.68, 0.88);
                float stabilize = Phase(p, 0.7, 0.86, 1.02);
                float active = smoothstep(0.82, 1.0, p);

                float baseAlpha = tex.a;
                float3 baseRgb = lerp(tex.rgb * _DimColor.rgb, tex.rgb, saturate(p * 1.5));

                float nodeOcclusion = smoothstep(0.12, 0.28, d);
                float collapsedOcclusion = smoothstep(0.12, 0.28, collapsedD);
                float innerGlow = (1.0 - smoothstep(0.14, _HaloSize * 0.9, collapsedD)) * collapsedOcclusion * (fill * 0.35 + integration * 1.6 + active * 0.22 + collapseCharge * 0.45);
                float outerHalo = (1.0 - smoothstep(0.18, _HaloSize * 1.45, collapsedD)) * collapsedOcclusion * (fill * 0.18 + integration * 0.75 + active * 0.18 + collapseCharge * 0.35);
                float coreFlash = (1.0 - smoothstep(0.12, 0.42, collapsedD)) * collapsedOcclusion * integration * 1.9;
                float collapseFlare = (1.0 - smoothstep(0.16, 0.46, collapsedD)) * nodeOcclusion * collapseCharge * 1.25;
                float wisps = EnergyWisp(angle01, collapsedD, p) * lerp(1.0, 1.45, collapseCharge);
                float rays = StarRay(angle01, rayD, p, 18.0);
                float sparks = Spark(angle01, collapsedD, p);
                float tinyStars = Spark(frac(angle01 + 0.371), collapsedD, saturate(p + 0.12)) * stabilize * 0.55;

                float energy = innerGlow + outerHalo * 0.55 + coreFlash + collapseFlare + wisps * 1.7 + rays * 2.5 + sparks * 1.4 + tinyStars;
                energy *= noise * visibility;

                float3 emission = _CoreColor.rgb * (innerGlow + sparks * 0.45)
                    + _WispColor.rgb * (wisps + outerHalo * 0.45 + tinyStars * 0.7)
                    + _BurstColor.rgb * (coreFlash + collapseFlare + rays);
                emission *= _EmissionIntensity * noise * visibility;

                float alpha = saturate(baseAlpha * visibility + energy * 0.55);

                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif

                float3 rgb = baseRgb * baseAlpha * visibility + emission;
                rgb *= lerp(0.85, 1.0, 1.0 - prep * (1.0 - baseAlpha));
                return float4(rgb, alpha);
            }
            ENDCG
        }
    }
}
