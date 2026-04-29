using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TerrainGenerator : MonoBehaviour
{
    public float sandHeight = 0.3f, grassHeight = 0.4f, rockHeight = 0.7f;
    public float offsetX, offsetY;
    public int sandIndex = 2, grassIndex = 0, rockIndex = 1;
    public Slider heightSlider, compSlider, otherSlider;
    public Terrain terrain;
    public Toggle heightMapToggle;
    public Texture2D heightMap, falloffMap;
    public RawImage noiseDisplay; 
    public GameObject settings, toggleSettings, camSettings;

    private Texture2D noiseTexture;
    private bool isSliderBeingUsed = false;


    void Start()
    {
        offsetX = Random.Range(0f, 100000f);
        offsetY = Random.Range(0f, 100000f);

        heightSlider.onValueChanged.AddListener(delegate { isSliderBeingUsed = true; });
        compSlider.onValueChanged.AddListener(delegate { isSliderBeingUsed = true; });
        //otherSlider.onValueChanged.AddListener(delegate { isSliderBeingUsed = true; });

        GenerateTerrain(true);
        PaintTerrain();

    }
    private void Update()
    {
        if (isSliderBeingUsed && Input.GetMouseButtonUp(0))
        {
            isSliderBeingUsed = false;
            GenerateTerrain(true);
            PaintTerrain();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            camSettings.SetActive(!camSettings.activeSelf);
            settings.SetActive(false);
            toggleSettings.SetActive(false);
            if (!camSettings.activeSelf) 
            {
                toggleSettings.SetActive(true);
            }
        }
    }
    public void GenerateTerrain(bool type)
    {
        if (!type)
        {
            GenerateFromHeightMap();
        }
        else
        {
            GenerateFromNoise();
        }
    }
    private void GenerateFromNoise()
    {
        TerrainData data = terrain.terrainData;
        int size = data.heightmapResolution;
        float[,] heights = new float[size, size];
        noiseTexture = new Texture2D(size, size);

        //define center of terrain
        Vector2 center = new Vector2(size / 2, size / 2);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noiseValue = 0f;

                //generate noise using multiple octaves of perlin noise
                float frequencyMultiplier = compSlider.value; 
                for (int i = 1; i <= 50; i *= 2)
                {
                    noiseValue += (1f / i) * Mathf.PerlinNoise(
                        x * frequencyMultiplier * (float)i / (size / 4) + offsetX,
                        y * frequencyMultiplier * (float)i / (size / 4) + offsetY
                    );
                }

                //calculate normalized distance from center of the terrain
                float normalizedX = (x - size / 2f) / (size / 2f);
                float normalizedY = (y - size / 2f) / (size / 2f);
                float distanceFromCenter = Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);

                //normalize distance
                float edgeDistance = Mathf.Clamp01(distanceFromCenter);

                //apply a falloff effect based on distance to edge
                float falloffSharpness = Mathf.Lerp(0.3f, 8f, 0.3f);
                float falloff = Mathf.Clamp01(1f - Mathf.Pow(edgeDistance, falloffSharpness));
                noiseValue *= falloff;

                //apply height adjustment to reduce terrain height near edges
                float heightAdjustment = heightSlider.value * (1f - edgeDistance);
                noiseValue = Mathf.Max(noiseValue - heightAdjustment, 0.2f);

                //increase contrast of terrain height using a power curve
                noiseValue = Mathf.Pow(noiseValue, 1.55f);

                //store calculated height value
                heights[x, y] = noiseValue;

                //clamp noise value for texture generation
                noiseValue = Mathf.Clamp01(noiseValue);
                Color color = new Color(noiseValue, noiseValue, noiseValue);

                //set corresponding pixel in noise texture
                noiseTexture.SetPixel(x, y, color);
            }
        }

        //apply heights to terrain
        data.SetHeights(0, 0, heights);
        terrain.terrainData.DirtyHeightmapRegion(new RectInt(0, 0, size, size), TerrainHeightmapSyncControl.HeightAndLod);

        //update noise texture and apply it to ui display
        noiseTexture.Apply();
        if (noiseDisplay != null)
        {
            noiseDisplay.texture = noiseTexture;
        }
    }
    private void GenerateFromHeightMap()
    {
        TerrainData data = terrain.terrainData;
        int size = data.heightmapResolution;
        float[,] heights = new float[size, size];
        Texture2D blendedTexture = new Texture2D(size, size);

        //determine blend
        float blendStrength = 0.4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                //get grayscale value from heightMap at corresponding pixel
                Color heightPixelColor = heightMap.GetPixel(x * heightMap.width / size, y * heightMap.height / size);

                //get grayscale value from falloffMap at corresponding pixel
                Color falloffPixelColor = falloffMap.GetPixel(x * falloffMap.width / size, y * falloffMap.height / size);

                //calculate height value from both maps and blend
                float heightValue = heightPixelColor.grayscale - 0.6f;
                float falloffValue = falloffPixelColor.grayscale;
                float blendedValue = Mathf.Lerp(heightValue, falloffValue, blendStrength);


                heights[x, y] = blendedValue;

                //set pixel color in blended texture for visualization
                blendedTexture.SetPixel(x, y, new Color(blendedValue, blendedValue, blendedValue));
            }
        }

        //apply blended texture to noise display for visuals
        blendedTexture.Apply();
        noiseDisplay.texture = blendedTexture;

        //apply heights to terrain
        data.SetHeights(0, 0, heights);
        terrain.terrainData.DirtyHeightmapRegion(new RectInt(0, 0, size, size), TerrainHeightmapSyncControl.HeightAndLod);
        PaintTerrain();
    }

    private void PaintTerrain()
    {
        TerrainData data = terrain.terrainData;

        int width = data.alphamapWidth;
        int height = data.alphamapHeight;
        float[,] heights = data.GetHeights(0, 0, width, height);

        //initialize a 3D array (width, height, # of texture layers)
        float[,,] splatmapData = new float[width, height, data.alphamapLayers];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //get normalized height of terrain at this point
                float terrainHeight = heights[x, y];

                //determine which texture to paint based on terrain height
                if (terrainHeight <= sandHeight)
                {
                    //apply sand texture if height is low enough
                    splatmapData[x, y, sandIndex] = 1;
                    splatmapData[x, y, grassIndex] = 0;
                    splatmapData[x, y, rockIndex] = 0;
                }
                else if (terrainHeight <= grassHeight)
                {
                    //blend between sand and grass
                    float blend = Mathf.InverseLerp(sandHeight, grassHeight, terrainHeight);
                    splatmapData[x, y, sandIndex] = 1 - blend;
                    splatmapData[x, y, grassIndex] = blend;
                    splatmapData[x, y, rockIndex] = 0;
                }
                else if (terrainHeight >= rockHeight)
                {
                    //apply rock texture if height is above rockHeight threshold
                    splatmapData[x, y, sandIndex] = 0;
                    splatmapData[x, y, grassIndex] = 0;
                    splatmapData[x, y, rockIndex] = 1;
                }
                else
                {
                    //blend between grass and rock textures for intermediate heights
                    float blend = Mathf.InverseLerp(grassHeight, rockHeight, terrainHeight);
                    splatmapData[x, y, sandIndex] = 0;
                    splatmapData[x, y, grassIndex] = 1 - blend;
                    splatmapData[x, y, rockIndex] = blend;
                }
            }
        }

        //apply the painted textures to the terrain
        data.SetAlphamaps(0, 0, splatmapData);
    }

    public void GenerateButton(bool genNew)
    {
        if (genNew)
        {
            offsetX = Random.Range(0f, 100000f);
            offsetY = Random.Range(0f, 100000f);
        }
        GenerateTerrain(true);
        PaintTerrain();
    }
    public void EnableSettings(GameObject target)
    {
        if (target.tag == "CameraSettings")
        {
            toggleSettings.SetActive(false);
            settings.SetActive(false);
        }
        target.SetActive(!settings.activeSelf);
    }

}
