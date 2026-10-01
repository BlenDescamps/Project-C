Shader "KJD/ApparitionDissolve"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture Principale", 2D) = "white" {}
        [MainColor] _BaseColor("Couleur de Base", Color) = (1, 1, 1, 1)

        [Header(Apparition Dissolve Settings)]
        _DissolveAmount("Niveau de Dissolution (1 = Invisible, 0 = Visible)", Range(0.0, 1.0)) = 0.0
        _NoiseScale("Échelle du Bruit 3D", Float) = 5.0
        _NoiseTex("Texture de Bruit (Optionnelle)", 2D) = "white" {}

        [Header(Bord Lumineux Magique)]
        [HDR] _EdgeColor("Couleur du Bord Lumineux", Color) = (1.0, 0.4, 0.05, 1.0)
        _EdgeWidth("Largeur de l'Effet de Bord", Range(0.005, 0.25)) = 0.08
        _EmissionIntensity("Intensité de l'Émission (HDR)", Range(1.0, 10.0)) = 4.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "TransparentCutout" 
            "Queue" = "AlphaTest" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        LOD 300

        // ==========================================
        // PASS 1 : FORWARD RENDERING
        // ==========================================
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _EdgeColor;
                float _DissolveAmount;
                float _NoiseScale;
                float _EdgeWidth;
                float _EmissionIntensity;
            CBUFFER_END

            // Fonction de bruit 3D procédural fluide
            float hash3D(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise3D(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(lerp(hash3D(p + float3(0,0,0)), hash3D(p + float3(1,0,0)), f.x),
                         lerp(hash3D(p + float3(0,1,0)), hash3D(p + float3(1,1,0)), f.x), f.y),
                    lerp(lerp(hash3D(p + float3(0,0,1)), hash3D(p + float3(1,0,1)), f.x),
                         lerp(hash3D(p + float3(0,1,1)), hash3D(p + float3(1,1,1)), f.x), f.y), f.z);
            }

            float getNoise(float3 posWS, float2 uv)
            {
                // Bruit fractal multi-octaves (FBM)
                float n = noise3D(posWS * _NoiseScale) * 0.65;
                n += noise3D(posWS * _NoiseScale * 2.1) * 0.35;
                return saturate(n);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Calcul du bruit pour la découpe
                float noiseVal = getNoise(input.positionWS, input.uv);

                // Découpe clip() : quand _DissolveAmount passe de 1.0 (invisible) à 0.0 (visible)
                // L'objet "se matérialise" (apparition)
                float threshold = _DissolveAmount;
                clip((noiseVal - threshold) + 0.0001);

                // 2. Calcul du bord magique lumineux
                float edgeFactor = 1.0 - saturate((noiseVal - threshold) / max(_EdgeWidth, 0.001));
                edgeFactor = pow(edgeFactor, 1.5);
                half3 edgeGlow = _EdgeColor.rgb * edgeFactor * _EmissionIntensity;

                // 3. Éclairage URP Diffuse basique
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalize(input.normalWS), mainLight.direction));
                half3 lightColor = mainLight.color * NdotL + half3(0.2, 0.2, 0.25); // Ambiante douce

                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 finalColor = (texColor.rgb * lightColor) + edgeGlow;

                return half4(finalColor, texColor.a);
            }
            ENDHLSL
        }

        // ==========================================
        // PASS 2 : SHADOW CASTER (Pour que l'ombre apparaisse aussi)
        // ==========================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float2 uv           : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _EdgeColor;
                float _DissolveAmount;
                float _NoiseScale;
                float _EdgeWidth;
                float _EmissionIntensity;
            CBUFFER_END

            float hash3D(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise3D(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(lerp(hash3D(p + float3(0,0,0)), hash3D(p + float3(1,0,0)), f.x),
                         lerp(hash3D(p + float3(0,1,0)), hash3D(p + float3(1,1,0)), f.x), f.y),
                    lerp(lerp(hash3D(p + float3(0,0,1)), hash3D(p + float3(1,0,1)), f.x),
                         lerp(hash3D(p + float3(0,1,1)), hash3D(p + float3(1,1,1)), f.x), f.y), f.z);
            }

            Varyings vertShadow(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                float3 positionWS = ApplyShadowBias(posInputs.positionWS, normInputs.normalWS, _MainLightPosition.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = posInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap_ST);

                return output;
            }

            half4 fragShadow(Varyings input) : SV_Target
            {
                float n = noise3D(input.positionWS * _NoiseScale) * 0.65;
                n += noise3D(input.positionWS * _NoiseScale * 2.1) * 0.35;
                clip(n - _DissolveAmount);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
