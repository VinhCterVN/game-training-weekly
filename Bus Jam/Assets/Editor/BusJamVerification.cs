using System;
using System.Collections.Generic;
using System.IO;
using BusJam;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BusJamVerification
{
    private static int checks;
    private static readonly List<string> results = new List<string>();

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAILED: " + name);
        checks++;
        results.Add("PASS " + name);
    }

    public static void SetupAndVerify()
    {
        BusJamSetup.Setup();
        Verify();
    }

    public static void VerifyAndBuildWebGL()
    {
        Verify();
        BusJamSetup.BuildWebGL();
    }

    public static void PrepareAndBuildSmoke()
    {
        var level = AssetDatabase.LoadAssetAtPath<LevelDataSO>("Assets/BusJam/Levels/Level01.asset");
        BusJamSetup.SetTutorialLayout(level);
        BusJamSetup.GenerateQueue(level, 1);
        EditorUtility.SetDirty(level);
        AssetDatabase.SaveAssets();
        Verify();
        BuildSmoke();
    }

    public static void PolishAndBuildSmoke()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/BusJam/GameCatalog.asset");
        for (int i = 1; i < 5; i++)
        {
            var level = catalog.levels[i];
            BusJamSetup.GenerateLayout(level, new[] { 4, 8, 12, 18, 24 }[i], 193 + i * 57);
            BusJamSetup.GenerateQueue(level, i < 3 ? 2 : 3);
            EditorUtility.SetDirty(level);
        }

        AssetDatabase.SaveAssets();
        BusJamSetup.Setup();
        Verify();
        BuildSmoke();
    }

    [MenuItem("Bus Jam/Run Rule Verification")]
    public static void Verify()
    {
        checks = 0;
        results.Clear();
        var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/BusJam/GameCatalog.asset");
        Check(catalog != null && catalog.levels.Length >= 5, "Five sample levels exist");
        int previousCount = 0;
        foreach (var level in catalog.levels)
        {
            Check(level.Validate().Count == 0, level.name + " valid dimensions, bounds, overlaps and passenger totals");
            Check(level.vehicles.Count > previousCount, level.name + " increases vehicle count");
            previousCount = level.vehicles.Count;
            var round = new RoundState(level);
            var solution = LevelSolver.Solve(round);
            Check(solution != null, level.name + " solver finds a full solution");
            foreach (int id in solution)
            {
                Check(round.Dispatch(id) == DispatchResult.Accepted, level.name + " dispatch " + id);
                Check(round.BoardingTarget() == -1, "No boarding while vehicle is in transit");
                round.Arrive(id);
                round.Settle();
            }

            Check(round.Won && round.Remaining == 0, level.name + " all passengers and vehicles delivered");
        }

        foreach (var art in catalog.vehicles)
            for (int i = 0; i < 4; i++)
                Check(art.closed[i] != null && art.seats[i] != null,
                    $"{art.kind} {art.color} {(Facing)i} roof/seat sprites");
        Check(new VehiclePlacement { kind = VehicleKind.Car }.Capacity == 4, "Cars seat four");
        Check(new VehiclePlacement { kind = VehicleKind.Bus }.Capacity == 6, "Buses seat six");
        var test = ScriptableObject.CreateInstance<LevelDataSO>();
        test.columns = 6;
        test.rows = 6;
        test.bayCount = 2;
        test.vehicles = new List<VehiclePlacement>
        {
            new VehiclePlacement { x = 0, y = 0, facing = Facing.Up, color = ColorID.Blue },
            new VehiclePlacement { x = 0, y = 3, facing = Facing.Right, color = ColorID.Red },
            new VehiclePlacement { x = 3, y = 0, facing = Facing.Down, color = ColorID.Yellow }
        };
        test.passengers = new List<ColorID>
        {
            ColorID.Blue, ColorID.Blue, ColorID.Blue, ColorID.Blue, ColorID.Red, ColorID.Red, ColorID.Red, ColorID.Red,
            ColorID.Yellow, ColorID.Yellow, ColorID.Yellow, ColorID.Yellow
        };
        var state = new RoundState(test);
        Check(!state.CanEscape(0) && state.Dispatch(0) == DispatchResult.Blocked,
            "Forward lane blocked by perpendicular footprint");
        Check(state.CanEscape(1) && state.CanEscape(2), "Right and downward edge escapes");
        state.Dispatch(1);
        Check(state.Bays[0] == 1, "Bay reserved before arrival");
        Check(state.CanEscape(0), "Dispatched vehicle releases grid occupancy");
        Check(state.BoardingTarget() == -1, "Cannot skip blue head passenger for red vehicle");
        state.Arrive(1);
        state.Dispatch(2);
        Check(!state.Deadlocked, "In-flight vehicle postpones deadlock decision");
        state.Arrive(2);
        Check(state.Deadlocked, "Full mismatched bays trigger loss");
        Check(state.Dispatch(0) == DispatchResult.BaysFull && state.Phases[0] == VehiclePhase.Grid,
            "Full bays reject launch without removing occupancy");
        state = new RoundState(test);
        state.Dispatch(1);
        state.Arrive(1);
        state.Dispatch(0);
        state.Arrive(0);
        state.Board(0);
        Check(state.Loads[0] == 1 && state.Phases[0] == VehiclePhase.Waiting, "Partial vehicle stays parked");
        bool threw = false;
        try
        {
            state.Depart(0);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Check(threw, "Partial vehicle cannot depart");
        state.Settle();
        Check(state.Delivered == 2 && state.Bays[0] == -1 && state.Bays[1] == -1,
            "Full vehicles depart and release bays");
        Check(!state.Won, "Win requires every vehicle and passenger");
        state.Dispatch(2);
        state.Arrive(2);
        state.Settle();
        Check(state.Won, "Win after all delivered");
        test.vehicles[0].facing = Facing.Left;
        Check(new RoundState(test).CanEscape(0), "Left edge escape");
        test.vehicles[0].x = 5;
        Check(test.Validate().Count > 0, "Out-of-bounds placement rejected");
        Check(!BusJamSetup.GenerateQueue(test, 2), "Queue generator safely rejects an out-of-bounds layout");
        test.vehicles[0].x = 0;
        test.vehicles[1].x = 0;
        test.vehicles[1].y = 0;
        Check(test.Validate().Count > 0, "Overlapping placement rejected");
        UnityEngine.Object.DestroyImmediate(test);
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/rule-verification.txt", results);
        Debug.Log($"BUSJAM_RULES_OK: {checks} checks passed.");
    }

    public static void BuildSmoke()
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MainScene.unity" }, locationPathName = "Builds/Smoke/BusJam.exe",
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Smoke build failed");
        Debug.Log("BUSJAM_SMOKE_BUILD_OK");
    }
}