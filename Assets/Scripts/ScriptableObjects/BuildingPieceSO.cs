using UnityEngine;

public enum PieceCategory {
    Foundation, // Nền móng
    Wall,       // Tường
    Floor,      // Sàn
    Roof,       // Mái nhà
    Utility     // Tiện ích
}

[CreateAssetMenu(fileName = "BuildingPiece", menuName = "ScriptableObjects/BuildingPieceSO")]
public class BuildingPieceSO : ScriptableObject {
    public string pieceName;              // Tên mảnh ghép
    public Sprite pieceIcon;              // Icon của mảnh ghép
    public PieceCategory category;        // Danh mục của mảnh ghép
    public GameObject prefab;             // Prefab sẽ được instantiate
    [Tooltip("Prefab được sử dụng cho preview. Nếu để trống sẽ dùng prefab chính")]
    public GameObject previewPrefab;      // Prefab dùng cho preview (nếu null sẽ dùng prefab chính)
    public ResourceTypeSO requiredResource; // Tài nguyên yêu cầu để xây mảnh ghép
    public int resourceCost;              // Chi phí tài nguyên cho mảnh ghép

    // Phương thức để lấy prefab phù hợp cho preview
    public GameObject GetPreviewPrefab() {
        return previewPrefab != null ? previewPrefab : prefab;
    }
}
