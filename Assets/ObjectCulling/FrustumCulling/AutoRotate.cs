using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    public float rotateSpeed = 30f;
    private Transform m_transform;

    private void Awake()
    {
        m_transform = transform;
    }

    private void Update()
    {
        m_transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    }
}
