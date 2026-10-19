using UnityEditor;
using UnityEngine;

public static class ScreenUVFitter
{
    [MenuItem("Tools/LudumDare60/Fit Monitor Texture To Screen UV")]
    static void Fit()
    {
        GameObject selected = Selection.activeGameObject;
        MeshRenderer meshRenderer = selected != null ? selected.GetComponent<MeshRenderer>() : null;
        MeshFilter meshFilter = selected != null ? selected.GetComponent<MeshFilter>() : null;

        if (meshRenderer == null || meshFilter == null || meshFilter.sharedMesh == null)
        {
            EditorUtility.DisplayDialog("Fit Monitor Texture", "Выделите в Hierarchy объект экрана (с MeshRenderer и MeshFilter).", "OK");
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        Material[] materials = meshRenderer.sharedMaterials;

        int subMesh = -1;
        for (int i = 0; i < materials.Length && i < mesh.subMeshCount; i++)
        {
            if (materials[i] != null && materials[i].HasProperty("_BaseMap") && materials[i].GetTexture("_BaseMap") is RenderTexture)
            {
                subMesh = i;
                break;
            }
        }

        if (subMesh < 0)
        {
            EditorUtility.DisplayDialog("Fit Monitor Texture", "На этом объекте не найден материал с Render Texture в Base Map.", "OK");
            return;
        }

        Vector2[] uvs;
        int[] triangles;
        Vector3[] vertices;
        try
        {
            uvs = mesh.uv;
            triangles = mesh.GetTriangles(subMesh);
            vertices = mesh.vertices;
        }
        catch
        {
            EditorUtility.DisplayDialog("Fit Monitor Texture", "Не удалось прочитать меш. Включите Read/Write в настройках импорта модели и попробуйте снова.", "OK");
            return;
        }

        if (uvs.Length == 0 || triangles.Length == 0)
        {
            EditorUtility.DisplayDialog("Fit Monitor Texture", "У этой части модели нет UV-развёртки.", "OK");
            return;
        }

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        Bounds shape = new Bounds(vertices[triangles[0]], Vector3.zero);

        foreach (int index in triangles)
        {
            min = Vector2.Min(min, uvs[index]);
            max = Vector2.Max(max, uvs[index]);
            shape.Encapsulate(vertices[index]);
        }

        Vector2 size = max - min;
        if (size.x <= 0f || size.y <= 0f)
        {
            EditorUtility.DisplayDialog("Fit Monitor Texture", "UV экрана вырождены (нулевой размер).", "OK");
            return;
        }

        Vector2 tiling = new Vector2(1f / size.x, 1f / size.y);
        Vector2 offset = new Vector2(-min.x / size.x, -min.y / size.y);

        Material material = materials[subMesh];
        Undo.RecordObject(material, "Fit Monitor Texture");
        material.SetTextureScale("_BaseMap", tiling);
        material.SetTextureOffset("_BaseMap", offset);
        EditorUtility.SetDirty(material);

        Vector3 extents = Vector3.Scale(shape.size, selected.transform.lossyScale);
        float[] sides = { extents.x, extents.y, extents.z };
        System.Array.Sort(sides);
        float screenAspect = sides[1] > 0f ? sides[2] / sides[1] : 0f;

        RenderTexture rt = (RenderTexture)material.GetTexture("_BaseMap");
        float textureAspect = (float)rt.width / rt.height;

        Debug.Log($"[Fit Monitor Texture] UV экрана: от {min} до {max}. Tiling = {tiling}, Offset = {offset}.\n" +
                  $"Пропорции экрана примерно {screenAspect:0.00}, у Render Texture {textureAspect:0.00} ({rt.width}x{rt.height}).");
    }
}
