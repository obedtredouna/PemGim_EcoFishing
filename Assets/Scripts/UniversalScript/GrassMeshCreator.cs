using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper: Buat grass mesh sederhana (cross quad) langsung dari script
/// Gunakan ini jika belum punya grass mesh dari Blender
/// </summary>
public class GrassMeshCreator : MonoBehaviour
{
    [Header("Blade Settings")]
    [Tooltip("Lebar satu blade grass")]
    public float bladeWidth = 0.1f;

    [Tooltip("Tinggi satu blade grass")]
    public float bladeHeight = 0.6f;

    [Tooltip("Jumlah quad yang di-cross (2 = X shape, 3 = bintang)")]
    [Range(2, 3)]
    public int crossCount = 2;

    /// <summary>
    /// Panggil dari GrassInstancer atau klik kanan di Inspector
    /// </summary>
    [ContextMenu("Create & Assign Grass Mesh")]
    public Mesh CreateGrassMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "GrassBlade_Cross";

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs   = new List<Vector2>();
        List<int>     tris  = new List<int>();

        float angleStep = 180f / crossCount;

        for (int i = 0; i < crossCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float cos   = Mathf.Cos(angle) * bladeWidth * 0.5f;
            float sin   = Mathf.Sin(angle) * bladeWidth * 0.5f;

            int baseIdx = verts.Count;

            verts.Add(new Vector3(-cos, 0f,         -sin));
            verts.Add(new Vector3( cos, 0f,          sin));
            verts.Add(new Vector3(-cos, bladeHeight, -sin));
            verts.Add(new Vector3( cos, bladeHeight,  sin));

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));

            // Front
            tris.Add(baseIdx+0); tris.Add(baseIdx+2); tris.Add(baseIdx+1);
            tris.Add(baseIdx+1); tris.Add(baseIdx+2); tris.Add(baseIdx+3);
            // Back
            tris.Add(baseIdx+1); tris.Add(baseIdx+2); tris.Add(baseIdx+0);
            tris.Add(baseIdx+3); tris.Add(baseIdx+2); tris.Add(baseIdx+1);
        }

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Auto assign ke GrassInstancer jika ada di GameObject yang sama
        GrassInstancer gi = GetComponent<GrassInstancer>();
        if (gi != null)
        {
            gi.grassMesh = mesh;
            Debug.Log("[GrassMeshCreator] Mesh berhasil dibuat dan di-assign ke GrassInstancer!");
        }

        return mesh;
    }
}
