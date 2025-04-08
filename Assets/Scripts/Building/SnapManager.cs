using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý quá trình snap giữa các đối tượng building
/// </summary>
public class SnapManager : MonoBehaviour
{
    public static SnapManager Instance { get; private set; }
    
    [Tooltip("Khoảng cách tối đa để tìm điểm snap gần nhất")]
    public float maxSnapDistance = 2.0f;
    
    [Tooltip("Hiệu ứng trực quan khi snap thành công")]
    public GameObject snapEffectPrefab;
    
    [Tooltip("Bật/tắt rotational snapping (xoay theo bước)")]
    public bool enableRotationalSnapping = true;
    
    [Tooltip("Bật/tắt tự động điều chỉnh kiểu kết nối")]
    public bool enableAutoConnection = true;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Tìm điểm snap gần nhất và tương thích với điểm snap nguồn
    /// </summary>
    /// <param name="sourcePoint">Điểm snap nguồn cần kiểm tra</param>
    /// <param name="excludeObject">Object cần loại trừ khỏi việc kiểm tra</param>
    /// <returns>Điểm snap tương thích gần nhất nếu có, null nếu không tìm thấy</returns>
    public SnapPoint FindNearestCompatibleSnapPoint(SnapPoint sourcePoint, GameObject excludeObject = null)
    {
        // Tìm tất cả snap point trong phạm vi
        Collider[] colliders = Physics.OverlapSphere(sourcePoint.transform.position, maxSnapDistance);
        
        SnapPoint nearestPoint = null;
        float nearestDistance = maxSnapDistance;
        
        foreach (Collider col in colliders)
        {
            // Bỏ qua nếu là object loại trừ
            if (excludeObject != null && (col.transform == excludeObject.transform || col.transform.IsChildOf(excludeObject.transform)))
                continue;
                
            // Tìm tất cả SnapPoint trên gameobject này
            SnapPoint[] points = col.GetComponentsInChildren<SnapPoint>();
            
            foreach (SnapPoint point in points)
            {
                // Kiểm tra tính tương thích
                bool sourceCanSnapToTarget = sourcePoint.CanSnapTo(point);
                bool targetCanSnapToSource = point.CanSnapTo(sourcePoint);
                
                if (sourceCanSnapToTarget || targetCanSnapToSource)
                {
                    float distance = Vector3.Distance(sourcePoint.transform.position, point.transform.position);
                    
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestPoint = point;
                    }
                }
            }
        }
        
        return nearestPoint;
    }
    
    /// <summary>
    /// Thực hiện snap một đối tượng vào điểm snap
    /// </summary>
    /// <param name="objectToSnap">Đối tượng cần snap</param>
    /// <param name="sourcePoint">Điểm snap trên đối tượng</param>
    /// <param name="targetPoint">Điểm snap đích</param>
    /// <returns>True nếu snap thành công</returns>
    public bool SnapObjectToPoint(GameObject objectToSnap, SnapPoint sourcePoint, SnapPoint targetPoint)
    {
        if (objectToSnap == null || sourcePoint == null || targetPoint == null)
            return false;
            
        // Tính toán vị trí
        Vector3 offset = sourcePoint.transform.position - objectToSnap.transform.position;
        Vector3 targetPosition = targetPoint.transform.position - offset;
        
        // Tự động điều chỉnh kiểu kết nối nếu được bật
        if (enableAutoConnection && sourcePoint.autoAdjustConnection)
        {
            sourcePoint.ApplyOptimalConnectionType(targetPoint);
        }
        
        // Tính toán góc xoay
        Quaternion currentRotation = objectToSnap.transform.rotation;
        Quaternion targetRotation = currentRotation;
        
        if (enableRotationalSnapping && sourcePoint.lockRotation)
        {
            targetRotation = sourcePoint.GetSnappedRotation(targetPoint, currentRotation);
        }
        
        // Áp dụng vị trí và góc xoay mới
        objectToSnap.transform.position = targetPosition;
        objectToSnap.transform.rotation = targetRotation;
        
        // Hiệu ứng snap nếu có
        if (snapEffectPrefab != null)
        {
            Instantiate(snapEffectPrefab, targetPoint.transform.position, Quaternion.identity);
        }
        
        return true;
    }
    
    /// <summary>
    /// Tìm tất cả điểm snap tương thích trong phạm vi
    /// </summary>
    /// <param name="sourcePoint">Điểm snap nguồn</param>
    /// <param name="excludeObject">Đối tượng cần loại trừ</param>
    /// <returns>Danh sách các điểm snap tương thích</returns>
    public List<SnapPoint> FindAllCompatibleSnapPoints(SnapPoint sourcePoint, GameObject excludeObject = null)
    {
        List<SnapPoint> compatiblePoints = new List<SnapPoint>();
        
        // Tìm tất cả snap point trong phạm vi
        Collider[] colliders = Physics.OverlapSphere(sourcePoint.transform.position, maxSnapDistance);
        
        foreach (Collider col in colliders)
        {
            // Bỏ qua nếu là object loại trừ
            if (excludeObject != null && (col.transform == excludeObject.transform || col.transform.IsChildOf(excludeObject.transform)))
                continue;
                
            // Tìm tất cả SnapPoint trên gameobject này
            SnapPoint[] points = col.GetComponentsInChildren<SnapPoint>();
            
            foreach (SnapPoint point in points)
            {
                // Kiểm tra tính tương thích
                bool sourceCanSnapToTarget = sourcePoint.CanSnapTo(point);
                bool targetCanSnapToSource = point.CanSnapTo(sourcePoint);
                
                if (sourceCanSnapToTarget || targetCanSnapToSource)
                {
                    compatiblePoints.Add(point);
                }
            }
        }
        
        return compatiblePoints;
    }
}
