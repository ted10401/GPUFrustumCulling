using UnityEditor;
using UnityEngine;

namespace JSLCore.AssetBundle
{
    public class AssetBundleBuildWindow : EditorWindow
    {
        [MenuItem("JSLCore/AssetBundles/AssetBundle Build Window", priority = 1)]
        private static void OpenWindow()
        {
            AssetBundleBuildWindow window = (AssetBundleBuildWindow)GetWindow(typeof(AssetBundleBuildWindow), false, "AssetBundle Build Window");
            window.Show();
        }

        private enum CompressionType
        {
            Uncompression,
            StandardCompression,
            ChunkBasedCompression
        }

        private Vector2 m_scrollPosition;
        private void OnGUI()
        {
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.Space();

            OnDrawOutputInformations();
            OnDrawOutputOptions();
            OnDrawAssetBundleBuildOptions();
            OnDrawBuildButton();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        protected BuildTarget m_buildTarget;
        protected string m_outputPath;
        private bool m_showOutputInformations = true;

        private void OnDrawOutputInformations()
        {
            m_showOutputInformations = EditorGUILayout.Foldout(m_showOutputInformations, "Output Informations");
            if (!m_showOutputInformations)
            {
                return;
            }

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;

            m_buildTarget = EditorUserBuildSettings.activeBuildTarget;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Active Build Target");
            EditorGUILayout.LabelField(m_buildTarget.ToString());
            EditorGUILayout.EndHorizontal();

            m_outputPath = AssetBundleDef.GetDefaultOutputPath();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Output Path");
            EditorGUILayout.LabelField(m_outputPath);
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel = indent;

            EditorGUILayout.Space();
        }

        private bool m_showOutputOptions = true;
        protected bool m_clearFolders;
        protected bool m_copyToStreamingAssets;

        private void OnDrawOutputOptions()
        {
            m_showOutputOptions = EditorGUILayout.Foldout(m_showOutputOptions, "Output Options");
            if (!m_showOutputOptions)
            {
                return;
            }

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;

            m_clearFolders = EditorGUILayout.ToggleLeft("Clear Folders", m_clearFolders);
            m_copyToStreamingAssets = EditorGUILayout.ToggleLeft("Copy to StreamingAssets", m_copyToStreamingAssets);

            EditorGUI.indentLevel = indent;

            EditorGUILayout.Space();
        }

        private bool m_showAssetBundleBuildOptions = true;
        private GUIContent m_compressionContent = new GUIContent("Compression", "Choose no compress, standard (LZMA), or chunk based (LZ4)");
        private GUIContent[] m_compressionOptions =
        {
            new GUIContent("No Compression"),
            new GUIContent("Standard Compression (LZMA)"),
            new GUIContent("Chunk Based Compression (LZ4)")
        };
        private int[] m_compressionValues = { 0, 1, 2 };
        private int m_compressionType = 1;
        private bool m_disableWriteTypeTree;
        private bool m_forceRebuild;
        private bool m_ignoreTypeTreeChanges;
        private bool m_appendHashToAssetBundleName;
        private bool m_strictMode;
        private bool m_dryRunBuild;
        protected BuildAssetBundleOptions m_buildAssetBundleOptions;

        private void OnDrawAssetBundleBuildOptions()
        {
            m_showAssetBundleBuildOptions = EditorGUILayout.Foldout(m_showAssetBundleBuildOptions, "AssetBundle Build Options");
            if (!m_showAssetBundleBuildOptions)
            {
                return;
            }

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;

            m_compressionType = EditorGUILayout.IntPopup(m_compressionContent, m_compressionType, m_compressionOptions, m_compressionValues);
            m_disableWriteTypeTree = EditorGUILayout.ToggleLeft("Disable Write Type Tree", m_disableWriteTypeTree);
            m_forceRebuild = EditorGUILayout.ToggleLeft("Force Rebuild", m_forceRebuild);
            m_ignoreTypeTreeChanges = EditorGUILayout.ToggleLeft("Ignore Type Tree Changes", m_ignoreTypeTreeChanges);
            m_appendHashToAssetBundleName = EditorGUILayout.ToggleLeft("Append Hash To AssetBundle", m_appendHashToAssetBundleName);
            m_strictMode = EditorGUILayout.ToggleLeft("Strict Mode", m_strictMode);
            m_dryRunBuild = EditorGUILayout.ToggleLeft("Dry Run Build", m_dryRunBuild);

            EditorGUI.indentLevel = indent;

            EditorGUILayout.Space();
        }

        private void OnDrawBuildButton()
        {
            if (GUILayout.Button("Build"))
            {
                m_buildAssetBundleOptions = BuildAssetBundleOptions.None;
                CompressionType compressionType = (CompressionType)m_compressionType;
                if (compressionType == CompressionType.Uncompression)
                {
                    m_buildAssetBundleOptions |= BuildAssetBundleOptions.UncompressedAssetBundle;
                }
                else if (compressionType == CompressionType.ChunkBasedCompression)
                {
                    m_buildAssetBundleOptions |= BuildAssetBundleOptions.ChunkBasedCompression;
                }

                if (m_disableWriteTypeTree)
                {
                    m_buildAssetBundleOptions |= BuildAssetBundleOptions.DisableWriteTypeTree;
                }

                if (m_forceRebuild)
                {
                    m_buildAssetBundleOptions |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
                }

                if (m_ignoreTypeTreeChanges)
                {
                    m_buildAssetBundleOptions |= BuildAssetBundleOptions.IgnoreTypeTreeChanges;
                }

                if (m_appendHashToAssetBundleName)
                {
                    m_buildAssetBundleOptions |= BuildAssetBundleOptions.AppendHashToAssetBundleName;
                }

                if (m_strictMode)
                {
                    m_buildAssetBundleOptions |= BuildAssetBundleOptions.StrictMode;
                }

                if (m_dryRunBuild)
                {
                    m_buildAssetBundleOptions |= BuildAssetBundleOptions.DryRunBuild;
                }

                BuildAssetBundles();
            }
        }

        protected virtual void BuildAssetBundles()
        {
            AssetBundleBuilder.Build(new AssetBundleBuildInfo(m_clearFolders, m_copyToStreamingAssets, m_buildAssetBundleOptions));
        }
    }
}
