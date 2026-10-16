using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TuberLevels
{
    public sealed class GameSmokeTest : MonoBehaviour
    {
        private LevelManager _levels;
        private WaterSortController _sort;
        private UIManager _ui;
        private string _output;

        public void Initialize(LevelManager levels, WaterSortController sort, UIManager ui)
        {
            _levels = levels;
            _sort = sort;
            _ui = ui;
            StartCoroutine(Run());
        }

        private void Require(bool condition, string message)
        {
            if (condition)
            {
                Debug.Log("PASS: " + message);
                return;
            }

            Debug.LogError("FAIL: " + message);
            File.WriteAllText(Path.Combine(_output, "FAILED.txt"), message);
            Application.Quit(1);
            throw new System.InvalidOperationException(message);
        }

        private IEnumerator Capture(string name)
        {
            // Explicit rendering also captures UI in a hidden batch player.
            var canvas = FindObjectOfType<Canvas>();
            var camera = Camera.main;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            yield return null;
            var target = new RenderTexture(Screen.width, Screen.height, 24);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.Combine(_output, name + ".png"), pixels.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            target.Release();
            Destroy(target);
            Destroy(pixels);
        }

        private void Click(BottleController bottle)
        {
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current)
                { position = RectTransformUtility.WorldToScreenPoint(null, bottle.transform.position) };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Require(hits.Count > 0 && hits[0].gameObject == bottle.gameObject,
                "UI raycast reaches bottle " + bottle.Index);
            ExecuteEvents.Execute(bottle.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        private IEnumerator Run()
        {
            _output = Path.Combine(Application.dataPath, "..", "QA");
            Directory.CreateDirectory(_output);
            yield return null;
            PlayerPrefs.SetInt("TubeLevels.Test.Current", 0);
            PlayerPrefs.SetInt("TubeLevels.Test.Unlocked", 0);
            for (int i = 0; i < 5; i++) PlayerPrefs.DeleteKey("TubeLevels.Test.Complete." + i);
            _levels.Initialize(_sort, _ui);
            yield return Capture("level-1");
            _ui.OpenLevels();
            yield return Capture("level-picker");
            _ui.CloseOverlays();
            for (int level = 0; level < _levels.Database.levels.Count; level++)
            {
                Require(_levels.CurrentLevelIndex == level, "Next level progression " + (level + 1));
                _levels.Initialize(_sort, _ui);
                Require(_levels.CurrentLevelIndex == level, "Reinitialization resumes saved level " + (level + 1));
                Require(_levels.Pool.CreatedCount == 10, "Exactly ten pooled bottles");
                Require(LevelSolver.Solve(_levels.Snapshot(), out List<PourMove> solution),
                    "Level " + (level + 1) + " is solvable");
                if (level == 0)
                {
                    var first = solution[0];
                    Click(_levels.Bottles[first.from]);
                    Click(_levels.Bottles[first.to]);
                    yield return new WaitForSeconds(.38f);
                    yield return Capture("pour-animation");
                    while (_sort.IsBusy) yield return null;
                    _sort.Undo();
                    Require(_sort.Moves == 0, "Undo restores move count");
                    var initial = _levels.Database.levels[0].CreateState();
                    var actual = _levels.Snapshot();
                    for (int i = 0; i < initial.Count; i++)
                        Require(string.Join(",", initial[i]) == string.Join(",", actual[i]),
                            "Undo restores bottle " + i);
                    _levels.Replay();
                    Require(_levels.Pool.CreatedCount == 10 && _sort.Moves == 0,
                        "Replay reuses pool and clears history");
                }

                if (level == 4) yield return Capture("level-5");
                Time.timeScale = 8;
                foreach (var move in solution)
                {
                    Click(_levels.Bottles[move.from]);
                    Click(_levels.Bottles[move.to]);
                    int frames = 0;
                    while (_sort.IsBusy && frames++ < 600) yield return null;
                    Require(frames < 600, "Pour animation completes");
                }

                Time.timeScale = 1;
                Require(_sort.HasWon && _ui.Win.gameObject.activeSelf, "Win popup for level " + (level + 1));
                Require(_levels.IsCompleted(level), "Completion saved for level " + (level + 1));
                yield return new WaitForSeconds(.4f);
                if (level == 0 || level == 4) yield return Capture("win-" + (level + 1));
                var next = _ui.Win.transform.Find("Card/Next").GetComponent<Button>();
                next.onClick.Invoke();
                yield return null;
            }

            Require(_levels.CurrentLevelIndex == 0, "Final level loops to level one");
            Require(_levels.Pool.CreatedCount == 10, "No bottle allocations across all levels");
            _ui.OpenLevels();
            yield return Capture("completed-levels");
            File.WriteAllText(Path.Combine(_output, "PASSED.txt"),
                "All five levels solved through animated pointer-click handlers. Undo, replay, pooling, win popup, Next, completion persistence and final loop passed.");
            Debug.Log("SMOKE TEST PASSED");
            Application.Quit(0);
        }
    }
}