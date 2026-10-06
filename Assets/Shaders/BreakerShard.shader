Shader "iTetris/BreakerShard"
{
    Properties { [PerRendererData] _MainTex ("Crystal", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings vert(Attributes input)
            {
                Varyings output; output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=input.uv; output.color=input.color; return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                half4 crystal=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv);
                // Replace the cyan refraction with the brick hue; preserve white facets and alpha.
                half low=min(crystal.r,min(crystal.g,crystal.b));
                half high=max(crystal.r,max(crystal.g,crystal.b));
                return half4(low+(high-low)*input.color.rgb,crystal.a*input.color.a);
            }
            ENDHLSL
        }
    }
}
