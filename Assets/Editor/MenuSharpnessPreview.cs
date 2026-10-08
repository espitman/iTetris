using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace iTetris.Editor
{
    public static class MenuSharpnessPreview
    {
        public static void VerifyAndBuildAndroid()
        {
            Verify();
            GestureRegressionTests.Verify();
            ProjectBuilder.BuildAndroid();
        }
        public static void Verify()
        {
            var shader=Resources.Load<Shader>("Shaders/ReferenceMenuSharp");
            var digits=Resources.Load<Shader>("Shaders/ReferenceDigit");
            if(shader==null||digits==null||!shader.isSupported||!digits.isSupported
                ||ShaderUtil.ShaderHasError(shader)||ShaderUtil.ShaderHasError(digits))
                throw new Exception("Menu sharpening shader failed validation");
            var owner=new GameObject("Menu sharpening render fixture");
            var target=new RenderTexture(1080,2400,24,RenderTextureFormat.ARGB32);
            var material=new Material(shader);
            var texture=Resources.Load<Texture2D>("Art/MobileUI/approved-reference");
            try
            {
                var camera=owner.AddComponent<Camera>();camera.targetTexture=target;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                camera.orthographic=true;camera.orthographicSize=1200;camera.nearClipPlane=.1f;camera.farClipPlane=100;
                var canvasOwner=new GameObject("Canvas",typeof(Canvas));canvasOwner.transform.SetParent(owner.transform,false);
                var canvas=canvasOwner.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;
                canvas.worldCamera=camera;canvas.planeDistance=10;
                var imageOwner=new GameObject("Artwork",typeof(RectTransform),typeof(RawImage));imageOwner.transform.SetParent(canvas.transform,false);
                var rect=imageOwner.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                var image=imageOwner.GetComponent<RawImage>();image.texture=texture;
                var crops=new[]{new Rect(1359,121,334,631),new Rect(1711,121,320,631),new Rect(667,121,326,631)};
                var names=new[]{"Pause","GameOver","Home"};
                string report="Unity GPU menu sharpening verification — 1080x2400\n";
                for(int i=0;i<crops.Length;i++)
                {
                    var crop=crops[i];float visibleWidth=crop.height*1080/2400;
                    image.uvRect=new Rect((crop.center.x-visibleWidth/2)/2043f,(770-crop.yMax)/770f,visibleWidth/2043f,crop.height/770f);
                    image.material=null;var before=Capture(camera,target);
                    image.material=material;var after=Capture(camera,target);
                    var a=before.GetPixels32();var b=after.GetPixels32();double error=0,edgeA=0,edgeB=0;
                    for(int p=1;p<a.Length;p++)
                    {
                        error+=(Math.Abs(a[p].r-b[p].r)+Math.Abs(a[p].g-b[p].g)+Math.Abs(a[p].b-b[p].b))/3.0;
                        if(p%1080!=0){edgeA+=Math.Abs(a[p].r-a[p-1].r)+Math.Abs(a[p].g-a[p-1].g)+Math.Abs(a[p].b-a[p-1].b);edgeB+=Math.Abs(b[p].r-b[p-1].r)+Math.Abs(b[p].g-b[p-1].g)+Math.Abs(b[p].b-b[p-1].b);}
                    }
                    double gain=edgeB/edgeA,mean=error/a.Length;
                    if(gain<=1.02||mean<=.1||mean>10)throw new Exception("Unexpected sharpening output: "+names[i]+" gain="+gain+" mean="+mean);
                    report+=names[i]+": horizontal edge contrast ratio "+gain.ToString("F3")+", mean RGB correction "+mean.ToString("F3")+"/255\n";
                    File.WriteAllBytes("Documentation/MenuSharpness"+names[i]+"Before.png",before.EncodeToPNG());
                    File.WriteAllBytes("Documentation/MenuSharpness"+names[i]+"After.png",after.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(before);UnityEngine.Object.DestroyImmediate(after);
                }
                File.WriteAllText("Documentation/MenuSharpnessTests.txt",report);
                Debug.Log("MENU_SHARPNESS_GPU_TESTS_PASSED\n"+report);
            }
            finally {RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(owner);UnityEngine.Object.DestroyImmediate(material);target.Release();UnityEngine.Object.DestroyImmediate(target);}
        }
        static Texture2D Capture(Camera camera,RenderTexture target)
        {
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var result=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            result.ReadPixels(new Rect(0,0,target.width,target.height),0,0);result.Apply();return result;
        }
    }
}
