using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PhoenixRace.Editor.SpaceViewport
{
    //负责保存红台座舷窗预制体并构建独立的 Windows 资源包。
    public static class PhoenixViewportBundleBuild
    {
        internal const string PrefabPath = "Assets/Prefabs/PhoenixRace/PhoenixSpaceViewportUI.prefab";
        internal const string ArtPath = "Assets/Art/UI/RedPedestalUI";
        private const string BundleName = "phoenixraceviewportui.ab";

        //供 Unity 批处理调用，只编译舷窗资源，不运行场景或游戏。
        public static void Build()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                PhoenixViewportPrefabComposer.Compose();
                AssetDatabase.SaveAssets();
                string output = GetArgument("-phoenixBundleOutput");
                Directory.CreateDirectory(output);
                AssetBundleBuild definition = new AssetBundleBuild
                {
                    assetBundleName = BundleName,
                    assetNames = new[] { PrefabPath }
                };
                AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(output,
                    new[] { definition }, BuildAssetBundleOptions.ChunkBasedCompression |
                    BuildAssetBundleOptions.StrictMode, BuildTarget.StandaloneWindows64);
                if (manifest == null)
                {
                    throw new InvalidOperationException("舷窗资源包构建失败，未返回构建清单。");
                }
                if (manifest.GetAllDependencies(BundleName).Length != 0)
                {
                    throw new InvalidOperationException("舷窗资源包存在外部 AB 依赖，无法作为独立资源交付。");
                }
                Debug.Log("PHOENIX_VIEWPORT_BUILD_SUCCESS: " + Path.Combine(output, BundleName));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        //读取显式指定的构建输出目录，缺失参数时直接报告错误。
        private static string GetArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == name)
                {
                    return Path.GetFullPath(arguments[i + 1]);
                }
            }
            throw new ArgumentException("缺少构建参数：" + name);
        }
    }
}
