// Taruh file ini di folder: Assets/Editor/WaterStuffPainterEditor.cs

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WaterStuffPainter))]
public class WaterStuffPainterEditor : Editor
{
    private WaterStuffPainter _painter;

    private enum PaintMode { Paint, Erase }
    private PaintMode _mode = PaintMode.Paint;

    private readonly Color _paintColorGround = new Color(0.2f, 0.9f, 0.3f, 0.35f);
    private readonly Color _paintColorWater  = new Color(0.2f, 0.6f, 1f,  0.35f);
    private readonly Color _eraseColor       = new Color(1f,   0.3f, 0.2f, 0.35f);

    void OnEnable() => _painter = (WaterStuffPainter)target;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        // ── Prefab status ──
        EditorGUILayout.LabelField("─── Prefab Status ───", EditorStyles.boldLabel);
        int variants = 0;
        if (_painter.waterStuffPrefab != null) variants++;
        if (_painter.additionalPrefabs != null)
            foreach (var p in _painter.additionalPrefabs)
                if (p != null) variants++;

        if (variants == 0)
            EditorGUILayout.HelpBox("Drag prefab ke slot 'WaterStuff Prefab' di atas.", MessageType.Warning);
        else
            EditorGUILayout.HelpBox($"✅ {variants} prefab variant siap.", MessageType.Info);

        // ── Target Mesh status ──
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("─── Target Mesh Status ───", EditorStyles.boldLabel);

        if (_painter.targetMesh == null)
        {
            if (_painter.restrictToTargetMesh)
                EditorGUILayout.HelpBox(
                    "⚠️ Target Mesh belum di-assign!\n" +
                    "Drag GameObject mesh ke slot 'Target Mesh' di atas,\n" +
                    "atau matikan 'Restrict To Target Mesh' untuk paint di semua permukaan.",
                    MessageType.Warning);
            else
                EditorGUILayout.HelpBox(
                    "ℹ️ Mode: Paint di semua permukaan (Paint Layer).\n" +
                    "Assign Target Mesh untuk membatasi area painting.",
                    MessageType.Info);
        }
        else
        {
            // Cek apakah ada collider
            var col = _painter.targetMesh.GetComponentInChildren<UnityEngine.Collider>();
            if (col == null)
                EditorGUILayout.HelpBox(
                    $"⚠️ '{_painter.targetMesh.name}' tidak punya Collider!\n" +
                    "Tambahkan Mesh Collider agar painting bisa berfungsi.",
                    MessageType.Error);
            else
                EditorGUILayout.HelpBox(
                    $"✅ Target: '{_painter.targetMesh.name}'\n" +
                    $"Collider: {col.GetType().Name}\n" +
                    (_painter.restrictToTargetMesh
                        ? "🎯 Restricted — foliage hanya muncul di mesh ini."
                        : "🌐 Unrestricted — foliage bisa di semua permukaan."),
                    MessageType.Info);

            // Tombol quick-add collider
            if (col == null)
            {
                if (GUILayout.Button("➕ Add Mesh Collider ke Target Mesh"))
                {
                    var meshFilter = _painter.targetMesh.GetComponentInChildren<MeshFilter>();
                    var mc = _painter.targetMesh.AddComponent<MeshCollider>();
                    if (meshFilter != null) mc.sharedMesh = meshFilter.sharedMesh;
                    EditorUtility.SetDirty(_painter.targetMesh);
                }
            }
        }

        // ── Water mode hint ──
        if (_painter.waterSurfaceMode)
        {
            EditorGUILayout.Space(4);
            if (_painter.waterHeightMode == WaterStuffPainter.WaterHeightMode.FixedY)
            {
                EditorGUILayout.HelpBox(
                    $"💧 Water Mode: Fixed Y = {_painter.waterSurfaceY}\n" +
                    "Semua object akan diletakkan tepat di Y tersebut.\n" +
                    "Garis biru kecil di Gizmo menunjukkan tinggi air.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "💧 Water Mode: Water Layer Raycast\n" +
                    "Pastikan mesh air punya Collider & Layer = Water,\n" +
                    "lalu assign layer tersebut ke slot 'Water Layer'.",
                    MessageType.Info);
            }
        }

        // ── Rotation preview ──
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("─── Rotation Preview ───", EditorStyles.boldLabel);

        if (_painter.rotationMode == WaterStuffPainter.RotationMode.Manual)
        {
            EditorGUILayout.HelpBox(
                $"🔧 Manual Rotation\n" +
                $"X: {_painter.manualRotation.x:F1}°   " +
                $"Y: {_painter.manualRotation.y:F1}°   " +
                $"Z: {_painter.manualRotation.z:F1}°\n" +
                "Semua object yang di-paint pakai rotasi ini.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"🎲 Random Rotation\n" +
                $"X: {_painter.randomRotationX.x:F0}° ~ {_painter.randomRotationX.y:F0}°   " +
                $"Y: {_painter.randomRotationY.x:F0}° ~ {_painter.randomRotationY.y:F0}°   " +
                $"Z: {_painter.randomRotationZ.x:F0}° ~ {_painter.randomRotationZ.y:F0}°\n" +
                $"Tilt tambahan: ±{_painter.randomTilt:F1}°",
                MessageType.Info);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("─── Paint Tool ───", EditorStyles.boldLabel);

        // ── Mode buttons ──
        EditorGUILayout.BeginHorizontal();

        Color activePaint = _painter.waterSurfaceMode ? _paintColorWater : _paintColorGround;
        GUI.backgroundColor = _mode == PaintMode.Paint ? (_painter.waterSurfaceMode ? new Color(0.4f, 0.8f, 1f) : Color.green) : Color.white;
        if (GUILayout.Button(_painter.waterSurfaceMode ? "💧  Paint (Water)" : "🌳  Paint", GUILayout.Height(32)))
            _mode = PaintMode.Paint;

        GUI.backgroundColor = _mode == PaintMode.Erase ? Color.red : Color.white;
        if (GUILayout.Button("⬜  Erase", GUILayout.Height(32)))
            _mode = PaintMode.Erase;

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        _painter.brushRadius    = EditorGUILayout.Slider("Brush Radius",    _painter.brushRadius,    1f, 50f);
        _painter.objectsPerStroke = EditorGUILayout.IntSlider("Per Stroke",   _painter.objectsPerStroke, 1,  20);
        _painter.minSpacing     = EditorGUILayout.Slider("Min Spacing",     _painter.minSpacing,     0f, 20f);

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            $"Total Objects: {_painter.WaterStuffCount}\n\n" +
            "• Tahan klik kiri di Scene = Paint/Erase\n" +
            "• Ctrl + scroll = ubah brush radius\n" +
            "• Shift + klik = paksa erase",
            MessageType.Info);

        EditorGUILayout.Space(4);
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("🗑  Clear All", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog("Clear",
                $"Hapus semua {_painter.WaterStuffCount} object?", "Hapus", "Batal"))
            {
                _painter.ClearAll();
                EditorUtility.SetDirty(_painter);
            }
        }
        GUI.backgroundColor = Color.white;
    }

    void OnSceneGUI()
    {
        if (_painter == null) return;

        bool hasPrefab = _painter.waterStuffPrefab != null;
        if (!hasPrefab && _painter.additionalPrefabs != null)
            foreach (var p in _painter.additionalPrefabs)
                if (p != null) { hasPrefab = true; break; }
        if (!hasPrefab) return;

        Event e = Event.current;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        // Ctrl + scroll = brush radius
        if (e.type == EventType.ScrollWheel && e.control)
        {
            _painter.brushRadius = Mathf.Clamp(_painter.brushRadius - e.delta.y * 0.5f, 1f, 50f);
            e.Use();
            Repaint();
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        // Untuk Water FixedY: raycast ke tanah dulu untuk dapat X,Z,
        // lalu snap Y ke waterSurfaceY
        RaycastHit hit;
        bool gotHit = false;

        if (_painter.waterSurfaceMode && _painter.waterHeightMode == WaterStuffPainter.WaterHeightMode.FixedY)
        {
            // Raycast ke semua layer untuk dapat posisi kursor di scene
            if (Physics.Raycast(ray, out hit, 2000f))
            {
                // Override Y ke water surface
                hit = OverrideHitY(hit, _painter.waterSurfaceY + _painter.waterYOffset);
                gotHit = true;
            }
            else
            {
                // Fallback: intersect ray dengan plane Y = waterSurfaceY
                Plane waterPlane = new Plane(Vector3.up,
                    new Vector3(0f, _painter.waterSurfaceY + _painter.waterYOffset, 0f));
                if (waterPlane.Raycast(ray, out float enter))
                {
                    Vector3 pt = ray.GetPoint(enter);
                    hit = CreateFakeHit(pt, Vector3.up);
                    gotHit = true;
                }
            }
        }
        else
        {
            gotHit = Physics.Raycast(ray, out hit, 2000f, _painter.paintLayer);
        }

        if (gotHit)
        {
            bool forceErase = e.shift;
            bool isErase    = forceErase || _mode == PaintMode.Erase;

            Color brushColor = isErase ? _eraseColor :
                (_painter.waterSurfaceMode ? _paintColorWater : _paintColorGround);

            DrawBrushCircle(hit.point, hit.normal, _painter.brushRadius, brushColor);

            // Label
            Handles.BeginGUI();
            Vector2 guiPos = HandleUtility.WorldToGUIPoint(hit.point + Vector3.up * 0.5f);
            string modeLabel = isErase ? "ERASE" : (_painter.waterSurfaceMode ? "PAINT (WATER)" : "PAINT");
            GUI.Label(new Rect(guiPos.x + 10, guiPos.y - 10, 240, 20),
                $"{modeLabel}  r={_painter.brushRadius:F1}m");
            Handles.EndGUI();

            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            {
                if (e.button == 0)
                {
                    if (isErase)
                        _painter.EraseWaterStuffAt(hit.point, _painter.brushRadius);
                    else
                        _painter.PaintWaterStuffAt(hit.point, hit.normal,
                            _painter.brushRadius, _painter.objectsPerStroke);

                    EditorUtility.SetDirty(_painter);
                    e.Use();
                }
            }
        }

        SceneView.RepaintAll();
    }

    // ── Helpers ──────────────────────────────────────────────

    private RaycastHit OverrideHitY(RaycastHit original, float y)
    {
        // Buat struct baru dengan posisi Y di-override (via reflection tidak perlu,
        // cukup gunakan point yang dimodifikasi di PaintWaterStuffAt melalui waterSurfaceY)
        return original; // posisi Y di-handle oleh TryGetWaterPosition di WaterStuffPainter
    }

    private RaycastHit CreateFakeHit(Vector3 point, Vector3 normal)
    {
        // Untuk Water Plane intersect tanpa collider
        // Kita pakai trick: set di variable lokal saja untuk brush display
        RaycastHit fakeHit = new RaycastHit();
        // Unity tidak expose constructor RaycastHit,
        // jadi kita pakai cara tidak langsung via reflection
        typeof(RaycastHit).GetField("m_Point",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(fakeHit, point);
        typeof(RaycastHit).GetField("m_Normal",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(fakeHit, normal);
        return fakeHit;
    }

    private void DrawBrushCircle(Vector3 center, Vector3 normal, float radius, Color color)
    {
        Handles.color = color;
        Handles.DrawSolidDisc(center, normal, radius);
        Handles.color = new Color(color.r, color.g, color.b, 1f);
        Handles.DrawWireDisc(center, normal, radius);
        Handles.DrawLine(center, center + normal * 2f);
    }

}
