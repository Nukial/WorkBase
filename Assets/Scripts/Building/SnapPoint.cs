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
    UpperFloorEdge,     // Cạnh sàn tầng trên (chuyên dụng cho sàn cao tầng)
    LowerFloorEdge,     // Cạnh sàn tầng dưới (chuyên dụng cho sàn cao tầng)

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
    StairLanding,       // Chiếu nghỉ cầu thang (cho cầu thang có chữ L hoặc U)
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
    Angle45,        // Góc 45 độ (dot product gần 0.7 hoặc -0.7): tạo góc xiên
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

    [Header("Thiết lập góc xoay")]
    [Tooltip("Cho phép khóa góc xoay khi snap vào điểm này")]
    public bool lockRotation = false;
    
    [Tooltip("Góc bước khi xoay (15, 45, 90 độ)")]
    public float rotationStep = 45f;

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
        
        // Kiểm tra đặc biệt cho sàn cao tầng và cầu thang
        if (!typeMatch) {
            typeMatch = IsElevatedFloorCompatible(otherPoint);
        }
        
        // Kiểm tra hướng
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
            
        // Thêm trường hợp đặc biệt cho kết nối tường-tường
        if (this.pointType == SnapType.WallSide && other.pointType == SnapType.WallSide) {
            return IsWallConnectionCompatible(other);
        }

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
                
            case ConnectionType.Angle45:        // Góc 45 độ
                float angle = Vector3.Angle(thisDirection, otherDirection);
                return (angle >= 35f && angle <= 55f) || (angle >= 125f && angle <= 145f);
                
            case ConnectionType.Parallel:       // Cùng hướng (khoảng 0 độ)
                return dotProduct >= 0.7f;      // Gần với 1
                
            default:
                return true; // Mặc định chấp nhận mọi hướng
        }
    }

    /// <summary>
    /// Phương thức đặc biệt để kiểm tra tính tương thích của kết nối tường-tường
    /// Hỗ trợ cả kết nối nối tiếp (thẳng hàng), góc vuông và góc 45 độ
    /// </summary>
    /// <param name="other">Điểm snap của tường khác</param>
    /// <returns>True nếu có thể kết nối</returns>
    private bool IsWallConnectionCompatible(SnapPoint other) {
        Vector3 thisDirection = GetDirectionVector();
        Vector3 otherDirection = other.GetDirectionVector();
        
        // Tính dot product để xác định góc giữa hai vector
        float dotProduct = Vector3.Dot(thisDirection.normalized, otherDirection.normalized);
        float angle = Vector3.Angle(thisDirection, otherDirection);
        
        // Trường hợp 1: Kết nối nối tiếp (thẳng hàng)
        // Hai tường nên đối diện nhau (gần 180 độ - dot product gần -1)
        bool isOppositeConnection = dotProduct <= -0.7f;
        
        // Trường hợp 2: Kết nối góc vuông (vuông góc với nhau)
        // Hai tường nên vuông góc với nhau (gần 90 độ - dot product gần 0)
        bool isPerpendicularConnection = Mathf.Abs(dotProduct) <= 0.3f;
        
        // Trường hợp 3: Kết nối góc 45 độ
        // Góc nên gần 45 độ hoặc 135 độ
        bool is45DegreeConnection = (angle >= 35f && angle <= 55f) || (angle >= 125f && angle <= 145f);
        
        // Trường hợp đặc biệt - kết nối linh hoạt cho WallSide
        // Cho phép cả kết nối thẳng hàng, góc vuông và góc 45 độ
        if (connectionType == ConnectionType.Any) {
            return isOppositeConnection || isPerpendicularConnection || is45DegreeConnection;
        }
        
        // Kiểm tra dựa trên loại kết nối được yêu cầu
        switch (connectionType) {
            case ConnectionType.Opposite:
                return isOppositeConnection;
                
            case ConnectionType.Perpendicular:
                return isPerpendicularConnection;
                
            case ConnectionType.Angle45:
                return is45DegreeConnection;
                
            case ConnectionType.Parallel:
                return dotProduct >= 0.7f;  // Cùng hướng (gần 0 độ)
                
            default:
                return false;
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

    /// <summary>
    /// Phương thức bổ sung để kiểm tra khả năng kết nối với các sàn cao tầng và cầu thang
    /// </summary>
    public bool IsElevatedFloorCompatible(SnapPoint otherPoint) {
        // Kiểm tra đặc biệt cho các kết nối sàn cao tầng và cầu thang
        if (this.pointType == SnapType.StairTop && otherPoint.pointType == SnapType.FloorEdge) {
            // Cho phép kết nối từ đỉnh cầu thang lên cạnh sàn
            return true;
        }
        
        if (this.pointType == SnapType.StairBottom && 
           (otherPoint.pointType == SnapType.FloorEdge || otherPoint.pointType == SnapType.FoundationTopEdge)) {
            // Cho phép kết nối từ chân cầu thang xuống cạnh sàn hoặc móng
            return true;
        }
        
        if (this.pointType == SnapType.FloorEdge && 
           (otherPoint.pointType == SnapType.StairTop || otherPoint.pointType == SnapType.StairBottom)) {
            // Cho phép kết nối từ sàn đến đỉnh/chân cầu thang
            return true;
        }
        
        // Các trường hợp khác có thể thêm vào đây
        
        return false;
    }

    /// <summary>
    /// Tính toán góc xoay quanh trục Y dựa trên loại kết nối và hướng
    /// </summary>
    public Quaternion GetSnappedRotation(SnapPoint otherPoint, Quaternion currentRotation) {
        // Nếu không khóa góc xoay, giữ nguyên góc xoay hiện tại
        if (!lockRotation) return currentRotation;
        
        // Lấy hướng của cả hai snap points
        Vector3 thisDirection = GetDirectionVector();
        Vector3 otherDirection = otherPoint.GetDirectionVector();
        
        // Loại bỏ thành phần Y để chỉ xoay quanh trục Y
        thisDirection.y = 0;
        otherDirection.y = 0;
        
        thisDirection.Normalize();
        otherDirection.Normalize();
        
        // Góc giữa hai hướng
        float angle = Vector3.SignedAngle(thisDirection, otherDirection, Vector3.up);
        
        // Làm tròn góc theo bước xoay
        float snappedAngle = Mathf.Round(angle / rotationStep) * rotationStep;
        
        // Tính góc xoay mới
        Quaternion targetRotation = Quaternion.Euler(0, snappedAngle, 0);
        
        // Áp dụng xoay với tham chiếu tới transform hiện tại
        Quaternion finalRotation = Quaternion.Euler(
            currentRotation.eulerAngles.x,
            snappedAngle,
            currentRotation.eulerAngles.z
        );
        
        return finalRotation;
    }
    
    /// <summary>
    /// Giúp PlayerBuilder duy trì góc xoay do người dùng thiết lập
    /// </summary>
    public Quaternion PreserveUserRotation(Quaternion currentRotation, Quaternion newRotation) {
        // Giữ nguyên góc xoay của người dùng khi không yêu cầu khóa góc
        if (!lockRotation)
            return currentRotation;
            
        // Nếu có khóa góc, chỉ áp dụng góc xoay theo bước
        float currentYRotation = currentRotation.eulerAngles.y;
        float closestStep = Mathf.Round(currentYRotation / rotationStep) * rotationStep;
        
        return Quaternion.Euler(
            currentRotation.eulerAngles.x,
            closestStep,
            currentRotation.eulerAngles.z
        );
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
            case ConnectionType.Angle45:
                Gizmos.color = new Color(0.5f, 0.5f, 1f, 1); // Blue for 45-degree angle
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
