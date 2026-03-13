Shader "Lab/Hologram"
{
    Properties
    {
        _BaseColor      ("Base Color", Color) = (0, 1, 1, 1)
        _MainTex        ("Main Texture", 2D) = "white" {}
        _ScanlinesTex   ("Scanlines Texture", 2D) = "white" {}
        _FresnelPower   ("Fresnel Power", Range(0.5, 8)) = 2.0
        _Alpha          ("Base Alpha", Range(0, 1)) = 0.6
        _ScanlineSpeed  ("Scanline Scroll Speed", Range(0, 5)) = 1.0
        _ScanlineDensity("Scanline Density", Range(1, 100)) = 20.0
        _GlitchStrength ("Glitch Strength", Range(0, 0.05)) = 0.005
        _GlitchSpeed    ("Glitch Speed", Range(0, 20)) = 8.0
        _EmissionIntensity("Emission Intensity", Range(1, 10)) = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue"      = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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
                float3 viewDir  : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            sampler2D _ScanlinesTex;
            float4    _ScanlinesTex_ST;
            float4    _BaseColor;
            float     _FresnelPower;
            float     _Alpha;
            float     _ScanlineSpeed;
            float     _ScanlineDensity;
            float     _GlitchStrength;
            float     _GlitchSpeed;
            float     _EmissionIntensity;

            v2f vert(appdata v)
            {
                v2f o;

                // Glitch: random horizontal displacement on world Y bands
                float glitch = sin(_Time.y * _GlitchSpeed + v.vertex.y * 20.0);
                glitch = step(0.98, abs(glitch)) * _GlitchStrength * sign(glitch);
                v.vertex.x += glitch;

                o.pos      = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal   = UnityObjectToWorldNormal(v.normal);
                o.viewDir  = normalize(_WorldSpaceCameraPos - o.worldPos);
                o.uv       = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal  = normalize(i.normal);
                float3 viewDir = normalize(i.viewDir);

                // Fresnel
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);

                // Scrolling scanlines
                float scanY = i.worldPos.y * _ScanlineDensity + _Time.y * _ScanlineSpeed;
                float scanline = abs(sin(scanY)) * 0.5 + 0.5;

                // Texture
                float4 texColor = tex2D(_MainTex, i.uv);

                float3 color  = _BaseColor.rgb * texColor.rgb * _EmissionIntensity;
                color        *= scanline;
                float  alpha  = (_Alpha + fresnel) * scanline * texColor.a;

                return fixed4(color, saturate(alpha));
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}
