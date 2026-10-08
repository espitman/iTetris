using UnityEngine;
using UnityEngine.UI;
namespace iTetris {
 public sealed class ArcadeFrame:MaskableGraphic {
  public Color accent=Color.cyan;
  public float border=12;
  Vector2[] Shape(float inset){var r=rectTransform.rect;float x=r.width/2-inset,y=r.height/2-inset,c=Mathf.Min(border<=3?12:28,Mathf.Min(x,y)*.3f);return new[]{new Vector2(-x+c,-y),new Vector2(x-c,-y),new Vector2(x,-y+c),new Vector2(x,y-c),new Vector2(x-c,y),new Vector2(-x+c,y),new Vector2(-x,y-c),new Vector2(-x,-y+c)};}
  void Ring(VertexHelper v,float a,float b,Color col){var outer=Shape(a);var inner=Shape(b);for(int i=0;i<8;i++){int n=(i+1)%8,k=v.currentVertCount;Color c=Color.Lerp(col,Color.white,i>=3&&i<=5?.42f:0);v.AddVert(outer[i],c,Vector2.zero);v.AddVert(outer[n],c,Vector2.zero);v.AddVert(inner[n],Color.Lerp(c,Color.black,.5f),Vector2.zero);v.AddVert(inner[i],Color.Lerp(c,Color.black,.5f),Vector2.zero);v.AddTriangle(k,k+1,k+2);v.AddTriangle(k,k+2,k+3);}}
  protected override void OnPopulateMesh(VertexHelper v){v.Clear();if(border<=3){var q=Shape(0);v.AddVert(Vector2.zero,new Color(.004f,.017f,.045f,.94f),Vector2.zero);for(int i=0;i<8;i++)v.AddVert(q[i],new Color(.004f,.017f,.045f,.94f),Vector2.zero);for(int i=0;i<8;i++)v.AddTriangle(0,i+1,(i+1)%8+1);Ring(v,0,1.5f,accent);return;}var s=Shape(border);v.AddVert(Vector2.zero,new Color(.018f,.035f,.09f,.96f),Vector2.zero);for(int i=0;i<8;i++)v.AddVert(s[i],new Color(accent.r*.11f,accent.g*.11f,accent.b*.16f,.98f),Vector2.zero);for(int i=0;i<8;i++)v.AddTriangle(0,i+1,(i+1)%8+1);Ring(v,0,3,new Color(accent.r,accent.g,accent.b,.25f));Ring(v,3,5,accent);Ring(v,5,border,Color.Lerp(accent,Color.black,.35f));Ring(v,border,border+2,Color.Lerp(accent,Color.white,.6f));}
 }
}
