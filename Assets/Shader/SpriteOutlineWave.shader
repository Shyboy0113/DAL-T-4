Shader "Custom/SpriteOutlineWave"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Outline)]
        _OutlineColor     ("Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Outline Thickness", Range(0, 10)) = 1.0

        [Header(Wave)]
        _WaveSpeed     ("Wave Speed",       Range(0.1, 10)) = 2.0
        _WaveAmplitude ("Wave Amplitude X", Range(0.0, 0.1)) = 0.02
        _WaveFrequency ("Wave Frequency X", Range(0.5, 20)) = 8.0
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
            float4 _MainTex_TexelSize; // x = 1/width, y = 1/height
            fixed4 _Color;

            fixed4 _OutlineColor;
            float  _OutlineThickness;

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
                // ── 1) Wave: UV 왜곡 ──
                float2 uv = i.uv;
                uv.x += sin(uv.y * _WaveFrequency  + _Time.y * _WaveSpeed)       * _WaveAmplitude;
                uv.y += sin(uv.x * _WaveFrequencyY + _Time.y * _WaveSpeed * 0.8) * _WaveAmplitudeY;

                // ── 2) 원본 텍스처 샘플링 ──
                fixed4 col = tex2D(_MainTex, uv) * i.color;

                // ── 3) Outline: 상하좌우 이웃 픽셀의 알파 확인 ──
                float2 offset = _MainTex_TexelSize.xy * _OutlineThickness;

                float alphaU = tex2D(_MainTex, uv + float2(0,  offset.y)).a;
                float alphaD = tex2D(_MainTex, uv + float2(0, -offset.y)).a;
                float alphaL = tex2D(_MainTex, uv + float2(-offset.x, 0)).a;
                float alphaR = tex2D(_MainTex, uv + float2( offset.x, 0)).a;

                // 현재 픽셀은 투명하지만 이웃 중 하나라도 불투명 → 외곽선
                float neighborAlpha = max(max(alphaU, alphaD), max(alphaL, alphaR));
                float isOutline = step(0.01, neighborAlpha) * step(col.a, 0.01);

                // 외곽선 색상 합성
                col = lerp(col, fixed4(_OutlineColor.rgb * _OutlineColor.a, _OutlineColor.a), isOutline);

                // premultiplied alpha
                col.rgb *= col.a;
                return col;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
