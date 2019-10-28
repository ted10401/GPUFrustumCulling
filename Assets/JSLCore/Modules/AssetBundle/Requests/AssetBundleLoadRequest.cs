using System.Collections;

namespace JSLCore.AssetBundle
{
    public abstract class AssetBundleLoadRequest : IEnumerator
    {
        public object Current
        {
            get
            {
                return null;
            }
        }

        public bool MoveNext()
        {
            return !IsDone();
        }

        public void Reset()
        {
            
        }

        public abstract bool Update();
        public abstract bool IsDone();
        public abstract float GetProgress();
    }

}