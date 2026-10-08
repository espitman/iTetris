using UnityEngine;
using UnityEngine.UI;

namespace iTetris
{
    // Live digits, not a baked score: original glyphs plus the generated missing
    // numerals share the concept's ink height, spacing, colour and baseline.
    [RequireComponent(typeof(Text))]
    public sealed class ReferenceDigits : MonoBehaviour
    {
        Text value;
        GameObject[] glyphs=new GameObject[0];
        string previous;
        bool gold,home;
        Material material;
        readonly System.Collections.Generic.List<Material> glyphMaterials=new System.Collections.Generic.List<Material>();
        Texture2D atlas, supplemental;
        static readonly Rect[] White = {
            new Rect(1806,375,20,28),new Rect(1829,375,13,28),new Rect(1848,375,20,28),
            new Rect(),new Rect(1870,375,21,28),new Rect(),new Rect(),new Rect(),
            new Rect(1892,375,21,28),new Rect()
        };
        static readonly Rect[] Yellow = {
            new Rect(1864,432,10,16),new Rect(),new Rect(1876,432,10,16),new Rect(),
            new Rect(1910,432,11,16),new Rect(),new Rect(1899,432,10,16),new Rect(),
            new Rect(1887,432,11,16),new Rect()
        };
        static readonly Rect[] Supplemental = {
            new Rect(),new Rect(),new Rect(),new Rect(657,204,153,288),new Rect(),
            new Rect(1086,204,157,288),new Rect(1301,204,156,288),new Rect(1512,204,150,288),new Rect(),
            new Rect(1933,204,153,288)
        };
        static readonly Rect[] HomeGold={
            new Rect(813,688,13,19),new Rect(),new Rect(828,688,11,19),new Rect(),
            new Rect(867,688,13,19),new Rect(),new Rect(854,688,12,19),new Rect(),
            new Rect(841,688,11,19),new Rect()
        };
        static readonly float[] WhiteAdvance={23,19,22,22,22,22,22,22,22,22};
        static readonly float[] GoldAdvance={12,9,11,12,12,12,11,12,12,12};
        static readonly float[] HomeAdvance={15,11,13,14,14,14,13,14,13,14};
        public void Initialize(Texture2D source,bool yellow,bool homePage=false)
        {
            value=GetComponent<Text>();atlas=source;gold=yellow;home=homePage;
            supplemental=Resources.Load<Texture2D>("Art/MobileUI/score-numerals-source");
            material=new Material(Resources.Load<Shader>("Shaders/ReferenceDigit"));
            material.SetFloat("_Monochrome",0);
            material.SetFloat("_OpaqueGlyph",1);
            value.color=Color.clear;
        }
        Rect Glyph(int digit,out bool auxiliary,out bool yellow)
        {
            auxiliary=false;yellow=gold&&Yellow[digit].width>0;
            if(home&&HomeGold[digit].width>0)return HomeGold[digit];
            if(yellow)return Yellow[digit];
            if(White[digit].width>0)return White[digit];
            auxiliary=true;return Supplemental[digit];
        }
        void LateUpdate()
        {
            if(value==null||previous==value.text)return;
            previous=value.text;
            foreach(var glyph in glyphs)if(glyph!=null)Destroy(glyph);
            foreach(var m in glyphMaterials)Destroy(m);glyphMaterials.Clear();
            glyphs=new GameObject[previous.Length];
            var rect=(RectTransform)transform;
            float unit=home?19:gold?16:28;
            float height=rect.rect.height*(home?19f/26:gold?16f/23:28f/36);
            var advance=home?HomeAdvance:gold?GoldAdvance:WhiteAdvance;
            float aspect=transform.lossyScale.y/transform.lossyScale.x;
            float width=0;
            for(int i=0;i<previous.Length;i++)
            {
                int digit=previous[i]-'0';if(digit<0||digit>9)continue;
                var source=Glyph(digit,out bool auxiliary,out bool yellow);
                float glyphWidth=source.width/source.height*height*aspect*(auxiliary?1.27f:1);
                width+=i==previous.Length-1?glyphWidth:advance[digit]/unit*height*aspect;
            }
            if(width<=0)return;
            float fit=Mathf.Min(1,rect.rect.width/width);
            float x=-width*fit/2;
            for(int i=0;i<previous.Length;i++)
            {
                int digit=previous[i]-'0';if(digit<0||digit>9)continue;
                var source=Glyph(digit,out bool auxiliary,out bool yellow);
                if(!gold&&digit==0&&i==previous.Length-1)source=new Rect(1914,375,20,28);
                if(home&&digit==0&&i==previous.Length-1)source=new Rect(881,688,13,19);
                if(gold&&!home&&digit==0&&i==previous.Length-1)source=new Rect(1922,432,10,16);
                float w=source.width/source.height*height*aspect*(auxiliary?1.27f:1)*fit;
                var obj=new GameObject("Live digit "+previous[i],typeof(RectTransform));obj.transform.SetParent(transform,false);glyphs[i]=obj;
                var r=(RectTransform)obj.transform;r.sizeDelta=new Vector2(w,height*fit);r.anchoredPosition=new Vector2(x+w/2,home?-rect.rect.height*.5f/26:gold?rect.rect.height*.5f/23:0);
                if(!auxiliary&&(!gold||yellow))
                {
                    // Keep the painted one-pixel shadow outside each ink box.
                    // It is part of the reference's weight and antialiasing.
                    r.sizeDelta+=new Vector2(w/source.width,height*fit/source.height)*2;
                    source=new Rect(source.x-1,source.y-1,source.width+2,source.height+2);
                }
                var image=obj.AddComponent<RawImage>();image.texture=auxiliary?supplemental:atlas;image.material=material;image.raycastTarget=false;
                float tw=auxiliary?2172:2043,th=auxiliary?724:770;
                image.uvRect=new Rect(source.x/tw,(th-source.y-source.height)/th,source.width/tw,source.height/th);
                if(auxiliary||gold&&!yellow)
                {
                    var m=new Material(material);var uv=image.uvRect;
                    m.SetFloat("_OpaqueGlyph",0);
                    if(auxiliary)
                    {
                        m.SetVector("_GlyphRect",new Vector4(uv.x,uv.y,uv.width,uv.height));
                        m.SetVector("_GlyphGrid",new Vector4(source.width/source.height*unit*1.27f,unit,0,0));
                    }
                    image.material=m;glyphMaterials.Add(m);
                }
                image.color=gold&&!yellow?new Color(1,.98f,.025f):Color.white;
                x+=advance[digit]/unit*height*aspect*fit;
            }
        }
        void OnDestroy(){if(material!=null)Destroy(material);foreach(var m in glyphMaterials)Destroy(m);}
    }
}
