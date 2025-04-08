using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(SnapPoint))]
public class SnapPointEditor : Editor
{
    private SnapPoint snapPoint;
    private bool showTestOptions = false;
    private bool showVisualization = false;
    private bool showHotkeys = true;
    private List<SnapPoint> nearbySnapPoints = new List<SnapPoint>();
    private Dictionary<SnapPoint, bool> compatibilityResults = new Dictionary<SnapPoint, bool>();
    
    // Mẫu màu cho các loại SnapPoint khác nhau
    private Dictionary<string, Color> categoryColors = new Dictionary<string, Color>() {
        {"Foundation", new Color(0.3f, 0.5f, 0.8f)},
        {"Wall", new Color(0.2f, 0.7f, 0.2f)},
        {"Floor", new Color(0.8f, 0.7f, 0.2f)},
        {"Roof", new Color(0.8f, 0.3f, 0.2f)},
        {"Door", new Color(0.5f, 0.3f, 0.8f)},
        {"Window", new Color(0.3f, 0.7f, 0.8f)},
        {"Pillar", new Color(0.5f, 0.5f, 0.5f)},
        {"Beam", new Color(0.6f, 0.4f, 0.2f)},
        {"Stair", new Color(0.7f, 0.3f, 0.5f)},
        {"Fence", new Color(0.4f, 0.6f, 0.3f)},
    };

    private void OnEnable()
    {
        snapPoint = (SnapPoint)target;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        
        // Trường cơ bản
        SerializedProperty pointType = serializedObject.FindProperty("pointType");
        SerializedProperty providesSupport = serializedObject.FindProperty("providesSupport");
        SerializedProperty snapDirection = serializedObject.FindProperty("snapDirection");
        SerializedProperty connectionType = serializedObject.FindProperty("connectionType");
        SerializedProperty autoAdjustConnection = serializedObject.FindProperty("autoAdjustConnection");
        SerializedProperty allowedConnectionTypes = serializedObject.FindProperty("allowedConnectionTypes");
        
        EditorGUILayout.PropertyField(pointType);
        
        // Hiển thị nhãn thông tin cho loại snap được chọn
        DisplaySnapTypeInfo(snapPoint.pointType);
        
        EditorGUILayout.PropertyField(snapDirection);
        
        // Phím tắt thiết lập loại kết nối
        showHotkeys = EditorGUILayout.Foldout(showHotkeys, "Phím tắt kết nối nhanh");
        if (showHotkeys)
        {
            DrawHotkeyButtons();
        }
        
        // Thêm phần điều khiển tự động điều chỉnh kết nối
        EditorGUILayout.PropertyField(autoAdjustConnection);
        
        if (snapPoint.autoAdjustConnection) {
            EditorGUILayout.HelpBox("Loại kết nối sẽ tự động được điều chỉnh dựa trên góc với điểm snap khác.", MessageType.Info);
            
            EditorGUILayout.PropertyField(allowedConnectionTypes, new GUIContent("Loại kết nối được phép"), true);
            
            if (snapPoint.allowedConnectionTypes.Count == 0) {
                EditorGUILayout.HelpBox("Không có loại kết nối nào được chọn. Tất cả các loại sẽ được sử dụng.", MessageType.Warning);
            }
        } else {
            EditorGUILayout.PropertyField(connectionType);
            // Hiển thị mô tả connection type
            DisplayConnectionTypeInfo(snapPoint.connectionType);
        }
        
        EditorGUILayout.PropertyField(providesSupport);
        
        // Các thuộc tính bổ sung được thu gọn trong các section
        EditorGUILayout.Space();
        
        // Hiển thị các loại accept trong một khu vực riêng
        EditorGUILayout.PropertyField(serializedObject.FindProperty("acceptedTypes"), true);
        
        EditorGUILayout.Space();
        SerializedProperty visualOffset = serializedObject.FindProperty("visualOffset");
        SerializedProperty visualRotationOffset = serializedObject.FindProperty("visualRotationOffset");
        SerializedProperty arrowSize = serializedObject.FindProperty("arrowSize");
        SerializedProperty showDirectionVectors = serializedObject.FindProperty("showDirectionVectors");
        
        showVisualization = EditorGUILayout.Foldout(showVisualization, "Tùy chỉnh trực quan");
        if (showVisualization)
        {
            EditorGUILayout.PropertyField(visualOffset);
            EditorGUILayout.PropertyField(visualRotationOffset);
            EditorGUILayout.PropertyField(arrowSize);
            EditorGUILayout.PropertyField(showDirectionVectors);
        }
        
        // Lưu lại các thay đổi
        if (EditorGUI.EndChangeCheck()) 
        {
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Công cụ hỗ trợ", EditorStyles.boldLabel);

        // Nút để điều chỉnh vị trí
        if (GUILayout.Button("Đặt mũi tên hướng ra ngoài"))
        {
            AdjustArrowDirection();
        }

        // Tùy chọn kiểm tra
        showTestOptions = EditorGUILayout.Foldout(showTestOptions, "Kiểm tra tương thích");
        if (showTestOptions)
        {
            if (GUILayout.Button("Tìm các snap point gần đó"))
            {
                FindNearbySnapPoints();
            }

            DrawCompatibilityResults();
        }
        
        // Xử lý phím tắt khi focus vào cửa sổ Inspector
        HandleHotkeys();
    }

    private void DrawHotkeyButtons()
    {
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.LabelField("Các phím tắt thiết lập kết nối:", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        // Nút thiết lập kết nối ngược hướng (Opposite)
        GUI.backgroundColor = snapPoint.connectionType == ConnectionType.Opposite ? 
            new Color(1f, 0.5f, 0, 1) : Color.white;
        if (GUILayout.Button(new GUIContent("1 - Ngược hướng", "Thiết lập kết nối ngược hướng (180°)")))
        {
            SetConnectionType(ConnectionType.Opposite);
        }
        
        // Nút thiết lập kết nối vuông góc (Perpendicular)
        GUI.backgroundColor = snapPoint.connectionType == ConnectionType.Perpendicular ? 
            new Color(0, 0.8f, 0.8f, 1) : Color.white;
        if (GUILayout.Button(new GUIContent("2 - Vuông góc", "Thiết lập kết nối vuông góc (90°)")))
        {
            SetConnectionType(ConnectionType.Perpendicular);
        }
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        // Nút thiết lập kết nối góc 45° (Angle45)
        GUI.backgroundColor = snapPoint.connectionType == ConnectionType.Angle45 ? 
            new Color(0.5f, 0.5f, 1f, 1) : Color.white;
        if (GUILayout.Button(new GUIContent("3 - Góc 45°", "Thiết lập kết nối góc 45°")))
        {
            SetConnectionType(ConnectionType.Angle45);
        }
        
        // Nút thiết lập kết nối song song (Parallel)
        GUI.backgroundColor = snapPoint.connectionType == ConnectionType.Parallel ? 
            new Color(1f, 0.8f, 0.2f, 1) : Color.white;
        if (GUILayout.Button(new GUIContent("4 - Song song", "Thiết lập kết nối song song (0°)")))
        {
            SetConnectionType(ConnectionType.Parallel);
        }
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        // Nút thiết lập kết nối tự do (Any)
        GUI.backgroundColor = snapPoint.connectionType == ConnectionType.Any ? 
            new Color(0.8f, 0.8f, 0.8f, 1) : Color.white;
        if (GUILayout.Button(new GUIContent("5 - Tự do", "Thiết lập kết nối tự do (mọi góc)")))
        {
            SetConnectionType(ConnectionType.Any);
        }
        
        // Nút thiết lập tự động điều chỉnh kết nối
        GUI.backgroundColor = snapPoint.autoAdjustConnection ? 
            new Color(0.5f, 0.8f, 0.5f, 1) : Color.white;
        if (GUILayout.Button(new GUIContent("0 - Tự động", "Bật/tắt tự động điều chỉnh kết nối")))
        {
            ToggleAutoAdjustConnection();
        }
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.HelpBox("Phím 1-5: Thiết lập loại kết nối cụ thể\nPhím 0: Bật/tắt tự động điều chỉnh kết nối", MessageType.Info);
        
        EditorGUILayout.EndVertical();
    }

    private void HandleHotkeys()
    {
        // Chỉ xử lý phím tắt khi cửa sổ Inspector đang focus
        if (Event.current != null && Event.current.type == EventType.KeyDown)
        {
            bool handled = false;
            
            // Phím tắt cho các loại kết nối
            switch (Event.current.keyCode)
            {
                case KeyCode.Alpha1:
                case KeyCode.Keypad1:
                    SetConnectionType(ConnectionType.Opposite);
                    handled = true;
                    break;
                    
                case KeyCode.Alpha2:
                case KeyCode.Keypad2:
                    SetConnectionType(ConnectionType.Perpendicular);
                    handled = true;
                    break;
                    
                case KeyCode.Alpha3:
                case KeyCode.Keypad3:
                    SetConnectionType(ConnectionType.Angle45);
                    handled = true;
                    break;
                    
                case KeyCode.Alpha4:
                case KeyCode.Keypad4:
                    SetConnectionType(ConnectionType.Parallel);
                    handled = true;
                    break;
                    
                case KeyCode.Alpha5:
                case KeyCode.Keypad5:
                    SetConnectionType(ConnectionType.Any);
                    handled = true;
                    break;
                    
                case KeyCode.Alpha0:
                case KeyCode.Keypad0:
                    ToggleAutoAdjustConnection();
                    handled = true;
                    break;
            }
            
            if (handled)
            {
                Event.current.Use();
                Repaint();
            }
        }
    }

    private void SetConnectionType(ConnectionType type)
    {
        // Thiết lập loại kết nối và tắt chế độ tự động
        snapPoint.connectionType = type;
        snapPoint.autoAdjustConnection = false;
        
        // Nếu đặt thành Angle45, tự động bật khóa góc xoay và đặt bước xoay 45 độ
        if (type == ConnectionType.Angle45)
        {
            snapPoint.lockRotation = true;
            snapPoint.rotationStep = 45f;
        }
        
        EditorUtility.SetDirty(snapPoint);
        Debug.Log($"Đã đặt loại kết nối: {type}");
        
        // Vẽ lại Inspector
        Repaint();
    }

    private void ToggleAutoAdjustConnection()
    {
        // Đảo trạng thái tự động điều chỉnh kết nối
        snapPoint.autoAdjustConnection = !snapPoint.autoAdjustConnection;
        
        if (snapPoint.autoAdjustConnection && snapPoint.allowedConnectionTypes.Count == 0)
        {
            // Nếu bật tự động và chưa có loại kết nối nào được chọn, thêm mặc định
            snapPoint.allowedConnectionTypes.Add(ConnectionType.Opposite);
            snapPoint.allowedConnectionTypes.Add(ConnectionType.Perpendicular);
            snapPoint.allowedConnectionTypes.Add(ConnectionType.Angle45);
        }
        
        EditorUtility.SetDirty(snapPoint);
        Debug.Log($"Tự động điều chỉnh kết nối: {(snapPoint.autoAdjustConnection ? "Bật" : "Tắt")}");
        
        // Vẽ lại Inspector
        Repaint();
    }

    private void DisplaySnapTypeInfo(SnapType type)
    {
        string info = GetSnapTypeDescription(type);
        EditorGUILayout.HelpBox(info, MessageType.Info);
    }
    
    private string GetSnapTypeDescription(SnapType type)
    {
        switch (type)
        {
            case SnapType.FoundationTopEdge: 
                return "Cạnh trên của móng: Kết nối với đáy tường, đáy cột hoặc cạnh sàn.";
            case SnapType.FoundationTopCorner: 
                return "Góc trên của móng: Kết nối đáy tường ở góc hoặc cột góc.";
            case SnapType.WallBottom: 
                return "Đáy tường: Đặt lên cạnh móng hoặc cạnh sàn.";
            case SnapType.WallTop: 
                return "Đỉnh tường: Đỡ cạnh mái, trần nhà hoặc cạnh sàn tầng trên.";
            case SnapType.WallSide: 
                return "Cạnh tường: Kết nối tường vuông góc, cửa, cửa sổ hoặc cạnh sàn.";
            case SnapType.FloorEdge: 
                return "Cạnh sàn: Đa năng, kết nối với đỉnh tường, cạnh tường, hoặc cạnh sàn khác.";
            // Thêm các mô tả cho các loại khác
            default: 
                return "Loại snap: " + type.ToString();
        }
    }
    
    private void DisplayConnectionTypeInfo(ConnectionType type)
    {
        string info = "";
        switch (type)
        {
            case ConnectionType.Opposite:
                info = "Kết nối ngược hướng: Hai snap point nên hướng ngược chiều nhau (180°)";
                break;
            case ConnectionType.Perpendicular:
                info = "Kết nối vuông góc: Hai snap point nên vuông góc với nhau (90°)";
                break;
            case ConnectionType.Parallel:
                info = "Kết nối song song: Hai snap point nên cùng hướng (0°)";
                break;
            case ConnectionType.Any:
                info = "Kết nối tự do: Không kiểm tra hướng khi kết nối";
                break;
            case ConnectionType.Angle45:
                info = "Kết nối góc 45°: Hai snap point nên tạo góc xiên 45° với nhau";
                break;
        }
        
        EditorGUILayout.HelpBox(info, MessageType.Info);
    }

    private void DrawCompatibilityResults()
    {
        if (nearbySnapPoints.Count > 0)
        {
            EditorGUILayout.LabelField("Snap Points trong phạm vi 3 đơn vị:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // Header cho bảng kết quả
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tên/Loại", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("Tương thích", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("Khoảng cách", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("Hướng", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            
            foreach (var point in nearbySnapPoints)
            {
                if (point == null) continue;

                bool canSnap = compatibilityResults[point];
                
                // Tính khoảng cách và góc giữa hai snap point
                float distance = Vector3.Distance(point.transform.position, snapPoint.transform.position);
                float angle = Vector3.Angle(snapPoint.GetDirectionVector(), point.GetDirectionVector());
                
                EditorGUILayout.BeginHorizontal();
                
                // Màu nền dựa theo loại snap point
                string category = point.pointType.ToString();
                Color bgColor = Color.white;
                foreach (var key in categoryColors.Keys)
                {
                    if (category.Contains(key))
                    {
                        bgColor = categoryColors[key];
                        break;
                    }
                }
                
                GUI.backgroundColor = bgColor;
                
                // Thông tin snap point
                EditorGUILayout.BeginVertical("box", GUILayout.Width(150));
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField(point.name);
                EditorGUILayout.LabelField(point.pointType.ToString(), EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                
                // Trạng thái tương thích
                GUI.color = canSnap ? Color.green : Color.red;
                EditorGUILayout.LabelField(canSnap ? "✓ Tương thích" : "✗ Không tương thích", GUILayout.Width(100));
                GUI.color = Color.white;
                
                // Thông tin khoảng cách và góc
                EditorGUILayout.LabelField(distance.ToString("F2") + "m", GUILayout.Width(80));
                
                // Hiển thị góc với màu tương ứng với loại connection
                if (angle < 30) GUI.color = new Color(1f, 0.8f, 0.2f); // Gần song song
                else if (angle > 150) GUI.color = new Color(1f, 0.5f, 0); // Gần ngược chiều
                else if (angle > 60 && angle < 120) GUI.color = new Color(0, 0.8f, 0.8f); // Gần vuông góc
                else GUI.color = Color.white;
                
                EditorGUILayout.LabelField(angle.ToString("F0") + "°", GUILayout.Width(100));
                GUI.color = Color.white;
                
                // Nút để chọn snap point này trong Scene
                if (GUILayout.Button("Chọn", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = point.gameObject;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
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

    private SnapPoint.SnapDirection GetHorizontalDirection()
    {
        // Tính vector từ tâm đối tượng cha (nếu có) đến snap point
        Vector3 direction = Vector3.right; // Mặc định

        if (snapPoint.transform.parent != null)
        {
            direction = snapPoint.transform.position - snapPoint.transform.parent.position;
            direction.y = 0; // Chỉ quan tâm đến hướng ngang
            direction.Normalize();
        }

        // Tìm hướng ngang phù hợp nhất
        float dotForward = Vector3.Dot(direction, snapPoint.transform.forward);
        float dotRight = Vector3.Dot(direction, snapPoint.transform.right);

        if (Mathf.Abs(dotForward) > Mathf.Abs(dotRight))
        {
            return dotForward > 0 ? SnapPoint.SnapDirection.Forward : SnapPoint.SnapDirection.Back;
        }
        else
        {
            return dotRight > 0 ? SnapPoint.SnapDirection.Right : SnapPoint.SnapDirection.Left;
        }
    }

    private void DrawAngleGuides(Vector3 position, float radius)
    {
        Handles.color = new Color(0.8f, 0.8f, 0.8f, 0.3f);
        
        // Vẽ vòng tròn hướng dẫn
        Handles.DrawWireDisc(position, Vector3.up, radius);
        
        // Vẽ các đường theo các góc 45°
        for (int angle = 0; angle < 360; angle += 45)
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
            Handles.DrawLine(position, position + direction * radius);
            
            // Hiển thị giá trị góc
            Handles.Label(position + direction * (radius + 0.1f), angle + "°");
        }
    }

    private void OnSceneGUI()
    {
        if (snapPoint == null) return;

        // Vẽ thông tin debug khi chọn snap point
        Vector3 position = snapPoint.transform.position;
        Vector3 direction = snapPoint.GetDirectionVector() * 0.5f;
        
        // Xác định màu dựa trên loại snap
        string category = snapPoint.pointType.ToString();
        Color snapColor = Color.yellow;
        foreach (var key in categoryColors.Keys)
        {
            if (category.Contains(key))
            {
                snapColor = categoryColors[key];
                break;
            }
        }
        
        Handles.color = snapColor;
        Handles.DrawLine(position, position + direction);
        
        // Thông tin chi tiết hơn
        string info = $"{snapPoint.pointType}\n{snapPoint.connectionType}\n{snapPoint.snapDirection}";
        Handles.Label(position + direction, info);
        
        // Vẽ mũi tên chỉ hướng lớn hơn và rõ ràng hơn
        Handles.color = snapColor;
        Handles.ArrowHandleCap(
            0,
            position,
            Quaternion.LookRotation(direction.normalized),
            direction.magnitude,
            EventType.Repaint
        );
        
        // Hiển thị hướng dẫn góc khi ConnectionType là Angle45 hoặc Any
        if (snapPoint.connectionType == ConnectionType.Angle45 || 
            snapPoint.connectionType == ConnectionType.Any)
        {
            DrawAngleGuides(position, 0.75f);
        }
        
        // Nếu đã bật tự động điều chỉnh kết nối, hiển thị các hướng kết nối có thể
        if (snapPoint.autoAdjustConnection) {
            DrawAllConnectionOptions(position, 0.8f);
        }
        
        // Hiển thị kết nối đến snap khác nếu có được tìm thấy
        if (nearbySnapPoints.Count > 0)
        {
            foreach (var point in nearbySnapPoints)
            {
                if (point == null) continue;
                
                bool canConnect = compatibilityResults[point];
                Handles.color = canConnect ? Color.green : Color.red;
                
                // Vẽ đường kết nối với độ dày khác nhau dựa vào khả năng kết nối
                Handles.DrawDottedLine(snapPoint.transform.position, point.transform.position, canConnect ? 5f : 2f);
                
                // Hiển thị thông tin khoảng cách
                if (canConnect)
                {
                    Vector3 midPoint = (snapPoint.transform.position + point.transform.position) / 2;
                    float distance = Vector3.Distance(snapPoint.transform.position, point.transform.position);
                    Handles.Label(midPoint, distance.ToString("F2") + "m");
                    
                    // Hiển thị loại kết nối sẽ được sử dụng nếu nối với điểm này
                    if (snapPoint.autoAdjustConnection) {
                        ConnectionType optimalType = snapPoint.DetermineOptimalConnectionType(point);
                        string connectionInfo = $"{optimalType}";
                        Handles.Label(midPoint + Vector3.up * 0.2f, connectionInfo);
                    }
                }
            }
        }
    }

    // Vẽ tất cả các tùy chọn kết nối để người dùng nhìn thấy
    private void DrawAllConnectionOptions(Vector3 position, float radius)
    {
        // Vẽ các kiểu kết nối được phép
        List<ConnectionType> types = snapPoint.allowedConnectionTypes.Count > 0 
            ? snapPoint.allowedConnectionTypes 
            : new List<ConnectionType>(System.Enum.GetValues(typeof(ConnectionType)) as ConnectionType[]);
        
        foreach (var type in types) {
            Color connectionColor;
            
            switch (type) {
                case ConnectionType.Opposite:
                    connectionColor = new Color(1f, 0.5f, 0, 0.5f); // Orange
                    DrawDirectionGuide(position, radius, 180, connectionColor, "Opposite");
                    break;
                case ConnectionType.Perpendicular:
                    connectionColor = new Color(0, 0.8f, 0.8f, 0.5f); // Cyan
                    DrawDirectionGuide(position, radius, 90, connectionColor, "Perpendicular");
                    DrawDirectionGuide(position, radius, 270, connectionColor, "Perpendicular");
                    break;
                case ConnectionType.Angle45:
                    connectionColor = new Color(0.5f, 0.5f, 1f, 0.5f); // Blue
                    DrawDirectionGuide(position, radius, 45, connectionColor, "45°");
                    DrawDirectionGuide(position, radius, 135, connectionColor, "45°");
                    DrawDirectionGuide(position, radius, 225, connectionColor, "45°");
                    DrawDirectionGuide(position, radius, 315, connectionColor, "45°");
                    break;
                case ConnectionType.Parallel:
                    connectionColor = new Color(1f, 0.8f, 0.2f, 0.5f); // Yellow
                    DrawDirectionGuide(position, radius, 0, connectionColor, "Parallel");
                    break;
            }
        }
    }

    // Vẽ hướng dẫn cho một góc cụ thể
    private void DrawDirectionGuide(Vector3 position, float radius, float angle, Color color, string label)
    {
        Handles.color = color;
        float rad = angle * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
        
        // Vẽ đường dẫn hướng
        Handles.DrawLine(position, position + direction * radius);
        
        // Hiển thị nhãn
        Handles.Label(position + direction * (radius + 0.1f), label);
    }
}
