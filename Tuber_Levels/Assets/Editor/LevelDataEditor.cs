using UnityEditor;
using UnityEngine;

namespace TuberLevels.Editor
{
    [CustomEditor(typeof(LevelDataSO))]
    public sealed class LevelDataEditor : UnityEditor.Editor
    {
        private string _message;
        private MessageType _messageType;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var level = (LevelDataSO)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Colors are stored bottom to top. Generation verifies a solution before replacing data. Positions use the 720 x 1280 reference canvas.", MessageType.Info);
            if (GUILayout.Button("Generate Random Solvable Level"))
            {
                Undo.RecordObject(level, "Generate level");
                try { LevelSolver.Generate(level); EditorUtility.SetDirty(level); _message = "Generated a verified solvable puzzle."; _messageType = MessageType.Info; }
                catch (System.Exception ex) { _message = ex.Message; _messageType = MessageType.Error; }
            }
            if (GUILayout.Button("Validate Level"))
            {
                bool valid = level.Validate(out _message);
                _messageType = valid ? MessageType.Info : MessageType.Error;
                if (valid)
                {
                    bool solved = LevelSolver.Solve(level.CreateState(), out var moves);
                    _message += solved ? " Verified solution: " + moves.Count + " moves." : " No solution found within the search budget.";
                    if (!solved) _messageType = MessageType.Warning;
                }
            }
            if (GUILayout.Button("Arrange Bottles")) { Undo.RecordObject(level, "Arrange bottles"); LevelSolver.Layout(level); EditorUtility.SetDirty(level); }
            if (GUILayout.Button("Clear Data")) { Undo.RecordObject(level, "Clear level"); level.bottles.Clear(); EditorUtility.SetDirty(level); }
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, _messageType);
        }
    }
}
