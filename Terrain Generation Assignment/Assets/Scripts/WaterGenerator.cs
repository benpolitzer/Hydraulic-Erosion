using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterGenerator : MonoBehaviour
{
    public float waveHeight = 0.5f;
    public float waveScale = 0.1f;   
    public float waveSpeed = 1.0f;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
    }

    void Update()
    {
        AnimateWaves();
    }

    private void AnimateWaves()
    {
        float time = Time.time * waveSpeed;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vertex = originalVertices[i];

            float perlinX = (vertex.x * waveScale) + time;
            float perlinZ = (vertex.z * waveScale) + time;
            float wave = Mathf.PerlinNoise(perlinX, perlinZ) * waveHeight;

            displacedVertices[i] = new Vector3(vertex.x, wave, vertex.z);
        }

        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();  
        mesh.RecalculateBounds();  
    }
}