using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using iTetris.Core;

namespace iTetris
{
    public sealed partial class GameController
    {
        const float BreakerScale = 1.63f;
        bool breakerActive, breakerPaused, breakerCaptureMode;
        GameObject breakerWorld, breakerUi, breakerMenu, breakerPaddle, breakerReflection;
        CrystalBreakerRules breaker;
        readonly List<GameObject> breakerObjects = new List<GameObject>();
        readonly List<GameObject> breakerBalls = new List<GameObject>();
        readonly List<GameObject> breakerGiftViews = new List<GameObject>();
        readonly List<int> breakerHP = new List<int>();
        readonly List<BreakerFragment> breakerFragments = new List<BreakerFragment>();
        readonly Sprite[] breakerSprites = new Sprite[8];
        readonly Image[] breakerLives = new Image[3];
        Text breakerStats, breakerLevel, breakerMessage, breakerBestText, breakerCombo, breakerMenuTitle;
        Button breakerPause, breakerResume;
        Font breakerFont;
        int breakerBest, breakerStage = -1;
        Vector3 breakerMouse;
        float breakerComboTime;
        struct BreakerFragment { public GameObject Object; public Vector3 Velocity; public float Life, Scale; }
        bool BreakerAvailable => Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor;
        static Vector3 BreakerPosition(float x, float y) => new Vector3(x * BreakerScale, y * BreakerScale, -.4f);

        void OpenBreaker()
        {
            if (!BreakerAvailable) return;
            hubVisible = false; hubRoot.SetActive(false); tetrisWorld.SetActive(false); tetrisUiRoot.SetActive(false);
            if (breakerWorld == null) BuildBreaker();
            breakerWorld.SetActive(true); breakerUi.SetActive(true); breakerActive = true; breakerPaused = false;
            breakerMouse = Input.mousePosition; ClearBreakerTrails(); DrawBreaker();
        }
        void LoadBreakerSprites()
        {
            var atlas = Resources.Load<Texture2D>("Art/BreakerSprites");
            // Pivots follow the opaque center of each isolated crystal; padding holds the optical glow.
            float sx = atlas.width / 1536f, sy = atlas.height / 1024f;
            for (int i = 0; i < 4; i++)
            {
                float center = i == 0 ? 199.5f : i == 1 ? 197f : i == 2 ? 192f : 188f;
                breakerSprites[i] = Sprite.Create(atlas, new Rect(i * 384 * sx, 512 * sy, 384 * sx, 512 * sy),
                    new Vector2(center / 384f, 225f / 512f), 150 * sx, 0, SpriteMeshType.FullRect);
            }
            Rect[] rects = { new Rect(0,0,480,512), new Rect(480,0,320,512), new Rect(800,0,330,512), new Rect(1130,0,406,512) };
            Vector2[] centers = { new Vector2(240,278), new Vector2(149,276), new Vector2(172,275), new Vector2(232,277) };
            for (int i = 0; i < 4; i++)
            {
                var r = rects[i];
                breakerSprites[4+i] = Sprite.Create(atlas, new Rect(r.x*sx,r.y*sy,r.width*sx,r.height*sy),
                    new Vector2(centers[i].x/r.width,centers[i].y/r.height),150*sx,0,SpriteMeshType.FullRect);
            }
        }
        GameObject BreakerSprite(string name, int sprite, Vector3 position, float scale, int order = 0)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer)); obj.transform.SetParent(breakerWorld.transform, false);
            obj.transform.localPosition = position; obj.transform.localScale = Vector3.one * scale;
            var renderer = obj.GetComponent<SpriteRenderer>(); renderer.sprite = breakerSprites[sprite]; renderer.sortingOrder = order;
            return obj;
        }
        Text BreakerLabel(Transform parent,string value,Vector2 pos,Vector2 size,int point)
        {
            var label = Label(parent,value,pos,size,point,new Color(.64f,.9f,1)); label.font = breakerFont;
            label.horizontalOverflow = HorizontalWrapMode.Overflow; return label;
        }
        void BreakerDiamond(Vector3 center,float size)
        {
            var points = new[] {Vector3.up,Vector3.right*.63f,Vector3.down,Vector3.left*.63f};
            for(int ring=0;ring<2;ring++)for(int i=0;i<4;i++)
                CrystalView.Line(breakerWorld.transform,center+points[i]*size*(ring==0?1:.5f),center+points[(i+1)%4]*size*(ring==0?1:.5f),ring==0?.025f:.012f,new Color(.4f,1.25f,1.6f));
        }
        void BuildBreaker()
        {
            breakerWorld = new GameObject("Crystal Breaker world"); LoadBreakerSprites();
            breakerFont = Font.CreateDynamicFontFromOSFont(new[]{"Baskerville","Times New Roman","Georgia"},32);
            var plate = Quad("Reference aurora landscape",new Vector3(0,0,6),new Vector2(40,25),Color.white,false);
            plate.transform.SetParent(breakerWorld.transform,true);
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetTexture("_BaseMap",Resources.Load<Texture2D>("Art/BreakerLandscape"));plate.GetComponent<Renderer>().sharedMaterial=material;
            var frame = new GameObject("Luminous perimeter");frame.transform.SetParent(breakerWorld.transform,false);
            frame.transform.localPosition=new Vector3(0,-.125f,0);
            var rimColor=new Color(.3f,1.15f,1.5f);
            CrystalView.Line(frame.transform,new Vector3(-19.4f,11.825f,0),new Vector3(-19.4f,-11.825f,0),.026f,rimColor);
            CrystalView.Line(frame.transform,new Vector3(19.4f,11.825f,0),new Vector3(19.4f,-11.825f,0),.026f,rimColor);
            CrystalView.Line(frame.transform,new Vector3(-19.4f,-11.825f,0),new Vector3(19.4f,-11.825f,0),.026f,rimColor);
            CrystalView.Line(frame.transform,new Vector3(-18.35f,11.05f,0),new Vector3(18.35f,11.05f,0),.018f,new Color(.25f,.8f,1));
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)
            {
                var at = new Vector3(x*19.4f,y>0?11.7f:-11.95f,-.1f); BreakerDiamond(at,.39f);
                CrystalView.Line(frame.transform,new Vector3(x*19.4f,y*11.45f,0),new Vector3(x*18.7f,y*10.78f,0),.013f,Teal);
            }
            BreakerDiamond(new Vector3(-4.92f,11.7f,0),.22f);BreakerDiamond(new Vector3(4.92f,11.7f,0),.22f);
            for(int sign=-1;sign<=1;sign+=2)CrystalView.Line(frame.transform,new Vector3(sign*5.25f,11.825f,0),new Vector3(sign*8.4f,11.825f,0),.01f,Muted);
            breakerPaddle=BreakerSprite("Faceted glass paddle",4,BreakerPosition(0,CrystalBreakerRules.PaddleY),1,10);
            breakerReflection=BreakerSprite("Paddle reflection on lake",4,Vector3.zero,1,1);
            breakerReflection.GetComponent<SpriteRenderer>().color=new Color(.45f,.8f,1,.19f);
            breakerUi=UIObject("Crystal Breaker interface",canvas.transform,Vector2.zero,new Vector2(1600,1000)).gameObject;
            var root=breakerUi.transform;
            BreakerLabel(root,"C R Y S T A L   B R E A K E R",new Vector2(0,469),new Vector2(470,46),26);
            BreakerLabel(root,"SCORE",new Vector2(-679,469),new Vector2(100,44),24);
            breakerStats=BreakerLabel(root,"000000",new Vector2(-527,469),new Vector2(190,44),33);breakerStats.alignment=TextAnchor.MiddleLeft;
            breakerLevel=BreakerLabel(root,"LEVEL 01",new Vector2(483,469),new Vector2(142,44),26);
            for(int i=0;i<3;i++)
            {
                var r=UIObject("Life "+(i+1),root,new Vector2(579+i*35,469),new Vector2(40,61));
                var image=r.gameObject.AddComponent<Image>();image.sprite=breakerSprites[5];image.raycastTarget=false;breakerLives[i]=image;
            }
            breakerPause=ButtonAt(root,"II",new Vector2(715,469),new Vector2(47,47),ToggleBreakerPause);
            breakerPause.GetComponent<Image>().color=new Color(.02f,.09f,.14f,.12f);
            breakerPause.GetComponentInChildren<Text>().fontSize=27;
            var circle=new GameObject("Pause circle");circle.transform.SetParent(breakerWorld.transform,false);circle.transform.localPosition=new Vector3(17.875f,11.725f,0);
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;CrystalView.Line(circle.transform,new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.59f,new Vector3(Mathf.Cos(b),Mathf.Sin(b),0)*.59f,.018f,Teal);}
            breakerMessage=BreakerLabel(root,"",new Vector2(0,-444),new Vector2(1100,38),18);
            breakerCombo=BreakerLabel(root,"",Vector2.zero,new Vector2(280,45),25);
            breakerMenu=Panel(root,Vector2.zero,new Vector2(650,430),new Color(.008f,.035f,.055f,.93f)).gameObject;
            breakerMenuTitle=BreakerLabel(breakerMenu.transform,"PAUSED",new Vector2(0,156),new Vector2(550,60),38);
            breakerBestText=BreakerLabel(breakerMenu.transform,"",new Vector2(0,92),new Vector2(550,38),20);
            BreakerLabel(breakerMenu.transform,"MOUSE / ← →  MOVE     SPACE / CLICK  LAUNCH\nGOLD  WIDE PADDLE     VIOLET  MULTIBALL",new Vector2(0,24),new Vector2(610,74),16);
            breakerResume=ButtonAt(breakerMenu.transform,"RESUME",new Vector2(0,-58),new Vector2(380,49),()=>{if(breaker.Over)NewBreaker();else ToggleBreakerPause();},true);
            ButtonAt(breakerMenu.transform,"NEW GAME",new Vector2(-136,-137),new Vector2(245,46),NewBreaker);
            ButtonAt(breakerMenu.transform,"GAME HUB",new Vector2(136,-137),new Vector2(245,46),OpenHub);
            breakerBest=PlayerPrefs.GetInt("BreakerBest",0);NewBreaker();
        }
        void ToggleBreakerPause(){breakerPaused=!breakerPaused;breakerMouse=Input.mousePosition;ClearBreakerTrails();DrawBreaker();}
        void ClearBreakerTrails(){foreach(var ball in breakerBalls)ball.GetComponent<TrailRenderer>().Clear();}
        void NewBreaker()
        {
            foreach(var fragment in breakerFragments)Destroy(fragment.Object);breakerFragments.Clear();
            breakerComboTime=0;breaker=new CrystalBreakerRules();breaker.Broken+=BreakerImpact;
            breakerPaused=false;ClearBreakerTrails();DrawBreaker();
        }
        void BreakerImpact(float x,float y,int color)
        {
            audioWorld.Play("clear");shake=.1f;
            var position=BreakerPosition(x,y);
            var flash=BreakerSprite("Crystal impact bloom",7,position,.9f,20);
            breakerFragments.Add(new BreakerFragment{Object=flash,Life=.32f,Scale=.9f});
            for(int i=0;i<10;i++)
            {
                float scale=UnityEngine.Random.Range(.25f,.6f);var shard=BreakerSprite("Refracted glass shard",6,position,scale,20);
                shard.transform.localRotation=Quaternion.Euler(0,0,UnityEngine.Random.Range(0,360));
                breakerFragments.Add(new BreakerFragment{Object=shard,Velocity=new Vector3(UnityEngine.Random.Range(-6f,6f),UnityEngine.Random.Range(-5f,7f),0),Life=.7f,Scale=scale});
            }
            if(breaker.Combo>=2){breakerComboTime=1.2f;breakerCombo.text="COMBO ×"+breaker.Combo;
                breakerCombo.rectTransform.anchoredPosition=new Vector2(Mathf.Clamp(position.x*40+130,-620,620),Mathf.Clamp(position.y*40-67,-390,390));}
        }
        void UpdateBreakerFragments()
        {
            breakerComboTime-=Time.deltaTime;
            for(int i=breakerFragments.Count-1;i>=0;i--)
            {
                var fragment=breakerFragments[i];fragment.Life-=Time.deltaTime;
                if(fragment.Life<=0){Destroy(fragment.Object);breakerFragments.RemoveAt(i);continue;}
                fragment.Object.transform.localPosition+=fragment.Velocity*Time.deltaTime;
                fragment.Object.transform.Rotate(0,0,130*Time.deltaTime);
                var renderer=fragment.Object.GetComponent<SpriteRenderer>();var c=renderer.color;c.a=Mathf.Min(1,fragment.Life*3);renderer.color=c;
                breakerFragments[i]=fragment;
            }
        }
        void UpdateBreaker()
        {
            UpdateAtmosphere();
            if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P))ToggleBreakerPause();
            if(!breakerPaused&&!breakerCaptureMode&&!breaker.Over)
            {
                UpdateBreakerFragments();var mouse=Input.mousePosition;
                if(mouse!=breakerMouse){breaker.SetPaddle(cameraMain.ScreenToWorldPoint(mouse).x/BreakerScale);breakerMouse=mouse;}
                float direction=(Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.A)?1:0);
                if(direction!=0)breaker.SetPaddle(breaker.Paddle+direction*17*Time.deltaTime);
                if(Input.GetKeyDown(KeyCode.Space)||(Input.GetMouseButtonDown(0)&&!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
                {if(breaker.Waiting)audioWorld.Play("drop");breaker.Launch();}
                breaker.Tick(Time.deltaTime);
                if(breaker.Score>breakerBest)
                {
                    breakerBest=breaker.Score;
                    if(!diagnosticBest.HasValue){PlayerPrefs.SetInt("BreakerBest",breakerBest);PlayerPrefs.Save();}
                }
            }
            DrawBreaker();
        }
        void DrawBreaker()
        {
            if(breaker==null)return;
            bool changed=breakerObjects.Count!=breaker.Bricks.Count||breakerStage!=breaker.Stage;
            if(!changed)for(int i=0;i<breakerHP.Count;i++)if(breakerHP[i]!=breaker.Bricks[i].HP){changed=true;break;}
            if(changed)
            {
                foreach(var obj in breakerObjects)Destroy(obj);breakerObjects.Clear();breakerHP.Clear();
                foreach(var brick in breaker.Bricks)
                {
                    var obj=BreakerSprite("Faceted crystal brick",brick.Color,BreakerPosition(brick.X,brick.Y),1.49f,5);
                    if(brick.HP>1)obj.GetComponent<SpriteRenderer>().color=new Color(.93f,.97f,1);
                    breakerObjects.Add(obj);breakerHP.Add(brick.HP);
                }
                breakerStage=breaker.Stage;
            }
            float paddleScale=breaker.HalfWidth*2*BreakerScale/(428f/150);
            breakerPaddle.transform.localPosition=BreakerPosition(breaker.Paddle,CrystalBreakerRules.PaddleY);
            breakerPaddle.transform.localScale=new Vector3(paddleScale,3.2f,1);
            breakerReflection.transform.localPosition=breakerPaddle.transform.localPosition+new Vector3(0,-.85f,.2f);
            breakerReflection.transform.localScale=new Vector3(paddleScale,-2.3f,1);
            while(breakerBalls.Count<breaker.Balls.Count)
            {
                var ball=BreakerSprite("Luminous pearl ball",5,Vector3.zero,.94f,15);
                var trail=ball.AddComponent<TrailRenderer>();trail.time=.45f;trail.startWidth=.7f;trail.endWidth=.01f;
                trail.minVertexDistance=.025f;trail.sharedMaterial=Resources.Load<Material>("Materials/BreakerTrail");
                trail.startColor=new Color(.65f,1,1,.9f);trail.endColor=new Color(.18f,.78f,1,0);trail.sortingOrder=12;
                breakerBalls.Add(ball);
            }
            for(int i=0;i<breakerBalls.Count;i++)
            {
                var ball=breakerBalls[i];ball.SetActive(i<breaker.Balls.Count);
                if(i<breaker.Balls.Count){ball.transform.localPosition=BreakerPosition(breaker.Balls[i].X,breaker.Balls[i].Y);ball.GetComponent<TrailRenderer>().emitting=!breaker.Waiting&&!breakerPaused;if(breaker.Waiting)ball.GetComponent<TrailRenderer>().Clear();}
            }
            DrawBreakerGifts();breakerStats.text=breaker.Score.ToString("D6");breakerLevel.text="LEVEL "+breaker.Stage.ToString("D2");
            for(int i=0;i<3;i++)breakerLives[i].color=i<breaker.Lives?Color.white:new Color(.25f,.45f,.55f,.25f);
            breakerBestText.text="BEST  "+breakerBest.ToString("D6");
            breakerMessage.text=breaker.Over?(breaker.Won?"ALL CRYSTALS CLEARED · YOU SHINE":"GAME OVER · PRESS PAUSE TO START AGAIN"):
                breakerPaused?"":breaker.Waiting?"CLICK OR PRESS SPACE TO LAUNCH":breaker.WideTime>0?"WIDE PADDLE · "+Mathf.CeilToInt(breaker.WideTime)+"s":"";
            breakerPause.GetComponentInChildren<Text>().text=breakerPaused?"▶":"II";
            breakerMenuTitle.text=breaker.Over?(breaker.Won?"YOU SHINE":"GAME OVER"):"PAUSED";
            breakerResume.GetComponentInChildren<Text>().text=breaker.Over?"PLAY AGAIN":"RESUME";
            breakerMenu.SetActive(breakerPaused||breaker.Over);breakerCombo.gameObject.SetActive(breakerComboTime>0&&!breakerPaused);
        }
        void DrawBreakerGifts()
        {
            while(breakerGiftViews.Count<breaker.Gifts.Count)breakerGiftViews.Add(BreakerSprite("Falling power crystal",6,Vector3.zero,.8f,10));
            for(int i=0;i<breakerGiftViews.Count;i++)
            {
                var obj=breakerGiftViews[i];obj.SetActive(i<breaker.Gifts.Count);
                if(i<breaker.Gifts.Count){var gift=breaker.Gifts[i];obj.transform.localPosition=BreakerPosition(gift.X,gift.Y);
                    obj.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.time*2)*15);
                    obj.GetComponent<SpriteRenderer>().color=gift.Wide?new Color(1,.84f,.4f):new Color(.8f,.5f,1);}
            }
        }
        #if !UNITY_WEBGL || UNITY_EDITOR
        IEnumerator BreakerCapture()
        {
            yield return new WaitForSecondsRealtime(1);OpenBreaker();breakerCaptureMode=true;breaker.Launch();
            var ball=breaker.Balls[0];ball.X=5.5f;ball.Y=1.5f;ball.VX=-4;ball.VY=-7;DrawBreaker();yield return null;ClearBreakerTrails();
            // A short simulated flight gives the live ball its real TrailRenderer history.
            for(int i=0;i<30;i++){ball.X=5.5f-i*.1f;ball.Y=1.5f-i*.105f;DrawBreaker();yield return null;}
            breakerPaused=false;DrawBreaker();
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--capture-path");
            ScreenCapture.CaptureScreenshot(at>=0&&at+1<args.Length?args[at+1]:System.IO.Path.Combine(Application.persistentDataPath,"CrystalBreaker.png"));
            breakerCaptureMode=true;yield return new WaitForSecondsRealtime(2);Application.Quit();
        }
        #endif
    }
}
