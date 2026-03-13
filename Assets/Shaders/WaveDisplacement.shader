Shader "Lab/WaveDisplacement"
{
    Properties
    {
        _BaseColor      ("Base Color", Color) = (0.2, 0.6, 1.0, 1.0)
        _MainTex        ("Main Texture", 2D) = "white" {}
        _WaveAmplitude  ("Wave Amplitude", Range(0, 2)) = 0.3
        _WaveFrequency  ("Wave Frequency", Range(0, 20)) = 5.0
        _WaveSpeed      ("Wave Speed", Range(0, 10)) = 2.0
        _WaveDirection  ("Wave Direction (XZ)", Vector) = (1, 0, 0, 0)
        _SpecularColor  ("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess      ("Shininess", Range(1, 256)) = 64
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
            float     _WaveAmplitude;
            float     _WaveFrequency;
            float     _WaveSpeed;
            float4    _WaveDirection;
            float4    _SpecularColor;
            float     _Shininess;

            v2f vert(appdata v)
            {
                v2f o;

                // World-space position for wave calculation
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float2 dir = normalize(_WaveDirection.xz);
                float wave = sin(dot(worldPos.xz, dir) * _WaveFrequency + _Time.y * _WaveSpeed);
                worldPos.y += wave * _WaveAmplitude;

                // Approximate displaced normal via finite differences
                float eps = 0.01;
                float waveX = sin(dot(worldPos.xz + float2(eps, 0), dir) * _WaveFrequency + _Time.y * _WaveSpeed);
                float waveZ = sin(dot(worldPos.xz + float2(0, eps), dir) * _WaveFrequency + _Time.y * _WaveSpeed);
                float3 tangentX = normalize(float3(eps, (waveX - wave) * _WaveAmplitude, 0));
                float3 tangentZ = normalize(float3(0, (waveZ - wave) * _WaveAmplitude, eps));
                float3 dispNormal = cross(tangentZ, tangentX);

                o.pos      = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.worldPos = worldPos;
                o.normal   = normalize(mul((float3x3)unity_ObjectToWorld, dispNormal));
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

                float NdotL   = max(0.0, dot(normal, lightDir));
                float NdotH   = max(0.0, dot(normal, halfDir));
                float atten   = LIGHT_ATTENUATION(i);

                float3 ambient  = UNITY_LIGHTMODEL_AMBIENT.rgb * 0.2;
                float3 diffuse  = _LightColor0.rgb * NdotL * atten;
                float3 specular = _LightColor0.rgb * _SpecularColor.rgb * pow(NdotH, _Shininess) * atten;

                float4 texColor = tex2D(_MainTex, i.uv) * _BaseColor;
                return fixed4((ambient + diffuse) * texColor.rgb + specular, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
