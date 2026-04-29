Shader "Custom/ShoreWater"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.0, 0.35, 0.6, 0.85)
        _ShallowColor ("Shallow Color", Color) = (0.2, 0.75, 0.8, 0.85)
        _FoamColor ("Foam Color", Color) = (1,1,1,1)

        _ShallowWidth ("Shallow Width", Float) = 12.0
        _FoamWidth ("Foam Width", Float) = 4.0

        _FoamThreshold ("Foam Threshold", Range(0,1)) = 0.45
        _FoamSoftness ("Foam Softness", Range(0.001,1)) = 0.12
        _FoamIntensity ("Foam Intensity", Range(0,2)) = 1.0

        _NoiseTex ("Noise", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 4.0
        _NoiseSpeedA ("Noise Speed A", Vector) = (0.04, 0.02, 0, 0)
        _NoiseSpeedB ("Noise Speed B", Vector) = (-0.03, 0.04, 0, 0)

        _SurfaceNoiseStrength ("Surface Noise Strength", Range(0,1)) = 0.15
        _FoamNoiseStrength ("Foam Noise Strength", Range(0,1)) = 0.25

        _WaveAmp ("Wave Amplitude", Range(0,1)) = 0.03
        _WaveFreq ("Wave Frequency", Float) = 0.15
        _WaveSpeed ("Wave Speed", Float) = 1.0

        _ShallowTintStrength ("Shallow Tint Strength", Range(0,2)) = 1.0
        _SpecularBoost ("Specular Boost (fake)", Range(0,2)) = 0.4
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;

            fixed4 _DeepColor;
            fixed4 _ShallowColor;
            fixed4 _FoamColor;

            float _ShallowWidth;
            float _FoamWidth;
            float _FoamThreshold;
            float _FoamSoftness;
            float _FoamIntensity;

            float _NoiseScale;
            float4 _NoiseSpeedA;
            float4 _NoiseSpeedB;

            float _SurfaceNoiseStrength;
            float _FoamNoiseStrength;

            float _WaveAmp;
            float _WaveFreq;
            float _WaveSpeed;

            float _ShallowTintStrength;
            float _SpecularBoost;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos       : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float2 uv        : TEXCOORD1;
                float3 worldPos  : TEXCOORD2;
                float3 viewDir   : TEXCOORD3;
            };

            v2f vert (appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float t = _Time.y * _WaveSpeed;
                float wave =
                    sin((worldPos.x * _WaveFreq) + t) * 0.5 +
                    cos((worldPos.z * (_WaveFreq * 1.17)) + t * 1.23) * 0.5;

                worldPos.y += wave * _WaveAmp;

                float4 clipPos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.pos = clipPos;
                o.screenPos = ComputeScreenPos(clipPos);
                o.uv = TRANSFORM_TEX(v.uv, _NoiseTex);
                o.worldPos = worldPos;
                o.viewDir = normalize(_WorldSpaceCameraPos.xyz - worldPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                float2 worldUV = i.worldPos.xz;
                float2 tiledUV = worldUV * _NoiseTex_ST.xy + _NoiseTex_ST.zw;

                float2 nUVA = tiledUV * (_NoiseScale * 0.05) + _Time.y * _NoiseSpeedA.xy;
                float2 nUVB = tiledUV * (_NoiseScale * 0.08) + _Time.y * _NoiseSpeedB.xy;

                float noiseA = tex2D(_NoiseTex, nUVA).r;
                float noiseB = tex2D(_NoiseTex, nUVB).r;
                float combinedNoise = (noiseA + noiseB) * 0.5;
                float centeredNoise = combinedNoise - 0.5;

                float2 depthDistort = centeredNoise * 0.01 * _SurfaceNoiseStrength;
                float2 distortedScreenUV = screenUV + depthDistort;

                float rawSceneDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, distortedScreenUV);
                float sceneEyeDepth = LinearEyeDepth(rawSceneDepth);

                float waterEyeDepth = LinearEyeDepth(i.screenPos.z / i.screenPos.w);

                float depthDiff = max(0, sceneEyeDepth - waterEyeDepth);

                float shallowMask = 1.0 - saturate(depthDiff / max(_ShallowWidth, 0.0001));

                float foamDepthMask = 1.0 - saturate(depthDiff / max(_FoamWidth, 0.0001));

                float camDist = distance(_WorldSpaceCameraPos.xyz, i.worldPos);
                float distBoost = saturate((camDist - 40.0) / 140.0);

                float foamThreshold = lerp(_FoamThreshold, _FoamThreshold - 0.10, distBoost);
                float foamSoftness  = lerp(_FoamSoftness,  _FoamSoftness + 0.05, distBoost);

                foamDepthMask = saturate(foamDepthMask * lerp(1.0, 1.25, distBoost));

                fixed4 waterCol = lerp(_DeepColor, _ShallowColor, shallowMask * _ShallowTintStrength);

                float surfaceVariation = centeredNoise * _SurfaceNoiseStrength;
                waterCol.rgb += surfaceVariation * 0.15;

                float fresnel = pow(1.0 - saturate(dot(normalize(i.viewDir), float3(0,1,0))), 3.0);
                float sparkle = saturate(combinedNoise * 1.2 - 0.7) * (_SpecularBoost + fresnel * 0.5);
                waterCol.rgb += sparkle * 0.10;

                float foamNoise = centeredNoise * _FoamNoiseStrength;
                float foamValue = foamDepthMask + foamNoise;

                float foamLine = smoothstep(
                    foamThreshold - foamSoftness,
                    foamThreshold + foamSoftness,
                    foamValue
                );

                float edgeBand = smoothstep(0.0, 0.25, foamDepthMask) * (1.0 - smoothstep(0.25, 0.65, foamDepthMask));
                foamLine = saturate(foamLine + edgeBand * 0.35);

                float foamBlend = saturate(foamLine * _FoamIntensity);

                fixed3 finalRgb = lerp(waterCol.rgb, _FoamColor.rgb, foamBlend);

                float alpha = waterCol.a;
                alpha = lerp(alpha, max(alpha, 0.95), foamBlend * 0.5);

                return fixed4(finalRgb, alpha);
            }
            ENDCG
        }
    }
}