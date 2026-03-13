Shader "Lab/NormalMap"
{
    Properties
    {
        _BaseColor      ("Base Color", Color) = (1, 1, 1, 1)
        _MainTex        ("Albedo Texture", 2D) = "white" {}
        _NormalMap      ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 3)) = 1.0
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
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
                float2 uv      : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                // TBN matrix rows stored in TEXCOORD2-4
                float3 T        : TEXCOORD2;
                float3 B        : TEXCOORD3;
                float3 N        : TEXCOORD4;
                LIGHTING_COORDS(5, 6)
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            sampler2D _NormalMap;
            float4    _NormalMap_ST;
            float4    _BaseColor;
            float     _NormalStrength;
            float4    _SpecularColor;
            float     _Shininess;
            float     _Ambient;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv       = TRANSFORM_TEX(v.uv, _MainTex);

                // Build TBN
                float3 N = UnityObjectToWorldNormal(v.normal);
                float3 T = UnityObjectToWorldDir(v.tangent.xyz);
                float3 B = cross(N, T) * v.tangent.w * unity_WorldTransformParams.w;
                o.T = T;
                o.B = B;
                o.N = N;

                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample and unpack normal map
                float2 normalUV = TRANSFORM_TEX(i.uv, _NormalMap);
                float3 tangentNormal = UnpackNormal(tex2D(_NormalMap, normalUV));
                tangentNormal.xy *= _NormalStrength;
                tangentNormal = normalize(tangentNormal);

                // Transform from tangent to world space
                float3x3 TBN    = float3x3(normalize(i.T), normalize(i.B), normalize(i.N));
                float3 normal   = normalize(mul(tangentNormal, TBN));

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDir  = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 halfDir  = normalize(lightDir + viewDir);

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
