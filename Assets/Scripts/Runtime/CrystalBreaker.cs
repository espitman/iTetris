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
        bool breakerActive,breakerPaused;GameObject breakerWorld,breakerUi;CrystalBreakerRules breaker;
        readonly List<GameObject> breakerObjects=new List<GameObject>(); readonly List<GameObject> breakerBalls=new List<GameObject>();
        CrystalView breakerView;GameObject breakerPaddle;Text breakerStats,breakerMessage,breakerBestText;Button breakerPause;int breakerBest;Vector3 breakerMouse;
        bool BreakerAvailable=>Application.platform==RuntimePlatform.OSXPlayer||Application.platform==RuntimePlatform.OSXEditor;
        void OpenBreaker(){if(!BreakerAvailable)return;hubVisible=false;hubRoot.SetActive(false);tetrisWorld.SetActive(false);tetrisUiRoot.SetActive(false);
            if(breakerWorld==null)BuildBreaker();breakerWorld.SetActive(true);breakerUi.SetActive(true);breakerActive=true;breakerPaused=false;breakerMouse=Input.mousePosition;DrawBreaker();}
        void BuildBreaker(){breakerWorld=new GameObject("Crystal Breaker world");breakerView=new CrystalView(breakerWorld.transform);
            var plate=Quad("Arena glass",new Vector3(0,0,1),new Vector2(24.8f,18.5f),new Color(.012f,.035f,.065f,.87f),false);plate.transform.SetParent(breakerWorld.transform,true);
            CrystalView.Outline(breakerWorld.transform,24.5f,18.2f,.045f,Teal,0);
            breakerPaddle=breakerView.Block(PieceKind.I,new Vector3(0,-8,0));
            breakerUi=UIObject("Crystal Breaker UI",canvas.transform,Vector2.zero,new Vector2(1600,1000)).gameObject;var root=breakerUi.transform;
            Label(root,"C R Y S T A L   B R E A K E R",new Vector2(0,434),new Vector2(1200,60),32,Pale);
            breakerStats=Label(root,"",new Vector2(-641,175),new Vector2(270,280),22,Pale);
            breakerBestText=Label(root,"",new Vector2(641,175),new Vector2(270,120),18,Muted);
            Label(root,"MOUSE / ← →\nMOVE THE PADDLE\n\nSPACE / CLICK\nLAUNCH\n\nGOLD · WIDE PADDLE\nVIOLET · MULTIBALL",new Vector2(641,-90),new Vector2(270,270),14,Muted);
            breakerMessage=Label(root,"",new Vector2(0,-398),new Vector2(1200,42),18,Teal);
            ButtonAt(root,"GAME HUB",new Vector2(-628,-450),new Vector2(200,44),OpenHub);
            ButtonAt(root,"NEW GAME",new Vector2(394,-450),new Vector2(200,44),NewBreaker);
            breakerPause=ButtonAt(root,"PAUSE",new Vector2(628,-450),new Vector2(190,44),()=>{breakerPaused=!breakerPaused;DrawBreaker();});
            breakerBest=PlayerPrefs.GetInt("BreakerBest",0);NewBreaker();}
        void NewBreaker(){breaker=new CrystalBreakerRules();breaker.Broken+=(x,y,c)=>{audioWorld.Play("clear");shake=.06f;for(int i=0;i<12;i++){var o=Quad("Breaker shard",new Vector3(x,y,-.6f),Vector2.one*.06f,CrystalView.Palette[c]*1.6f,false);o.transform.SetParent(breakerWorld.transform,true);sparks.Add(new Spark{T=o.transform,Velocity=new Vector3(UnityEngine.Random.Range(-4f,4f),UnityEngine.Random.Range(-4f,4f),0),Life=.5f,Max=.5f});}};breakerPaused=false;DrawBreaker();}
        void UpdateBreaker(){UpdateAtmosphere();if(!breakerPaused)UpdateSparks();if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P)){breakerPaused=!breakerPaused;DrawBreaker();}
            if(!breakerPaused&&!breaker.Over){var mouse=Input.mousePosition;if(mouse!=breakerMouse){breaker.SetPaddle(cameraMain.ScreenToWorldPoint(mouse).x);breakerMouse=mouse;}float direction=(Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.A)?1:0);if(direction!=0)breaker.SetPaddle(breaker.Paddle+direction*22*Time.deltaTime);
                if(Input.GetKeyDown(KeyCode.Space)||(Input.GetMouseButtonDown(0)&&!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())){breaker.Launch();audioWorld.Play("drop");}breaker.Tick(Time.deltaTime);
                if(breaker.Score>breakerBest){breakerBest=breaker.Score;if(!diagnosticBest.HasValue){PlayerPrefs.SetInt("BreakerBest",breakerBest);PlayerPrefs.Save();}}}
            DrawBreaker();}
        int breakerStage=-1;readonly List<int> breakerHP=new List<int>();
        void DrawBreaker(){if(breaker==null)return;
            bool changed=breakerObjects.Count!=breaker.Bricks.Count||breakerStage!=breaker.Stage; if(!changed)for(int i=0;i<breakerHP.Count;i++)if(breakerHP[i]!=breaker.Bricks[i].HP){changed=true;break;}
            if(changed){foreach(var o in breakerObjects)Destroy(o);breakerObjects.Clear();breakerHP.Clear();foreach(var b in breaker.Bricks){var o=breakerView.Block((PieceKind)b.Color,new Vector3(b.X,b.Y,0));o.transform.localScale=new Vector3(2,.7f,.8f);breakerObjects.Add(o);breakerHP.Add(b.HP);if(b.HP>1)CrystalView.Outline(o.transform,.75f,.75f,.028f,Color.white,-.3f);}breakerStage=breaker.Stage;}
            breakerPaddle.transform.localPosition=new Vector3(breaker.Paddle,-8,0);breakerPaddle.transform.localScale=new Vector3(breaker.HalfWidth*2,.35f,.6f);
            while(breakerBalls.Count<breaker.Balls.Count){var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(sphere.GetComponent<Collider>());sphere.transform.SetParent(breakerWorld.transform,false);sphere.transform.localScale=Vector3.one*.4f;sphere.GetComponent<Renderer>().sharedMaterial=CrystalView.Lit(Color.white,.1f,.9f,Teal*2);var trail=sphere.AddComponent<TrailRenderer>();trail.time=.17f;trail.startWidth=.26f;trail.endWidth=0;trail.material=CrystalView.Unlit(Teal);breakerBalls.Add(sphere);}
            for(int i=0;i<breakerBalls.Count;i++){breakerBalls[i].SetActive(i<breaker.Balls.Count);if(i<breaker.Balls.Count)breakerBalls[i].transform.localPosition=new Vector3(breaker.Balls[i].X,breaker.Balls[i].Y,-.4f);}
            DrawBreakerGifts();breakerStats.text="SCORE\n"+breaker.Score.ToString("N0")+"\n\nSTAGE\n"+breaker.Stage+" / 5\n\nLIVES\n"+breaker.Lives.ToString();breakerBestText.text="BEST\n"+breakerBest.ToString("N0");
            breakerMessage.text=breaker.Over?(breaker.Won?"ALL CRYSTALS CLEARED · YOU SHINE":"GAME OVER · START A NEW GAME"):breakerPaused?"PAUSED · P / ESC TO RESUME":breaker.Waiting?"CLICK OR PRESS SPACE TO LAUNCH":breaker.WideTime>0?"WIDE PADDLE · "+Mathf.CeilToInt(breaker.WideTime)+"s":"BREAK THE CRYSTALS · CATCH THE LIGHT";
            breakerPause.GetComponentInChildren<Text>().text=breakerPaused?"RESUME":"PAUSE";}
        readonly List<GameObject> breakerGiftViews=new List<GameObject>();
        void DrawBreakerGifts(){while(breakerGiftViews.Count<breaker.Gifts.Count){var o=breakerView.Block(PieceKind.T,Vector3.zero,.6f);breakerGiftViews.Add(o);}for(int i=0;i<breakerGiftViews.Count;i++){var o=breakerGiftViews[i];o.SetActive(i<breaker.Gifts.Count);if(i<breaker.Gifts.Count){var g=breaker.Gifts[i];o.transform.localPosition=new Vector3(g.X,g.Y,-.3f);o.transform.localRotation=Quaternion.Euler(0,0,Time.time*90);breakerView.SetKind(o,g.Wide?PieceKind.O:PieceKind.T);}}}
        #if !UNITY_WEBGL || UNITY_EDITOR
        IEnumerator BreakerCapture(){yield return new WaitForSecondsRealtime(1);OpenBreaker();breaker.Launch();breaker.Balls[0].X=3;breaker.Balls[0].Y=-2;breakerPaused=true;DrawBreaker();yield return new WaitForSecondsRealtime(1);var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--capture-path");ScreenCapture.CaptureScreenshot(at>=0&&at+1<args.Length?args[at+1]:System.IO.Path.Combine(Application.persistentDataPath,"CrystalBreaker.png"));yield return new WaitForSecondsRealtime(2);Application.Quit();}
        #endif
    }
}
