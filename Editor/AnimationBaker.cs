using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.U2D.Animation;
using UnityEngine.UIElements;

public class AnimationBaker : EditorWindow
{
    [MenuItem("Window/2D Vat Baker")]
    static void ShowWindow() => GetWindow<AnimationBaker>();

    public GameObject GameObject;
    public Space Space = Space.Self;
    public int AnimationFps = 5;

    public SpriteSkin SpriteSkin;
    public AnimationClip Clip;

    private Button _bakeButton;

    private Texture2D _tex;

    private void CreateGUI()
    {
        var root = rootVisualElement;

        var gameObjectPropertyField = new PropertyField() { bindingPath = nameof(GameObject) };
        _bakeButton = new Button(Bake) { text = nameof(Bake) };

        root.Add(gameObjectPropertyField);
        root.Add(new PropertyField() { bindingPath = nameof(Space) });
        root.Add(new PropertyField() { bindingPath = nameof(AnimationFps) });
        root.Add(new PropertyField() { bindingPath = nameof(Clip) });
        root.Add(new PropertyField() { bindingPath = nameof(SpriteSkin) });
        root.Add(_bakeButton);

        root.Bind(new SerializedObject(this));
    }

    public void Bake()
    {
        var assetName = $"{GameObject.name}_{Clip.name}";
        EditorCoroutineUtility.StartCoroutine(BakeClip(assetName, GameObject, SpriteSkin, Clip, AnimationFps, Space), this);
    }

    //

    public IEnumerator BakeClip(string name, GameObject gameObject, SpriteSkin skin, AnimationClip clip, float fps, Space space)
    {
        var vertexCount = skin.GetDeformedVertexPositionData().ToList<Vector3>().Count;
        var frameCount = Mathf.FloorToInt(clip.length * fps) + 1; // for loop

        var posTex = new Texture2D(vertexCount, frameCount, TextureFormat.RGBAHalf, false, true)
        {
            name = $"{name}.posTex",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat
        };

        using var poolVtx0 = ListPool<Vector3>.Get(out var tmpVertexList);
        using var poolVtx1 = ListPool<Vector3>.Get(out var localVertices);
        
        var dt = 1f / fps;
        for (var i = 0; i < frameCount; i++)
        {
            clip.SampleAnimation(gameObject, dt * i);
            SceneView.RepaintAll();
            tmpVertexList = skin.GetDeformedVertexPositionData().ToList<Vector3>();

            localVertices.AddRange(tmpVertexList);
            yield return new EditorWaitForSeconds(0.1f);
        }

        var trans = gameObject.transform;

        var vertices = space switch
        {
            Space.Self => localVertices.Select(vtx => trans.InverseTransformPoint(vtx)),
            Space.World => localVertices,
            _ => throw new ArgumentOutOfRangeException(nameof(space), space, null)
        };


        posTex.SetPixels(ListToColorArray(vertices));

        _tex = posTex;

        static Color[] ListToColorArray(IEnumerable<Vector3> list) =>
            list.Select(v3 => new Color(v3.x, v3.y, v3.z)).ToArray();
        
        GenerateAssets(name, SpriteSkin, AnimationFps, Clip.length, null, _tex);
    }

    public static void GenerateAssets(string name, SpriteSkin skin, float fps, float animLength, Shader shader, Texture posTex)
    {
        const string folderName = "2DVatBakerOutput";

        var folderPath = CombinePathAndCreateFolderIfNotExist("Assets", folderName, false);
        var subFolderPath = CombinePathAndCreateFolderIfNotExist(folderPath, name);
        
        AssetDatabase.CreateAsset(posTex, CreatePath(subFolderPath, posTex.name, "asset"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Asset saved at: {CreatePath(subFolderPath, posTex.name, "asset")}");

        static string CreatePath(string folder, string file, string extension)
            => Path.Combine(folder, $"{ReplaceInvalidPathChar(file)}.{extension}");
    }


    static string CombinePathAndCreateFolderIfNotExist(string parent, string folderName, bool unique = true)
    {
        parent = ReplaceInvalidPathChar(parent);
        folderName = ReplaceInvalidPathChar(folderName);

        var path = Path.Combine(parent, folderName);

        if (unique)
        {
            path = AssetDatabase.GenerateUniqueAssetPath(path);
        }

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }

        return path;

    }

    static readonly string InvalidChars = new string(Path.GetInvalidPathChars());

    static string ReplaceInvalidPathChar(string path)
    {
        return Regex.Replace(path, $"[{InvalidChars}]", "_");
    }
}
