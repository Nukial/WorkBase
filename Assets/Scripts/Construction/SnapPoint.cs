using UnityEngine;
// Đối tượng này dùng để đánh dấu các điểm snap trong scene,
public enum SnapType { Connector, //Điểm kết nối, 
                       Anchor, //Điểm neo
                       Edge, //Cạnh
                       Corner, //Góc
                       Center, //Giữa
                       Surface } //Bề mặt
// Nhóm điểm snap, có thể là tường, sàn, mái hoặc móng
public enum SnapGroup { Any,//Tất cả, 
                        Wall, //Tường
                        Floor, //Sàn
                        Roof, //Mái
                        Foundation } //Móng 

public class SnapPoint : MonoBehaviour
{
    public SnapType pointType = SnapType.Connector;
    public SnapGroup group = SnapGroup.Any;
    
    // Cài đặt hiển thị
    public float gizmoRadius = 0.05f;
    public float directionLineLength = 0.1f;
    public bool showLabels = true;

    void OnDrawGizmos()
    {
        // Vẽ quả cầu tại điểm snap
        Gizmos.color = (pointType == SnapType.Connector) ? Color.blue : Color.green;
        Gizmos.DrawSphere(transform.position, gizmoRadius);
        
        // Vẽ đường chỉ hướng
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * directionLineLength);
        
        // Vẽ hướng lên với màu khác
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * directionLineLength * 0.8f);
        
        // Vẽ nhãn trong scene view
        if (showLabels)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * gizmoRadius * 2, 
                $"{name}\n{pointType} - {group}");
#endif
        }
    }
    
    // Hiển thị layer của điểm snap này
    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * gizmoRadius * 4, 
            $"Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})");
#endif
    }
}
