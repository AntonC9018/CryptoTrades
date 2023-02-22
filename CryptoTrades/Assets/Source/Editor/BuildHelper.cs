using System;
using UnityEditor;

namespace Build
{
    public static class BuildHelper
    {
        public static void BuildWindows()
        {
            string outputDirectory = Console.ReadLine();
            string[] scenes = {"Assets/Main/MainScene.unity"};
            BuildPipeline.BuildPlayer(scenes, outputDirectory + "/Trades.exe", BuildTarget.StandaloneWindows, BuildOptions.None);
        }
    }
}