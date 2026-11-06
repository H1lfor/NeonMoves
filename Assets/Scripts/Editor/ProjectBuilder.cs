using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Neonmoves.Editor
{
    public static class ProjectBuilder
    {
        [MenuItem("Neonmoves/Prepare project")]
        public static void Prepare()
        {
            PlayerSettings.companyName="Neonmoves Studio";
            PlayerSettings.productName="Neonmoves";
            PlayerSettings.bundleVersion="1.5.1";
            var logoImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Brand/NeonmovesLogo.png");
            if(logoImporter) {
                logoImporter.textureType=TextureImporterType.Default;logoImporter.alphaIsTransparency=true;
                logoImporter.isReadable=true;logoImporter.mipmapEnabled=false;logoImporter.sRGBTexture=true;
                logoImporter.npotScale=TextureImporterNPOTScale.None;logoImporter.maxTextureSize=2048;
                logoImporter.textureCompression=TextureImporterCompression.Uncompressed;
                logoImporter.filterMode=FilterMode.Bilinear;logoImporter.wrapMode=TextureWrapMode.Clamp;
                logoImporter.SaveAndReimport();
            }
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            // Alt+Enter is handled by the game so it always selects native resolution.
            PlayerSettings.allowFullscreenSwitch=false;
            PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{UnityEngine.Rendering.GraphicsDeviceType.Direct3D11});
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("NEONMOVES — runtime composition");root.AddComponent<NeonGame>();
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/Neonmoves.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Neonmoves.unity",true)};
            AssetDatabase.SaveAssets();
            RuleTests.Run();
            Debug.Log("NEONMOVES: project prepared, rule tests passed.");
        }
        [MenuItem("Neonmoves/Build Windows")]
        public static void BuildWindows()
        {
            Prepare();
            Directory.CreateDirectory("Build/Neonmoves");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Scenes/Neonmoves.unity"},locationPathName="Build/Neonmoves/Neonmoves.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
            File.WriteAllText("Logs/build-result.txt","Windows build succeeded\n"+report.summary.totalSize+" bytes\n"+report.summary.totalTime);
            Debug.Log("NEONMOVES: Windows build succeeded.");
        }
    }
}
