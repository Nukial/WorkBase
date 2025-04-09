using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// Quản lý tất cả đầu vào người dùng và cho phép tùy chỉnh phím tắt
/// </summary>
public class InputManager : MonoBehaviour
{
    // Singleton để truy cập từ bất kỳ đâu
    public static InputManager Instance { get; private set; }

    // Định nghĩa các hành động có thể được gán phím
    public enum InputAction
    {
        // Hành động chung
        ToggleBuildMode,
        
        // Chế độ xây dựng
        RotateLeft,
        RotateRight,
        PlaceObject,
        ToggleFreePlacement,
        
        // Danh mục xây dựng
        CategoryFoundation,
        CategoryWall,
        CategoryFloor, 
        CategoryRoof,
        CategoryUtility,
        
        // Điều hướng danh mục
        NextItem,
        PreviousItem
    }

    // Cấu trúc lưu trữ ánh xạ phím
    [Serializable]
    public class KeyMapping
    {
        public InputAction action;
        public KeyCode primaryKey;
        public KeyCode alternateKey = KeyCode.None;
    }

    // Danh sách ánh xạ phím và giá trị mặc định
    [SerializeField]
    private List<KeyMapping> keyMappings = new List<KeyMapping>();
    
    // Dictionary để tra cứu nhanh
    private Dictionary<InputAction, KeyMapping> actionMappings = new Dictionary<InputAction, KeyMapping>();

    // Đường dẫn lưu cấu hình
    private string configPath;

    private void Awake()
    {
        // Thiết lập singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Xác định đường dẫn lưu file cấu hình
            configPath = Path.Combine(Application.persistentDataPath, "keybindings.json");
            
            // Khởi tạo các giá trị mặc định nếu chưa có trong danh sách
            InitializeDefaultKeyMappings();
            
            // Tạo dictionary để tra cứu nhanh
            foreach (var mapping in keyMappings)
            {
                actionMappings[mapping.action] = mapping;
            }
            
            // Tải cấu hình phím nếu có
            LoadKeyMappings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Khởi tạo các giá trị mặc định
    private void InitializeDefaultKeyMappings()
    {
        // Chỉ thêm các giá trị mặc định nếu danh sách trống
        if (keyMappings.Count == 0)
        {
            // Hành động chung
            keyMappings.Add(new KeyMapping { action = InputAction.ToggleBuildMode, primaryKey = KeyCode.B });
            
            // Chế độ xây dựng
            keyMappings.Add(new KeyMapping { action = InputAction.RotateLeft, primaryKey = KeyCode.Q });
            keyMappings.Add(new KeyMapping { action = InputAction.RotateRight, primaryKey = KeyCode.E });
            keyMappings.Add(new KeyMapping { action = InputAction.PlaceObject, primaryKey = KeyCode.Mouse0 });
            keyMappings.Add(new KeyMapping { action = InputAction.ToggleFreePlacement, primaryKey = KeyCode.LeftControl });
            
            // Danh mục xây dựng
            keyMappings.Add(new KeyMapping { action = InputAction.CategoryFoundation, primaryKey = KeyCode.Alpha1 });
            keyMappings.Add(new KeyMapping { action = InputAction.CategoryWall, primaryKey = KeyCode.Alpha2 });
            keyMappings.Add(new KeyMapping { action = InputAction.CategoryFloor, primaryKey = KeyCode.Alpha3 });
            keyMappings.Add(new KeyMapping { action = InputAction.CategoryRoof, primaryKey = KeyCode.Alpha4 });
            keyMappings.Add(new KeyMapping { action = InputAction.CategoryUtility, primaryKey = KeyCode.Alpha5 });
            
            // Điều hướng danh mục
            keyMappings.Add(new KeyMapping { action = InputAction.NextItem, primaryKey = KeyCode.RightArrow, alternateKey = KeyCode.Period });
            keyMappings.Add(new KeyMapping { action = InputAction.PreviousItem, primaryKey = KeyCode.LeftArrow, alternateKey = KeyCode.Comma });
        }
    }

    /// <summary>
    /// Kiểm tra xem một hành động đã được thực hiện (phím được nhấn) hay chưa
    /// </summary>
    /// <param name="action">Hành động cần kiểm tra</param>
    /// <returns>True nếu phím tương ứng với hành động được nhấn</returns>
    public bool GetButtonDown(InputAction action)
    {
        if (actionMappings.TryGetValue(action, out KeyMapping mapping))
        {
            return Input.GetKeyDown(mapping.primaryKey) || 
                   (mapping.alternateKey != KeyCode.None && Input.GetKeyDown(mapping.alternateKey));
        }
        return false;
    }

    /// <summary>
    /// Kiểm tra xem một hành động có đang được thực hiện (phím đang được giữ) hay không
    /// </summary>
    /// <param name="action">Hành động cần kiểm tra</param>
    /// <returns>True nếu phím tương ứng với hành động đang được giữ</returns>
    public bool GetButton(InputAction action)
    {
        if (actionMappings.TryGetValue(action, out KeyMapping mapping))
        {
            return Input.GetKey(mapping.primaryKey) || 
                   (mapping.alternateKey != KeyCode.None && Input.GetKey(mapping.alternateKey));
        }
        return false;
    }

    /// <summary>
    /// Thay đổi phím chính cho một hành động
    /// </summary>
    /// <param name="action">Hành động cần thay đổi phím</param>
    /// <param name="newKey">Phím mới</param>
    public void ChangeKeyBinding(InputAction action, KeyCode newKey)
    {
        if (actionMappings.TryGetValue(action, out KeyMapping mapping))
        {
            mapping.primaryKey = newKey;
            SaveKeyMappings();
        }
    }

    /// <summary>
    /// Thay đổi phím thay thế cho một hành động
    /// </summary>
    /// <param name="action">Hành động cần thay đổi phím</param>
    /// <param name="newKey">Phím thay thế mới</param>
    public void ChangeAlternateKeyBinding(InputAction action, KeyCode newKey)
    {
        if (actionMappings.TryGetValue(action, out KeyMapping mapping))
        {
            mapping.alternateKey = newKey;
            SaveKeyMappings();
        }
    }

    /// <summary>
    /// Lấy phím hiện tại được gán cho một hành động
    /// </summary>
    /// <param name="action">Hành động cần lấy phím</param>
    /// <returns>Phím đang được gán cho hành động</returns>
    public KeyCode GetKeyForAction(InputAction action)
    {
        if (actionMappings.TryGetValue(action, out KeyMapping mapping))
        {
            return mapping.primaryKey;
        }
        return KeyCode.None;
    }

    /// <summary>
    /// Lấy phím thay thế hiện tại được gán cho một hành động
    /// </summary>
    /// <param name="action">Hành động cần lấy phím thay thế</param>
    /// <returns>Phím thay thế đang được gán cho hành động</returns>
    public KeyCode GetAlternateKeyForAction(InputAction action)
    {
        if (actionMappings.TryGetValue(action, out KeyMapping mapping))
        {
            return mapping.alternateKey;
        }
        return KeyCode.None;
    }

    /// <summary>
    /// Lưu cấu hình phím vào file
    /// </summary>
    public void SaveKeyMappings()
    {
        try
        {
            string json = JsonUtility.ToJson(new KeyMappingList { mappings = keyMappings }, true);
            File.WriteAllText(configPath, json);
            Debug.Log("Đã lưu cấu hình phím tắt thành công.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Lỗi khi lưu cấu hình phím tắt: {e.Message}");
        }
    }

    /// <summary>
    /// Tải cấu hình phím từ file
    /// </summary>
    public void LoadKeyMappings()
    {
        try
        {
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                KeyMappingList loadedMappings = JsonUtility.FromJson<KeyMappingList>(json);
                
                if (loadedMappings != null && loadedMappings.mappings != null)
                {
                    keyMappings = loadedMappings.mappings;
                    
                    // Cập nhật dictionary
                    actionMappings.Clear();
                    foreach (var mapping in keyMappings)
                    {
                        actionMappings[mapping.action] = mapping;
                    }
                    
                    Debug.Log("Đã tải cấu hình phím tắt thành công.");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Lỗi khi tải cấu hình phím tắt: {e.Message}");
        }
    }

    /// <summary>
    /// Khôi phục các phím tắt về giá trị mặc định
    /// </summary>
    public void ResetToDefaults()
    {
        keyMappings.Clear();
        actionMappings.Clear();
        InitializeDefaultKeyMappings();
        
        // Cập nhật dictionary
        foreach (var mapping in keyMappings)
        {
            actionMappings[mapping.action] = mapping;
        }
        
        SaveKeyMappings();
        Debug.Log("Đã khôi phục cấu hình phím tắt về mặc định.");
    }

    // Lớp giúp serialization danh sách KeyMapping
    [Serializable]
    private class KeyMappingList
    {
        public List<KeyMapping> mappings;
    }

    /// <summary>
    /// Lấy tên hiển thị cho KeyCode
    /// </summary>
    public static string GetKeyDisplayName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Mouse0:
                return "Chuột Trái";
            case KeyCode.Mouse1:
                return "Chuột Phải";
            case KeyCode.Mouse2:
                return "Chuột Giữa";
            case KeyCode.LeftControl:
                return "Ctrl Trái";
            case KeyCode.RightControl:
                return "Ctrl Phải";
            case KeyCode.LeftShift:
                return "Shift Trái";
            case KeyCode.RightShift:
                return "Shift Phải";
            case KeyCode.LeftAlt:
                return "Alt Trái";
            case KeyCode.RightAlt:
                return "Alt Phải";
            case KeyCode.Alpha0:
            case KeyCode.Alpha1:
            case KeyCode.Alpha2:
            case KeyCode.Alpha3:
            case KeyCode.Alpha4:
            case KeyCode.Alpha5:
            case KeyCode.Alpha6:
            case KeyCode.Alpha7:
            case KeyCode.Alpha8:
            case KeyCode.Alpha9:
                return key.ToString().Replace("Alpha", "");
            case KeyCode.None:
                return "Không có";
            default:
                return key.ToString();
        }
    }
}
