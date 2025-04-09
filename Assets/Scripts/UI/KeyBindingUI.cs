using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý UI cho việc tùy chỉnh phím tắt
/// </summary>
public class KeyBindingUI : MonoBehaviour
{
    [Tooltip("Prefab cho mỗi mục cấu hình phím tắt")]
    public GameObject keyBindingEntryPrefab;
    
    [Tooltip("Transform cha để chứa các mục phím tắt")]
    public Transform keyBindingContainer;
    
    [Tooltip("Nút đặt lại về mặc định")]
    public Button resetButton;
    
    [Tooltip("Nút lưu thay đổi")]
    public Button saveButton;
    
    [Tooltip("Nút hủy thay đổi")]
    public Button cancelButton;
    
    // Danh sách các action cần hiển thị trong UI
    private List<InputManager.InputAction> actionsToDisplay = new List<InputManager.InputAction>();
    
    // Từ điển lưu các UI element tương ứng với mỗi action
    private Dictionary<InputManager.InputAction, KeyBindingEntry> entries = new Dictionary<InputManager.InputAction, KeyBindingEntry>();
    
    // Phím tắt đang được thay đổi
    private KeyBindingEntry activeRebinding = null;

    private void Start()
    {
        InitializeActionsList();
        CreateUI();
        
        // Đăng ký các callback cho các nút
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetToDefaults);
        
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveChanges);
        
        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelChanges);
    }
    
    // Khởi tạo danh sách các action cần hiển thị
    private void InitializeActionsList()
    {
        // Lưu ý: Thứ tự các action quyết định thứ tự hiển thị trong UI
        actionsToDisplay.Add(InputManager.InputAction.ToggleBuildMode);
        
        actionsToDisplay.Add(InputManager.InputAction.RotateLeft);
        actionsToDisplay.Add(InputManager.InputAction.RotateRight);
        actionsToDisplay.Add(InputManager.InputAction.PlaceObject);
        actionsToDisplay.Add(InputManager.InputAction.ToggleFreePlacement);
        
        actionsToDisplay.Add(InputManager.InputAction.CategoryFoundation);
        actionsToDisplay.Add(InputManager.InputAction.CategoryWall);
        actionsToDisplay.Add(InputManager.InputAction.CategoryFloor);
        actionsToDisplay.Add(InputManager.InputAction.CategoryRoof);
        actionsToDisplay.Add(InputManager.InputAction.CategoryUtility);
        
        actionsToDisplay.Add(InputManager.InputAction.NextItem);
        actionsToDisplay.Add(InputManager.InputAction.PreviousItem);
    }
    
    // Tạo UI cho tất cả các phím tắt
    private void CreateUI()
    {
        if (keyBindingContainer == null || keyBindingEntryPrefab == null)
            return;
        
        // Xóa tất cả các con hiện tại
        foreach (Transform child in keyBindingContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Tạo mới các entry cho mỗi action
        foreach (var action in actionsToDisplay)
        {
            GameObject entryObj = Instantiate(keyBindingEntryPrefab, keyBindingContainer);
            KeyBindingEntry entry = entryObj.GetComponent<KeyBindingEntry>();
            
            if (entry != null)
            {
                entry.Initialize(action, GetActionDisplayName(action));
                entry.OnRequestRebind += StartRebinding;
                entries[action] = entry;
            }
        }
    }
    
    // Bắt đầu quá trình thay đổi phím tắt
    private void StartRebinding(KeyBindingEntry entry, bool isPrimary)
    {
        // Nếu đang thay đổi phím tắt khác, hủy trước
        if (activeRebinding != null)
        {
            activeRebinding.CancelRebinding();
        }
        
        activeRebinding = entry;
        entry.StartRebinding(isPrimary);
    }
    
    // Đặt lại về giá trị mặc định
    private void ResetToDefaults()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.ResetToDefaults();
            RefreshAllBindings();
        }
    }
    
    // Lưu các thay đổi
    private void SaveChanges()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SaveKeyMappings();
        }
        
        // Đóng UI nếu cần
        gameObject.SetActive(false);
    }
    
    // Hủy các thay đổi
    private void CancelChanges()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.LoadKeyMappings();
            RefreshAllBindings();
        }
        
        // Đóng UI nếu cần
        gameObject.SetActive(false);
    }
    
    // Cập nhật hiển thị tất cả các phím tắt
    private void RefreshAllBindings()
    {
        foreach (var entry in entries)
        {
            entry.Value.Refresh();
        }
    }
    
    // Phương thức để lấy tên hiển thị cho mỗi action
    private string GetActionDisplayName(InputManager.InputAction action)
    {
        switch (action)
        {
            case InputManager.InputAction.ToggleBuildMode:
                return "Bật/Tắt Chế Độ Xây Dựng";
            case InputManager.InputAction.RotateLeft:
                return "Xoay Trái";
            case InputManager.InputAction.RotateRight:
                return "Xoay Phải";
            case InputManager.InputAction.PlaceObject:
                return "Đặt Đối Tượng";
            case InputManager.InputAction.ToggleFreePlacement:
                return "Bật/Tắt Chế Độ Đặt Tự Do";
            case InputManager.InputAction.CategoryFoundation:
                return "Danh Mục: Nền Móng";
            case InputManager.InputAction.CategoryWall:
                return "Danh Mục: Tường";
            case InputManager.InputAction.CategoryFloor:
                return "Danh Mục: Sàn";
            case InputManager.InputAction.CategoryRoof:
                return "Danh Mục: Mái Nhà";
            case InputManager.InputAction.CategoryUtility:
                return "Danh Mục: Tiện Ích";
            case InputManager.InputAction.NextItem:
                return "Vật Phẩm Tiếp Theo";
            case InputManager.InputAction.PreviousItem:
                return "Vật Phẩm Trước Đó";
            default:
                return action.ToString();
        }
    }
    
    // Update được gọi mỗi frame
    private void Update()
    {
        // Nếu đang trong quá trình rebinding, kiểm tra phím nhấn
        if (activeRebinding != null && activeRebinding.IsRebinding)
        {
            // Kiểm tra tất cả các phím có thể nhận diện được
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    activeRebinding.CompleteRebinding(key);
                    activeRebinding = null;
                    break;
                }
            }
            
            // Kiểm tra nếu người dùng nhấn Escape để hủy
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                activeRebinding.CancelRebinding();
                activeRebinding = null;
            }
        }
    }
}

/// <summary>
/// Component cho mỗi mục cấu hình phím tắt trong UI
/// </summary>
public class KeyBindingEntry : MonoBehaviour
{
    [Tooltip("Text hiển thị tên hành động")]
    public TextMeshProUGUI actionLabel;
    
    [Tooltip("Button cho phím chính")]
    public Button primaryKeyButton;
    
    [Tooltip("Text hiển thị phím chính")]
    public TextMeshProUGUI primaryKeyText;
    
    [Tooltip("Button cho phím thay thế")]
    public Button alternateKeyButton;
    
    [Tooltip("Text hiển thị phím thay thế")]
    public TextMeshProUGUI alternateKeyText;
    
    [Tooltip("Text hiển thị khi đang chờ nhấn phím")]
    public TextMeshProUGUI waitingForInputText;
    
    // Hành động tương ứng với entry này
    private InputManager.InputAction action;
    
    // Đang thay đổi phím chính hay phím thay thế
    private bool rebindingPrimary;
    
    // Đang trong quá trình thay đổi phím
    private bool isRebinding;
    
    // Event khi bắt đầu thay đổi phím
    public event System.Action<KeyBindingEntry, bool> OnRequestRebind;
    
    // Getter cho trạng thái đang thay đổi phím
    public bool IsRebinding => isRebinding;
    
    // Khởi tạo entry với action và tên hiển thị
    public void Initialize(InputManager.InputAction action, string displayName)
    {
        this.action = action;
        
        if (actionLabel != null)
            actionLabel.text = displayName;
        
        if (primaryKeyButton != null)
            primaryKeyButton.onClick.AddListener(() => RequestRebind(true));
        
        if (alternateKeyButton != null)
            alternateKeyButton.onClick.AddListener(() => RequestRebind(false));
        
        // Ẩn text "đang chờ nhấn phím" ban đầu
        if (waitingForInputText != null)
            waitingForInputText.gameObject.SetActive(false);
        
        Refresh();
    }
    
    // Yêu cầu thay đổi phím
    public void RequestRebind(bool isPrimary)
    {
        OnRequestRebind?.Invoke(this, isPrimary);
    }
    
    // Bắt đầu quá trình thay đổi phím
    public void StartRebinding(bool isPrimary)
    {
        isRebinding = true;
        rebindingPrimary = isPrimary;
        
        // Hiển thị text "đang chờ nhấn phím"
        if (waitingForInputText != null)
        {
            waitingForInputText.gameObject.SetActive(true);
            waitingForInputText.transform.position = (isPrimary ? primaryKeyButton : alternateKeyButton).transform.position;
        }
        
        // Ẩn tạm thời text hiển thị phím
        if (primaryKeyText != null && isPrimary)
            primaryKeyText.gameObject.SetActive(false);
        
        if (alternateKeyText != null && !isPrimary)
            alternateKeyText.gameObject.SetActive(false);
    }
    
    // Hoàn tất quá trình thay đổi phím
    public void CompleteRebinding(KeyCode newKey)
    {
        isRebinding = false;
        
        if (InputManager.Instance != null)
        {
            if (rebindingPrimary)
                InputManager.Instance.ChangeKeyBinding(action, newKey);
            else
                InputManager.Instance.ChangeAlternateKeyBinding(action, newKey);
        }
        
        // Ẩn text "đang chờ nhấn phím"
        if (waitingForInputText != null)
            waitingForInputText.gameObject.SetActive(false);
        
        // Hiển thị lại và cập nhật text hiển thị phím
        Refresh();
    }
    
    // Hủy quá trình thay đổi phím
    public void CancelRebinding()
    {
        isRebinding = false;
        
        // Ẩn text "đang chờ nhấn phím"
        if (waitingForInputText != null)
            waitingForInputText.gameObject.SetActive(false);
        
        // Hiển thị lại text hiển thị phím
        if (primaryKeyText != null && rebindingPrimary)
            primaryKeyText.gameObject.SetActive(true);
        
        if (alternateKeyText != null && !rebindingPrimary)
            alternateKeyText.gameObject.SetActive(true);
    }
    
    // Cập nhật hiển thị phím
    public void Refresh()
    {
        if (InputManager.Instance == null)
            return;
        
        if (primaryKeyText != null)
        {
            primaryKeyText.gameObject.SetActive(true);
            KeyCode key = InputManager.Instance.GetKeyForAction(action);
            primaryKeyText.text = InputManager.GetKeyDisplayName(key);
        }
        
        if (alternateKeyText != null)
        {
            alternateKeyText.gameObject.SetActive(true);
            KeyCode key = InputManager.Instance.GetAlternateKeyForAction(action);
            alternateKeyText.text = InputManager.GetKeyDisplayName(key);
        }
    }
}
