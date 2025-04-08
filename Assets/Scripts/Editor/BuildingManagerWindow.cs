using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BuildingManagerWindow : EditorWindow {
    private List<BuildingPieceSO> pieces = new List<BuildingPieceSO>();

    [MenuItem("Tools/Quản Lý Công Trình")]
    public static void ShowWindow() {
        GetWindow(typeof(BuildingManagerWindow), false, "Quản Lý Công Trình");
    }

    void OnEnable() {
        // Tìm tất cả các asset BuildingPieceSO
        string[] guids = AssetDatabase.FindAssets("t:BuildingPieceSO");
        pieces.Clear();
        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BuildingPieceSO piece = AssetDatabase.LoadAssetAtPath<BuildingPieceSO>(path);
            if(piece != null)
                pieces.Add(piece);
        }
    }

    void OnGUI() {
        GUILayout.Label("Danh sách mảnh công trình", EditorStyles.boldLabel);
        if(pieces.Count == 0) {
            GUILayout.Label("Không tìm thấy mảnh công trình!");
        }
        else {
            foreach(var piece in pieces) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(piece, typeof(BuildingPieceSO), false);
                if(GUILayout.Button("Xóa", GUILayout.Width(50))) {
                    string path = AssetDatabase.GetAssetPath(piece);
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.Refresh();
                    OnEnable();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.Space();
        if(GUILayout.Button("Tạo mảnh công trình mới")) {
            BuildingPieceSO newPiece = CreateInstance<BuildingPieceSO>();
            string assetPath = EditorUtility.SaveFilePanelInProject("Lưu BuildingPieceSO", "NewBuildingPiece", "asset", "Nhập tên asset");
            if(!string.IsNullOrEmpty(assetPath)) {
                AssetDatabase.CreateAsset(newPiece, assetPath);
                AssetDatabase.SaveAssets();
                OnEnable();
            }
        }
    }
}
