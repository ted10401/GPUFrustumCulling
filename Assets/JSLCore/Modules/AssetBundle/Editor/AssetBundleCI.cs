using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JSLCore.AssetBundle
{
    public class AssetBundleCI
    {
        [MenuItem("Assets/CI/AssetBundles/Build Specific AssetBundle", false, 0)]
        private static void BuildSpecificAssetBundle()
        {
            Object[] assets = Selection.objects.Where(o => !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(o))).ToArray();
            HashSet<string> processedBundles = new HashSet<string>();

            List<AssetBundleBuild> assetBundleBuilds = GetBuildsForPaths(assets, processedBundles);

            foreach (Object obj in assets)
            {
                var paths = AssetDatabase.GetDependencies(new[] { AssetDatabase.GetAssetPath(obj) });
                assetBundleBuilds = assetBundleBuilds.Concat(GetBuildsForPaths(paths.Select(p => AssetDatabase.LoadAssetAtPath<Object>(p)).ToArray(), processedBundles)).ToList();
            }

            AssetBundleBuilder.Build(new AssetBundleBuildInfo(false, true, BuildAssetBundleOptions.None, assetBundleBuilds.ToArray()));
        }

        private static List<AssetBundleBuild> GetBuildsForPaths(Object[] assets, HashSet<string> processedBundles)
        {
            List<AssetBundleBuild> assetBundleBuilds = new List<AssetBundleBuild>();

            // Get asset bundle names from selection
            foreach (var o in assets)
            {
                var assetPath = AssetDatabase.GetAssetPath(o);
                var importer = AssetImporter.GetAtPath(assetPath);

                if (importer == null)
                {
                    continue;
                }

                // Get asset bundle name & variant
                var assetBundleName = importer.assetBundleName;
                var assetBundleVariant = importer.assetBundleVariant;
                var assetBundleFullName = string.IsNullOrEmpty(assetBundleVariant) ? assetBundleName : assetBundleName + "." + assetBundleVariant;

                // Only process assetBundleFullName once. No need to add it again.
                if (processedBundles.Contains(assetBundleFullName))
                {
                    continue;
                }

                processedBundles.Add(assetBundleFullName);

                AssetBundleBuild build = new AssetBundleBuild();

                build.assetBundleName = assetBundleName;
                build.assetBundleVariant = assetBundleVariant;
                build.assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleFullName);

                assetBundleBuilds.Add(build);
            }

            return assetBundleBuilds;
        }
    }
}
