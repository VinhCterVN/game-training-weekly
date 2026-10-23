using System.Collections.Generic;
using BusJam;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class BusJamLevelEditor : EditorWindow
{
    private LevelDataSO level;
    private VehicleKind kind;
    private ColorID color;
    private Facing facing;
    private Vector2 scroll;
    private string report = "Select a level to begin.";
    private SerializedObject serializedLevel;
    private ReorderableList passengerList;
    private GameCatalog catalog;

    [MenuItem("Bus Jam/Level Editor")]
    public static void Open() => GetWindow<BusJamLevelEditor>("Bus Jam Levels");

    private void OnEnable()
    {
        minSize = new Vector2(520, 600);
        catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/BusJam/GameCatalog.asset");
    }

    private void Bind()
    {
        if (level == null) return;
        serializedLevel = new SerializedObject(level);
        passengerList = new ReorderableList(serializedLevel, serializedLevel.FindProperty("passengers"), true, true,
            true, true);
        passengerList.drawHeaderCallback = r => EditorGUI.LabelField(r, "Passenger order • first entry boards first");
        passengerList.drawElementCallback = (r, index, active, focused) =>
        {
            r.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(r, passengerList.serializedProperty.GetArrayElementAtIndex(index),
                new GUIContent((index + 1).ToString()));
        };
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("BUS JAM / LEVEL WORKSHOP", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        level = (LevelDataSO)EditorGUILayout.ObjectField("Level", level, typeof(LevelDataSO), false);
        if (EditorGUI.EndChangeCheck()) Bind();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("New level")) NewLevel();
        if (GUILayout.Button("Save")) AssetDatabase.SaveAssets();
        if (GUILayout.Button("Open game")) BusJamSetup.OpenGame();
        EditorGUILayout.EndHorizontal();
        if (level == null) return;
        if (serializedLevel == null || serializedLevel.targetObject != level) Bind();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        serializedLevel.Update();
        EditorGUILayout.PropertyField(serializedLevel.FindProperty("title"));
        EditorGUILayout.PropertyField(serializedLevel.FindProperty("hint"));
        EditorGUILayout.PropertyField(serializedLevel.FindProperty("columns"));
        EditorGUILayout.PropertyField(serializedLevel.FindProperty("rows"));
        EditorGUILayout.PropertyField(serializedLevel.FindProperty("bayCount"));
        serializedLevel.ApplyModifiedProperties();
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        kind = (VehicleKind)EditorGUILayout.EnumPopup(kind);
        color = (ColorID)EditorGUILayout.EnumPopup(color);
        facing = (Facing)EditorGUILayout.EnumPopup(facing);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox(
            "Left click an empty cell to place. Right click a vehicle to remove. Shift + click rotates it. Coordinates mark its bottom-left cell. Cars: 1×2 / 4 seats; buses: 1×3 / 6 seats.",
            MessageType.Info);
        DrawGrid();
        EditorGUILayout.LabelField($"{level.vehicles.Count} vehicles • {level.passengers.Count} passengers",
            EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate solvable queue"))
        {
            Undo.RecordObject(level, "Generate passenger queue");
            report = BusJamSetup.GenerateQueue(level, level.bayCount)
                ? "Queue generated from a valid escape order. Validate to check the complete puzzle."
                : "Fix out-of-bounds, overlapping, or mutually blocked vehicles before generating a queue.";
            EditorUtility.SetDirty(level);
            Bind();
        }

        if (GUILayout.Button("Validate + solve")) ValidateLevel();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox(report, MessageType.None);
        serializedLevel.Update();
        passengerList.DoLayoutList();
        serializedLevel.ApplyModifiedProperties();
        EditorGUILayout.Space();
        if (GUILayout.Button("Add this level to campaign"))
        {
            if (catalog != null)
            {
                Undo.RecordObject(catalog, "Add campaign level");
                var levels = new List<LevelDataSO>(catalog.levels);
                if (!levels.Contains(level)) levels.Add(level);
                catalog.levels = levels.ToArray();
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
                report = "Added to campaign. Campaign order is editable on GameCatalog.";
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawGrid()
    {
        float size = Mathf.Min((position.width - 42) / level.columns, 52);
        Rect area = GUILayoutUtility.GetRect(level.columns * size, level.rows * size);
        for (int x = 0; x < level.columns; x++)
        for (int y = 0; y < level.rows; y++)
        {
            var r = new Rect(area.x + x * size, area.y + (level.rows - y - 1) * size, size - 1, size - 1);
            EditorGUI.DrawRect(r, new Color(.22f, .27f, .34f));
        }

        for (int i = 0; i < level.vehicles.Count; i++)
        {
            var v = level.vehicles[i];
            var r = new Rect(area.x + v.x * size + 2, area.y + (level.rows - v.y - v.Height) * size + 2,
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
            cy = level.rows - 1 - Mathf.FloorToInt((e.mousePosition.y - area.y) / size);
        int found = level.vehicles.FindIndex(v => cx >= v.x && cx < v.x + v.Width && cy >= v.y && cy < v.y + v.Height);
        Undo.RecordObject(level, "Edit vehicle grid");
        if (found >= 0)
        {
            if (e.button == 1) level.vehicles.RemoveAt(found);
            else if (e.shift)
            {
                var old = level.vehicles[found];
                var candidate = old.Copy();
                candidate.facing = (Facing)(((int)candidate.facing + 1) % 4);
                if (Fits(candidate, found)) level.vehicles[found] = candidate;
                else report = "Rotation would overlap another vehicle or leave the board.";
            }
        }
        else if (e.button == 0)
        {
            var v = new VehiclePlacement { kind = kind, color = color, facing = facing, x = cx, y = cy };
            if (Fits(v, -1)) level.vehicles.Add(v);
            else report = "That vehicle does not fit here.";
        }

        EditorUtility.SetDirty(level);
        e.Use();
        Repaint();
    }

    private bool Fits(VehiclePlacement v, int except)
    {
        if (v.x < 0 || v.y < 0 || v.x + v.Width > level.columns || v.y + v.Height > level.rows) return false;
        for (int i = 0; i < level.vehicles.Count; i++)
        {
            var old = level.vehicles[i];
            if (i != except && v.x < old.x + old.Width && v.x + v.Width > old.x && v.y < old.y + old.Height &&
                v.y + v.Height > old.y) return false;
        }

        return true;
    }

    private void ValidateLevel()
    {
        var issues = level.Validate();
        if (issues.Count > 0)
        {
            report = string.Join("\n", issues);
            return;
        }

        var solution = LevelSolver.Solve(new RoundState(level));
        report = solution == null
            ? "No solution found within the 150,000-state search limit. Review the grid and passenger order."
            : "Solvable. Vehicle order: " + string.Join(" → ", solution.ConvertAll(i => (i + 1).ToString()));
    }

    private void NewLevel()
    {
        var path = EditorUtility.SaveFilePanelInProject("New Bus Jam level", "Level", "asset",
            "Choose where to save the level", "Assets/BusJam/Levels");
        if (string.IsNullOrEmpty(path)) return;
        level = CreateInstance<LevelDataSO>();
        AssetDatabase.CreateAsset(level, path);
        AssetDatabase.SaveAssets();
        Bind();
    }
}