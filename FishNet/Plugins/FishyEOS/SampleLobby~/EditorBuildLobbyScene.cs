#if UNITY_EDITOR
namespace EOSLobby
{
    public static class EditorBuildLobbyScene
    {
        [UnityEditor.MenuItem("Tools/FishyEOS/Build and Run Lobby Sample (Windows)")]
        public static void BuildAndRunLobbySceneWindows()
        {
            var originalBuildTargetGroup = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
            var originalBuildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            UnityEditor.BuildPipeline.BuildPlayer(
                new[] { "Assets/EOSLobby/Lobby.unity" }, "Build/Windows/EOSLobbySample.exe", 
                UnityEditor.BuildTarget.StandaloneWindows64, UnityEditor.BuildOptions.None);
            UnityEditor.EditorUserBuildSettings.SwitchActiveBuildTarget(originalBuildTargetGroup, originalBuildTarget);
            var executable = System.IO.Path.GetFullPath("Build/Windows/EOSLobbySample.exe");
            System.Diagnostics.Process.Start(executable);
        }
    
        [UnityEditor.MenuItem("Tools/FishyEOS/Build and Run Lobby Sample (Mac)")]
        public static void BuildAndRunLobbySceneMac()
        {
            var originalBuildTargetGroup = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
            var originalBuildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            UnityEditor.BuildPipeline.BuildPlayer(
                new[] { "Assets/EOSLobby/Lobby.unity" }, "Build/Mac/EOSLobbySample.app", 
                UnityEditor.BuildTarget.StandaloneOSX, UnityEditor.BuildOptions.None);
            UnityEditor.EditorUserBuildSettings.SwitchActiveBuildTarget(originalBuildTargetGroup, originalBuildTarget);
            var executable = System.IO.Path.GetFullPath("Build/Mac/EOSLobbySample.app");
            System.Diagnostics.Process.Start(executable);
        }
    
        [UnityEditor.MenuItem("Tools/FishyEOS/Build and Run Lobby Sample (Linux)")]
        public static void BuildAndRunLobbySceneLinux()
        {
            var originalBuildTargetGroup = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
            var originalBuildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            UnityEditor.BuildPipeline.BuildPlayer(
                new[] { "Assets/EOSLobby/Lobby.unity" }, "Build/Linux/EOSLobbySample.x86_64", 
                UnityEditor.BuildTarget.StandaloneLinux64, UnityEditor.BuildOptions.None);
            UnityEditor.EditorUserBuildSettings.SwitchActiveBuildTarget(originalBuildTargetGroup, originalBuildTarget);
            var executable = System.IO.Path.GetFullPath("Build/Linux/EOSLobbySample.x86_64");
            System.Diagnostics.Process.Start(executable);
        }
    }
}
#endif