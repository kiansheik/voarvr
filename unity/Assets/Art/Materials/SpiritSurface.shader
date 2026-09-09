Shader "VoarVR/SpiritSurface"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.2, 0.4, 0.35, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half shade:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                float3 normal = TransformObjectToWorldNormal(input.normalOS);
                output.shade = 0.48h + 0.52h * saturate(dot(normal, normalize(float3(-0.6, 1.0, -0.4))));
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half haze = saturate((distance(input.positionWS, _WorldSpaceCameraPos) - 35.0) / 200.0) * 0.8h;
                half3 color = _BaseColor.rgb * input.shade;
                return half4(lerp(color, half3(0.32h, 0.52h, 0.59h), haze), 1.0h);
            }
            ENDHLSL
        }
    }
}
