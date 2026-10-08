Shader "iTetris/ReferenceDigit"
{
 Properties { [PerRendererData] _MainTex("Glyph artwork",2D)="white"{} _Monochrome("White digits",Float)=0 _OpaqueGlyph("Preserve painted edges",Float)=0 _GlyphRect("Supplemental glyph UV",Vector)=(0,0,1,1) _GlyphGrid("Reference pixel grid",Vector)=(0,0,0,0) }
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
   sampler2D _MainTex;float4 _MainTex_TexelSize;float _Monochrome,_OpaqueGlyph;float4 _GlyphRect,_GlyphGrid;
   fixed4 sampleGlyph(float2 grid)
   {
    float2 uv=_GlyphRect.xy+(grid+.5)/_GlyphGrid.xy*_GlyphRect.zw;
    float2 step=_GlyphRect.zw/_GlyphGrid.xy/3;
    // Area sampling retains fractional edge coverage when the generated glyph
    // is reduced to the original artwork's small raster size.
    fixed4 c=0;
    for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
        c+=tex2D(_MainTex,uv+float2(x,y)*step);
    return c/9;
   }
   output vert(input v){output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   fixed4 frag(output i):SV_Target
   {
    fixed4 source=ReferenceSharp(_MainTex,i.uv,_MainTex_TexelSize.xy);
    if(_GlyphGrid.y>0)
    {
     float2 grid=(i.uv-_GlyphRect.xy)/_GlyphRect.zw*_GlyphGrid.xy-.5;
     float2 base=floor(grid),f=frac(grid);
     fixed4 a=sampleGlyph(base),b=sampleGlyph(base+float2(1,0)),c=sampleGlyph(base+float2(0,1)),d=sampleGlyph(base+float2(1,1));
     source=lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y);
     float coverage=dot(source.rgb,float3(.2126,.7152,.0722));
     float outer=lerp(coverage,max(max(a.r,b.r),max(c.r,d.r)),.2);
     return fixed4(i.color.rgb*coverage/max(outer,.001),outer*i.color.a);
    }
    if(_OpaqueGlyph>0){source.a=1;return source*i.color;}
    source.a=smoothstep(.12,.25,min(source.r,source.g));
    source.rgb=lerp(source.rgb,min(source.r,source.g).xxx,_Monochrome);
    return source*i.color;
   }
   ENDCG
  }
 }
}
