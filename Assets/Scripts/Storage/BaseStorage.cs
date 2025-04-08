using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ResourceEntry {
    public ResourceTypeSO resource; // Tài nguyên
    public int amount;              // Số lượng
}

public class BaseStorage : MonoBehaviour {
    // Sử dụng List cho Inspector serialize thay vì Dictionary
    public List<ResourceEntry> storedResourceEntries = new List<ResourceEntry>();

    // Phương thức kiểm tra tài nguyên
    public bool CheckResources(ResourceTypeSO resource, int amount) {
        foreach(var entry in storedResourceEntries) {
            if(entry.resource == resource)
                return entry.amount >= amount;
        }
        return false;
    }
    
    // Phương thức tiêu hao tài nguyên
    public bool ConsumeResources(ResourceTypeSO resource, int amount) {
        foreach(var entry in storedResourceEntries) {
            if(entry.resource == resource) {
                if(entry.amount >= amount) {
                    entry.amount -= amount;
                    return true;
                }
                return false;
            }
        }
        return false;
    }
    
    // Phương thức thêm tài nguyên
    public void AddResources(ResourceTypeSO resource, int amount) {
        foreach(var entry in storedResourceEntries) {
            if(entry.resource == resource) {
                entry.amount += amount;
                return;
            }
        }
        // Nếu không có, tạo mới entry
        storedResourceEntries.Add(new ResourceEntry { resource = resource, amount = amount });
    }
}