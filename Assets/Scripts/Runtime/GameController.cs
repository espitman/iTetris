using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using iTetris.Core;

namespace iTetris
{
    public sealed class GameController : MonoBehaviour
    {
        TetrisGame game;
        CrystalView crystals;
        Soundscape audioWorld;
        Camera cameraMain;
        Canvas canvas;
        Font font;
        readonly GameObject[,] settled = new GameObject[10,20];
        readonly int[,] displayed = new int[10,20];
        readonly GameObject[] active = new GameObject[4], ghosts = new GameObject[4];
        readonly GameObject[] previews = new GameObject[16];
        readonly List<Spark> sparks = new List<Spark>();
        readonly List<GameObject> clearFlashes = new List<GameObject>();
        readonly List<Transform> stars = new List<Transform>();
        Text scoreText,levelText,linesText,bestText,toastText,modeText,soundText,holdText;
        RectTransform overlay,menuPanel;
        Text overlayTitle,overlaySubtitle;
        Button primaryButton,secondaryButton;
        GameObject helpPanel;
        bool started,paused,zen,dirty=true,clearing,endedPending;
        float horizontalTimer,softTimer,elapsed,toastTimer,shake;
        int lastDirection,best;
        
        Material backdropMaterial;
        Coroutine clearRoutine;
        int? diagnosticBest;
        struct Spark {public Transform T;public Vector3 Velocity;public float Life,Max;}
        static readonly Color Pale = new Color(.76f,.90f,.98f);
        static readonly Color Muted = new Color(.39f,.59f,.70f);
        static readonly Color Teal = new Color(.32f,.91f,.96f);
        public static Vector3 BoardPosition(int x,int y) => new Vector3(x-4.5f,y-9.8f,0);

        void Start()
        {
            Application.targetFrameRate=120;
            best=PlayerPrefs.GetInt("BestScore",0);
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildScene();BuildInterface();
            audioWorld=gameObject.AddComponent<Soundscape>();audioWorld.Initialize();UpdateSound();
            game=new TetrisGame();game.Changed+=()=>dirty=true;game.Locked+=OnLock;game.Ended+=()=>endedPending=true;
            DrawAttract();ShowMenu("iTetris","CRYSTAL / AURORA", "PLAY",false);
            var launchArgs=Environment.GetCommandLineArgs();
            if(Array.IndexOf(launchArgs,"--visual-test")>=0||Array.IndexOf(launchArgs,"--smoke-test")>=0)diagnosticBest=best;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--visual-test")>=0){Application.runInBackground=true;StartCoroutine(VisualCapture());}
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--smoke-test")>=0){Application.runInBackground=true;StartCoroutine(SmokeTest());}
        }
        void BuildScene()
        {
            cameraMain=new GameObject("Main Camera").AddComponent<Camera>();cameraMain.tag="MainCamera";
            cameraMain.orthographic=true;cameraMain.orthographicSize=12.5f;cameraMain.transform.position=new Vector3(0,0,-30);
            cameraMain.backgroundColor=new Color(.01f,.025f,.05f);cameraMain.clearFlags=CameraClearFlags.SolidColor;
            cameraMain.allowHDR=true;cameraMain.nearClipPlane=.1f;cameraMain.farClipPlane=100;
            cameraMain.gameObject.AddComponent<AudioListener>();cameraMain.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var volume=new GameObject("Aurora post processing").AddComponent<Volume>();volume.isGlobal=true;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.profile=profile;
            var bloom=profile.Add<Bloom>();bloom.threshold.Override(1.1f);bloom.intensity.Override(.42f);bloom.scatter.Override(.6f);
            var tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.35f,.43f,.56f);
            var light=new GameObject("Crystal key light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.7f;light.color=new Color(.73f,.89f,1);light.transform.rotation=Quaternion.Euler(35,25,0);
            var fill=new GameObject("Violet fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.6f;fill.color=new Color(.62f,.55f,1);fill.transform.rotation=Quaternion.Euler(-30,-55,0);
            var back=GameObject.CreatePrimitive(PrimitiveType.Quad);back.name="Aurora lake backdrop";Destroy(back.GetComponent<Collider>());back.transform.position=new Vector3(0,0,8);back.transform.localScale=new Vector3(40,25,1);
            backdropMaterial=new Material(Shader.Find("iTetris/AuroraBackground"));backdropMaterial.SetTexture("_MainTex",Resources.Load<Texture2D>("Art/AuroraLake"));back.GetComponent<Renderer>().sharedMaterial=backdropMaterial;
            var board=new GameObject("Board frame");board.transform.position=new Vector3(0,-.3f,0);
            Quad("Board glass",new Vector3(0,-.3f,1),new Vector2(10.1f,20.1f),new Color(.008f,.025f,.04f,.86f));
            CrystalView.Outline(board.transform,10.35f,20.35f,.032f,new Color(.47f,1.5f,1.65f),.3f);
            CrystalView.Outline(board.transform,10.65f,20.65f,.014f,new Color(.12f,.44f,.54f),.5f);
            for(int x=0;x<=10;x++)CrystalView.Line(board.transform,new Vector3(x-5,-10,.6f),new Vector3(x-5,10,.6f),.009f,new Color(.14f,.23f,.30f,.65f));
            for(int y=0;y<=20;y++)CrystalView.Line(board.transform,new Vector3(-5,y-10,.6f),new Vector3(5,y-10,.6f),.009f,new Color(.14f,.23f,.30f,.65f));
            crystals=new CrystalView(new GameObject("Crystal pieces").transform);
            for(int x=0;x<10;x++)for(int y=0;y<20;y++) {settled[x,y]=crystals.Block(PieceKind.I,BoardPosition(x,y));settled[x,y].SetActive(false);}
            for(int i=0;i<4;i++) {active[i]=crystals.Block(PieceKind.I,Vector3.zero);active[i].SetActive(false);ghosts[i]=crystals.Ghost(PieceKind.I,Vector3.zero);ghosts[i].SetActive(false);}
            for(int i=0;i<16;i++) {previews[i]=crystals.Block(PieceKind.T,Vector3.zero,.7f);previews[i].SetActive(false);}
            var rng=new System.Random(12);
            for(int i=0;i<45;i++)
            {
                var s=Quad("Atmospheric mote",new Vector3((float)rng.NextDouble()*38-19,(float)rng.NextDouble()*24-12,4),Vector2.one*.024f,new Color(.18f,.48f,.58f,.5f));stars.Add(s.transform);
            }
        }
        static GameObject Quad(string name,Vector3 pos,Vector2 size,Color c)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name=name;Destroy(g.GetComponent<Collider>());g.transform.position=pos;g.transform.localScale=new Vector3(size.x,size.y,1);g.GetComponent<Renderer>().sharedMaterial=CrystalView.Unlit(c);return g;
        }
        void WorldOutline(Vector3 pos,float w,float h)
        {var p=new GameObject("Panel light rim");p.transform.position=pos;CrystalView.Outline(p.transform,w,h,.017f,new Color(.21f,.38f,.46f));}
        void BuildInterface()
        {
            canvas=new GameObject("Interface",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var root=canvas.transform;
            Label(root,"i T e t r i s",new Vector2(0,461),new Vector2(400,48),34,Pale);
            Label(root,"F I N D   Y O U R   F L O W",new Vector2(0,422),new Vector2(400,22),10,Muted);
            var left=Panel(root,new Vector2(-412,244),new Vector2(258,150));
            Label(left,"S C O R E",new Vector2(0,40),new Vector2(210,25),15,Pale);
            scoreText=Label(left,"000000",new Vector2(0,-15),new Vector2(230,70),43,Pale);
            left=Panel(root,new Vector2(-412,64),new Vector2(258,145));
            Label(left,"L E V E L",new Vector2(0,35),new Vector2(210,25),15,Pale);
            levelText=Label(left,"01",new Vector2(0,-16),new Vector2(200,64),44,Pale);
            linesText=Label(root,"LINES  000",new Vector2(-412,-63),new Vector2(260,30),13,Muted);
            bestText=Label(root,"BEST  "+best.ToString("D6"),new Vector2(-412,-104),new Vector2(280,30),13,Muted);
            Label(root,"S M A L L   M O V E S\nB R I G H T E R   D A Y S",new Vector2(-412,-190),new Vector2(285,64),10,Muted);
            modeText=Label(root,"MARATHON",new Vector2(-412,-270),new Vector2(260,32),12,Teal);
            Quad("Next glass",new Vector3(10.3f,3.95f,2.6f),new Vector2(6.45f,10.625f),new Color(.018f,.04f,.07f,.83f));
            WorldOutline(new Vector3(10.3f,3.95f,2.1f),6.45f,10.625f);
            var next=Panel(root,new Vector2(412,158),new Vector2(258,425),new Color(0,0,0,0));
            Label(next,"N E X T",new Vector2(0,178),new Vector2(220,28),18,Pale);
            for(int i=0;i<3;i++){Panel(next,new Vector2(0,92-i*120),new Vector2(214,102),new Color(0,0,0,0));Quad("Preview glass",new Vector3(10.3f,6.25f-i*3,2.3f),new Vector2(5.35f,2.55f),new Color(.025f,.05f,.08f,.4f));}
            Quad("Hold glass",new Vector3(10.3f,-4.425f,2.6f),new Vector2(6.45f,5f),new Color(.018f,.04f,.07f,.83f));
            WorldOutline(new Vector3(10.3f,-4.425f,2.1f),6.45f,5f);
            var held=Panel(root,new Vector2(412,-177),new Vector2(258,200),new Color(0,0,0,0));
            holdText=Label(held,"H O L D",new Vector2(0,65),new Vector2(220,28),18,Pale);
            Label(held,"C  /  SHIFT",new Vector2(0,-78),new Vector2(210,22),10,Muted);
            Label(root,"C L E A R\nF O C U S\nR E P E A T",new Vector2(412,-357),new Vector2(200,80),10,Muted);
            ButtonAt(root,"II",new Vector2(716,453),new Vector2(48,48),()=>TogglePause());
            var sound=ButtonAt(root,"",new Vector2(-716,453),new Vector2(80,36),()=>{audioWorld.Toggle();UpdateSound();});soundText=sound.GetComponentInChildren<Text>();
            ButtonAt(root,"?",new Vector2(716,-445),new Vector2(44,36),()=>ToggleHelp());
            Label(root,"← →  MOVE     ↑ / X  ROTATE     Z  REVERSE     ↓  SOFT DROP     SPACE  DROP",new Vector2(0,-469),new Vector2(1150,24),11,Muted);
            toastText=Label(root,"",new Vector2(0,-426),new Vector2(680,28),17,Teal);
            overlay=UIObject("Menu overlay",root,new Vector2(0,-12),new Vector2(405,590));
            var image=overlay.gameObject.AddComponent<Image>();image.sprite=RoundedSprite();image.type=Image.Type.Sliced;image.color=new Color(.018f,.04f,.07f,.97f);
            var outline=overlay.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.21f,.59f,.65f,.5f);outline.effectDistance=Vector2.one;
            overlayTitle=Label(overlay,"",new Vector2(0,170),new Vector2(365,80),43,Pale);
            overlaySubtitle=Label(overlay,"",new Vector2(0,103),new Vector2(360,70),13,Muted);
            primaryButton=ButtonAt(overlay,"PLAY",new Vector2(0,15),new Vector2(275,58),()=>PrimaryAction(),true);
            secondaryButton=ButtonAt(overlay,"NEW GAME",new Vector2(0,-61),new Vector2(275,46),()=>StartGame());
            ButtonAt(overlay,"MARATHON  /  ZEN",new Vector2(0,-135),new Vector2(275,40),()=>{zen=!zen;modeText.text=zen?"ZEN · NO GRAVITY":"MARATHON";RefreshMenuMode();});
            ButtonAt(overlay,"QUIT",new Vector2(0,-182),new Vector2(100,30),()=>Application.Quit());
            Label(overlay,"SPACE TO PLAY  ·  ESC TO PAUSE",new Vector2(0,-224),new Vector2(370,26),10,Muted);
            helpPanel=Panel(root,Vector2.zero,new Vector2(750,660),new Color(.015f,.035f,.06f,.99f)).gameObject;
            Label(helpPanel.transform,"HOW TO PLAY",new Vector2(0,255),new Vector2(650,60),32,Pale);
            Label(helpPanel.transform,"Fill a horizontal row to clear it.\nKeep the stack below the top of the board.\n\n← / →     Move\n↑ / X     Rotate clockwise\nZ     Rotate counterclockwise\n↓     Soft drop   ·   SPACE     Hard drop\nC / SHIFT     Hold a piece\nESC / P     Pause   ·   M     Sound\n\nMARATHON: speed rises every 10 lines.\nZEN: place pieces at your own pace.\n\nFour lines at once, combos and T-spins earn bonuses.",new Vector2(0,-5),new Vector2(670,460),19,Pale);
            ButtonAt(helpPanel.transform,"GOT IT",new Vector2(0,-265),new Vector2(240,48),()=>ToggleHelp(),true);helpPanel.SetActive(false);
        }
        static Sprite rounded;
        static Sprite RoundedSprite()
        {
            if(rounded!=null)return rounded;
            const int n=64;var t=new Texture2D(n,n,TextureFormat.RGBA32,false);t.filterMode=FilterMode.Bilinear;
            var colors=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++) {float dx=Mathf.Max(12-x,x-51),dy=Mathf.Max(12-y,y-51);float d=Mathf.Sqrt(Mathf.Max(0,dx)*Mathf.Max(0,dx)+Mathf.Max(0,dy)*Mathf.Max(0,dy));colors[y*n+x]=new Color(1,1,1,1-Mathf.Clamp01(d-11));}
            t.SetPixels(colors);t.Apply();rounded=Sprite.Create(t,new Rect(0,0,n,n),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(15,15,15,15));return rounded;
        }
        static RectTransform UIObject(string name,Transform parent,Vector2 pos,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;
        }
        RectTransform Panel(Transform parent,Vector2 pos,Vector2 size,Color? color=null)
        {
            var r=UIObject("Glass panel",parent,pos,size);var im=r.gameObject.AddComponent<Image>();im.sprite=RoundedSprite();im.type=Image.Type.Sliced;im.color=color??new Color(.02f,.04f,.065f,.83f);
            if(!color.HasValue||color.Value.a>0){var o=r.gameObject.AddComponent<Outline>();o.effectDistance=Vector2.one;o.effectColor=new Color(.26f,.48f,.59f,.65f);}
            return r;
        }
        Text Label(Transform parent,string value,Vector2 pos,Vector2 size,int point,Color color)
        {
            var r=UIObject(value,parent,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=point;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;return t;
        }
        Button ButtonAt(Transform parent,string text,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action,bool accent=false)
        {
            var r=UIObject(text,parent,pos,size);var im=r.gameObject.AddComponent<Image>();im.sprite=RoundedSprite();im.type=Image.Type.Sliced;im.color=accent?new Color(.09f,.42f,.48f,.96f):new Color(.045f,.10f,.15f,.96f);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;var c=b.colors;c.highlightedColor=new Color(.64f,.96f,1);c.pressedColor=new Color(.3f,.65f,.7f);b.colors=c;b.onClick.AddListener(action);
            var nav=b.navigation;nav.mode=Navigation.Mode.None;b.navigation=nav;
            Label(r,text,Vector2.zero,size-Vector2.one*8,accent?18:13,Pale);return b;
        }
        void UpdateSound(){if(soundText!=null)soundText.text=audioWorld.Muted?"SOUND OFF":"SOUND ON";}
        void ShowMenu(string title,string subtitle,string button,bool secondary)
        {
            overlay.gameObject.SetActive(true);overlayTitle.text=title;overlaySubtitle.text=subtitle;primaryButton.GetComponentInChildren<Text>().text=button;secondaryButton.gameObject.SetActive(secondary);
        }
        void RefreshMenuMode(){if(!started)overlaySubtitle.text=zen?"ZEN\nPLACE AT YOUR OWN PACE":"MARATHON\nFIND YOUR FLOW";}
        void PrimaryAction(){if(started&&!game.GameOver) {paused=false;overlay.gameObject.SetActive(false);ResetInput();}else StartGame();}
        void StartGame()
        {
            started=true;paused=false;clearing=false;endedPending=false;elapsed=0;toastTimer=0;toastText.text="";
            if(clearRoutine!=null){StopCoroutine(clearRoutine);clearRoutine=null;}foreach(var flash in clearFlashes)if(flash!=null)Destroy(flash);clearFlashes.Clear();
            for(int x=0;x<10;x++)for(int y=0;y<20;y++)settled[x,y].transform.localScale=Vector3.one;
            game.Restart();overlay.gameObject.SetActive(false);helpPanel.SetActive(false);dirty=true;ResetInput();
        }
        void ResetInput(){horizontalTimer=softTimer=0;lastDirection=0;}
        void TogglePause()
        {
            if(!started||game.GameOver||helpPanel.activeSelf)return;
            paused=!paused;ResetInput();if(paused)ShowMenu("PAUSED","TAKE A BREATH","RESUME",true);else overlay.gameObject.SetActive(false);
        }
        bool helpWasPaused;
        void ToggleHelp()
        {
            if(helpPanel.activeSelf){helpPanel.SetActive(false);paused=helpWasPaused;ResetInput();}
            else{helpWasPaused=paused;paused=true;helpPanel.SetActive(true);}
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.F12))ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.persistentDataPath,"Gameplay.png"));
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/1000f);canvas.scaleFactor=scale;
            cameraMain.orthographicSize=Screen.height/(scale*80);
            if(Input.GetKeyDown(KeyCode.M)){audioWorld.Toggle();UpdateSound();}
            if(Input.GetKeyDown(KeyCode.F11))Screen.fullScreen=!Screen.fullScreen;
            if(helpPanel.activeSelf){if(Input.GetKeyDown(KeyCode.Escape))ToggleHelp();}
            else if(!started||game.GameOver)
            {if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return))StartGame();}
            else
            {
                if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P))TogglePause();
                if(!paused&&!clearing&&!game.GameOver)
                {
                    elapsed+=Time.deltaTime;HandleInput();
                    if(!zen&&!clearing&&!game.GameOver)game.Tick(Time.deltaTime);
                }
            }
            if(dirty&&!clearing&&started){DrawGame();dirty=false;}
            if(endedPending&&!clearing){endedPending=false;Finish();}
            if(toastTimer>0){toastTimer-=Time.deltaTime;if(toastTimer<=0)toastText.text="";}
            UpdateAtmosphere();UpdateSparks();
        }
        void HandleInput()
        {
            int direction=Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.A)?-1:Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.D)?1:0;
            if(direction!=lastDirection){horizontalTimer=.16f;if(direction!=0&&game.Move(direction))audioWorld.Play("move");lastDirection=direction;}
            else if(direction!=0){horizontalTimer-=Time.deltaTime;if(horizontalTimer<=0){horizontalTimer+=.045f;if(game.Move(direction))audioWorld.Play("move");}}
            if(Input.GetKeyDown(KeyCode.UpArrow)||Input.GetKeyDown(KeyCode.X)||Input.GetKeyDown(KeyCode.W)){if(game.Rotate(1))audioWorld.Play("rotate");}
            if(Input.GetKeyDown(KeyCode.Z)){if(game.Rotate(-1))audioWorld.Play("rotate");}
            if(Input.GetKeyDown(KeyCode.C)||Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift)){if(game.Hold())audioWorld.Play("hold");}
            if(Input.GetKeyDown(KeyCode.Space)){game.HardDrop();audioWorld.Play("drop");shake=.12f;return;}
            if(Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.S))
            {softTimer-=Time.deltaTime;if(softTimer<=0){softTimer=.035f;game.SoftDrop();}}
            else softTimer=0;
        }
        void DrawGame()
        {
            for(int x=0;x<10;x++)for(int y=0;y<20;y++)
            {
                int k=game.Board[x,y];var block=settled[x,y];block.SetActive(k!=0);
                if(k!=0&&displayed[x,y]!=k)crystals.SetKind(block,(PieceKind)(k-1));displayed[x,y]=k;
            }
            var cells=game.ActiveCells();var ghost=game.ActiveCells(game.GhostY());
            for(int i=0;i<4;i++)
            {
                bool visible=!game.GameOver&&cells[i].Y<20;active[i].SetActive(visible);ghosts[i].SetActive(!game.GameOver&&ghost[i].Y<20&&game.GhostY()!=game.Y);
                active[i].transform.position=BoardPosition(cells[i].X,cells[i].Y);crystals.SetKind(active[i],game.Active);
                ghosts[i].transform.position=BoardPosition(ghost[i].X,ghost[i].Y);
            }
            var next=game.Next(3);for(int i=0;i<3;i++)DrawPreview(next[i],i*4,new Vector3(10.3f,6.25f-i*3,0),.72f);
            for(int i=12;i<16;i++)previews[i].SetActive(game.Held.HasValue);
            if(game.Held.HasValue)DrawPreview(game.Held.Value,12,new Vector3(10.3f,-4.65f,0),.78f);
            holdText.color=game.HoldUsed?Muted:Pale;
            scoreText.text=game.Score.ToString("D6");levelText.text=game.Level.ToString("D2");linesText.text="LINES  "+game.Lines.ToString("D3");
            if(game.Score>best){best=game.Score;PlayerPrefs.SetInt("BestScore",best);}bestText.text="BEST  "+best.ToString("D6");
        }
        void DrawPreview(PieceKind kind,int offset,Vector3 center,float scale)
        {
            var shape=TetrisGame.Shape(kind);float mx=0,my=0;foreach(var c in shape){mx+=c.X/4f;my+=c.Y/4f;}
            for(int i=0;i<4;i++){var b=previews[offset+i];b.SetActive(true);b.transform.position=center+new Vector3((shape[i].X-mx)*scale,(shape[i].Y-my)*scale,-.6f);b.transform.localScale=Vector3.one*scale;crystals.SetKind(b,kind);}
        }
        void DrawAttract()
        {
            var rng=new System.Random(41);
            int[] heights={5,4,3,4,2,2,3,4,5,6};
            for(int x=0;x<10;x++)for(int y=0;y<heights[x];y++)
            {if(y>0&&rng.Next(7)==0)continue;var k=(PieceKind)rng.Next(7);crystals.SetKind(settled[x,y],k);settled[x,y].SetActive(true);displayed[x,y]=(int)k+1;}
            DrawPreview(PieceKind.T,0,new Vector3(10.3f,6.25f,0),.72f);DrawPreview(PieceKind.L,4,new Vector3(10.3f,3.25f,0),.72f);DrawPreview(PieceKind.Z,8,new Vector3(10.3f,.25f,0),.72f);DrawPreview(PieceKind.O,12,new Vector3(10.3f,-4.65f,0),.78f);
        }
        void OnLock(LockResult result)
        {
            foreach(var c in result.Cells)Burst(BoardPosition(c.X,c.Y),CrystalView.Palette[(int)result.Kind],3);
            if(result.ClearedRows.Length>0)
            {
                clearing=true;audioWorld.Play("clear");shake=.22f;
                string message=result.PerfectClear?"PERFECT CLEAR":result.TSpin?"T-SPIN":result.ClearedRows.Length==4?"TETRIS":new[]{"","SINGLE","DOUBLE","TRIPLE"}[result.ClearedRows.Length];
                if(result.BackToBack)message="BACK TO BACK · "+message;
                if(result.Combo>0)message+=" · COMBO "+result.Combo;
                toastText.text=message+"   +"+result.ScoreGained;toastTimer=2.4f;
                clearRoutine=StartCoroutine(ClearAnimation(result));
            }
        }
        IEnumerator ClearAnimation(LockResult result)
        {
            for(int i=0;i<4;i++){active[i].SetActive(false);ghosts[i].SetActive(false);}
            foreach(var c in result.Cells)if(c.Y<20){settled[c.X,c.Y].SetActive(true);crystals.SetKind(settled[c.X,c.Y],result.Kind);displayed[c.X,c.Y]=(int)result.Kind+1;}
            var flashes=clearFlashes;
            foreach(int row in result.ClearedRows)if(row<20)
            {
                flashes.Add(Quad("Line clear light",new Vector3(0,row-9.8f,-.6f),new Vector2(10,.085f),new Color(.8f,2,2)));
                for(int x=0;x<10;x++)Burst(BoardPosition(x,row),new Color(.4f,.9f,1),6);
            }
            float t=0;while(t<.23f)
            {
                if(!paused){t+=Time.deltaTime;foreach(int row in result.ClearedRows)if(row<20)for(int x=0;x<10;x++)settled[x,row].transform.localScale=Vector3.one*Mathf.Max(.01f,1-t/.23f);}
                yield return null;
            }
            foreach(var f in flashes)Destroy(f);flashes.Clear();
            foreach(int row in result.ClearedRows)if(row<20)for(int x=0;x<10;x++)settled[x,row].transform.localScale=Vector3.one;
            clearing=false;dirty=true;clearRoutine=null;
        }
        void Burst(Vector3 position,Color c,int count)
        {
            for(int i=0;i<count;i++)
            {
                var s=Quad("Crystal spark",position+new Vector3(0,0,-.5f),Vector2.one*UnityEngine.Random.Range(.025f,.075f),c*1.6f);
                float life=UnityEngine.Random.Range(.25f,.65f);sparks.Add(new Spark{T=s.transform,Velocity=new Vector3(UnityEngine.Random.Range(-3,3),UnityEngine.Random.Range(1,5),0),Life=life,Max=life});
            }
        }
        void UpdateSparks()
        {
            for(int i=sparks.Count-1;i>=0;i--)
            {
                var s=sparks[i];s.Life-=Time.deltaTime;
                if(s.Life<=0){Destroy(s.T.gameObject);sparks.RemoveAt(i);continue;}
                s.Velocity+=Vector3.down*Time.deltaTime*7;s.T.position+=s.Velocity*Time.deltaTime;s.T.localScale=Vector3.one*.065f*(s.Life/s.Max);sparks[i]=s;
            }
        }
        void UpdateAtmosphere()
        {
            float dt=Time.deltaTime;
            for(int i=0;i<stars.Count;i++){var t=stars[i];t.position+=new Vector3(Mathf.Sin(Time.time*.15f+i)*.015f,.065f,0)*dt;if(t.position.y>12.5f)t.position+=Vector3.down*25;}
            if(shake>0){shake-=dt;cameraMain.transform.position=new Vector3(0,Mathf.Sin(Time.time*70)*shake*.22f,-30);}else cameraMain.transform.position=new Vector3(0,0,-30);
        }
        void Finish()
        {
            audioWorld.Play("end");PlayerPrefs.Save();
            ShowMenu("GAME OVER","SCORE  "+game.Score.ToString("N0")+"\nLINES  "+game.Lines+"   ·   LEVEL  "+game.Level,"PLAY AGAIN",false);
        }
        void OnApplicationFocus(bool focus){if(!focus&&started&&!paused&&!game.GameOver)TogglePause();}
        void OnApplicationQuit(){if(diagnosticBest.HasValue)PlayerPrefs.SetInt("BestScore",diagnosticBest.Value);PlayerPrefs.Save();}
        static void Check(bool condition,string name){if(!condition)throw new Exception("SMOKE FAILED: "+name);Debug.Log("SMOKE PASS: "+name);}
        IEnumerator SmokeTest()
        {
            yield return new WaitForSecondsRealtime(.5f);
            StartGame();zen=true;
            Check(started&&!overlay.gameObject.activeSelf,"start menu closes");
            Check(game.Hold(),"hold input action");DrawGame();Check(previews[12].activeSelf,"held piece is rendered");
            Check(game.Move(-1)&&game.Rotate(1),"move and rotate actions");game.HardDrop();DrawGame();
            Check(game.Score>0,"drop updates score");
            game.Restart();Array.Clear(game.Board,0,game.Board.Length);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            foreach(var pair in new System.Collections.Generic.Dictionary<string,object>{{"Active",PieceKind.O},{"X",3},{"Y",-1},{"Rotation",0}})
                typeof(TetrisGame).GetField("<"+pair.Key+">k__BackingField",flags).SetValue(game,pair.Value);
            for(int y=0;y<2;y++)for(int x=0;x<10;x++)if(x!=4&&x!=5)game.Board[x,y]=1;
            DrawGame();game.HardDrop();Check(clearing,"line clear animation starts");
            yield return new WaitForSecondsRealtime(.4f);
            Check(!clearing&&game.Lines==2&&game.Score==2300,"line clear finishes with correct score");
            TogglePause();Check(paused&&overlay.gameObject.activeSelf,"pause menu opens");TogglePause();Check(!paused&&!overlay.gameObject.activeSelf,"resume closes menu");
            ToggleHelp();Check(helpPanel.activeSelf&&paused,"help pauses game");ToggleHelp();Check(!helpPanel.activeSelf&&!paused,"help restores play");
            bool wasMuted=audioWorld.Muted;audioWorld.Toggle();Check(audioWorld.Muted!=wasMuted,"sound toggle");audioWorld.Toggle();
            for(int y=17;y<20;y++)for(int x=0;x<10;x++)game.Board[x,y]=1;game.Hold();
            yield return null;Check(game.GameOver&&overlay.gameObject.activeSelf&&overlayTitle.text=="GAME OVER","game over menu");
            StartGame();Check(!game.GameOver&&game.Score==0&&game.Held==null,"restart resets game");
            Debug.Log("ALL RUNTIME SMOKE TESTS PASSED");Application.Quit();
        }
        IEnumerator VisualCapture()
        {
            yield return new WaitForSecondsRealtime(2);overlay.gameObject.SetActive(false);started=true;paused=true;
            Array.Clear(game.Board,0,game.Board.Length);var rng=new System.Random(41);int[] h={7,6,5,4,3,4,5,6,7,8};
            for(int x=0;x<10;x++)for(int y=0;y<h[x];y++)if(y==0||rng.Next(9)!=0)game.Board[x,y]=rng.Next(1,8);
            game.Hold();dirty=true;
            yield return new WaitForSecondsRealtime(.5f);var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--capture-path");
            string path=index>=0&&index+1<args.Length?args[index+1]:System.IO.Path.Combine(Application.persistentDataPath,"Gameplay.png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("VISUAL_CAPTURE_REQUESTED");yield return new WaitForSecondsRealtime(2);
            if(Array.IndexOf(args,"--quit-after-capture")>=0)Application.Quit();
        }
    }
}
