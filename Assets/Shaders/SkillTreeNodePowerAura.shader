Shader "SkillTree/Node Power Aura"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Power ("Power", Range(0,1)) = 0
        [HDR] _PowerColor ("Power Color", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Range(0,8)) = 1
        _RingIntensity ("Ring Intensity", Range(0,8)) = 1
        _RayIntensity ("Ray Intensity", Range(0,8)) = 1

        _InnerRadius ("Inner Radius", Range(0,1)) = 0.34
        _RingRadius ("Ring Radius", Range(0,1)) = 0.48
        _RingWidth ("Ring Width", Range(0.001,0.25)) = 0.018
        _AuraRadius ("Aura Radius", Range(0.1,1.8)) = 0.98
        _RayLength ("Ray Length", Range(0,2)) = 0.86
        _RayWidth ("Ray Width", Range(0.001,0.2)) = 0.016
        _RayCoreWidth ("Ray Core Width", Range(0.0005,0.05)) = 0.004
        _RayBaseLength ("Ray Base Length", Range(0.01,0.5)) = 0.14
        _RayPulseSpeed ("Ray Pulse Speed", Range(0,5)) = 0.85
        _RayLengthPulse ("Ray Length Pulse", Range(0,0.4)) = 0.08
        _RayWidthPulse ("Ray Width Pulse", Range(0,0.4)) = 0.06
        _RayIntensityPulse ("Ray Intensity Pulse", Range(0,0.6)) = 0.12
        _Softness ("Softness", Range(0.001,0.2)) = 0.018
        _TextureAlphaInfluence ("Texture Alpha Influence", Range(0,1)) = 0
        _EdgeFade ("Edge Fade", Range(0.001,0.25)) = 0.09
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Power;
            float4 _PowerColor;
            float _GlowIntensity;
            float _RingIntensity;
            float _RayIntensity;
            float _InnerRadius;
            float _RingRadius;
            float _RingWidth;
            float _AuraRadius;
            float _RayLength;
            float _RayWidth;
            float _RayCoreWidth;
            float _RayBaseLength;
            float _RayPulseSpeed;
            float _RayLengthPulse;
            float _RayWidthPulse;
            float _RayIntensityPulse;
            float _Softness;
            float _TextureAlphaInfluence;
            float _EdgeFade;

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
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float Ring(float d, float radius, float width, float softness)
            {
                float halfWidth = width * 0.5;
                float dist = abs(d - radius);
                return 1.0 - smoothstep(halfWidth, halfWidth + softness, dist);
            }

            float AxisRay(float along, float across, float start, float length, float width, float softness)
            {
                float head = smoothstep(start, start + softness, abs(along));
                float localDistance = max(0.0, abs(along) - start);
                float rayDistance = saturate(localDistance / max(length, 0.0001));
                float baseDistance = saturate(localDistance / max(_RayBaseLength, 0.0001));

                float baseWidth = width * (1.0 - smoothstep(0.08, 1.0, baseDistance));
                float coreWidth = _RayCoreWidth * lerp(1.45, 0.45, rayDistance);
                float bodyWidth = max(baseWidth, coreWidth);

                float baseBody = 1.0 - smoothstep(bodyWidth, bodyWidth + softness, abs(across));
                float hotCore = 1.0 - smoothstep(coreWidth, coreWidth + softness * 0.45, abs(across));
                float baseGlow = (1.0 - smoothstep(width * 1.8, width * 1.8 + softness * 1.8, abs(across)))
                    * (1.0 - smoothstep(0.0, 1.0, baseDistance));

                float lineFade = pow(saturate(1.0 - rayDistance), 1.45);
                float endFade = 1.0 - smoothstep(0.72, 1.0, rayDistance);
                float baseFlash = 1.0 - smoothstep(0.0, 1.0, baseDistance);

                return head * endFade * (baseGlow * baseFlash * 0.55 + baseBody * lineFade * 0.75 + hotCore * lineFade * 1.6);
            }

            float RayPulse(float phase, float speedMul)
            {
                if (_RayPulseSpeed <= 0.0)
                    return 0.0;

                float t = _Time.y * _RayPulseSpeed * speedMul + phase;
                float primary = sin(t) * 0.5 + 0.5;
                float secondary = sin(t * 1.73 + phase * 0.37) * 0.5 + 0.5;
                return primary * 0.72 + secondary * 0.28;
            }

            float PulsedAxisRay(
                float along,
                float across,
                float start,
                float length,
                float width,
                float softness,
                float phase,
                float speedMul)
            {
                float pulse = RayPulse(phase, speedMul);
                float centeredPulse = pulse * 2.0 - 1.0;
                float pulsedLength = length * (1.0 + centeredPulse * _RayLengthPulse);
                float pulsedWidth = width * (1.0 + centeredPulse * _RayWidthPulse);
                float intensity = 1.0 + centeredPulse * _RayIntensityPulse;
                return AxisRay(along, across, start, pulsedLength, pulsedWidth, softness) * intensity;
            }

            float EdgeFade(float2 uv)
            {
                float left = smoothstep(0.0, _EdgeFade, uv.x);
                float right = smoothstep(0.0, _EdgeFade, 1.0 - uv.x);
                float bottom = smoothstep(0.0, _EdgeFade, uv.y);
                float top = smoothstep(0.0, _EdgeFade, 1.0 - uv.y);
                return left * right * bottom * top;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float textureAlpha = tex2D(_MainTex, i.uv).a;
                float p = saturate(_Power);
                float visualPower = smoothstep(0.0, 1.0, p);

                float2 centered = i.uv * 2.0 - 1.0;
                float d = length(centered);
                float haloCore = 1.0 - smoothstep(_InnerRadius, _AuraRadius, d);
                haloCore *= smoothstep(0.06, _InnerRadius, d);
                float outerHalo = 1.0 - smoothstep(_RingRadius, _AuraRadius, d);
                outerHalo *= smoothstep(_InnerRadius, _RingRadius, d);

                float ring = Ring(d, _RingRadius, _RingWidth, _Softness);
                float hotRing = Ring(d, _RingRadius, _RingWidth * lerp(0.45, 0.85, visualPower), _Softness * 0.35);

                float rayStart = _RingRadius * 0.82;
                float rayLength = _RayLength * lerp(0.24, 1.0, visualPower);
                float rayWidth = _RayWidth * lerp(0.75, 1.55, visualPower);
                float horizontalBoost = lerp(1.0, 1.85, smoothstep(0.55, 1.0, p));

                float horizontalRay = PulsedAxisRay(centered.x, centered.y, rayStart, rayLength, rayWidth, _Softness, 0.4, 1.0) * horizontalBoost;
                float verticalRay = PulsedAxisRay(centered.y, centered.x, rayStart, rayLength * 0.72, rayWidth * 0.9, _Softness, 2.7, 0.83);
                float diagonalRayA = PulsedAxisRay((centered.x + centered.y) * 0.7071, (centered.x - centered.y) * 0.7071, rayStart, rayLength * 0.56, rayWidth * 0.55, _Softness, 4.1, 1.17);
                float diagonalRayB = PulsedAxisRay((centered.x - centered.y) * 0.7071, (centered.x + centered.y) * 0.7071, rayStart, rayLength * 0.56, rayWidth * 0.55, _Softness, 5.9, 0.71);

                float glowEnergy = (haloCore * 0.45 + outerHalo * 0.8) * _GlowIntensity * visualPower;
                float ringEnergy = (ring * 0.9 + hotRing * 1.7) * _RingIntensity * lerp(0.35, 1.0, visualPower);
                float rayEnergy = (horizontalRay + verticalRay + (diagonalRayA + diagonalRayB) * 0.6) * _RayIntensity;

                float energy = glowEnergy + ringEnergy + rayEnergy;
                float alphaMask = lerp(1.0, smoothstep(0.0, 0.02, textureAlpha), _TextureAlphaInfluence);
                energy *= alphaMask * EdgeFade(i.uv);

                float3 rgb = _PowerColor.rgb * energy * i.color.rgb;
                return float4(rgb, saturate(energy));
            }
            ENDCG
        }
    }
}
