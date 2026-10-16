using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TuberLevels.Editor
{
    public static class Lab2Verification
    {
        private static int _checks;

        private static void Check(bool value, string message)
        {
            if (!value) throw new Exception("Verification failed: " + message);
            _checks++;
        }

        private static List<WaterColor> Colors(params WaterColor[] colors) => new(colors);

        [MenuItem("Tube Levels/Run Logic Verification")]
        public static void Run()
        {
            _checks = 0;
            var blue = WaterColor.Blue;
            var red = WaterColor.Red;
            Check(WaterSortRules.PourCount(Colors(), Colors()) == 0, "empty source rejected");
            var same = Colors(blue);
            Check(WaterSortRules.PourCount(same, same) == 0, "self pour rejected");
            Check(WaterSortRules.PourCount(Colors(blue), Colors(red)) == 0, "different color rejected");
            Check(WaterSortRules.PourCount(Colors(blue), Colors(blue, blue, blue, blue)) == 0,
                "full destination rejected");
            Check(WaterSortRules.PourCount(Colors(red, blue, blue, blue), Colors()) == 3,
                "entire top color run transfers");
            Check(WaterSortRules.PourCount(Colors(red, blue, blue, blue), Colors(blue, blue)) == 2,
                "capacity caps transfer");
            Check(!WaterSortRules.IsComplete(Colors(blue, blue)), "partial monochrome is not complete");
            Check(WaterSortRules.IsComplete(Colors(blue, blue, blue, blue)), "full monochrome is complete");
            var database =
                AssetDatabase.LoadAssetAtPath<LevelDatabaseSO>("Assets/ScriptableObjects/LevelDatabase.asset");
            Check(database != null && database.levels.Count == 5, "five levels present");
            var report = new System.Text.StringBuilder();
            foreach (var level in database.levels)
            {
                Check(level.Validate(out string message), level.name + " validation: " + message);
                foreach (var bottle in level.bottles)
                    Check(!WaterSortRules.IsComplete(bottle.colors), "no pre-completed bottles");
                var state = level.CreateState();
                Check(LevelSolver.Solve(state, out var solution), level.name + " solvable");
                foreach (var move in solution)
                {
                    Check(WaterSortRules.PourCount(state[move.from], state[move.to]) == move.count,
                        "solution move is legal");
                    WaterSortRules.Apply(state[move.from], state[move.to], move.count);
                }

                Check(WaterSortRules.IsWon(state), "solution reaches win");
                report.AppendLine(level.name + ": " + level.bottles.Count + " bottles, " + level.colorCount +
                                  " colors, verified solution " + solution.Count + " moves.");
            }

            var generated = ScriptableObject.CreateInstance<LevelDataSO>();
            for (int colors = 3; colors <= 8; colors++)
            {
                generated.colorCount = colors;
                generated.emptyBottles = 2;
                generated.seed = colors * 41;
                LevelSolver.Generate(generated);
                Check(generated.Validate(out _), "generated level valid");
                Check(LevelSolver.Solve(generated.CreateState(), out _), "generated level solvable");
            }

            generated.bottles[0].colors.RemoveAt(0);
            Check(!generated.Validate(out _), "unbalanced color count rejected");
            UnityEngine.Object.DestroyImmediate(generated);
            string[] sounds = { "Click", "Pour", "Complete", "Win", "Error" };
            foreach (string sound in sounds)
                Check(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + sound + ".wav") != null,
                    sound + " imported");
            foreach (var component in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
            {
                Check(component != null, "no missing scripts");
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.name.StartsWith("_"))
                        Check(property.objectReferenceValue != null,
                            component.name + "." + property.name + " assigned");
            }

            Directory.CreateDirectory("Logs");
            report.AppendLine(_checks + " checks passed.");
            File.WriteAllText("Logs/logic-verification.txt", report.ToString());
            Debug.Log(report.ToString());
        }
    }
}