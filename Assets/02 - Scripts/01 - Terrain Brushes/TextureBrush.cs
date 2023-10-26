using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class TextureBrush : TerrainBrush {

	//const int textureSize = Terrain.activeTerrain.terrainData.;
	const TextureFormat textureFormat = TextureFormat.RGB565;
	const int textureSize = 512;
	public CustomLayer[] Terrainlayers;

	Terrain active;
	TerrainData data;
	private int amap_width;
	private int amap_height;
	float[,,] splatMapData;
	float minHeight;
	float maxHeight;
	Vector3 gridSize;

    private void Start()
    {
        // Active terrain
        active = Terrain.activeTerrain;
        data = active.terrainData;
        amap_width = data.alphamapWidth;
        amap_height = data.alphamapHeight;
        splatMapData = new float[amap_width, amap_height, Terrainlayers.Count()];
		// Custom terrain


	}

    public override void draw(int x, int z)
	{
		Start();
		minHeight = terrain.getMinHeight();
		maxHeight = terrain.getMaxHeight();
		terrain.GetComponent<Renderer>().sharedMaterial.SetFloat("minHeight", minHeight);
        terrain.GetComponent<Renderer>().sharedMaterial.SetFloat("maxHeight", maxHeight);
		
        ApplyToMaterial(terrain.GetComponent<Renderer>().sharedMaterial);
	}

	Texture2DArray GenerateTextureArray(Texture2D[] textures)
	{
		Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
		for (int i = 0; i < textures.Length; i++)
		{
			textureArray.SetPixels(textures[i].GetPixels(), i);
		}
		textureArray.Apply();
		return textureArray;
	}

	public void ApplyToMaterial(Material material)
	{
		Debug.Log("Entered");
		material.SetInt("layerCount", Terrainlayers.Length);
        material.SetColorArray("baseColours", Terrainlayers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseStartHeights", Terrainlayers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", Terrainlayers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("baseColourStrength", Terrainlayers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", Terrainlayers.Select(x => x.textureScale).ToArray());
        Texture2DArray texturesArray = GenerateTextureArray(Terrainlayers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTextures", texturesArray);
	}

	float getNormalizedHeight(float y, float min, float max)
    {
		return (y - min) / (max - min);
    }

    [System.Serializable]
	public class CustomLayer
	{
		public Texture2D texture;
		public Color tint;
		[Range(0, 1)]
		public float tintStrength;
		[Range(0, 1)]
		public float startHeight;
		[Range(0, 1)]
		public float blendStrength;
		public float textureScale;
	}

}
