using UnityEngine;
using UnityEditor;
using WorkBase.Building;

public enum SnapKey {
    Forward,
    Back,
    Right,
    Left
}

public class SnapPointAssigner : EditorWindow {
    GameObject targetObject;
    SnapType selectedSnapType = SnapType.None;
    SnapPoint.SnapDirection selectedDirection = SnapPoint.SnapDirection.Forward;
    ConnectionType selectedConnectionType = ConnectionType.Opposite;
    float offsetDistance = 1f;
    
    // Add fields for quantity and spacing
    int snapPointCount = 1;
    float snapPointSpacing = 0.5f;
    
    // Add counters to track snap points
    private int addedSnapPointsCount = 0;
    private int existingSnapPointsCount = 0;

    [MenuItem("Tools/Snap Point Assigner")]
    public static void ShowWindow() {
        GetWindow<SnapPointAssigner>("Snap Point Assigner");
    }

    void OnGUI() {
        GUILayout.Label("Assign Snap Point", EditorStyles.boldLabel);
        
        // Track if target object changed to update counts
        EditorGUI.BeginChangeCheck();
        GameObject newTargetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck()) {
            targetObject = newTargetObject;
            UpdateSnapPointCount();
        }
        
        selectedSnapType = (SnapType)EditorGUILayout.EnumPopup("Snap Type", selectedSnapType);
        selectedDirection = (SnapPoint.SnapDirection)EditorGUILayout.EnumPopup("Snap Direction", selectedDirection);
        selectedConnectionType = (ConnectionType)EditorGUILayout.EnumPopup("Connection Type", selectedConnectionType);
        offsetDistance = EditorGUILayout.FloatField("Offset Distance", offsetDistance);
        
        EditorGUILayout.Space();
        
        // Multiple snap points settings
        GUILayout.Label("Multiple Snap Points", EditorStyles.boldLabel);
        snapPointCount = EditorGUILayout.IntSlider("Number of Points", snapPointCount, 1, 10);
        
        if (snapPointCount > 1) {
            snapPointSpacing = EditorGUILayout.FloatField("Spacing Between Points", snapPointSpacing);
        }
        
        // Display snap point counts
        EditorGUILayout.Space();
        if (targetObject != null) {
            EditorGUILayout.LabelField($"Existing Snap Points: {existingSnapPointsCount}");
        }
        EditorGUILayout.LabelField($"Snap Points Added This Session: {addedSnapPointsCount}");

        if (GUILayout.Button("Add Snap Point" + (snapPointCount > 1 ? "s" : ""))) {
            if (targetObject == null) {
                EditorUtility.DisplayDialog("Error", "Please select a target GameObject.", "OK");
                return;
            }
            AddSnapPoints();
        }
    }

    void AddSnapPoints() {
        // Check if we have a building piece component, add if needed
        BuildingPiece bp = targetObject.GetComponent<BuildingPiece>();
        if (bp == null) {
            bp = targetObject.AddComponent<BuildingPiece>();
        }
        
        // Create group for undo
        Undo.IncrementCurrentGroup();
        string undoName = "Add " + snapPointCount + " Snap Points";
        Undo.SetCurrentGroupName(undoName);
        int undoGroup = Undo.GetCurrentGroup();
        
        // Create the specified number of snap points
        for (int i = 0; i < snapPointCount; i++) {
            GameObject snapPointObj = new GameObject("SnapPoint_" + selectedSnapType.ToString() + "_" + selectedDirection.ToString() + (snapPointCount > 1 ? "_" + (i+1).ToString() : ""));
            snapPointObj.transform.parent = targetObject.transform;

            // Calculate position - adjust based on index for multiple points
            Vector3 offset = CalculateOffset(i);
            snapPointObj.transform.localPosition = offset;
            snapPointObj.transform.localRotation = Quaternion.identity;

            // Add component and set properties
            SnapPoint sp = snapPointObj.AddComponent<SnapPoint>();
            sp.pointType = selectedSnapType;
            sp.snapDirection = selectedDirection;
            sp.connectionType = selectedConnectionType;
            
            // Register with building piece
            bp.RegisterSnapPoint(sp);
            
            // Register for undo
            Undo.RegisterCreatedObjectUndo(snapPointObj, "Create Snap Point");
            
            // Increment counter
            addedSnapPointsCount++;
        }
        
        // Group all operations as a single undo step
        Undo.CollapseUndoOperations(undoGroup);
        
        // Update counts
        UpdateSnapPointCount();
    }

    // Calculate position offset based on index for multiple points
    Vector3 CalculateOffset(int index) {
        Vector3 baseOffset;
        Renderer rend = targetObject.GetComponent<Renderer>();
        
        if (rend != null) {
            baseOffset = GetDirectionVector(selectedDirection) * rend.bounds.extents.magnitude;
        } else {
            baseOffset = GetDirectionVector(selectedDirection) * offsetDistance;
        }
        
        // For multiple points, distribute them perpendicular to the main direction
        if (snapPointCount > 1 && index > 0) {
            // Calculate perpendicular vector based on the main direction
            Vector3 perpendicular = Vector3.up;
            if (selectedDirection == SnapPoint.SnapDirection.Up || selectedDirection == SnapPoint.SnapDirection.Down) {
                perpendicular = Vector3.right;
            }
            
            // Calculate spacing (centered around the base position)
            float spacing = snapPointSpacing * (index - (snapPointCount - 1) / 2.0f);
            baseOffset += perpendicular * spacing;
        }
        
        return baseOffset;
    }
    
    void UpdateSnapPointCount() {
        existingSnapPointsCount = 0;
        if (targetObject != null) {
            // Get all child SnapPoints
            SnapPoint[] snapPoints = targetObject.GetComponentsInChildren<SnapPoint>();
            existingSnapPointsCount = snapPoints.Length;
        }
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
