using System.IO;
using UnityEngine;
using UnityEditor;

namespace JSLCore.AssetBundle
{
    public class AssetBundleNameBuilder
    {
        private static string ASSET_BUNDLE_RESOURCE_FOLDER = "AssetBundleResources/";

        public static void Build()
        {
            string assetPath = Path.Combine(Application.dataPath, ASSET_BUNDLE_RESOURCE_FOLDER);
            DirectoryInfo directoryInfo = new DirectoryInfo(assetPath);

            if (!directoryInfo.Exists)
            {
                return;
            }

            SetAssetBundleNames(directoryInfo);

            AssetDatabase.Refresh();
        }

        private static void SetAssetBundleNames(DirectoryInfo directoryInfo)
        {
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            for (int i = 0; i < fileInfos.Length; i++)
            {
                if (fileInfos[i].Extension == ".meta")
                {
                    continue;
                }

                SetAssetBundleName(directoryInfo, fileInfos[i]);
            }

            DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();
            if (directoryInfos.Length != 0)
            {
                for (int i = 0; i < directoryInfos.Length; i++)
                {
                    SetAssetBundleNames(directoryInfos[i]);
                }
            }
        }

        private static void SetAssetBundleName(DirectoryInfo directoryInfo, FileInfo fileInfo)
        {
            string assetBundleFolderPath = Application.dataPath + "/" + ASSET_BUNDLE_RESOURCE_FOLDER;
            assetBundleFolderPath = assetBundleFolderPath.Replace("/", "\\");

            string assetbundleName = directoryInfo.FullName;
            assetbundleName = assetbundleName.Replace(assetBundleFolderPath, "");
            assetbundleName = Path.Combine(assetbundleName, fileInfo.Name.Replace(fileInfo.Extension, ""));
            assetbundleName = assetbundleName.ToLower();

            string filePath = fileInfo.FullName;
            filePath = filePath.Replace(Application.dataPath.Replace("/", "\\"), "");
            filePath = "Assets" + filePath;
            JSLDebug.Log(filePath);

            AssetImporter.GetAtPath(filePath).assetBundleName = assetbundleName;
        }
    }
}