#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LuckySpin.SlotMachine.Editor
{
    /// <summary>
    /// Automated build utility for generating production-ready WebGL packages from CLI or Unity Editor menu.
    /// Configures 1920x1080 canvas resolution, WebAssembly target, uncompressed payload for broad HTTP server compatibility,
    /// and targets Build/WebGL/ output directory.
    /// </summary>
    public static class WebGLBuilder
    {
        [MenuItem("Build/Build WebGL (Release)")]
        public static void BuildWebGL()
        {
            Debug.Log("[WebGLBuilder] Starting WebGL release build process...");

            // 1. Switch Build Target to WebGL
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            // 2. Configure Resolution & Presentation
            PlayerSettings.defaultWebScreenWidth = 1920;
            PlayerSettings.defaultWebScreenHeight = 1080;
            PlayerSettings.runInBackground = true;

            // 3. Configure WebGL Platform Settings
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // Uncompressed for direct local HTTP server compatibility
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.WebGL.memorySize = 256; // 256 MB initial heap
            PlayerSettings.WebGL.dataCaching = true;

            // 4. Set Scenes to Build
            string[] scenes = new string[]
            {
                "Assets/Scenes/MainGame.unity"
            };

            // 5. Build Player Options
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Build/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            // 6. Execute Build Pipeline
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log(string.Format("[WebGLBuilder] Build SUCCEEDED: Total size {0} bytes in {1} seconds.",
                    summary.totalSize, summary.totalTime.TotalSeconds));
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError(string.Format("[WebGLBuilder] Build FAILED: {0} errors recorded.", summary.totalErrors));
            }
        }
    }
}
#endif

