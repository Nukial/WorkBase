using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý thông tin và trạng thái của một khối xây dựng
/// 
/// Bao gồm:
/// - Các thuộc tính cơ bản (dữ liệu, máu, trạng thái nền)
/// - Danh sách các điểm snap cho kết nối
/// - Xử lý nhận sát thương và sửa chữa
/// 
/// Hướng dẫn đặt SnapPoint cho các loại BuildingPiece phổ biến:
/// 
/// 1. Nền móng (Foundation):
///    - FoundationTopEdge: Đặt tại trung điểm các cạnh trên, hướng ra ngoài
///    - FoundationTopCorner: Đặt tại các góc trên, hướng theo đường chéo
///    - FoundationSide: Đặt tại trung điểm cạnh bên, hướng ra ngoài
///    
/// 2. Tường (Wall):
///    - WallBottom: Đặt dọc cạnh dưới, hướng xuống dưới
///    - WallTop: Đặt dọc cạnh trên, hướng lên trên
///    - WallSide: Đặt tại giữa cạnh bên, hướng ra ngoài
///    
/// 3. Mái (Roof):
///    - RoofBottomEdge: Đặt tại cạnh dưới, hướng xuống
///    - RoofRidge: Đặt dọc đỉnh mái, hướng song song với đỉnh
///    
/// 4. Cửa/Cửa sổ:
///    - Điểm snap nên đặt tại khung, không đặt tại cánh cửa/cửa sổ
///    - DoorFrameSide: Đặt tại giữa trụ cửa, hướng vào trong khung
/// </summary>
public class BuildingPiece : MonoBehaviour {
    public BuildingPieceSO pieceData;
    public float health = 100f;
    public List<SnapPoint> snapPoints;
    public bool isGrounded;

    void Awake() {
        snapPoints = new List<SnapPoint>(GetComponentsInChildren<SnapPoint>());
    }

    public void TakeDamage(float amount) {
        health -= amount;
        if (health <= 0) {
            DestroyPiece();
        }
    }

    public void Repair(float amount) {
        health = Mathf.Min(health + amount, 100f);
    }

    public void DestroyPiece() {
        // Chạy hiệu ứng phá hủy tại đây nếu cần
        Destroy(gameObject);
    }
}