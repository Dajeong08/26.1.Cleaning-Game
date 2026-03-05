Shader "Custom/SubmarineEraser"

{

    Properties

    {

        _DirtyTex ("더러운 텍스처", 2D) = "white" {}

        _MaskTex  ("닦임 마스크", 2D) = "black" {}

    }



    SubShader

    {

        Tags

        {

            "RenderType"  = "Transparent"

            "Queue"       = "Transparent+1"

            "RenderPipeline" = "UniversalPipeline"

        }



        Blend SrcAlpha OneMinusSrcAlpha

        ZWrite Off

        Cull Back



        Pass

        {

            Name "SubmarineEraserPass"

            Tags { "LightMode" = "UniversalForward" }   // ← 이게 없으면 URP가 패스를 무시함



            HLSLPROGRAM

            #pragma vertex vert

            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"



            struct Attributes

            {

                float4 positionOS : POSITION;

                float2 uv         : TEXCOORD0;

            };



            struct Varyings

            {

                float4 positionHCS : SV_POSITION;

                float2 uv          : TEXCOORD0;

            };



            TEXTURE2D(_DirtyTex); SAMPLER(sampler_DirtyTex);

            TEXTURE2D(_MaskTex);  SAMPLER(sampler_MaskTex);



            CBUFFER_START(UnityPerMaterial)

                float4 _DirtyTex_ST;

            CBUFFER_END



            Varyings vert(Attributes IN)

            {

                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv = TRANSFORM_TEX(IN.uv, _DirtyTex);

                return OUT;

            }



            half4 frag(Varyings IN) : SV_Target

            {

                half4 dirtyColor = SAMPLE_TEXTURE2D(_DirtyTex, sampler_DirtyTex, IN.uv);

                float mask       = SAMPLE_TEXTURE2D(_MaskTex,  sampler_MaskTex,  IN.uv).r;



                // mask=0(안 닦임) → alpha 유지 / mask=1(닦임) → alpha=0(투명)

                dirtyColor.a *= (1.0 - mask);

                return dirtyColor;

            }

            ENDHLSL

        }

    }

}