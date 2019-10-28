using UnityEditor;
using System.IO;
using UnityEngine;

namespace JSLCore.AssetBundle
{
    public class AssetBundleBuilder
    {
        private const string STREAMING_ASSETS_FOLDER = "StreamingAssets";
        private const string STREAMING_ASSETS_FOLDER_PATH = "Assets/StreamingAssets/";

        public static void Build(AssetBundleBuildInfo buildInfo)
        {
            AssetBundleNameBuilder.Build();

            AssetDatabase.Refresh();
            AssetDatabase.RemoveUnusedAssetBundleNames();

            if (buildInfo.cleanFolders)
            {
                if (Directory.Exists(buildInfo.outputPath))
                {
                    Directory.Delete(buildInfo.outputPath, true);
                }
            }

            if (!Directory.Exists(buildInfo.outputPath))
            {
                Directory.CreateDirectory(buildInfo.outputPath);
            }

            if (buildInfo.specificAssetBundles == null || buildInfo.specificAssetBundles.Length == 0)
            {
                BuildPipeline.BuildAssetBundles(buildInfo.outputPath, buildInfo.buildAssetBundleOptions, buildInfo.buildTarget);
            }
            else
            {
                BuildPipeline.BuildAssetBundles(buildInfo.outputPath, buildInfo.specificAssetBundles, buildInfo.buildAssetBundleOptions, buildInfo.buildTarget);
            }

            AssetBundleCatalogBuilder.Build(buildInfo.outputPath);

            if (buildInfo.copyToStreamingAssets)
            {
                string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, AssetBundleDef.ASSET_BUNDLE_OUTPUT_FOLDER);
                streamingAssetsPath = Path.Combine(streamingAssetsPath, AssetBundleDef.GetPlatformName());

                if (Directory.Exists(streamingAssetsPath))
                {
                    Directory.Delete(streamingAssetsPath, true);
                }

                DirectoryCopy(buildInfo.outputPath, streamingAssetsPath);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            JSLDebug.Log("Build AssetBundles successfully.");
        }

#if UNITY_IOS
        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            foreach (string folderPath in Directory.GetDirectories(sourceDirName, "*", SearchOption.AllDirectories))
            {
                if (!Directory.Exists(folderPath.Replace(sourceDirName, destDirName)))
                {
                    Directory.CreateDirectory(folderPath.Replace(sourceDirName, destDirName));
                }
            }

            foreach (string filePath in Directory.GetFiles(sourceDirName, "*.*", SearchOption.AllDirectories))
            {
                string newFilePath = Path.Combine(Path.GetDirectoryName(filePath).Replace(sourceDirName, destDirName), Path.GetFileName(filePath));
                File.Copy(filePath, newFilePath, true);
            }
        }
#else
        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirName);

            if (!directoryInfo.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
            }

            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }
            
            FileInfo[] files = directoryInfo.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();
            foreach (DirectoryInfo subdir in directoryInfos)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath);
            }
        }
#endif
    }
}