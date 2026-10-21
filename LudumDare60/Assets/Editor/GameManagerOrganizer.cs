using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameManagerOrganizer
{
    static readonly (string group, string[] types)[] Groups =
    {
        ("Mission", new[] { "LaunchValidator", "MissionGenerator", "MissionTimer", "MissionSheet" }),
        ("Launch", new[] { "FuelComputer", "ChargeSelector" }),
        ("Monitor", new[] { "RocketFlight", "MonitorCameraDirector", "LaunchCountdown" }),
        ("Effects", new[] { "Siren", "CinemachineImpulseSource", "LaunchShake" })
    };

    [MenuItem("Tools/LudumDare60/Organize GameManager")]
    static void Organize()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null) root = GameObject.Find("GameManager");
        if (root == null)
        {
            EditorUtility.DisplayDialog("Organize GameManager", "Выдели GameManager в иерархии.", "Ок");
            return;
        }

        if (!EditorUtility.DisplayDialog("Organize GameManager",
                $"Разложить компоненты объекта «{root.name}» по дочерним Mission, Launch, Monitor, Effects?\nВсе ссылки в сцене будут перенастроены автоматически.",
                "Да", "Отмена"))
            return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Organize GameManager");

        Dictionary<Component, Component> replacements = new Dictionary<Component, Component>();
        Component[] components = root.GetComponents<Component>();

        foreach ((string groupName, string[] types) in Groups)
        {
            List<Component> found = new List<Component>();
            foreach (string typeName in types)
                foreach (Component component in components)
                    if (component != null && component.GetType().Name == typeName) found.Add(component);

            if (found.Count == 0) continue;

            GameObject child = FindOrCreateChild(root, groupName);
            foreach (Component component in found)
            {
                if (!ComponentUtility.CopyComponent(component)) continue;
                if (!ComponentUtility.PasteComponentAsNew(child)) continue;
                Component[] copies = child.GetComponents(component.GetType());
                replacements[component] = copies[copies.Length - 1];
            }
        }

        if (replacements.Count == 0)
        {
            EditorUtility.DisplayDialog("Organize GameManager", "Нечего переносить.", "Ок");
            return;
        }

        Scene scene = root.scene;
        int fixedReferences = 0;
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        foreach (Component component in sceneRoot.GetComponentsInChildren<Component>(true))
        {
            if (component == null || replacements.ContainsKey(component)) continue;

            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.GetIterator();
            bool changed = false;
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (property.objectReferenceValue is Component old && replacements.TryGetValue(old, out Component replacement))
                {
                    property.objectReferenceValue = replacement;
                    changed = true;
                    fixedReferences++;
                }
            }

            if (changed) serialized.ApplyModifiedProperties();
        }

        foreach (Component old in replacements.Keys) Undo.DestroyObjectImmediate(old);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("Organize GameManager",
            $"Перенесено компонентов: {replacements.Count}\nПеренастроено ссылок: {fixedReferences}\n\nПроверь игру и сохрани сцену. Отменить: Ctrl+Z.",
            "Ок");
    }

    static GameObject FindOrCreateChild(GameObject root, string childName)
    {
        Transform existing = root.transform.Find(childName);
        if (existing != null) return existing.gameObject;

        GameObject child = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(child, "Create " + childName);
        Undo.SetTransformParent(child.transform, root.transform, "Parent " + childName);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        return child;
    }
}
