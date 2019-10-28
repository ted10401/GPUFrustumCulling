using UnityEngine;
using System.IO;

namespace JSLCore.AssetBundle
{
    public class AssetBundleDef
    {
        public const string ASSET_BUNDLE_OUTPUT_FOLDER = "AssetBundles";
        public const string CATALOG_FILE_NAME = "catalog.txt";
        private const string ASSETBUNDLE_LOAD_TYPE_KEY = "AssetBundleLoadType";

        public static string GetDefaultOutputPath()
        {
            return GetOutputPath(GetPlatformName());
        }

        public static string GetOutputPath(string buildTarget)
        {
            string outputPath = Path.Combine(Application.dataPath.Replace("Assets", ""), ASSET_BUNDLE_OUTPUT_FOLDER);
            outputPath = Path.Combine(outputPath, buildTarget);

            return outputPath;
        }

        public static AssetBundleLoadType GetAssetBundleLoadType()
        {
#if UNITY_EDITOR
            int loadType = UnityEditor.EditorPrefs.GetInt(ASSETBUNDLE_LOAD_TYPE_KEY, (int)AssetBundleLoadType.Simulate);
            return (AssetBundleLoadType)loadType;
#elif ASSETBUNDLE_LOAD_TYPE_STREAMING
            return AssetBundleLoadType.Streaming;
#else
            return AssetBundleLoadType.Network;
#endif
        }

#if UNITY_EDITOR
        public static void SetAssetBundleLoadType(AssetBundleLoadType loadType)
        {
            UnityEditor.EditorPrefs.SetInt(ASSETBUNDLE_LOAD_TYPE_KEY, (int)loadType);
        }
#endif

        public static string GetStreamingAssetsPath()
        {
            return Application.streamingAssetsPath + "/AssetBundles";
        }

        public static string GetDownloadStreamingAssetsPath()
        {
#if UNITY_EDITOR || !UNITY_ANDROID
            return "file://" + Application.streamingAssetsPath + "/AssetBundles";
#else
            return Application.streamingAssetsPath + "/AssetBundles";
#endif
        }

        public static string GetPlatformName()
        {
            string platformName = null;

#if UNITY_EDITOR
            switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget)
            {
                case UnityEditor.BuildTarget.StandaloneLinux:
                case UnityEditor.BuildTarget.StandaloneLinux64:
                case UnityEditor.BuildTarget.StandaloneLinuxUniversal:
                    platformName = "StandaloneLinux";
                    break;
                case UnityEditor.BuildTarget.StandaloneOSX:
                    platformName = "StandaloneOSX";
                    break;
                case UnityEditor.BuildTarget.StandaloneWindows:
                case UnityEditor.BuildTarget.StandaloneWindows64:
                    platformName = "StandaloneWindows";
                    break;
                case UnityEditor.BuildTarget.Android:
                    platformName = "Android";
                    break;
                case UnityEditor.BuildTarget.iOS:
                    platformName = "iOS";
                    break;
            }
#else
            switch (Application.platform)
            {
                case RuntimePlatform.LinuxPlayer:
                    platformName = "StandaloneLinux";
                    break;
                case RuntimePlatform.OSXPlayer:
                    platformName = "StandaloneOSX";
                    break;
                case RuntimePlatform.WindowsPlayer:
                    platformName = "StandaloneWindows";
                    break;
                case RuntimePlatform.Android:
                    platformName = "Android";
                    break;
                case RuntimePlatform.IPhonePlayer:
                    platformName = "iOS";
                    break;
            }
#endif

            return platformName;
        }
    }
}
