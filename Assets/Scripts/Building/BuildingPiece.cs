using System.Collections.Generic;
using UnityEngine;

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