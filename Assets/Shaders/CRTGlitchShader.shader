Shader "Custom/CRTGlitchShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlitchTime ("Glitch Time", Float) = 0.0
        _GlitchIntensity ("Glitch Intensity", Float) = 0.05
        _BaseColor ("Base Color", Color) = (0.21, 0.16, 0.16, 0.92)
        _ScanlineSpeed ("Scanline Speed", Float) = 4.0
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _GlitchTime;
            float _GlitchIntensity;
            float4 _BaseColor;
            float _ScanlineSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float timeVal = _GlitchTime;
                
                // Glitch line jump
                float glitchLine = step(0.97, rand(float2(floor(uv.y * 25.0), frac(timeVal * 0.7))));
                float shift = glitchLine * _GlitchIntensity * sin(timeVal * 40.0);
                uv.x += shift;

                fixed4 col = tex2D(_MainTex, uv) * _BaseColor;
                
                // Add color noise/flicker
                float flicker = 0.94 + 0.06 * rand(float2(timeVal, timeVal * 1.3));
                col.rgb *= flicker;

                // Add CRT scanlines
                float scan = 0.90 + 0.10 * sin(uv.y * 300.0 + timeVal * _ScanlineSpeed);
                col.rgb *= scan;

                // Subtle red warning pulse
                float pulse = 0.5 + 0.5 * sin(timeVal * 2.0);
                col.rgb += float3(0.05, 0.015, 0.0) * pulse;

                // Set alpha
                col.a = _BaseColor.a;

                return col;
            }
            ENDCG
        }
    }
}
