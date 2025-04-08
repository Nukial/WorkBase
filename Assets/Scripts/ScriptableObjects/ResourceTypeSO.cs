using UnityEngine;

[CreateAssetMenu(fileName = "ResourceType", menuName = "ScriptableObjects/ResourceTypeSO")]
public class ResourceTypeSO : ScriptableObject {
    public string resourceName; // Tên của tài nguyên
    public Sprite resourceIcon; // Icon đại diện cho tài nguyên
}
