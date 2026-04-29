using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
public class PlaneGenerator : MonoBehaviour
{
    [SerializeField] private int width = 256;
    private int height;

    public Mesh GeneratedMesh { get; private set; }

    private void Awake()
    {
        height = width;
        CreatePlane();
    }

    public Mesh GetOrCreateMesh()
    {
        if (GeneratedMesh == null)
        {
            height = width;
            CreatePlane();
        }

        return GeneratedMesh;
    }

    private void CreatePlane()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Generated Plane Mesh";

        // Needed for 256 x 256 = 65,536 vertices.
        // Unity's default mesh index format is 16-bit, which is too small here.
        mesh.indexFormat = IndexFormat.UInt32;

        int vertexCount = width * height;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        int[] triangles = new int[(width - 1) * (height - 1) * 6];

        float xScale = 1f / (width - 1);
        float zScale = 1f / (height - 1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;

                float xPos = x * xScale * (width - 1);
                float zPos = y * zScale * (height - 1);

                vertices[i] = new Vector3(xPos, 0f, zPos);
                uv[i] = new Vector2(x * xScale, y * zScale);
            }
        }

        int t = 0;

        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                int i = y * width + x;

                triangles[t++] = i;
                triangles[t++] = i + width;
                triangles[t++] = i + 1;

                triangles[t++] = i + 1;
                triangles[t++] = i + width;
                triangles[t++] = i + width + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        GeneratedMesh = mesh;
    }
}