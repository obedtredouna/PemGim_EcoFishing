// Taruh file ini di folder: Assets/Editor/TreePainterEditor.cs
// PENTING: File ini HARUS berada di dalam folder bernama "Editor"

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TreePainter))]
public class TreePainterEditor : Editor
{
    private TreePainter _painter;

    private enum PaintMode { Paint, Erase }
    private PaintMode _mode = PaintMode.Paint;

    private readonly Color _paintColor = new Color(0.2f, 0.9f, 0.3f, 0.35f);
    private readonly Color _eraseColor = new Color(1f, 0.3f, 0.2f, 0.35f);

    void OnEnable()
    {
        _painter = (TreePainter)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("─── Prefab Status ───", EditorStyles.boldLabel);

        int variants = 0;
        if (_painter.treePrefab != null) variants++;
        if (_painter.additionalPrefabs != null)
        {
            foreach (var p in _painter.additionalPrefabs)
                if (p != null) variants++;
        }

        if (variants == 0)
        {
            EditorGUILayout.HelpBox(
                "Drag sebuah Prefab tree ke slot 'Tree Prefab' di atas.\n" +
                "Kamu juga bisa menambah beberapa variasi di 'Additional Prefabs' untuk hasil lebih natural.",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"✅ {variants} tree variant siap dipakai.",
                MessageType.Info);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("─── Paint Tool ───", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = _mode == PaintMode.Paint ? Color.green : Color.white;
        if (GUILayout.Button("🌳  Paint", GUILayout.Height(32)))
            _mode = PaintMode.Paint;

        GUI.backgroundColor = _mode == PaintMode.Erase ? Color.red : Color.white;
        if (GUILayout.Button("⬜  Erase", GUILayout.Height(32)))
            _mode = PaintMode.Erase;
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        _painter.brushRadius    = EditorGUILayout.Slider("Brush Radius", _painter.brushRadius, 1f, 50f);
        _painter.treesPerStroke = EditorGUILayout.IntSlider("Trees per Stroke", _painter.treesPerStroke, 1, 20);
        _painter.minSpacing     = EditorGUILayout.Slider("Min Spacing", _painter.minSpacing, 0f, 20f);

        EditorGUILayout.Space(6);

        EditorGUILayout.HelpBox(
            $"Total Trees: {_painter.TreeCount}\n\n" +
            "Cara pakai:\n" +
            "• Tahan klik kiri di Scene = Paint/Erase\n" +
            "• Ctrl + scroll mouse = ubah brush radius\n" +
            "• Shift + klik = paksa erase",
            MessageType.Info
        );

        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("🗑  Clear All Trees", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog(
                "Clear Trees",
                $"Hapus semua {_painter.TreeCount} tree?",
                "Hapus", "Batal"))
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

        // Cek minimal ada satu prefab
        bool hasPrefab = _painter.treePrefab != null;
        if (!hasPrefab && _painter.additionalPrefabs != null)
        {
            foreach (var p in _painter.additionalPrefabs)
                if (p != null) { hasPrefab = true; break; }
        }
        if (!hasPrefab) return;

        Event e = Event.current;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        // Ctrl + scroll = ubah brush radius
        if (e.type == EventType.ScrollWheel && e.control)
        {
            _painter.brushRadius = Mathf.Clamp(
                _painter.brushRadius - e.delta.y * 0.5f, 1f, 50f
            );
            e.Use();
            Repaint();
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, _painter.paintLayer))
        {
            bool forceErase = e.shift;
            bool isErase = forceErase || _mode == PaintMode.Erase;

            DrawBrushCircle(hit.point, hit.normal, _painter.brushRadius,
                isErase ? _eraseColor : _paintColor);

            Handles.BeginGUI();
            Vector2 guiPos = HandleUtility.WorldToGUIPoint(hit.point + Vector3.up * 0.5f);
            GUI.Label(new Rect(guiPos.x + 10, guiPos.y - 10, 220, 20),
                $"{(isErase ? "ERASE" : "PAINT")}  r={_painter.brushRadius:F1}m");
            Handles.EndGUI();

            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            {
                if (e.button == 0)
                {
                    if (isErase)
                        _painter.EraseTreeAt(hit.point, _painter.brushRadius);
                    else
                        _painter.PaintTreeAt(hit.point, hit.normal,
                            _painter.brushRadius, _painter.treesPerStroke);

                    EditorUtility.SetDirty(_painter);
                    e.Use();
                }
            }
        }

        SceneView.RepaintAll();
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