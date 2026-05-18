// Taruh file ini di folder: Assets/Editor/GrassPainterEditor.cs
// PENTING: File ini HARUS berada di dalam folder bernama "Editor"

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GrassPainter))]
public class GrassPainterEditor : Editor
{
    private GrassPainter _painter;

    // Mode
    private enum PaintMode { Paint, Erase }
    private PaintMode _mode = PaintMode.Paint;

    // State
    private bool _isPainting = false;
    private Vector3 _brushPos;
    private bool _brushVisible = false;

    // Warna brush
    private readonly Color _paintColor = new Color(0.2f, 1f, 0.3f, 0.35f);
    private readonly Color _eraseColor = new Color(1f, 0.3f, 0.2f, 0.35f);

    void OnEnable()
    {
        _painter = (GrassPainter)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("─── Paint Tool ───", EditorStyles.boldLabel);

        // Mode toggle
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = _mode == PaintMode.Paint ? Color.green : Color.white;
        if (GUILayout.Button("🖌  Paint", GUILayout.Height(32)))
            _mode = PaintMode.Paint;

        GUI.backgroundColor = _mode == PaintMode.Erase ? Color.red : Color.white;
        if (GUILayout.Button("⬜  Erase", GUILayout.Height(32)))
            _mode = PaintMode.Erase;
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Brush settings
        _painter.brushRadius = EditorGUILayout.Slider("Brush Radius", _painter.brushRadius, 0.5f, 20f);
        _painter.grassPerStroke = EditorGUILayout.IntSlider("Grass per Stroke", _painter.grassPerStroke, 1, 50);

        EditorGUILayout.Space(6);

        // Info
        EditorGUILayout.HelpBox(
            $"Total Grass: {_painter.GrassCount}\n" +
            $"Batches: {Mathf.CeilToInt(_painter.GrassCount / 1023f)}\n\n" +
            "Cara pakai:\n" +
            "• Tahan klik kiri di Scene = Paint/Erase\n" +
            "• Scroll mouse = ubah brush radius\n" +
            "• Shift + klik = paksa erase",
            MessageType.Info
        );

        EditorGUILayout.Space(4);

        // Clear button
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("🗑  Clear All Grass", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog(
                "Clear Grass",
                $"Hapus semua {_painter.GrassCount} grass?",
                "Hapus", "Batal"))
            {
                Undo.RecordObject(_painter, "Clear Grass");
                _painter.ClearAll();
                EditorUtility.SetDirty(_painter);
            }
        }
        GUI.backgroundColor = Color.white;

        // Rebuild button
        if (GUILayout.Button("↺  Rebuild Batches", GUILayout.Height(24)))
        {
            _painter.RebuildBatches();
        }
    }

    void OnSceneGUI()
    {
        if (_painter == null) return;

        Event e = Event.current;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        // Scroll wheel = ubah brush radius
        if (e.type == EventType.ScrollWheel && e.control)
        {
            _painter.brushRadius = Mathf.Clamp(
                _painter.brushRadius - e.delta.y * 0.2f, 0.5f, 20f
            );
            e.Use();
            Repaint();
        }

        // Raycast dari mouse ke scene
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        _brushVisible = false;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _painter.paintLayer))
        {
            _brushVisible = true;
            _brushPos = hit.point;

            // Paksa erase jika tahan Shift
            bool forceErase = e.shift;
            bool isErase = forceErase || _mode == PaintMode.Erase;

            // Gambar brush circle di permukaan
            DrawBrushCircle(hit.point, hit.normal, _painter.brushRadius,
                isErase ? _eraseColor : _paintColor);

            // Label info brush
            Handles.BeginGUI();
            Vector2 guiPos = HandleUtility.WorldToGUIPoint(hit.point + Vector3.up * 0.5f);
            GUI.Label(new Rect(guiPos.x + 10, guiPos.y - 10, 200, 20),
                $"{(isErase ? "ERASE" : "PAINT")}  r={_painter.brushRadius:F1}m");
            Handles.EndGUI();

            // Paint / Erase saat mouse ditekan / drag
            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            {
                if (e.button == 0)
                {
                    Undo.RecordObject(_painter, isErase ? "Erase Grass" : "Paint Grass");

                    if (isErase)
                        _painter.EraseGrassAt(hit.point, _painter.brushRadius);
                    else
                        _painter.PaintGrassAt(hit.point, hit.normal,
                            _painter.brushRadius, _painter.grassPerStroke);

                    EditorUtility.SetDirty(_painter);
                    e.Use();
                }
            }
        }

        SceneView.RepaintAll();
    }

    /// <summary>
    /// Gambar circle brush yang mengikuti normal permukaan
    /// </summary>
    private void DrawBrushCircle(Vector3 center, Vector3 normal, float radius, Color color)
    {
        Handles.color = color;
        Handles.DrawSolidDisc(center, normal, radius);

        Handles.color = new Color(color.r, color.g, color.b, 1f);
        Handles.DrawWireDisc(center, normal, radius);

        // Garis kecil di tengah
        Handles.DrawLine(center, center + normal * 0.5f);
    }
}
