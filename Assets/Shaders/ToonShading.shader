Shader "Lab/ToonShading"
{
    Properties
    {
        _BaseColor      ("Base Color", Color) = (1, 1, 1, 1)
        _MainTex        ("Albedo Texture", 2D) = "white" {}
        _ShadowColor    ("Shadow Color", Color) = (0.2, 0.2, 0.3, 1)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.5
        _ShadowSmoothness("Shadow Smoothness", Range(0, 0.2)) = 0.02
        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 1)
        _HighlightThreshold("Highlight Threshold", Range(0, 1)) = 0.95
        _OutlineColor   ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth   ("Outline Width", Range(0, 0.05)) = 0.005
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        // ---- Outline pass (back-face expansion) ----
        Pass
        {
            Name "Outline"
            Cull Front

            CGPROGRAM
            #pragma vertex   vertOutline
            #pragma fragment fragOutline
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float4 _OutlineColor;
            float  _OutlineWidth;

            v2f vertOutline(appdata v)
            {
                v2f o;
                float3 norm  = normalize(v.normal);
                float4 pos   = v.vertex + float4(norm * _OutlineWidth, 0);
                o.pos = UnityObjectToClipPos(pos);
                return o;
            }

            fixed4 fragOutline(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // ---- Toon lighting pass ----
        Pass
        {
            Name "ToonLit"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 normal   : TEXCOORD0;
                float2 uv       : TEXCOORD1;
                LIGHTING_COORDS(2, 3)
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _BaseColor;
            float4    _ShadowColor;
            float     _ShadowThreshold;
            float     _ShadowSmoothness;
            float4    _HighlightColor;
            float     _HighlightThreshold;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos    = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal   = normalize(i.normal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float  atten    = LIGHT_ATTENUATION(i);

                float NdotL = dot(normal, lightDir) * 0.5 + 0.5; // remap to [0,1]
                NdotL *= atten;

                // Stepped lighting with smoothstep
                float3 shadow    = _ShadowColor.rgb;
                float3 highlight = _HighlightColor.rgb;
                float3 lit       = _LightColor0.rgb;

                float shadowMask    = smoothstep(_ShadowThreshold - _ShadowSmoothness,
                                                 _ShadowThreshold + _ShadowSmoothness, NdotL);
                float highlightMask = smoothstep(_HighlightThreshold - _ShadowSmoothness,
                                                 _HighlightThreshold + _ShadowSmoothness, NdotL);

                float3 toon = lerp(shadow, lit, shadowMask);
                toon        = lerp(toon, highlight, highlightMask);

                float4 texColor = tex2D(_MainTex, i.uv) * _BaseColor;
                return fixed4(toon * texColor.rgb, texColor.a);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
