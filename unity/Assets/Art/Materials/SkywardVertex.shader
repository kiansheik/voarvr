Shader "VoarVR/SkywardVertex"
{
    Properties { _HazeStart ("Haze start",Float)=350 _HazeEnd ("Haze end",Float)=1700 }
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
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float3 world : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            float _HazeStart, _HazeEnd;
            float4 _FoliageImpactPoint, _FoliageImpactDirection, _FoliageImpact;
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 world=TransformObjectToWorld(input.positionOS.xyz);
                // This uniform branch is inactive for every ordinary frame and active
                // on one struck chunk for less than a second, keeping Quest vertex cost bounded.
                if(_FoliageImpact.x>0.0001)
                {
                    float distanceToImpact=distance(world,_FoliageImpactPoint.xyz);
                    half leaf=saturate((input.color.g-input.color.r-0.025)*8.0)*saturate((input.color.g-input.color.b-0.015)*8.0);
                    float life=saturate(1.0-_FoliageImpact.z/max(0.01,_FoliageImpact.w));
                    float radial=saturate(1.0-distanceToImpact/max(0.1,_FoliageImpact.y));
                    float ripple=sin(_FoliageImpact.z*30.0-distanceToImpact*3.5)*life*radial*leaf*_FoliageImpact.x;
                    float3 impactDirection=normalize(_FoliageImpactDirection.xyz+float3(0.0,0.18,0.0));
                    world+=impactDirection*ripple;
                }
                output.positionCS = TransformWorldToHClip(world);
                output.world=world;
                float3 n=TransformObjectToWorldNormal(input.normalOS);
                // Cheap opaque palette shading; no textures, transparency or extra light passes.
                half sun=saturate(dot(n,normalize(float3(-.4,1,.3))));
                half sky=saturate(n.y*.5h+.5h);
                half3 shade=lerp(half3(.32,.43,.48),half3(1.08,1.01,.82),sun);
                shade+=half3(.08,.12,.10)*sky;
                output.color=half4(SRGBToLinear(input.color.rgb)*shade,1);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); float distanceToEye=distance(input.world,_WorldSpaceCameraPos); float haze=smoothstep(_HazeStart,_HazeEnd,distanceToEye); return half4(lerp(input.color.rgb,SRGBToLinear(half3(.42,.65,.76)),haze),1); }
            ENDHLSL
        }
    }
}
