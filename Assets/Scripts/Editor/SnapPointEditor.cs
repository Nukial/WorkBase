using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(SnapPoint))]
public class SnapPointEditor : Editor
{
    private SnapPoint snapPoint;
    private bool showTestOptions = false;
    private bool showPresets = true;
    private bool showVisualization = false;
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
        
        EditorGUILayout.PropertyField(pointType);
        
        // Hiển thị nhãn thông tin cho loại snap được chọn
        DisplaySnapTypeInfo(snapPoint.pointType);
        
        EditorGUILayout.PropertyField(snapDirection);
        EditorGUILayout.PropertyField(connectionType);
        
        // Hiển thị mô tả connection type
        DisplayConnectionTypeInfo(snapPoint.connectionType);
        
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

        // Section mới: Preset cho từng loại
        showPresets = EditorGUILayout.Foldout(showPresets, "Preset Nhanh Theo Loại");
        if (showPresets)
        {
            DisplayPresetButtons();
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

    private void DisplayPresetButtons()
    {
        // Preset theo loại chính
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Presets Phổ Biến", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button(new GUIContent("Nền Móng", "Cạnh trên của móng, kết nối với tường"), GUILayout.Height(30)))
        {
            SetFoundationPreset();
        }
        
        if (GUILayout.Button(new GUIContent("Đáy Tường", "Đáy tường, kết nối với nền móng"), GUILayout.Height(30)))
        {
            SetWallBottomPreset();
        }
        
        if (GUILayout.Button(new GUIContent("Đỉnh Tường", "Đỉnh tường, kết nối với mái hoặc sàn"), GUILayout.Height(30)))
        {
            SetWallTopPreset();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button(new GUIContent("Cạnh Tường", "Cạnh bên của tường, kết nối vuông góc"), GUILayout.Height(30)))
        {
            SetWallSidePreset();
        }
        
        if (GUILayout.Button(new GUIContent("Cạnh Sàn", "Cạnh sàn, có thể kết nối đa dạng"), GUILayout.Height(30)))
        {
            SetFloorEdgePreset();
        }
        
        if (GUILayout.Button(new GUIContent("Đáy Mái", "Cạnh dưới mái, kết nối với đỉnh tường"), GUILayout.Height(30)))
        {
            SetRoofBottomPreset();
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Thêm hàng mới cho cầu thang
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button(new GUIContent("Chân Cầu Thang", "Điểm bắt đầu cầu thang, nối với sàn tầng dưới"), GUILayout.Height(30)))
        {
            SetStairBottomPreset();
        }
        
        if (GUILayout.Button(new GUIContent("Đỉnh Cầu Thang", "Điểm kết thúc cầu thang, nối với sàn tầng trên"), GUILayout.Height(30)))
        {
            SetStairTopPreset();
        }
        
        if (GUILayout.Button(new GUIContent("Cạnh Cầu Thang", "Cạnh bên cầu thang, nối với lan can"), GUILayout.Height(30)))
        {
            SetStairSidePreset();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        // Presets cho kết nối
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Presets Kết Nối", EditorStyles.boldLabel);
        
        // Nút thiết lập cho Sàn - Tường
        if (GUILayout.Button(new GUIContent("Thiết lập Sàn-Tường", "Cấu hình tự động cho kết nối sàn và tường"), GUILayout.Height(30)))
        {
            SetFloorWallConnectionPreset();
        }
        
        // Nút thiết lập cho Góc Tường
        if (GUILayout.Button(new GUIContent("Thiết lập Góc Tường", "Cấu hình tự động cho hai tường vuông góc"), GUILayout.Height(30)))
        {
            SetWallCornerPreset();
        }
        
        // Nút thiết lập cho Mái - Tường
        if (GUILayout.Button(new GUIContent("Thiết lập Mái-Tường", "Cấu hình tự động cho kết nối mái với tường"), GUILayout.Height(30)))
        {
            SetRoofWallPreset();
        }
        
        // Thêm nút thiết lập cho Cầu thang - Sàn
        if (GUILayout.Button(new GUIContent("Thiết lập Cầu thang-Sàn", "Cấu hình tự động cho kết nối cầu thang với sàn"), GUILayout.Height(30)))
        {
            SetStairFloorConnectionPreset();
        }
        
        // Thêm nút thiết lập cho tường nối tiếp và tường góc
        if (GUILayout.Button(new GUIContent("Thiết lập Tường Nối Tiếp", "Cấu hình tự động cho tường nối tiếp thẳng hàng"), GUILayout.Height(30)))
        {
            SetWallLineConnectionPreset();
        }
        
        if (GUILayout.Button(new GUIContent("Thiết lập Tường Góc Vuông", "Cấu hình tự động cho tường góc vuông với nhau"), GUILayout.Height(30)))
        {
            SetWallCornerConnectionPreset();
        }
        
        // Thêm nút thiết lập kết hợp cho tường
        if (GUILayout.Button(new GUIContent("Tường Đa Kết Nối", "Cho phép tường kết nối cả thẳng hàng và góc vuông"), GUILayout.Height(30)))
        {
            SetWallFlexibleConnectionPreset();
        }
        
        // Thêm nút thiết lập cho kết nối góc 45 độ
        if (GUILayout.Button(new GUIContent("Tường Góc 45°", "Cấu hình tự động cho tường kết nối góc 45 độ"), GUILayout.Height(30)))
        {
            SetWall45DegreeConnectionPreset();
        }
        
        EditorGUILayout.EndVertical();
    }

    private void SetFoundationPreset()
    {
        snapPoint.pointType = SnapType.FoundationTopEdge;
        snapPoint.snapDirection = SnapPoint.SnapDirection.Up;
        snapPoint.connectionType = ConnectionType.Opposite;
        snapPoint.providesSupport = true;
        
        snapPoint.acceptedTypes.Clear();
        snapPoint.acceptedTypes.Add(SnapType.WallBottom);
        snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
        snapPoint.acceptedTypes.Add(SnapType.PillarBottom);
        
        EditorUtility.SetDirty(snapPoint);
    }
    
    private void SetWallBottomPreset()
    {
        snapPoint.pointType = SnapType.WallBottom;
        snapPoint.snapDirection = SnapPoint.SnapDirection.Down;
        snapPoint.connectionType = ConnectionType.Opposite;
        snapPoint.providesSupport = false;
        
        snapPoint.acceptedTypes.Clear();
        snapPoint.acceptedTypes.Add(SnapType.FoundationTopEdge);
        snapPoint.acceptedTypes.Add(SnapType.FoundationTopCorner);
        snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
        snapPoint.acceptedTypes.Add(SnapType.WallTop);
        
        EditorUtility.SetDirty(snapPoint);
    }
    
    private void SetWallTopPreset()
    {
        snapPoint.pointType = SnapType.WallTop;
        snapPoint.snapDirection = SnapPoint.SnapDirection.Up;
        snapPoint.connectionType = ConnectionType.Opposite;
        snapPoint.providesSupport = true;
        
        snapPoint.acceptedTypes.Clear();
        snapPoint.acceptedTypes.Add(SnapType.RoofBottomEdge);
        snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
        snapPoint.acceptedTypes.Add(SnapType.Ceiling);
        snapPoint.acceptedTypes.Add(SnapType.WallBottom);
        
        EditorUtility.SetDirty(snapPoint);
    }
    
    private void SetWallSidePreset()
    {
        snapPoint.pointType = SnapType.WallSide;
        // Tự động xác định hướng ngang dựa vào vị trí
        snapPoint.snapDirection = GetHorizontalDirection();
        snapPoint.connectionType = ConnectionType.Perpendicular;
        snapPoint.providesSupport = true;
        
        snapPoint.acceptedTypes.Clear();
        snapPoint.acceptedTypes.Add(SnapType.WallSide);
        snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
        snapPoint.acceptedTypes.Add(SnapType.DoorFrameSide);
        snapPoint.acceptedTypes.Add(SnapType.WindowFrameSide);
        
        EditorUtility.SetDirty(snapPoint);
    }
    
    private void SetFloorEdgePreset()
    {
        snapPoint.pointType = SnapType.FloorEdge;
        // Xác định hướng ngang hoặc dọc dựa vào vị trí
        bool isHorizontalEdge = true; // Mặc định là cạnh ngang
        
        // Kiểm tra có phải cạnh dưới sàn không
        if (isHorizontalEdge)
        {
            snapPoint.snapDirection = SnapPoint.SnapDirection.Down;
            snapPoint.connectionType = ConnectionType.Opposite;
        }
        else
        {
            snapPoint.snapDirection = GetHorizontalDirection();
            snapPoint.connectionType = ConnectionType.Perpendicular;
        }
        
        snapPoint.providesSupport = true;
        
        snapPoint.acceptedTypes.Clear();
        snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
        snapPoint.acceptedTypes.Add(SnapType.WallTop);
        snapPoint.acceptedTypes.Add(SnapType.WallBottom);
        snapPoint.acceptedTypes.Add(SnapType.WallSide);
        
        EditorUtility.SetDirty(snapPoint);
    }
    
    private void SetRoofBottomPreset()
    {
        snapPoint.pointType = SnapType.RoofBottomEdge;
        snapPoint.snapDirection = SnapPoint.SnapDirection.Down;
        snapPoint.connectionType = ConnectionType.Opposite;
        snapPoint.providesSupport = false;
        
        snapPoint.acceptedTypes.Clear();
        snapPoint.acceptedTypes.Add(SnapType.WallTop);
        
        EditorUtility.SetDirty(snapPoint);
    }

    private void SetStairBottomPreset()
    {
        snapPoint.pointType = SnapType.StairBottom;
        snapPoint.snapDirection = SnapPoint.SnapDirection.Down;
        snapPoint.connectionType = ConnectionType.Opposite;
        snapPoint.providesSupport = false;
        
        snapPoint.acceptedTypes.Clear();
        snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
        snapPoint.acceptedTypes.Add(SnapType.FoundationTopEdge);
        
        EditorUtility.SetDirty(snapPoint);
    }

    private void SetStairTopPreset()
    {
        snapPoint.pointType = SnapType.StairTop;
        snapPoint.snapDirection = SnapPoint.SnapDirection.Up;
        snapPoint.connectionType = ConnectionType.Opposite;
        snapPoint.providesSupport = false;
        
        snapPoint.acceptedTypes.Clear();
        snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
        
        EditorUtility.SetDirty(snapPoint);
    }

    private void SetStairSidePreset()
    {
        snapPoint.pointType = SnapType.StairSide;
        snapPoint.snapDirection = GetHorizontalDirection();
        snapPoint.connectionType = ConnectionType.Perpendicular;
        snapPoint.providesSupport = true;
        
        snapPoint.acceptedTypes.Clear();
        snapPoint.acceptedTypes.Add(SnapType.FencePostBottom);
        snapPoint.acceptedTypes.Add(SnapType.WallSide);
        
        EditorUtility.SetDirty(snapPoint);
    }

    private void SetStairFloorConnectionPreset()
    {
        string[] options = new string[] {
            "Chân cầu thang nối sàn tầng dưới",
            "Đỉnh cầu thang nối sàn tầng trên",
            "Cạnh cầu thang nối lan can",
            "Sàn nối với chân cầu thang",
            "Sàn nối với đỉnh cầu thang"
        };

        int choice = EditorUtility.DisplayDialogComplex(
            "Chọn kiểu kết nối Cầu thang-Sàn",
            "Chọn cách cầu thang và sàn kết nối với nhau:",
            options[0], options[1], options[2]);

        switch (choice)
        {
            case 0: // Chân cầu thang nối sàn tầng dưới
                snapPoint.pointType = SnapType.StairBottom;
                snapPoint.snapDirection = SnapPoint.SnapDirection.Down;
                snapPoint.connectionType = ConnectionType.Opposite;
                snapPoint.providesSupport = false;
                snapPoint.acceptedTypes.Clear();
                snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
                snapPoint.acceptedTypes.Add(SnapType.FoundationTopEdge);
                break;

            case 1: // Đỉnh cầu thang nối sàn tầng trên
                snapPoint.pointType = SnapType.StairTop;
                snapPoint.snapDirection = SnapPoint.SnapDirection.Up;
                snapPoint.connectionType = ConnectionType.Opposite;
                snapPoint.providesSupport = false;
                snapPoint.acceptedTypes.Clear();
                snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
                break;

            case 2: // Cạnh cầu thang nối lan can
                snapPoint.pointType = SnapType.StairSide;
                snapPoint.snapDirection = GetHorizontalDirection();
                snapPoint.connectionType = ConnectionType.Perpendicular;
                snapPoint.providesSupport = true;
                snapPoint.acceptedTypes.Clear();
                snapPoint.acceptedTypes.Add(SnapType.FencePostBottom);
                snapPoint.acceptedTypes.Add(SnapType.WallSide);
                break;

            case 3: // Sàn nối với chân cầu thang
                snapPoint.pointType = SnapType.FloorEdge;
                snapPoint.snapDirection = SnapPoint.SnapDirection.Up;
                snapPoint.connectionType = ConnectionType.Opposite;
                snapPoint.providesSupport = true;
                snapPoint.acceptedTypes.Clear();
                snapPoint.acceptedTypes.Add(SnapType.StairBottom);
                break;

            case 4: // Sàn nối với đỉnh cầu thang
                snapPoint.pointType = SnapType.FloorEdge;
                snapPoint.snapDirection = SnapPoint.SnapDirection.Down;
                snapPoint.connectionType = ConnectionType.Opposite;
                snapPoint.providesSupport = true;
                snapPoint.acceptedTypes.Clear();
                snapPoint.acceptedTypes.Add(SnapType.StairTop);
                break;
        }

        EditorUtility.SetDirty(snapPoint);
        Debug.Log($"Thiết lập kết nối Cầu thang-Sàn: {options[choice]}");
    }

    // Phương thức thiết lập cho kết nối góc tường
    private void SetWallCornerPreset()
    {
        string[] options = new string[] {
            "Cạnh bên trái tường",
            "Cạnh bên phải tường",
            "Cạnh trước tường",
            "Cạnh sau tường"
        };

        int choice = EditorUtility.DisplayDialogComplex(
            "Chọn vị trí góc tường",
            "Chọn vị trí cạnh của tường này:",
            options[0], options[1], options[2]);

        snapPoint.pointType = SnapType.WallSide;
        snapPoint.connectionType = ConnectionType.Perpendicular;
        snapPoint.providesSupport = true;
        
        // Thiết lập hướng dựa vào lựa chọn
        switch (choice)
        {
            case 0: // Cạnh bên trái
                snapPoint.snapDirection = SnapPoint.SnapDirection.Left;
                break;
            case 1: // Cạnh bên phải
                snapPoint.snapDirection = SnapPoint.SnapDirection.Right;
                break;
            case 2: // Cạnh trước
                snapPoint.snapDirection = SnapPoint.SnapDirection.Forward;
                break;
            case 3: // Cạnh sau
                snapPoint.snapDirection = SnapPoint.SnapDirection.Back;
                break;
        }
        
        snapPoint.acceptedTypes.Clear();
        snapPoint.acceptedTypes.Add(SnapType.WallSide);
        
        EditorUtility.SetDirty(snapPoint);
    }

    // Phương thức thiết lập cho kết nối mái với tường
    private void SetRoofWallPreset()
    {
        string[] options = new string[] {
            "Đáy mái (gắn với đỉnh tường)",
            "Đỉnh mái (ridge)",
            "Mép hông mái (gable edge)"
        };

        int choice = EditorUtility.DisplayDialogComplex(
            "Chọn vị trí trên mái",
            "Chọn loại điểm snap trên mái:",
            options[0], options[1], options[2]);

        switch (choice)
        {
            case 0: // Đáy mái
                snapPoint.pointType = SnapType.RoofBottomEdge;
                snapPoint.snapDirection = SnapPoint.SnapDirection.Down;
                snapPoint.connectionType = ConnectionType.Opposite;
                snapPoint.acceptedTypes.Clear();
                snapPoint.acceptedTypes.Add(SnapType.WallTop);
                break;
                
            case 1: // Đỉnh mái
                snapPoint.pointType = SnapType.RoofRidge;
                snapPoint.snapDirection = SnapPoint.SnapDirection.Up;
                snapPoint.connectionType = ConnectionType.Opposite;
                snapPoint.acceptedTypes.Clear();
                snapPoint.acceptedTypes.Add(SnapType.RoofRidge);
                break;
                
            case 2: // Mép hông mái
                snapPoint.pointType = SnapType.RoofGableEdge;
                snapPoint.snapDirection = GetHorizontalDirection();
                snapPoint.connectionType = ConnectionType.Perpendicular;
                snapPoint.acceptedTypes.Clear();
                snapPoint.acceptedTypes.Add(SnapType.WallTop);
                snapPoint.acceptedTypes.Add(SnapType.RoofGableEdge);
                break;
        }
        
        EditorUtility.SetDirty(snapPoint);
    }

    // Phương thức thiết lập cho kết nối sàn-tường
    private void SetFloorWallConnectionPreset()
    {
        string[] options = new string[] {
            "Sàn đặt trên đỉnh tường (Floor on WallTop)",
            "Sàn gắn vào cạnh tường (Floor to WallSide)",
            "Tường đặt trên sàn (Wall on FloorEdge)",
            "Tường gắn vào cạnh sàn (Wall to FloorEdge)"
        };

        int choice = EditorUtility.DisplayDialogComplex(
            "Chọn kiểu kết nối Sàn-Tường",
            "Chọn cách sàn và tường kết nối với nhau:",
            options[0], options[1], options[2]);

        switch (choice)
        {
            case 0: // Sàn đặt trên đỉnh tường
                if (snapPoint.gameObject.name.ToLower().Contains("floor"))
                {
                    // Đây là snap point trên sàn
                    snapPoint.pointType = SnapType.FloorEdge;
                    snapPoint.snapDirection = SnapPoint.SnapDirection.Down;
                    snapPoint.connectionType = ConnectionType.Opposite;
                    snapPoint.acceptedTypes.Clear();
                    snapPoint.acceptedTypes.Add(SnapType.WallTop);
                }
                else
                {
                    // Đây là snap point trên tường
                    snapPoint.pointType = SnapType.WallTop;
                    snapPoint.snapDirection = SnapPoint.SnapDirection.Up;
                    snapPoint.connectionType = ConnectionType.Opposite;
                    snapPoint.acceptedTypes.Clear();
                    snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
                }
                break;

            case 1: // Sàn gắn vào cạnh tường
                if (snapPoint.gameObject.name.ToLower().Contains("floor"))
                {
                    // Đây là snap point trên sàn
                    snapPoint.pointType = SnapType.FloorEdge;
                    snapPoint.snapDirection = GetHorizontalDirection();
                    snapPoint.connectionType = ConnectionType.Perpendicular;
                    snapPoint.acceptedTypes.Clear();
                    snapPoint.acceptedTypes.Add(SnapType.WallSide);
                }
                else
                {
                    // Đây là snap point trên tường
                    snapPoint.pointType = SnapType.WallSide;
                    snapPoint.snapDirection = GetHorizontalDirection();
                    snapPoint.connectionType = ConnectionType.Perpendicular;
                    snapPoint.acceptedTypes.Clear();
                    snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
                }
                break;

            case 2: // Tường đặt trên sàn
                if (snapPoint.gameObject.name.ToLower().Contains("wall"))
                {
                    // Đây là snap point trên tường
                    snapPoint.pointType = SnapType.WallBottom;
                    snapPoint.snapDirection = SnapPoint.SnapDirection.Down;
                    snapPoint.connectionType = ConnectionType.Opposite;
                    snapPoint.acceptedTypes.Clear();
                    snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
                }
                else
                {
                    // Đây là snap point trên sàn
                    snapPoint.pointType = SnapType.FloorEdge;
                    snapPoint.snapDirection = SnapPoint.SnapDirection.Up;
                    snapPoint.connectionType = ConnectionType.Opposite;
                    snapPoint.acceptedTypes.Clear();
                    snapPoint.acceptedTypes.Add(SnapType.WallBottom);
                }
                break;

            case 3: // Tường gắn vào cạnh sàn
                if (snapPoint.gameObject.name.ToLower().Contains("wall"))
                {
                    // Đây là snap point trên tường
                    snapPoint.pointType = SnapType.WallSide;
                    snapPoint.snapDirection = GetHorizontalDirection();
                    snapPoint.connectionType = ConnectionType.Perpendicular;
                    snapPoint.acceptedTypes.Clear();
                    snapPoint.acceptedTypes.Add(SnapType.FloorEdge);
                }
                else
                {
                    // Đây là snap point trên sàn
                    snapPoint.pointType = SnapType.FloorEdge;
                    snapPoint.snapDirection = GetHorizontalDirection();
                    snapPoint.connectionType = ConnectionType.Perpendicular;
                    snapPoint.acceptedTypes.Clear();
                    snapPoint.acceptedTypes.Add(SnapType.WallSide);
                }
                break;
        }

        EditorUtility.SetDirty(snapPoint);
        Debug.Log($"Thiết lập kết nối Sàn-Tường: {options[choice]}");
    }

    private void SetWallLineConnectionPreset()
    {
        snapPoint.pointType = SnapType.WallSide;
        snapPoint.snapDirection = GetHorizontalDirection();
        snapPoint.connectionType = ConnectionType.Opposite;
        snapPoint.providesSupport = true;
        
        snapPoint.acceptedTypes.Clear();
        snapPoint.acceptedTypes.Add(SnapType.WallSide);
        
        EditorUtility.SetDirty(snapPoint);
        Debug.Log("Thiết lập tường nối tiếp: Sử dụng hướng ngược nhau (Opposite) để kết nối các tường thẳng hàng");
    }

    private void SetWallCornerConnectionPreset()
    {
        snapPoint.pointType = SnapType.WallSide;
        snapPoint.snapDirection = GetHorizontalDirection();
        snapPoint.connectionType = ConnectionType.Perpendicular;
        snapPoint.providesSupport = true;
        
        snapPoint.acceptedTypes.Clear();
        snapPoint.acceptedTypes.Add(SnapType.WallSide);
        
        EditorUtility.SetDirty(snapPoint);
        Debug.Log("Thiết lập tường góc: Sử dụng hướng vuông góc (Perpendicular) để kết nối các tường theo góc 90 độ");
    }

    private void SetWallFlexibleConnectionPreset()
    {
        snapPoint.pointType = SnapType.WallSide;
        snapPoint.snapDirection = GetHorizontalDirection();
        snapPoint.connectionType = ConnectionType.Any;  // Sử dụng Any để chấp nhận nhiều loại kết nối
        snapPoint.providesSupport = true;
        
        snapPoint.acceptedTypes.Clear();
        snapPoint.acceptedTypes.Add(SnapType.WallSide);
        
        EditorUtility.SetDirty(snapPoint);
        Debug.Log("Thiết lập tường đa kết nối: Có thể kết nối cả thẳng hàng và góc vuông");
    }

    private void SetWall45DegreeConnectionPreset()
    {
        snapPoint.pointType = SnapType.WallSide;
        snapPoint.snapDirection = GetHorizontalDirection();
        snapPoint.connectionType = ConnectionType.Angle45;
        snapPoint.providesSupport = true;
        snapPoint.lockRotation = true;
        snapPoint.rotationStep = 45f;
        
        // Hiển thị hộp thoại để cho phép người dùng chọn mức độ chính xác
        string[] options = new string[] {
            "Góc 45° (chính xác)",
            "Góc 45° (dung sai ±15°)",
            "Tất cả các góc bội số của 45°"
        };
        
        int choice = EditorUtility.DisplayDialogComplex(
            "Tùy chọn góc 45°",
            "Chọn mức độ chính xác cho kết nối góc 45°:",
            options[0], options[1], options[2]);
        
        // Thiết lập độ chính xác cho góc 45°
        switch (choice) {
            case 0: // Chính xác 45°
                snapPoint.rotationStep = 45f;
                break;
            case 1: // Dung sai ±15°
                snapPoint.rotationStep = 15f;
                break;
            case 2: // Tất cả các góc bội số của 45°
                snapPoint.rotationStep = 45f;
                // Thêm tất cả các góc 45°, 90°, 135°, 180° vào accepted types
                break;
        }
        
        snapPoint.acceptedTypes.Clear();
        snapPoint.acceptedTypes.Add(SnapType.WallSide);
        
        EditorUtility.SetDirty(snapPoint);
        Debug.Log("Thiết lập tường góc 45°: Tạo góc xiên 45 độ giữa các tường, xoay theo bước " + snapPoint.rotationStep + "°");
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
                }
            }
        }
    }
}
