Shader "Custom/SpriteWave"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // 물결 파라미터
        _WaveSpeed   ("Wave Speed",   Range(0.1, 10)) = 2.0
        _WaveAmplitude("Wave Amplitude", Range(0.0, 0.1)) = 0.02
        _WaveFrequency("Wave Frequency", Range(0.5, 20)) = 8.0

        // 수직 방향 물결 (선택)
        _WaveAmplitudeY("Wave Amplitude Y", Range(0.0, 0.1)) = 0.0
        _WaveFrequencyY("Wave Frequency Y", Range(0.5, 20)) = 6.0

        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            float _WaveSpeed;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveAmplitudeY;
            float _WaveFrequencyY;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                o.color  = v.color * _Color;

                #ifdef PIXELSNAP_ON
                o.vertex = UnityPixelSnap(o.vertex);
                #endif

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // X축 일렁임: UV.y 기준으로 sin 파동
                uv.x += sin(uv.y * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveAmplitude;

                // Y축 일렁임 (Amplitude > 0일 때만 실질적 효과)
                uv.y += sin(uv.x * _WaveFrequencyY + _Time.y * _WaveSpeed * 0.8) * _WaveAmplitudeY;

                fixed4 col = tex2D(_MainTex, uv) * i.color;
                col.rgb *= col.a; // premultiplied alpha
                return col;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
