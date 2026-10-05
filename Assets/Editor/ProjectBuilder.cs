using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using iTetris.Core;

namespace iTetris.Editor
{
    public static class ProjectBuilder
    {
        [MenuItem("iTetris/Prepare Project")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/Settings");Directory.CreateDirectory("Assets/Scenes");
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/CrystalRenderer.asset");
            if(renderer==null){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,"Assets/Settings/CrystalRenderer.asset");}
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/CrystalURP.asset");
            if(pipeline==null){pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,"Assets/Settings/CrystalURP.asset");}
            pipeline.supportsHDR=true;pipeline.msaaSampleCount=4;pipeline.renderScale=1;
            GraphicsSettings.defaultRenderPipeline=pipeline;
            for(int q=0;q<QualitySettings.names.Length;q++)QualitySettings.SetQualityLevel(q,false);
            QualitySettings.renderPipeline=pipeline;QualitySettings.vSyncCount=1;
            PlayerSettings.companyName="iTetris";PlayerSettings.productName="iTetris";PlayerSettings.bundleVersion="1.0.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"com.itetris.crystal");
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=1000;
            PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone,2); // Universal Intel + Apple Silicon.
            PlayerSettings.usePlayerLog=true;
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input=settings.FindProperty("activeInputHandler");if(input!=null)input.intValue=0;
            settings.ApplyModifiedPropertiesWithoutUndo();
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Art"}))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if(importer.maxTextureSize!=2048||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.mipmapEnabled)
                {importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();}
            }
            Directory.CreateDirectory("Assets/Resources/Materials");
            for(int i=0;i<7;i++)
            {
                string path="Assets/Resources/Materials/CrystalGlass"+i+".mat";
                if(AssetDatabase.LoadAssetAtPath<Material>(path)==null)AssetDatabase.CreateAsset(CrystalView.Lit(CrystalView.Palette[i],.35f,.87f,CrystalView.Palette[i]*.16f),path);
            }
            string[] names={"OpaqueEdge","TransparentEdge","Background"};
            for(int i=0;i<3;i++)
            {
                string path="Assets/Resources/Materials/"+names[i]+".mat";
                if(AssetDatabase.LoadAssetAtPath<Material>(path)==null)
                {
                    Material mat=i==2?new Material(Shader.Find("iTetris/AuroraBackground")):CrystalView.Unlit(new Color(1,1,1,i==1?.5f:1));
                    if(i==2)mat.SetTexture("_MainTex",Resources.Load<Texture2D>("Art/AuroraLake"));
                    AssetDatabase.CreateAsset(mat,path);
                }
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("iTetris",typeof(GameController));
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/iTetris.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/iTetris.unity",true)};
            EditorUtility.SetDirty(pipeline);AssetDatabase.SaveAssets();
            Debug.Log("ITETRIS_PROJECT_READY");
        }
        [MenuItem("iTetris/Build macOS")]
        public static void BuildMac()
        {
            Prepare();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/iTetris.unity"},locationPathName="Builds/iTetris.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("macOS build failed: "+report.summary.result);
            Debug.Log("ITETRIS_BUILD_SUCCEEDED "+report.summary.totalSize);
        }
        [MenuItem("iTetris/Build Web")]
        public static void BuildWeb()
        {
            Prepare();
            // Desktop WebGL textures use GPU compression to fit the Pages asset limit.
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Art"}))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var web=importer.GetPlatformTextureSettings("WebGL");web.overridden=true;web.maxTextureSize=2048;
                web.format=TextureImporterFormat.DXT5;web.textureCompression=TextureImporterCompression.CompressedHQ;web.compressionQuality=100;
                importer.SetPlatformTextureSettings(web);importer.SaveAndReimport();
            }
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback=true;
            PlayerSettings.WebGL.dataCaching=true;
            PlayerSettings.WebGL.nameFilesAsHashes=true;
            PlayerSettings.WebGL.template="PROJECT:Aurora";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL,ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL,ManagedStrippingLevel.High);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL,new[]{GraphicsDeviceType.OpenGLES3});
            PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/iTetris.unity"},locationPathName="Builds/Web",target=BuildTarget.WebGL,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Web build failed: "+report.summary.result);
            File.Copy("Tools/cloudflare-headers","Builds/Web/_headers",true);
            if(File.Exists("Builds/Web/_redirects"))File.Delete("Builds/Web/_redirects");
            foreach(var file in Directory.GetFiles("Builds/Web","*",SearchOption.AllDirectories))
                if(new FileInfo(file).Length>25L*1024*1024)throw new Exception("Cloudflare Pages asset exceeds 25 MiB: "+file);
            Debug.Log("ITETRIS_WEB_BUILD_SUCCEEDED "+report.summary.totalSize);
        }
        [MenuItem("iTetris/Build Android APK")]
        public static void BuildAndroid()
        {
            var tools=Path.Combine(EditorApplication.applicationContentsPath,"PlaybackEngines/AndroidPlayer");
            string jdk=Environment.GetEnvironmentVariable("ITETRIS_JDK_PATH");
            if(string.IsNullOrEmpty(jdk))jdk=Directory.Exists("/opt/homebrew/opt/openjdk@17/libexec/openjdk.jdk/Contents/Home")?"/opt/homebrew/opt/openjdk@17/libexec/openjdk.jdk/Contents/Home":Path.Combine(tools,"OpenJDK");
            UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath=jdk;
            UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath=Path.Combine(tools,"SDK");
            string ndk=Path.Combine(tools,"SDK/ndk/27.2.12479018");
            UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath=Directory.Exists(ndk)?ndk:Path.Combine(tools,"NDK");
            Prepare();
            PlayerSettings.productName="Crystal Arcade";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.itetris.crystalarcade");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android,ManagedStrippingLevel.High);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode=1;PlayerSettings.Android.useCustomKeystore=false;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            EditorUserBuildSettings.buildAppBundle=false;EditorUserBuildSettings.androidBuildSystem=AndroidBuildSystem.Gradle;
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Art"}))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var android=importer.GetPlatformTextureSettings("Android");
                if(android.overridden&&android.maxTextureSize==2048&&android.format==TextureImporterFormat.ETC2_RGBA8&&android.textureCompression==TextureImporterCompression.CompressedHQ&&android.compressionQuality==100)continue;
                android.overridden=true;android.maxTextureSize=2048;
                android.format=TextureImporterFormat.ETC2_RGBA8;android.textureCompression=TextureImporterCompression.CompressedHQ;android.compressionQuality=100;
                importer.SetPlatformTextureSettings(android);importer.SaveAndReimport();
            }
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/iTetris.unity"},locationPathName="Builds/Android/CrystalArcade.apk",target=BuildTarget.Android,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Android build failed: "+report.summary.result);
            Debug.Log("CRYSTAL_ARCADE_ANDROID_BUILD_SUCCEEDED "+report.summary.totalSize);
        }
        public static void RunRulesTests()
        {
            var path=Path.GetFullPath("Documentation/RulesTests.txt");
            string results=RulesVerification.Run()+"\n"+BrickGardenVerification.Run()+"\n"+CrystalBreakerVerification.Run();File.WriteAllText(path,results);Debug.Log(results);
        }
    }
}
