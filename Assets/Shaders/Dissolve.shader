Shader "Lab/Dissolve"
{
    Properties
    {
        _BaseColor      ("Base Color", Color) = (1, 1, 1, 1)
        _MainTex        ("Albedo Texture", 2D) = "white" {}
        _NoiseTex       ("Dissolve Noise Texture", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0.0
        _EdgeWidth      ("Edge Width", Range(0, 0.2)) = 0.05
        _EdgeColor      ("Edge Color", Color) = (1, 0.3, 0, 1)
        _EdgeIntensity  ("Edge Glow Intensity", Range(1, 10)) = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue"      = "AlphaTest"
            "LightMode"  = "ForwardBase"
        }

        Cull Off

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
                float2 uv       : TEXCOORD1;
                float2 noiseUV  : TEXCOORD2;
                LIGHTING_COORDS(3, 4)
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            sampler2D _NoiseTex;
            float4    _NoiseTex_ST;
            float4    _BaseColor;
            float     _DissolveAmount;
            float     _EdgeWidth;
            float4    _EdgeColor;
            float     _EdgeIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos     = UnityObjectToClipPos(v.vertex);
                o.normal  = UnityObjectToWorldNormal(v.normal);
                o.uv      = TRANSFORM_TEX(v.uv, _MainTex);
                o.noiseUV = TRANSFORM_TEX(v.uv, _NoiseTex);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float noise = tex2D(_NoiseTex, i.noiseUV).r;

                // Discard pixels below threshold
                clip(noise - _DissolveAmount);

                // Edge glow
                float edge = step(noise - _DissolveAmount, _EdgeWidth);
                float3 edgeColor = _EdgeColor.rgb * _EdgeIntensity * edge;

                // Lighting
                float3 normal   = normalize(i.normal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL     = max(0.0, dot(normal, lightDir));
                float atten     = LIGHT_ATTENUATION(i);
                float3 diffuse  = _LightColor0.rgb * NdotL * atten;
                float3 ambient  = UNITY_LIGHTMODEL_AMBIENT.rgb * 0.2;

                float4 texColor = tex2D(_MainTex, i.uv) * _BaseColor;
                float3 result   = (ambient + diffuse) * texColor.rgb + edgeColor;

                return fixed4(result, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
