Shader "SkillTree/Bonus Zone Fill"
{
    Properties
    {
        [PerRendererData] _MainTex ("SpriteShape texture", 2D) = "white" {}
        _Tint ("Energy tint", Color) = (0.12,0.94,0.92,1)
        _Opacity ("Fill opacity", Range(0,0.3)) = 0.065
        _FillContrast ("Transparent valley contrast", Range(1,5)) = 2.6
        _DotDensity ("Interior dot density", Range(0.1,5)) = 1.8
        _DotRadius ("Interior dot radius", Range(0.003,0.06)) = 0.018
        _DotIntensity ("Interior dot intensity", Range(0,1)) = 0.5
        _DotSpeed ("Interior dot motion speed", Range(0,1)) = 0.12
        _NoiseScale ("Pattern scale", Float) = 0.8
        _Flow ("Flow velocities", Vector) = (0.035,0.022,-0.015,0.028)
        _Phase ("Phase seed", Float) = 1.7
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _Tint, _Flow;
            float _Opacity, _NoiseScale, _Phase;
            float _FillContrast, _DotDensity, _DotRadius, _DotIntensity, _DotSpeed;
            struct Input { float4 positionOS : POSITION; float4 color : COLOR; };
            struct Output { float4 positionCS : SV_POSITION; float2 local : TEXCOORD0; float4 color : COLOR; };
            Output Vert(Input i) { Output o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.local=i.positionOS.xy; o.color=i.color; return o; }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 cell=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
            float Fractal(float2 p) { return Noise(p)*0.65 + Noise(p*2.03+4.7)*0.25 + Noise(p*4.07)*0.1; }
            half4 Frag(Output i) : SV_Target
            {
                float2 p=i.local*_NoiseScale;
                float t=_Time.y;
                float n=Fractal(p+_Flow.xy*t+_Phase);
                float m=Fractal(float2(p.x*.7-p.y*.7,p.x*.7+p.y*.7)+_Flow.zw*t-_Phase);
                float veinDistance=abs(n+m-1)*3.5;
                float wisps=pow(saturate(1-veinDistance),5);
                float veil=0.3+n*0.45+m*0.25;
                float strength=veil*0.6+wisps*1.3;
                // Lower already dim areas nearly to zero while retaining the bright flow peaks.
                strength=pow(saturate(strength/1.7),_FillContrast)*1.7;
                // A wider Gaussian shoulder fades gradually around the narrow bright veins.
                // Fade its contribution at the core to retain the existing peak brightness.
                float veinHalo=exp(-veinDistance*veinDistance*2.8);
                strength+=veinHalo*.65*(1-wisps);
                // Stable random dot positions in tree units, softly wandering inside each cell.
                // The SpriteShape mesh clips this field to the same authored polygon as the fill.
                float2 dotPosition=i.local*_DotDensity;
                float2 cell=floor(dotPosition), local=frac(dotPosition);
                float seed=Hash(cell+_Phase);
                float2 center=.18+.64*float2(Hash(cell+17.3),Hash(cell-9.2));
                center+=.08*float2(sin(t*_DotSpeed+seed*31),cos(t*_DotSpeed*.73+seed*47));
                float distance=length(local-center);
                float radius=_DotRadius*_DotDensity;
                // Derivatives must use continuous coordinates: distance jumps between random cells.
                float aa=max(length(fwidth(dotPosition))*.5,.0005);
                float core=1-smoothstep(max(0,radius-aa),radius+aa,distance);
                float halo=exp(-distance*distance/max(radius*radius*5,.00001));
                float shimmer=.2+.8*pow(.5+.5*sin(t*.9+seed*53),2);
                float dots=(core+halo*.16)*shimmer*step(.25,seed)*_DotIntensity;
                // Dots fill the negative space without competing with the brightest wisps.
                dots*=1-wisps*.65;
                float fillAlpha=saturate(_Opacity*strength);
                float alpha=saturate(fillAlpha+dots);
                float3 color=lerp(_Tint.rgb, float3(.65,1,1),saturate(dots/max(alpha,.0001))*.45);
                return half4(color*i.color.rgb,alpha*_Tint.a*i.color.a);
            }
            ENDHLSL
        }
    }
}
