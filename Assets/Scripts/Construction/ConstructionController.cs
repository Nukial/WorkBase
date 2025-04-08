using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Lớp đại diện cho mỗi loại khối xây dựng
[System.Serializable]
public class BuildingPiece
{
    public string pieceName;
    public GameObject realPrefab;
    public GameObject ghostPrefab;
}

// Struct đại diện cho một kết nối điểm snap tiềm năng
public struct PotentialSnap
{
    public Transform ghostSnapPoint;
    public Transform existingSnapPoint;
    public float distance;
    public Vector3 targetPosition;
    public Quaternion targetRotation;

    public PotentialSnap(Transform ghostSnapPoint, Transform existingSnapPoint, float distance, Vector3 targetPosition, Quaternion targetRotation)
    {
        this.ghostSnapPoint = ghostSnapPoint;
        this.existingSnapPoint = existingSnapPoint;
        this.distance = distance;
        this.targetPosition = targetPosition;
        this.targetRotation = targetRotation;
    }
}

public class ConstructionController : MonoBehaviour
{
    // Nhiệm vụ 1: Chế độ xây dựng & xử lý đầu vào
    public bool isInBuildMode = false;
    private bool wasInBuildMode = false;
    
    // Nhiệm vụ 2: Các mảnh xây dựng & cài đặt
    public List<BuildingPiece> buildablePieces;
    private int currentPieceIndex = 0;
    private BuildingPiece currentSelectedPiece;
    private bool hasBuildablePieces = false;
    
    public LayerMask placementLayerMask;
    public float maxBuildDistance = 10f;
    private GameObject currentGhostInstance;

    // Cài đặt màu sắc cho vị trí hợp lệ/không hợp lệ
    public Color validPlacementColor = new Color(0f, 1f, 0f, 0.5f); // Màu xanh lá trong suốt
    public Color invalidPlacementColor = new Color(1f, 0f, 0f, 0.5f); // Màu đỏ trong suốt

    // Cài đặt phát hiện va chạm
    public LayerMask collisionCheckMask; // Đặt để bao gồm các Buildings, Player, v.v.
    private bool isValidPlacement = true;
    private Renderer[] ghostRenderers;

    // Cài đặt layer cho việc đặt công trình
    public int buildingLayer = 8; // Mặc định là layer 8, thường là "Buildings" trong Unity

    // Cài đặt bắt dính
    public float snapSearchRadius = 1.5f;
    public KeyCode cycleSnapKey = KeyCode.T;
    [Tooltip("Phải bao gồm layer Buildings nơi các công trình đã đặt sẽ được đặt. Thường là layer 8.")]
    public LayerMask buildingSnapCheckMask = 256; // Mặc định là layer 8 (1 << 8 = 256)
    
    // Tùy chọn gỡ lỗi
    public bool showDebugInfo = true;
    public bool visualizeSnapRadius = true;

    // Trạng thái bắt dính
    private List<PotentialSnap> potentialSnaps = new List<PotentialSnap>();
    private int currentSnapIndex = -1;
    private bool isSnapping = false;
    private SnapPoint[] ghostSnapPoints;

    // Biến để kiểm soát nhấp nháy
    private float snapSearchCooldown = 0.1f; // Thời gian giữa các lần tìm kiếm điểm snap
    private float lastSnapSearchTime = 0f;
    private bool lockPlacementState = false;
    private float placementStateLockTime = 0.3f;
    private float lastPlacementStateChangeTime = 0f;

    // Biến theo dõi bổ sung
    private bool debugLogSnapState = true; // Bật để theo dõi trạng thái snap
    
    void Start()
    {
        // Khởi tạo giá trị
        isInBuildMode = false;
        wasInBuildMode = false;
        
        // Kiểm tra xem chúng ta có các mảnh xây dựng được cấu hình không
        hasBuildablePieces = buildablePieces != null && buildablePieces.Count > 0;
        
        if (hasBuildablePieces)
        {
            // Chọn mảnh xây dựng đầu tiên
            SelectBuildingPiece(currentPieceIndex);
        }
        else
        {
            Debug.LogError("Không có mảnh xây dựng nào được gán cho ConstructionController! Thêm các mảnh trong Inspector.");
        }
        
        // Đảm bảo ghost được hủy khi bắt đầu
        if (currentGhostInstance != null)
        {
            Destroy(currentGhostInstance);
            currentGhostInstance = null;
        }

        // Xác minh layer mask được đặt đúng và sửa nếu cần
        if (buildingSnapCheckMask.value == 0)
        {
            Debug.LogWarning("Building snap check mask chưa được đặt! Tự động đặt để bao gồm layer Buildings.");
            // Tự động đặt thành building layer
            buildingSnapCheckMask = 1 << buildingLayer;
            
            if (showDebugInfo)
            {
                Debug.Log($"Tự động đặt buildingSnapCheckMask thành layer {buildingLayer} ({LayerMask.LayerToName(buildingLayer)})");
            }
        }
        
        // Xác minh building layer hợp lệ
        if (buildingLayer < 0 || buildingLayer > 31)
        {
            Debug.LogError($"Building layer không hợp lệ: {buildingLayer}. Layer phải từ 0-31.");
        }
        else if (showDebugInfo)
        {
            Debug.Log($"Building layer được đặt thành: {LayerMask.LayerToName(buildingLayer)} (index: {buildingLayer})");
            Debug.Log($"Building snap check mask: {buildingSnapCheckMask.value} (nhị phân: {System.Convert.ToString(buildingSnapCheckMask.value, 2)})");
        }
    }
    
    private void SelectBuildingPiece(int index)
    {
        // Xác thực chỉ mục
        if (!hasBuildablePieces || index < 0 || index >= buildablePieces.Count) return;
        
        // Cập nhật lựa chọn hiện tại
        currentPieceIndex = index;
        currentSelectedPiece = buildablePieces[currentPieceIndex];
        
        // Dọn dẹp phiên bản ghost trước đó
        if (currentGhostInstance != null)
        {
            Destroy(currentGhostInstance);
            currentGhostInstance = null;
        }
        
        // Xác thực ghost prefab
        if (currentSelectedPiece.ghostPrefab == null)
        {
            Debug.LogError($"Ghost prefab cho mảnh xây dựng '{currentSelectedPiece.pieceName}' chưa được gán!");
            return;
        }
        
        // Tạo phiên bản ghost mới
        currentGhostInstance = Instantiate(currentSelectedPiece.ghostPrefab);
        
        // Vô hiệu hóa tất cả collider trên ghost
        Collider[] colliders = currentGhostInstance.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
        
        // Lưu cache renderer để thay đổi vật liệu
        ghostRenderers = currentGhostInstance.GetComponentsInChildren<Renderer>();
        
        // Lưu cache điểm snap cho ghost
        ghostSnapPoints = currentGhostInstance.GetComponentsInChildren<SnapPoint>();
        Debug.Log($"Tìm thấy {ghostSnapPoints.Length} điểm snap trên ghost {currentSelectedPiece.pieceName}");
        
        // Khởi tạo vật liệu ghost cho độ trong suốt phù hợp
        InitializeGhostMaterials();
        
        // Ẩn ghost ban đầu
        currentGhostInstance.SetActive(false);
        
        Debug.Log($"Đã chọn mảnh xây dựng: {currentSelectedPiece.pieceName}");
    }
    
    // Khởi tạo vật liệu ghost cho độ trong suốt
    private void InitializeGhostMaterials()
    {
        if (ghostRenderers == null || ghostRenderers.Length == 0) return;
        
        foreach (Renderer renderer in ghostRenderers)
        {
            // Tạo phiên bản vật liệu mới để tránh sửa đổi vật liệu được chia sẻ
            Material[] newMaterials = new Material[renderer.sharedMaterials.Length];
            
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                // Nhân bản vật liệu để tạo một phiên bản duy nhất
                newMaterials[i] = new Material(renderer.sharedMaterials[i]);
                
                // Thiết lập cài đặt độ trong suốt
                // Kiểm tra xem đang sử dụng shader tiêu chuẩn
                if (newMaterials[i].HasProperty("_Mode"))
                {
                    newMaterials[i].SetFloat("_Mode", 3); // Chế độ trong suốt
                    newMaterials[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    newMaterials[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    newMaterials[i].SetInt("_ZWrite", 0);
                    newMaterials[i].DisableKeyword("_ALPHATEST_ON");
                    newMaterials[i].EnableKeyword("_ALPHABLEND_ON");
                    newMaterials[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    newMaterials[i].renderQueue = 3000;
                }
            }
            
            // Gán vật liệu mới cho renderer
            renderer.materials = newMaterials;
        }
        
        // Áp dụng màu ban đầu
        UpdateGhostVisuals();
    }
    
    void Update()
    {
        // Thoát sớm nếu không có mảnh được cấu hình
        if (!hasBuildablePieces) return;
        
        // Bật/tắt chế độ xây dựng
        if (Input.GetKeyDown(KeyCode.B))
        {
            isInBuildMode = !isInBuildMode;
        }
        
        // Nhiệm vụ 4: Quản lý vòng đời đối tượng Ghost
        // Xử lý khi vào chế độ xây dựng
        if (isInBuildMode && !wasInBuildMode)
        {
            if (currentSelectedPiece == null)
            {
                SelectBuildingPiece(0);
            }
            else if (currentGhostInstance == null)
            {
                SelectBuildingPiece(currentPieceIndex);
            }
            
            if (currentGhostInstance != null)
            {
                currentGhostInstance.SetActive(false);
            }
        }
        
        // Xử lý khi thoát chế độ xây dựng
        if (!isInBuildMode && wasInBuildMode)
        {
            if (currentGhostInstance != null)
            {
                Destroy(currentGhostInstance);
                currentGhostInstance = null;
            }
        }
        
        wasInBuildMode = isInBuildMode;
        
        // Nhiệm vụ 3: Triển khai Raycasting
        // Thoát sớm nếu không ở chế độ xây dựng
        if (!isInBuildMode) return;
        
        // Xử lý đầu vào chọn mảnh
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectBuildingPiece(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) && buildablePieces.Count > 1) SelectBuildingPiece(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) && buildablePieces.Count > 2) SelectBuildingPiece(2);
        if (Input.GetKeyDown(KeyCode.Alpha4) && buildablePieces.Count > 3) SelectBuildingPiece(3);
        if (Input.GetKeyDown(KeyCode.Alpha5) && buildablePieces.Count > 4) SelectBuildingPiece(4);
        
        // Chọn bằng bánh xe chuột
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            int nextIndex = (currentPieceIndex + 1) % buildablePieces.Count;
            SelectBuildingPiece(nextIndex);
        }
        else if (scroll < 0f)
        {
            int prevIndex = (currentPieceIndex - 1 + buildablePieces.Count) % buildablePieces.Count;
            SelectBuildingPiece(prevIndex);
        }
        
        bool placeInputPressed = Input.GetMouseButtonDown(0);
        
        // Xử lý xoay
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (currentGhostInstance != null)
            {
                // Xoay ghost 90 độ quanh trục Y
                currentGhostInstance.transform.Rotate(0, 90, 0);
            }
        }
        
        // Lấy camera và tạo tia
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        bool raycastHitSomething = Physics.Raycast(ray, out hitInfo, maxBuildDistance, placementLayerMask);

        // Gọi FindPotentialSnaps mỗi frame để đảm bảo cập nhật liên tục (có thể bỏ cooldown nếu cần)
        FindPotentialSnaps();
        HandleSnapCycling();

        // --- Nhiệm vụ 5: Cập nhật vị trí & hiển thị Ghost (đã sửa lỗi nhấp nháy) ---
        if (currentGhostInstance == null) return;

        bool positionedBySnap = false; // Đánh dấu đã định vị bằng snap nếu có
        if (isSnapping && currentSnapIndex != -1 && currentSnapIndex < potentialSnaps.Count)
        {
            // Dùng điểm snap đã chọn
            PotentialSnap activeSnap = potentialSnaps[currentSnapIndex];
            currentGhostInstance.transform.position = activeSnap.targetPosition;
            currentGhostInstance.transform.rotation = activeSnap.targetRotation;
            currentGhostInstance.SetActive(true);
            positionedBySnap = true;
            
            if (showDebugInfo && debugLogSnapState)
                Debug.Log($"Định vị bằng snap: Index {currentSnapIndex}, Pos={activeSnap.targetPosition}");
        }

        if (!positionedBySnap && raycastHitSomething)
        {
            // Nếu không có snap hợp lệ, dùng tia raycast giữ ghost ở vị trí hit
            currentGhostInstance.transform.position = hitInfo.point;
            // Giữ lại góc xoay hiện tại để không làm mất thiết lập người dùng
            currentGhostInstance.SetActive(true);
            
            if (showDebugInfo && debugLogSnapState)
                Debug.Log($"Định vị bằng raycast: Pos={hitInfo.point}, isSnapping={isSnapping}");
        }
        else if (!positionedBySnap && !raycastHitSomething)
        {
            // Nếu không có snap và không có raycast hit, ẩn ghost
            currentGhostInstance.SetActive(false);
        }

        // Kiểm tra va chạm và cập nhật hiển thị nếu ghost đang hoạt động
        if (currentGhostInstance.activeSelf)
        {
            // Chỉ thay đổi trạng thái nếu đã qua thời gian khóa hoặc mới bắt đầu đặt
            if (!lockPlacementState || Time.time - lastPlacementStateChangeTime >= placementStateLockTime)
            {
                bool newPlacementState = !CheckForCollisions();
                
                // Nếu trạng thái thay đổi, cập nhật thời gian thay đổi
                if (isValidPlacement != newPlacementState)
                {
                    isValidPlacement = newPlacementState;
                    lastPlacementStateChangeTime = Time.time;
                    lockPlacementState = true;
                    
                    // Cập nhật hiển thị khi trạng thái thay đổi
                    UpdateGhostVisuals();
                }
            }
        }
        else
        {
            isValidPlacement = false;
            lockPlacementState = false;
        }
        
        // Nhiệm vụ 6: Logic đặt khối cơ bản
        if (placeInputPressed && currentGhostInstance.activeSelf && isValidPlacement && currentSelectedPiece != null)
        {
            // Đảm bảo chúng ta có real prefab hợp lệ
            if (currentSelectedPiece.realPrefab != null)
            {
                // Tạo công trình thực tế với góc xoay của ghost
                GameObject newBuilding = Instantiate(currentSelectedPiece.realPrefab, 
                                                     currentGhostInstance.transform.position, 
                                                     currentGhostInstance.transform.rotation);
                
                // Đặt layer chính xác cho công trình và tất cả con của nó
                SetLayerRecursively(newBuilding, buildingLayer);

                if (showDebugInfo)
                {
                    Debug.Log($"Đã đặt công trình: {currentSelectedPiece.pieceName} với layer {buildingLayer} ({LayerMask.LayerToName(buildingLayer)})");
                    // Xác minh layer của công trình đã được đặt đúng
                    Debug.Log($"Building layer sau khi đặt: {newBuilding.layer} ({LayerMask.LayerToName(newBuilding.layer)})");
                    // Kiểm tra xem layer này có nằm trong buildingSnapCheckMask không
                    bool layerInMask = ((1 << buildingLayer) & buildingSnapCheckMask.value) != 0;
                    Debug.Log($"Layer của công trình có trong snap check mask không? {layerInMask}");
                }
            }
            else
            {
                Debug.LogError($"Real prefab cho mảnh xây dựng '{currentSelectedPiece.pieceName}' chưa được gán!");
            }
        }
    }
    
    // Tìm điểm snap tiềm năng gần đối tượng ghost hiện tại
    private void FindPotentialSnaps()
    {
        // Lưu giá trị trạng thái trước khi thay đổi để debug
        bool wasSnapping = isSnapping;
        int oldSnapIndex = currentSnapIndex;
        
        potentialSnaps.Clear();
        
        if (currentGhostInstance == null || ghostSnapPoints == null || ghostSnapPoints.Length == 0) 
        {
            if (showDebugInfo) Debug.Log("Không có điểm snap ghost nào khả dụng");
            return;
        }
        
        // Xác minh mask của chúng ta đã được đặt
        if (buildingSnapCheckMask.value == 0)
        {
            Debug.LogError("Building snap check mask chưa được đặt! Vui lòng đặt để bao gồm layer Buildings.");
            return;
        }
        
        if (showDebugInfo && visualizeSnapRadius)
        {
            // Hiển thị bán kính tìm kiếm snap
            Debug.DrawLine(currentGhostInstance.transform.position, 
                          currentGhostInstance.transform.position + Vector3.up * snapSearchRadius, 
                          Color.yellow, 0.1f);
            // Tạo một quả cầu debug để hiển thị bán kính tìm kiếm
            DebugDrawSphere(currentGhostInstance.transform.position, snapSearchRadius, Color.yellow, 0.1f);
        }
        
        // Tìm các đối tượng gần đó có thể có điểm snap
        Collider[] nearbyColliders = Physics.OverlapSphere(currentGhostInstance.transform.position, snapSearchRadius, buildingSnapCheckMask);
        
        if (showDebugInfo)
        {
            Debug.Log($"Tìm thấy {nearbyColliders.Length} collider gần đó để kiểm tra snap (Mask: {buildingSnapCheckMask.value})");
            // Hiển thị các layer nào được bao gồm trong mask
            for (int i = 0; i < 32; i++)
            {
                if (((1 << i) & buildingSnapCheckMask.value) != 0)
                {
                    Debug.Log($"Mask bao gồm layer {i}: {LayerMask.LayerToName(i)}");
                }
            }
        }
        
        foreach (Collider nearbyCollider in nearbyColliders)
        {
            // Bỏ qua nếu collider thuộc về đối tượng ghost
            if (nearbyCollider.transform.root == currentGhostInstance.transform.root) continue;
            
            Debug.Log($"Kiểm tra collider: {nearbyCollider.name}");
            
            // Lấy tất cả các điểm snap trên đối tượng gần đó
            SnapPoint[] existingSnapPoints = nearbyCollider.GetComponentsInChildren<SnapPoint>();
            
            foreach (SnapPoint esPoint in existingSnapPoints)
            {
                Debug.Log($"-- Tìm thấy ES Point: {esPoint.name} ({esPoint.pointType})");
                
                foreach (SnapPoint gsPoint in ghostSnapPoints)
                {
                    Debug.Log($"---- So sánh với GS Point: {gsPoint.name} ({gsPoint.pointType})");
                    
                    // Kiểm tra tương thích cơ bản
                    if (gsPoint.pointType != esPoint.pointType) 
                    {
                        Debug.Log("---- Loại không tương thích, bỏ qua");
                        continue;
                    }
                    
                    // Đối với các connector, kiểm tra rằng chúng đang đối mặt với hướng ngược lại
                    if (gsPoint.pointType == SnapType.Connector)
                    {
                        float alignment = Vector3.Dot(gsPoint.transform.forward, esPoint.transform.forward);
                        Debug.Log($"---- Độ căn chỉnh connector: {alignment} (nên gần -1)");
                        if (alignment > -0.7f)
                        {
                            Debug.Log("---- Connector không được căn chỉnh đúng, bỏ qua");
                            continue;
                        }
                    }
                    
                    // Kiểm tra tương thích nhóm bổ sung
                    if (gsPoint.group != SnapGroup.Any && esPoint.group != SnapGroup.Any && gsPoint.group != esPoint.group)
                    {
                        Debug.Log("---- Nhóm không tương thích, bỏ qua");
                        continue;
                    }
                    
                    // Kiểm tra khoảng cách
                    float dist = Vector3.Distance(gsPoint.transform.position, esPoint.transform.position);
                    if (dist < snapSearchRadius)
                    {
                        Debug.Log($"---- Cặp tương thích được tìm thấy! Khoảng cách: {dist}");
                        
                        // Tính toán vị trí và góc xoay mục tiêu cho ghost
                        Vector3 targetPos;
                        Quaternion targetRot;
                        
                        // Tính toán khác nhau dựa trên loại điểm snap
                        if (gsPoint.pointType == SnapType.Connector)
                        {
                            // Tính toán góc xoay tốt hơn cho connector
                            Quaternion targetWorldOrientation = Quaternion.LookRotation(-esPoint.transform.forward, esPoint.transform.up);
                            Quaternion ghostSnapLocalRot = Quaternion.Inverse(currentGhostInstance.transform.rotation) * gsPoint.transform.rotation;
                            targetRot = targetWorldOrientation * Quaternion.Inverse(ghostSnapLocalRot);
                            Vector3 ghostSnapLocalPos = gsPoint.transform.localPosition;
                            targetPos = esPoint.transform.position - (targetRot * ghostSnapLocalPos);
                        }
                        else // Loại Surface
                        {
                            targetRot = Quaternion.FromToRotation(gsPoint.transform.up, esPoint.transform.up) * 
                                      currentGhostInstance.transform.rotation;
                            Vector3 ghostSnapLocalPos = gsPoint.transform.localPosition;
                            targetPos = esPoint.transform.position - (targetRot * ghostSnapLocalPos) + esPoint.transform.up * 0.01f;
                        }
                        
                        Debug.Log($"---- Đã tính toán Pos: {targetPos}, Rot (Euler): {targetRot.eulerAngles}");
                        
                        // Thêm điểm snap tiềm năng này vào danh sách
                        potentialSnaps.Add(new PotentialSnap(
                            gsPoint.transform,
                            esPoint.transform,
                            dist,
                            targetPos,
                            targetRot
                        ));
                    }
                }
            }
        }
        
        // Sắp xếp các điểm snap tiềm năng theo khoảng cách
        potentialSnaps = potentialSnaps.OrderBy(snap => snap.distance).ToList();
        
        // Đơn giản hóa logic cập nhật trạng thái snap
        if (potentialSnaps.Count > 0)
        {
            // Có snap khả thi
            if (!isSnapping) // Nếu trước đó không snap, giờ bắt đầu snap vào điểm gần nhất
            {
                currentSnapIndex = 0;
                isSnapping = true;
                if (showDebugInfo && debugLogSnapState) 
                    Debug.Log($"Bắt đầu snap tại index 0 (Gần nhất), snap count={potentialSnaps.Count}");
            }
            else // Nếu trước đó đang snap
            {
                // Đảm bảo index hiện tại còn hợp lệ trong danh sách mới
                if (currentSnapIndex >= potentialSnaps.Count)
                {
                    currentSnapIndex = 0; // Quay về điểm gần nhất nếu index cũ không còn
                    if (showDebugInfo && debugLogSnapState) 
                        Debug.Log($"Index snap cũ không hợp lệ, reset về 0");
                }
                // Giữ nguyên isSnapping = true
            }
        }
        else
        {
            // Không tìm thấy snap nào
            currentSnapIndex = -1;
            isSnapping = false;
            
            if (showDebugInfo && debugLogSnapState && wasSnapping) 
                Debug.Log("Không tìm thấy điểm snap, dừng snap");
        }
        
        // Ghi log thay đổi trạng thái để debug
        if (showDebugInfo && debugLogSnapState && (wasSnapping != isSnapping || oldSnapIndex != currentSnapIndex))
        {
            Debug.Log($"Trạng thái snap thay đổi: isSnapping {wasSnapping}->{isSnapping}, index {oldSnapIndex}->{currentSnapIndex}, count={potentialSnaps.Count}");
        }
    }
    
    // Xử lý việc chuyển đổi qua các điểm snap có sẵn
    private void HandleSnapCycling()
    {
        if (Input.GetKeyDown(cycleSnapKey) && potentialSnaps.Count > 0)
        {
            // Chuyển sang điểm snap tiếp theo
            currentSnapIndex++;
            
            // Quay lại điểm snap đầu tiên nếu đã đi qua hết danh sách
            if (currentSnapIndex >= potentialSnaps.Count)
            {
                currentSnapIndex = 0; // Reset về điểm đầu tiên thay vì -1
            }
            
            isSnapping = true; // Giữ trạng thái snap luôn true khi có điểm snap
            
            if (showDebugInfo && debugLogSnapState)
                Debug.Log($"Đã chuyển sang điểm snap {currentSnapIndex + 1}/{potentialSnaps.Count}");
        }
    }
    
    // Kiểm tra xem có va chạm nào tại vị trí ghost không
    private bool CheckForCollisions()
    {
        if (currentGhostInstance == null) return false;
        
        // Cố gắng lấy tất cả các collider liên quan từ prefab ghost
        Collider[] ghostColliders = currentGhostInstance.GetComponentsInChildren<Collider>();
        if (ghostColliders == null || ghostColliders.Length == 0)
        {
            // Không tìm thấy collider - sử dụng kích thước hộp mặc định
            Vector3 size = new Vector3(1f, 1f, 1f);
            return Physics.CheckBox(
                currentGhostInstance.transform.position,
                size / 2f, // Nửa kích thước
                currentGhostInstance.transform.rotation,
                collisionCheckMask
            );
        }
        
        // Kiểm tra va chạm bằng cách sử dụng mỗi bounds của collider
        foreach (Collider ghostCollider in ghostColliders)
        {
            if (ghostCollider is BoxCollider boxCollider)
            {
                // Tính toán trung tâm và kích thước trong không gian thế giới dựa trên transform của ghost
                Vector3 center = currentGhostInstance.transform.position + 
                    currentGhostInstance.transform.rotation * Vector3.Scale(boxCollider.center, currentGhostInstance.transform.lossyScale);
                
                Vector3 size = Vector3.Scale(boxCollider.size, currentGhostInstance.transform.lossyScale);
                
                // Thực hiện kiểm tra va chạm
                if (Physics.CheckBox(
                    center,
                    size / 2f,
                    currentGhostInstance.transform.rotation,
                    collisionCheckMask))
                {
                    return true; // Phát hiện va chạm
                }
            }
            else if (ghostCollider is SphereCollider sphereCollider)
            {
                // Xử lý collider hình cầu
                Vector3 center = currentGhostInstance.transform.position + 
                    currentGhostInstance.transform.rotation * Vector3.Scale(sphereCollider.center, currentGhostInstance.transform.lossyScale);
                
                float radius = sphereCollider.radius * Mathf.Max(
                    currentGhostInstance.transform.lossyScale.x,
                    Mathf.Max(currentGhostInstance.transform.lossyScale.y, currentGhostInstance.transform.lossyScale.z)
                );
                
                if (Physics.CheckSphere(center, radius, collisionCheckMask))
                {
                    return true; // Phát hiện va chạm
                }
            }
            else if (ghostCollider is CapsuleCollider capsuleCollider)
            {
                // Đối với collider hình capsule, chúng ta sẽ sử dụng một phép xấp xỉ hình cầu đơn giản hơn
                Vector3 center = currentGhostInstance.transform.position + 
                    currentGhostInstance.transform.rotation * Vector3.Scale(capsuleCollider.center, currentGhostInstance.transform.lossyScale);
                
                float radius = capsuleCollider.radius * Mathf.Max(
                    currentGhostInstance.transform.lossyScale.x, 
                    currentGhostInstance.transform.lossyScale.z
                );
                
                if (Physics.CheckSphere(center, radius, collisionCheckMask))
                {
                    return true; // Phát hiện va chạm
                }
            }
            else
            {
                // Đối với các loại collider khác, sử dụng kiểm tra hình cầu bao quanh
                // Điều này ít chính xác hơn nhưng cung cấp một số phát hiện va chạm
                if (Physics.CheckSphere(
                    currentGhostInstance.transform.position,
                    ghostCollider.bounds.extents.magnitude * 0.8f, // Sử dụng 80% của extents để xấp xỉ tốt hơn
                    collisionCheckMask))
                {
                    return true; // Phát hiện va chạm
                }
            }
        }
        
        // Không phát hiện va chạm với bất kỳ collider nào
        return false;
    }
    
    // Cập nhật hình ảnh ghost dựa trên tính hợp lệ của vị trí
    private void UpdateGhostVisuals()
    {
        if (ghostRenderers == null || ghostRenderers.Length == 0) return;
        
        // Chọn màu dựa trên tính hợp lệ của vị trí
        Color targetColor = isValidPlacement ? validPlacementColor : invalidPlacementColor;
        
        // Áp dụng màu cho mỗi renderer
        foreach (Renderer renderer in ghostRenderers)
        {
            Material[] materials = renderer.materials;
            
            foreach (Material mat in materials)
            {
                if (mat == null) continue;
                
                // Tối ưu hóa: Chỉ cập nhật màu nếu cần thiết
                bool needsUpdate = false;
                
                if (mat.HasProperty("_Color"))
                {
                    Color currentColor = mat.GetColor("_Color");
                    needsUpdate = !ColorApproximatelyEqual(currentColor, targetColor);
                    if (needsUpdate) mat.SetColor("_Color", targetColor);
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    Color currentColor = mat.GetColor("_BaseColor");
                    needsUpdate = !ColorApproximatelyEqual(currentColor, targetColor);
                    if (needsUpdate) mat.SetColor("_BaseColor", targetColor);
                }
                else if (mat.HasProperty("_TintColor"))
                {
                    Color currentColor = mat.GetColor("_TintColor");
                    needsUpdate = !ColorApproximatelyEqual(currentColor, targetColor);
                    if (needsUpdate) mat.SetColor("_TintColor", targetColor);
                }
            }
        }
    }
    
    // Kiểm tra hai màu có gần giống nhau không
    private bool ColorApproximatelyEqual(Color a, Color b, float tolerance = 0.01f)
    {
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance &&
               Mathf.Abs(a.a - b.a) < tolerance;
    }
    
    // Đệ quy đặt layer cho một đối tượng và tất cả con của nó
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        
        // Đặt layer
        if (obj.layer != layer)
        {
            obj.layer = layer;
            if (showDebugInfo) Debug.Log($"Đặt layer cho {obj.name} thành {layer} ({LayerMask.LayerToName(layer)})");
        }
        
        // Đặt layer cho tất cả các con
        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }

    // Trợ giúp vẽ một quả cầu debug
    private void DebugDrawSphere(Vector3 center, float radius, Color color, float duration)
    {
        // Xác định số đoạn cần sử dụng cho hình ảnh hóa quả cầu
        int segments = 16;
        float angle = 360f / segments;
        
        // Vẽ ba vòng tròn dọc theo ba trục chính
        for (int axis = 0; axis < 3; axis++)
        {
            for (int i = 0; i < segments; i++)
            {
                // Tính toán hai điểm trên vòng tròn
                float rad1 = Mathf.Deg2Rad * angle * i;
                float rad2 = Mathf.Deg2Rad * angle * ((i + 1) % segments);
                
                Vector3 p1 = center;
                Vector3 p2 = center;
                
                // Đặt các điểm dựa trên trục hiện tại
                if (axis == 0) // Mặt phẳng XY
                {
                    p1.x += radius * Mathf.Cos(rad1);
                    p1.y += radius * Mathf.Sin(rad1);
                    p2.x += radius * Mathf.Cos(rad2);
                    p2.y += radius * Mathf.Sin(rad2);
                }
                else if (axis == 1) // Mặt phẳng XZ
                {
                    p1.x += radius * Mathf.Cos(rad1);
                    p1.z += radius * Mathf.Sin(rad1);
                    p2.x += radius * Mathf.Cos(rad2);
                    p2.z += radius * Mathf.Sin(rad2);
                }
                else // Mặt phẳng YZ
                {
                    p1.y += radius * Mathf.Cos(rad1);
                    p1.z += radius * Mathf.Sin(rad1);
                    p2.y += radius * Mathf.Cos(rad2);
                    p2.z += radius * Mathf.Sin(rad2);
                }
                
                Debug.DrawLine(p1, p2, color, duration);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Hiển thị bán kính tìm kiếm snap trong editor
        if (visualizeSnapRadius && currentGhostInstance != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f); // Màu vàng bán trong suốt
            Gizmos.DrawSphere(currentGhostInstance.transform.position, snapSearchRadius);
        }
    }
}
