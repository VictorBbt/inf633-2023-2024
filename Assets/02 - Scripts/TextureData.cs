using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureData : MonoBehaviour
{

	//const int textureSize = Terrain.activeTerrain.terrainData.;
	const TextureFormat textureFormat = TextureFormat.RGB565;
	const int textureSize = 512;
	public Layer[] layers;

	float savedMinHeight;
	float savedMaxHeight;

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


	[System.Serializable]
	public class Layer
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
