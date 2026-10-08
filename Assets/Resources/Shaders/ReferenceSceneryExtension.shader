Shader "iTetris/ReferenceSceneryExtension"
{
 Properties { [PerRendererData] _MainTex("Scenery",2D)="white"{} _Bounds("UV bounds",Vector)=(0,0,1,1) _Edge("Seam row",Float)=0 _Top("Top extension",Float)=1 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct input {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
   sampler2D _MainTex;float4 _Bounds;float _Edge,_Top;
   output vert(input v){output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
   fixed4 frag(output i):SV_Target
   {
    float position=(i.uv.y-_Bounds.y)/_Bounds.w;
    float distance=lerp(1-position,position,_Top);
    fixed3 edge=0;
    float spread=lerp(1,16,smoothstep(0,.35,distance))/2043;
    float weight=0;
    for(int j=-4;j<=4;j++)
    {
     float w=5-abs(j);
     float x=clamp(i.uv.x+j*spread,_Bounds.x+.5/2043,_Bounds.x+_Bounds.z-.5/2043);
     edge+=tex2D(_MainTex,float2(x,_Edge)).rgb*w;weight+=w;
    }
    edge/=weight;
    // A calm vignette continues the scene without stretching stars or mirroring
    // large foreground rocks. Only scenery outside the reference is extended.
    return fixed4(lerp(edge,fixed3(.005,.017,.045),smoothstep(0,.9,distance)),1);
   }
   ENDCG
  }
 }
}
