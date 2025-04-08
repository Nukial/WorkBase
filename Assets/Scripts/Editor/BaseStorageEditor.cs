using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(BaseStorage))]
public class BaseStorageEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        BaseStorage storage = (BaseStorage)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quản lý tài nguyên", EditorStyles.boldLabel);

        // Hiển thị, chỉnh sửa và xóa từng tài nguyên đã có
        if(storage.storedResourceEntries != null) {
            for (int i = 0; i < storage.storedResourceEntries.Count; i++) {
                EditorGUILayout.BeginHorizontal();
                var entry = storage.storedResourceEntries[i];
                string resName = entry.resource != null ? entry.resource.resourceName : "Null";
                EditorGUILayout.LabelField(resName, GUILayout.Width(100));
                int newAmount = EditorGUILayout.IntField(entry.amount);
                if(newAmount != entry.amount) {
                    entry.amount = newAmount;
                    EditorUtility.SetDirty(storage);
                }
                if(GUILayout.Button("Xóa", GUILayout.Width(50))) {
                    storage.storedResourceEntries.RemoveAt(i);
                    EditorUtility.SetDirty(storage);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}