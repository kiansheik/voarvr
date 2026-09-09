Shader "VoarVR/WindRibbon"
{
    Properties
    {
        _BaseColor ("Color", Color) = (.2, .9, 1, .65)
        _FlowDirection ("Flow direction", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _FlowDirection;
            CBUFFER_END
            // Set from the flight simulation clock; pause/recenter never leaves pulses drifting.
            float _FlightWindTime;
            Varyings Vert(Attributes input)
            {
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=input.uv; output.color=input.color; return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                // Soft luminous strokes rather than hard dash edges. Fade across width and
                // both ends; vertex alpha handles complete draft-tail recycling, not vertices.
                half wave=.5h+.5h*sin(input.uv.x*12.56637h-_FlightWindTime*_FlowDirection*2.2h);
                half pulse=.24h+.76h*wave*wave;
                half edge=saturate(1.0h-abs(input.uv.y*2.0h-1.0h));
                half ends=smoothstep(0.0h,.08h,input.uv.x)*(1.0h-smoothstep(.88h,1.0h,input.uv.x));
                return half4(_BaseColor.rgb*input.color.rgb,_BaseColor.a*input.color.a*pulse*edge*ends);
            }
            ENDHLSL
        }
    }
}
