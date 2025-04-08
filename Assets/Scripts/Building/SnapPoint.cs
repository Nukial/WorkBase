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

// Enum SnapType đã được định nghĩa ở trên

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
        // Kiểm tra xem loại của điểm kia có nằm trong danh sách chấp nhận của điểm này không
        return this.acceptedTypes.Contains(otherPoint.pointType);

        // --- Hoặc kiểm tra 2 chiều (khắt khe hơn) ---
        // return this.acceptedTypes.Contains(otherPoint.pointType) &&
        //        otherPoint.acceptedTypes != null && // Tránh lỗi null
        //        otherPoint.acceptedTypes.Contains(this.pointType);
    }

    // --- Vẽ Gizmo trong Scene View để dễ dàng hình dung ---
    void OnDrawGizmos() {
        // Thay đổi màu dựa trên loại hoặc trạng thái
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
        Gizmos.DrawWireSphere(transform.position, 0.08f); // Giảm kích thước một chút

        // Vẽ hướng của điểm snap (hữu ích để debug xoay)
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.2f);
    }
}
