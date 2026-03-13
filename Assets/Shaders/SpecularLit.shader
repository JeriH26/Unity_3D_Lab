Shader "Lab/SpecularLit"
{
    Properties
    {
        _BaseColor      ("Base Color", Color) = (1, 1, 1, 1)
        _MainTex        ("Albedo Texture", 2D) = "white" {}
        _SpecularColor  ("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess      ("Shininess", Range(1, 256)) = 64
        _Ambient        ("Ambient Strength", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "LightMode"  = "ForwardBase"
        }

        Pass
        {
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
                float3 worldPos : TEXCOORD1;
                float2 uv       : TEXCOORD2;
                LIGHTING_COORDS(3, 4)
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _BaseColor;
            float4    _SpecularColor;
            float     _Shininess;
            float     _Ambient;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal   = UnityObjectToWorldNormal(v.normal);
                o.uv       = TRANSFORM_TEX(v.uv, _MainTex);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal   = normalize(i.normal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDir  = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 halfDir  = normalize(lightDir + viewDir);

                // Blinn-Phong
                float NdotL   = max(0.0, dot(normal, lightDir));
                float NdotH   = max(0.0, dot(normal, halfDir));
                float atten   = LIGHT_ATTENUATION(i);

                float3 ambient  = UNITY_LIGHTMODEL_AMBIENT.rgb * _Ambient;
                float3 diffuse  = _LightColor0.rgb * NdotL * atten;
                float3 specular = _LightColor0.rgb * _SpecularColor.rgb * pow(NdotH, _Shininess) * atten;

                float4 texColor = tex2D(_MainTex, i.uv) * _BaseColor;
                float3 result   = (ambient + diffuse) * texColor.rgb + specular;

                return fixed4(result, texColor.a);
            }
            ENDCG
        }
    }
    FallBack "Specular"
}
