Shader "Custom/DirtShader" {
    Properties {
        _MainTex ("Clean Texture", 2D) = "white" {}      // 깨끗한 본체 이미지
        _DirtTex ("Dirt Texture", 2D) = "white" {}       // 오염된 이미지
        _MaskTex ("Mask Texture (R)", 2D) = "white" {}   // 지워진 부위를 기록할 마스크
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        struct Input { float2 uv_MainTex; };
        sampler2D _MainTex, _DirtTex, _MaskTex;

        void surf (Input IN, inout SurfaceOutputStandard o) {
            fixed4 clean = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 dirt = tex2D(_DirtTex, IN.uv_MainTex);
            float mask = tex2D(_MaskTex, IN.uv_MainTex).r; // 빨간색 채널을 마스크로 사용

            // 마스크 값이 1에 가까울수록 깨끗한 면이 나옵니다.
            o.Albedo = lerp(dirt.rgb, clean.rgb, mask);
        }
        ENDCG
    }
    FallBack "Diffuse"
}