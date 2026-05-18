using UnityEngine;
using UnityEngine.UI;

public class SimpleMiniMap : MonoBehaviour
{
    [Header("Reference")]
    public Transform player;
    public RawImage minimapImage;

    [Header("MiniMap Setting")]
    public int textureSize = 128;
    public float worldSize = 40f; // area yang ditampilkan di sekitar player
    public float rayHeight = 100f;
    public LayerMask minimapLayerMask;

    [Header("Player Marker")]
    public Color playerColor = Color.red;
    public int playerMarkerSize = 3;

    private Texture2D minimapTexture;
    private Color[] pixels;

    void Start()
    {
        minimapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        minimapTexture.filterMode = FilterMode.Point;
        minimapImage.texture = minimapTexture;

        pixels = new Color[textureSize * textureSize];
    }

    void Update()
    {
        if (player == null) return;

        DrawMiniMap();
    }

    void DrawMiniMap()
    {
        float halfWorld = worldSize / 2f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float percentX = (float)x / (textureSize - 1);
                float percentY = (float)y / (textureSize - 1);

                float worldX = player.position.x - halfWorld + percentX * worldSize;
                float worldZ = player.position.z - halfWorld + percentY * worldSize;

                Vector3 rayOrigin = new Vector3(worldX, rayHeight, worldZ);
                Color pixelColor = Color.black;

                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayHeight * 2f, minimapLayerMask))
                {
                    MapColorProvider colorProvider = hit.collider.GetComponentInParent<MapColorProvider>();

                    if (colorProvider != null)
                    {
                        pixelColor = colorProvider.GetMapColor();                    }
                    else
                    {
                        // warna default kalau object belum punya MapColorProvider
                        pixelColor = Color.gray;
                    }
                }

                pixels[y * textureSize + x] = pixelColor;
            }
        }

        // gambar marker player di tengah
        int centerX = textureSize / 2;
        int centerY = textureSize / 2;

        for (int py = -playerMarkerSize; py <= playerMarkerSize; py++)
        {
            for (int px = -playerMarkerSize; px <= playerMarkerSize; px++)
            {
                int drawX = centerX + px;
                int drawY = centerY + py;

                if (drawX >= 0 && drawX < textureSize && drawY >= 0 && drawY < textureSize)
                {
                    pixels[drawY * textureSize + drawX] = playerColor;
                }
            }
        }

        minimapTexture.SetPixels(pixels);
        minimapTexture.Apply();
    }
}