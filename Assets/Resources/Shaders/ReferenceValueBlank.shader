Shader "iTetris/ReferenceValueBlank"
{
 Properties { [PerRendererData] _MainTex("Artwork",2D)="white"{} _Bounds("Live value bounds",Vector)=(0,0,1,1) }
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
   struct input { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
   struct output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
   sampler2D _MainTex; float4 _Bounds;
   output vert(input v){output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   fixed4 frag(output i):SV_Target
   {
    float t=saturate((i.uv.x-_Bounds.x)/(_Bounds.z-_Bounds.x));
    fixed4 left=tex2D(_MainTex,float2(_Bounds.x,i.uv.y));
    fixed4 right=tex2D(_MainTex,float2(_Bounds.z,i.uv.y));
    fixed4 original=tex2D(_MainTex,i.uv);
    float feather=saturate(min(i.uv.y-_Bounds.y,_Bounds.w-i.uv.y)*770/2);
    return lerp(original,lerp(left,right,t),feather)*i.color;
   }
   ENDCG
  }
 }
}
