// WaveShader.shader
Shader "Unlit/WaveShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Wavelength ("Wavelength", Range(0.1, 10)) = 1
        _Amplitude ("Amplitude", Range(0, 5)) = 0.5
        _Speed ("Speed", Range(0, 10)) = 1
        _LineThickness ("Line Thickness", Range(0.001, 0.5)) = 0.02 // Our new thickness control!
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Shader properties controlled by C#
            fixed4 _Color;
            float _Wavelength;
            float _Amplitude;
            float _Speed;
            float _LineThickness;

            #define PI 3.1415926535

            v2f vert (appdata v)
            {
                v2f o;
                // The vertex shader now only passes data through, no more displacement.
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // The fragment shader does all the work now.
            fixed4 frag (v2f i) : SV_Target
            {
                // Center the UV coordinates so (0,0) is the middle of the sprite.
                float2 centeredUV = i.uv - 0.5;

                // Calculate the target Y position of the sine wave based on the pixel's X position.
                // We multiply by a large number (like 10) to get multiple waves across the sprite.
                float waveY = _Amplitude * sin(
                    (centeredUV.x * 10.0 / _Wavelength) * 2.0 * PI + _Time.y * _Speed
                );
                
                // Calculate the distance of the current pixel from the sine wave's path.
                float distance = abs(centeredUV.y - waveY);

                // Use smoothstep to draw an anti-aliased line.
                // It creates a soft fade instead of a hard, jagged edge.
                float lineAlpha = 1.0 - smoothstep(
                    _LineThickness - 0.01, // Inner edge of the fade
                    _LineThickness + 0.01, // Outer edge of the fade
                    distance
                );
                
                // The final pixel color is the main color, with an alpha determined by our calculation.
                return fixed4(_Color.rgb, _Color.a * lineAlpha);
            }
            ENDCG
        }
    }
}