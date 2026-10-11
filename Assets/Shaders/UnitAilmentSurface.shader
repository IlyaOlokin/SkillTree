Shader "SkillTree/Ailment Surface"
{
    Properties
    {
        _ShapeRadius ("Shape Radius", Float) = 0.48
        _TriangleSize ("Triangle Size", Float) = 0.72
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
            float _ShapeType, _ShapeRadius, _TriangleSize;
            #include "UnitShapeGeometry.hlsl"
            float _Extent, _Rotation, _MirrorX, _MirrorY, _Layer, _LineWidth, _VisualTime;
            float _FireSize, _FireApplicationBoost, _FrostSize, _IceAlpha, _ChargeOffset, _ChargeTravel;
            float _PaintStrength, _PaintSteps, _PaintSize, _PaintVariation;
            float4 _State, _Pulse, _Growth, _Established, _Exit;
            float4 _IgniteColor, _ChillColor, _FreezeColor, _ChargeColor;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            float2 Rot(float2 p,float radians)
            {
                float s=sin(radians),c=cos(radians);
                return float2(p.x*c-p.y*s,p.x*s+p.y*c);
            }
            void Anchor(float t,out float2 anchorPoint,out float2 normal)
            {
                if (_ShapeType<0.5)
                {
                    normal=float2(sin(t*6.2831853),cos(t*6.2831853)); anchorPoint=normal*_ShapeRadius;
                }
                else if (_ShapeType<1.5) SquareAnchor(t,anchorPoint,normal);
                else TriangleAnchor(t,anchorPoint,normal);
            }
            float2 ToMesh(float2 p)
            {
                p=Rot(p,-_Rotation*0.01745329252);
                p*=float2(_MirrorX>0.5?-1:1,_MirrorY>0.5?-1:1);
                return p;
            }
            float Band(float d,float width) { return 1-smoothstep(width,width+0.008,abs(d)); }
            void VerticalLimits(float2 axis,out float bottom,out float top)
            {
                bottom=-_ShapeRadius; top=_ShapeRadius;
                if (_ShapeType<0.5) return;
                bottom=100; top=-100;
                float sides=_ShapeType<1.5?4:3;
                [unroll] for (int j=0;j<4;j++)
                {
                    float2 a,n; Anchor(j/sides,a,n);
                    float height=dot(ToMesh(a),axis);
                    bottom=min(bottom,height); top=max(top,height);
                }
            }
            float BottomUp(float height,float bottom,float top,float coverage)
            {
                float front=lerp(bottom-0.03,top+0.03,coverage);
                return 1-smoothstep(front-0.025,front+0.025,height);
            }
            float Flame(float2 p,float2 anchor,float2 up,float height,float phase)
            {
                float2 delta=p-anchor;
                float along=dot(delta,up)/max(height,0.001);
                float side=dot(delta,float2(up.y,-up.x));
                float bend=sin(along*6.0+phase)*height*0.09*along;
                float width=height*(0.3*(1-saturate(along))+0.02);
                float edge=1-smoothstep(width,width+0.006,abs(side-bend));
                float rootSide=abs(side-bend)/max(height*0.33,0.001);
                float root=-0.2*sqrt(saturate(1-rootSide*rootSide));
                root+=0.02*sin(rootSide*13+phase);
                return edge*smoothstep(root-0.02,root+0.025,along)*(1-smoothstep(0.82,1,along));
            }
            float Frost(float2 p,float t)
            {
                float2 anchor,n; Anchor(t,anchor,n);
                float2 delta=p-anchor;
                float2 tangent=float2(n.y,-n.x);
                float length=_FrostSize*lerp(0.3,1,max(_Growth.y,_Established.y))*(1+0.45*_Pulse.y)*(1-_Exit.y);
                if (length<0.001) return 0;
                float a=dot(delta,-n),x=dot(delta,tangent);
                float stem=Band(x,length*0.045)*step(0,a)*(1-smoothstep(length*0.85,length,a));
                // Two short broad leaves, not a fractal/connected lightning web.
                float leaves=0;
                [unroll] for (int j=0;j<2;j++)
                {
                    float2 leaf=float2(x,a-length*(0.28+j*0.31));
                    float l=abs(leaf.x)/max(length*0.42,0.001);
                    float diagonal=abs(leaf.y-abs(leaf.x)*0.5);
                    leaves=max(leaves,(1-smoothstep(length*0.06,length*0.1,diagonal))*(1-smoothstep(0.2,1,l)));
                }
                return max(stem,leaves);
            }
            float4 frag(v2f i):SV_Target
            {
                float2 q=(i.uv*2-1)*_Extent;
                float2 p=q*float2(_MirrorX>0.5?-1:1,_MirrorY>0.5?-1:1);
                p=Rot(p,_Rotation*0.01745329252);
                float sd,t; ShapeSdf(p,sd,t);
                float patch=SkillTreePaintPatch(p,_PaintSize);
                float aa=max(0.003,fwidth(sd));
                float rimGap=smoothstep(_LineWidth*0.55,_LineWidth*0.55+aa,abs(sd));
                float coreGap=smoothstep(0.07,0.13,length(p));
                float3 color=0; float alpha=0; float dimming=0;
                if (_Layer<0.5)
                {
                    if (_State.z>0.5)
                    {
                        // Polygonal cells with crack seams; static in unit coordinates.
                        float2 cells=float2(p.x+p.y*0.37,p.y-p.x*0.19)*7;
                        float2 f=frac(cells);
                        float seam=min(min(f.x,1-f.x),min(f.y,1-f.y));
                        seam=min(seam,abs(f.x-f.y)*0.65);
                        float cracks=1-smoothstep(0.008,0.034,seam);
                        float depth=_ShapeRadius*(_ShapeType<0.5?1:(_ShapeType<1.5?0.89:0.76*_TriangleSize));
                        float frontDepth=depth*_Growth.z;
                        float inside=1-smoothstep(-aa,aa,sd);
                        float coverage=inside*max(_Established.z,smoothstep(-aa,aa,sd+frontDepth));
                        // Thaw starts in the deepest interior and leaves the outside edge last.
                        float thaw=smoothstep(-aa,aa,sd+depth*(1-_Exit.z));
                        coverage*=thaw;
                        float front=Band(sd+frontDepth,0.035)*inside*_Pulse.z;
                        float energy=0.6+patch*0.65+cracks*0.55+_Pulse.z*1.4;
                        color=_FreezeColor.rgb*energy+float3(0.55,0.85,1.1)*front;
                        alpha=max(coverage*(_IceAlpha+cracks*0.17+_Pulse.z*0.35),front*thaw*0.85)*rimGap*coreGap;
                    }
                    else if (_State.y>0.5)
                    {
                        float frost=max(Frost(p,0.86),Frost(p,0.38));
                        color=_ChillColor.rgb*(0.9+patch*0.7+_Pulse.y*0.4);
                        alpha=frost*rimGap*coreGap*0.92;
                    }
                }
                else
                {
                    if (_State.x>0.5)
                    {
                        float fire=0;
                        float2 up=normalize(mul((float3x3)unity_WorldToObject,float3(0,1,0)).xy);
                        float2 heightAxis=normalize(float2(unity_ObjectToWorld._m10,unity_ObjectToWorld._m11));
                        float bottom,top; VerticalLimits(heightAxis,bottom,top);
                        [unroll] for (int k=0;k<24;k++)
                        {
                            float anchorT=(k+0.37)/24.0;
                            float2 a,n; Anchor(anchorT,a,n);
                            a=ToMesh(a); n=ToMesh(n);
                            float persistent=(k%2)==0?1:0;
                            float reach=BottomUp(dot(a,heightAxis),bottom,top,_Growth.x);
                            float quietReach=max(reach,_Established.x);
                            float height=_FireSize*(1+0.22*sin(_VisualTime*7+k*2)+_Pulse.x*reach*_FireApplicationBoost);
                            float flame=Flame(q,a+n*0.022,up,height,_VisualTime*4+k);
                            float extra=(k%4)==1?1:0;
                            fire=max(fire,flame*max(persistent*quietReach,extra*_Pulse.x*reach));
                        }
                        // A smaller front rises from the physical bottom of any silhouette.
                        float flash=Band(sd-0.035,0.01+_Pulse.x*0.02)*_Pulse.x*
                            BottomUp(dot(q,heightAxis),bottom,top,_Growth.x);
                        float mask=max(fire*(0.7+0.3*rimGap),flash*rimGap);
                        float heat=0.7+patch*0.8;
                        color=_IgniteColor.rgb*heat;
                        alpha=saturate(SkillTreePaintEnergy(mask*0.9,patch,_PaintStrength,_PaintSteps,_PaintVariation))*(1-_Exit.x);
                        dimming=alpha*(1-_Exit.x);
                    }
                    if (_State.w>0.5)
                    {
                        // The path is fixed in shape coordinates; only its visibility advances.
                        float pathVertex=floor(t*48);
                        float pathFraction=frac(t*48);
                        float from=SkillTreePaintHash(float2(pathVertex,17.3));
                        float to=SkillTreePaintHash(float2(fmod(pathVertex+1,48),17.3));
                        float zigzag=(lerp(from,to,pathFraction)-0.5)*0.075;
                        float arc=Band(sd-(_ChargeOffset+zigzag),0.009+_Pulse.w*0.012);
                        float behind=frac((_ChargeTravel-t)*3+0.18);
                        // After removal the leading edge holds; the tail catches up to it.
                        float trailLength=max(0.0001,(0.36+_Pulse.w*0.3)*(1-_Exit.w));
                        float reveal=smoothstep(0,0.018,behind);
                        float tail=1-smoothstep(trailLength*0.25,trailLength,behind);
                        float gate=reveal*tail;
                        float flicker=0.9+0.1*sin(_VisualTime*13+t*19);
                        float mask=arc*gate*flicker*rimGap;
                        float chargeAlpha=saturate(SkillTreePaintEnergy(mask*0.95,patch,_PaintStrength,_PaintSteps,_PaintVariation));
                        float combined=chargeAlpha+alpha*(1-chargeAlpha);
                        dimming=chargeAlpha+dimming*(1-chargeAlpha);
                        color=(_ChargeColor.rgb*(1+patch*0.65+_Pulse.w*0.5)*chargeAlpha+
                            color*alpha*(1-chargeAlpha))/max(combined,0.00001);
                        alpha=combined;
                    }
                }
                color=SkillTreePaintColor(color,patch,_PaintStrength,_PaintSteps,_PaintVariation);
                if (_Layer>0.5 && alpha>0) color*=dimming/alpha;
                if (_Layer<0.5) alpha=saturate(SkillTreePaintEnergy(alpha,patch,_PaintStrength,_PaintSteps,_PaintVariation));
                float edge=1-smoothstep(0.93,1,max(abs(i.uv.x*2-1),abs(i.uv.y*2-1)));
                return float4(color,alpha*edge);
            }
            ENDCG
        }
    }
}
