using System.IO;
using UnityEditor;
using UnityEngine;

public static class PlanarUVTool
{
    const string Folder = "Assets/Models/GeneratedUV";

    [MenuItem("Tools/LudumDare60/Planar UV For Selected")]
    static void Generate()
    {
        int done = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            MeshFilter filter = go.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) continue;

            Mesh source = filter.sharedMesh;
            Mesh mesh = Object.Instantiate(source);
            mesh.name = source.name.Replace("_PlanarUV", "") + "_PlanarUV";

            Vector3[] vertices = mesh.vertices;

            Vector3 size = mesh.bounds.size;
            Vector3 axis = Vector3.forward;
            if (size.x <= size.y && size.x <= size.z) axis = Vector3.right;
            else if (size.y <= size.x && size.y <= size.z) axis = Vector3.up;

            Vector3 tangent = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 bitangent = Vector3.Cross(axis, tangent);

            Vector2[] uv = new Vector2[vertices.Length];
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < vertices.Length; i++)
            {
                uv[i] = new Vector2(Vector3.Dot(vertices[i], tangent), Vector3.Dot(vertices[i], bitangent));
                min = Vector2.Min(min, uv[i]);
                max = Vector2.Max(max, uv[i]);
            }

            float extent = Mathf.Max(max.x - min.x, max.y - min.y, 1e-5f);
            for (int i = 0; i < uv.Length; i++) uv[i] = (uv[i] - min) / extent;

            mesh.uv = uv;
            mesh.RecalculateTangents();

            Directory.CreateDirectory(Folder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{Folder}/{mesh.name}.asset");
            AssetDatabase.CreateAsset(mesh, path);

            Undo.RecordObject(filter, "Planar UV");
            filter.sharedMesh = mesh;
            EditorUtility.SetDirty(filter);
            done++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Planar UV", done > 0 ? $"Готово: {done}" : "Выдели объекты с MeshFilter", "OK");
    }
}
