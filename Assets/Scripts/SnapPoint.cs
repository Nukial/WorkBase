using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Xác định các loại điểm kết nối cho việc xây dựng công trình
/// </summary>
public enum SnapType
{
    Wall,
    Floor,
    Roof,
    PipeEnd,
    PillarBase,
    Corner,
    Window,
    Door
    // Thêm các loại khác nếu cần
}

/// <summary>
/// Đại diện cho một điểm kết nối trong hệ thống khớp nối công trình.
/// Gắn thành phần này vào các GameObject trống là con của các prefab có thể xây dựng.
/// </summary>
public class SnapPoint : MonoBehaviour
{
    // Kiểu của điểm khớp nối này
    public SnapType pointType;
    
    // Các loại mà điểm khớp nối này có thể kết nối đến
    public List<SnapType> allowedTargetTypes = new List<SnapType>();
    
    // Hướng để khớp nối định hướng
    public Vector3 snapDirection = Vector3.forward;
    
    // Hiển thị trực quan để gỡ lỗi
    [Header("Hiển Thị Gỡ Lỗi")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private float gizmoSize = 0.1f;
    [SerializeField] private Color gizmoColor = Color.yellow;
    
    [Header("Hiển Thị Hướng")]
    [SerializeField] private bool showDirectionArrow = true;
    [SerializeField] private float arrowLength = 0.3f;
    [SerializeField] private float arrowHeadSize = 0.05f;
    [SerializeField] private Color directionColor = Color.blue;

    [Header("Xác Thực Kết Nối")]
    [SerializeField] private string buildingTag = "Buildings";
    [SerializeField] private LayerMask validBuildingLayers = -1; // Mặc định là tất cả
    
    [Header("Cấu Hình Bề Mặt Hợp Lệ")]
    [Tooltip("Các tag được coi là bề mặt hợp lệ để đặt công trình")]
    [SerializeField] private static List<string> validSurfaceTags = new List<string>() { "Ground", "Terrain", "Foundation" };
    [Tooltip("Các layer được coi là bề mặt hợp lệ để đặt công trình")]
    [SerializeField] private static List<string> validSurfaceLayers = new List<string>() { "Ground", "BuildingSurface" };
    
    /// <summary>
    /// Vẽ chỉ báo trực quan cho điểm khớp nối trong editor
    /// </summary>
    private void OnDrawGizmos()
    {
        if (showGizmo)
        {
            // Vẽ hình cầu tại điểm khớp nối
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, gizmoSize);
            
            // Vẽ mũi tên hướng
            if (showDirectionArrow)
            {
                Vector3 direction = transform.TransformDirection(snapDirection).normalized;
                Vector3 arrowStart = transform.position;
                Vector3 arrowEnd = arrowStart + direction * arrowLength;
                
                // Vẽ trục mũi tên
                Gizmos.color = directionColor;
                Gizmos.DrawLine(arrowStart, arrowEnd);
                
                // Vẽ đầu mũi tên
                DrawArrowHead(arrowEnd, direction, arrowHeadSize, directionColor);
                
                // Vẽ nhãn văn bản nhỏ
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(arrowEnd, pointType.ToString());
                #endif
            }
        }
    }
    
    /// <summary>
    /// Vẽ đầu mũi tên tại vị trí chỉ định hướng theo direction
    /// </summary>
    private void DrawArrowHead(Vector3 position, Vector3 direction, float size, Color color)
    {
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.Cross(direction, Vector3.forward).normalized;
        }
        
        Vector3 up = Vector3.Cross(right, direction).normalized;
        
        // Tính toán các điểm của đầu mũi tên
        Vector3 back = position - direction * size;
        Vector3 right1 = back + right * size * 0.5f;
        Vector3 right2 = back - right * size * 0.5f;
        Vector3 up1 = back + up * size * 0.5f;
        Vector3 up2 = back - up * size * 0.5f;
        
        Gizmos.color = color;
        
        // Vẽ 4 cạnh của đầu mũi tên
        Gizmos.DrawLine(position, right1);
        Gizmos.DrawLine(position, right2);
        Gizmos.DrawLine(position, up1);
        Gizmos.DrawLine(position, up2);
        
        // Vẽ đáy của đầu mũi tên
        Gizmos.DrawLine(right1, up1);
        Gizmos.DrawLine(up1, right2);
        Gizmos.DrawLine(right2, up2);
        Gizmos.DrawLine(up2, right1);
    }
    
    /// <summary>
    /// Kiểm tra xem một điểm khớp nối khác có hợp lệ để kết nối không
    /// </summary>
    /// <param name="otherSnapPoint">Điểm khớp nối khác cần kiểm tra</param>
    /// <returns>True nếu kết nối hợp lệ, ngược lại là false</returns>
    public bool IsValidConnection(SnapPoint otherSnapPoint)
    {
        // Kiểm tra xem đối tượng kia có tag công trình không
        if (!string.IsNullOrEmpty(buildingTag) && !otherSnapPoint.CompareTag(buildingTag))
        {
            return false;
        }
        
        // Kiểm tra xem đối tượng kia có nằm trên các layer công trình hợp lệ không
        if (validBuildingLayers != -1 && (validBuildingLayers.value & (1 << otherSnapPoint.gameObject.layer)) == 0)
        {
            return false;
        }
        
        // Kiểm tra tính tương thích của các loại khớp nối
        if (!allowedTargetTypes.Contains(otherSnapPoint.pointType))
        {
            return false;
        }
        
        // Tất cả kiểm tra đã vượt qua
        return true;
    }
    
    /// <summary>
    /// Kiểm tra xem một bề mặt có hợp lệ cho việc đặt ban đầu không
    /// </summary>
    /// <param name="surfaceObject">Đối tượng đại diện cho bề mặt</param>
    /// <returns>True nếu bề mặt hợp lệ để đặt, ngược lại là false</returns>
    public static bool IsValidPlacementSurface(GameObject surfaceObject)
    {
        if (surfaceObject == null) return false;
        
        // Kiểm tra tag (chỉ kiểm tra nếu tag tồn tại để tránh lỗi)
        bool isValidTag = false;
        foreach (string tag in validSurfaceTags)
        {
            // Chỉ kiểm tra nếu tag tồn tại để tránh lỗi
            try
            {
                if (surfaceObject.CompareTag(tag))
                {
                    isValidTag = true;
                    break;
                }
            }
            catch (UnityException)
            {
                // Tag không tồn tại, bỏ qua
                continue;
            }
        }
        
        // Kiểm tra layer
        bool isValidLayer = false;
        foreach (string layerName in validSurfaceLayers)
        {
            int layerIndex = LayerMask.NameToLayer(layerName);
            if (layerIndex != -1 && surfaceObject.layer == layerIndex)
            {
                isValidLayer = true;
                break;
            }
        }
        
        // Nếu không có tag hoặc layer hợp lệ được định nghĩa, cho phép tất cả bề mặt
        if (validSurfaceTags.Count == 0 && validSurfaceLayers.Count == 0)
        {
            return true;
        }
        
        return isValidTag || isValidLayer;
    }
    
    /// <summary>
    /// Kiểm tra xem việc đặt có gây chồng lấn với các đối tượng quan trọng không
    /// </summary>
    /// <param name="bounds">Giới hạn của đối tượng đang được đặt</param>
    /// <param name="exclusionLayers">Các layer cần kiểm tra chồng lấn</param>
    /// <returns>True nếu việc đặt an toàn (không có chồng lấn), ngược lại là false</returns>
    public static bool IsSafePlacement(Bounds bounds, LayerMask exclusionLayers)
    {
        // Kiểm tra chồng lấn với người chơi, đối tượng quan trọng, v.v.
        Collider[] overlaps = Physics.OverlapBox(
            bounds.center, 
            bounds.extents * 0.9f, // Nhỏ hơn một chút để cho phép các tiếp xúc nhỏ
            Quaternion.identity, 
            exclusionLayers
        );
        
        // Nếu tìm thấy bất kỳ chồng lấn nào, việc đặt không an toàn
        return overlaps.Length == 0;
    }
    
    /// <summary>
    /// Thêm tag vào danh sách bề mặt hợp lệ
    /// </summary>
    public static void AddValidSurfaceTag(string tag)
    {
        if (!string.IsNullOrEmpty(tag) && !validSurfaceTags.Contains(tag))
        {
            validSurfaceTags.Add(tag);
        }
    }
    
    /// <summary>
    /// Thêm layer vào danh sách bề mặt hợp lệ
    /// </summary>
    public static void AddValidSurfaceLayer(string layerName)
    {
        if (!string.IsNullOrEmpty(layerName) && !validSurfaceLayers.Contains(layerName))
        {
            validSurfaceLayers.Add(layerName);
        }
    }
    
    /// <summary>
    /// Xóa tất cả bề mặt hợp lệ đã đăng ký
    /// </summary>
    public static void ClearValidSurfaces()
    {
        validSurfaceTags.Clear();
        validSurfaceLayers.Clear();
    }
}
