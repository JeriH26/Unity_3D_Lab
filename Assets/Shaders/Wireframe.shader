Shader "Lab/Wireframe"
{
    Properties
    {
        _FillColor      ("Fill Color", Color) = (0.1, 0.1, 0.1, 1)
        _WireColor      ("Wire Color", Color) = (0, 1, 0.5, 1)
        _WireThickness  ("Wire Thickness", Range(0, 10)) = 1.5
        _WireGlow       ("Wire Glow Intensity", Range(1, 10)) = 2.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target   4.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2g
            {
                float4 pos : SV_POSITION;
            };

            struct g2f
            {
                float4 pos      : SV_POSITION;
                // Barycentric coordinates
                float3 bary     : TEXCOORD0;
            };

            float4 _FillColor;
            float4 _WireColor;
            float  _WireThickness;
            float  _WireGlow;

            v2g vert(appdata v)
            {
                v2g o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> stream)
            {
                g2f o;

                o.pos  = input[0].pos; o.bary = float3(1,0,0); stream.Append(o);
                o.pos  = input[1].pos; o.bary = float3(0,1,0); stream.Append(o);
                o.pos  = input[2].pos; o.bary = float3(0,0,1); stream.Append(o);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                // Distance from nearest edge
                float3 bary   = i.bary;
                float  minB   = min(bary.x, min(bary.y, bary.z));
                float  delta  = fwidth(minB);
                float  edge   = smoothstep(0.0, delta * _WireThickness, minB);

                float3 color  = lerp(_WireColor.rgb * _WireGlow, _FillColor.rgb, edge);
                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Color"
}
