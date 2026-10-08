using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace iTetris.Editor
{
    // Run against the actual gesture component and Unity pointer events, with a
    // deterministic clock. No display, emulator, or third-party test package needed.
    public static class GestureRegressionTests
    {
        public static void VerifyAndBuildAndroid()
        {
            Verify();
            ProjectBuilder.BuildAndroid();
        }

        public static void Verify()
        {
            var results=new List<string>();
            var owner=new GameObject("Gesture regression fixture");
            try
            {
                var surface=owner.AddComponent<TetrisGestureSurface>();
                float now=0;int horizontal=0,vertical=0,drops=0,holds=0,taps=0;
                surface.CellSize=()=>100;
                surface.Clock=()=>now;
                surface.Dragged=d=>{horizontal+=(int)d.x;vertical+=(int)d.y;};
                surface.Dropped=()=>drops++;
                surface.Held=()=>holds++;
                surface.Tapped=p=>taps++;
                var pointer=new PointerEventData(null){pointerId=1};
                Action begin=()=>
                {
                    surface.Cancel();horizontal=vertical=drops=holds=taps=0;
                    now=0;pointer.position=Vector2.zero;surface.OnPointerDown(pointer);
                };
                Action<float,float,float> drag=(x,y,elapsed)=>
                {
                    now+=elapsed;pointer.position+=new Vector2(x,y);surface.OnDrag(pointer);
                };
                Action release=()=>{now+=.01f;surface.OnPointerUp(pointer);};
                Action<bool,string> check=(pass,label)=>
                {
                    if(!pass)throw new Exception("GESTURE REGRESSION FAILED: "+label);
                    results.Add("PASS "+label);
                };

                begin();drag(120,-400,.08f);release();
                check(horizontal==0&&drops==1,"long downward flick with right drift does not move horizontally");
                begin();drag(-120,-400,.08f);release();
                check(horizontal==0&&drops==1,"long downward flick with left drift does not move horizontally");
                begin();for(int i=0;i<4;i++)drag(35,-110,.02f);release();
                check(horizontal==0&&drops==1,"right drift cannot accumulate over several downward samples");
                begin();for(int i=0;i<4;i++)drag(-35,-110,.02f);release();
                check(horizontal==0&&drops==1,"left drift cannot accumulate over several downward samples");
                begin();drag(30,-100,.02f);now+=.02f;pointer.position+=new Vector2(90,-250);surface.OnPointerUp(pointer);
                check(horizontal==0&&drops==1,"final release sample cannot add a horizontal move before the drop");
                surface.OnPointerUp(pointer);
                check(drops==1,"duplicate release cannot drop a second piece");
                begin();drag(90,0,.06f);drag(40,-200,.03f);release();
                check(horizontal==0&&drops==1,"unconsumed horizontal remainder is discarded when turning downward");
                begin();drag(210,0,.1f);drag(120,-400,.06f);release();
                check(horizontal==2&&drops==1,"intentional right movement is preserved before downward flick");
                begin();drag(-210,0,.1f);drag(120,-400,.06f);release();
                check(horizontal==-2&&drops==1,"intentional left movement is preserved before downward flick");
                begin();drag(120,-400,.06f);drag(110,0,.03f);release();
                check(horizontal==1&&drops==0,"changing direction back to horizontal permits an intentional move");
                begin();for(int i=0;i<8;i++)drag(30,-50,.12f);now+=.02f;surface.OnPointerUp(pointer);
                check(horizontal==0&&vertical<0&&drops==0,"slow downward drag stays soft drop without sideways drift");
                begin();drag(110,0,.1f);release();
                check(horizontal==1&&drops==0&&taps==0,"ordinary horizontal drag remains responsive");
                begin();drag(0,-80,.08f);release();
                check(drops==1,"short fast downward flick still drops on release");
                begin();drag(120,400,.08f);release();
                check(horizontal==0&&holds==1,"upward hold flick also ignores lateral drift");
                begin();drag(120,-400,.08f);surface.Cancel();release();
                check(drops==0,"cancelled pointer cannot drop");
                begin();release();check(taps==1&&drops==0,"stationary tap still rotates through the tap callback");
                begin();drag(0,-100,.06f);now+=.3f;surface.OnPointerUp(pointer);
                check(drops==0,"pausing before release does not trigger hard drop");
                File.WriteAllLines("Documentation/GestureRegressionTests.txt",results);
                Debug.Log("ALL GESTURE REGRESSION TESTS PASSED: "+results.Count);
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
    }
}
