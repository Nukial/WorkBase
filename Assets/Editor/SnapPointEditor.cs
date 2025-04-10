using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SnapPoint))]
public class SnapPointEditor : Editor {
    public override void OnInspectorGUI() {
        // Vẽ các thuộc tính mặc định
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Connection Test Tools", EditorStyles.boldLabel);
        SnapPoint sp = (SnapPoint)target;
        sp.connectionTestRadius = EditorGUILayout.FloatField("Test Radius", sp.connectionTestRadius);
        sp.drawConnectionTest = EditorGUILayout.Toggle("Draw Connection Test", sp.drawConnectionTest);

        if (GUILayout.Button("Test Connections")) {
            SnapPoint[] allPoints = GameObject.FindObjectsByType<SnapPoint>(FindObjectsSortMode.None);
            foreach (var candidate in allPoints) {
                if (candidate == sp) continue;
                bool canConnect = sp.CanSnapTo(candidate);
                string msg = $"{sp.gameObject.name} -> {candidate.gameObject.name}: " + (canConnect ? "Compatible" : "Not Compatible");
                Debug.Log(msg, sp.gameObject);
            }
        }
    }
}
