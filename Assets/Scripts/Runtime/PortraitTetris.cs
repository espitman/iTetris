using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using iTetris.Core;
namespace iTetris {
 public sealed partial class GameController {
  enum MobilePage {Splash,Home,Play,Pause,GameOver,Help}
  MobilePage mobilePage,helpReturnPage;
  RectTransform mobileHud,mobileHome,mobileSplash,mobileMenu;
  Camera mobileBackground;
  readonly List<Text> mobileSoundLabels=new List<Text>();
  readonly RawImage[] mobilePreviewPieces=new RawImage[4];
  readonly Camera[] previewCameras=new Camera[4];
  readonly GameObject[,] previewBlocks=new GameObject[4,4];
  readonly CrystalView[] previewViews=new CrystalView[4];
  readonly PieceKind?[] previewKinds=new PieceKind?[4];
  Button mobileHelpButton,menuMainButton,menuSoundButton;
  Text mobileResult,mobileBest,homeBest;Button marathonButton,zenButton;
  static readonly Color Gold=new Color(1,.73f,.18f),Blue=new Color(.08f,.65f,1),Purple=new Color(.7f,.22f,1);
  Text MobileLabel(Transform p,string value,Vector2 pos,Vector2 size,int points,Color c){var r=UIObject(value,p,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=Resources.Load<Font>("Fonts/Rajdhani-Bold")??font;t.text=value;t.fontSize=points;t.color=c;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
  RectTransform Frame(Transform p,Vector2 pos,Vector2 size,Color c,float border=12){var r=UIObject("Crystal frame",p,pos,size);var f=r.gameObject.AddComponent<ArcadeFrame>();f.accent=c;f.border=border;return r;}
  Button ArcadeButton(Transform p,string title,Vector2 pos,Vector2 size,Color c,System.Action action,int points=48)
  {
   var tex=size.x/size.y>=2?Resources.Load<Texture2D>("Art/MobileUI/"+(c==Gold?"button-gold":"button-neutral")):null;
   var r=tex!=null?UIObject("Crystal button",p,pos,size):Frame(p,pos,size,c,size.x/size.y<2?3:12);
   Graphic graphic;
   if(tex!=null){var image=r.gameObject.AddComponent<Image>();image.sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(tex.width*.14f,tex.height*.26f,tex.width*.14f,tex.height*.26f));image.type=Image.Type.Simple;image.color=c==Gold?Color.white:Color.Lerp(c,Color.white,.12f);graphic=image;}
   else graphic=r.GetComponent<ArcadeFrame>();
   var b=r.gameObject.AddComponent<Button>();b.targetGraphic=graphic;var colors=b.colors;colors.pressedColor=new Color(.65f,.75f,.85f);b.colors=colors;b.onClick.AddListener(()=>action());var n=b.navigation;n.mode=Navigation.Mode.None;b.navigation=n;MobileLabel(r,title,Vector2.zero,size-Vector2.one*24,points,Color.Lerp(c,Color.white,.8f));return b;
  }

  void Art(Transform p,string asset,Vector2 pos,Vector2 size){var r=UIObject(asset,p,pos,size);var im=r.gameObject.AddComponent<RawImage>();im.texture=Resources.Load<Texture2D>("Art/MobileUI/"+asset);im.raycastTarget=false;}
  Button SoundButton(Transform p,Vector2 pos,Vector2 size){var b=ArcadeButton(p,"SOUND ON",pos,size,Blue,()=>{audioWorld.Toggle();UpdateSound();},30);mobileSoundLabels.Add(b.GetComponentInChildren<Text>());return b;}
  void SetMobilePage(MobilePage page){mobilePage=page;mobileHud.gameObject.SetActive(page==MobilePage.Play);mobileHome.gameObject.SetActive(page==MobilePage.Home);mobileSplash.gameObject.SetActive(page==MobilePage.Splash);mobileMenu.gameObject.SetActive(page==MobilePage.Pause||page==MobilePage.GameOver);overlay.gameObject.SetActive(page==MobilePage.Pause||page==MobilePage.GameOver);helpPanel.SetActive(page==MobilePage.Help);if(mobileHelpButton!=null)mobileHelpButton.gameObject.SetActive(page==MobilePage.Play);if(toastText!=null)toastText.gameObject.SetActive(page==MobilePage.Play);tetrisWorld.SetActive(page==MobilePage.Play);cameraMain.enabled=page==MobilePage.Play;ResetInput();if(homeBest!=null)homeBest.text=best.ToString("D6");}
  IEnumerator MobileLaunch(){SetMobilePage(MobilePage.Splash);yield return new WaitForSecondsRealtime(1.25f);SetMobilePage(MobilePage.Home);}
  void MobileHome(){paused=true;SetMobilePage(MobilePage.Home);}
  void LayoutReferenceMenu(float height)
  {
   // Fill a portrait screen by cropping the scenery, never by stretching the
   // crystal frame, lettering or buttons. Both menu frames fit inside this crop.
   var page=mobilePage==MobilePage.GameOver?OverCrop:PauseCrop;
   float naturalHeight=900*page.height/page.width;
   float scale=Mathf.Max(1,height/naturalHeight);
   mobileMenu.localScale=new Vector3(scale,scale*naturalHeight/1750,1);
  }
  void LayoutPortraitChrome(float height){((RectTransform)tetrisUiRoot.transform).sizeDelta=new Vector2(900,height);mobileHud.anchoredPosition=new Vector2(0,height*.5f);LayoutReferencePage(mobileHome,HomeCrop,height,homeTop,homeBottom);LayoutReferencePage(mobileSplash,LaunchCrop,height,launchTop,launchBottom);LayoutReferenceMenu(height);helpPanel.transform.localScale=Vector3.one*Mathf.Min(1,(height-80)/1400f);if(mobileBackground!=null){var tex=Resources.Load<Texture2D>("Art/MobileUI/aurora-background");mobileBackground.orthographicSize=Mathf.Min(tex.height/200f,tex.width/200f/((float)Screen.width/Screen.height));}}
  void BuildPortraitInterface(Transform root){
   mobileBackground=new GameObject("Aurora fullscreen camera").AddComponent<Camera>();mobileBackground.depth=-10;mobileBackground.orthographic=true;mobileBackground.transform.position=new Vector3(0,0,-100);mobileBackground.cullingMask=1<<10;mobileBackground.clearFlags=CameraClearFlags.SolidColor;mobileBackground.backgroundColor=new Color(.01f,.025f,.05f);
   var tex=Resources.Load<Texture2D>("Art/MobileUI/aurora-background");var backdrop=new GameObject("Aurora fullscreen art");backdrop.layer=10;var sr=backdrop.AddComponent<SpriteRenderer>();sr.sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),Vector2.one*.5f,100);backdrop.transform.position=new Vector3(0,0,-90);
   mobileHud=UIObject("Gameplay HUD",root,Vector2.zero,new Vector2(900,400));
   var score=Frame(mobileHud,new Vector2(-220,-70),new Vector2(420,95),Blue,3);MobileLabel(score,"SCORE",new Vector2(-128,0),new Vector2(125,65),30,Pale);scoreText=MobileLabel(score,"000000",new Vector2(62,0),new Vector2(245,75),48,Pale);
   var level=Frame(mobileHud,new Vector2(155,-70),new Vector2(260,95),Blue,3);MobileLabel(level,"LEVEL",new Vector2(-54,0),new Vector2(135,65),30,Pale);levelText=MobileLabel(level,"01",new Vector2(70,0),new Vector2(95,75),48,Pale);
   ArcadeButton(mobileHud,"II",new Vector2(375,-70),new Vector2(106,108),Blue,()=>TogglePause(),52);
   var hold=Frame(mobileHud,new Vector2(-315,-252),new Vector2(230,218),Blue,3);holdText=MobileLabel(hold,"HOLD",new Vector2(0,73),new Vector2(200,42),34,Pale);mobileHoldButton=hold.gameObject.AddComponent<Button>();mobileHoldButton.targetGraphic=hold.GetComponent<ArcadeFrame>();mobileHoldButton.onClick.AddListener(()=>TouchAction("hold"));
   var next=Frame(mobileHud,new Vector2(127,-252),new Vector2(610,218),Blue,3);MobileLabel(next,"NEXT",new Vector2(0,73),new Vector2(570,42),34,Pale);
   for(int g=0;g<4;g++){Transform p=g==3?hold:next;Vector2 pos=g==3?new Vector2(0,-22):new Vector2((g-1)*190,-22);if(g!=3)Frame(p,pos,new Vector2(174,123),Blue,3);var rr=UIObject("Original crystal preview",p,pos,new Vector2(170,105));var raw=rr.gameObject.AddComponent<RawImage>();raw.raycastTarget=false;mobilePreviewPieces[g]=raw;var rt=new RenderTexture(340,210,16,RenderTextureFormat.ARGB32);rt.Create();raw.texture=rt;var camera=new GameObject("Preview camera "+g).AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.35f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<9;camera.targetTexture=rt;camera.transform.position=new Vector3(1000+g*12,1000,-20);camera.enabled=false;previewCameras[g]=camera;var group=new GameObject("Original preview crystals");group.transform.position=new Vector3(1000+g*12,1000,0);previewViews[g]=new CrystalView(group.transform);for(int i=0;i<4;i++){previewBlocks[g,i]=previewViews[g].Block(PieceKind.I,Vector3.zero);foreach(Transform child in previewBlocks[g,i].GetComponentsInChildren<Transform>())child.gameObject.layer=9;}}
   linesText=MobileLabel(mobileHud,"",new Vector2(0,-390),new Vector2(1,1),1,Muted);bestText=MobileLabel(mobileHud,"",Vector2.zero,Vector2.one,1,Muted);modeText=MobileLabel(mobileHud,"",Vector2.zero,Vector2.one,1,Muted);
   toastText=MobileLabel(root,"",Vector2.zero,new Vector2(600,50),32,Teal);toastText.rectTransform.anchorMin=toastText.rectTransform.anchorMax=new Vector2(.5f,0);toastText.rectTransform.anchoredPosition=new Vector2(0,45);
   var help=mobileHelpButton=ArcadeButton(root,"?",Vector2.zero,new Vector2(65,60),Blue,()=>ToggleHelp(),34);help.GetComponent<RectTransform>().anchorMin=help.GetComponent<RectTransform>().anchorMax=new Vector2(.5f,0);help.GetComponent<RectTransform>().anchoredPosition=new Vector2(380,45);
   BuildReferenceMenus(root);
   helpPanel=Frame(root,Vector2.zero,new Vector2(790,1330),Blue,16).gameObject;MobileLabel(helpPanel.transform,"HOW TO PLAY",new Vector2(0,520),new Vector2(740,100),64,Pale);MobileLabel(helpPanel.transform,"DRAG LEFT / RIGHT TO MOVE\n\nTAP LEFT / RIGHT TO ROTATE\n\nSLOW SLIDE DOWN: SOFT DROP\n\nQUICK FLICK DOWN + RELEASE:\nHARD DROP\n\nFLICK UP OR TAP HOLD TO SWAP\n\nMARATHON GETS FASTER\nZEN PLAYS AT YOUR PACE",new Vector2(0,20),new Vector2(700,850),34,Pale);ArcadeButton(helpPanel.transform,"GOT IT",new Vector2(0,-525),new Vector2(660,130),Gold,()=>ToggleHelp(),48);
   SetMobilePage(MobilePage.Home);
  }
  void ShowMobileMenu(string title,string subtitle,string button,bool secondary)
  {
   bool over=title=="GAME OVER";
   SetMobilePage(title=="PAUSED"?MobilePage.Pause:over?MobilePage.GameOver:MobilePage.Home);
   overlayTitle.text=title;overlaySubtitle.text=subtitle;
   referenceMenuArt.uvRect=ReferenceUV(over?OverCrop:PauseCrop);
   LayoutReferenceMenu(((RectTransform)tetrisUiRoot.transform).sizeDelta.y);
   PositionReference(primaryButton.GetComponent<RectTransform>(),over?new Rect(1754,466,230,63):new Rect(1410,371,228,62),over?OverCrop:PauseCrop);
   PositionReference(menuMainButton.GetComponent<RectTransform>(),over?new Rect(1754,541,230,59):new Rect(1410,501,229,51),over?OverCrop:PauseCrop);
   primaryButton.GetComponentInChildren<Text>().text=button;secondaryButton.gameObject.SetActive(secondary);menuSoundButton.gameObject.SetActive(!over);
   mobileResult.transform.parent.gameObject.SetActive(over);mobileBest.transform.parent.gameObject.SetActive(over);
   mobileResult.text=over?game.Score.ToString("D6"):"";mobileBest.text=over?best.ToString("D6"):"";
  }
  void DrawMobilePreviews(PieceKind[] next){for(int i=0;i<3;i++)DrawMobilePreview(next[i],i);mobilePreviewPieces[3].gameObject.SetActive(game.Held.HasValue);if(game.Held.HasValue)DrawMobilePreview(game.Held.Value,3);}
  void DrawMobilePreview(PieceKind kind,int group){if(previewKinds[group]==kind)return;previewKinds[group]=kind;var cells=TetrisGame.Shape(kind);float mx=0,my=0;foreach(var c in cells){mx+=c.X/4f;my+=c.Y/4f;}for(int i=0;i<4;i++){previewViews[group].SetKind(previewBlocks[group,i],kind);previewBlocks[group,i].transform.localPosition=new Vector3(cells[i].X-mx,cells[i].Y-my,0);}previewCameras[group].enabled=true;StartCoroutine(StopPreviewCamera(group));}
  IEnumerator StopPreviewCamera(int group){yield return new WaitForEndOfFrame();previewCameras[group].enabled=false;}
 }
}
