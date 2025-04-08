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
    public ResourceTypeSO requiredResource; // Tài nguyên yêu cầu để xây mảnh ghép
    public int resourceCost;              // Chi phí tài nguyên cho mảnh ghép
}
