using UnityEngine;
using iTetris.Core;

namespace iTetris
{
    public sealed class CrystalView
    {
        public static readonly Color[] Palette = {
            new Color(.12f,.79f,.96f), new Color(.28f,.43f,.98f), new Color(1f,.58f,.19f),
            new Color(1f,.79f,.28f), new Color(.20f,.89f,.66f), new Color(.65f,.36f,.98f), new Color(1f,.34f,.44f)
        };
        readonly Material[] materials = new Material[7];
        readonly Material[] insetMaterials = new Material[7];
        readonly Mesh crystal;
        readonly Transform parent;
        static readonly System.Collections.Generic.Dictionary<Color,Material> flat = new System.Collections.Generic.Dictionary<Color,Material>();
        public CrystalView(Transform parent)
        {
            this.parent = parent;
            var asset = Resources.Load<GameObject>("Art/CrystalBlock");
            crystal = asset != null ? asset.GetComponentInChildren<MeshFilter>().sharedMesh : MakeCrystal();
            for (int i = 0; i < 7; i++)
            {
                var c = Palette[i];
                materials[i] = Lit(c, .35f, .87f, c * .16f);
                insetMaterials[i] = Lit(Color.Lerp(c,Color.white,.18f), .15f,.96f,c*.22f);
            }
        }
        static Mesh MakeCrystal()
        {
            var m = new Mesh();
            // Separate vertices keep the bevel faces optically sharp.
            var p = new[] {new Vector3(-.48f,-.48f,.18f),new Vector3(.48f,-.48f,.18f),new Vector3(.48f,.48f,.18f),new Vector3(-.48f,.48f,.18f),new Vector3(-.35f,-.35f,-.22f),new Vector3(.35f,-.35f,-.22f),new Vector3(.35f,.35f,-.22f),new Vector3(-.35f,.35f,-.22f)};
            int[][] faces = {new[]{0,3,2,1},new[]{4,5,6,7},new[]{0,1,5,4},new[]{1,2,6,5},new[]{2,3,7,6},new[]{3,0,4,7}};
            var verts = new Vector3[24]; var tris = new int[36];
            for (int f=0;f<6;f++) { for(int v=0;v<4;v++) verts[f*4+v]=p[faces[f][v]]; int b=f*4,t=f*6; tris[t]=b;tris[t+1]=b+2;tris[t+2]=b+1;tris[t+3]=b;tris[t+4]=b+3;tris[t+5]=b+2; }
            m.vertices=verts;m.triangles=tris;m.RecalculateNormals();return m;
        }
        public static Material Lit(Color c,float metallic,float smooth,Color emission)
        {
            var m = new Material(Shader.Find("iTetris/CrystalGlass"));
            m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);
            m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission);return m;
        }
        public static Material Unlit(Color c)
        {
            if(flat.TryGetValue(c,out var existing))return existing;
            var m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.SetColor("_BaseColor",c);flat[c]=m;
            if(c.a<.99f) {m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.SetInt("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;}
            return m;
        }
        public GameObject Block(PieceKind kind,Vector3 pos,float scale=1)
        {
            var g = new GameObject("Crystal " + kind);g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=Vector3.one*scale;
            var face=new GameObject("Faceted crystal");face.transform.SetParent(g.transform,false);
            if(Resources.Load<GameObject>("Art/CrystalBlock")!=null)face.transform.localRotation=Quaternion.Euler(-90,0,0);
            face.AddComponent<MeshFilter>().sharedMesh=crystal;face.AddComponent<MeshRenderer>().sharedMaterial=materials[(int)kind];
            // Fine perimeter and refracted interior diagonals give readable glass depth.
            var c=Palette[(int)kind];
            Outline(g.transform,.94f,.94f,.018f,new Color(c.r*1.4f,c.g*1.4f,c.b*1.4f,1),-.245f);
            Outline(g.transform,.69f,.69f,.008f,Color.Lerp(c,Color.white,.38f),-.25f);
            for(int i=0;i<4;i++)
            {
                float sx=(i%2==0?-1:1),sy=(i<2?-1:1);
                Line(g.transform,new Vector3(sx*.47f,sy*.47f,-.255f),new Vector3(sx*.345f,sy*.345f,-.255f),.009f,Color.Lerp(c,Color.white,.3f));
            }
            return g;
        }
        public void SetKind(GameObject block,PieceKind kind)
        {
            block.GetComponentInChildren<MeshRenderer>().sharedMaterial=materials[(int)kind];
            var c=Palette[(int)kind];
            foreach(var line in block.GetComponentsInChildren<LineRenderer>())
                line.sharedMaterial=Unlit(line.startWidth>.015f?new Color(c.r*1.4f,c.g*1.4f,c.b*1.4f,1):Color.Lerp(c,Color.white,.38f));
        }
        public GameObject Ghost(PieceKind kind,Vector3 pos)
        {
            var g=new GameObject("Landing outline");g.transform.SetParent(parent,false);g.transform.localPosition=pos;
            Outline(g.transform,.91f,.91f,.018f,new Color(.30f,.61f,.70f,.55f),-.26f);return g;
        }
        public static void Outline(Transform p,float w,float h,float thickness,Color color,float z=0)
        {
            Line(p,new Vector3(-w/2,-h/2,z),new Vector3(w/2,-h/2,z),thickness,color);
            Line(p,new Vector3(w/2,-h/2,z),new Vector3(w/2,h/2,z),thickness,color);
            Line(p,new Vector3(w/2,h/2,z),new Vector3(-w/2,h/2,z),thickness,color);
            Line(p,new Vector3(-w/2,h/2,z),new Vector3(-w/2,-h/2,z),thickness,color);
        }
        public static GameObject Line(Transform parent,Vector3 from,Vector3 to,float width,Color c)
        {
            var g=new GameObject("Light edge");g.transform.SetParent(parent,false);
            var lr=g.AddComponent<LineRenderer>();lr.useWorldSpace=false;lr.positionCount=2;lr.SetPosition(0,from);lr.SetPosition(1,to);lr.startWidth=lr.endWidth=width;lr.sharedMaterial=Unlit(c);lr.numCapVertices=3;return g;
        }
    }
}
