Shader "iTetris/AuroraBackground"
{
 Properties { _MainTex("Lake",2D)="black"{} }
 SubShader
 {
 Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
 Pass
 {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
 struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 CBUFFER_START(UnityPerMaterial)
 float4 _MainTex_ST;
 CBUFFER_END
 Varyings vert(Attributes a){Varyings o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;return o;}
 half4 frag(Varyings i):SV_Target
 {
 float2 uv=i.uv;
 float wave=sin(uv.x*12+_Time.y*.08)*sin(uv.y*8+_Time.y*.06);
 uv.x+=wave*.0009*smoothstep(.5,.9,uv.y);
 half3 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb;
 c*=.72+.035*sin(_Time.y*.12+uv.x*5);
 return half4(c,1);
 }
 ENDHLSL
 }
 }
}
