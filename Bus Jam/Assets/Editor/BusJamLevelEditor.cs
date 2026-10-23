using System.Collections.Generic;
using BusJam;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class BusJamLevelEditor : EditorWindow
{
    private LevelDataSO _level;
    private VehicleKind _kind;
    private ColorID _color;
    private Facing _facing;
    private Vector2 _scroll;
    private string _report = "Select a level to begin.";
    private SerializedObject _serializedLevel;
    private ReorderableList _passengerList;
    private GameCatalog _catalog;

    [MenuItem("Bus Jam/Level Editor")]
    public static void Open() => GetWindow<BusJamLevelEditor>("Bus Jam Levels");

    private void OnEnable()
    {
        minSize = new Vector2(520, 600);
        _catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/BusJam/GameCatalog.asset");
    }

    private void Bind()
    {
        if (_level == null) return;
        _serializedLevel = new SerializedObject(_level);
        _passengerList = new ReorderableList(_serializedLevel, _serializedLevel.FindProperty("passengers"), true, true,
            true, true);
        _passengerList.drawHeaderCallback = r => EditorGUI.LabelField(r, "Passenger order • first entry boards first");
        _passengerList.drawElementCallback = (r, index, active, focused) =>
        {
            r.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(r, _passengerList.serializedProperty.GetArrayElementAtIndex(index),
                new GUIContent((index + 1).ToString()));
        };
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("BUS JAM / LEVEL WORKSHOP", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _level = (LevelDataSO)EditorGUILayout.ObjectField("Level", _level, typeof(LevelDataSO), false);
        if (EditorGUI.EndChangeCheck()) Bind();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("New level")) NewLevel();
        if (GUILayout.Button("Save")) AssetDatabase.SaveAssets();
        if (GUILayout.Button("Open game")) BusJamSetup.OpenGame();
        EditorGUILayout.EndHorizontal();
        if (_level == null) return;
        if (_serializedLevel == null || _serializedLevel.targetObject != _level) Bind();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _serializedLevel.Update();
        EditorGUILayout.PropertyField(_serializedLevel.FindProperty("title"));
        EditorGUILayout.PropertyField(_serializedLevel.FindProperty("hint"));
        EditorGUILayout.PropertyField(_serializedLevel.FindProperty("columns"));
        EditorGUILayout.PropertyField(_serializedLevel.FindProperty("rows"));
        EditorGUILayout.PropertyField(_serializedLevel.FindProperty("bayCount"));
        _serializedLevel.ApplyModifiedProperties();
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        _kind = (VehicleKind)EditorGUILayout.EnumPopup(_kind);
        _color = (ColorID)EditorGUILayout.EnumPopup(_color);
        _facing = (Facing)EditorGUILayout.EnumPopup(_facing);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox(
            "Left click an empty cell to place. Right click a vehicle to remove. Shift + click rotates it. Coordinates mark its bottom-left cell. Cars: 1×2 / 4 seats; buses: 1×3 / 6 seats.",
            MessageType.Info);
        DrawGrid();
        EditorGUILayout.LabelField($"{_level.vehicles.Count} vehicles • {_level.passengers.Count} passengers",
            EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate solvable queue"))
        {
            Undo.RecordObject(_level, "Generate passenger queue");
            _report = BusJamSetup.GenerateQueue(_level, _level.bayCount)
                ? "Queue generated from a valid escape order. Validate to check the complete puzzle."
                : "Fix out-of-bounds, overlapping, or mutually blocked vehicles before generating a queue.";
            EditorUtility.SetDirty(_level);
            Bind();
        }

        if (GUILayout.Button("Validate + solve")) ValidateLevel();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox(_report, MessageType.None);
        _serializedLevel.Update();
        _passengerList.DoLayoutList();
        _serializedLevel.ApplyModifiedProperties();
        EditorGUILayout.Space();
        if (GUILayout.Button("Add this level to campaign"))
        {
            if (_catalog != null)
            {
                Undo.RecordObject(_catalog, "Add campaign level");
                var levels = new List<LevelDataSO>(_catalog.levels);
                if (!levels.Contains(_level)) levels.Add(_level);
                _catalog.levels = levels.ToArray();
                EditorUtility.SetDirty(_catalog);
                AssetDatabase.SaveAssets();
                _report = "Added to campaign. Campaign order is editable on GameCatalog.";
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawGrid()
    {
        float size = Mathf.Min((position.width - 42) / _level.columns, 52);
        Rect area = GUILayoutUtility.GetRect(_level.columns * size, _level.rows * size);
        for (int x = 0; x < _level.columns; x++)
        for (int y = 0; y < _level.rows; y++)
        {
            var r = new Rect(area.x + x * size, area.y + (_level.rows - y - 1) * size, size - 1, size - 1);
            EditorGUI.DrawRect(r, new Color(.22f, .27f, .34f));
        }

        for (int i = 0; i < _level.vehicles.Count; i++)
        {
            var v = _level.vehicles[i];
            var r = new Rect(area.x + v.x * size + 2, area.y + (_level.rows - v.y - v.Height) * size + 2,
                v.Width * size - 5, v.Height * size - 5);
            EditorGUI.DrawRect(r, GameCatalog.Tint(v.color));
            string arrow = v.facing == Facing.Up ? "↑" :
                v.facing == Facing.Down ? "↓" :
                v.facing == Facing.Left ? "←" : "→";
            GUI.Label(r, $"{i + 1} {arrow}\n{v.Capacity}",
                new GUIStyle(EditorStyles.boldLabel)
                    { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
        }

        var e = Event.current;
        if (e.type != EventType.MouseDown || !area.Contains(e.mousePosition)) return;
        int cx = Mathf.FloorToInt((e.mousePosition.x - area.x) / size),
            cy = _level.rows - 1 - Mathf.FloorToInt((e.mousePosition.y - area.y) / size);
        int found = _level.vehicles.FindIndex(v => cx >= v.x && cx < v.x + v.Width && cy >= v.y && cy < v.y + v.Height);
        Undo.RecordObject(_level, "Edit vehicle grid");
        if (found >= 0)
        {
            if (e.button == 1) _level.vehicles.RemoveAt(found);
            else if (e.shift)
            {
                var old = _level.vehicles[found];
                var candidate = old.Copy();
                candidate.facing = (Facing)(((int)candidate.facing + 1) % 4);
                if (Fits(candidate, found)) _level.vehicles[found] = candidate;
                else _report = "Rotation would overlap another vehicle or leave the board.";
            }
        }
        else if (e.button == 0)
        {
            var v = new VehiclePlacement { kind = _kind, color = _color, facing = _facing, x = cx, y = cy };
            if (Fits(v, -1)) _level.vehicles.Add(v);
            else _report = "That vehicle does not fit here.";
        }

        EditorUtility.SetDirty(_level);
        e.Use();
        Repaint();
    }

    private bool Fits(VehiclePlacement v, int except)
    {
        if (v.x < 0 || v.y < 0 || v.x + v.Width > _level.columns || v.y + v.Height > _level.rows) return false;
        for (int i = 0; i < _level.vehicles.Count; i++)
        {
            var old = _level.vehicles[i];
            if (i != except && v.x < old.x + old.Width && v.x + v.Width > old.x && v.y < old.y + old.Height &&
                v.y + v.Height > old.y) return false;
        }

        return true;
    }

    private void ValidateLevel()
    {
        var issues = _level.Validate();
        if (issues.Count > 0)
        {
            _report = string.Join("\n", issues);
            return;
        }

        var solution = LevelSolver.Solve(new RoundState(_level));
        _report = solution == null
            ? "No solution found within the 150,000-state search limit. Review the grid and passenger order."
            : "Solvable. Vehicle order: " + string.Join(" → ", solution.ConvertAll(i => (i + 1).ToString()));
    }

    private void NewLevel()
    {
        var path = EditorUtility.SaveFilePanelInProject("New Bus Jam level", "Level", "asset",
            "Choose where to save the level", "Assets/BusJam/Levels");
        if (string.IsNullOrEmpty(path)) return;
        _level = CreateInstance<LevelDataSO>();
        AssetDatabase.CreateAsset(_level, path);
        AssetDatabase.SaveAssets();
        Bind();
    }
}