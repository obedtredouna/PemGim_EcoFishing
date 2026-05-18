using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WaterStuffPainter - Paint foliage/object di Scene View menggunakan PREFAB.
/// Support penempatan di permukaan mesh tertentu dan permukaan air.
/// Compatible: Unity 6, URP
/// </summary>
[ExecuteAlways]
public class WaterStuffPainter : MonoBehaviour
{
    [Header("=== WaterStuff Prefab ===")]
    [Tooltip("Drag prefab object ke sini.")]
    public GameObject waterStuffPrefab;

    [Tooltip("Prefab tambahan untuk variasi (opsional).")]
    public List<GameObject> additionalPrefabs = new List<GameObject>();

    [Header("=== Target Mesh ===")]
    [Tooltip("Drag GameObject mesh yang ingin ditumbuhi foliage.\nHanya permukaan mesh ini yang bisa di-paint.")]
    public GameObject targetMesh;

    [Tooltip("Jika ON: foliage HANYA bisa diletakkan di mesh yang di-assign.\nJika OFF: semua permukaan pada paintLayer bisa di-paint.")]
    public bool restrictToTargetMesh = true;

    [Header("=== Paint Settings ===")]
    public float brushRadius = 5f;

    [Range(1, 20)]
    public int objectsPerStroke = 3;

    [Tooltip("Jarak minimum antar object")]
    public float minSpacing = 2f;

    [Tooltip("Layer mask fallback (dipakai jika Target Mesh kosong atau restrictToTargetMesh OFF)")]
    public LayerMask paintLayer = ~0;

    [Header("=== Water Surface Mode ===")]
    [Tooltip("Aktifkan jika ingin meletakkan object di atas permukaan air")]
    public bool waterSurfaceMode = false;

    [Tooltip("Mode penentuan tinggi air")]
    public WaterHeightMode waterHeightMode = WaterHeightMode.FixedY;

    public enum WaterHeightMode
    {
        [Tooltip("Gunakan nilai Y tetap sebagai tinggi permukaan air")]
        FixedY,
        [Tooltip("Raycast ke layer air")]
        WaterLayer
    }

    [Tooltip("Tinggi Y permukaan air (untuk mode FixedY)")]
    public float waterSurfaceY = 0f;

    [Tooltip("Layer mask air (untuk mode WaterLayer)")]
    public LayerMask waterLayer;

    [Tooltip("Offset Y di atas permukaan air (+ = mengapung, - = tenggelam)")]
    public float waterYOffset = 0f;

    [Tooltip("Jika true, object selalu tegak di air")]
    public bool alwaysUprightOnWater = true;

    [Header("=== Object Transform ===")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.3f);
    public bool uniformScale = true;
    public float yOffset = 0f;

    [Tooltip("Ikuti normal permukaan (hanya berlaku di mode non-air)")]
    public bool alignToNormal = false;

    [Header("=== Rotation ===")]
    [Tooltip("Manual: semua object pakai rotasi ini sebagai base.\nRandom: rotasi acak dalam range min-max.")]
    public RotationMode rotationMode = RotationMode.Random;

    public enum RotationMode { Manual, Random }

    [Tooltip("Rotasi manual untuk SEMUA axis (X, Y, Z) dalam derajat.\nDipakai saat mode = Manual.")]
    public Vector3 manualRotation = Vector3.zero;

    [Tooltip("Range random rotasi X (min, max) dalam derajat.\nDipakai saat mode = Random.")]
    public Vector2 randomRotationX = new Vector2(0f, 0f);

    [Tooltip("Range random rotasi Y (min, max) dalam derajat.\nBiasanya (0, 360) untuk variasi penuh.")]
    public Vector2 randomRotationY = new Vector2(0f, 360f);

    [Tooltip("Range random rotasi Z (min, max) dalam derajat.")]
    public Vector2 randomRotationZ = new Vector2(0f, 0f);

    [Tooltip("Tambahan tilt random kecil (X & Z) agar tidak terlalu kaku.")]
    [Range(0f, 15f)]
    public float randomTilt = 2f;

    [Header("=== Slope Filter (non-air) ===")]
    [Range(0f, 90f)]
    public float maxSlopeAngle = 30f;

    [Header("=== Data ===")]
    public bool parentToThis = true;

    [SerializeField, HideInInspector]
    private List<GameObject> _spawnedObjects = new List<GameObject>();

    public int WaterStuffCount
    {
        get
        {
            if (parentToThis) return transform.childCount;
            return _spawnedObjects.Count;
        }
    }

    // Cache collider target mesh
    private Collider _targetCollider;

    // ─────────────────────────────────────────
    // Paint API
    // ─────────────────────────────────────────

    public void PaintWaterStuffAt(Vector3 hitPoint, Vector3 hitNormal, float radius, int count)
    {
        if (waterStuffPrefab == null && (additionalPrefabs == null || additionalPrefabs.Count == 0))
        {
            Debug.LogWarning("[WaterStuffPainter] Prefab belum di-assign.", this);
            return;
        }

        // Cache collider dari targetMesh
        RefreshTargetCollider();

        for (int i = 0; i < count; i++)
        {
            Vector2 rand2D      = Random.insideUnitCircle * radius;
            Vector3 finalPos    = Vector3.zero;
            Vector3 finalNormal = Vector3.up;
            bool    placed      = false;

            if (waterSurfaceMode)
            {
                placed = TryGetWaterPosition(hitPoint, rand2D, out finalPos, out finalNormal);
            }
            else
            {
                Vector3 sampleOrigin = hitPoint + new Vector3(rand2D.x, 50f, rand2D.y);

                if (restrictToTargetMesh && _targetCollider != null)
                {
                    // Raycast ke semua collider, lalu cek apakah yang kena adalah targetMesh
                    RaycastHit[] hits = Physics.RaycastAll(sampleOrigin, Vector3.down, 200f);
                    foreach (var h in hits)
                    {
                        // Cek apakah collider yang kena milik targetMesh atau child-nya
                        if (!IsPartOfTargetMesh(h.collider)) continue;

                        float slope = Vector3.Angle(h.normal, Vector3.up);
                        if (slope > maxSlopeAngle) break;

                        finalPos    = h.point + Vector3.up * yOffset;
                        finalNormal = h.normal;
                        placed      = true;
                        break;
                    }
                }
                else
                {
                    // Fallback: raycast ke paintLayer seperti biasa
                    if (Physics.Raycast(sampleOrigin, Vector3.down, out RaycastHit hit, 200f, paintLayer))
                    {
                        float slope = Vector3.Angle(hit.normal, Vector3.up);
                        if (slope > maxSlopeAngle) continue;

                        finalPos    = hit.point + Vector3.up * yOffset;
                        finalNormal = hit.normal;
                        placed      = true;
                    }
                }
            }

            if (!placed) continue;
            if (minSpacing > 0f && IsTooClose(finalPos, minSpacing)) continue;

            GameObject prefab = PickRandomPrefab();
            if (prefab == null) continue;

            Quaternion baseRot;
            if (waterSurfaceMode && alwaysUprightOnWater)
                baseRot = Quaternion.identity;
            else
                baseRot = alignToNormal
                    ? Quaternion.FromToRotation(Vector3.up, finalNormal)
                    : Quaternion.identity;

            Quaternion rotQ;
            if (rotationMode == RotationMode.Manual)
            {
                // Manual: semua object pakai rotasi yang sama persis
                rotQ = Quaternion.Euler(manualRotation);
            }
            else
            {
                // Random: pilih nilai acak dalam range masing-masing axis
                float rx = Random.Range(randomRotationX.x, randomRotationX.y);
                float ry = Random.Range(randomRotationY.x, randomRotationY.y);
                float rz = Random.Range(randomRotationZ.x, randomRotationZ.y);
                rotQ = Quaternion.Euler(rx, ry, rz);
            }

            // Tilt kecil tambahan (berlaku di kedua mode)
            Quaternion tilt = Quaternion.Euler(
                Random.Range(-randomTilt, randomTilt), 0f,
                Random.Range(-randomTilt, randomTilt)
            );
            Quaternion finalRot = baseRot * rotQ * tilt;

            Vector3 scale;
            if (uniformScale)
            {
                float s = Random.Range(scaleRange.x, scaleRange.y);
                scale = new Vector3(s, s, s);
            }
            else
            {
                scale = new Vector3(
                    Random.Range(scaleRange.x, scaleRange.y),
                    Random.Range(scaleRange.x, scaleRange.y),
                    Random.Range(scaleRange.x, scaleRange.y)
                );
            }

            GameObject obj = InstantiatePrefab(prefab);
            if (obj == null) continue;

            obj.transform.position   = finalPos;
            obj.transform.rotation   = finalRot;
            obj.transform.localScale = scale;

            if (parentToThis)
                obj.transform.SetParent(transform, true);
            else
                _spawnedObjects.Add(obj);

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(obj, "Paint WaterStuff");
#endif
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        if (!Application.isPlaying && gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    /// <summary>
    /// Cek apakah collider adalah bagian dari targetMesh (termasuk children)
    /// </summary>
    private bool IsPartOfTargetMesh(Collider col)
    {
        if (targetMesh == null) return false;
        Transform t = col.transform;
        while (t != null)
        {
            if (t.gameObject == targetMesh) return true;
            t = t.parent;
        }
        return false;
    }

    /// <summary>
    /// Refresh cache collider dari targetMesh
    /// </summary>
    public void RefreshTargetCollider()
    {
        if (targetMesh == null)
        {
            _targetCollider = null;
            return;
        }
        _targetCollider = targetMesh.GetComponentInChildren<Collider>();
        if (_targetCollider == null)
            Debug.LogWarning($"[WaterStuffPainter] Target Mesh '{targetMesh.name}' tidak punya Collider! Tambahkan Mesh Collider.", this);
    }

    private bool TryGetWaterPosition(Vector3 hitPoint, Vector2 rand2D,
        out Vector3 pos, out Vector3 normal)
    {
        normal = Vector3.up;
        float targetX = hitPoint.x + rand2D.x;
        float targetZ = hitPoint.z + rand2D.y;

        if (waterHeightMode == WaterHeightMode.FixedY)
        {
            pos = new Vector3(targetX, waterSurfaceY + waterYOffset, targetZ);
            return true;
        }
        else
        {
            Vector3 origin = new Vector3(targetX, hitPoint.y + 50f, targetZ);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f, waterLayer))
            {
                pos    = hit.point + Vector3.up * waterYOffset;
                normal = hit.normal;
                return true;
            }
        }

        pos = Vector3.zero;
        return false;
    }

    public void EraseWaterStuffAt(Vector3 center, float radius)
    {
        float r2 = radius * radius;
        List<GameObject> toDelete = new List<GameObject>();

        if (parentToThis)
        {
            foreach (Transform child in transform)
            {
                float dx = child.position.x - center.x;
                float dz = child.position.z - center.z;
                if ((dx * dx + dz * dz) <= r2)
                    toDelete.Add(child.gameObject);
            }
        }
        else
        {
            foreach (var go in _spawnedObjects)
            {
                if (go == null) continue;
                float dx = go.transform.position.x - center.x;
                float dz = go.transform.position.z - center.z;
                if ((dx * dx + dz * dz) <= r2)
                    toDelete.Add(go);
            }
            _spawnedObjects.RemoveAll(g => g == null || toDelete.Contains(g));
        }

        foreach (var go in toDelete)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.DestroyObjectImmediate(go);
#else
            DestroyImmediate(go);
#endif
        }

        if (toDelete.Count > 0)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            if (!Application.isPlaying && gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            UnityEditor.SceneView.RepaintAll();
#endif
        }
    }

    public void ClearAll()
    {
        var children = new List<GameObject>();

        if (parentToThis)
        {
            foreach (Transform child in transform)
                children.Add(child.gameObject);
        }
        else
        {
            children.AddRange(_spawnedObjects);
            _spawnedObjects.Clear();
        }

        foreach (var go in children)
        {
            if (go == null) continue;
#if UNITY_EDITOR
            UnityEditor.Undo.DestroyObjectImmediate(go);
#else
            DestroyImmediate(go);
#endif
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        if (!Application.isPlaying && gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    // ─── Helpers ──────────────────────────────────────────────

    private bool IsTooClose(Vector3 pos, float spacing)
    {
        float s2 = spacing * spacing;

        if (parentToThis)
        {
            foreach (Transform child in transform)
            {
                float dx = child.position.x - pos.x;
                float dz = child.position.z - pos.z;
                if ((dx * dx + dz * dz) < s2) return true;
            }
        }
        else
        {
            foreach (var go in _spawnedObjects)
            {
                if (go == null) continue;
                float dx = go.transform.position.x - pos.x;
                float dz = go.transform.position.z - pos.z;
                if ((dx * dx + dz * dz) < s2) return true;
            }
        }
        return false;
    }

    private GameObject PickRandomPrefab()
    {
        List<GameObject> pool = new List<GameObject>();
        if (waterStuffPrefab != null) pool.Add(waterStuffPrefab);
        if (additionalPrefabs != null)
            foreach (var p in additionalPrefabs)
                if (p != null) pool.Add(p);

        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    private GameObject InstantiatePrefab(GameObject prefab)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var instance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) instance = Instantiate(prefab);
            return instance;
        }
#endif
        return Instantiate(prefab);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = waterSurfaceMode
            ? new Color(0.2f, 0.6f, 1f, 0.2f)
            : new Color(0.2f, 0.8f, 0.3f, 0.15f);
        Gizmos.DrawSphere(transform.position, brushRadius);

        if (waterSurfaceMode && waterHeightMode == WaterHeightMode.FixedY)
        {
            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.5f);
            Vector3 c = new Vector3(transform.position.x, waterSurfaceY + waterYOffset, transform.position.z);
            Gizmos.DrawWireCube(c, new Vector3(10f, 0.02f, 10f));
        }

        // Highlight target mesh boundary
        if (targetMesh != null && restrictToTargetMesh)
        {
            Renderer rend = targetMesh.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.3f);
                Gizmos.DrawWireCube(rend.bounds.center, rend.bounds.size);
            }
        }
    }
}
