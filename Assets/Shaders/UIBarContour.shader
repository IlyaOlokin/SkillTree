Shader "UI/Bar Contour"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _GlowColor ("Bloom Color (HDR)", Color) = (1,1,1,1)
        _GlowIntensity ("Bloom Intensity", Range(0,20)) = 2
        [Enum(Alpha,10,Additive,1)] _DstBlend ("Contour Blending", Float) = 10
        _CornerRadius ("Corner Radius (Fraction of Short Side)", Range(0,0.5)) = 0.2
        _FadeStartX ("Left / Right - Fade Start (Alpha 1)", Range(0,0.499)) = 0.01
        _FadeEndX ("Left / Right - Fade End (Alpha 0)", Range(0,0.499)) = 0.06
        _FadeStartY ("Top / Bottom - Fade Start (Alpha 1)", Range(0,0.499)) = 0.08
        _FadeEndY ("Top / Bottom - Fade End (Alpha 0)", Range(0,0.499)) = 0.4
        _FadePower ("Alpha Fade Curve Power", Range(0.1,8)) = 1
        _TopAlpha ("Top Half Alpha", Range(0,1)) = 1
        _BottomAlpha ("Bottom Half Alpha", Range(0,1)) = 1
        _HalfSplit ("Half Split Height (Bottom to Top)", Range(0.01,0.99)) = 0.5
        _HalfFadeWidth ("Half Fade Distance", Range(0.001,0.5)) = 0.2
        _FillEdgeSoftness ("Fill Edge Softness (Fraction of Short Side)", Range(0,1)) = 0.2
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
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
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
        // Additive RGB is order-independent between contours; alpha uses coverage union.
        Blend SrcAlpha [_DstBlend], One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd;
            half4 _GlowColor;
            float _GlowIntensity, _CornerRadius;
            float _FadeStartX, _FadeEndX, _FadeStartY, _FadeEndY, _FadePower;
            float _TopAlpha, _BottomAlpha, _HalfSplit, _HalfFadeWidth;
            float _FillEdgeSoftness;
            float4 _ClipRect;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 contour : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 contour : TEXCOORD1;
                float4 mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = v.uv;
                // Without the companion component, full-texture Simple images still work.
                o.contour = v.contour.w > 0.5
                    ? float4(v.contour.xy, max(v.contour.z, 0.00001), v.contour.w)
                    : float4(v.uv, 1.0, 1.0);
                float2 pixelSize = o.vertex.w;
                pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                o.mask = float4(v.vertex.xy * 2 - rect.xy - rect.zw,
                    0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                return o;
            }

            float RoundedRectDistance(float2 p, float2 halfSize, float radius)
            {
                radius = clamp(radius, 0.0, min(halfSize.x, halfSize.y));
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 color = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;
                // Work in units of the shorter side to keep corners circular on long bars.
                float2 size = float2(i.contour.z, 1.0) / min(i.contour.z, 1.0);
                float2 halfSize = size * 0.5;
                float2 p = (i.contour.xy - 0.5) * size;
                float radius = saturate(_CornerRadius * 2.0) * 0.5;
                // Independent positions measured inward from the edge, not a start + length.
                // An end at/before the start becomes a near-hard cut at the start.
                float2 opaqueAt = clamp(float2(_FadeStartX, _FadeStartY), 0.0, 0.49899);
                float2 clearAt = clamp(float2(_FadeEndX, _FadeEndY),
                    opaqueAt + 0.00001, 0.499);
                float2 opaqueInset = opaqueAt * size;
                float2 clearInset = clearAt * size;
                float outerDistance = RoundedRectDistance(p, halfSize, radius);
                float opaqueDistance = RoundedRectDistance(p, halfSize - opaqueInset,
                    max(0.0, radius - min(opaqueInset.x, opaqueInset.y)));
                float clearDistance = RoundedRectDistance(p, halfSize - clearInset,
                    max(0.0, radius - min(clearInset.x, clearInset.y)));
                float fade = saturate(-opaqueDistance / max(clearDistance - opaqueDistance, 0.000001));
                float antialias = max(fwidth(outerDistance), 0.00001);
                float outerMask = 1.0 - smoothstep(-antialias, 0.0, outerDistance);
                float contourAlpha = pow(saturate(1.0 - smoothstep(0.0, 1.0, fade)),
                    max(_FadePower, 0.1));
                // Fade on the MORE visible side of the split, so a disabled half stays
                // entirely transparent, including the left/right edges. Equal alpha
                // settings leave the original contour unchanged, without a center seam.
                float split = clamp(_HalfSplit, 0.01, 0.99);
                float width = max(_HalfFadeWidth, 0.001);
                float topWeight = smoothstep(split, min(1.0, split + width), i.contour.y);
                float bottomWeight = 1.0 - smoothstep(max(0.0, split - width), split, i.contour.y);
                float topAlpha = saturate(_TopAlpha);
                float bottomAlpha = saturate(_BottomAlpha);
                float halfAlpha = min(topAlpha, bottomAlpha)
                    + max(topAlpha - bottomAlpha, 0.0) * topWeight
                    + max(bottomAlpha - topAlpha, 0.0) * bottomWeight;
                // Image.Filled cuts the mesh. Fade inward from its actual moving edge,
                // affecting every contour band, rather than just its side border.
                float fillEdgeAlpha = 1.0;
                if (i.contour.w > 1.5 && _FillEdgeSoftness > 0.0)
                {
                    float edgeDistance;
                    if (i.contour.w < 2.5)
                        edgeDistance = (1.0 - i.contour.x) * size.x;
                    else if (i.contour.w < 3.5)
                        edgeDistance = i.contour.x * size.x;
                    else if (i.contour.w < 4.5)
                        edgeDistance = (1.0 - i.contour.y) * size.y;
                    else
                        edgeDistance = i.contour.y * size.y;
                    fillEdgeAlpha = smoothstep(0.0, max(_FillEdgeSoftness, 0.00001), edgeDistance);
                }
                color.a *= contourAlpha * outerMask * halfAlpha * fillEdgeAlpha;
                // HDR RGB survives to the camera's bloom pass; alpha still masks the hollow center.
                color.rgb *= 1.0 + _GlowColor.rgb * max(0.0, _GlowIntensity);
                #ifdef UNITY_UI_CLIP_RECT
                float2 mask = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                color.a *= mask.x * mask.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
