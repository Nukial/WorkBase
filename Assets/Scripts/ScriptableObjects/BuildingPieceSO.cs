using System.Collections.Generic;
using UnityEngine;

public enum PieceCategory {
    Foundation, // Nền móng
    Wall,       // Tường
    Floor,      // Sàn
    Roof,       // Mái nhà
    Utility     // Tiện ích
}

[System.Serializable]
public class ResourceRequirement {
    public ResourceTypeSO resourceType;   // Loại tài nguyên
    public int amount;                    // Số lượng cần thiết
    
    [Tooltip("Hiển thị tài nguyên này trong UI")]
    public bool showInUI = true;          // Tùy chọn hiển thị trong UI
}

[CreateAssetMenu(fileName = "BuildingPiece", menuName = "ScriptableObjects/BuildingPieceSO")]
public class BuildingPieceSO : ScriptableObject {
    public string pieceName;              // Tên mảnh ghép
    public Sprite pieceIcon;              // Icon của mảnh ghép
    public PieceCategory category;        // Danh mục của mảnh ghép
    public GameObject prefab;             // Prefab sẽ được instantiate
    [Tooltip("Prefab được sử dụng cho preview. Nếu để trống sẽ dùng prefab chính")]
    public GameObject previewPrefab;      // Prefab dùng cho preview (nếu null sẽ dùng prefab chính)
    
    [Header("Yêu cầu tài nguyên")]
    [Tooltip("Danh sách các loại tài nguyên cần thiết để xây dựng")]
    public List<ResourceRequirement> requiredResources = new List<ResourceRequirement>();
    
    // Các biến cũ giữ lại để tương thích ngược (deprecated)
    [HideInInspector] public ResourceTypeSO requiredResource;
    [HideInInspector] public int resourceCost;

    // Phương thức để lấy prefab phù hợp cho preview
    public GameObject GetPreviewPrefab() {
        return previewPrefab != null ? previewPrefab : prefab;
    }
    
    // Phương thức kiểm tra đủ tài nguyên từ một kho lưu trữ
    public bool CheckResourcesAvailable(BaseStorage storage) {
        // Kiểm tra theo danh sách mới
        if (requiredResources.Count > 0) {
            foreach (var requirement in requiredResources) {
                if (!storage.CheckResources(requirement.resourceType, requirement.amount)) {
                    return false;
                }
            }
            return true;
        }
        
        // Kiểm tra tương thích ngược với cách cũ
        return storage.CheckResources(requiredResource, resourceCost);
    }
    
    // Phương thức tiêu thụ tất cả tài nguyên cần thiết
    public bool ConsumeAllResources(BaseStorage storage) {
        // Sử dụng danh sách mới
        if (requiredResources.Count > 0) {
            // Kiểm tra trước khi tiêu thụ
            if (!CheckResourcesAvailable(storage))
                return false;
                
            // Tiêu thụ tất cả các loại tài nguyên
            foreach (var requirement in requiredResources) {
                storage.ConsumeResources(requirement.resourceType, requirement.amount);
            }
            return true;
        }
        
        // Tương thích ngược với cách cũ
        return storage.ConsumeResources(requiredResource, resourceCost);
    }
    
    // Chuyển đổi từ định dạng cũ sang định dạng mới cho tương thích ngược
    public void MigrateFromLegacyFormat() {
        if (requiredResource != null && resourceCost > 0 && requiredResources.Count == 0) {
            requiredResources.Add(new ResourceRequirement {
                resourceType = requiredResource,
                amount = resourceCost
            });
        }
    }
    
    // Gọi hàm này trong OnEnable để tự động chuyển đổi
    private void OnEnable() {
        MigrateFromLegacyFormat();
    }
}
