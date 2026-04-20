Shader "Unlit/SkillTreeFogOfWar"
{
    Properties
    {
        _RevealTex ("Reveal Texture", 2D) = "black" {}
        _FogColor ("Fog Color", Color) = (0.08, 0.09, 0.11, 0.92)
        _FogHighlightColor ("Fog Highlight Color", Color) = (0.22, 0.24, 0.28, 1.0)
        _WorldMin ("World Min", Vector) = (0, 0, 0, 0)
        _WorldSize ("World Size", Vector) = (10, 10, 0, 0)
        _MaskBlurRadius ("Mask Blur Radius", Range(0, 8)) = 2.2
        _MaskBlurRadiusWide ("Mask Blur Radius Wide", Range(0, 16)) = 5.5
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.12
        _EdgeSoftnessLayerB ("Edge Softness Layer B", Range(0.001, 0.7)) = 0.2
        _EdgeSoftnessLayerC ("Edge Softness Layer C", Range(0.001, 0.9)) = 0.32
        _EdgeNoiseStrength ("Edge Noise Strength", Range(0, 0.35)) = 0.1
        _FogOpacity ("Fog Opacity", Range(0, 1)) = 0.85
        _InnerNoiseOpacity ("Inner Noise Opacity", Range(0, 1)) = 0.15
        _LayerAlphaA ("Layer Alpha A", Range(0, 1)) = 0.65
        _LayerAlphaB ("Layer Alpha B", Range(0, 1)) = 0.42
        _LayerAlphaC ("Layer Alpha C", Range(0, 1)) = 0.24
        _VeilOpacity ("Veil Opacity", Range(0, 1)) = 0.28
        _VeilWidth ("Veil Width", Range(0.01, 1)) = 0.34
        _VeilNoiseScale ("Veil Noise Scale", Float) = 0.52
        _VeilNoiseStrength ("Veil Noise Strength", Range(0, 1)) = 0.42
        _NoiseScaleA ("Noise Scale A", Float) = 0.65
        _NoiseScaleB ("Noise Scale B", Float) = 1.35
        _NoiseScaleC ("Noise Scale C", Float) = 2.4
        _NoiseSpeedA ("Noise Speed A", Vector) = (0.03, 0.015, 0, 0)
        _NoiseSpeedB ("Noise Speed B", Vector) = (-0.022, 0.035, 0, 0)
        _NoiseSpeedC ("Noise Speed C", Vector) = (0.05, -0.028, 0, 0)
        _WarpScale ("Warp Scale", Float) = 0.38
        _WarpStrength ("Warp Strength", Range(0, 1.5)) = 0.28
        _WarpSpeed ("Warp Speed", Vector) = (0.018, -0.012, 0, 0)
        _SwirlStrength ("Swirl Strength", Range(0, 1)) = 0.18
        _SwirlSpeed ("Swirl Speed", Float) = 0.22
        _PulseStrength ("Pulse Strength", Range(0, 0.5)) = 0.08
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _RevealTex;
            float4 _RevealTex_ST;
            float4 _RevealTex_TexelSize;
            float4 _FogColor;
            float4 _FogHighlightColor;
            float4 _WorldMin;
            float4 _WorldSize;
            float _MaskBlurRadius;
            float _MaskBlurRadiusWide;
            float _EdgeSoftness;
            float _EdgeSoftnessLayerB;
            float _EdgeSoftnessLayerC;
            float _EdgeNoiseStrength;
            float _FogOpacity;
            float _InnerNoiseOpacity;
            float _LayerAlphaA;
            float _LayerAlphaB;
            float _LayerAlphaC;
            float _VeilOpacity;
            float _VeilWidth;
            float _VeilNoiseScale;
            float _VeilNoiseStrength;
            float _NoiseScaleA;
            float _NoiseScaleB;
            float _NoiseScaleC;
            float4 _NoiseSpeedA;
            float4 _NoiseSpeedB;
            float4 _NoiseSpeedC;
            float _WarpScale;
            float _WarpStrength;
            float4 _WarpSpeed;
            float _SwirlStrength;
            float _SwirlSpeed;
            float _PulseStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldXY : TEXCOORD1;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 uv)
            {
                float2 cell = floor(uv);
                float2 local = frac(uv);
                float2 smoothLocal = local * local * (3.0 - 2.0 * local);

                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));

                float x1 = lerp(a, b, smoothLocal.x);
                float x2 = lerp(c, d, smoothLocal.x);
                return lerp(x1, x2, smoothLocal.y);
            }

            float Fbm(float2 uv)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float2x2 octaveMatrix = float2x2(1.6, -1.2, 1.2, 1.6);

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    value += ValueNoise(uv) * amplitude;
                    uv = mul(octaveMatrix, uv) * 1.12;
                    amplitude *= 0.5;
                }

                return value;
            }

            float ComputeFogLayerMask(float reveal, float softness, float edgeNoise)
            {
                float edge = smoothstep(0.5 - softness + edgeNoise, 0.5 + softness + edgeNoise, reveal);
                return 1.0 - edge;
            }

            float SampleBlurredReveal(float2 uv, float2 texelSize, float radius, float rotation)
            {
                float2 dirA = float2(cos(rotation), sin(rotation));
                float2 dirB = float2(-dirA.y, dirA.x);
                float2 offsetA = dirA * texelSize * radius;
                float2 offsetB = dirB * texelSize * radius;
                float2 offsetC = (dirA + dirB) * texelSize * radius * 0.7;
                float2 offsetD = (dirA - dirB) * texelSize * radius * 0.7;

                float sum = tex2D(_RevealTex, uv).r;
                sum += tex2D(_RevealTex, uv + offsetA).r;
                sum += tex2D(_RevealTex, uv - offsetA).r;
                sum += tex2D(_RevealTex, uv + offsetB).r;
                sum += tex2D(_RevealTex, uv - offsetB).r;
                sum += tex2D(_RevealTex, uv + offsetC).r;
                sum += tex2D(_RevealTex, uv - offsetC).r;
                sum += tex2D(_RevealTex, uv + offsetD).r;
                sum += tex2D(_RevealTex, uv - offsetD).r;

                return sum / 9.0;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _RevealTex);
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldXY = worldPos.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 safeWorldSize = max(_WorldSize.xy, float2(0.001, 0.001));
                float2 maskUv = saturate((i.worldXY - _WorldMin.xy) / safeWorldSize);

                float time = _Time.y;
                float2 baseUv = i.worldXY;
                float2 warpUv = baseUv * _WarpScale + _WarpSpeed.xy * time;
                float2 warp = float2(
                    Fbm(warpUv + float2(5.1, 1.7)),
                    Fbm(warpUv + float2(-2.4, 3.8)))
                    - 0.5;

                float2 swirl = float2(
                    sin(baseUv.y * 0.42 + time * _SwirlSpeed),
                    cos(baseUv.x * 0.37 - time * (_SwirlSpeed * 0.85)))
                    * _SwirlStrength;

                float2 driftedUv = baseUv + (warp * _WarpStrength) + swirl;

                float noiseA = Fbm(driftedUv * _NoiseScaleA + _NoiseSpeedA.xy * time);
                float noiseB = Fbm(driftedUv * _NoiseScaleB + _NoiseSpeedB.xy * time + float2(3.1, -4.6));
                float noiseC = Fbm(driftedUv * _NoiseScaleC + _NoiseSpeedC.xy * time + float2(-6.2, 2.7));
                float layeredNoise = noiseA * 0.5 + noiseB * 0.3 + noiseC * 0.2;
                float pulse = sin(time * 0.9 + (baseUv.x + baseUv.y) * 0.18) * _PulseStrength;

                float maskRotation = (noiseA * 6.2831853) + time * 0.05;
                float2 texelSize = _RevealTex_TexelSize.xy;
                float revealSoft = SampleBlurredReveal(maskUv, texelSize, _MaskBlurRadius, maskRotation);
                float revealWide = SampleBlurredReveal(maskUv, texelSize, _MaskBlurRadiusWide, -maskRotation * 0.7);

                float edgeNoiseA = ((noiseA - 0.5) + pulse) * _EdgeNoiseStrength;
                float edgeNoiseB = ((noiseB - 0.5) - pulse * 0.6) * (_EdgeNoiseStrength * 0.85);
                float edgeNoiseC = ((noiseC - 0.5) + pulse * 0.4) * (_EdgeNoiseStrength * 0.7);

                float fogMaskA = ComputeFogLayerMask(revealSoft, _EdgeSoftness, edgeNoiseA) * _LayerAlphaA;
                float fogMaskB = ComputeFogLayerMask(revealSoft, _EdgeSoftnessLayerB, edgeNoiseB) * _LayerAlphaB;
                float fogMaskC = ComputeFogLayerMask(revealWide, _EdgeSoftnessLayerC, edgeNoiseC) * _LayerAlphaC;

                float transitionBand = saturate(1.0 - abs(revealWide * 2.0 - 1.0) / max(_VeilWidth, 0.0001));
                float veilNoise = Fbm(baseUv * _VeilNoiseScale + warp * 0.8 + float2(time * 0.025, -time * 0.018));
                float veilAlpha = transitionBand * saturate(0.55 + (veilNoise - 0.5) * _VeilNoiseStrength * 2.0) * _VeilOpacity;

                float fogMask = saturate(fogMaskA + fogMaskB + fogMaskC + veilAlpha);

                float innerMovement = saturate(layeredNoise + pulse);
                float density = saturate(_FogOpacity + (innerMovement - 0.5) * _InnerNoiseOpacity * 2.0);
                float highlight = saturate(innerMovement * 1.15);
                float3 color = lerp(_FogColor.rgb, _FogHighlightColor.rgb, highlight);
                float alpha = fogMask * density;

                return float4(color, alpha);
            }
            ENDCG
        }
    }
}
