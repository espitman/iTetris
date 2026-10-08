using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace iTetris
{
    public sealed class TetrisGestureSurface : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Action<Vector2> Dragged, Tapped;
        public Action Dropped, Held;
        public Func<float> CellSize;
        public Func<float> Clock=()=>Time.realtimeSinceStartup;
        const float FlickWindow=.14f, FlickCellsPerSecond=6f, FlickMinCells=.5f;
        struct Sample { public Vector2 Position; public float Time; }
        readonly System.Collections.Generic.Queue<Sample> samples=new System.Collections.Generic.Queue<Sample>();
        int pointer=int.MinValue;
        Vector2 origin, previous;
        float step, lastSampleTime;
        Vector2 lastSamplePosition;
        int sampleAxis;
        bool moved;
        public void OnPointerDown(PointerEventData e)
        {
            if(pointer!=int.MinValue)return;
            pointer=e.pointerId;origin=previous=e.position;moved=false;
            step=Mathf.Max(1,CellSize());samples.Clear();sampleAxis=0;
            lastSamplePosition=e.position;lastSampleTime=Clock();Remember(e.position);
        }
        void Remember(Vector2 position)
        {
            float now=Clock();
            var delta=position-lastSamplePosition;
            int axis=0;
            if(Mathf.Abs(delta.y)>step*.03f&&Mathf.Abs(delta.y)>Mathf.Abs(delta.x)*1.2f)axis=delta.y<0?-1:1;
            else if(Mathf.Abs(delta.x)>step*.03f&&Mathf.Abs(delta.x)>Mathf.Abs(delta.y)*1.2f)axis=2;
            if(axis!=0&&axis!=sampleAxis)
            {
                // A downward flick can follow a horizontal move without lifting the finger.
                samples.Clear();
                if(now-lastSampleTime<=FlickWindow)samples.Enqueue(new Sample{Position=lastSamplePosition,Time=lastSampleTime});
                sampleAxis=axis;
            }
            while(samples.Count>0&&now-samples.Peek().Time>FlickWindow)samples.Dequeue();
            samples.Enqueue(new Sample{Position=position,Time=now});
            lastSamplePosition=position;lastSampleTime=now;
        }
        public void OnDrag(PointerEventData e)
        {
            if(pointer!=e.pointerId)return;
            Remember(e.position);
            if((e.position-origin).magnitude>step*.2f)moved=true;
            var delta=e.position-previous;
            int x=(int)(delta.x/step), y=(int)(delta.y/step);
            if(sampleAxis==-1||sampleAxis==1)
            {
                // Discard lateral drift during a vertical stroke, including its release.
                // Rebase X so discarded drift cannot move the piece on a later event.
                x=0;previous.x=e.position.x;
            }
            if(x!=0||y!=0){previous+=new Vector2(x,y)*step;Dragged?.Invoke(new Vector2(x,y));}
        }
        public void OnPointerUp(PointerEventData e)
        {
            if(pointer!=e.pointerId)return;
            OnDrag(e);
            // A lock or another action cancels this pointer, protecting the next piece.
            if(pointer!=e.pointerId)return;
            var start=samples.Peek();
            var travel=e.position-start.Position;
            float duration=Clock()-start.Time;
            bool flick=duration>=.001f&&Mathf.Abs(travel.y)>=step*FlickMinCells
                &&Mathf.Abs(travel.y)>Mathf.Abs(travel.x)*1.2f
                &&Mathf.Abs(travel.y)/duration>=step*FlickCellsPerSecond;
            pointer=int.MinValue;samples.Clear();
            if(flick){if(travel.y<0)Dropped?.Invoke();else Held?.Invoke();}
            else if(!moved)Tapped?.Invoke(e.position);
        }
        public void Cancel(){pointer=int.MinValue;samples.Clear();}
        void OnDisable(){Cancel();}
    }

    public sealed partial class GameController
    {
        bool mobileMode,mobileDiagnostics;
        TetrisGestureSurface gestureSurface;
        Button mobileHoldButton;
        RectTransform touchBar;
        void ConfigureMobile()
        {
            mobileMode=Application.platform==RuntimePlatform.Android||Application.platform==RuntimePlatform.IPhonePlayer;
            #if !UNITY_WEBGL || UNITY_EDITOR
            mobileMode|=Array.IndexOf(Environment.GetCommandLineArgs(),"--android-preview")>=0;
            #endif
            #if !UNITY_WEBGL || UNITY_EDITOR
            mobileDiagnostics=Array.IndexOf(Environment.GetCommandLineArgs(),"--smoke-test")>=0;
            #if UNITY_ANDROID && !UNITY_EDITOR
            using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
            using(var intent=activity.Call<AndroidJavaObject>("getIntent"))
                mobileDiagnostics=intent.Call<string>("getStringExtra","crystalTest")=="smoke";
            #endif
            #endif
            Application.targetFrameRate=mobileMode?60:120;
            if(mobileMode){Screen.fullScreen=true;SetMobileOrientation(true);}
            if(Application.platform==RuntimePlatform.Android)
            {
                var pipeline=Instantiate((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline);
                pipeline.msaaSampleCount=1;QualitySettings.renderPipeline=pipeline;
            }
        }
        void BuildTouchControls()
        {
            if(!mobileMode)return;
            touchBar=UIObject("Finger gesture surface",tetrisUiRoot.transform,Vector2.zero,Vector2.one);
            touchBar.gameObject.AddComponent<Image>().color=Color.clear;
            gestureSurface=touchBar.gameObject.AddComponent<TetrisGestureSurface>();
            gestureSurface.CellSize=()=>cameraMain.pixelRect.height/20.8f;
            gestureSurface.Tapped=position=>TouchAction(position.x<cameraMain.pixelRect.center.x?"reverse":"rotate");
            gestureSurface.Dropped=()=>TouchAction("drop");
            gestureSurface.Held=()=>TouchAction("hold");
            gestureSurface.Dragged=delta=>
            {
                if(!MobileCanPlay)return;
                for(int i=0;i<Mathf.Abs(delta.x);i++)if(game.Move(delta.x<0?-1:1))audioWorld.Play("move");
                for(int i=0;i<-delta.y;i++){game.SoftDrop();if(clearing||game.GameOver)break;}
            };
            touchBar.SetAsFirstSibling();
        }
        bool MobileCanPlay=>started&&!paused&&!clearing&&!game.GameOver&&!hubVisible&&!gardenActive&&!breakerActive&&!helpPanel.activeSelf&&mobilePage==MobilePage.Play;
        void TouchAction(string action)
        {
            if(!MobileCanPlay)return;
            if(action=="rotate"||action=="reverse"){if(game.Rotate(action=="rotate"?1:-1))audioWorld.Play("rotate");}
            else if(action=="hold"){gestureSurface.Cancel();if(game.Hold())audioWorld.Play("hold");}
            else if(action=="drop"){game.HardDrop();audioWorld.Play("drop");shake=.12f;}
        }
        void LayoutViewport()
        {
            Rect area=mobileMode?Screen.safeArea:new Rect(0,0,Screen.width,Screen.height);
            if(area.width<=0||area.height<=0)area=new Rect(0,0,Screen.width,Screen.height);
            bool tetris=mobileMode&&!hubVisible&&!gardenActive;
            float scale=tetris?Mathf.Min(area.width/900f,area.height/1100f):Mathf.Min(area.width/1600f,area.height/1000f);
            canvas.scaleFactor=scale;
            var center=(area.center-new Vector2(Screen.width,Screen.height)*.5f)/scale;
            ((RectTransform)tetrisUiRoot.transform).anchoredPosition=center;
            if(hubRoot!=null)((RectTransform)hubRoot.transform).anchoredPosition=center;
            if(gardenUi!=null)((RectTransform)gardenUi.transform).anchoredPosition=center;
            if(tetris)
            {
                float header=390*scale,footer=100*scale;
                float height=Mathf.Min(area.height-header-footer,area.width/.516f);
                float width=height*.516f;
                cameraMain.pixelRect=new Rect(area.center.x-width*.5f,area.yMin+footer+(area.height-header-footer-height)*.5f,width,height);
                cameraMain.orthographicSize=10.4f;
                LayoutPortraitChrome(area.height/scale);
            }
            else{cameraMain.pixelRect=area;cameraMain.orthographicSize=area.height/(scale*80);}
            if(touchBar!=null)
            {
                touchBar.gameObject.SetActive(tetris&&MobileCanPlay);
                touchBar.localScale=Vector3.one;
                touchBar.anchoredPosition=(cameraMain.pixelRect.center-area.center)/scale;
                touchBar.sizeDelta=cameraMain.pixelRect.size/scale;
                mobileHoldButton.interactable=MobileCanPlay&&!game.HoldUsed;
            }
        }
        void SetMobileOrientation(bool portrait)
        {
            if(mobileMode)Screen.orientation=ScreenOrientation.Portrait;
        }

        #if !UNITY_WEBGL || UNITY_EDITOR
        System.Collections.IEnumerator MobileSmoke()
        {
            yield return new WaitForSecondsRealtime(2);zen=true;paused=false;LayoutViewport();
            if(Application.platform==RuntimePlatform.Android)Check(Screen.height>Screen.width,"Android Tetris uses portrait orientation");
            Check(touchBar!=null&&touchBar.gameObject.activeSelf,"portrait gesture surface is visible");
            Check(touchBar.GetComponentsInChildren<Button>().Length==0,"movement uses no buttons");
            var data=new PointerEventData(EventSystem.current){pointerId=1,position=cameraMain.pixelRect.center};
            int x=game.X;
            gestureSurface.OnPointerDown(data);data.position+=Vector2.left*gestureSurface.CellSize()*1.1f;gestureSurface.OnDrag(data);gestureSurface.OnPointerUp(data);
            Check(game.X==x-1,"finger drag moves piece left without rotating");
            mobileHoldButton.onClick.Invoke();Check(game.Held.HasValue,"hold box swaps piece");
            data.position=cameraMain.pixelRect.center+Vector2.right*gestureSurface.CellSize();
            int rotation=game.Rotation;
            gestureSurface.OnPointerDown(data);gestureSurface.OnPointerUp(data);
            Check(game.Rotation==(rotation+1)%4,"right-side tap rotates clockwise");
            data.position=cameraMain.pixelRect.center-Vector2.right*gestureSurface.CellSize();
            gestureSurface.OnPointerDown(data);gestureSurface.OnPointerUp(data);
            Check(game.Rotation==rotation,"left-side tap rotates counterclockwise");
            float gestureTime=0;
            gestureSurface.Clock=()=>gestureTime;
            StartGame();zen=true;
            int locks=0;
            Action<iTetris.Core.LockResult> countLock=result=>locks++;
            game.Locked+=countLock;
            data.position=cameraMain.pixelRect.center;
            int y=game.Y;
            gestureSurface.OnPointerDown(data);
            for(int i=0;i<8;i++)
            {
                gestureTime+=.12f;data.position+=Vector2.down*gestureSurface.CellSize()*.5f;
                gestureSurface.OnDrag(data);
            }
            gestureTime+=.02f;gestureSurface.OnPointerUp(data);
            Check(locks==0&&game.Y<y,"long slow downward drag remains soft drop after release");
            gestureSurface.OnPointerDown(data);
            gestureTime+=.08f;data.position+=Vector2.down*gestureSurface.CellSize()*.8f;
            gestureSurface.OnDrag(data);
            Check(locks==0,"fast downward flick waits for release");
            gestureTime+=.01f;gestureSurface.OnPointerUp(data);
            Check(locks==1,"short fast downward flick hard drops exactly one piece");
            gestureSurface.OnPointerUp(data);
            Check(locks==1,"duplicate release never drops a second piece");
            gestureSurface.OnPointerDown(data);
            gestureTime+=.1f;data.position+=Vector2.left*gestureSurface.CellSize()*4;
            gestureSurface.OnDrag(data);
            gestureTime+=.06f;data.position+=Vector2.down*gestureSurface.CellSize()*.8f;
            gestureSurface.OnDrag(data);
            gestureTime+=.01f;gestureSurface.OnPointerUp(data);
            Check(locks==2,"downward flick after a long horizontal drag still hard drops");
            gestureSurface.OnPointerDown(data);
            gestureTime+=.06f;data.position+=Vector2.down*gestureSurface.CellSize();
            gestureSurface.OnDrag(data);
            gestureTime+=.3f;gestureSurface.OnPointerUp(data);
            Check(locks==2,"pausing before release does not turn a drag into a hard drop");
            gestureSurface.OnPointerDown(data);
            gestureTime+=.06f;data.position+=Vector2.down*gestureSurface.CellSize();
            gestureSurface.OnDrag(data);gestureSurface.Cancel();gestureSurface.OnPointerUp(data);
            Check(locks==2,"cancelled gesture does not hard drop");
            gestureSurface.OnPointerDown(data);
            game.HardDrop();int afterLock=locks;
            gestureTime+=.06f;data.position+=Vector2.down*gestureSurface.CellSize();
            gestureSurface.OnPointerUp(data);
            Check(locks==afterLock,"locking during a gesture protects the next piece");
            game.Locked-=countLock;
            StartGame();zen=true;
            gestureSurface.OnPointerDown(data);
            gestureTime+=.08f;data.position+=Vector2.up*gestureSurface.CellSize()*.8f;
            gestureSurface.OnDrag(data);gestureTime+=.01f;gestureSurface.OnPointerUp(data);
            Check(game.Held.HasValue&&game.HoldUsed,"upward flick holds the piece");
            gestureSurface.Clock=()=>Time.realtimeSinceStartup;
            StartGame();zen=true;LayoutViewport();
            data.position=RectTransformUtility.WorldToScreenPoint(null,mobileHoldButton.transform.position);
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Exists(hit=>hit.gameObject==mobileHoldButton.gameObject),"hold box accepts UI raycast");
            Check(Screen.safeArea.Contains(cameraMain.pixelRect.min)&&Screen.safeArea.Contains(cameraMain.pixelRect.max-Vector2.one),"portrait board fits within safe area");
            OnApplicationPause(true);Check(paused,"backgrounding pauses Tetris");PrimaryAction();zen=true;
            var holdCorners=new Vector3[4];mobileHoldButton.GetComponent<RectTransform>().GetWorldCorners(holdCorners);
            Check(holdCorners[0].y>cameraMain.pixelRect.yMax,"HOLD and three NEXT stay wholly above the board");
            Check(mobilePreviewPieces.Length==4&&mobilePreviewPieces[0].gameObject.activeSelf&&mobilePreviewPieces[2].gameObject.activeSelf,"exactly three NEXT previews are visible");
            Check(previewBlocks[0,0].GetComponentInChildren<MeshFilter>().sharedMesh==active[0].GetComponentInChildren<MeshFilter>().sharedMesh,"previews use the original gameplay crystal mesh");
            bool previousMute=audioWorld.Muted;if(previousMute){audioWorld.Toggle();UpdateSound();}
            TogglePause();Check(!tetrisWorld.activeSelf&&!cameraMain.enabled&&mobilePage==MobilePage.Pause,"pause uses a dedicated page without covering the board");
            Check(referenceAtlas.width==2043&&referenceAtlas.height==770,"approved artwork keeps original pixel dimensions");
            var menuScale=mobileMenu.localScale;
            Check(Mathf.Abs(menuScale.y/menuScale.x-(900*PauseCrop.height/PauseCrop.width)/1750)<.001f,"pause preserves artwork aspect ratio");
            yield return CaptureMobilePage("MobilePause.png");
            menuSoundButton.onClick.Invoke();Check(audioWorld.Muted,"pause sound button mutes audio");yield return CaptureMobilePage("MobilePauseSoundOff.png");
            menuSoundButton.onClick.Invoke();Check(!audioWorld.Muted,"pause sound button restores audio");
            primaryButton.onClick.Invoke();Check(!paused&&mobilePage==MobilePage.Play&&tetrisWorld.activeSelf,"RESUME returns to the unobstructed board");
            TogglePause();secondaryButton.onClick.Invoke();Check(!paused&&game.Score==0&&mobilePage==MobilePage.Play,"NEW GAME restarts from pause");
            TogglePause();menuMainButton.onClick.Invoke();Check(paused&&mobilePage==MobilePage.Home,"MAIN MENU preserves and pauses the running game");
            yield return CaptureMobilePage("MobileHome.png");
            homeBest.text="028640";yield return CaptureMobilePage("MobileHomeReference.png");homeBest.text=best.ToString("D6");
            ToggleHelp();Check(mobilePage==MobilePage.Help,"home opens the controls guide");yield return CaptureMobilePage("MobileHelp.png");ToggleHelp();Check(mobilePage==MobilePage.Home&&paused,"guide returns to home without resuming play");
            ShowMobileMenu("GAME OVER","","PLAY AGAIN",false);Check(!tetrisWorld.activeSelf&&!cameraMain.enabled,"game over keeps the board unobstructed on a separate page");yield return CaptureMobilePage("MobileGameOver.png");
            // Exercise the live renderer with the concept's values for visual
            // comparison, then with every remaining glyph. The runtime score
            // screenshot above still contains the real game state.
            mobileResult.text="012480";mobileBest.text="028640";
            yield return CaptureMobilePage("MobileGameOverReference.png");
            mobileResult.text="345679";mobileBest.text="987531";
            yield return CaptureMobilePage("MobileGameOverDigits.png");
            ShowMobileMenu("GAME OVER","","PLAY AGAIN",false);
            StartGame();int drops=0;while(!game.GameOver&&drops++<100)game.HardDrop();yield return null;
            Check(game.GameOver&&mobilePage==MobilePage.GameOver,"a real completed game opens the game over page");
            primaryButton.onClick.Invoke();Check(!game.GameOver&&game.Score==0&&!paused&&mobilePage==MobilePage.Play,"PLAY AGAIN restarts a completed game");
            if(previousMute){audioWorld.Toggle();UpdateSound();}
            SetMobilePage(MobilePage.Splash);yield return CaptureMobilePage("MobileLaunch.png");PrimaryAction();zen=true;
            Array.Clear(game.Board,0,game.Board.Length);var rng=new System.Random(41);
            for(int col=0;col<10;col++)for(int row=0;row<4+(col%4);row++)if(row==0||rng.Next(9)!=0)game.Board[col,row]=rng.Next(1,8);
            DrawGame();yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Application.platform==RuntimePlatform.Android?"AndroidTetris.png":System.IO.Path.Combine(Application.persistentDataPath,"AndroidTetris.png"));
            yield return new WaitForSecondsRealtime(1);OpenHub();
            yield return new WaitForSecondsRealtime(2);
            if(Application.platform==RuntimePlatform.Android)Check(Screen.height>Screen.width,"mobile stays in portrait orientation");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Application.platform==RuntimePlatform.Android?"AndroidHub.png":System.IO.Path.Combine(Application.persistentDataPath,"AndroidHub.png"));
            yield return new WaitForSecondsRealtime(1);OpenGarden();gardenPaused=false;garden.Restore(new[]{0,1,2,1,1,1,0,2,2,0,3,0,0,1,2,1},1240,26,1);DrawGarden();
            yield return new WaitForSecondsRealtime(2);LayoutViewport();Physics.SyncTransforms();
            gardenSelected=-1;GardenPointer(cameraMain.WorldToScreenPoint(GardenPosition(1)));
            Check(gardenSelected==1,"garden screen touch selects calibrated pedestal");
            OnApplicationPause(true);Check(gardenPaused,"backgrounding pauses garden");ResumeGarden();
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Application.platform==RuntimePlatform.Android?"AndroidGarden.png":System.IO.Path.Combine(Application.persistentDataPath,"AndroidGarden.png"));
            yield return new WaitForSecondsRealtime(1);
        }
        #endif
        System.Collections.IEnumerator CaptureMobilePage(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Application.platform==RuntimePlatform.Android?name:System.IO.Path.Combine(Application.persistentDataPath,name));
            yield return new WaitForSecondsRealtime(.5f);
        }
        void OnApplicationPause(bool suspended)
        {
            if(!suspended)return;
            SaveGarden();ResetInput();
            if(gardenActive&&!gardenPaused)PauseGarden();
            else if(started&&!paused&&!game.GameOver&&!hubVisible)TogglePause();
        }
    }
}
