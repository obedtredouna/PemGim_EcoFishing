using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GPU Instanced Grass Scatterer
/// Menyebar grass mesh di atas permukaan mesh apapun menggunakan Raycast + GPU Instancing
/// Compatible: Unity 6, URP
/// </summary>
public class GrassInstancer : MonoBehaviour
{
    [Header("=== Target Mesh ===")]
    [Tooltip("Mesh object yang akan ditumbuhi grass (dari Blender)")]
    public MeshCollider targetMeshCollider;

    [Header("=== Grass Mesh & Material ===")]
    [Tooltip("Mesh rumput (bisa buat sendiri atau import dari Blender)")]
    public Mesh grassMesh;

    [Tooltip("Material grass - harus pakai shader yang enable GPU Instancing")]
    public Material grassMaterial;

    [Header("=== Scatter Settings ===")]
    [Tooltip("Jumlah total grass yang di-spawn")]
    public int grassCount = 10000;

    [Tooltip("Area scatter (luas wilayah dalam meter)")]
    public Vector2 scatterArea = new Vector2(100f, 100f);

    [Tooltip("Layer mask untuk raycast (set ke layer mesh kamu)")]
    public LayerMask groundLayer = ~0;

    [Header("=== Grass Transform ===")]
    [Tooltip("Ukuran minimum grass")]
    public Vector3 scaleMin = new Vector3(0.8f, 0.8f, 0.8f);

    [Tooltip("Ukuran maksimum grass")]
    public Vector3 scaleMax = new Vector3(1.2f, 1.5f, 1.2f);

    [Tooltip("Rotasi random (derajat) agar grass tidak seragam")]
    public float randomRotationY = 360f;

    [Tooltip("Tilt random supaya grass tidak tegak semua")]
    public float randomTilt = 5f;

    [Tooltip("Offset posisi Y agar grass tidak melayang/tenggelam")]
    public float yOffset = 0f;

    [Header("=== Slope Filter ===")]
    [Tooltip("Grass tidak muncul di kemiringan lebih dari ini (derajat)")]
    public float maxSlopeAngle = 35f;

    [Header("=== LOD / Culling ===")]
    [Tooltip("Jarak render maksimum")]
    public float renderDistance = 150f;

    [Tooltip("Shadow casting mode")]
    public UnityEngine.Rendering.ShadowCastingMode shadowMode = UnityEngine.Rendering.ShadowCastingMode.Off;

    [Header("=== Debug ===")]
    public bool showDebugInfo = true;

    // Internal
    private List<Matrix4x4[]> _batches = new List<Matrix4x4[]>();
    private int _totalPlaced = 0;
    private const int BATCH_SIZE = 1023; // Unity limit per DrawMeshInstanced call
    private Camera _mainCamera;
    private Bounds _renderBounds;

    void Start()
    {
        _mainCamera = Camera.main;
        GenerateGrass();
    }

    [ContextMenu("Regenerate Grass")]
    public void GenerateGrass()
    {
        if (grassMesh == null)
        {
            Debug.LogError("[GrassInstancer] grassMesh belum diisi!");
            return;
        }
        if (grassMaterial == null)
        {
            Debug.LogError("[GrassInstancer] grassMaterial belum diisi!");
            return;
        }
        if (!grassMaterial.enableInstancing)
        {
            Debug.LogWarning("[GrassInstancer] Material belum enable GPU Instancing! Mengaktifkan otomatis...");
            grassMaterial.enableInstancing = true;
        }

        _batches.Clear();
        _totalPlaced = 0;

        List<Matrix4x4> matrices = new List<Matrix4x4>(grassCount);
        Vector3 center = transform.position;
        float halfX = scatterArea.x / 2f;
        float halfZ = scatterArea.y / 2f;
        int attempts = grassCount * 3; // coba 3x lipat untuk kompensasi yang kena slope filter

        for (int i = 0; i < attempts && matrices.Count < grassCount; i++)
        {
            // Random posisi di dalam area scatter
            float randX = Random.Range(center.x - halfX, center.x + halfX);
            float randZ = Random.Range(center.z - halfZ, center.z + halfZ);
            Vector3 rayOrigin = new Vector3(randX, center.y + 500f, randZ);

            // Raycast ke bawah untuk cari permukaan mesh
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1000f, groundLayer))
            {
                // Filter slope
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > maxSlopeAngle)
                    continue;

                // Posisi grass
                Vector3 pos = hit.point + Vector3.up * yOffset;

                // Rotasi: align dengan normal tanah + random Y + random tilt
                Quaternion normalRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Quaternion yRot = Quaternion.Euler(0f, Random.Range(0f, randomRotationY), 0f);
                Quaternion tilt = Quaternion.Euler(
                    Random.Range(-randomTilt, randomTilt),
                    0f,
                    Random.Range(-randomTilt, randomTilt)
                );
                Quaternion finalRot = normalRot * yRot * tilt;

                // Scale random
                Vector3 scale = new Vector3(
                    Random.Range(scaleMin.x, scaleMax.x),
                    Random.Range(scaleMin.y, scaleMax.y),
                    Random.Range(scaleMin.z, scaleMax.z)
                );

                matrices.Add(Matrix4x4.TRS(pos, finalRot, scale));
            }
        }

        _totalPlaced = matrices.Count;

        // Bagi ke batch (maks 1023 per batch = Unity limit)
        for (int i = 0; i < matrices.Count; i += BATCH_SIZE)
        {
            int count = Mathf.Min(BATCH_SIZE, matrices.Count - i);
            Matrix4x4[] batch = new Matrix4x4[count];
            matrices.CopyTo(i, batch, 0, count);
            _batches.Add(batch);
        }

        // Buat bounds untuk frustum culling kasar
        _renderBounds = new Bounds(center, new Vector3(scatterArea.x, 500f, scatterArea.y));

        if (showDebugInfo)
            Debug.Log($"[GrassInstancer] Placed {_totalPlaced} grass dalam {_batches.Count} batch.");
    }

    void Update()
    {
        if (_batches.Count == 0 || grassMesh == null || grassMaterial == null)
            return;

        // Cek jarak kamera (kasar, per GrassInstancer object)
        if (_mainCamera != null)
        {
            float dist = Vector3.Distance(_mainCamera.transform.position, transform.position);
            if (dist > renderDistance)
                return;
        }

        // Render semua batch setiap frame
        foreach (var batch in _batches)
        {
            Graphics.DrawMeshInstanced(
                grassMesh,
                0,              // submesh index
                grassMaterial,
                batch,
                batch.Length,
                null,           // MaterialPropertyBlock (bisa untuk variasi warna)
                shadowMode,
                false,          // receive shadows
                gameObject.layer
            );
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualisasi area scatter di editor
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawCube(
            transform.position,
            new Vector3(scatterArea.x, 1f, scatterArea.y)
        );
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(scatterArea.x, 1f, scatterArea.y)
        );
    }
}
