// Éclairage en aplats façon bande dessinée : la lumière est découpée net (éclairé / ombre)
// au lieu d'un dégradé. Toutes les lumières de la scène (principale et additionnelles, en
// Forward+) et leurs ombres portées. La décroissance des lumières reste continue : la
// découper en paliers crée des anneaux cernés d'encre dans le post-process gravure.
//
// La texture (atlas Synty) est passée en gris et éclaircie : le décor reste en niveaux de
// gris (une couleur saturée deviendrait un « accent » du post-process) et pas trop sombre
// (sinon tout se couvre de hachures).
Shader "Deckscent/Toon"
{
    Properties
    {
        [MainColor] _BaseColor("Couleur", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        _TextureStrength("Force de la texture (0 = ignorée)", Range(0, 1)) = 1
        _TextureGain("Gain de la texture (plafonné à 1)", Range(0, 4)) = 1
        _TextureSaturation("Saturation gardée (0 = gris)", Range(0, 1)) = 0
        _ShadeThreshold("Frontière éclairé / ombre (-1 à 1)", Range(-1, 1)) = 0
        _ShadeSoftness("Douceur de la frontière", Range(0, 0.5)) = 0.03
        _Glow("Lueur propre, ajoutée à la lumière reçue (0 = aucune ; ennemis et trésors, pour qu'on les voie dans le noir)", Range(0, 1)) = 0
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Découpe alpha (feuillages : la texture porte la forme des feuilles)", Float) = 0
        _Cutoff("Seuil alpha", Range(0, 1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Faces dessinées (Off = les deux : feuillages en cartes)", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonVertex
            #pragma fragment ToonFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON

            #include "ToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ToonVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            // Frontière nette entre la face éclairée et l'ombre propre.
            half Band(half ndl)
            {
                return smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, ndl);
            }

            half3 ShadeLight(Light light, half3 normalWS)
            {
                // L'atténuation contient l'ombre portée (bords durs) et la décroissance (continue).
                return light.color * Band(dot(normalWS, light.direction))
                    * light.distanceAttenuation * light.shadowAttenuation;
            }

            half4 ToonFragment(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texel = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
                #if defined(_ALPHATEST_ON)
                    clip(texel.a * _BaseColor.a - _Cutoff);
                #endif
                half3 tex = texel.rgb;
                half gray = dot(tex, half3(0.299, 0.587, 0.114));
                tex = min(lerp(gray.xxx, tex, _TextureSaturation) * _TextureGain, 1.0);
                half3 albedo = _BaseColor.rgb * lerp(half3(1, 1, 1), tex, _TextureStrength);

                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                // Faces doubles (feuillages) : le dos d'une carte est éclairé comme sa face.
                normalWS = isFrontFace ? normalWS : -normalWS;
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                half4 shadowMask = half4(1, 1, 1, 1);

                Light mainLight = GetMainLight(inputData.shadowCoord, input.positionWS, shadowMask);
                half3 lighting = ShadeLight(mainLight, normalWS);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint lightCount = GetAdditionalLightsCount();
                    #if USE_CLUSTER_LIGHT_LOOP
                        // En Forward+, les lumières directionnelles en plus sont hors des clusters.
                        [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                        {
                            Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                            lighting += ShadeLight(light, normalWS);
                        }
                    #endif
                    LIGHT_LOOP_BEGIN(lightCount)
                        Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                        lighting += ShadeLight(light, normalWS);
                    LIGHT_LOOP_END
                #endif

                // Lumière ambiante (Environment > Ambient, couleur unie).
                half3 ambient = SampleSH(normalWS);
                // Lueur propre : les accents (slimes, coffres) restent lisibles dans le noir.
                half3 color = albedo * (lighting + ambient + _Glow);
                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma shader_feature_local _ALPHATEST_ON
            #include "ToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull [_Cull]
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON
            #include "ToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // Normales pour les contours du post-process gravure (texture de normales d'URP).
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS
            #pragma shader_feature_local _ALPHATEST_ON
            #include "ToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
