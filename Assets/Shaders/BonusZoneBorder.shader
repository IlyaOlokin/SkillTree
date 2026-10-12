Shader "SkillTree/Bonus Zone Border"
{
    Properties
    {
        _Tint ("Energy tint", Color) = (0.12,0.94,0.92,1)
        _LineWidth ("Contour width", Float) = 0.018
        _HaloWidth ("Halo width", Float) = 0.16
        _Brightness ("Contour brightness", Float) = 1.25
        _HaloBrightness ("Halo brightness", Float) = 0.22
        _SparkCount ("Sparks around perimeter", Float) = 300
        _SparkSpeed ("Perimeter cycles per second", Float) = 0.004
        _SparkIntensity ("Spark intensity", Float) = 0.8
        _Phase ("Phase seed", Float) = 1.7
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off Blend SrcAlpha One
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _Tint;
            float _LineWidth,_HaloWidth,_Brightness,_HaloBrightness,_SparkCount,_SparkSpeed,_SparkIntensity,_Phase;
            struct Input { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Output Vert(Input i) { Output o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; return o; }
            half4 Frag(Output i) : SV_Target
            {
                float d=abs(i.uv.y), aa=max(fwidth(i.uv.y),0.0005);
                float core=1-smoothstep(_LineWidth*.5,_LineWidth*.5+aa,d);
                float halo=exp(-d*d/max(_HaloWidth*_HaloWidth*.13,0.00001))*(1-smoothstep(_HaloWidth*.7,_HaloWidth,d));
                // Integer count and periodic phase make the closing edge seamless.
                float u=frac(i.uv.x+_Time.y*_SparkSpeed+_Phase)*_SparkCount;
                float id=floor(u);
                float offset=sin(id*12.9898+_Phase)*_HaloWidth*.42;
                float shimmer=pow(.5+.5*sin(_Time.y*1.3+id*2.399),3);
                float spark=exp(-pow((frac(u)-.5)*9,2)-pow((i.uv.y-offset)/max(_LineWidth*.55,aa),2))*shimmer;
                float intensity=core*_Brightness+halo*_HaloBrightness+spark*_SparkIntensity;
                return half4(lerp(_Tint.rgb,half3(.72,1,1),core*.65), intensity*_Tint.a);
            }
            ENDHLSL
        }
    }
}
