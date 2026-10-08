using UnityEngine;
using UnityEngine.UI;
namespace iTetris
{
    public sealed partial class GameController
    {
        // Coordinates refer to the approved concept sheet. Keeping its actual artwork
        // preserves the lettering, facets, corner geometry and scenic composition.
        static readonly Rect HomeCrop=new Rect(667,121,326,631);
        static readonly Rect LaunchCrop=new Rect(315,121,334,631);
        static readonly Rect PauseCrop=new Rect(1359,121,334,631);
        static readonly Rect OverCrop=new Rect(1711,121,320,631);
        RawImage referenceMenuArt;
        Texture2D referenceAtlas;
        Material referenceSharpMaterial;
        RawImage homeTop,homeBottom,launchTop,launchBottom;
        void ExtendSceneryEdge(RawImage image,Rect page,bool top)
        {
            var uv=image.uvRect;
            image.uvRect=new Rect(uv.x,uv.yMax,uv.width,-uv.height);
            uv=image.uvRect;
            var material=new Material(Resources.Load<Shader>("Shaders/ReferenceSceneryExtension"));
            material.SetVector("_Bounds",new Vector4(uv.x,uv.y,uv.width,uv.height));
            material.SetFloat("_Edge",(770-(top?page.y:page.yMax))/770);
            material.SetFloat("_Top",top?1:0);image.material=material;
        }
        void LayoutReferencePage(RectTransform root,Rect page,float height,RawImage top,RawImage bottom)
        {
            float naturalHeight=900*page.height/page.width;
            root.localScale=new Vector3(1,naturalHeight/1750,1);
            // Extend only the empty sky and foreground scenery on taller phones.
            // The logo, hero crystals, lettering and every button retain their
            // original proportions, with all controls visible at full width.
            float extra=Mathf.Max(0,(height/naturalHeight-1)*1750/2);
            top.gameObject.SetActive(extra>0);bottom.gameObject.SetActive(extra>0);
            top.rectTransform.sizeDelta=bottom.rectTransform.sizeDelta=new Vector2(900,extra+.2f);
            top.rectTransform.anchoredPosition=new Vector2(0,875+extra/2);
            bottom.rectTransform.anchoredPosition=new Vector2(0,-875-extra/2);
        }
        Rect ReferenceUV(Rect source){return new Rect(source.x/2043f,(770-source.y-source.height)/770f,source.width/2043f,source.height/770f);}
        void PositionReference(RectTransform target,Rect region,Rect page)
        {
            target.anchoredPosition=new Vector2(((region.center.x-page.x)/page.width-.5f)*900,(.5f-(region.center.y-page.y)/page.height)*1750);
            target.sizeDelta=new Vector2(region.width/page.width*900,region.height/page.height*1750);
        }
        RawImage ReferenceArt(Transform parent,Rect source,Rect destination,Rect page,string name)
        {
            var r=UIObject(name,parent,Vector2.zero,Vector2.one);PositionReference(r,destination,page);
            var image=r.gameObject.AddComponent<RawImage>();image.texture=referenceAtlas;image.uvRect=ReferenceUV(source);image.material=referenceSharpMaterial;image.raycastTarget=false;return image;
        }
        Button ReferenceButton(Transform parent,Rect bounds,Rect page,string name,System.Action click)
        {
            var r=UIObject(name,parent,Vector2.zero,Vector2.one);PositionReference(r,bounds,page);
            var image=r.gameObject.AddComponent<Image>();image.color=Color.white;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.clear;colors.highlightedColor=Color.clear;colors.pressedColor=new Color(.1f,.85f,1,.14f);colors.selectedColor=Color.clear;button.colors=colors;
            var n=button.navigation;n.mode=Navigation.Mode.None;button.navigation=n;button.onClick.AddListener(()=>click());
            var bookkeeping=MobileLabel(r,name,Vector2.zero,Vector2.one,1,Color.clear);bookkeeping.color=Color.clear;
            return button;
        }
        Text ReferenceValue(Transform parent,Rect dest,Rect page,string value,int points,Color color)
        {
            var patch=ReferenceArt(parent,dest,dest,page,"Live value area");
            var bounds=ReferenceUV(dest);var material=new Material(Resources.Load<Shader>("Shaders/ReferenceValueBlank"));material.SetVector("_Bounds",new Vector4(bounds.xMin,bounds.yMin,bounds.xMax,bounds.yMax));patch.material=material;
            var label=MobileLabel(patch.transform,value,Vector2.zero,patch.rectTransform.sizeDelta,points,color);label.horizontalOverflow=HorizontalWrapMode.Overflow;return label;
        }
        void BuildReferenceMenus(Transform root)
        {
            referenceAtlas=Resources.Load<Texture2D>("Art/MobileUI/approved-reference");
            referenceSharpMaterial=new Material(Resources.Load<Shader>("Shaders/ReferenceMenuSharp"));
            mobileHome=UIObject("Approved home composition",root,Vector2.zero,new Vector2(900,1750));
            homeTop=ReferenceArt(mobileHome,new Rect(HomeCrop.x,HomeCrop.y,HomeCrop.width,16),HomeCrop,HomeCrop,"Fullscreen sky extension");
            homeBottom=ReferenceArt(mobileHome,new Rect(HomeCrop.x,HomeCrop.yMax-32,HomeCrop.width,32),HomeCrop,HomeCrop,"Fullscreen foreground extension");
            ExtendSceneryEdge(homeTop,HomeCrop,true);ExtendSceneryEdge(homeBottom,HomeCrop,false);
            ReferenceArt(mobileHome,HomeCrop,HomeCrop,HomeCrop,"Approved home art");
            ReferenceButton(mobileHome,new Rect(691,441,278,91),HomeCrop,"PLAY",()=>PrimaryAction());
            marathonButton=ReferenceButton(mobileHome,new Rect(680,539,147,66),HomeCrop,"MARATHON",()=>{zen=false;RefreshMenuMode();});
            zenButton=ReferenceButton(mobileHome,new Rect(837,539,146,66),HomeCrop,"ZEN",()=>{zen=true;RefreshMenuMode();});
            ReferenceButton(mobileHome,new Rect(680,615,147,51),HomeCrop,"HOW TO PLAY",()=>ToggleHelp());
            var sound=ReferenceButton(mobileHome,new Rect(837,615,147,51),HomeCrop,"SOUND",()=>{audioWorld.Toggle();UpdateSound();});
            var soundLabel=ReferenceValue(mobileHome,new Rect(889,631,72,19),HomeCrop,"SOUND ON",40,Pale);mobileSoundLabels.Add(soundLabel);
            homeBest=ReferenceValue(mobileHome,new Rect(810,684,87,26),HomeCrop,"000000",70,Gold);
            homeBest.gameObject.AddComponent<ReferenceDigits>().Initialize(referenceAtlas,true,true);
            mobileSplash=UIObject("Approved launch composition",root,Vector2.zero,new Vector2(900,1750));
            launchTop=ReferenceArt(mobileSplash,new Rect(LaunchCrop.x,LaunchCrop.y,LaunchCrop.width,16),LaunchCrop,LaunchCrop,"Fullscreen sky extension");
            launchBottom=ReferenceArt(mobileSplash,new Rect(LaunchCrop.x,LaunchCrop.yMax-32,LaunchCrop.width,32),LaunchCrop,LaunchCrop,"Fullscreen foreground extension");
            ExtendSceneryEdge(launchTop,LaunchCrop,true);ExtendSceneryEdge(launchBottom,LaunchCrop,false);
            ReferenceArt(mobileSplash,LaunchCrop,LaunchCrop,LaunchCrop,"Approved launch art");
            mobileMenu=UIObject("Approved dedicated menu",root,Vector2.zero,new Vector2(900,1750));
            overlay=UIObject("Dedicated menu content",mobileMenu,Vector2.zero,new Vector2(900,1750));
            referenceMenuArt=ReferenceArt(overlay,PauseCrop,PauseCrop,PauseCrop,"Approved menu art");
            overlayTitle=MobileLabel(overlay,"PAUSED",Vector2.zero,Vector2.one,1,Color.clear);overlayTitle.gameObject.SetActive(false);
            overlaySubtitle=MobileLabel(overlay,"TAKE A BREATH",Vector2.zero,Vector2.one,1,Color.clear);overlaySubtitle.gameObject.SetActive(false);
            primaryButton=ReferenceButton(overlay,new Rect(1410,371,228,62),PauseCrop,"RESUME",()=>PrimaryAction());
            secondaryButton=ReferenceButton(overlay,new Rect(1410,440,229,54),PauseCrop,"NEW GAME",()=>StartGame());
            menuMainButton=ReferenceButton(overlay,new Rect(1410,501,229,51),PauseCrop,"MAIN MENU",()=>MobileHome());
            menuSoundButton=ReferenceButton(overlay,new Rect(1430,563,192,47),PauseCrop,"SOUND",()=>{audioWorld.Toggle();UpdateSound();});
            var menuSound=ReferenceValue(menuSoundButton.transform,new Rect(1503,573,82,19),new Rect(1430,563,192,47),"SOUND ON",48,Color.white);
            // Button-local patch follows that button instead of a second full-page coordinate system.
            menuSound.transform.parent.GetComponent<RectTransform>().anchoredPosition=new Vector2(48,5);
            menuSound.transform.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(221,53);
            menuSound.rectTransform.sizeDelta=new Vector2(221,53);mobileSoundLabels.Add(menuSound);
            mobileResult=ReferenceValue(overlay,new Rect(1803,371,134,36),OverCrop,"000000",110,Color.white);
            mobileBest=ReferenceValue(overlay,new Rect(1861,429,74,23),OverCrop,"000000",64,new Color(1,.95f,.15f));
            mobileResult.gameObject.AddComponent<ReferenceDigits>().Initialize(referenceAtlas,false);
            mobileBest.gameObject.AddComponent<ReferenceDigits>().Initialize(referenceAtlas,true);
        }
    }
}
