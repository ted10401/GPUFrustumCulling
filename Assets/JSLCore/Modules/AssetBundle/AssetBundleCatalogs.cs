using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace JSLCore.AssetBundle
{
    public class AssetBundleCatalogs
    {
        private AssetBundleCatalog[] m_catalogs;

        public AssetBundleCatalogs(string text)
        {
            string[] separator = new string[] { "\n" };
            string[] splits = text.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);

            m_catalogs = new AssetBundleCatalog[splits.Length / 4];
            for (int i = 0; i < m_catalogs.Length; i++)
            {
                m_catalogs[i] = new AssetBundleCatalog(splits[4 * i], Hash128.Parse(splits[4 * i + 1]), uint.Parse(splits[4 * i + 2]), float.Parse(splits[4 * i + 3]));
            }
        }

        public uint GetCrc(string assetBundleName)
        {
            for (int i = 0; i < m_catalogs.Length; i++)
            {
                if (m_catalogs[i].assetBundleName == assetBundleName)
                {
                    return m_catalogs[i].crc;
                }
            }

            return 0;
        }

        public float GetFileSize(string assetBundleName)
        {
            for (int i = 0; i < m_catalogs.Length; i++)
            {
                if (m_catalogs[i].assetBundleName == assetBundleName)
                {
                    return m_catalogs[i].fileSize;
                }
            }

            return 0;
        }

        public float GetAllFileSize(List<string> assetBundleNames)
        {
            IEnumerable<AssetBundleCatalog> catalogs = m_catalogs.Where(a => assetBundleNames.Contains(a.assetBundleName));
            return catalogs.Sum(a => a.fileSize);
        }
    }


    public class AssetBundleCatalog
    {
        public string assetBundleName;
        public Hash128 hash;
        public uint crc;
        public float fileSize;

        public AssetBundleCatalog(string assetBundleName, Hash128 hash, uint crc, float fileSize)
        {
            this.assetBundleName = assetBundleName;
            this.hash = hash;
            this.crc = crc;
            this.fileSize = fileSize;
        }
    }
}