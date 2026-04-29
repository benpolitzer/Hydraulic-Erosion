Shader "Custom/ErosionShader"
{
    Properties
    {
        _GrassTexture ("Grass Texture", 2D) = "white" {}
        _RockTexture ("Rock Texture", 2D) = "white" {}
        _GrassColour ("Grass Colour", Color) = (0, 1, 0, 1)
        _RockColour ("Rock Colour", Color) = (1, 1, 1, 1)
        _GrassSlopeThreshold ("Grass Slope Threshold", Range(0, 1)) = 0.5
        _GrassBlendAmount ("Grass Blend Amount", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float2 uv_GrassTexture; // Add UV coordinates for Grass Texture
            float3 worldNormal;     // World normal for slope calculation
        };

        sampler2D _GrassTexture;
        sampler2D _RockTexture;
        fixed4 _GrassColour;
        fixed4 _RockColour;
        half _GrassSlopeThreshold;
        half _GrassBlendAmount;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Calculate slope based on the world normal's y-component
            float slope = 1 - IN.worldNormal.y;

            // Blend between grass and rock textures based on slope
            float grassBlendHeight = _GrassSlopeThreshold * (1 - _GrassBlendAmount);
            float grassWeight = 1 - saturate((slope - grassBlendHeight) / (_GrassSlopeThreshold - grassBlendHeight));

            // Sample textures using correct UV coordinates
            fixed4 grassTexture = tex2D(_GrassTexture, IN.uv_GrassTexture);
            fixed4 rockTexture = tex2D(_RockTexture, IN.uv_GrassTexture);

            // Final color based on blend
            fixed4 grass = grassTexture * _GrassColour;
            fixed4 rock = rockTexture * _RockColour;
            o.Albedo = lerp(rock.rgb, grass.rgb, grassWeight);
            o.Smoothness = 0.5; // Adjust as needed
        }
        ENDCG
    }

    FallBack "Diffuse"
}
