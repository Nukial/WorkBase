using System.Collections.Generic;
using System.Linq;
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
    
    [Tooltip("Tự động điều chỉnh loại kết nối dựa trên góc xoay")]
    public bool autoAdjustConnection = true;
    
    [Tooltip("Danh sách các kiểu kết nối được chấp nhận khi tự động điều chỉnh")]
    public List<ConnectionType> allowedConnectionTypes = new List<ConnectionType>();

    [Tooltip("Bán kính kiểm tra kết nối (chỉ dùng trong Editor)")]
    public float connectionTestRadius = 1.5f;
    [Tooltip("Bật/vẽ các đường nối kiểm tra kết nối trong Editor")]
    public bool drawConnectionTest = false;

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
    /// 
    /// Ví dụ cách sử dụng:
    /// - WallSide.CanSnapTo(WallSide) => true nếu góc xoay phù hợp với connectionType
    /// - WallBottom.CanSnapTo(FoundationTopEdge) => true (tường kết nối với nền)
    /// - RoofBottomEdge.CanSnapTo(WallTop) => true (mái kết nối với đỉnh tường)
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
        bool directionMatch = false;
        
        // Nếu cho phép tự động điều chỉnh, thử tìm kiểu kết nối phù hợp
        if (autoAdjustConnection) {
            directionMatch = TryFindCompatibleConnection(otherPoint);
        } else {
            directionMatch = AreDirectionsCompatible(otherPoint);
        }
        
        return typeMatch && directionMatch;
    }
    
    /// <summary>
    /// Thử tìm kiểu kết nối phù hợp giữa hai snap point
    /// </summary>
    private bool TryFindCompatibleConnection(SnapPoint otherPoint) {
        // Lưu lại kiểu kết nối hiện tại
        ConnectionType originalType = this.connectionType;
        bool isCompatible = false;
        
        // Kiểm tra các kiểu kết nối được cho phép
        if (allowedConnectionTypes.Count > 0) {
            foreach (var connType in allowedConnectionTypes) {
                this.connectionType = connType;
                if (AreDirectionsCompatible(otherPoint)) {
                    isCompatible = true;
                    break;
                }
            }
        } else {
            // Nếu không có danh sách cụ thể, kiểm tra tất cả các kiểu
            foreach (ConnectionType connType in System.Enum.GetValues(typeof(ConnectionType))) {
                this.connectionType = connType;
                if (AreDirectionsCompatible(otherPoint)) {
                    isCompatible = true;
                    break;
                }
            }
        }
        
        // Khôi phục kiểu kết nối ban đầu nếu không tìm thấy kiểu phù hợp
        if (!isCompatible) {
            this.connectionType = originalType;
        }
        
        return isCompatible;
    }
    
    /// <summary>
    /// Xác định kiểu kết nối phù hợp nhất dựa trên góc giữa hai snap point
    /// </summary>
    /// <param name="otherPoint">Điểm snap khác</param>
    /// <returns>Kiểu kết nối phù hợp nhất</returns>
    public ConnectionType DetermineOptimalConnectionType(SnapPoint otherPoint) {
        // Lấy vector hướng của cả hai snap point
        Vector3 thisDirection = GetDirectionVector();
        Vector3 otherDirection = otherPoint.GetDirectionVector();
        
        // Tính dot product để xác định góc giữa hai vector
        float dotProduct = Vector3.Dot(thisDirection.normalized, otherDirection.normalized);
        float angle = Vector3.Angle(thisDirection, otherDirection);
        
        // Dựa vào góc để xác định kiểu kết nối phù hợp nhất
        if (angle > 165f) {
            // Gần như ngược hướng (180 độ ± 15)
            return ConnectionType.Opposite;
        } else if (angle < 15f) {
            // Gần như cùng hướng (0 độ ± 15)
            return ConnectionType.Parallel;
        } else if (angle >= 75f && angle <= 105f) {
            // Gần như vuông góc (90 độ ± 15)
            return ConnectionType.Perpendicular;
        } else if ((angle >= 30f && angle <= 60f) || (angle >= 120f && angle <= 150f)) {
            // Góc 45 độ hoặc 135 độ (± 15)
            return ConnectionType.Angle45;
        } else {
            // Các trường hợp khác
            return ConnectionType.Any;
        }
    }
    
    /// <summary>
    /// Áp dụng kiểu kết nối phù hợp nhất dựa trên góc với điểm snap khác
    /// </summary>
    /// <param name="otherPoint">Điểm snap khác</param>
    /// <returns>True nếu tìm được kiểu kết nối phù hợp</returns>
    public bool ApplyOptimalConnectionType(SnapPoint otherPoint) {
        if (!autoAdjustConnection)
            return false;
            
        // Xác định kiểu kết nối tối ưu dựa trên góc giữa hai điểm
        ConnectionType optimalType = DetermineOptimalConnectionType(otherPoint);
        
        // Xây dựng danh sách các kiểu kết nối được phép cho điểm này
        List<ConnectionType> thisAllowed = allowedConnectionTypes.Count > 0
            ? new List<ConnectionType>(allowedConnectionTypes)
            : System.Enum.GetValues(typeof(ConnectionType)).Cast<ConnectionType>().ToList();
        
        // Xây dựng danh sách các kiểu kết nối được phép cho điểm kia
        List<ConnectionType> otherAllowed = otherPoint.allowedConnectionTypes.Count > 0
            ? new List<ConnectionType>(otherPoint.allowedConnectionTypes)
            : System.Enum.GetValues(typeof(ConnectionType)).Cast<ConnectionType>().ToList();
        
        // Tìm giao của hai danh sách
        List<ConnectionType> commonTypes = thisAllowed.Intersect(otherAllowed).ToList();
        if (commonTypes.Count == 0)
            return false;
        
        // Nếu kiểu tối ưu có trong giao, dùng nó; nếu không, chọn kiểu đầu tiên
        connectionType = commonTypes.Contains(optimalType) ? optimalType : commonTypes[0];
        return true;
    }
    
    /// <summary>
    /// Kiểm tra xem hướng của hai snap point có tương thích cho việc kết nối không
    /// dựa trên ConnectionType đã chỉ định.
    /// 
    /// Ví dụ thực tế:
    /// - Opposite (180°): Kết nối thẳng hàng giữa hai tường, hai thanh rào, v.v.
    /// - Perpendicular (90°): Tường góc vuông, kết nối tường-nền, tường-sàn
    /// - Angle45 (45° hoặc 135°): Tường góc 45 độ, thanh chéo, mái xiên
    /// - Parallel (0°): Cùng hướng như sàn-sàn, mái-mái cùng hướng nghiêng
    /// </summary>
    private bool AreDirectionsCompatible(SnapPoint other) {
        // Nếu một trong hai snap point chấp nhận mọi hướng, luôn trả về true
        if (this.connectionType == ConnectionType.Any || other.connectionType == ConnectionType.Any)
            return true;
            
        // Áp dụng kiểm tra hướng cho tất cả các loại snap point, không chỉ tường
        Vector3 thisDirection = GetDirectionVector();
        Vector3 otherDirection = other.GetDirectionVector();
        
        // Tính dot product để xác định góc giữa hai vector
        float dotProduct = Vector3.Dot(thisDirection.normalized, otherDirection.normalized);
        float angle = Vector3.Angle(thisDirection, otherDirection);
        
        // Kiểm tra theo loại kết nối được yêu cầu
        switch (this.connectionType) {
            case ConnectionType.Opposite:       // Ngược hướng (khoảng 180 độ)
                return angle >= 165f;           // Cho phép sai số 15 độ
                
            case ConnectionType.Perpendicular:  // Vuông góc (khoảng 90 độ)
                return angle >= 75f && angle <= 105f;  // Cho phép sai số 15 độ
                
            case ConnectionType.Angle45:        // Góc 45 độ hoặc 135 độ
                return IsAngle45Compatible(angle);
                
            case ConnectionType.Parallel:       // Cùng hướng (khoảng 0 độ)
                return angle <= 15f;            // Cho phép sai số 15 độ
                
            default:
                return true; // Mặc định chấp nhận mọi hướng
        }
    }
    
    /// <summary>
    /// Kiểm tra riêng cho góc 45 độ, hỗ trợ cả góc 45, 135, 225, 315 độ
    /// </summary>
    public bool IsAngle45Compatible(float angle) {
        // Góc 45 +/- 15 độ
        if (angle >= 30f && angle <= 60f) return true;
        // Góc 135 +/- 15 độ
        if (angle >= 120f && angle <= 150f) return true;
        //// Góc 225 +/- 15 độ (tương đương 135 độ)
        if (angle >= 210f && angle <= 240f) return true;
        // Góc 315 +/- 15 độ (tương đương 45 độ)
        if (angle >= 300f && angle <= 330f) return true;
        return false;
    }

    /// <summary>
    /// Phương thức đặc biệt để kiểm tra tính tương thích của kết nối tường-tường
    /// Hỗ trợ cả kết nối nối tiếp (thẳng hàng), góc vuông và góc 45 độ
    /// 
    /// Ví dụ thực tế:
    /// - Tường thẳng hàng: Hai tường cùng mặt phẳng, snap hướng ngược nhau
    /// - Tường góc 90 độ: Tạo góc nhà vuông, snap vuông góc nhau
    /// - Tường góc 45 độ: Tạo góc xiên trong nhà, phổ biến trong thiết kế hiện đại
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
        
        // Trường hợp 3: Kết nối góc 45 độ hoặc 135 độ
        bool is45DegreeConnection = IsAngle45Compatible(angle);
        
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
    /// 
    /// Lưu ý vị trí đặt snap:
    /// - Tường: WallSide nên đặt tại giữa cạnh, hướng ra ngoài
    /// - Nền: FoundationTopEdge đặt ở cạnh trên nền, hướng ra ngoài
    /// - Góc: *Corner nên đặt chính xác tại góc, hướng ra theo đường chéo
    /// - Cửa: DoorFrameSide đặt giữa khung cửa, hướng vào trong khung
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
    /// 
    /// Ví dụ thực tế:
    /// - Cầu thang nối với sàn: StairTop kết nối với FloorEdge ở tầng trên
    /// - Cầu thang nối với nền: StairBottom kết nối với FoundationTopEdge
    /// - Cầu thang có bản mã: StairLanding kết nối với FloorEdge hoặc StairTop/Bottom
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
    /// Tính toán góc xoay dựa trên loại kết nối và hướng, hỗ trợ snap nhiều góc độ
    /// 
    /// Ví dụ thực tế:
    /// - Tường nối tiếp: Giữ nguyên hướng khi nối thẳng hàng
    /// - Tường góc 90 độ: Xoay 90 độ so với tường hiện có
    /// - Mái nhà: Mái dốc cần xoay để đỉnh chỉ lên trên chính xác
    /// </summary>
    public Quaternion GetSnappedRotation(SnapPoint otherPoint, Quaternion currentRotation) {
        // Nếu không khóa góc xoay, giữ nguyên góc xoay hiện tại
        if (!lockRotation) return currentRotation;
        
        // Tự động áp dụng kiểu kết nối phù hợp nếu được bật
        if (autoAdjustConnection) {
            ApplyOptimalConnectionType(otherPoint);
        }
        
        // Lấy hướng của cả hai snap points
        Vector3 thisDirection = GetDirectionVector();
        Vector3 otherDirection = otherPoint.GetDirectionVector();
        
        // Xử lý dựa trên hướng của snap point
        Vector3 targetDirection;
        
        switch (connectionType) {
            case ConnectionType.Opposite:
                // Ngược hướng với snap point kia
                targetDirection = -otherDirection;
                break;
                
            case ConnectionType.Perpendicular:
                // Góc vuông với hướng của snap point kia
                targetDirection = GetPerpendicularDirection(otherDirection);
                break;
                
            case ConnectionType.Angle45:
                // Góc 45 độ với hướng của snap point kia
                targetDirection = Get45DegreeDirection(otherDirection);
                break;
                
            case ConnectionType.Parallel:
                // Cùng hướng với snap point kia
                targetDirection = otherDirection;
                break;
                
            default:
                // Chọn kiểu kết nối gần nhất với góc hiện tại
                targetDirection = GetClosestDirectionToCurrentAngle(otherDirection, currentRotation);
                break;
        }
        
        // Tính góc xoay để đạt được hướng mong muốn
        Quaternion targetRotation = SnapToNearestStep(targetDirection, currentRotation);
        
        return targetRotation;
    }
    
    /// <summary>
    /// Tìm hướng gần nhất với góc xoay hiện tại
    /// </summary>
    private Vector3 GetClosestDirectionToCurrentAngle(Vector3 baseDirection, Quaternion currentRotation) {
        // Lấy forward direction của đối tượng hiện tại
        Vector3 currentForward = currentRotation * Vector3.forward;
        
        // Tính góc giữa hướng hiện tại và hướng cơ sở
        float angle = Vector3.SignedAngle(currentForward, baseDirection, Vector3.up);
        
        // Xây dựng danh sách các góc cần kiểm tra (0, 45, 90, 135, 180, 225, 270, 315)
        float[] possibleAngles = { 0, 45, 90, 135, 180, 225, 270, 315 };
        
        // Tìm góc gần nhất
        float closestAngle = 0;
        float minDifference = 360;
        
        foreach (float possibleAngle in possibleAngles) {
            float difference = Mathf.Abs(Mathf.DeltaAngle(angle, possibleAngle));
            if (difference < minDifference) {
                minDifference = difference;
                closestAngle = possibleAngle;
            }
        }
        
        // Tạo hướng mới bằng cách xoay hướng cơ sở theo góc đã chọn
        return Quaternion.Euler(0, closestAngle, 0) * baseDirection;
    }
    
    /// <summary>
    /// Tìm vector vuông góc với vector đã cho
    /// </summary>
    private Vector3 GetPerpendicularDirection(Vector3 direction) {
        // Tạo vector vuông góc bằng cách hoán đổi x,z và đảo dấu
        return new Vector3(direction.z, direction.y, -direction.x).normalized;
    }
    
    /// <summary>
    /// Tính vector hướng tạo góc 45 độ với vector đã cho
    /// </summary>
    private Vector3 Get45DegreeDirection(Vector3 direction) {
        // Kết hợp vector gốc và vector vuông góc với tỷ lệ 1:1 để tạo góc 45 độ
        Vector3 perpendicular = GetPerpendicularDirection(direction);
        return (direction + perpendicular).normalized;
    }
    
    /// <summary>
    /// Làm tròn góc xoay đến bội số của rotationStep (mặc định là 45 độ)
    /// </summary>
    private Quaternion SnapToNearestStep(Vector3 targetDirection, Quaternion currentRotation) {
        // Tách góc xoay hiện tại
        Vector3 currentEuler = currentRotation.eulerAngles;
        
        // Tính góc xoay mới từ target direction, giữ nguyên góc X và Z
        Quaternion lookRotation = Quaternion.LookRotation(targetDirection);
        Vector3 newEuler = lookRotation.eulerAngles;
        
        // Làm tròn góc Y đến bội số của rotationStep
        float snappedY = Mathf.Round(newEuler.y / rotationStep) * rotationStep;
        
        // Tùy thuộc vào hướng snap, có thể cần xoay quanh trục khác
        switch (snapDirection) {
            case SnapDirection.Up:
            case SnapDirection.Down:
                // Xoay quanh trục Y cho hướng lên/xuống
                return Quaternion.Euler(currentEuler.x, snappedY, currentEuler.z);
                
            case SnapDirection.Forward:
            case SnapDirection.Back:
                // Xoay quanh trục Y cho hướng trước/sau
                return Quaternion.Euler(currentEuler.x, snappedY, currentEuler.z);
                
            case SnapDirection.Left:
            case SnapDirection.Right:
                // Xoay quanh trục Y cho hướng trái/phải
                return Quaternion.Euler(currentEuler.x, snappedY, currentEuler.z);
                
            default:
                return Quaternion.Euler(currentEuler.x, snappedY, currentEuler.z);
        }
    }
    
    /// <summary>
    /// Giúp duy trì góc xoay do người dùng thiết lập, nhưng vẫn tuân theo bước xoay
    /// </summary>
    public Quaternion PreserveUserRotation(Quaternion currentRotation, Quaternion newRotation) {
        // Nếu không yêu cầu khóa góc, trả về góc hiện tại
        if (!lockRotation)
            return currentRotation;
            
        // Tách các thành phần góc xoay
        Vector3 euler = currentRotation.eulerAngles;
        
        // Làm tròn góc Y theo bước xoay
        float yRotation = euler.y;
        float snappedY = Mathf.Round(yRotation / rotationStep) * rotationStep;
        
        // Áp dụng góc Y đã làm tròn, giữ nguyên X và Z
        return Quaternion.Euler(euler.x, snappedY, euler.z);
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

        // Vẽ kết nối kiểm tra (chỉ dùng trong Editor)
#if UNITY_EDITOR
        if (drawConnectionTest) {
            SnapPoint[] allPoints = FindObjectsByType<SnapPoint>(FindObjectsSortMode.None);
            foreach (var candidate in allPoints) {
                if (candidate == this) continue;
                float dist = Vector3.Distance(transform.position, candidate.transform.position);
                if (dist <= connectionTestRadius) {
                    bool canConnect = CanSnapTo(candidate);
                    Gizmos.color = canConnect ? Color.green : Color.red;
                    Gizmos.DrawLine(transform.position, candidate.transform.position);
                }
            }
            // Vẽ vòng tròn bán kính test
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, connectionTestRadius);
        }
#endif
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
