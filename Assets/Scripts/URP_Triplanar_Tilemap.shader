Shader "Custom/URP_Triplanar_Tilemap"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Tiling ("Tiling", Float) = 1.0
        _Sharpness ("Blend Sharpness", Range(1, 64)) = 10.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Geometry" 
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Tiling;
                float _Sharpness;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 weights = abs(input.normalWS);
                weights = pow(weights, _Sharpness);
                weights = weights / (weights.x + weights.y + weights.z);

                float2 uvX = input.positionWS.zy * _Tiling;
                float2 uvY = input.positionWS.xz * _Tiling;
                float2 uvZ = input.positionWS.xy * _Tiling;

                half4 colX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvX);
                half4 colY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvY);
                half4 colZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvZ);

                half4 finalCol = colX * weights.x + colY * weights.y + colZ * weights.z;
                
                return finalCol * _Color;
            }
            ENDHLSL
        }
    }
}