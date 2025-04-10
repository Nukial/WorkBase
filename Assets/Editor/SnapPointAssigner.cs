using UnityEngine;
using UnityEditor;

public class SnapPointAssigner : EditorWindow {
    GameObject targetObject;
    SnapType selectedSnapType = SnapType.None;
    SnapPoint.SnapDirection selectedDirection = SnapPoint.SnapDirection.Forward;
    ConnectionType selectedConnectionType = ConnectionType.Opposite;
    float offsetDistance = 1f;

    [MenuItem("Tools/Snap Point Assigner")]
    public static void ShowWindow() {
        GetWindow<SnapPointAssigner>("Snap Point Assigner");
    }

    void OnGUI() {
        GUILayout.Label("Assign Snap Point", EditorStyles.boldLabel);
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true);
        selectedSnapType = (SnapType)EditorGUILayout.EnumPopup("Snap Type", selectedSnapType);
        selectedDirection = (SnapPoint.SnapDirection)EditorGUILayout.EnumPopup("Snap Direction", selectedDirection);
        selectedConnectionType = (ConnectionType)EditorGUILayout.EnumPopup("Connection Type", selectedConnectionType);
        offsetDistance = EditorGUILayout.FloatField("Offset Distance", offsetDistance);

        if (GUILayout.Button("Add Snap Point")) {
            if (targetObject == null) {
                EditorUtility.DisplayDialog("Error", "Please select a target GameObject.", "OK");
                return;
            }
            AddSnapPoint();
        }
    }

    void AddSnapPoint() {
        // Tạo GameObject con mới cho SnapPoint
        GameObject snapPointObj = new GameObject("SnapPoint_" + selectedSnapType.ToString());
        snapPointObj.transform.parent = targetObject.transform;

        // Tính toán offset dựa trên bounds nếu có Renderer
        Vector3 offset = Vector3.zero;
        Renderer rend = targetObject.GetComponent<Renderer>();
        if (rend != null) {
            // Dùng kích thước extents của bounds
            offset = GetDirectionVector(selectedDirection) * rend.bounds.extents.magnitude;
        } else {
            offset = GetDirectionVector(selectedDirection) * offsetDistance;
        }
        snapPointObj.transform.localPosition = offset;
        snapPointObj.transform.localRotation = Quaternion.identity;

        // Thêm component SnapPoint và thiết lập các thuộc tính
        SnapPoint sp = snapPointObj.AddComponent<SnapPoint>();
        sp.pointType = selectedSnapType;
        sp.snapDirection = selectedDirection;
        sp.connectionType = selectedConnectionType;

        // Cho phép Undo
        Undo.RegisterCreatedObjectUndo(snapPointObj, "Add SnapPoint");
    }

    Vector3 GetDirectionVector(SnapPoint.SnapDirection dir) {
        switch (dir) {
            case SnapPoint.SnapDirection.Forward: return Vector3.forward;
            case SnapPoint.SnapDirection.Back: return Vector3.back;
            case SnapPoint.SnapDirection.Up: return Vector3.up;
            case SnapPoint.SnapDirection.Down: return Vector3.down;
            case SnapPoint.SnapDirection.Right: return Vector3.right;
            case SnapPoint.SnapDirection.Left: return Vector3.left;
            default: return Vector3.forward;
        }
    }
}
