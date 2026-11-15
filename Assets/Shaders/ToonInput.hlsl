#ifndef DECKSCENT_TOON_INPUT_INCLUDED
#define DECKSCENT_TOON_INPUT_INCLUDED

// Propriétés du shader Deckscent/Toon, partagées par toutes ses passes (même CBUFFER :
// compatible SRP Batcher). Les passes ombre / profondeur d'URP lisent _BaseMap, _BaseColor
// et _Cutoff.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _Cutoff;
    half _TextureStrength;
    half _TextureGain;
    half _TextureSaturation;
    half _ShadeThreshold;
    half _ShadeSoftness;
CBUFFER_END

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

#endif
