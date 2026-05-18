using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GrassPainter - Paint grass langsung di Scene View
/// Grass terlihat di Editor tanpa perlu Play mode
/// Compatible: Unity 6, URP
/// </summary>
[ExecuteAlways]
public class GrassPainter : MonoBehaviour
{
    [Header("=== Grass Mesh & Material ===")]
    public Mesh grassMesh;
    public Material grassMaterial;

    [Header("=== Paint Settings ===")]
    [Tooltip("Radius brush saat painting")]
    public float brushRadius = 2f;

    [Tooltip("Jumlah grass per brush stroke")]
    public int grassPerStroke = 10;

    [Tooltip("Layer mask target mesh")]
    public LayerMask paintLayer = ~0;

    [Header("=== Grass Transform ===")]
    public Vector3 scaleMin = new Vector3(0.8f, 0.8f, 0.8f);
    public Vector3 scaleMax = new Vector3(1.2f, 1.5f, 1.2f);
    public float randomRotationY = 360f;
    public float randomTilt = 5f;
    public float yOffset = 0f;

    [Header("=== Slope Filter ===")]
    [Tooltip("Grass tidak muncul di kemiringan lebih dari ini")]
    public float maxSlopeAngle = 35f;

    [Header("=== Render Settings ===")]
    public float renderDistance = 150f;
    public UnityEngine.Rendering.ShadowCastingMode shadowMode =
        UnityEngine.Rendering.ShadowCastingMode.Off;

    [Header("=== Data (jangan edit manual) ===")]
    [HideInInspector]
    public List<Matrix4x4> grassMatrices = new List<Matrix4x4>();

    // Internal
    private List<Matrix4x4[]> _batches = new List<Matrix4x4[]>();
    private const int BATCH_SIZE = 1023;

    // ─────────────────────────────────────────
    // Unity Messages
    // ─────────────────────────────────────────

    void OnEnable()
    {
        if (grassMaterial != null)
            grassMaterial.enableInstancing = true;

        RebuildBatches();

#if UNITY_EDITOR
        UnityEditor.SceneView.duringSceneGui += OnSceneViewRender;
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.SceneView.duringSceneGui -= OnSceneViewRender;
#endif
    }

    void Update()
    {
        // Render saat Play mode via game camera
        if (Application.isPlaying)
            RenderGrass(Camera.main);
    }

#if UNITY_EDITOR
    private void OnSceneViewRender(UnityEditor.SceneView sceneView)
    {
        if (!Application.isPlaying)
            RenderGrass(sceneView.camera);
    }
#endif

    // ─────────────────────────────────────────
    // Render
    // ─────────────────────────────────────────

    private void RenderGrass(Camera cam)
    {
        if (_batches.Count == 0 || grassMesh == null || grassMaterial == null) return;
        if (cam == null) return;

        if (renderDistance > 0f)
        {
            float dist = Vector3.Distance(cam.transform.position, transform.position);
            if (dist > renderDistance + 50f) return;
        }

        foreach (var batch in _batches)
        {
            Graphics.DrawMeshInstanced(
                grassMesh, 0, grassMaterial,
                batch, batch.Length,
                null, shadowMode, false, gameObject.layer,
                cam
            );
        }
    }

    // ─────────────────────────────────────────
    // Paint & Erase API
    // ─────────────────────────────────────────

    public void PaintGrassAt(Vector3 hitPoint, Vector3 hitNormal, float radius, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 rand2D       = Random.insideUnitCircle * radius;
            Vector3 sampleOrigin = hitPoint + new Vector3(rand2D.x, 50f, rand2D.y);

            if (Physics.Raycast(sampleOrigin, Vector3.down, out RaycastHit hit, 200f, paintLayer))
            {
                float slope = Vector3.Angle(hit.normal, Vector3.up);
                if (slope > maxSlopeAngle) continue;

                Vector3 pos = hit.point + Vector3.up * yOffset;

                Quaternion normalRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Quaternion yRot      = Quaternion.Euler(0f, Random.Range(0f, randomRotationY), 0f);
                Quaternion tilt      = Quaternion.Euler(
                    Random.Range(-randomTilt, randomTilt), 0f,
                    Random.Range(-randomTilt, randomTilt)
                );

                Vector3 scale = new Vector3(
                    Random.Range(scaleMin.x, scaleMax.x),
                    Random.Range(scaleMin.y, scaleMax.y),
                    Random.Range(scaleMin.z, scaleMax.z)
                );

                grassMatrices.Add(Matrix4x4.TRS(pos, normalRot * yRot * tilt, scale));
            }
        }

        RebuildBatches();
    }

    public void EraseGrassAt(Vector3 center, float radius)
    {
        float r2     = radius * radius;
        int   before = grassMatrices.Count;

        grassMatrices.RemoveAll(m =>
        {
            Vector3 pos = new Vector3(m.m03, m.m13, m.m23);
            float dx = pos.x - center.x;
            float dz = pos.z - center.z;
            return (dx * dx + dz * dz) <= r2;
        });

        if (grassMatrices.Count != before)
            RebuildBatches();
    }

    public void ClearAll()
    {
        grassMatrices.Clear();
        _batches.Clear();
    }

    public void RebuildBatches()
    {
        _batches.Clear();

        if (grassMaterial != null)
            grassMaterial.enableInstancing = true;

        for (int i = 0; i < grassMatrices.Count; i += BATCH_SIZE)
        {
            int count = Mathf.Min(BATCH_SIZE, grassMatrices.Count - i);
            Matrix4x4[] batch = new Matrix4x4[count];
            grassMatrices.CopyTo(i, batch, 0, count);
            _batches.Add(batch);
        }

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    public int GrassCount => grassMatrices.Count;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.15f);
        Gizmos.DrawSphere(transform.position, brushRadius);
    }
}
