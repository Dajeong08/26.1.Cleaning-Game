Shader "Custom/DirtShader" {
    Properties {
        _MainTex ("Clean Texture", 2D) = "white" {}
        _DirtTex ("Dirt Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture (R)", 2D) = "white" {}
        _ScanColor ("Scan Color", Color) = (0, 1, 0, 1) // 형광색 초록
        [HideInInspector] _IsScanning ("Is Scanning", Float) = 0 
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        struct Input { float2 uv_MainTex; };
        sampler2D _MainTex, _DirtTex, _MaskTex;
        fixed4 _ScanColor;
        float _IsScanning;

        void surf (Input IN, inout SurfaceOutputStandard o) {
            fixed4 clean = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 dirt = tex2D(_DirtTex, IN.uv_MainTex);
            float mask = tex2D(_MaskTex, IN.uv_MainTex).r;

            fixed3 baseColor = lerp(dirt.rgb, clean.rgb, mask);
            
            // 만약 스캔 모드라면 더러운 부분(1-mask)을 형광색으로!
            if (_IsScanning > 0.5) {
                float intensity = (1.0 - mask);
                o.Albedo = baseColor + (_ScanColor.rgb * intensity);
                o.Emission = _ScanColor.rgb * intensity * 1.5; // 빛나는 효과
            } else {
                o.Albedo = baseColor;
                o.Emission = fixed3(0,0,0);
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}