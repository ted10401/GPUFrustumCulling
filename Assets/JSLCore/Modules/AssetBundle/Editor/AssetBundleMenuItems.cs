using UnityEngine;
using UnityEditor;

namespace JSLCore.AssetBundle
{
    public class AssetBundleMenuItems
    {
        private const string ASSETBUNDLE_CLEAR_CACHE_NAME = "JSLCore/AssetBundles/Clear Cache";
        private const string ASSETBUNDLE_EDITOR_LOAD_TYPE_SIMULATE = "JSLCore/AssetBundles/Editor Load Type/Simulate";
        private const string ASSETBUNDLE_EDITOR_LOAD_TYPE_STREAMING = "JSLCore/AssetBundles/Editor Load Type/Streaming";
        private const string ASSETBUNDLE_EDITOR_LOAD_TYPE_NETWORK = "JSLCore/AssetBundles/Editor Load Type/Network";

        [MenuItem(ASSETBUNDLE_CLEAR_CACHE_NAME, priority = 5)]
        private static void ClearCache()
        {
            if (Caching.ClearCache())
            {
                JSLDebug.Log("Cleaned all caches successfully.");
            }
            else
            {
                JSLDebug.LogWarning("Failed to clean caches.");
            }
        }

        [MenuItem(ASSETBUNDLE_EDITOR_LOAD_TYPE_SIMULATE, false)]
        private static void SwitchLoadModeToSimulate()
        {
            AssetBundleDef.SetAssetBundleLoadType(AssetBundleLoadType.Simulate);
        }

        [MenuItem(ASSETBUNDLE_EDITOR_LOAD_TYPE_SIMULATE, true)]
        private static bool SwitchLoadModeToSimulateValidate()
        {
            AssetBundleLoadType assetBundleLoadType = AssetBundleDef.GetAssetBundleLoadType();
            Menu.SetChecked(ASSETBUNDLE_EDITOR_LOAD_TYPE_SIMULATE, assetBundleLoadType == AssetBundleLoadType.Simulate);
            return assetBundleLoadType != AssetBundleLoadType.Simulate;
        }

        [MenuItem(ASSETBUNDLE_EDITOR_LOAD_TYPE_STREAMING, false)]
        private static void SwitchLoadModeToStreaming()
        {
            AssetBundleDef.SetAssetBundleLoadType(AssetBundleLoadType.Streaming);
        }

        [MenuItem(ASSETBUNDLE_EDITOR_LOAD_TYPE_STREAMING, true)]
        private static bool SwitchLoadModeToStreamingValidate()
        {
            AssetBundleLoadType assetBundleLoadType = AssetBundleDef.GetAssetBundleLoadType();
            Menu.SetChecked(ASSETBUNDLE_EDITOR_LOAD_TYPE_STREAMING, assetBundleLoadType == AssetBundleLoadType.Streaming);
            return assetBundleLoadType != AssetBundleLoadType.Streaming;
        }

        [MenuItem(ASSETBUNDLE_EDITOR_LOAD_TYPE_NETWORK, false)]
        private static void SwitchLoadModeToNetwork()
        {
            AssetBundleDef.SetAssetBundleLoadType(AssetBundleLoadType.Network);
        }

        [MenuItem(ASSETBUNDLE_EDITOR_LOAD_TYPE_NETWORK, true)]
        private static bool SwitchLoadModeToNetworkValidate()
        {
            AssetBundleLoadType assetBundleLoadType = AssetBundleDef.GetAssetBundleLoadType();
            Menu.SetChecked(ASSETBUNDLE_EDITOR_LOAD_TYPE_NETWORK, assetBundleLoadType == AssetBundleLoadType.Network);
            return assetBundleLoadType != AssetBundleLoadType.Network;
        }
    }
}
