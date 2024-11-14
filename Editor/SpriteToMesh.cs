using UnityEditor;
using UnityEngine;

public class SpriteToMesh : Editor
{
    [MenuItem("Assets/Extract Sprite Mesh", true)] // Only enable for Sprite assets
    private static bool ValidateExtractSpriteMesh()
    {
        return Selection.activeObject is Sprite;
    }

    [MenuItem("Assets/Extract Sprite Mesh")]
    private static void ExtractSpriteMesh()
    {
        Sprite sprite = Selection.activeObject as Sprite;
        if (sprite == null)
        {
            Debug.LogError("No sprite selected for extraction.");
            return;
        }

        // Create a new mesh from the sprite's vertices and triangles
        Mesh mesh = new Mesh
        {
            name = sprite.name,
            vertices = System.Array.ConvertAll(sprite.vertices, i => (Vector3)i), // Convert 2D vertices to 3D
            triangles = System.Array.ConvertAll(sprite.triangles, i => (int)i),
            uv = sprite.uv
        };

        // Define asset path and save the mesh as an asset
        string path = AssetDatabase.GetAssetPath(sprite);
        path = System.IO.Path.ChangeExtension(path, ".mesh");
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"Mesh extracted and saved at: {path}");
    }
}
