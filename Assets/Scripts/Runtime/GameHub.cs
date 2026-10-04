using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace iTetris
{
    public sealed partial class GameController
    {
        GameObject tetrisWorld,tetrisUiRoot,hubRoot;
        bool hubVisible;
        int selectedHubGame;
        const int HubGameCount=6;
        const float HubCardStride=548,HubViewportWidth=1260;
        readonly Outline[] hubCardRims=new Outline[HubGameCount];
        readonly Button[] hubPlayButtons=new Button[HubGameCount];
        ScrollRect hubScroll;
        Text hubSoundText;
        Button hubPrevious,hubNext;

        void BuildHub()
        {
            hubRoot=UIObject("Game hub",canvas.transform,Vector2.zero,new Vector2(1600,1000)).gameObject;
            var root=hubRoot.transform;
            // A quiet glass wash keeps the library readable over the shared aurora.
            var wash=UIObject("Hub backdrop wash",root,Vector2.zero,new Vector2(4000,4000));
            wash.gameObject.AddComponent<Image>().color=new Color(.012f,.029f,.05f,.5f);
            Label(root,"C R Y S T A L   A R C A D E",new Vector2(0,402),new Vector2(1100,65),39,Pale);
            Label(root,"A LITTLE SPACE TO PLAY",new Vector2(0,344),new Vector2(900,32),13,Muted);
            var sound=ButtonAt(root,"",new Vector2(-685,421),new Vector2(120,38),()=>{audioWorld.Toggle();UpdateSound();UpdateHubSound();});
            hubSoundText=sound.GetComponentInChildren<Text>();
            BuildHubExitButton(root);
            var viewport=UIObject("Horizontal game library",root,new Vector2(0,-12),new Vector2(HubViewportWidth,614));
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color=new Color(0,0,0,.001f);
            var content=UIObject("Game cards",viewport,Vector2.zero,new Vector2(HubGameCount*HubCardStride-28,590));
            content.anchorMin=content.anchorMax=content.pivot=new Vector2(0,.5f);
            hubScroll=viewport.gameObject.AddComponent<ScrollRect>();hubScroll.viewport=viewport;hubScroll.content=content;
            hubScroll.horizontal=true;hubScroll.vertical=false;hubScroll.movementType=ScrollRect.MovementType.Clamped;
            hubScroll.scrollSensitivity=45;hubScroll.decelerationRate=.08f;
            HubCard(content,0,"Tetris","CLASSIC PUZZLE","Arrange. Clear. Find your flow.","Art/HubTetris");
            HubCard(content,1,"Brick Garden","MERGE PUZZLE","Small pieces. A brighter garden.","Art/HubBrickGarden");
            HubCard(content,2,"Crystal Breaker","ARCADE","One ball. A thousand little sparks.","Art/HubCrystalBreaker");
            HubCard(content,3,"Light Path","LOGIC PUZZLE","Reflect the light. Illuminate the way.","Art/HubLightPath");
            HubCard(content,4,"Orbit Rings","COLOR PUZZLE","Turn the rings. Find your harmony.","Art/HubOrbitRings");
            HubCard(content,5,"Glass Tower","PRECISION ARCADE","Stack with care. Reach a little higher.","Art/HubGlassTower");
            hubPrevious=ButtonAt(root,"←",new Vector2(-710,-12),new Vector2(62,62),()=>SelectHubGame(selectedHubGame-1));
            hubNext=ButtonAt(root,"→",new Vector2(710,-12),new Vector2(62,62),()=>SelectHubGame(selectedHubGame+1));
            Label(root,"← / →  SELECT     ENTER  PLAY     M  SOUND",new Vector2(0,-466),new Vector2(1000,28),11,Muted);
            SelectHubGame(0);UpdateHubSound();
        }
        void BuildHubExitButton(Transform root)
        {
            var button=ButtonAt(root,"",new Vector2(675,421),new Vector2(142,48),()=>Application.Quit());
            button.gameObject.name="Exit game";
            button.GetComponent<Image>().color=new Color(.14f,.04f,.07f,.95f);
            var rim=button.gameObject.AddComponent<Outline>();rim.effectColor=new Color(.87f,.32f,.43f,.75f);rim.effectDistance=Vector2.one;
            var colors=button.colors;colors.highlightedColor=new Color(1,.68f,.75f);colors.pressedColor=new Color(.8f,.3f,.4f);button.colors=colors;
            var caption=button.GetComponentInChildren<Text>();caption.text="EXIT GAME";caption.fontSize=13;caption.color=new Color(.96f,.72f,.77f);caption.rectTransform.anchoredPosition=new Vector2(15,0);
            caption.rectTransform.sizeDelta=new Vector2(102,40);
            var icon=UIObject("Power icon",button.transform,new Vector2(-49,0),new Vector2(21,21));
            var image=icon.gameObject.AddComponent<Image>();image.raycastTarget=false;image.color=new Color(1,.57f,.66f);
            const int n=64;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false);var pixels=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float dx=x-31.5f,dy=y-31.5f,d=Mathf.Sqrt(dx*dx+dy*dy);
                bool ring=Mathf.Abs(d-22)<2.6f&&!(dy>9&&Mathf.Abs(dx)<9);
                bool stem=Mathf.Abs(dx)<2.6f&&dy>1&&dy<28;
                pixels[y*n+x]=new Color(1,1,1,ring||stem?1:0);
            }
            texture.SetPixels(pixels);texture.Apply();image.sprite=Sprite.Create(texture,new Rect(0,0,n,n),new Vector2(.5f,.5f));
        }
        void HubCard(Transform root,int index,string title,string genre,string description,string art)
        {
            var card=Panel(root,new Vector2(260+index*HubCardStride,0),new Vector2(520,590),new Color(.019f,.041f,.065f,.95f));
            card.anchorMin=card.anchorMax=new Vector2(0,.5f);
            card.gameObject.name=title+" library card";
            var button=card.gameObject.AddComponent<Button>();button.targetGraphic=card.GetComponent<Image>();
            var colors=button.colors;colors.highlightedColor=new Color(.76f,.94f,1);colors.pressedColor=new Color(.5f,.8f,.9f);button.colors=colors;
            var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;
            button.onClick.AddListener(()=>SelectHubGame(index));
            hubCardRims[index]=card.GetComponent<Outline>();hubCardRims[index].effectDistance=new Vector2(2,2);
            var pictureFrame=UIObject(title+" artwork",card,new Vector2(0,118),new Vector2(472,295));
            pictureFrame.gameObject.AddComponent<RectMask2D>();
            var picture=UIObject("Preview",pictureFrame,Vector2.zero,new Vector2(472,295));
            var image=picture.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>(art);image.raycastTarget=false;
            // Concept images advertise only a future game; no game scene exists yet.
            Label(card,genre,new Vector2(0,-70),new Vector2(460,24),11,Muted);
            Label(card,title,new Vector2(0,-111),new Vector2(460,60),36,Pale);
            Label(card,description,new Vector2(0,-163),new Vector2(480,38),15,Muted);
            hubPlayButtons[index]=ButtonAt(card,index==0?"PLAY":"COMING SOON",new Vector2(0,-230),new Vector2(360,52),()=>{SelectHubGame(index);PrimaryHubAction();},index==0);
            hubPlayButtons[index].interactable=index==0;
            if(index>0)Label(pictureFrame,"CONCEPT PREVIEW",new Vector2(0,-123),new Vector2(472,25),10,Color.white);
        }
        void SelectHubGame(int index)
        {
            selectedHubGame=Mathf.Clamp(index,0,HubGameCount-1);
            for(int i=0;i<HubGameCount;i++)hubCardRims[i].effectColor=i==selectedHubGame?new Color(.3f,.83f,.9f,.95f):new Color(.2f,.38f,.46f,.55f);
            hubPlayButtons[0].GetComponentInChildren<Text>().text=started&&!game.GameOver?"RESUME":"PLAY";
            hubPrevious.interactable=selectedHubGame>0;hubNext.interactable=selectedHubGame<HubGameCount-1;
            Canvas.ForceUpdateCanvases();hubScroll.StopMovement();
            float range=hubScroll.content.rect.width-HubViewportWidth;
            hubScroll.horizontalNormalizedPosition=Mathf.Clamp01((260+selectedHubGame*HubCardStride-HubViewportWidth*.5f)/range);
        }

        void UpdateHubSound(){if(hubSoundText!=null)hubSoundText.text=audioWorld.Muted?"SOUND OFF":"SOUND ON";}
        void OpenHub()
        {
            if(hubRoot==null)return;
            paused=started&&!game.GameOver;
            helpPanel.SetActive(false);ResetInput();
            hubVisible=true;tetrisWorld.SetActive(false);tetrisUiRoot.SetActive(false);hubRoot.SetActive(true);
            SelectHubGame(selectedHubGame);UpdateHubSound();
        }
        void PrimaryHubAction(){if(selectedHubGame==0){OpenTetris();PrimaryAction();}}
        void OpenTetris()
        {
            hubVisible=false;if(hubRoot!=null)hubRoot.SetActive(false);
            tetrisWorld.SetActive(true);tetrisUiRoot.SetActive(true);ResetInput();
            if(started&&!game.GameOver){paused=true;ShowMenu("PAUSED","TAKE A BREATH","RESUME",true);}
        }
        void HandleHubInput()
        {
            if(Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.A))SelectHubGame(selectedHubGame-1);
            if(Input.GetKeyDown(KeyCode.RightArrow)||Input.GetKeyDown(KeyCode.D))SelectHubGame(selectedHubGame+1);
            if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Space))PrimaryHubAction();
        }
        #if !UNITY_WEBGL || UNITY_EDITOR
        IEnumerator HubCapture()
        {
            yield return new WaitForSecondsRealtime(2);
            var args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"--hub-last-card")>=0)SelectHubGame(HubGameCount-1);
            yield return null;int index=Array.IndexOf(args,"--capture-path");
            string path=index>=0&&index+1<args.Length?args[index+1]:System.IO.Path.Combine(Application.persistentDataPath,"GameHub.png");
            ScreenCapture.CaptureScreenshot(path);Debug.Log("HUB_CAPTURE_REQUESTED");
            yield return new WaitForSecondsRealtime(2);
            if(Array.IndexOf(args,"--quit-after-capture")>=0)Application.Quit();
        }
        #endif
    }
}
