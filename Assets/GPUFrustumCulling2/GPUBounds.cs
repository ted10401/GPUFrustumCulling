using UnityEngine;
using System;

[Serializable]
public class GPUBounds
{
    public bool valid;
    public bool isStatic;
    public Renderer renderer;
    public Collider collider;
    public Vector3 center;
    public Vector3 extents;
    public uint visible;

    public void SetStatic(bool value)
    {
        isStatic = value;
    }

    public void SetVisible(uint value)
    {
        if(visible == value)
        {
            return;
        }

        visible = value;
        renderer.enabled = visible == 1;
    }

    private uint m_cacheVisible;
    public void SetVisible(bool value)
    {
        m_cacheVisible = (uint)(value ? 1 : 0);
        if (visible == m_cacheVisible)
        {
            return;
        }

        visible = m_cacheVisible;
        renderer.enabled = visible == 1;
    }
}
