Shader "Unlit/NodeConnectionShader"
{
Properties
    {
        _StateTex ("State Texture", 2D) = "white" {}
        _StateTexWidth ("State Tex Width", Float) = 1
        _BaseWidth ("Base Width", Float) = 1
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
            float _StateTexWidth;
            float _BaseWidth;

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
                float4 col : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;

                float id = v.uv2.x;
                float2 stateUV = float2((id + 0.5) / _StateTexWidth, 0.5);
                float4 state = tex2Dlod(_StateTex, float4(stateUV, 0, 0));

                float thickness = state.r;
                float side = v.uv.y;

                float3 normal = v.normal;
                float3 offset = normal * side * thickness * _BaseWidth;
                v.vertex.xyz += offset;

                v.vertex.xyz += offset;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.col = float4(state.gba, 1);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.col;
            }
            ENDCG
        }
    }
}
