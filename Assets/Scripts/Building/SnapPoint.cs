using System.Collections.Generic;
using UnityEngine;

public enum SnapType {
    // --- Cơ bản ---
    None,               // Không xác định hoặc không snap
    Any,                // Chấp nhận mọi loại (dùng cẩn thận)

    // --- Nền Móng (Foundation) ---
    FoundationTopEdge,  // Cạnh trên của móng (cho tường, sàn)
    FoundationTopCorner,// Góc trên của móng
    FoundationSide,     // Cạnh bên (nối các móng với nhau)
    FoundationBottom,   // Đáy (nếu cho phép chồng móng)

    // --- Tường (Wall) ---
    WallBottom,         // Đáy tường (nối vào FoundationTop, FloorEdge)
    WallTop,            // Đỉnh tường (nối vào WallBottom, RoofBottom, Ceiling, FloorEdge)
    WallSide,           // Cạnh tường (nối các tường với nhau)

    // --- Sàn / Trần (Floor / Ceiling) ---
    FloorEdge,          // Cạnh sàn/trần
    FloorCorner,        // Góc sàn/trần
    Ceiling,            // Bề mặt trần (có thể trùng với FloorEdge/Corner)

    // --- Mái Nhà (Roof) ---
    RoofBottomEdge,     // Cạnh dưới mái (đặt lên WallTop)
    RoofRidge,          // Đỉnh mái (nối các mái dốc)
    RoofGableEdge,      // Cạnh mái dốc ở đầu hồi
    RoofValley,         // Góc lõm giữa 2 mái dốc
    RoofHip,            // Góc lồi giữa 2 mái dốc

    // --- Cửa Đi / Cửa Sổ ---
    DoorFrameBottom,    // Khung cửa dưới (thường trùng WallBottom)
    DoorFrameSide,      // Khung cửa bên
    DoorFrameTop,       // Khung cửa trên
    WindowSill,         // Bệ cửa sổ (thường trùng WallBottom/WallSide)
    WindowFrameSide,    // Khung cửa sổ bên
    WindowFrameTop,     // Khung cửa sổ trên

    // --- Cấu Trúc Hỗ Trợ ---
    PillarTop,          // Đỉnh cột
    PillarBottom,       // Đáy cột
    BeamEnd,            // Đầu dầm
    BeamSide,           // Cạnh dầm

    // --- Cầu Thang / Hàng Rào ---
    StairBottom,        // Chân cầu thang
    StairTop,           // Đỉnh cầu thang
    StairSide,          // Bên cạnh cầu thang (cho lan can)
    FencePostBottom,    // Chân cọc hàng rào
    FencePostTop,       // Đỉnh cọc hàng rào
    FenceRailMount,     // Điểm gắn thanh rào

    // --- Gắn Vật Dụng ---
    WallSurfaceMount,   // Điểm gắn đồ lên tường (đèn, kệ)
    CeilingMount,       // Điểm gắn đồ lên trần
    FloorMount          // Điểm đặt đồ trên sàn
}

public enum ConnectionType {
    Opposite,       // Ngược hướng nhau (dot product gần -1): ví dụ nối 2 thanh thẳng
    Perpendicular,  // Vuông góc với nhau (dot product gần 0): ví dụ góc tường, thanh ngang với cột
    Parallel,       // Cùng hướng (dot product gần 1): ví dụ nối sàn với mái
    Any             // Không quan tâm đến hướng (chấp nhận mọi hướng)
}

[ExecuteInEditMode] // Cho phép script chạy trong editor
public class SnapPoint : MonoBehaviour {

    [Tooltip("Loại của điểm snap này.")]
    public SnapType pointType = SnapType.None;

    [Tooltip("Danh sách các loại điểm snap mà điểm này chấp nhận kết nối TỚI.")]
    public List<SnapType> acceptedTypes = new List<SnapType>();

    [Tooltip("Điểm snap này có cung cấp hỗ trợ cấu trúc không?")]
    public bool providesSupport = true; // Mặc định là có cho các bộ phận kết cấu

    // --- Tùy chọn: Tinh chỉnh hình ảnh ---
    [Tooltip("Offset vị trí nhỏ để điều chỉnh khớp nối hình ảnh.")]
    public Vector3 visualOffset = Vector3.zero;
    [Tooltip("Offset góc xoay nhỏ để điều chỉnh khớp nối hình ảnh.")]
    public Quaternion visualRotationOffset = Quaternion.identity;

    [Header("Thiết lập hướng snap")]
    [Tooltip("Hướng của snap point (forward) nên hướng ra bên ngoài khối building. Tự động hiển thị trực quan trong Scene.")]
    public SnapDirection snapDirection = SnapDirection.Forward;

    [Tooltip("Loại kết nối được chấp nhận cho snap point này")]
    public ConnectionType connectionType = ConnectionType.Opposite;

    [Tooltip("Kích thước mũi tên hiển thị trong editor")]
    public float arrowSize = 0.25f;

    [Tooltip("Hiển thị các vector hướng (debug)")]
    public bool showDirectionVectors = false;

    // Enum xác định hướng của snap point
    public enum SnapDirection {
        Forward,    // +Z: Điểm snap hướng ra trước
        Back,       // -Z: Điểm snap hướng ra sau
        Up,         // +Y: Điểm snap hướng lên trên
        Down,       // -Y: Điểm snap hướng xuống dưới
        Left,       // -X: Điểm snap hướng sang trái
        Right       // +X: Điểm snap hướng sang phải
    }

    /// <summary>
    /// Kiểm tra xem điểm snap này có thể kết nối tới một điểm snap khác không.
    /// Phiên bản này kiểm tra một chiều (điểm này chấp nhận điểm kia).
    /// </summary>
    /// <param name="otherPoint">Điểm snap khác cần kiểm tra.</param>
    /// <returns>True nếu có thể kết nối.</returns>
    public bool CanSnapTo(SnapPoint otherPoint) {
        if (otherPoint == null || this.acceptedTypes == null) {
            return false;
        }
        
        // Kiểm tra loại snap
        bool typeMatch = this.acceptedTypes.Contains(otherPoint.pointType);
        
        // Nếu muốn thêm kiểm tra hướng, thêm ở đây
        bool directionMatch = AreDirectionsCompatible(otherPoint);
        
        return typeMatch && directionMatch;
    }
    
    /// <summary>
    /// Kiểm tra xem hướng của hai snap point có tương thích cho việc kết nối không
    /// dựa trên ConnectionType đã chỉ định
    /// </summary>
    private bool AreDirectionsCompatible(SnapPoint other) {
        // Nếu một trong hai snap point chấp nhận mọi hướng, luôn trả về true
        if (this.connectionType == ConnectionType.Any || other.connectionType == ConnectionType.Any)
            return true;

        // Lấy vector hướng của cả hai snap point
        Vector3 thisDirection = GetDirectionVector();
        Vector3 otherDirection = other.GetDirectionVector();
        
        // Tính dot product để xác định góc giữa hai vector
        float dotProduct = Vector3.Dot(thisDirection.normalized, otherDirection.normalized);
        
        // Kiểm tra theo loại kết nối được yêu cầu
        switch (this.connectionType) {
            case ConnectionType.Opposite:       // Ngược hướng (khoảng 180 độ)
                return dotProduct <= -0.7f;     // Gần với -1
                
            case ConnectionType.Perpendicular:  // Vuông góc (khoảng 90 độ)
                return Mathf.Abs(dotProduct) <= 0.3f;  // Gần với 0
                
            case ConnectionType.Parallel:       // Cùng hướng (khoảng 0 độ)
                return dotProduct >= 0.7f;      // Gần với 1
                
            default:
                return true; // Mặc định chấp nhận mọi hướng
        }
    }
    
    /// <summary>
    /// Lấy vector hướng tương ứng với SnapDirection đã chọn
    /// </summary>
    public Vector3 GetDirectionVector() {
        switch (snapDirection) {
            case SnapDirection.Forward:
                return transform.forward;
            case SnapDirection.Back:
                return -transform.forward;
            case SnapDirection.Up:
                return transform.up;
            case SnapDirection.Down:
                return -transform.up;
            case SnapDirection.Right:
                return transform.right;
            case SnapDirection.Left:
                return -transform.right;
            default:
                return transform.forward;
        }
    }

    // --- Vẽ Gizmo trong Scene View để dễ dàng hình dung ---
    void OnDrawGizmos() {
        // Thay đổi màu dựa trên loại snap
        switch (pointType) {
            case SnapType.FoundationTopEdge:
            case SnapType.FoundationTopCorner:
                Gizmos.color = Color.blue;
                break;
            case SnapType.WallBottom:
            case SnapType.WallTop:
            case SnapType.WallSide:
                Gizmos.color = Color.green;
                break;
            case SnapType.RoofBottomEdge:
            case SnapType.RoofRidge:
                Gizmos.color = Color.red;
                break;
            default:
                Gizmos.color = Color.yellow;
                break;
        }

        // Vẽ một hình cầu nhỏ tại vị trí điểm snap
        Gizmos.DrawWireSphere(transform.position, 0.08f);

        // Vẽ hướng của điểm snap theo SnapDirection
        Vector3 directionVector = GetDirectionVector();
        
        // Màu khác nhau cho các loại kết nối
        switch (connectionType) {
            case ConnectionType.Opposite:
                Gizmos.color = new Color(1f, 0.5f, 0, 1); // Orange for opposite
                break;
            case ConnectionType.Perpendicular:
                Gizmos.color = new Color(0, 0.8f, 0.8f, 1); // Cyan for perpendicular
                break;
            case ConnectionType.Parallel:
                Gizmos.color = new Color(1f, 0.8f, 0.2f, 1); // Yellow for parallel
                break;
            case ConnectionType.Any:
                Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 1); // White for any
                break;
        }
        
        // Vẽ mũi tên chỉ hướng
        DrawArrow(transform.position, directionVector * arrowSize);
        
        // Hiển thị các vector hướng local nếu bật debug
        if (showDirectionVectors) {
            Gizmos.color = Color.red;   // X-axis
            Gizmos.DrawLine(transform.position, transform.position + transform.right * 0.2f);
            
            Gizmos.color = Color.green; // Y-axis
            Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.2f);
            
            Gizmos.color = Color.blue;  // Z-axis
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.2f);
        }
    }
    
    // Vẽ mũi tên giúp trực quan hướng của snap point
    private void DrawArrow(Vector3 start, Vector3 direction) {
        Gizmos.DrawRay(start, direction);
        
        // Vẽ đầu mũi tên
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 30, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 30, 0) * Vector3.forward;
        Vector3 up = Quaternion.LookRotation(direction) * Quaternion.Euler(-30, 180, 0) * Vector3.forward;
        Vector3 down = Quaternion.LookRotation(direction) * Quaternion.Euler(30, 180, 0) * Vector3.forward;
        
        float arrowHeadLength = direction.magnitude * 0.3f;
        Gizmos.DrawRay(start + direction, right * arrowHeadLength);
        Gizmos.DrawRay(start + direction, left * arrowHeadLength);
        Gizmos.DrawRay(start + direction, up * arrowHeadLength);
        Gizmos.DrawRay(start + direction, down * arrowHeadLength);
    }

#if UNITY_EDITOR
    // Cập nhật trực quan trong Editor
    private void Update() {
        if (UnityEditor.EditorApplication.isPlaying == false) {
            // Force redraw để hiển thị đúng hướng snap khi thay đổi
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
    }
#endif
}
