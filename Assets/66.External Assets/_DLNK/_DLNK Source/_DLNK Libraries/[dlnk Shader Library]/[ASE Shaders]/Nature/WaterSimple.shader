// Modified for URP - GrabPass removed
// Original: Made with Amplify Shader Editor
Shader "DLNK Shaders/URP/Nature/WaterSimple"
{
    Properties
    {
        _UVScale("UVScale", Float) = 1
        _ColorA("Color A", Color) = (0.2971698,0.6247243,1,0)
        _ColorB("Color B", Color) = (0.09838911,0.1034623,0.3113208,0)
        _NormalA("Normal A", 2D) = "bump" {}
        _NormalB("Normal B", 2D) = "bump" {}
        _NormalScale("NormalScale", Float) = 1
        _SpecXYSnsZW("Spec(XY)Sns(ZW)", Vector) = (0.1,0,0.5,0.2)
        _VelocityXYFoamZ("Velocity(XY)Foam(Z)", Vector) = (0.03,-0.05,0.04,0)
        _Depth("Depth", Float) = 0.9
        _Falloff("Falloff", Float) = -3
        _Distorsion("Distorsion", Float) = 0.1
        _ColorFoam("ColorFoam", Color) = (0.9386792,0.9671129,1,0)
        _FoamMask("FoamMask", 2D) = "white" {}
        _FoamTiling("FoamTiling", Float) = 1
        _FoamDepth("FoamDepth", Float) = 0.9
        _FoamFalloff("FoamFalloff", Float) = -3
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 100
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float fogFactor : TEXCOORD5;
            };

            TEXTURE2D(_NormalA);
            SAMPLER(sampler_NormalA);
            TEXTURE2D(_NormalB);
            SAMPLER(sampler_NormalB);
            TEXTURE2D(_FoamMask);
            SAMPLER(sampler_FoamMask);

            CBUFFER_START(UnityPerMaterial)
                float _UVScale;
                float4 _ColorA;
                float4 _ColorB;
                float _NormalScale;
                float4 _SpecXYSnsZW;
                float3 _VelocityXYFoamZ;
                float _Depth;
                float _Falloff;
                float _Distorsion;
                float4 _ColorFoam;
                float _FoamTiling;
                float _FoamDepth;
                float _FoamFalloff;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // UV Setup
                float2 worldUV = float2(input.positionWS.x, input.positionWS.z) * _UVScale;
                
                // Animated normals
                float2 uv1 = worldUV + _VelocityXYFoamZ.x * _Time.y;
                float2 uv2 = worldUV + _VelocityXYFoamZ.y * _Time.y;
                
                float3 normal1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalA, sampler_NormalA, uv1), _NormalScale);
                float3 normal2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalB, sampler_NormalB, uv2), _NormalScale);
                float3 blendedNormal = normalize(normal1 + normal2);
                
                // Transform normal to world space
                float3x3 TBN = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 worldNormal = normalize(mul(blendedNormal, TBN));

                // Depth calculation
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float surfaceDepth = input.screenPos.w;
                float depthDiff = abs(sceneDepth - surfaceDepth);
                
                // Water depth fade
                float depthFade = saturate(pow(depthDiff + _Depth, _Falloff));
                
                // Foam
                float2 foamUV = worldUV * _FoamTiling + _VelocityXYFoamZ.z * _Time.y;
                float foamMask = SAMPLE_TEXTURE2D(_FoamMask, sampler_FoamMask, foamUV).r;
                float foamDepth = saturate(pow(depthDiff + _FoamDepth, _FoamFalloff)) * foamMask;
                
                // Color blending
                float4 waterColor = lerp(_ColorA, _ColorB, depthFade);
                float4 finalColor = lerp(waterColor, _ColorFoam, foamDepth);
                
                // Sample opaque texture with distortion (URP replacement for GrabPass)
                float2 distortedUV = screenUV + worldNormal.xy * _Distorsion * 0.01;
                float3 opaqueColor = SampleSceneColor(distortedUV);
                
                // Blend with scene
                finalColor.rgb = lerp(finalColor.rgb, opaqueColor, depthFade * 0.5);
                
                // Lighting
                Light mainLight = GetMainLight();
                float3 lighting = mainLight.color * saturate(dot(worldNormal, mainLight.direction));
                finalColor.rgb *= 0.5 + lighting * 0.5;
                
                // Transparency
                finalColor.a = lerp(0.85, 0.3, depthFade);
                
                // Fog
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
