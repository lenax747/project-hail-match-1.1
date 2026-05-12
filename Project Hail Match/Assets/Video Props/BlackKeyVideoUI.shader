
Shader "UI/BlackKeyVideoUI_CircleMask_Clean"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _Threshold ("Black Remove Threshold", Range(0,1)) = 0.12
        _BlackSoftness ("Black Smoothness", Range(0,1)) = 0.08
        _Alpha ("Alpha", Range(0,1)) = 1

        _CircleSize ("Circle Size", Range(0,1.5)) = 0.7
        _OuterSoftness ("Outer Softness", Range(0,0.5)) = 0.12

        _Aspect ("Rect Aspect Width / Height", Float) = 0.5625
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Threshold;
            float _BlackSoftness;
            float _Alpha;

            float _CircleSize;
            float _OuterSoftness;
            float _Aspect;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                float brightness = max(col.r, max(col.g, col.b));
                float blackAlpha = smoothstep(_Threshold, _Threshold + _BlackSoftness, brightness);

                float2 centeredUV = i.uv - 0.5;
                centeredUV.x *= _Aspect;

                float dist = length(centeredUV);

                float circleMask = 1.0 - smoothstep(
                    _CircleSize - _OuterSoftness,
                    _CircleSize,
                    dist
                );

                col.a *= blackAlpha * circleMask * _Alpha;

                return col;
            }
            ENDCG
        }
    }
}