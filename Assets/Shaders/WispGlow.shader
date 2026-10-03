Shader "SkillTree/Wisp Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (3, 3, 3, 1)
        [Header(Painterly Color)]
        _PaintStrength ("Paint Strength", Range(0, 1)) = 1
        _PaintSteps ("Paint Tonal Steps", Range(2, 12)) = 5
        _PaintSize ("Paint Patch Size", Range(0.001, 0.4)) = 0.14
        _PaintVariation ("Paint Patch Variation", Range(0, 1)) = 0.65
        [HideInInspector] _TrailMode ("Trail Mode", Float) = 0
        [HideInInspector] _WispTheme ("Steel Ash Frost Storm", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
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
            float4 _EmissionColor;
            float _PaintStrength, _PaintSteps, _PaintSize, _PaintVariation;
            float _TrailMode, _WispTheme;

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 sprite = tex2D(_MainTex, input.uv);
                if (_TrailMode > 0.5)
                {
                    float across = abs(input.uv.y * 2.0 - 1.0);
                    float body;
                    if (_WispTheme < 0.5) // Steel: a narrow, clean blade.
                        body = 1.0 - smoothstep(0.65, 1.0, across);
                    else if (_WispTheme < 1.5) // Ash: uneven licking flame edges.
                    {
                        float flame = 0.72 + 0.18 * sin(input.uv.x * 31.0 - _Time.y * 7.0)
                            + 0.1 * sin(input.uv.x * 57.0 + _Time.y * 4.0);
                        body = 1.0 - smoothstep(flame * 0.3, flame, across);
                    }
                    else if (_WispTheme < 2.5) // Frost: a soft crystalline ribbon.
                        body = pow(saturate(1.0 - across), 0.65);
                    else // Storm: bright, thin lightning along the angular path.
                        body = (1.0 - smoothstep(0.12, 0.36, across))
                            + (1.0 - smoothstep(0.36, 1.0, across)) * 0.3;
                    sprite = float4(1.0, 1.0, 1.0, saturate(body));
                }
                float2 patchUv = input.uv * 2.0 - 1.0;
                if (_TrailMode > 0.5) patchUv.x *= 4.0;
                float patchValue = SkillTreePaintPatch(patchUv, _PaintSize);
                float3 color = SkillTreePaintColor(sprite.rgb * _EmissionColor.rgb, patchValue,
                    _PaintStrength, _PaintSteps, _PaintVariation);
                float alpha = saturate(SkillTreePaintEnergy(sprite.a, patchValue,
                    _PaintStrength, _PaintSteps, _PaintVariation));
                // Apply lifetime fading after banding so trail tails disappear smoothly.
                return float4(color, alpha * saturate(_EmissionColor.a) * input.color.a);
            }
            ENDCG
        }
    }
}
