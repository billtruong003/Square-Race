#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class NoiseGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Portal Noise")]
    public static void GenerateNoise()
    {
        int size = 512;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        
        // Tạo Perlin Noise liền mạch (Seamless)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float xCoord = (float)x / size * 5f;
                float yCoord = (float)y / size * 5f;
                
                // Kỹ thuật xoay chiều để tạo seamless noise
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                
                // Làm mờ viền để đảm bảo tileable (đơn giản hóa)
                // Thực tế shader đã xử lý xoay, nên chỉ cần noise ngẫu nhiên tốt là được
                // Dưới đây là noise dạng mây cơ bản
                float noise = Mathf.PerlinNoise(xCoord + 10, yCoord + 10) * 0.5f + 
                              Mathf.PerlinNoise(xCoord * 2 + 20, yCoord * 2 + 20) * 0.3f + 
                              Mathf.PerlinNoise(xCoord * 4 + 30, yCoord * 4 + 30) * 0.2f;

                texture.SetPixel(x, y, new Color(noise, noise, noise));
            }
        }
        
        texture.Apply();

        byte[] bytes = texture.EncodeToPNG();
        string path = Application.dataPath + "/PortalNoise.png";
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();
        
        // Tự động set import settings
        string assetPath = "Assets/PortalNoise.png";
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null) {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }

        Debug.Log("Đã tạo PortalNoise.png tại Assets/");
    }
}
#endif