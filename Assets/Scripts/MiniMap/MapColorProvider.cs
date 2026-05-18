using UnityEngine;

public class MapColorProvider : MonoBehaviour
{
    public enum MapSurfaceType
    {
        Grass,
        Sand,
        Mud,
        ClearWater,
        MuddyWater,
        Wood,
        House,
        Rock,
        Tree,
        Road,
        Player,
        Custom
    }

    [Header("Jenis Permukaan")]
    public MapSurfaceType surfaceType = MapSurfaceType.Grass;

    [Header("Warna Khusus jika memilih Custom")]
    public Color customColor = Color.white;

    public Color GetMapColor()
    {
        switch (surfaceType)
        {
            case MapSurfaceType.Grass:
                return new Color(0.25f, 0.75f, 0.25f); 
                // Hijau rumput

            case MapSurfaceType.Sand:
                return new Color(0.85f, 0.75f, 0.45f); 
                // Pasir / tepi sungai

            case MapSurfaceType.Mud:
                return new Color(0.45f, 0.28f, 0.12f); 
                // Lumpur / tanah basah

            case MapSurfaceType.ClearWater:
                return new Color(0.15f, 0.45f, 0.95f); 
                // Air biru

            case MapSurfaceType.MuddyWater:
                return new Color(0.55f, 0.38f, 0.18f); 
                // Air keruh kecoklatan seperti di map Anda

            case MapSurfaceType.Wood:
                return new Color(0.55f, 0.32f, 0.12f); 
                // Kayu / jembatan

            case MapSurfaceType.House:
                return new Color(0.38f, 0.20f, 0.08f); 
                // Rumah kayu / bangunan coklat tua

            case MapSurfaceType.Rock:
                return new Color(0.45f, 0.45f, 0.45f); 
                // Batu abu-abu

            case MapSurfaceType.Tree:
                return new Color(0.10f, 0.45f, 0.10f); 
                // Pohon hijau tua

            case MapSurfaceType.Road:
                return new Color(0.50f, 0.40f, 0.25f); 
                // Jalan tanah

            case MapSurfaceType.Player:
                return Color.red; 
                // Player merah

            case MapSurfaceType.Custom:
                return customColor;

            default:
                return Color.gray;
        }
    }
}