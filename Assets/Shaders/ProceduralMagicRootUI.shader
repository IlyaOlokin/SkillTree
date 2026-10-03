Shader "UI/Procedural Magic Root"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture (unused)", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1,0.843137,0.619608,1)
        _Intensity ("Intensity", Float) = 1.5
        [HDR] _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Float) = 0.5
        _CoreWidth ("Core Width", Range(0.05,0.8)) = 0.2
        _CoreSharpness ("Core Sharpness", Range(0.5,4)) = 1.5
        _EdgeBrightness ("Edge Brightness", Range(0,1)) = 0.5
        _GlowFalloff ("Glow Falloff", Range(0,4)) = 1.2
        _Alpha ("Alpha", Range(0,1)) = 1
        _Seed ("Seed", Float) = 17
        _StyleBlend ("Style Blend", Range(0,1)) = 0
        _Thickness ("Thickness", Float) = 0.055
        _EndThickness ("End Thickness", Float) = 0.001
        _VerticalScale ("Vertical Scale", Float) = 1
        _MainPathAmplitude ("Main Path Amplitude", Float) = 0.09
        _MainPathFrequency ("Main Path Frequency", Float) = 6
        _MainPathOffset ("Main Path Offset", Float) = 0
        _MainPathSharpness ("Main Path Sharpness", Range(0,1)) = 0.1
        _BranchDensity ("Branch Density", Range(0,1)) = 0.8
        _BranchCount ("Branch Count", Range(8,96)) = 64
        _BranchLength ("Branch Length", Float) = 0.85
        _BranchWidth ("Branch Width", Float) = 0.6
        _BranchAngle ("Branch Angle", Float) = 28
        _BranchAngleRandomness ("Branch Angle Randomness", Range(0,1)) = 0.35
        _BranchLengthRandomness ("Branch Length Randomness", Range(0,1)) = 0.45
        _SecondaryBranchDensity ("Secondary Density", Range(0,1)) = 0.2
        _SecondaryBranchLength ("Secondary Length", Float) = 0.4
        _MaxBranchSegments ("Branch Segments", Range(2,6)) = 6
        _Smoothness ("Smoothness", Range(0,1)) = 0.65
        _Jaggedness ("Jaggedness", Float) = 0.015
        _JaggedFrequency ("Jagged Frequency", Float) = 20
        _SpikeAmount ("Spike Amount", Range(0,1)) = 0
        _Curvature ("Curvature", Float) = 0.16
        _TipSharpness ("Tip Sharpness", Float) = 1.5
        _TaperPower ("Taper Power", Float) = 1.2
        _DensityFalloff ("Density Falloff", Float) = 0.65
        _LengthFalloff ("Length Falloff", Float) = 0.8
        _ThicknessFalloff ("Thickness Falloff", Float) = 1
        _FillAmount ("Fill Amount", Range(0,1)) = 1
        _FillSoftness ("Fill Softness", Range(0,0.1)) = 0.002
        _AspectRatio ("Aspect Ratio", Float) = 10
        _LeftIntensityBoost ("Left Intensity Boost", Float) = 0.2
        [HideInInspector] _RootRect ("Local Rect", Vector) = (0,0,100,10)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"

            float4 _Color, _GlowColor, _RootRect, _ClipRect;
            float _GlowIntensity;
            float _CoreWidth, _CoreSharpness, _EdgeBrightness, _GlowFalloff;
            float4x4 _RootWorldToLocal;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;
            float _Intensity, _Alpha, _Seed, _StyleBlend, _Thickness, _EndThickness;
            float _VerticalScale, _MainPathAmplitude, _MainPathFrequency, _MainPathOffset, _MainPathSharpness;
            float _BranchCount, _BranchDensity, _BranchLength, _BranchWidth, _BranchAngle, _BranchAngleRandomness;
            float _BranchLengthRandomness, _SecondaryBranchDensity, _SecondaryBranchLength, _MaxBranchSegments;
            float _Smoothness, _Jaggedness, _JaggedFrequency, _SpikeAmount, _Curvature, _TipSharpness, _TaperPower;
            float _DensityFalloff, _LengthFalloff, _ThicknessFalloff, _FillAmount, _FillSoftness, _AspectRatio, _LeftIntensityBoost;
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 mask : TEXCOORD1; float4 color : COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Canvas batching can bake vertices into canvas space. Reconstruct
                // the target Graphic's local position instead of assuming v.vertex is local.
                float2 rootPosition = mul(_RootWorldToLocal, mul(unity_ObjectToWorld, v.vertex)).xy;
                o.uv = (rootPosition - _RootRect.xy) / max(_RootRect.zw, 0.0001);
                o.color = v.color;
                float2 pixelSize = o.vertex.w / abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                o.mask = float4(v.vertex.xy * 2 - rect.xy - rect.zw,
                    0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                return o;
            }
            // Seed, cell, and depth are the only random inputs. No animated noise.
            float hash(float cell, float depth)
            {
                uint h = (uint)cell * 374761393u + (uint)depth * 668265263u + asuint(_Seed);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0x00ffffffu) / 16777216.0;
            }
            float noise(float x, float depth, float sharpness)
            {
                float t = frac(x);
                t = lerp(t*t*(3-2*t), t, sharpness);
                return lerp(hash(floor(x),depth),hash(floor(x)+1,depth),t)*2-1;
            }
            float2 path(float x)
            {
                float y = noise(x * _MainPathFrequency + _MainPathOffset, 1, _MainPathSharpness) * _MainPathAmplitude;
                y += noise(x * _JaggedFrequency, 2, 1) * _Jaggedness * lerp(0.15,1,_StyleBlend);
                return float2(x * _AspectRatio, y);
            }
            // Branch anchors interpolate the actual trunk polyline, never a different curve.
            float2 anchor(float x)
            {
                float k = min(x * 48, 47.9999);
                return lerp(path(floor(k)/48), path((floor(k)+1)/48), frac(k));
            }
            float radius(float x)
            {
                float remaining = saturate(1-x/max(_FillAmount,0.00001));
                return lerp(_EndThickness, _Thickness, pow(remaining, max(0.05,_TaperPower * _ThicknessFalloff)))
                    * smoothstep(0,0.12,remaining);
            }
            float2 segment(float2 p, float2 a, float2 b, float ra, float rb)
            {
                float2 ba = b-a;
                float h = saturate(dot(p-a,ba) / max(dot(ba,ba),1e-8));
                // Evaluate outer silhouette and inner core together, sharing distance work.
                return length(p-a-ba*h) - lerp(ra,rb,h)*float2(1,_CoreWidth);
            }
            float2 join(float2 a, float2 b)
            {
                float2 k = max(0.00001, _Thickness * _Smoothness * lerp(0.35,0.08,_StyleBlend)*float2(1,_CoreWidth));
                float2 h = saturate(0.5 + 0.5*(b-a)/k);
                return lerp(b,a,h)-k*h*(1-h);
            }
            float2 branchPoint(float2 a, float2 delta, float t, float id)
            {
                float bend = sin(t*3.14159265) * _Curvature * length(delta) * (hash(id,7)*2-1) * lerp(1,0.35,_StyleBlend);
                float jag = noise(t * 5 + id, 8, 1) * _Jaggedness * _StyleBlend * min(1,length(delta));
                // Zero displacement at both ends guarantees connected roots and tips.
                return a + delta*t + float2(0,bend + jag*sin(t*3.14159265));
            }
            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p = float2(i.uv.x * _AspectRatio, (i.uv.y-0.5)/max(_VerticalScale,0.01));
                float margin = max(fwidth(p.x),fwidth(p.y))*2 + _Thickness*_Smoothness;
                float2 d = 10000;
                [loop] for (int n=0; n<48; n++)
                {
                    float x = n/48.0;
                    if (x >= _FillAmount) break;
                    float end = min((n+1)/48.0, _FillAmount);
                    float bound = max(_Thickness,_EndThickness) + margin;
                    if (p.x < x*_AspectRatio-bound || p.x > (n+1)/48.0*_AspectRatio+bound) continue;
                    d = join(d,segment(p,path(x),path(end),radius(x),radius(end)));
                }
                [loop] for (int cell=0; cell<96; cell++)
                {
                    if (cell >= _BranchCount) break;
                    float x = (cell + 0.2 + hash(cell,3)*0.6)/max(_BranchCount,1);
                    if (x >= _FillAmount) break;
                    if (hash(cell,4) > _BranchDensity * pow(1-x,_DensityFalloff)) continue;
                    float2 a = anchor(x);
                    float side = hash(cell,5) < 0.5 ? -1 : 1;
                    float angle = radians(clamp(_BranchAngle*(1+(hash(cell,6)*2-1)*_BranchAngleRandomness),5,80));
                    float len = _BranchLength * pow(saturate(1-x/max(_FillAmount,0.00001)),_LengthFalloff) * lerp(1,0.35+hash(cell,9)*1.3,_BranchLengthRandomness);
                    // Keep tips in the bar. Length units are bar heights, independent of aspect.
                    len = min(len, max(0,0.44/max(_VerticalScale,0.01)-side*a.y)/max(sin(angle),0.01));
                    len = min(len, (_FillAmount*_AspectRatio-a.x)*0.95/max(cos(angle),0.01));
                    float2 delta = float2(cos(angle),side*sin(angle))*len;
                    float width = radius(x)*_BranchWidth;
                    if (p.x < a.x-width-margin || p.x > a.x+delta.x+width+margin) continue;
                    float count = clamp(floor(_MaxBranchSegments),2,6);
                    [loop] for (int s=0; s<6; s++)
                    {
                        if (s >= count) break;
                        float t0=s/count, t1=(s+1)/count;
                        float2 b0=branchPoint(a,delta,t0,cell), b1=branchPoint(a,delta,t1,cell);
                        d=join(d,segment(p,b0,b1,width*pow(1-t0,_TipSharpness),width*pow(1-t1,_TipSharpness)));
                        if (s>0 && s<4 && hash(cell*6+s,10)<_SecondaryBranchDensity)
                        {
                            float2 tip=b0+float2(delta.x, -delta.y*0.8)*_SecondaryBranchLength*(1-t0);
                            d=join(d,segment(p,b0,tip,width*0.45*(1-t0),0));
                        }
                    }
                }
                [loop] for (int spike=0; spike<48; spike++)
                {
                    float x=(spike+hash(spike,11))/48;
                    if (x >= _FillAmount) break;
                    if (hash(spike,12)>_SpikeAmount*lerp(0.2,1,_StyleBlend)*pow(1-x,_DensityFalloff)) continue;
                    float2 a=anchor(x);
                    float side=hash(spike,13)<0.5 ? -1 : 1;
                    float len=(0.035+0.09*hash(spike,14))*pow(saturate(1-x/max(_FillAmount,0.00001)),_LengthFalloff);
                    len=min(len,(_FillAmount*_AspectRatio-a.x)*0.95/0.6);
                    if (p.x < a.x-_Thickness-margin || p.x > a.x+len*0.6+_Thickness+margin) continue;
                    d=join(d,segment(p,a,a+float2(len*0.6,side*len),radius(x)*0.65,0));
                }
                // Coordinate derivatives remain valid at spatial-culling boundaries.
                float aa=max(max(fwidth(p.x),fwidth(p.y))*0.65,0.00001);
                float coverage=1-smoothstep(-aa,aa,d.x);
                // A zero-radius SDF still covers half a pixel. Fade subpixel tips
                // by their area so the moving endpoint cannot leave a bright dot.
                coverage *= saturate(radius(i.uv.x)/aa);
                float edge=max(_FillSoftness, fwidth(i.uv.x));
                float fill=_FillAmount<=0 ? 0 : (_FillAmount>=1 ? 1 : 1-smoothstep(_FillAmount-edge,_FillAmount,i.uv.x));
                // Cross-section: saturated rim -> smooth shoulder -> hot emissive core.
                // The paired SDFs track local width on trunk AND every offshoot.
                float core = pow(saturate(-d.x/max(d.y-d.x,aa)),max(0.05,_CoreSharpness));
                float energy = pow(max(0.00001,saturate(1-i.uv.x/max(_FillAmount,0.00001))),_GlowFalloff);
                float3 emission = _GlowColor.rgb * max(0,_GlowIntensity) * saturate(_GlowColor.a) * core * energy;
                float3 body = _Color.rgb * _Intensity * lerp(_EdgeBrightness,1,core);
                float3 rgb = i.color.rgb * (body + emission);
                float4 color=float4(rgb*(1+_LeftIntensityBoost*pow(saturate(1-i.uv.x),3)), i.color.a*_Color.a*_Alpha*coverage*fill);
                #ifdef UNITY_UI_CLIP_RECT
                float2 m=saturate((_ClipRect.zw-_ClipRect.xy-abs(i.mask.xy))*i.mask.zw);
                color.a*=m.x*m.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a-0.001);
                #endif
                return color;
            }
            ENDHLSL
        }
    }
}
