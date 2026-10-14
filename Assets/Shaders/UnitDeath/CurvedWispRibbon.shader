Shader "SkillTree/Unit Death/Curved Wisp Ribbon"
{
    Properties
    {
        [HDR] _Color ("Glow Color", Color) = (3.2,0.1,0.1,1)
        [HDR] _CoreColor ("Core Color", Color) = (1,0.9,0.9,1)
        _PaintStrength ("Paint Strength", Range(0,1)) = 1
        _PaintSteps ("Paint Steps", Float) = 5
        _PaintSize ("Paint Size", Float) = 0.14
        _PaintVariation ("Paint Variation", Range(0,1)) = 0.65
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "../PainterlyColor.hlsl"
            float4 _Color, _CoreColor;
            float _PaintStrength, _PaintSteps, _PaintSize, _PaintVariation;
            struct Vertex { float4 position:POSITION; float2 uv:TEXCOORD0; float2 kind:TEXCOORD1; float4 color:COLOR; };
            struct Pixel { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float kind:TEXCOORD1; float4 color:COLOR; };
            Pixel vert(Vertex input)
            {
                Pixel output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv; output.kind = input.kind.x; output.color = input.color;
                return output;
            }
            float4 frag(Pixel input):SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float coverage, hot;
                if (input.kind > 1.5)
                {
                    coverage = 1.0 - smoothstep(0.15, 1.0, abs(p.x));
                    hot = 1.0 - smoothstep(0.03, 0.25, abs(p.x));
                }
                else
                {
                    float distance = input.kind > 0.5 ? max(abs(p.x), abs(p.y)) - 0.58
                        : max(abs(p.x) * 0.8660254 + p.y * 0.5, -p.y) - 0.42;
                    float softness = max(0.06, fwidth(distance));
                    coverage = 1.0 - smoothstep(0, softness, distance);
                    hot = coverage * 0.65;
                }
                float patch = SkillTreePaintPatch(p, _PaintSize);
                float glow = SkillTreePaintEnergy(coverage * 0.65, patch, _PaintStrength, _PaintSteps, _PaintVariation);
                float core = SkillTreePaintEnergy(hot * 1.5, patch, _PaintStrength, _PaintSteps, _PaintVariation);
                return float4((_Color.rgb * glow + _CoreColor.rgb * core) * input.color.rgb,
                    coverage * input.color.a);
            }
            ENDCG
        }
    }
}
