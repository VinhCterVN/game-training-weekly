using System.IO;
using BusJam;
using UnityEditor;
using UnityEngine;

public static class BusJamScenerySetup
{
    private const string LinePath = "Assets/Sprites/Park/parking_line.png";

    public static void Create(BusJamGame game)
    {
        var scenery = new GameObject("Static scenery").transform;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(scenery.gameObject, game.gameObject.scene);
        var background = new GameObject("Mart, trees, sidewalk and road").AddComponent<SpriteRenderer>();
        background.transform.SetParent(scenery, false);
        background.sprite = game.catalog.background;
        background.sortingOrder = -20;
        background.transform.localScale = new Vector3(10.8f / background.sprite.bounds.size.x,
            19.2f / background.sprite.bounds.size.y, 1);

        var parking = new GameObject("Parking bays").transform;
        parking.SetParent(scenery, false);
        var white = ParkingLineSprite();
        game.parkingBays = new Transform[3];
        int count = game.catalog.levels[0].bayCount;
        for (int i = 0; i < game.parkingBays.Length; i++)
        {
            var bay = new GameObject("Bay " + (i + 1)).transform;
            bay.SetParent(parking, false);
            bay.localPosition = new Vector3((i - (count - 1) * .5f) * 2.15f, 3.85f, 0);
            for (int d = 0; d < 8; d++)
            {
                Dash(bay, white, new Vector2(-.62f, -.88f + d * .25f), new Vector2(.035f, .14f));
                Dash(bay, white, new Vector2(.62f, -.88f + d * .25f), new Vector2(.035f, .14f));
            }

            for (int d = 0; d < 5; d++)
            {
                Dash(bay, white, new Vector2(-.5f + d * .25f, -.99f), new Vector2(.14f, .035f));
                Dash(bay, white, new Vector2(-.5f + d * .25f, .99f), new Vector2(.14f, .035f));
            }

            game.parkingBays[i] = bay;
            bay.gameObject.SetActive(i < count);
        }

        EditorUtility.SetDirty(game);
    }

    private static Sprite ParkingLineSprite()
    {
        if (!File.Exists(LinePath))
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            File.WriteAllBytes(LinePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(LinePath);
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(LinePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 2;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(LinePath);
    }

    private static void Dash(Transform parent, Sprite sprite, Vector2 position, Vector2 size)
    {
        var dash = new GameObject("Parking line").AddComponent<SpriteRenderer>();
        dash.transform.SetParent(parent, false);
        dash.transform.localPosition = position;
        dash.transform.localScale = new Vector3(size.x, size.y, 1);
        dash.sprite = sprite;
        dash.color = new Color(1, 1, 1, .85f);
        dash.sortingOrder = -5;
    }
}
