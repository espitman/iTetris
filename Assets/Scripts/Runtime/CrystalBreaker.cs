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
        readonly Color[] breakerShardColors = {new Color(.12f,.9f,1),new Color(.16f,.36f,1),new Color(.63f,.24f,1),new Color(1,.76f,.27f)};
        Text breakerStageAnnouncement;
        Image breakerSeal;
        readonly Sprite[] breakerHudSprites=new Sprite[9];
        readonly List<GameObject> breakerStageMotes=new List<GameObject>();
        readonly Sprite[] breakerSprites = new Sprite[8];
        readonly Sprite[] breakerDurabilitySprites = new Sprite[8];
        readonly Image[] breakerLives = new Image[3];
        Text breakerStats, breakerLevel, breakerMessage, breakerBestText, breakerCombo, breakerMenuTitle;
        Button breakerPause, breakerResume;
        Font breakerFont;
        int breakerBest, breakerStage = -1;
        Vector3 breakerMouse;
        float breakerComboTime, breakerRevealTime;
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
            var hud=Resources.Load<Texture2D>("Art/BreakerHud");
            Rect[] hudRects={new Rect(15,1007,400,74),new Rect(433,1001,387,85),new Rect(951,932,187,229),new Rect(138,489,139,298),new Rect(510,507,232,242),new Rect(889,496,308,263),new Rect(71,66,277,289),new Rect(429,62,394,307),new Rect(972,116,142,196)};
            for(int i=0;i<9;i++)breakerHudSprites[i]=Sprite.Create(hud,hudRects[i],new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            var armor = Resources.Load<Texture2D>("Art/BreakerDurability");
            float cellWidth = armor.width / 4f, cellHeight = armor.height / 2f;
            // Calibrated to the core of each generated sprite, excluding the broken corner debris.
            float[] armorCenters = {228,225,223,222};
            for (int state = 0; state < 2; state++)
                for (int color = 0; color < 4; color++)
                    breakerDurabilitySprites[state*4+color] = Sprite.Create(armor,
                        new Rect(color*cellWidth,(1-state)*cellHeight,cellWidth,cellHeight),
                        new Vector2(armorCenters[color]/cellWidth,(state==0?cellHeight-260:cellHeight-169)/cellHeight),
                        150,0,SpriteMeshType.FullRect);
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
            HudImage(root,0,new Vector2(0,464),new Vector2(430,80));
            HudImage(root,1,new Vector2(-527,464),new Vector2(240,53));
            breakerStats=BreakerLabel(root,"000000",new Vector2(-527,464),new Vector2(190,42),30);
            HudImage(root,2,new Vector2(483,464),new Vector2(55,67));
            breakerLevel=BreakerLabel(root,"1",new Vector2(483,464),new Vector2(45,40),26);
            for(int i=0;i<3;i++)breakerLives[i]=HudImage(root,3,new Vector2(557+i*44,464),new Vector2(25,53));
            breakerPause=ButtonAt(root,"",new Vector2(715,464),new Vector2(55,58),ToggleBreakerPause);
            breakerPause.GetComponent<Image>().sprite=breakerHudSprites[4];
            breakerPause.GetComponent<Image>().color=Color.white;
            breakerPause.GetComponent<Image>().type=Image.Type.Simple;
            breakerMessage=BreakerLabel(root,"",new Vector2(0,-444),new Vector2(1100,38),18);
            breakerSeal=HudImage(root,7,new Vector2(0,100),new Vector2(350,270));
            breakerStageAnnouncement=BreakerLabel(root,"",new Vector2(0,100),new Vector2(280,180),85);
            for(int i=0;i<27;i++)
            {
                var mote=BreakerSprite("Stage forming crystal",6,Vector3.zero,.25f,18);
                mote.GetComponent<SpriteRenderer>().sprite=breakerHudSprites[8];
                var trail=mote.AddComponent<TrailRenderer>();trail.sharedMaterial=Resources.Load<Material>("Materials/BreakerTrail");
                trail.time=.22f;trail.startWidth=.045f;trail.endWidth=0;trail.startColor=new Color(.3f,.85f,1,.7f);trail.endColor=new Color(.3f,.85f,1,0);
                breakerStageMotes.Add(mote);
            }
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
                var shardRenderer=shard.GetComponent<SpriteRenderer>();
                shardRenderer.sharedMaterial=Resources.Load<Material>("Materials/BreakerShard");
                shardRenderer.color=breakerShardColors[color];
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
                    if(brick.MaxHP>1)
                    {
                        obj.GetComponent<SpriteRenderer>().sprite=breakerDurabilitySprites[(brick.HP>1?0:4)+brick.Color];
                        obj.transform.localScale=new Vector3(1.30f,1.03f,1);
                    }
                    breakerObjects.Add(obj);breakerHP.Add(brick.HP);
                }
                if(breakerStage!=breaker.Stage)breakerRevealTime=Time.time;
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
                var ball=breakerBalls[i];ball.SetActive(i<breaker.Balls.Count||(breaker.Transitioning&&i==0));
                if(breaker.Transitioning&&i==0)ball.transform.localPosition=BreakerPosition(breaker.Paddle,CrystalBreakerRules.PaddleY+.65f);
                if(i<breaker.Balls.Count){ball.transform.localPosition=BreakerPosition(breaker.Balls[i].X,breaker.Balls[i].Y);ball.GetComponent<TrailRenderer>().emitting=!breaker.Waiting&&!breakerPaused;if(breaker.Waiting)ball.GetComponent<TrailRenderer>().Clear();}
            }
            if(breaker.Transitioning)ClearBreakerTrails();
            float reveal=breaker.Waiting&&!breaker.Transitioning?Mathf.Clamp01((Time.time-breakerRevealTime)/.65f):1;
            foreach(var obj in breakerObjects){var c=obj.GetComponent<SpriteRenderer>().color;c.a=reveal;obj.GetComponent<SpriteRenderer>().color=c;}
            DrawBreakerTransition();
            DrawBreakerGifts();breakerStats.text=breaker.Score.ToString("D6");breakerLevel.text=breaker.Stage.ToString();
            for(int i=0;i<3;i++)breakerLives[i].color=i<breaker.Lives?Color.white:new Color(.25f,.45f,.55f,.25f);
            breakerBestText.text="BEST  "+breakerBest.ToString("D6");
            breakerMessage.text=breaker.Over?(breaker.Won?"ALL CRYSTALS CLEARED · YOU SHINE":"GAME OVER · PRESS PAUSE TO START AGAIN"):
                breakerPaused||breaker.Transitioning?"":breaker.Waiting?"CLICK OR PRESS SPACE TO LAUNCH":breaker.WideTime>0?"WIDE PADDLE · "+Mathf.CeilToInt(breaker.WideTime)+"s":"";
            breakerPause.GetComponentInChildren<Text>().text="";
            breakerMenuTitle.text=breaker.Over?(breaker.Won?"YOU SHINE":"GAME OVER"):"PAUSED";
            breakerResume.GetComponentInChildren<Text>().text=breaker.Over?"PLAY AGAIN":"RESUME";
            breakerMenu.SetActive(breakerPaused||breaker.Over);breakerCombo.gameObject.SetActive(breakerComboTime>0&&!breakerPaused);
        }
        void DrawBreakerGifts()
        {
            while(breakerGiftViews.Count<breaker.Gifts.Count)
            {
                var badge=HudImage(breakerUi.transform,5,Vector2.zero,new Vector2(95,82)).rectTransform;
                var beam=HudImage(badge,8,new Vector2(0,65),new Vector2(5,100));beam.name="Falling light";
                HudImage(badge,8,new Vector2(7,61),new Vector2(7,10));
                HudImage(badge,8,new Vector2(-4,94),new Vector2(5,8));
                breakerGiftViews.Add(badge.gameObject);
            }
            for(int i=0;i<breakerGiftViews.Count;i++)
            {
                var obj=breakerGiftViews[i];obj.SetActive(i<breaker.Gifts.Count);
                if(i<breaker.Gifts.Count)
                {
                    var gift=breaker.Gifts[i];var at=BreakerPosition(gift.X,gift.Y);
                    obj.GetComponent<RectTransform>().anchoredPosition=new Vector2(at.x*40,at.y*40);
                    obj.GetComponent<Image>().sprite=breakerHudSprites[gift.Wide?5:6];
                    obj.transform.Find("Falling light").GetComponent<Image>().color=gift.Wide?new Color(1,.8f,.3f,.45f):new Color(.7f,.4f,1,.45f);
                    obj.transform.localScale=Vector3.one*(1+.06f*Mathf.Sin(Time.time*5));
                }
            }
        }

        Image HudImage(Transform parent,int sprite,Vector2 pos,Vector2 size)
        {
            var image=UIObject("Crystal HUD art "+sprite,parent,pos,size).gameObject.AddComponent<Image>();
            image.sprite=breakerHudSprites[sprite];image.raycastTarget=false;return image;
        }
        void DrawBreakerTransition()
        {
            bool show=breaker.Transitioning&&!breakerPaused;
            breakerSeal.gameObject.SetActive(show);breakerStageAnnouncement.gameObject.SetActive(show);
            float t=1-breaker.TransitionRemaining/CrystalBreakerRules.StageTransitionDuration;
            float alpha=Mathf.Clamp01(Mathf.Min(t*5,(1-t)*6));
            breakerSeal.color=new Color(1,1,1,alpha);
            string[] roman={"I","II","III","IV","V"};
            breakerStageAnnouncement.text=roman[Mathf.Min(breaker.Stage,4)]+"\n<size=15>N E X T   C H A P T E R</size>";
            breakerStageAnnouncement.color=new Color(.7f,.95f,1,alpha);
            for(int i=0;i<breakerStageMotes.Count;i++)
            {
                var mote=breakerStageMotes[i];mote.SetActive(show);
                if(!show){mote.GetComponent<TrailRenderer>().Clear();continue;}
                float travel=Mathf.Repeat(t*1.35f+i*.037f,1);
                var end=BreakerPosition((i%8-3.5f)*2.3f,5.2f-i/8*1.02f);
                var start=BreakerPosition(breaker.Paddle,CrystalBreakerRules.PaddleY+.65f);
                mote.transform.localPosition=Vector3.Lerp(start,end,travel)+Vector3.right*Mathf.Sin(travel*Mathf.PI)*(i%2==0?3:-3);
                mote.GetComponent<SpriteRenderer>().color=new Color(.6f,.95f,1,alpha*Mathf.Sin(travel*Mathf.PI));
            }
        }
        void VerifyBreakerDurability(Action<bool,string> check)
        {
            check(breaker.Bricks[0].MaxHP==1&&breakerObjects[0].GetComponent<SpriteRenderer>().sprite==breakerSprites[breaker.Bricks[0].Color],"ordinary bricks use single-rim art");
            for(int i=0;i<2;i++){breaker.Launch();breaker.Bricks.Clear();breaker.Tick(.01f);for(int wait=0;wait<25;wait++)breaker.Tick(.1f);}
            DrawBreaker();var brick=breaker.Bricks[0];
            check(brick.MaxHP==2&&brick.HP==2&&breakerObjects[0].GetComponent<SpriteRenderer>().sprite==breakerDurabilitySprites[brick.Color],"stage three armor uses reinforced intact art");
            breaker.Launch();var ball=breaker.Balls[0];
            for(int hit=0;hit<2;hit++)
            {
                ball.X=brick.X;ball.Y=brick.Y+CrystalBreakerRules.BrickHalfHeight+CrystalBreakerRules.Radius+.02f;
                ball.VX=0;ball.VY=-9;breaker.Tick(.02f);DrawBreaker();
                if(hit==0)check(brick.HP==1&&brick.MaxHP==2&&breakerObjects[0].GetComponent<SpriteRenderer>().sprite==breakerDurabilitySprites[4+brick.Color],"first impact immediately switches to damaged armor art");
                else check(!breaker.Bricks.Contains(brick)&&breaker.Score==300,"second impact destroys damaged armor and scores");
            }
            NewBreaker();
            for(int color=0;color<4;color++)
            {
                BreakerImpact(color*3,0,color);
                var renderer=breakerFragments[breakerFragments.Count-1].Object.GetComponent<SpriteRenderer>();
                check(renderer.sprite==breakerSprites[6]&&renderer.color==breakerShardColors[color]&&renderer.sharedMaterial.shader.name=="iTetris/BreakerShard","original shard design retains brick color "+color);
            }
            breaker.Gifts.Add(new CrystalBreakerRules.Gift{X=-2,Y=0,Wide=true});
            breaker.Gifts.Add(new CrystalBreakerRules.Gift{X=2,Y=0,Wide=false});DrawBreaker();
            check(breakerGiftViews[0].GetComponent<Image>().sprite==breakerHudSprites[5]&&breakerGiftViews[1].GetComponent<Image>().sprite==breakerHudSprites[6]&&breakerGiftViews[0].GetComponentInChildren<Text>()==null&&!breakerGiftViews[0].GetComponent<Image>().raycastTarget,"falling rewards use distinct crystal pictograms without boxes or labels");
            breaker.Launch();breaker.Bricks.Clear();breaker.Tick(.01f);DrawBreaker();
            check(breaker.Transitioning&&breakerStageAnnouncement.gameObject.activeSelf&&breaker.Stage==1,"stage clear displays announcement before changing level");
            NewBreaker();
        }
        #if !UNITY_WEBGL || UNITY_EDITOR
        IEnumerator BreakerCapture()
        {
            yield return new WaitForSecondsRealtime(1);OpenBreaker();breakerCaptureMode=true;breaker.Launch();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--breaker-durability-capture")>=0)
            {
                for(int stage=0;stage<2;stage++){breaker.Bricks.Clear();breaker.Tick(.01f);for(int wait=0;wait<25;wait++)breaker.Tick(.1f);breaker.Launch();}
                for(int i=0;i<8;i+=2)
                {
                    var brick=breaker.Bricks[i];var hit=breaker.Balls[0];hit.X=brick.X;
                    hit.Y=brick.Y+CrystalBreakerRules.BrickHalfHeight+CrystalBreakerRules.Radius+.02f;
                    hit.VX=0;hit.VY=-9;breaker.Tick(.02f);
                }
            }
            var ball=breaker.Balls[0];ball.X=5.5f;ball.Y=1.5f;ball.VX=-4;ball.VY=-7;DrawBreaker();yield return null;ClearBreakerTrails();
            // A short simulated flight gives the live ball its real TrailRenderer history.
            for(int i=0;i<30;i++){ball.X=5.5f-i*.1f;ball.Y=1.5f-i*.105f;DrawBreaker();yield return null;}
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--breaker-effects-capture")>=0)
            {
                for(int color=0;color<4;color++)BreakerImpact(-6+color*4,0,color);
                breaker.Gifts.Add(new CrystalBreakerRules.Gift{X=-3,Y=-2,Wide=true});
                breaker.Gifts.Add(new CrystalBreakerRules.Gift{X=3,Y=-2,Wide=false});
                for(int i=0;i<10;i++){UpdateBreakerFragments();yield return null;}
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--breaker-transition-capture")>=0)
            {
                breaker.Bricks.Clear();breaker.Tick(.01f);
                for(int i=0;i<60;i++){breaker.TransitionRemaining=Mathf.Max(.1f,breaker.TransitionRemaining-Time.deltaTime);DrawBreaker();yield return null;}
            }
            breakerPaused=false;DrawBreaker();
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--capture-path");
            ScreenCapture.CaptureScreenshot(at>=0&&at+1<args.Length?args[at+1]:System.IO.Path.Combine(Application.persistentDataPath,"CrystalBreaker.png"));
            breakerCaptureMode=true;yield return new WaitForSecondsRealtime(2);Application.Quit();
        }
        #endif
    }
}
