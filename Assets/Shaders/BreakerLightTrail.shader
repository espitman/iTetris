Shader "iTetris/BreakerLightTrail"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings vert(Attributes input)
            {
                Varyings output; output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=input.uv;output.color=input.color;return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                half distance=abs(input.uv.y*2-1);
                half glow=exp(-distance*distance*6)*.18+exp(-distance*distance*100)*.82;
                return half4(input.color.rgb*2, input.color.a*glow);
            }
            ENDHLSL
        }
    }
}
