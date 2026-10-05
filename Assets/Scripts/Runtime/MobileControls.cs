using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace iTetris
{
    public sealed class TouchHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action<bool> Changed;
        int pointer=int.MinValue;
        public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue)return;pointer=e.pointerId;Changed?.Invoke(true);}
        public void OnPointerUp(PointerEventData e){if(pointer!=e.pointerId)return;Release();}
        public void OnPointerExit(PointerEventData e){if(pointer==e.pointerId)Release();}
        void OnDisable(){Release();}
        void Release(){if(pointer==int.MinValue)return;pointer=int.MinValue;Changed?.Invoke(false);}
    }

    public sealed partial class GameController
    {
        bool mobileMode,touchLeft,touchRight,touchDown;
        RectTransform touchBar;
        void ConfigureMobile()
        {
            mobileMode=Application.platform==RuntimePlatform.Android;
            #if !UNITY_WEBGL || UNITY_EDITOR
            mobileMode|=Array.IndexOf(Environment.GetCommandLineArgs(),"--android-preview")>=0;
            #endif
            Application.targetFrameRate=mobileMode?60:120;
            if(Application.platform==RuntimePlatform.Android)
            {
                var pipeline=Instantiate((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline);
                pipeline.msaaSampleCount=1;QualitySettings.renderPipeline=pipeline;
            }
        }
        void BuildTouchControls()
        {
            if(!mobileMode)return;
            touchBar=UIObject("Touch controls",canvas.transform,Vector2.zero,new Vector2(900,310));
            touchBar.gameObject.AddComponent<Image>().color=new Color(.015f,.035f,.055f,.96f);
            TouchButton("←",-300,()=>{},v=>touchLeft=v,70);
            TouchButton("→",-100,()=>{},v=>touchRight=v,70);
            TouchButton("↶",100,()=>TouchAction("reverse"),null,70);
            TouchButton("↷",300,()=>TouchAction("rotate"),null,70);
            TouchButton("HOLD",-290,()=>TouchAction("hold"),null,-75);
            TouchButton("↓",0,()=>{},v=>touchDown=v,-75);
            TouchButton("DROP",290,()=>TouchAction("drop"),null,-75);
        }
        void TouchButton(string caption,float x,UnityEngine.Events.UnityAction action,Action<bool> held=null,float y=0)
        {
            var button=ButtonAt(touchBar,caption,new Vector2(x,y),new Vector2(caption.Length>2||y<0?260:180,125),action,caption=="DROP");
            button.GetComponentInChildren<Text>().fontSize=caption.Length>2?24:42;
            if(held!=null)button.gameObject.AddComponent<TouchHold>().Changed=held;
        }
        void TouchAction(string action)
        {
            if(!started||paused||clearing||game.GameOver||hubVisible||gardenActive||helpPanel.activeSelf)return;
            if(action=="rotate"||action=="reverse"){if(game.Rotate(action=="rotate"?1:-1))audioWorld.Play("rotate");}
            else if(action=="hold"){if(game.Hold())audioWorld.Play("hold");}
            else if(action=="drop"){game.HardDrop();audioWorld.Play("drop");shake=.12f;}
        }
        void LayoutViewport()
        {
            Rect area=mobileMode?Screen.safeArea:new Rect(0,0,Screen.width,Screen.height);
            if(area.width<=0||area.height<=0)area=new Rect(0,0,Screen.width,Screen.height);
            bool tetris=mobileMode&&!hubVisible&&!gardenActive;
            float scale=tetris?Mathf.Min(area.width/900f,area.height/1900f):Mathf.Min(area.width/1600f,area.height/1000f);
            canvas.scaleFactor=scale;
            var center=(area.center-new Vector2(Screen.width,Screen.height)*.5f)/scale;
            ((RectTransform)tetrisUiRoot.transform).anchoredPosition=center;
            if(hubRoot!=null)((RectTransform)hubRoot.transform).anchoredPosition=center;
            if(gardenUi!=null)((RectTransform)gardenUi.transform).anchoredPosition=center;
            if(tetris)
            {
                cameraMain.pixelRect=new Rect(area.center.x-320*scale,area.center.y-630*scale,640*scale,1280*scale);
                cameraMain.orthographicSize=12;
            }
            else{cameraMain.pixelRect=area;cameraMain.orthographicSize=area.height/(scale*80);}
            if(touchBar!=null)
            {
                touchBar.gameObject.SetActive(tetris&&started&&!paused&&!game.GameOver&&!helpPanel.activeSelf);
                touchBar.localScale=Vector3.one;touchBar.anchoredPosition=center+new Vector2(0,-790);
            }
        }
        void SetMobileOrientation(bool portrait)
        {
            if(Application.platform==RuntimePlatform.Android)Screen.orientation=portrait?ScreenOrientation.Portrait:ScreenOrientation.LandscapeLeft;
        }

        #if !UNITY_WEBGL || UNITY_EDITOR
        System.Collections.IEnumerator MobileSmoke()
        {
            yield return new WaitForSecondsRealtime(2);zen=true;paused=false;LayoutViewport();
            if(Application.platform==RuntimePlatform.Android)Check(Screen.height>Screen.width,"Android Tetris uses portrait orientation");
            Check(touchBar!=null&&touchBar.gameObject.activeSelf,"portrait touch controls are visible");
            var buttons=touchBar.GetComponentsInChildren<Button>();Check(buttons.Length==7,"all seven touch controls exist");
            var data=new PointerEventData(EventSystem.current){pointerId=1};
            int x=game.X;ExecuteEvents.Execute(buttons[0].gameObject,data,ExecuteEvents.pointerDownHandler);HandleInput();
            Check(game.X==x-1,"touch press moves piece left");ExecuteEvents.Execute(buttons[0].gameObject,data,ExecuteEvents.pointerUpHandler);
            Check(!touchLeft,"releasing touch stops horizontal repeat");
            buttons[4].onClick.Invoke();Check(game.Held.HasValue,"touch hold swaps piece");
            buttons[3].onClick.Invoke();Check(game.Rotation==1,"touch rotate turns piece clockwise");
            int y=game.Y;ExecuteEvents.Execute(buttons[5].gameObject,data,ExecuteEvents.pointerDownHandler);HandleInput();
            Check(game.Y<y,"touch soft drop lowers piece");ExecuteEvents.Execute(buttons[5].gameObject,data,ExecuteEvents.pointerUpHandler);
            buttons[6].onClick.Invoke();Check(game.Score>0,"touch hard drop locks and scores");
            data.position=RectTransformUtility.WorldToScreenPoint(null,buttons[0].transform.position);
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Exists(hit=>hit.gameObject==buttons[0].gameObject),"touch control accepts UI raycast");
            Check(Screen.safeArea.Contains(cameraMain.pixelRect.min)&&Screen.safeArea.Contains(cameraMain.pixelRect.max-Vector2.one),"portrait board fits within safe area");
            OnApplicationPause(true);Check(paused,"backgrounding pauses Tetris");PrimaryAction();zen=true;
            Array.Clear(game.Board,0,game.Board.Length);var rng=new System.Random(41);
            for(int col=0;col<10;col++)for(int row=0;row<4+(col%4);row++)if(row==0||rng.Next(9)!=0)game.Board[col,row]=rng.Next(1,8);
            DrawGame();yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Application.platform==RuntimePlatform.Android?"AndroidTetris.png":System.IO.Path.Combine(Application.persistentDataPath,"AndroidTetris.png"));
            yield return new WaitForSecondsRealtime(1);OpenHub();
            yield return new WaitForSecondsRealtime(2);
            if(Application.platform==RuntimePlatform.Android)Check(Screen.width>Screen.height,"hub returns to landscape orientation");
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
        void OnApplicationPause(bool suspended)
        {
            if(!suspended)return;
            SaveGarden();touchLeft=touchRight=touchDown=false;
            if(gardenActive&&!gardenPaused)PauseGarden();
            else if(started&&!paused&&!game.GameOver&&!hubVisible)TogglePause();
        }
    }
}
