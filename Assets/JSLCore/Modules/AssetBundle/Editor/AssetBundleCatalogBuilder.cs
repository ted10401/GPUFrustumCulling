using UnityEngine;
using UnityEditor;
using System.IO;

namespace JSLCore.AssetBundle
{
    public class AssetBundleCatalogBuilder
    {
        public static void Build(string outputPath)
        {
            string[] allAssetBundleNames = AssetDatabase.GetAllAssetBundleNames();

            string fullPath;
            Hash128 hash128;
            uint crc = 0;
            FileInfo fileInfo;

            string catalogContents = string.Empty;
            for (int i = 0; i < allAssetBundleNames.Length; i++)
            {
                string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(allAssetBundleNames[i]);
                if (assetPaths.Length == 0)
                {
                    JSLDebug.LogErrorFormat("[AssetBundleCatalogBuilder] - The AssetBundle '{0}' is empty. It might be a folder, need to check it again!", allAssetBundleNames[i]);
                    continue;
                }

                fullPath = outputPath + "/" + allAssetBundleNames[i];

                if (!BuildPipeline.GetCRCForAssetBundle(fullPath, out crc))
                {
                    JSLDebug.LogErrorFormat("[AssetBundleCatalogBuilder] - Failed to get CRC from {0}", fullPath);
                    continue;
                }

                if (!BuildPipeline.GetHashForAssetBundle(fullPath, out hash128))
                {
                    JSLDebug.LogErrorFormat("[AssetBundleCatalogBuilder] - Failed to get Hash128 from {0}", fullPath);
                    continue;
                }

                fileInfo = new FileInfo(fullPath);
                if (!fileInfo.Exists)
                {
                    JSLDebug.LogErrorFormat("[AssetBundleCatalogBuilder] - Failed to get file size from {0}", fullPath);
                    continue;
                }

                if (i != 0)
                {
                    catalogContents += System.Environment.NewLine;
                }

                catalogContents += allAssetBundleNames[i];
                catalogContents += System.Environment.NewLine;
                catalogContents += System.Convert.ToString(hash128);
                catalogContents += System.Environment.NewLine;
                catalogContents += crc.ToString();
                catalogContents += System.Environment.NewLine;
                catalogContents += GetFileSizeInB(fileInfo.Length).ToString();
            }

            string catalogPath = Path.Combine(outputPath, AssetBundleDef.CATALOG_FILE_NAME);

            File.WriteAllText(catalogPath, catalogContents);

            JSLDebug.LogFormat("[AssetBundleCatalogBuilder] - Build catalog.txt success in {0}", catalogPath);
        }

        private static float GetFileSizeInB(long length)
        {
            return Mathf.Round(length * 100) / 100;
        }
    }
}
