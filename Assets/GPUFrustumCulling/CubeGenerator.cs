using UnityEngine;

public class CubeGenerator : MonoBehaviour
{
    public Transform parent;
    public GameObject cubePrefab;
    public int generateCount = 1;
    public Vector2 generateRadius = new Vector2(0f, 1f);

    private void Update()
    {
        if(Input.GetKey(KeyCode.Space))
        {
            Generate();
        }
    }

    [ContextMenu("Generate")]
    private void Generate()
    {
        Vector3 position = Vector3.zero;
        GameObject instance = null;
        for (int i = 0; i < generateCount; i++)
        {
            position = Vector3.forward * Random.Range(generateRadius.x, generateRadius.y);
            position = Quaternion.Euler(0, Random.Range(0, 360), 0) * position;
            position.y = cubePrefab.transform.position.y;
            instance = Instantiate(cubePrefab, parent.transform);
            instance.transform.localPosition = position;
            instance.transform.rotation = Random.rotation;
        }
    }
}
