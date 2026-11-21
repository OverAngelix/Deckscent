// Post-process plein écran façon gravure / BD à l'encre, en noir et blanc (inspiration :
// les jeux de donjon dessinés à la plume, dont The Secret of Weepstone) :
// - contours à l'encre : sauts de profondeur, de normale et de luminosité (ce dernier cerne
//   les ombres portées et les joints des pierres), tracé légèrement tremblé ;
// - pas de gris : fond papier, le ton est rendu par l'ÉPAISSEUR des traits de deux séries de
//   hachures diagonales croisées, jusqu'au noir plein ;
// - hachures ancrées sur les surfaces (en coordonnées monde, elles ne glissent pas quand on
//   bouge), écartement constant à l'écran ; au loin, un trait sur deux s'efface ;
// - seules les couleurs très saturées (accents) sortent du noir et blanc.
// Utilisé par un Full Screen Pass Renderer Feature (PC_Renderer), qui fournit l'image
// éclairée (_BlitTexture), la profondeur et les normales.
Shader "Deckscent/Gravure"
{
    Properties
    {
        [Header(Encre)]
        _InkColor("Couleur de l'encre", Color) = (0.03, 0.03, 0.035, 1)
        _LineThickness("Épaisseur des contours (px)", Range(0.5, 4)) = 1.6
        _DepthSensitivity("Seuil de profondeur (relatif)", Range(0, 1)) = 0.05
        _NormalSensitivity("Seuil d'angle entre surfaces", Range(0, 1)) = 0.3
        _LumSensitivity("Seuil de saut de luminosité (0 = aucun)", Range(0, 1)) = 0.12
        _LineWobble("Tremblement des contours (px)", Range(0, 3)) = 0.8

        [Header(Tons)]
        _WhitePoint("Au-dessus : papier blanc", Range(0, 1)) = 0.55
        _BlackPoint("En dessous : noir plein", Range(0, 1)) = 0.12
        _SolidInk("Ton en dessous duquel l'encre est pleine (aplat)", Range(0, 0.5)) = 0.08

        [Header(Obscurite)]
        _DarkStart("Distance où l'obscurité commence (m)", Range(0, 60)) = 8
        _DarkEnd("Distance du noir complet, sans hachures (m)", Range(0, 80)) = 16
        _LitKeep("Luminosité à partir de laquelle une zone éclairée échappe à l'obscurité", Range(0, 1)) = 0.6

        [Header(Hachures)]
        _HatchSpacingPx("Écartement visé entre deux traits (px)", Range(3, 30)) = 5
        _HatchMinSpacing("Écartement le plus fin sur les surfaces (m)", Range(0.005, 0.2)) = 0.025
        _ThresholdFirst("Ton où apparaît la 1re série", Range(0, 1)) = 0.8
        _ThresholdCross("Ton où apparaît la série croisée", Range(0, 1)) = 0.5
        _HatchAngle("Angle de la 1re série (degrés)", Range(0, 180)) = 45
        _HatchMinWidthPx("Épaisseur d'un trait qui apparaît (px)", Range(0, 3)) = 0.9
        _HatchWobble("Tremblement des traits (m)", Range(0, 0.05)) = 0.006

        [Header(Couleur)]
        _PaperColor("Couleur du papier", Color) = (0.97, 0.965, 0.95, 1)
        _AccentSaturation("Saturation d'un accent (1 = tout en N&B)", Range(0, 1)) = 0.45
        _AccentBoost("Intensité des accents", Range(0, 2)) = 1.1
        _Grain("Grain du papier", Range(0, 0.2)) = 0.025
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Gravure"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment GravureFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _InkColor;
            float _LineThickness, _DepthSensitivity, _NormalSensitivity, _LumSensitivity, _LineWobble;
            float _WhitePoint, _BlackPoint, _SolidInk;
            float _DarkStart, _DarkEnd, _LitKeep;
            float _HatchSpacingPx, _HatchMinSpacing, _ThresholdFirst, _ThresholdCross, _HatchAngle, _HatchMinWidthPx, _HatchWobble;
            float4 _PaperColor;
            float _AccentSaturation, _AccentBoost, _Grain;
            // Global (hors Properties, pour que Shader.SetGlobalFloat s'applique) : 0 = image non
            // touchée. Mis à 0 pour la vue Scène de l'éditeur par GravureSceneView.cs.
            float _GravureOff;
            // Ciel gravé (globaux, hors Properties, posés par CampSky.cs ; 0 = fond noir du donjon) :
            // _SkyOn allume le ciel, _MoonDir = direction de la lune (monde), _SkyParams =
            // (ton à l'horizon, ton au zénith, densité des étoiles, rayon de la lune en radians).
            float _SkyOn;
            float4 _MoonDir;
            float4 _SkyParams;
            // Dehors (camp) : les distances de l'obscurité (_DarkStart, _DarkEnd) sont multipliées
            // par ce facteur global (0 = 1 : le donjon garde les siennes).
            float _DarkScale;

            float3 SceneColor(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0).rgb;
            }

            float EyeDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            float PerceivedLum(float3 c)
            {
                return pow(max(dot(c, float3(0.299, 0.587, 0.114)), 0.0), 1.0 / 2.2);
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Bruit lisse (valeur), pour faire trembler les contours sans scintiller.
            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash(i), b = Hash(i + float2(1, 0)), c = Hash(i + float2(0, 1)), d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Une série de traits espacés de `spacing` mètres. `fill` = part de l'intervalle
            // couverte par le trait (0 rien, 1 noir plein), avec un minimum en pixels. Les traits
            // sont sur les multiples entiers : la série 2x plus espacée est un sous-ensemble de
            // celle-ci, on passe de l'une à l'autre sans saut.
            float HatchLayer(float coord, float spacing, float fill)
            {
                float u = coord / spacing;
                // Variation de u par pixel. fwidth la surestime un peu pour des traits en
                // diagonale : des traits plus larges et plus serrés, choix de l'utilisateur.
                float pxPerPeriod = 1.0 / max(fwidth(u), 1e-5);
                float d = abs(frac(u + 0.5) - 0.5) * pxPerPeriod; // distance au trait, en pixels
                float width = fill > 0.0 ? max(fill * pxPerPeriod, _HatchMinWidthPx) : 0.0;
                // Bord anticrénelé d'un pixel ; un trait plus fin qu'un pixel est seulement atténué.
                return saturate(width * 0.5 + 0.5 - d) * min(width, 1.0);
            }

            // Traits sur une surface (p en mètres). n = direction perpendiculaire aux traits.
            float HatchLines(float2 p, float2 n, float fill)
            {
                float along = dot(p, float2(-n.y, n.x));
                // Deux fréquences : le trait ondule sans motif régulier visible.
                float coord = dot(p, n) + (sin(along * 3.1) + 0.5 * sin(along * 7.3 + 1.7)) * _HatchWobble;
                // Combien de fois doubler l'écartement pour atteindre _HatchSpacingPx à l'écran ?
                // Jamais plus serré que _HatchMinSpacing sur la surface : de près, les traits
                // s'élargissent à l'écran (gros traits devant la caméra, choix de l'utilisateur).
                float basePx = _HatchMinSpacing / max(fwidth(coord), 1e-6);
                float lod = max(log2(_HatchSpacingPx / basePx), 0.0);
                float spacing = _HatchMinSpacing * exp2(floor(lod));
                float fine = HatchLayer(coord, spacing, fill);
                float coarse = HatchLayer(coord, spacing * 2.0, fill);
                return lerp(fine, coarse, frac(lod));
            }

            // Épaisseur relative des traits d'une série qui commence au ton `start`.
            float LayerFill(float tone, float start)
            {
                return saturate((start - tone) / max(start, 1e-4));
            }

            float Hash3(float3 p)
            {
                return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453);
            }

            // Angle (radians) entre deux directions unitaires.
            float AngleBetween(float3 a, float3 b)
            {
                return acos(clamp(dot(a, b), -1.0, 1.0));
            }

            // Ciel de nuit, là où rien n'est dessiné : un aplat d'encre, noir au zénith, gris sombre
            // vers l'horizon (les silhouettes du décor s'y découpent en noir) ; étoiles en points de
            // papier, fixes dans le ciel ; croissant de lune en papier, cerné d'encre, dans un halo.
            float3 SkyColor(float2 uv)
            {
                // Rayon de vue : du plan proche au plan lointain sous ce pixel.
                float3 nearP = ComputeWorldSpacePosition(uv, UNITY_NEAR_CLIP_VALUE, UNITY_MATRIX_I_VP);
                float3 farP = ComputeWorldSpacePosition(uv, UNITY_RAW_FAR_CLIP_VALUE, UNITY_MATRIX_I_VP);
                float3 dir = normalize(farP - nearP);
                float el = asin(clamp(dir.y, -1.0, 1.0));
                // Pixels par radian à l'écran (champ vertical de la caméra ; la projection peut être
                // retournée quand on rend dans une texture, d'où abs).
                float pxPerRad = _ScreenParams.y * 0.5 * abs(UNITY_MATRIX_P[1][1]);

                float3 moon = normalize(_MoonDir.xyz);
                float moonR = _SkyParams.w;
                float toMoon = AngleBetween(dir, moon);

                // Ton du ciel (aplat, sans traits, choix de l'utilisateur) : _SkyParams.x à
                // l'horizon, .y au zénith (0 = encre, 1 = papier), plus clair autour de la lune.
                float t = pow(saturate(el / 1.2), 0.6);
                float tone = lerp(_SkyParams.x, _SkyParams.y, t);
                tone += 0.12 * (1.0 - smoothstep(moonR * 1.2, moonR * 8.0, toMoon));
                float ink = 1.0 - saturate(tone);

                // Étoiles : une sur quelques cellules d'une grille en direction, au centre de sa cellule.
                float3 cell = floor(dir * _SkyParams.z);
                float h = Hash3(cell);
                float3 starDir = normalize((cell + 0.5) / _SkyParams.z);
                float starPx = AngleBetween(dir, starDir) * pxPerRad;
                float size = 0.6 + 1.2 * frac(h * 37.0);
                float star = (h < 0.022 ? 1.0 : 0.0) * saturate(size - starPx + 0.5) * smoothstep(0.03, 0.15, el);
                star *= smoothstep(moonR * 2.0, moonR * 4.0, toMoon); // pas d'étoile dans le halo

                // Lune : un disque moins un disque décalé (la part dans l'ombre) = un croissant.
                float3 right = normalize(cross(float3(0, 1, 0), moon) + 1e-4);
                float3 up = cross(moon, right);
                float3 shadowCenter = normalize(moon + (right * 0.62 + up * 0.3) * moonR);
                float inDisc = saturate((moonR - toMoon) * pxPerRad + 0.5);
                float lit = inDisc * saturate((AngleBetween(dir, shadowCenter) - moonR * 0.95) * pxPerRad + 0.5);
                // La part sombre de la lune se fond dans le ciel, elle cache
                // seulement les étoiles ; le croissant est en papier, cerné d'un trait d'encre.
                star *= 1.0 - inDisc;
                float litSide = saturate((AngleBetween(dir, shadowCenter) - moonR * 0.95) * pxPerRad + 0.5);
                float rim = saturate(1.2 - abs(toMoon - moonR) * pxPerRad) * litSide;
                ink = lerp(ink, 0.0, max(lit, star));
                ink = max(ink, rim);
                // Sous l'horizon (au loin, sous le décor) : le noir.
                ink = lerp(ink, 1.0, 1.0 - smoothstep(-0.02, 0.0, el));

                float3 paper = _PaperColor.rgb + (Hash(floor(uv * _ScreenParams.xy)) - 0.5) * _Grain;
                return lerp(paper, _InkColor.rgb, ink);
            }

            half4 GravureFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 texel = 1.0 / _ScreenParams.xy;
                float3 col = SceneColor(uv);
                if (_GravureOff > 0.5)
                    return half4(col, 1);

                float rawDepth = SampleSceneDepth(uv);
                float d0 = LinearEyeDepth(rawDepth, _ZBufferParams);
                // Pas d'encre ni de hachures sur le fond (rien de dessiné).
                #if UNITY_REVERSED_Z
                    float geoMask = rawDepth > 0.0 ? 1.0 : 0.0;
                #else
                    float geoMask = rawDepth < 1.0 ? 1.0 : 0.0;
                #endif
                if (geoMask < 0.5 && _SkyOn > 0.5)
                    return half4(SkyColor(uv), 1);

                // --- Contours ---
                // Le point de mesure tremble un peu, d'après la position à l'écran : un trait de
                // plume plutôt qu'un trait de règle.
                float2 wobble = (float2(Noise(uv * _ScreenParams.xy * 0.05), Noise(uv * _ScreenParams.xy * 0.05 + 17.0)) - 0.5) * 2.0 * _LineWobble;
                float2 suv0 = uv + wobble * texel;
                float2 px = _LineThickness * texel;
                float3 n0 = SampleSceneNormals(suv0);
                float lum = PerceivedLum(SceneColor(suv0));
                float dRef = EyeDepth(suv0);
                float depthEdge = 0.0, normalEdge = 0.0, lumEdge = 0.0;
                float2 offsets[4] = { float2(px.x, 0), float2(-px.x, 0), float2(0, px.y), float2(0, -px.y) };
                [unroll] for (int i = 0; i < 4; i++)
                {
                    float2 suv = suv0 + offsets[i];
                    float d = EyeDepth(suv);
                    depthEdge = max(depthEdge, abs(d - dRef) / max(dRef, 0.001));
                    normalEdge = max(normalEdge, 1.0 - dot(n0, SampleSceneNormals(suv)));
                    // Seul le côté sombre du saut est encré, pour un trait net.
                    lumEdge = max(lumEdge, PerceivedLum(SceneColor(suv)) - lum);
                }
                float edge = max(step(_DepthSensitivity, depthEdge), step(_NormalSensitivity, normalEdge));
                if (_LumSensitivity > 0.0)
                    edge = max(edge, step(_LumSensitivity, lumEdge));

                // --- Hachures (sur la luminosité perçue, après éclairage et ombres) ---
                float3 worldPos = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 an = abs(SampleSceneNormals(uv));
                float2 surf = an.y > max(an.x, an.z) ? worldPos.xz : (an.x > an.z ? worldPos.zy : worldPos.xy);

                // Accents : une couleur vive a une luminance faible (le rouge pur ~0,3). Son ton
                // vient plutôt de son intensité, sinon même sa face éclairée serait hachurée.
                float g = dot(col, float3(0.299, 0.587, 0.114));
                float cMax = max(col.r, max(col.g, col.b));
                float cMin = min(col.r, min(col.g, col.b));
                float sat = (cMax - cMin) / max(cMax, 1e-4);
                float keep = smoothstep(_AccentSaturation, _AccentSaturation + 0.1, sat);
                float toneLum = lerp(PerceivedLum(col), pow(max(cMax, 0.0), 1.0 / 2.2), keep);

                float tone = smoothstep(_BlackPoint, _WhitePoint, toneLum);
                // Obscurité avec la distance : la lumière ambiante est la même partout, un couloir
                // lointain resterait sinon à mi-ton (quadrillé). Le ton descend vers le noir entre
                // _DarkStart et _DarkEnd.
                // Là où rien n'est dessiné (au-delà de tout), le noir complet.
                float darkScale = _DarkScale > 0.0 ? _DarkScale : 1.0;
                float darkness = geoMask > 0.0 ? smoothstep(_DarkStart * darkScale, max(_DarkEnd, _DarkStart + 0.01) * darkScale, d0) : 1.0;
                // Ce qu'une vraie lumière éclaire (torche au loin) échappe à l'obscurité : un point
                // lumineux dans le noir. Seul ce qui n'a que la lumière ambiante s'y perd.
                darkness *= 1.0 - smoothstep(_LitKeep, _LitKeep + 0.25, toneLum) * geoMask;
                tone *= 1.0 - darkness;
                float a = radians(_HatchAngle);
                float2 n1 = float2(cos(a), sin(a));
                float2 n2 = float2(-n1.y, n1.x);
                float h1 = HatchLines(surf, n1, LayerFill(tone, _ThresholdFirst));
                float h2 = HatchLines(surf, n2, LayerFill(tone, _ThresholdCross));
                // Deux séries croisées : chacune couvre une part de ce que l'autre laisse.
                float hatch = (1.0 - (1.0 - h1) * (1.0 - h2)) * geoMask;
                // Tout en bas, un aplat d'encre : même des traits qui se rejoignent laissent un peu
                // de papier entre eux, d'où un quadrillage gris au lieu du noir.
                float solid = 1.0 - smoothstep(0.0, max(_SolidInk, 1e-3), tone);
                hatch = max(hatch, solid);

                // --- Couleur : papier, accents, grain ---
                float3 accent = saturate(col * _AccentBoost / max(cMax, 0.25));
                float3 outColor = lerp(_PaperColor.rgb, accent, keep);
                outColor += (Hash(floor(uv * _ScreenParams.xy)) - 0.5) * _Grain;
                outColor = lerp(outColor, _InkColor.rgb, hatch);
                outColor = lerp(outColor, _InkColor.rgb, edge * geoMask);
                return half4(outColor, 1);
            }
            ENDHLSL
        }
    }
}
