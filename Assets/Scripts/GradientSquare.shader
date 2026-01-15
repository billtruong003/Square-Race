Shader "Custom/SolidSquare_Rounded_Outline"
{
    Properties
    {
        [Header(Base Settings)]
        [MainColor] _BaseColor("Base Color", Color) = (0, 0.8, 1, 1)
        _EmissionIntensity("Emission Intensity", Range(0, 20)) = 0
        [NoScaleOffset] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Shape Settings)]
        _CornerRadius("Corner Radius", Range(0, 0.5)) = 0.2
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Width", Range(0, 0.5)) = 0.1
        _Softness("Edge Softness", Range(0.001, 0.1)) = 0.005
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "IgnoreProjector" = "True" 
            "PreviewType" = "Plane" 
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

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
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _EmissionIntensity;
                float4 _OutlineColor;
                float _OutlineWidth;
                float _CornerRadius;
                float _Softness;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float RoundedRectSDF(float2 samplePosition, float2 halfSize, float radius)
            {
                float2 d = abs(samplePosition) - halfSize + radius;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - radius;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                float2 centerUV = input.uv - 0.5;
                float2 halfSize = 0.5;
                
                float dist = RoundedRectSDF(centerUV, halfSize, _CornerRadius);
                
                float shapeAlpha = 1.0 - smoothstep(0.0, _Softness, dist);
                
                float borderMask = smoothstep(-_OutlineWidth - _Softness, -_OutlineWidth, dist);
                
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float3 baseRGB = texColor.rgb * _BaseColor.rgb;
                float3 emission = baseRGB * _EmissionIntensity;
                float3 innerContentColor = baseRGB + emission;

                float3 finalRGB = lerp(innerContentColor, _OutlineColor.rgb, borderMask);
                
                float finalAlpha = texColor.a * _BaseColor.a * shapeAlpha;

                return float4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
}