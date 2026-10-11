Shader "SkillTree/Ailment Particle"
{
    Properties
    {
        _Kind ("Kind (Mote/Drop/Wound/Splash)", Float) = 0
        _Emission ("Emission", Float) = 1
        _PaintStrength ("Paint Strength", Range(0,1)) = 1
        _PaintSteps ("Paint Steps", Range(2,12)) = 5
        _PaintSize ("Paint Patch Size", Float) = 0.14
        _PaintVariation ("Paint Variation", Range(0,1)) = 0.65
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "PainterlyColor.hlsl"
            float _Kind,_Emission,_PaintStrength,_PaintSteps,_PaintSize,_PaintVariation;
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            float BloodShard(float2 p,float2 direction,float size,float width)
            {
                float along=dot(p,direction);
                float side=abs(dot(p,float2(direction.y,-direction.x)));
                float taper=width*(1-saturate(along/size));
                return (1-smoothstep(taper,taper+0.025,side))*smoothstep(-0.15,0,along)*
                    (1-smoothstep(size-0.06,size,along));
            }
            float BloodClot(float2 p)
            {
                float body=1-smoothstep(0.3,0.43,length(float2(p.x*0.8,p.y*1.25)));
                float shard=BloodShard(p,normalize(float2(1,0.35)),0.9,0.24);
                shard=max(shard,BloodShard(p,normalize(float2(-1,0.7)),0.78,0.21));
                shard=max(shard,BloodShard(p,normalize(float2(0.25,-1)),0.72,0.16));
                shard=max(shard,BloodShard(p,normalize(float2(-0.7,-0.35)),0.67,0.22));
                return max(body,shard);
            }
            float4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float mask, highlight;
                if (_Kind>2.5)
                {
                    mask=BloodClot(p);
                    highlight=step(0,p.y+p.x*0.3)*step(0.08,p.x)*0.5;
                }
                else if (_Kind>1.5)
                {
                    // Asymmetric painted blood clump and sharp facets, matching the wound reference.
                    mask=BloodClot(p*1.05);
                    highlight=step(0.1,p.y-p.x*0.35)*step(-0.25,p.x)*0.65;
                }
                else if (_Kind>0.5)
                {
                    // Angular teardrop: pointed at top, fuller lower body.
                    float lower=saturate((p.y+0.9)/0.65);
                    float width=p.y<-0.25 ? min(lower*0.8,0.55) : 0.55*(1-saturate((p.y+0.25)/1.15));
                    float d=max(abs(p.x)-width,max(p.y-0.9,-p.y-0.9));
                    mask=1-smoothstep(-0.04,0.04,d);
                    highlight=(1-smoothstep(0.025,0.09,abs(p.x+width*0.7)))*step(-0.65,p.y);
                }
                else
                {
                    mask=1-smoothstep(0.65,0.82,abs(p.x)+abs(p.y));
                    highlight=step(0,p.y)*0.4;
                }
                float patch=SkillTreePaintPatch(p,_PaintSize);
                float3 c=i.color.rgb*_Emission*(0.55+patch*0.55);
                if (_Kind>1.5 && _Kind<2.5) c*=0.7;
                if (_Kind>0.5) c+=float3(0.8,0.38,0.44)*highlight;
                float3 rgb=SkillTreePaintColor(c,patch,_PaintStrength,_PaintSteps,_PaintVariation);
                float alpha=saturate(SkillTreePaintEnergy(mask,patch,_PaintStrength,_PaintSteps,_PaintVariation));
                return float4(rgb,alpha*i.color.a);
            }
            ENDCG
        }
    }
}
