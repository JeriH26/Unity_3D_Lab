Shader "Custom/CubeToSphere"
{
    Properties
    {
        _Color      ("Color",          Color)   = (0.2, 0.6, 1.0, 1.0)
        _Speed      ("Morph Speed",    Float)   = 1.0
        _Smoothness ("Smoothness",     Range(0,1)) = 0.5
        _Metallic   ("Metallic",       Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Opaque"
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // URP includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ────────────────────────────────────────────────────────────
            // Properties → CBUFFER (SRP Batcher compatible)
            // ────────────────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Speed;
                float  _Smoothness;
                float  _Metallic;
            CBUFFER_END

            // ────────────────────────────────────────────────────────────
            // Structs
            // ────────────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
            };

            // ────────────────────────────────────────────────────────────
            // Vertex shader – the morphing happens here
            // ────────────────────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Unity's default Cube has vertices in the range [-0.5, 0.5].
                // A sphere of radius 0.5 is obtained by normalising each vertex
                // and scaling to 0.5 – so both shapes have the same "size".

                float3 cubePos   = IN.positionOS.xyz;
                float3 spherePos = normalize(cubePos) * 0.5;

                // Smooth ping-pong between 0 (cube) and 1 (sphere)
                float t = (sin(_Time.y * _Speed) * 0.5 + 0.5);   // [0 … 1]
                // Use smoothstep for a more organic ease-in/ease-out feel
                float morph = smoothstep(0.0, 1.0, t);

                float3 morphedPos = lerp(cubePos, spherePos, morph);

                // Blend normals the same way so lighting stays correct
                float3 cubeNormal   = IN.normalOS;
                float3 sphereNormal = normalize(cubePos);          // sphere normal = normalised position
                float3 morphedNormal = normalize(lerp(cubeNormal, sphereNormal, morph));

                OUT.positionHCS = TransformObjectToHClip(morphedPos);
                OUT.normalWS    = TransformObjectToWorldNormal(morphedNormal);
                OUT.positionWS  = TransformObjectToWorld(morphedPos);

                return OUT;
            }

            // ────────────────────────────────────────────────────────────
            // Fragment shader – simple PBR lighting via URP helpers
            // ────────────────────────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                // Normalise after interpolation
                float3 normalWS = normalize(IN.normalWS);

                // Build InputData for URP lighting functions
                InputData inputData = (InputData)0;
                inputData.positionWS        = IN.positionWS;
                inputData.normalWS          = normalWS;
                inputData.viewDirectionWS   = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord       = float4(0, 0, 0, 0);
                inputData.fogCoord          = 0;
                inputData.bakedGI           = SampleSHPixel(normalWS, normalWS);
                inputData.normalizedScreenSpaceUV = float2(0, 0);
                inputData.shadowMask        = float4(1, 1, 1, 1);

                // Build SurfaceData
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo       = _Color.rgb;
                surfaceData.alpha        = 1.0;
                surfaceData.metallic     = _Metallic;
                surfaceData.smoothness   = _Smoothness;
                surfaceData.normalTS     = float3(0, 0, 1);
                surfaceData.occlusion    = 1.0;

                return UniversalFragmentPBR(inputData, surfaceData);
            }

            ENDHLSL
        }

        // Shadow caster pass so the morphing shape casts correct shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   shadowVert
            #pragma fragment shadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Speed;
                float  _Smoothness;
                float  _Metallic;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings shadowVert(Attributes IN)
            {
                Varyings OUT;

                float3 cubePos   = IN.positionOS.xyz;
                float3 spherePos = normalize(cubePos) * 0.5;
                float  morph     = smoothstep(0.0, 1.0, sin(_Time.y * _Speed) * 0.5 + 0.5);
                float3 morphedPos  = lerp(cubePos, spherePos, morph);

                float3 cubeNormal    = IN.normalOS;
                float3 sphereNormal  = normalize(cubePos);
                float3 morphedNormal = normalize(lerp(cubeNormal, sphereNormal, morph));

                // Apply shadow bias using the morphed normal
                float3 posWS  = TransformObjectToWorld(morphedPos);
                float3 normWS = TransformObjectToWorldNormal(morphedNormal);
                posWS = ApplyShadowBias(posWS, normWS, _MainLightPosition.xyz);

                OUT.positionHCS = TransformWorldToHClip(posWS);
                return OUT;
            }

            half4 shadowFrag(Varyings IN) : SV_Target { return 0; }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
