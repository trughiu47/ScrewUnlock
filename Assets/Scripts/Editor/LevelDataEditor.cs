#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LevelData level = (LevelData)target;

        // Nut mo Level Editor Window
        GUI.color = new Color(0.5f, 1f, 0.6f);
        if (GUILayout.Button("  Open Level Editor  ", GUILayout.Height(34)))
        {
            var win = EditorWindow.GetWindow<LevelEditorWindow>("Level Editor");
            win.Show();
        }
        GUI.color = Color.white;

        GUILayout.Space(4);

        // Thong ke nhanh
        int totalBlocks = 0;
        foreach (var b in level.boards) totalBlocks += b.blocks.Length;

        EditorGUILayout.HelpBox(
            $"Level {level.levelIndex}  |  {level.boards.Length} board(s)  |  " +
            $"{totalBlocks} block(s)  |  Time: {level.timeLimit}s",
            MessageType.None);

        GUILayout.Space(4);
        DrawDefaultInspector();
    }
}
#endif
