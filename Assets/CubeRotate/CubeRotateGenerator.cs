using UnityEngine;

public class CubeRotateGenerator : MonoBehaviour
{
    public GameObject generatePrefab;
    public int generateCount = 10;
    public Vector2 generateRadius = new Vector2(0f, 1f);
    public CubeRotateComputeShader cubeRotateComputeShader;
    private GameObject m_parent;
    private Transform[] m_cubes;

    private void Awake()
    {
        Generate();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            Generate();
        }
    }

    private void Generate()
    {
        if(!Application.isPlaying)
        {
            return;
        }

        if(m_parent != null)
        {
            GameObject.Destroy(m_parent);
        }

        m_parent = new GameObject("Parent");
        m_cubes = new Transform[generateCount];

        Vector3 position = Vector3.zero;
        GameObject instance = null;
        for (int i = 0; i < generateCount; i++)
        {
            position = Vector3.forward * Random.Range(generateRadius.x, generateRadius.y);
            position = Quaternion.Euler(0, Random.Range(0, 360), 0) * position;
            instance = Instantiate(generatePrefab, position, Random.rotation, m_parent.transform);
            m_cubes[i] = instance.transform;
        }

        cubeRotateComputeShader.CreateComputeBuffer(m_cubes);
    }
}
