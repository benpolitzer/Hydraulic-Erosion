using System.Collections.Generic;
using UnityEngine;

public class HydraulicErosion : MonoBehaviour
{

    [Header("Runtime UI Controls")]
    [SerializeField] private bool simulationPaused = false;
    [SerializeField] private bool showFlowLines = false;
    [Header("Flow Line Rendering")]
    [SerializeField] private Material flowLineMaterial;
    [SerializeField] private int maxVisibleFlowLines = 1000;
    [SerializeField] private float flowLineWidth = 0.35f;
    [SerializeField] private float flowLineHeightOffset = 0.75f;
    [SerializeField] private int flowLineParticleStride = 10;
    [SerializeField] private bool drawEveryParticlePath = false;

    [SerializeField] private Color flowLineStartColor = Color.blue;
    [SerializeField] private Color flowLineEndColor = Color.yellow;
    [SerializeField] private float flowLineStartAlpha = 0.25f;
    [SerializeField] private float flowLineEndAlpha = 1.0f;

    private LineRenderer[] flowLines;
    private int nextFlowLineIndex = 0;
    private Transform flowLineParent;
    public bool IsSimulationPaused => simulationPaused;
    public bool AreFlowLinesVisible => showFlowLines;

    public float persistence = 0.5f, lacunarity = 2.0f;
    public int octaves = 8;
    public float erosionRate = 0.01f, depositionRate = 0.01f, evaporationRate = 0.01f, initialWaterAmount = 0.1f;
    public int erosionIterationsPerFrame = 5;
    public int particleCount = 1000;
    public int maxParticleSteps = 50; // Maximum steps a particle can take
    public int smoothingInterval = 10;
    public float smoothingStrength = 0.1f;
    public int frameCounter = 0;
    public PlaneGenerator planeGenerator;

    private Mesh mesh;
    private Vector3[] vertices;
    private float[] sedimentAmount;
    private int width, height;
    private bool drawDebugLines = false;
    private Vector3[] startingVertices;

    void Start()
    {
        if (planeGenerator == null)
        {
            Debug.LogError("HydraulicErosion has no PlaneGenerator assigned.");
            enabled = false;
            return;
        }

        mesh = planeGenerator.GetOrCreateMesh();

        if (mesh == null)
        {
            Debug.LogError("PlaneGenerator failed to create a mesh.");
            enabled = false;
            return;
        }

        vertices = mesh.vertices;
        width = Mathf.RoundToInt(Mathf.Sqrt(vertices.Length));
        height = width;

        InitializeTerrain();
        startingVertices = (Vector3[])vertices.Clone();
        InitializeSimulation();
        CreateFlowLinePool();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            ToggleFlowLines();
        }

        if (simulationPaused)
        {
            return;
        }

        RunSimulationFrame();
    }
    private void RunSimulationFrame()
    {
        for (int i = 0; i < erosionIterationsPerFrame; i++)
        {
            SimulateParticles();
        }

        if (frameCounter > 0 && frameCounter % smoothingInterval == 0)
        {
            SmoothTerrain();
        }

        frameCounter++;

        UpdateMeshVertices();
    }
    void InitializeTerrain()
    {
        if (mesh == null)
        {
            return;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;

                float amplitude = 2f;
                float frequency = 1f;
                float noiseHeight = 1f;
                float scale = 10f;

                for (int o = 0; o < octaves; o++)
                {
                    float xCoord = (float)x / width * frequency * scale;
                    float yCoord = (float)y / height * frequency * scale;

                    noiseHeight += Mathf.PerlinNoise(xCoord, yCoord) * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                vertices[i].y = noiseHeight * 15f;
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        Debug.Log("Initialized Terrain");
    }

    void InitializeSimulation()
    {
        sedimentAmount = new float[vertices.Length];
    }

    void SimulateParticles()
    {
        for (int i = 0; i < particleCount; i++)
        {
            int startX = Random.Range(0, width);
            int startY = Random.Range(0, height);

            bool shouldDrawPath = showFlowLines &&
                                  (
                                      drawEveryParticlePath ||
                                      flowLineParticleStride <= 1 ||
                                      i % flowLineParticleStride == 0
                                  );

            SimulateParticle(startX, startY, shouldDrawPath);
        }
    }

    void SimulateParticle(int startX, int startY, bool drawPath)
    {
        int x = startX;
        int y = startY;

        float water = initialWaterAmount;
        float sediment = 0.0f;

        List<Vector3> pathPoints = null;

        if (drawPath)
        {
            pathPoints = new List<Vector3>();
            pathPoints.Add(new Vector3(x, vertices[y * width + x].y, y));
        }

        for (int step = 0; step < maxParticleSteps; step++)
        {
            int index = y * width + x;

            Vector2 steepestDir = Vector2.zero;
            float maxSlope = 0f;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        int neighborIndex = ny * width + nx;
                        float slope = vertices[index].y - vertices[neighborIndex].y;

                        if (slope > maxSlope)
                        {
                            maxSlope = slope;
                            steepestDir = new Vector2(dx, dy);
                        }
                    }
                }
            }

            if (maxSlope <= 0)
            {
                break;
            }

            x += (int)steepestDir.x;
            y += (int)steepestDir.y;

            Vector3 nextPosition = new Vector3(x, vertices[y * width + x].y, y);

            if (drawPath)
            {
                pathPoints.Add(nextPosition);
            }

            float eroded = Mathf.Min(maxSlope * erosionRate, vertices[index].y);
            vertices[index].y -= eroded;
            sediment += eroded;

            float depositAmount = Mathf.Min(sediment, depositionRate * water);
            DepositSediment(index, depositAmount);
            sediment -= depositAmount;

            water *= 1f - evaporationRate;

            if (water < 0.01f)
            {
                break;
            }
        }

        if (drawPath && pathPoints != null && pathPoints.Count > 1)
        {
            DrawFlowPath(pathPoints);
        }
    }
    private Gradient CreateFlowLineGradient()
    {
        Color startColor = flowLineStartColor;
        Color endColor = flowLineEndColor;

        startColor.a = flowLineStartAlpha;
        endColor.a = flowLineEndAlpha;

        Gradient gradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(startColor, 0f);
        colorKeys[1] = new GradientColorKey(endColor, 1f);

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(flowLineStartAlpha, 0f);
        alphaKeys[1] = new GradientAlphaKey(flowLineEndAlpha, 1f);

        gradient.SetKeys(colorKeys, alphaKeys);

        return gradient;
    }
    private void CreateFlowLinePool()
    {
        flowLines = new LineRenderer[maxVisibleFlowLines];

        flowLineParent = new GameObject("Flow Lines").transform;
        flowLineParent.SetParent(transform);

        if (flowLineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                flowLineMaterial = new Material(shader);
            }
        }

        for (int i = 0; i < maxVisibleFlowLines; i++)
        {
            GameObject lineObject = new GameObject("Flow Line " + i);
            lineObject.transform.SetParent(flowLineParent);

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = flowLineWidth;
            lineRenderer.endWidth = flowLineWidth;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 4;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;

            lineRenderer.colorGradient = CreateFlowLineGradient();

            if (flowLineMaterial != null)
            {
                lineRenderer.material = flowLineMaterial;
            }

            flowLines[i] = lineRenderer;
        }
    }

    private void DrawFlowPath(List<Vector3> localPoints)
    {
        if (!showFlowLines)
        {
            return;
        }

        if (flowLines == null || flowLines.Length == 0)
        {
            return;
        }

        LineRenderer lineRenderer = flowLines[nextFlowLineIndex];

        lineRenderer.positionCount = localPoints.Count;

        lineRenderer.startWidth = flowLineWidth;
        lineRenderer.endWidth = flowLineWidth;
        lineRenderer.colorGradient = CreateFlowLineGradient();

        if (flowLineMaterial != null)
        {
            lineRenderer.material = flowLineMaterial;
        }

        for (int i = 0; i < localPoints.Count; i++)
        {
            Vector3 point = localPoints[i];
            point.y += flowLineHeightOffset;

            Vector3 worldPoint = transform.TransformPoint(point);
            lineRenderer.SetPosition(i, worldPoint);
        }

        lineRenderer.enabled = true;

        nextFlowLineIndex++;

        if (nextFlowLineIndex >= flowLines.Length)
        {
            nextFlowLineIndex = 0;
        }
    }

    private void ClearFlowLines()
    {
        if (flowLines == null)
        {
            return;
        }

        for (int i = 0; i < flowLines.Length; i++)
        {
            if (flowLines[i] != null)
            {
                flowLines[i].enabled = false;
                flowLines[i].positionCount = 0;
            }
        }

        nextFlowLineIndex = 0;
    }
    void DepositSediment(int index, float depositAmount)
    {
        vertices[index].y += depositAmount;
    }

    void SmoothTerrain()
    {
        float[] smoothedHeights = new float[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            smoothedHeights[i] = vertices[i].y;
        }

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int index = y * width + x;

                float avgHeight = 0f;
                int count = 0;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;

                        int neighborIndex = ny * width + nx;
                        avgHeight += vertices[neighborIndex].y;
                        count++;
                    }
                }

                avgHeight /= count;
                smoothedHeights[index] = Mathf.Lerp(vertices[index].y, avgHeight, smoothingStrength);
            }
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].y = smoothedHeights[i];
        }
    }

    public void TogglePauseSimulation()
    {
        simulationPaused = !simulationPaused;
    }

    public void StepSimulation()
    {
        RunSimulationFrame();
    }
    public void ResetErosion()
    {
        if (startingVertices == null)
        {
            Debug.LogWarning("Cannot reset erosion because startingVertices was never saved.");
            return;
        }

        vertices = (Vector3[])startingVertices.Clone();

        frameCounter = 0;

        InitializeSimulation();
        ClearFlowLines();
        UpdateMeshVertices();
    }

    public void ToggleFlowLines()
    {
        showFlowLines = !showFlowLines;

        if (!showFlowLines)
        {
            ClearFlowLines();
        }

        Debug.Log("Show Flow Lines: " + showFlowLines);
    }

    public void SetErosionRate(float value)
    {
        erosionRate = value;
    }

    public void SetDepositionRate(float value)
    {
        depositionRate = value;
    }

    public void SetInitialWaterAmount(float value)
    {
        initialWaterAmount = value;
    }

    public void SetParticleCount(float value)
    {
        particleCount = Mathf.RoundToInt(value);
    }

    public void SetMaxParticleSteps(float value)
    {
        maxParticleSteps = Mathf.RoundToInt(value);
    }

    public void SetErosionIterationsPerFrame(float value)
    {
        erosionIterationsPerFrame = Mathf.RoundToInt(value);
    }

    void UpdateMeshVertices()
    {
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}