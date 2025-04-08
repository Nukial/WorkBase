using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(SnapPoint))]
public class SnapPointEditor : Editor
{
    private SnapPoint snapPoint;
    private bool showTestOptions = false;
    private List<SnapPoint> nearbySnapPoints = new List<SnapPoint>();
    private Dictionary<SnapPoint, bool> compatibilityResults = new Dictionary<SnapPoint, bool>();

    private void OnEnable()
    {
        snapPoint = (SnapPoint)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Công cụ hỗ trợ", EditorStyles.boldLabel);

        // Nút để điều chỉnh vị trí
        if (GUILayout.Button("Đặt mũi tên hướng ra ngoài"))
        {
            AdjustArrowDirection();
        }

        // Nút để điền sẵn accepted types phổ biến
        if (GUILayout.Button("Thiết lập AcceptedTypes theo kiểu phổ biến"))
        {
            SetCommonAcceptedTypes();
        }

        // Tùy chọn kiểm tra
        showTestOptions = EditorGUILayout.Foldout(showTestOptions, "Kiểm tra tương thích");
        if (showTestOptions)
        {
            if (GUILayout.Button("Tìm các snap point gần đó"))
            {
                FindNearbySnapPoints();
            }

            if (nearbySnapPoints.Count > 0)
            {
                EditorGUILayout.LabelField("Snap Points trong phạm vi 3 đơn vị:", EditorStyles.boldLabel);
                foreach (var point in nearbySnapPoints)
                {
                    if (point == null) continue;

                    bool canSnap = compatibilityResults[point];
                    GUI.color = canSnap ? Color.green : Color.red;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{point.name} ({point.pointType})");
                    EditorGUILayout.LabelField(canSnap ? "✓ Có thể kết nối" : "✗ Không thể kết nối");
                    EditorGUILayout.EndHorizontal();

                    GUI.color = Color.white;
                }
            }
        }
    }

    private void AdjustArrowDirection()
    {
        // Giả định rằng mũi tên nên hướng ra ngoài khỏi center của parent object
        if (snapPoint.transform.parent != null)
        {
            Vector3 fromCenter = snapPoint.transform.position - snapPoint.transform.parent.position;
            fromCenter.Normalize();

            // Tính toán hướng snap từ vector fromCenter
            SetBestDirection(fromCenter);
        }
        else
        {
            EditorUtility.DisplayDialog("Cảnh báo", "Snap point nên là con của một game object khác để tự động điều chỉnh hướng.", "OK");
        }
    }

    private void SetBestDirection(Vector3 direction)
    {
        // Tìm trục chính gần nhất với hướng
        float dotX = Vector3.Dot(direction, snapPoint.transform.right);
        float dotY = Vector3.Dot(direction, snapPoint.transform.up);
        float dotZ = Vector3.Dot(direction, snapPoint.transform.forward);

        float absDotX = Mathf.Abs(dotX);
        float absDotY = Mathf.Abs(dotY);
        float absDotZ = Mathf.Abs(dotZ);

        if (absDotX > absDotY && absDotX > absDotZ)
        {
            snapPoint.snapDirection = dotX > 0 ? SnapPoint.SnapDirection.Right : SnapPoint.SnapDirection.Left;
        }
        else if (absDotY > absDotX && absDotY > absDotZ)
        {
            snapPoint.snapDirection = dotY > 0 ? SnapPoint.SnapDirection.Up : SnapPoint.SnapDirection.Down;
        }
        else
        {
            snapPoint.snapDirection = dotZ > 0 ? SnapPoint.SnapDirection.Forward : SnapPoint.SnapDirection.Back;
        }

        EditorUtility.SetDirty(snapPoint);
    }

    private void SetCommonAcceptedTypes()
    {
        // Đặt các loại chấp nhận phổ biến dựa trên loại snap point
        snapPoint.acceptedTypes.Clear();

        switch (snapPoint.pointType)
        {
            case SnapType.FoundationTopEdge:
            case SnapType.FoundationTopCorner:
                snapPoint.acceptedTypes.Add(SnapType.WallBottom);
                snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
                snapPoint.acceptedTypes.Add(SnapType.PillarBottom);
                break;

            case SnapType.WallBottom:
                snapPoint.acceptedTypes.Add(SnapType.FoundationTopEdge);
                snapPoint.acceptedTypes.Add(SnapType.FoundationTopCorner);
                snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
                break;

            case SnapType.WallTop:
                snapPoint.acceptedTypes.Add(SnapType.RoofBottomEdge);
                snapPoint.acceptedTypes.Add(SnapType.Ceiling);
                snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
                break;

            case SnapType.WallSide:
                snapPoint.acceptedTypes.Add(SnapType.WallSide);
                snapPoint.acceptedTypes.Add(SnapType.DoorFrameSide);
                snapPoint.acceptedTypes.Add(SnapType.WindowFrameSide);
                break;

            case SnapType.FloorEdge:
                snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
                snapPoint.acceptedTypes.Add(SnapType.WallBottom);
                snapPoint.acceptedTypes.Add(SnapType.WallTop);
                break;

            case SnapType.RoofBottomEdge:
                snapPoint.acceptedTypes.Add(SnapType.WallTop);
                snapPoint.acceptedTypes.Add(SnapType.Ceiling);
                break;

            case SnapType.RoofRidge:
                snapPoint.acceptedTypes.Add(SnapType.RoofRidge);
                break;

            // Thêm các trường hợp khác khi cần
        }

        // Thiết lập ConnectionType phù hợp với PointType
        switch (snapPoint.pointType)
        {
            case SnapType.WallSide:
                snapPoint.connectionType = ConnectionType.Perpendicular;
                break;
            case SnapType.FloorEdge:
                snapPoint.connectionType = ConnectionType.Parallel;
                break;
            case SnapType.WallBottom:
            case SnapType.WallTop:
            case SnapType.FoundationTopEdge:
            case SnapType.RoofBottomEdge:
                snapPoint.connectionType = ConnectionType.Opposite;
                break;
            default:
                snapPoint.connectionType = ConnectionType.Any;
                break;
        }

        EditorUtility.SetDirty(snapPoint);
    }

    private void FindNearbySnapPoints()
    {
        nearbySnapPoints.Clear();
        compatibilityResults.Clear();

        // Tìm tất cả snap points trong scene
        SnapPoint[] allSnapPoints = FindObjectsByType<SnapPoint>(FindObjectsSortMode.None);
        
        foreach (var point in allSnapPoints)
        {
            if (point == snapPoint) continue;

            float distance = Vector3.Distance(point.transform.position, snapPoint.transform.position);
            
            // Chỉ lấy các điểm trong phạm vi 3 đơn vị
            if (distance <= 3f)
            {
                nearbySnapPoints.Add(point);
                
                // Kiểm tra khả năng kết nối hai chiều
                bool canSnapTo = snapPoint.CanSnapTo(point);
                bool canBeSnappedTo = point.CanSnapTo(snapPoint);
                
                compatibilityResults[point] = canSnapTo || canBeSnappedTo;
            }
        }
    }

    private void OnSceneGUI()
    {
        if (snapPoint == null) return;

        // Vẽ thông tin debug khi chọn snap point
        Vector3 position = snapPoint.transform.position;
        Vector3 direction = snapPoint.GetDirectionVector() * 0.5f;
        
        Handles.color = Color.yellow;
        Handles.DrawLine(position, position + direction);
        
        Handles.Label(position + direction, $"{snapPoint.pointType}\n{snapPoint.connectionType}");
        
        // Hiển thị kết nối đến snap khác nếu có được tìm thấy
        if (nearbySnapPoints.Count > 0)
        {
            foreach (var point in nearbySnapPoints)
            {
                if (point == null) continue;
                
                bool canConnect = compatibilityResults[point];
                Handles.color = canConnect ? Color.green : Color.red;
                Handles.DrawDottedLine(snapPoint.transform.position, point.transform.position, 3f);
            }
        }
    }
}
