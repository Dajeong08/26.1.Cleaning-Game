Shader "Custom/URP_DirtShader"
{
    Properties
    {
        _MainTex ("Clean Texture", 2D) = "white" {}
        _DirtTex ("Dirt Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture (R)", 2D) = "black" {}
        _ScanColor ("Scan Color", Color) = (0, 1, 0, 1)
        _IsScanning ("Is Scanning", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_DirtTex); SAMPLER(sampler_DirtTex);
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);

            float4 _ScanColor;
            float _IsScanning;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 텍스처 샘플링
                half4 clean = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 dirt = SAMPLE_TEXTURE2D(_DirtTex, sampler_DirtTex, IN.uv);
                half mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, IN.uv).r;

                // 마스크 값(0~1)에 따라 깨끗한 텍스처와 더러운 텍스처 보간
                half4 finalColor = lerp(dirt, clean, mask);

                // 스캔 효과 (Q 키 눌렀을 때 더러운 부분 강조)
                if (_IsScanning > 0.5 && mask < 0.9)
                {
                    finalColor += _ScanColor * 0.5;
                }

                return finalColor;
            }
            ENDHLSL
        }
    }
}