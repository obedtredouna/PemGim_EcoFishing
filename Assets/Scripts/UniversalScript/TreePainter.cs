using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TreePainter - Paint tree langsung di Scene View menggunakan PREFAB.
///
/// Berbeda dengan GrassPainter yang pakai GPU Instancing (karena grass ada ribuan
/// dan tidak perlu collider), TreePainter meng-instantiate prefab sebagai
/// GameObject child. Ini supaya tree mendukung:
///   - Collider (physics, raycast, player collision)
///   - LODGroup bawaan prefab
///   - Shader wind / material animation
///   - Bisa di-select individual di Hierarchy
///
/// Tree tetap "terlihat" di Scene View tanpa Play karena tree adalah
/// GameObject beneran di scene (bukan render runtime).
///
/// Cara pakai: Add component ini ke GameObject (mis. "TreeContainer"),
/// drag Tree Prefab ke slot, lalu gunakan TreePainterEditor untuk painting
/// di Scene View.
///
/// Compatible: Unity 6, URP
/// </summary>
[ExecuteAlways]
public class TreePainter : MonoBehaviour
{
    [Header("=== Tree Prefab ===")]
    [Tooltip("Drag prefab tree ke sini. Prefab akan di-instantiate sebagai child GameObject saat painting.")]
    public GameObject treePrefab;

    [Tooltip("Prefab tambahan untuk variasi (opsional). Saat painting, salah satu prefab dipilih secara random.")]
    public List<GameObject> additionalPrefabs = new List<GameObject>();

    [Header("=== Paint Settings ===")]
    [Tooltip("Radius brush saat painting")]
    public float brushRadius = 5f;

    [Tooltip("Jumlah tree per brush stroke")]
    [Range(1, 20)]
    public int treesPerStroke = 3;

    [Tooltip("Jarak minimum antar tree (mencegah overlap)")]
    public float minSpacing = 2f;

    [Tooltip("Layer mask target permukaan (terrain/ground)")]
    public LayerMask paintLayer = ~0;

    [Header("=== Tree Transform ===")]
    [Tooltip("Range skala acak untuk tree")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.3f);

    [Tooltip("Apakah skala seragam di X/Y/Z, atau random per axis")]
    public bool uniformScale = true;

    [Tooltip("Random rotasi Y (derajat)")]
    public float randomRotationY = 360f;

    [Tooltip("Random tilt kecil (derajat) pada X & Z. Biasanya kecil agar tree tetap tegak.")]
    [Range(0f, 15f)]
    public float randomTilt = 2f;

    [Tooltip("Offset vertikal untuk menanam root ke tanah (negatif = masuk tanah)")]
    public float yOffset = 0f;

    [Tooltip("Jika true, tree mengikuti normal permukaan. Jika false, selalu tegak lurus (up).")]
    public bool alignToNormal = false;

    [Header("=== Slope Filter ===")]
    [Tooltip("Tree tidak muncul di kemiringan lebih dari ini")]
    [Range(0f, 90f)]
    public float maxSlopeAngle = 30f;

    [Header("=== Data ===")]
    [Tooltip("Jika true, tree hasil painting akan di-parent ke GameObject ini.")]
    public bool parentToThis = true;

    public int TreeCount
    {
        get
        {
            if (parentToThis) return transform.childCount;
            return _spawnedTrees.Count;
        }
    }

    // Tracking manual kalau tidak di-parent
    [SerializeField, HideInInspector]
    private List<GameObject> _spawnedTrees = new List<GameObject>();

    /// <summary>
    /// Tambah tree di titik tertentu (dipanggil dari Editor tool)
    /// </summary>
    public void PaintTreeAt(Vector3 hitPoint, Vector3 hitNormal, float radius, int count)
    {
        if (treePrefab == null && (additionalPrefabs == null || additionalPrefabs.Count == 0))
        {
            Debug.LogWarning("[TreePainter] Tree Prefab belum di-assign.", this);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            // Random posisi dalam radius brush
            Vector2 rand2D = Random.insideUnitCircle * radius;
            Vector3 sampleOrigin = hitPoint + new Vector3(rand2D.x, 50f, rand2D.y);

            if (!Physics.Raycast(sampleOrigin, Vector3.down, out RaycastHit hit, 200f, paintLayer))
                continue;

            // Slope filter
            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > maxSlopeAngle) continue;

            // Spacing check
            if (minSpacing > 0f && IsTooClose(hit.point, minSpacing))
                continue;

            // Pilih prefab (random antara treePrefab & additionalPrefabs)
            GameObject prefab = PickRandomPrefab();
            if (prefab == null) continue;

            // Rotasi
            Quaternion baseRot = alignToNormal
                ? Quaternion.FromToRotation(Vector3.up, hit.normal)
                : Quaternion.identity;
            Quaternion yRot   = Quaternion.Euler(0f, Random.Range(0f, randomRotationY), 0f);
            Quaternion tilt   = Quaternion.Euler(
                Random.Range(-randomTilt, randomTilt), 0f,
                Random.Range(-randomTilt, randomTilt)
            );
            Quaternion finalRot = baseRot * yRot * tilt;

            // Skala
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

            Vector3 pos = hit.point + Vector3.up * yOffset;

            // Instantiate (pakai PrefabUtility di Editor supaya tetap sebagai prefab instance)
            GameObject tree = InstantiatePrefab(prefab);
            if (tree == null) continue;

            tree.transform.position = pos;
            tree.transform.rotation = finalRot;
            tree.transform.localScale = scale;

            if (parentToThis)
                tree.transform.SetParent(transform, true);
            else
                _spawnedTrees.Add(tree);

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(tree, "Paint Tree");
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
    /// Hapus tree dalam radius tertentu (erase mode)
    /// </summary>
    public void EraseTreeAt(Vector3 center, float radius)
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
            foreach (var go in _spawnedTrees)
            {
                if (go == null) continue;
                float dx = go.transform.position.x - center.x;
                float dz = go.transform.position.z - center.z;
                if ((dx * dx + dz * dz) <= r2)
                    toDelete.Add(go);
            }
            _spawnedTrees.RemoveAll(g => g == null || toDelete.Contains(g));
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

    /// <summary>
    /// Hapus semua tree
    /// </summary>
    public void ClearAll()
    {
        if (parentToThis)
        {
            // Copy dulu supaya tidak memodifikasi collection saat iterasi
            var children = new List<GameObject>();
            foreach (Transform child in transform)
                children.Add(child.gameObject);

            foreach (var go in children)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.DestroyObjectImmediate(go);
#else
                DestroyImmediate(go);
#endif
            }
        }
        else
        {
            foreach (var go in _spawnedTrees)
            {
                if (go == null) continue;
#if UNITY_EDITOR
                UnityEditor.Undo.DestroyObjectImmediate(go);
#else
                DestroyImmediate(go);
#endif
            }
            _spawnedTrees.Clear();
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        if (!Application.isPlaying && gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    // ─── Helpers ────────────────────────────────────────────────────────

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
            foreach (var go in _spawnedTrees)
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
        // Kumpulkan semua prefab valid
        List<GameObject> pool = new List<GameObject>();
        if (treePrefab != null) pool.Add(treePrefab);
        if (additionalPrefabs != null)
        {
            foreach (var p in additionalPrefabs)
                if (p != null) pool.Add(p);
        }

        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    private GameObject InstantiatePrefab(GameObject prefab)
    {
#if UNITY_EDITOR
        // Di editor, pakai PrefabUtility agar tetap menjadi prefab instance
        if (!Application.isPlaying)
        {
            var instance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                instance = Instantiate(prefab); // fallback
            return instance;
        }
#endif
        return Instantiate(prefab);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.15f);
        Gizmos.DrawSphere(transform.position, brushRadius);
    }
}