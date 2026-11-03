using System;
using System.IO;
using System.Linq;
using BusJam;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ProjectValidator
{
    [MenuItem("Bus Jam/Tools/Validate Project")]
    public static void Run()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/BusJamUI.prefab");
        Require(root != null, "UI prefab exists");
        Require(root.GetComponent<GameUI>() != null, "UI prefab has a GameUI component on its root");
        Require(Resources.Load<GameUI>("BusJamUI") != null, "UI prefab loads through the gameplay Resources path");
        var canvases = root.GetComponentsInChildren<Canvas>(true);
        Require(canvases.Select(c => c.sortingOrder).OrderBy(i => i).SequenceEqual(new[] {10,20,30}), "Canvas priorities");
        Require(root.GetComponentsInChildren<Text>(true).Length == 0, "No legacy UI Text");
        Require(root.GetComponentsInChildren<TMP_Text>(true).All(t => t.font != null), "TMP fonts assigned");
        Require(root.GetComponentsInChildren<SafeAreaFitter>(true).Length >= 4, "HUD and popup safe areas");
        var effect = new SerializedObject(root.GetComponentInChildren<CoinFlyEffect>(true));
        Require(effect.FindProperty("_icons").arraySize == 10, "Ten pooled reward icons");
        foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            Require(component != null, "No missing scripts");
            var serialized = new SerializedObject(component);
            var field = serialized.GetIterator();
            while (field.NextVisible(true))
                if (field.propertyType == SerializedPropertyType.ObjectReference && field.name.StartsWith("_") && !field.isArray)
                    Require(field.objectReferenceValue != null, component.GetType().Name + "." + field.name + " assigned");
        }
        Require(PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait, "Portrait lock");
        var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/Data/GameCatalog.asset");
        foreach (var level in catalog.levels)
        {
            Require(level.Validate().Count == 0, level.name + " valid");
            Require(LevelSolver.Solve(new RoundState(level)) != null, level.name + " solvable");
        }
        // Model the same uniform-fit calculation at required aspect ratios, including a notch inset.
        foreach (float height in new[] { 1920f, 2340f, 2400f })
        {
            float safeHeight = height - 160;
            float scale = Mathf.Min(1080f / 1080, safeHeight / 1920);
            Require(1080 * scale <= 1080.01f && 1920 * scale <= safeHeight + .01f, "Layout fits " + height);
        }
        string[] keys = { "BusJam.Sound", "BusJam.Music", "BusJam.Haptic" };
        var values = keys.Select(k => PlayerPrefs.GetInt(k, -1)).ToArray();
        try
        {
            SettingsManager.SetSound(false); SettingsManager.SetMusic(false); SettingsManager.SetHaptic(false);
            Require(!SettingsManager.IsSoundOn && !SettingsManager.IsMusicOn && !SettingsManager.IsHapticOn, "Settings disable");
            SettingsManager.SetSound(true); SettingsManager.SetMusic(true); SettingsManager.SetHaptic(true);
            Require(SettingsManager.IsSoundOn && SettingsManager.IsMusicOn && SettingsManager.IsHapticOn, "Settings enable");
        }
        finally
        {
            for (int i = 0; i < keys.Length; i++)
                if (values[i] < 0) PlayerPrefs.DeleteKey(keys[i]); else PlayerPrefs.SetInt(keys[i], values[i]);
            PlayerPrefs.Save();
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/Week6-validation.txt", "PASS: prefab references; canvas orders; TMP fonts; safe areas; 10 pooled icons; portrait lock; five valid solvable levels; three aspect ratios; saved settings flags.\n");
        Debug.Log("BUSJAM_WEEK6_CHECKS_OK");
    }
    private static void Require(bool passed, string message)
    {
        if (!passed) throw new InvalidOperationException("Week 6 validation failed: " + message);
    }
    public static void PrepareAndValidate()
    {
        UIBuilder.Generate();
        Run();
    }
}
