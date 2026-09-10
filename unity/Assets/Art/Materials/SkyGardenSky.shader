Shader "VoarVR/SkyGardenSky"
{
 Properties { _Exposure("Exposure",Float)=1 _SkyTint("Weather tint",Color)=(1,1,1,1) }
 SubShader
 {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
  Cull Off ZWrite Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
   struct A {float4 positionOS:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
   float _Exposure;half4 _SkyTint;
   V Vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.direction=a.positionOS.xyz;return o;}
   half4 Frag(V i):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float3 direction=normalize(i.direction);float y=direction.y;
    half3 lower=half3(.42,.65,.76),horizon=half3(.70,.83,.80),zenith=half3(.21,.39,.56);
    half3 color=y<0?lerp(lower,horizon,smoothstep(-.22,0,y)):lerp(horizon,zenith,smoothstep(0,.85,y));
    float sun=saturate(dot(direction,normalize(float3(-.4,.5,.3))));color+=half3(.24,.13,.03)*pow(sun,128);
    // Match the shared terrain haze below the horizon even inside rain fronts.
    half3 weather=lerp(half3(1,1,1),_SkyTint.rgb*_Exposure,smoothstep(-.22,.12,y));
    return half4(SRGBToLinear(saturate(color))*weather,1);
   }
   ENDHLSL
  }
 }
}
