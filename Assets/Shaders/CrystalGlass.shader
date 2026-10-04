Shader "iTetris/CrystalGlass"
{
 Properties
 {
 _BaseColor("Crystal tint",Color)=(.1,.8,1,1)
 _Metallic("Metallic",Range(0,1))=.35
 _Smoothness("Polish",Range(0,1))=.9
 _EmissionColor("Glow",Color)=(.05,.2,.3,1)
 }
 SubShader
 {
 Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
 Pass
 {
 Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
 struct Varyings {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float2 uv:TEXCOORD1;};
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;half4 _EmissionColor;float _Metallic;float _Smoothness;
 CBUFFER_END
 Varyings vert(Attributes a){Varyings o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(a.normalOS);o.uv=a.uv;return o;}
 half4 frag(Varyings i):SV_Target
 {
 float2 p=i.uv-.5;
 float3 n=normalize(i.normalWS);
 float front=pow(abs(n.z),10);
 float key=saturate(dot(n,normalize(float3(-.55,.8,-.9))));
 float edge=pow(saturate(max(abs(p.x),abs(p.y))*2),10);
 float diagonal=exp(-abs(p.x-p.y)*36)+exp(-abs(p.x+p.y)*36);
 float broad=exp(-pow((p.x+p.y-.12)*6,2));
 float secondary=exp(-pow((p.x-p.y+.22)*9,2));
 float inner=smoothstep(.31,.355,max(abs(p.x),abs(p.y)));
 float bevel=(1-front)*(.4+key*.7);
 half3 tint=_BaseColor.rgb;
 half3 color=tint*(.24+.25*key+.22*broad+.10*secondary+bevel);
 color+=lerp(tint,half3(1,1,1),.28)*(diagonal*.11+edge*.38+inner*.13);
 color+=half3(.65,.85,1)*pow(saturate((p.y-p.x)*.9+.2),9)*.3;
 color+=_EmissionColor.rgb*.35;
 return half4(color,1);
 }
 ENDHLSL
 }
 }
}
