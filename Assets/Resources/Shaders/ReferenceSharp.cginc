// Restore local edge contrast lost when the small concept artwork is enlarged.
// Limit correction to avoid bright halos around crystal rims and text.
fixed4 ReferenceSharp(sampler2D artwork,float2 uv,float2 texel)
{
    fixed4 center=tex2D(artwork,uv);
    fixed3 neighbours=(tex2D(artwork,uv+float2(texel.x,0)).rgb
        +tex2D(artwork,uv-float2(texel.x,0)).rgb
        +tex2D(artwork,uv+float2(0,texel.y)).rgb
        +tex2D(artwork,uv-float2(0,texel.y)).rgb)*.25;
    float3 detail=center.rgb-neighbours;
    float edge=max(abs(detail.r),max(abs(detail.g),abs(detail.b)));
    center.rgb=saturate(center.rgb+clamp(detail*.9,-.085,.085)*smoothstep(.008,.06,edge));
    return center;
}
