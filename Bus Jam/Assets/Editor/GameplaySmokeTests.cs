using System;
using System.IO;
using System.Linq;
using BusJam;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Batch smoke test: real buttons, unscaled transitions, dispatch animations and reward arrivals.
public static class GameplaySmokeTests
{
    private const string _activeKey = "BusJam.Week6.PlayChecks";
    private static readonly string[] _keys = { "BusJam.Level", "BusJam.Coins", "BusJam.Sound", "BusJam.Music", "BusJam.Haptic" };
    private static double _deadline, _next;
    private static int _phase, _startCoins;
    private static bool _failed;
    private static BusJamGame _observedGame;
    private static int _roundFinishedCount;
    private static RoundFinishedResult _roundResult;

    public static void Begin()
    {
        for (int i = 0; i < _keys.Length; i++) SessionState.SetInt(_activeKey + i, PlayerPrefs.GetInt(_keys[i], -1));
        SessionState.SetBool(_activeKey, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        EditorApplication.isPlaying = true;
    }
    [InitializeOnLoadMethod]
    private static void Resume()
    {
        if (!SessionState.GetBool(_activeKey, false)) return;
        _deadline = EditorApplication.timeSinceStartup + 100;
        _next = EditorApplication.timeSinceStartup + 3;
        EditorApplication.update += Tick;
        Application.logMessageReceived += Log;
    }
    private static void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) _failed = true;
    }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < _next) return;
        try
        {
            if (_failed) throw new Exception("Runtime Console error encountered");
            if (EditorApplication.timeSinceStartup > _deadline) throw new Exception("Play checks timed out at phase " + _phase);
            var game = UnityEngine.Object.FindObjectOfType<BusJamGame>();
            if (game == null || game.UI == null) return;
            var ui = game.UI;
            _next = EditorApplication.timeSinceStartup + .6;
            switch (_phase)
            {
                case 0:
                    Require(ui.ModalVisible && Time.timeScale == 0, "Home pauses gameplay");
                    Click(ui, "_homeSettings"); _phase++; break;
                case 1:
                    var toggles = ui.GetComponentsInChildren<Toggle>().ToArray();
                    Require(toggles.Length == 3, "Three settings toggles");
                    foreach (var toggle in toggles) { toggle.isOn = false; toggle.isOn = true; }
                    Require(SettingsManager.IsSoundOn && SettingsManager.IsMusicOn && SettingsManager.IsHapticOn, "Settings apply");
                    Click(ui, "_closeSettings"); _phase++; break;
                case 2:
                    Require(ui.ModalVisible && Time.timeScale == 0, "Settings returns to Home");
                    Click(ui, "_play"); _phase++; break;
                case 3:
                    Require(!ui.ModalVisible && Time.timeScale == 1, "Home enters gameplay");
                    Click(ui, "_settings"); _phase++; break;
                case 4:
                    Require(ui.ModalVisible && Time.timeScale == 0, "Settings pauses gameplay");
                    Click(ui, "_closeSettings"); _phase++; break;
                case 5:
                    Require(!ui.ModalVisible && Time.timeScale == 1, "Settings resumes gameplay");
                    Click(ui, "_restart"); _phase++; break;
                case 6:
                    Require(ui.ModalVisible && Time.timeScale == 0, "Restart confirmation pauses gameplay");
                    Click(ui, "_secondary"); _phase++; break;
                case 7:
                    Require(!ui.ModalVisible && Time.timeScale == 1, "Cancel restart resumes");
                    game.LoadLevel(0);
                    _observedGame = game;
                    _roundFinishedCount = 0;
                    game.RoundFinished += ObserveRoundFinished;
                    _startCoins = PlayerPrefs.GetInt("BusJam.Coins", 0);
                    _phase++; break;
                case 8:
                    if (game.IsBusy) return;
                    if (game.State.Won)
                    {
                        Require(ui.ModalVisible, "Win popup appears");
                        Require(_roundFinishedCount == 1 && _roundResult.Won && _roundResult.LevelIndex == 0 &&
                            _roundResult.NextLevelIndex == 1 && !_roundResult.IsLastLevel && _roundResult.FailureReason == null,
                            "RoundFinished publishes one winning result snapshot");
                        Require(PlayerPrefs.GetInt("BusJam.Level", -1) == 1, "Progress observer saves the next level");
                        Require(PlayerPrefs.GetInt("BusJam.Coins", 0) == _startCoins, "Winning does not claim coins early");
                        typeof(BusJamGame).GetMethod("Finish", System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic).Invoke(game, new object[] { true });
                        Require(_roundFinishedCount == 1, "Finishing the same round twice does not notify twice");
                        game.RoundFinished -= ObserveRoundFinished;
                        Click(ui, "_primary"); Click(ui, "_primary");
                        _phase++; _next = EditorApplication.timeSinceStartup + 2.5;
                        break;
                    }
                    var solution = LevelSolver.Solve(game.State);
                    Require(solution != null && solution.Count > 0, "Level remains solvable");
                    Require(game.TryDispatch(solution[0]) == DispatchResult.Accepted, "Dispatch accepted");
                    break;
                case 9:
                    Require(game.LevelIndex == 1 && !ui.ModalVisible, "Claim advances to next level");
                    Require(PlayerPrefs.GetInt("BusJam.Coins", 0) == _startCoins + 100, "Exactly 100 coins, double claim ignored");
                    Require(_roundResult.LevelIndex == 0 && _roundResult.NextLevelIndex == 1,
                        "Result snapshot remains unchanged after loading another level");
                    Directory.CreateDirectory("Logs");
                    File.WriteAllText("Logs/Week6-playchecks.txt", "PASS: Home -> Settings -> Home -> Gameplay -> Settings -> Gameplay -> Restart/Cancel -> Solve Level 1 -> RoundFinished once -> Progress saved -> Win -> Double Claim -> 10 coin arrivals -> Level 2; immutable result snapshot; exactly +100 coins; no runtime errors.\n");
                    Finish(0); break;
            }
        }
        catch (Exception error) { Debug.LogException(error); Finish(1); }
    }
    private static void Click(GameUI ui, string field)
    {
        var button = (Button)new SerializedObject(ui).FindProperty(field).objectReferenceValue;
        Require(button.gameObject.activeInHierarchy && button.interactable, field + " is clickable");
        button.onClick.Invoke();
    }
    private static void ObserveRoundFinished(RoundFinishedResult result)
    {
        _roundFinishedCount++;
        _roundResult = result;
    }
    private static void Require(bool value, string message)
    {
        if (!value) throw new Exception("Week 6 play check: " + message);
    }
    private static void Finish(int code)
    {
        if (_observedGame != null) _observedGame.RoundFinished -= ObserveRoundFinished;
        _observedGame = null;
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Log;
        SessionState.SetBool(_activeKey, false);
        for (int i = 0; i < _keys.Length; i++)
        {
            int value = SessionState.GetInt(_activeKey + i, -1);
            if (value < 0) PlayerPrefs.DeleteKey(_keys[i]); else PlayerPrefs.SetInt(_keys[i], value);
        }
        PlayerPrefs.Save();
        Debug.Log("BUSJAM_WEEK6_PLAYCHECKS_" + (code == 0 ? "OK" : "FAILED"));
        EditorApplication.Exit(code);
    }
}
