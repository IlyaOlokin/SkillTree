Shader "Unlit/NodeConnectionShader"
{
Properties
    {
        _StateTex ("State Texture", 2D) = "white" {}
        _ProgressTex ("Progress Texture", 2D) = "black" {}
        _StateTexWidth ("State Tex Width", Float) = 1
        _BaseWidth ("Base Width", Float) = 1
        _DefaultColor ("Default Color", Color) = (1,1,1,1)
        _FrontWidth ("Front Width", Float) = 0.05
        _FrontThicknessBoost ("Front Thickness Boost", Float) = 0.35
        _FrontThicknessWidth ("Front Thickness Width", Float) = 0.08
        [HDR]_FrontGlowColor ("Front Glow Color", Color) = (1.5,1.8,1.2,1)
        _FrontGlowWidth ("Front Glow Width", Float) = 0.06
        _FrontGlowIntensity ("Front Glow Intensity", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _StateTex;
            sampler2D _ProgressTex;
            float _StateTexWidth;
            float _BaseWidth;
            float4 _DefaultColor;
            float _FrontWidth;
            float _FrontThicknessBoost;
            float _FrontThicknessWidth;
            float4 _FrontGlowColor;
            float _FrontGlowWidth;
            float _FrontGlowIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0; // x = t, y = side
                float2 uv2    : TEXCOORD1; // x = connection id
                float3 normal : NORMAL; 
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 allocatedCol : COLOR;
                float t : TEXCOORD0;
                float3 progressData : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;

                float id = v.uv2.x;
                float2 stateUV = float2((id + 0.5) / _StateTexWidth, 0.5);
                float4 state = tex2Dlod(_StateTex, float4(stateUV, 0, 0));
                float4 progressState = tex2Dlod(_ProgressTex, float4(stateUV, 0, 0));

                float directedT = progressState.g > 0.5 ? (1.0 - v.uv.x) : v.uv.x;
                float frontActive = progressState.b;
                float frontThicknessWidth = max(_FrontThicknessWidth, 0.0001);
                float frontDistance = abs(directedT - progressState.r);
                float frontThicknessMask = (1.0 - smoothstep(0.0, frontThicknessWidth, frontDistance)) * frontActive;
                float thickness = state.r * (1.0 + frontThicknessMask * _FrontThicknessBoost);
                float side = v.uv.y;

                float3 normal = v.normal;
                float3 offset = normal * side * thickness * _BaseWidth;
                v.vertex.xyz += offset;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.allocatedCol = float4(state.gba, 1);
                o.t = v.uv.x;
                o.progressData = progressState.rgb;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float directedT = i.progressData.y > 0.5 ? (1.0 - i.t) : i.t;
                float frontWidth = max(_FrontWidth, 0.0001);
                float fill = 1.0 - smoothstep(i.progressData.x - frontWidth, i.progressData.x + frontWidth, directedT);
                float glowWidth = max(_FrontGlowWidth, 0.0001);
                float frontDistance = abs(directedT - i.progressData.x);
                float glowMask = (1.0 - smoothstep(0.0, glowWidth, frontDistance)) * i.progressData.z;
                float3 baseColor = lerp(_DefaultColor.rgb, i.allocatedCol.rgb, saturate(fill));
                float3 glowColor = _FrontGlowColor.rgb * (_FrontGlowIntensity * glowMask);
                return float4(baseColor + glowColor, 1);
            }
            ENDCG
        }
    }
}
