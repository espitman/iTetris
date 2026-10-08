Shader "iTetris/ReferenceMenuSharp"
{
 Properties { [PerRendererData] _MainTex("Menu artwork",2D)="white"{} }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   #include "ReferenceSharp.cginc"
   struct input {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   sampler2D _MainTex;float4 _MainTex_TexelSize;
   output vert(input v){output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   fixed4 frag(output i):SV_Target{return ReferenceSharp(_MainTex,i.uv,_MainTex_TexelSize.xy)*i.color;}
   ENDCG
  }
 }
}
