#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace BusJam
{
    // Opt-in development-player test. Excluded entirely from release players.
    public class BusJamSmoke : MonoBehaviour
    {
        private BusJamGame game;
        private string folder;
        private bool failed;
        private bool hadProgress;
        private int savedProgress;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-busJamSmoke") >= 0)
                new GameObject("Development smoke test").AddComponent<BusJamSmoke>();
        }

        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                failed = true;
                if (folder != null)
                    File.AppendAllText(Path.Combine(folder, "failure.txt"), message + "\n" + stack + "\n");
            }
        }

        private IEnumerator Start()
        {
            folder = Path.Combine(Application.dataPath, "..", "Evidence");
            Directory.CreateDirectory(folder);
            hadProgress = PlayerPrefs.HasKey("BusJam.Level");
            savedProgress = PlayerPrefs.GetInt("BusJam.Level", 0);
            Screen.SetResolution(540, 960, false);
            yield return null;
            yield return new WaitForSecondsRealtime(1);
            game = FindObjectOfType<BusJamGame>();
            Capture("start.png");
            yield return new WaitForSecondsRealtime(.3f);
            game.UI.Hide();
            int vehiclePool = game.Pools.Vehicles.CountAll,
                passengerPool = game.Pools.Passengers.CountAll,
                effectPool = game.Pools.Effects.CountAll;
            for (int level = 0; level < game.catalog.levels.Length; level++)
            {
                game.LoadLevel(level);
                yield return new WaitForSecondsRealtime(.25f);
                Capture($"level{level + 1}.png");
                yield return new WaitForSecondsRealtime(.25f);
                var solution = LevelSolver.Solve(game.State);
                if (solution == null)
                {
                    Fail("No solution for level " + level);
                    yield break;
                }

                Time.timeScale = 6;
                foreach (int id in solution)
                {
                    if (game.TryDispatch(id) != DispatchResult.Accepted)
                    {
                        Fail("Rejected solution move " + id);
                        yield break;
                    }

                    float deadline = Time.realtimeSinceStartup + 15;
                    while (game.IsBusy && Time.realtimeSinceStartup < deadline && !failed) yield return null;
                    if (failed || game.IsBusy)
                    {
                        Fail("Animation timeout or exception");
                        yield break;
                    }

                    if (game.Pools.Vehicles.CountAll != vehiclePool ||
                        game.Pools.Passengers.CountAll != passengerPool || game.Pools.Effects.CountAll != effectPool)
                    {
                        Fail("Pool grew during gameplay");
                        yield break;
                    }
                }

                if (!game.State.Won)
                {
                    Fail("Level did not win");
                    yield break;
                }

                Time.timeScale = 1;
                yield return new WaitForSecondsRealtime(.15f);
                Capture($"win{level + 1}.png");
                yield return new WaitForSecondsRealtime(.15f);
            }

            // Restart during a live movement must cancel callbacks and return all objects.
            game.LoadLevel(1);
            var route = LevelSolver.Solve(game.State);
            game.TryDispatch(route[0]);
            yield return new WaitForSecondsRealtime(.12f);
            game.LoadLevel(1);
            if (game.IsBusy || game.Pools.Passengers.CountActive != Mathf.Min(20, game.State.Remaining) ||
                game.Pools.Vehicles.CountActive != game.State.Level.vehicles.Count)
            {
                Fail("Retry did not reclaim all pooled objects");
                yield break;
            }

            game.TogglePause();
            if (Time.timeScale != 0)
            {
                Fail("Pause did not freeze gameplay");
                yield break;
            }

            game.TogglePause();
            if (Time.timeScale != 1)
            {
                Fail("Resume did not restore gameplay");
                yield break;
            }

            // Verify the actual loss dialog using a deliberately bad dispatch sequence.
            game.LoadLevel(1);
            var losing = new System.Collections.Generic.List<int>();
            if (!FindLoss(game.State, losing, 0))
            {
                Fail("Missing reachable loss scenario");
                yield break;
            }

            Time.timeScale = 6;
            foreach (int id in losing)
            {
                game.TryDispatch(id);
                float deadline = Time.realtimeSinceStartup + 15;
                while (game.IsBusy && Time.realtimeSinceStartup < deadline) yield return null;
                if (game.IsBusy)
                {
                    Fail("Loss flow timeout");
                    yield break;
                }
            }

            Time.timeScale = 1;
            if (!game.State.Deadlocked || !game.UI.ModalVisible)
            {
                Fail("Deadlock did not show loss dialog");
                yield break;
            }

            Capture("lose.png");
            File.WriteAllText(Path.Combine(folder, "smoke-success.txt"),
                "Five animated levels won; pool counts stable; mid-movement retry and pause/resume passed.\nVehicle pool: " +
                vehiclePool + "; Passenger pool: " + passengerPool + "; Effect pool: " + effectPool);
            RestoreProgress();
            Debug.Log("BUSJAM_RUNTIME_OK");
            Application.Quit(0);
        }

        private void RestoreProgress()
        {
            if (hadProgress) PlayerPrefs.SetInt("BusJam.Level", savedProgress);
            else PlayerPrefs.DeleteKey("BusJam.Level");
            PlayerPrefs.Save();
        }

        private void Fail(string message)
        {
            RestoreProgress();
            File.AppendAllText(Path.Combine(folder, "failure.txt"), message + "\n");
            Debug.LogError(message);
            Application.Quit(1);
        }

        private bool FindLoss(RoundState state, System.Collections.Generic.List<int> path, int depth)
        {
            if (state.Deadlocked) return true;
            if (depth > 7 || state.Won) return false;
            for (int i = 0; i < state.Phases.Length; i++)
            {
                var next = state.Copy();
                if (next.Dispatch(i) != DispatchResult.Accepted) continue;
                next.Arrive(i);
                next.Settle();
                path.Add(i);
                if (FindLoss(next, path, depth + 1)) return true;
                path.RemoveAt(path.Count - 1);
            }

            return false;
        }

        private void Capture(string name)
        {
            // Render explicitly: hidden Windows players do not present a swapchain screenshot.
            var camera = game.gameCamera;
            var canvas = game.UI.GetComponent<Canvas>();
            var texture = RenderTexture.GetTemporary(540, 960, 24);
            var oldRect = camera.rect;
            var oldTarget = camera.targetTexture;
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            int oldOrder = canvas.sortingOrder;
            camera.targetTexture = texture;
            camera.rect = new Rect(0, 0, 1, 1);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            canvas.sortingOrder = 1000;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var oldActive = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(540, 960, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 540, 960), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(folder, name), image.EncodeToPNG());
            Destroy(image);
            RenderTexture.active = oldActive;
            camera.targetTexture = oldTarget;
            camera.rect = oldRect;
            canvas.renderMode = oldMode;
            canvas.worldCamera = oldCamera;
            canvas.sortingOrder = oldOrder;
            RenderTexture.ReleaseTemporary(texture);
            Canvas.ForceUpdateCanvases();
        }
    }
}
#endif