using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Quản lý quá trình xây dựng của người chơi
/// 
/// Hướng dẫn vị trí Snap Point và cách chúng kết nối:
/// 
/// 1. Kết nối tường-nền:
///    - SnapPoint loại WallBottom (đặt ở đáy tường, hướng xuống) với 
///    - SnapPoint loại FoundationTopEdge (đặt ở cạnh trên nền, hướng lên)
///    
/// 2. Kết nối tường-tường:
///    - Thẳng hàng: Hai WallSide (đặt ở cạnh tường, hướng ra), connectionType = Opposite
///    - Góc 90°: Hai WallSide (đặt ở cạnh tường, hướng ra), connectionType = Perpendicular
///    - Góc 45°: Hai WallSide (đặt ở cạnh tường, hướng ra), connectionType = Angle45
///    
/// 3. Kết nối sàn-tường:
///    - FloorEdge (đặt ở cạnh sàn, hướng ra) với 
///    - WallTop (đặt ở đỉnh tường, hướng lên)
///    
/// 4. Kết nối mái-tường:
///    - RoofBottomEdge (đặt ở cạnh dưới mái, hướng xuống) với 
///    - WallTop (đặt ở đỉnh tường, hướng lên)
///    
/// 5. Kết nối cửa-tường:
///    - DoorFrameSide (đặt ở cạnh khung cửa, hướng ra) với 
///    - WallSide (đặt ở cạnh tường, hướng ra)
/// </summary>
public class PlayerBuilder : MonoBehaviour {
    public Camera buildCamera;
    public float buildDistance = 10f;
    public LayerMask placementLayerMask;
    [Tooltip("Layer mask dùng để kiểm tra va chạm khi đặt building. Chỉ định các layer cần kiểm tra va chạm")]
    public LayerMask collisionCheckMask;
    public bool showCollisionCheck = false;
    public float snapDetectionRadius = 1f;
    public float snapMaxDistance = 0.5f;
    public List<BuildingPieceSO> allBuildablePieces;
    public BuildingPieceSO currentPieceSO;
    public GameObject previewInstance;
    public BaseStorage activeBaseStorage;
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;
    public bool isBuildingMode;
    public bool canPlaceCurrentPreview;
    public bool isPreviewSnapped;
    public bool useAdvancedSnap;
    public bool applyRotationDuringSnap = false; // Added option to control rotation during snap
    public float snapTolerance = 0.1f; // ngưỡng để giữ snap nếu chưa thay đổi nhiều
    private Transform lastSnapTarget; // Lưu kết quả snap từ frame trước
    private SnapPoint lastUsedSourceSnap; // Lưu điểm snap nguồn đã dùng

    // Thêm biến mới để kiểm soát ưu tiên snap
    public float snapPriorityDistance = 2.0f; // Khoảng cách ưu tiên tìm snap trước khi dùng raycast
    private float snapStabilityTimer = 0f; // Đếm thời gian từ lần snap cuối
    public float snapStabilityThreshold = 0.5f; // Thời gian (giây) trước khi cho phép bỏ snap
    private bool forceStableSnap = false; // Khi true, sẽ duy trì snap hiện tại cho đến khi có snap tốt hơn
    private float lastSnapQuality = 0f; // Độ tốt của snap hiện tại (0 = không snap)

    // Thêm biến để theo dõi vị trí chuột trước đó
    private Vector3 lastMousePosition;
    private Vector3 lastRaycastHitPoint;
    private float mouseMovementThreshold = 1.5f; // Ngưỡng chuyển động chuột để bỏ snap
    private bool hasValidRaycastHit = false; // Kiểm tra nếu raycast hit thành công

    // Cache của preview instances
    private Dictionary<BuildingPieceSO, GameObject> previewCache = new Dictionary<BuildingPieceSO, GameObject>();
    private Quaternion lastPreviewRotation = Quaternion.identity; // Lưu góc xoay khi đổi pieces

    // Số lượng tối đa preview trong cache
    public int maxCacheSize = 5;

    // Thêm biến để theo dõi danh mục được chọn hiện tại
    private PieceCategory currentCategory = PieceCategory.Foundation;
    private int currentPieceIndex = 0;

    // Thêm mapping từ phím số đến danh mục
    private Dictionary<KeyCode, PieceCategory> categoryKeyMapping = new Dictionary<KeyCode, PieceCategory>() {
        { KeyCode.Alpha1, PieceCategory.Foundation },
        { KeyCode.Alpha2, PieceCategory.Wall },
        { KeyCode.Alpha3, PieceCategory.Floor },
        { KeyCode.Alpha4, PieceCategory.Roof },
        { KeyCode.Alpha5, PieceCategory.Utility }
    };

    [Header("Snap Settings")]
    public bool respectSnapDirection = true; // Mới: tùy chọn tôn trọng hướng của snap point
    [Tooltip("Hiển thị thông tin về loại kết nối snap khi debug")]
    public bool showConnectionTypeInfo = false;
    public float minDirectionDotProduct = -0.7f; // Mới: giá trị dot product tối thiểu để snap (mặc định đòi hỏi khá ngược hướng)
    public bool showSnapDebug = false; // Mới: hiển thị hướng snap khi debug

    // Thêm chế độ đặt tự do
    [Header("Free Placement")]
    [Tooltip("Khi bật, đối tượng sẽ không snap vào bất kỳ điểm nào")]
    public bool freeplacementMode = false;
    [Tooltip("Phím tắt để bật/tắt chế độ đặt tự do")]
    public KeyCode freeplacementToggleKey = KeyCode.LeftControl;
    [Tooltip("Hiển thị thông báo khi chuyển chế độ")]
    public bool showFreePlacementMessages = true;
    [Tooltip("Khoảng cách từ bề mặt khi đặt đồ theo chế độ tự do")]
    public float surfaceOffset = 0.01f;

    // Thêm biến để theo dõi xem người dùng đã xoay thủ công chưa
    private bool hasUserRotation = false;

    void Update() {
        // Xử lý phím tắt để thay đổi danh mục và phần tử
        CheckForCategoryAndPieceSelection();

        if (Input.GetKeyDown(KeyCode.B)) {
            ToggleBuildMode();
        }
        
        // Thêm phím tắt để bật/tắt chế độ đặt tự do
        if (isBuildingMode && Input.GetKeyDown(freeplacementToggleKey)) {
            ToggleFreePlacementMode();
        }

        if (isBuildingMode && currentPieceSO != null) {
            HandlePreviewUpdate();
            if (Input.GetKeyDown(KeyCode.Q)) {
                RotatePreview(-45f);
            }
            if (Input.GetKeyDown(KeyCode.E)) {
                RotatePreview(45f);
            }
            if (Input.GetMouseButtonDown(0)) {
                TryPlacePiece();
            }

            // Cập nhật điểm hitpoint của raycast trong mỗi frame
            Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, buildDistance, placementLayerMask)) {
                lastRaycastHitPoint = hit.point;
                hasValidRaycastHit = true;
            } else {
                hasValidRaycastHit = false;
            }
        }
    }

    // Phương thức mới để kiểm tra phím tắt và thay đổi danh mục/piece
    private void CheckForCategoryAndPieceSelection() {
        if (!isBuildingMode) return;

        // Kiểm tra phím số để chọn danh mục
        foreach (var categoryMapping in categoryKeyMapping) {
            if (Input.GetKeyDown(categoryMapping.Key)) {
                SelectCategory(categoryMapping.Value);
                return;
            }
        }

        // Kiểm tra phím mũi tên để chuyển đổi giữa các pieces
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Period)) {
            SelectNextPiece();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Comma)) {
            SelectPreviousPiece();
        }
    }

    // Phương thức để chọn danh mục
    public void SelectCategory(PieceCategory category) {
        currentCategory = category;

        // Lấy pieces thuộc danh mục được chọn
        var piecesInCategory = allBuildablePieces.Where(p => p.category == category).ToList();

        if (piecesInCategory.Count > 0) {
            // Reset index và chọn piece đầu tiên trong danh mục
            currentPieceIndex = 0;
            SetSelectedPiece(piecesInCategory[currentPieceIndex]);

            // Hiển thị thông báo
            Debug.Log($"Switched to category: {category} - Selected: {currentPieceSO.pieceName}");
        } else {
            Debug.Log($"No pieces available in category: {category}");
        }
    }

    // Phương thức để chọn piece tiếp theo trong danh mục hiện tại
    public void SelectNextPiece() {
        var piecesInCategory = allBuildablePieces.Where(p => p.category == currentCategory).ToList();

        if (piecesInCategory.Count > 0) {
            currentPieceIndex = (currentPieceIndex + 1) % piecesInCategory.Count;
            SetSelectedPiece(piecesInCategory[currentPieceIndex]);

            // Hiển thị thông báo
            Debug.Log($"Selected: {currentPieceSO.pieceName} ({currentPieceIndex + 1}/{piecesInCategory.Count})");
        }
    }

    // Phương thức để chọn piece trước đó trong danh mục hiện tại
    public void SelectPreviousPiece() {
        var piecesInCategory = allBuildablePieces.Where(p => p.category == currentCategory).ToList();

        if (piecesInCategory.Count > 0) {
            currentPieceIndex = (currentPieceIndex - 1 + piecesInCategory.Count) % piecesInCategory.Count;
            SetSelectedPiece(piecesInCategory[currentPieceIndex]);

            // Hiển thị thông báo
            Debug.Log($"Selected: {currentPieceSO.pieceName} ({currentPieceIndex + 1}/{piecesInCategory.Count})");
        }
    }

    public void ToggleBuildMode() {
        isBuildingMode = !isBuildingMode;
        if (isBuildingMode) {
            activeBaseStorage = FindNearestBaseStorage();
            if (currentPieceSO != null) {
                GetOrCreatePreviewInstance();
            }
        } else {
            // Khi tắt build mode, ẩn preview thay vì hủy
            if (previewInstance) {
                previewInstance.SetActive(false);
            }
        }
    }

    public void SetSelectedPiece(BuildingPieceSO pieceSO) {
        if (pieceSO == currentPieceSO) return; // Không thay đổi nếu là piece hiện tại

        // Lưu lại rotation từ preview trước nếu có
        if (previewInstance != null) {
            lastPreviewRotation = previewInstance.transform.rotation;
            // Ẩn preview hiện tại thay vì hủy
            previewInstance.SetActive(false);
        }

        currentPieceSO = pieceSO;

        if (isBuildingMode) {
            GetOrCreatePreviewInstance();
        }
    }

    private void GetOrCreatePreviewInstance() {
        // Nếu không có piece hoặc không ở buidingMode, không làm gì cả
        if (currentPieceSO == null || !isBuildingMode) return;

        // Kiểm tra xem có trong cache không
        if (previewCache.TryGetValue(currentPieceSO, out GameObject cachedPreview)) {
            previewInstance = cachedPreview;
            previewInstance.SetActive(true);
        } else {
            InstantiatePreview();

            // Thêm vào cache
            previewCache[currentPieceSO] = previewInstance;

            // Xóa các instance cũ nếu cache quá lớn
            if (previewCache.Count > maxCacheSize) {
                CleanupOldestCacheEntry();
            }
        }

        // Áp dụng rotation đã lưu từ trước
        previewInstance.transform.rotation = lastPreviewRotation;

        // Đảm bảo material được cập nhật
        ApplyPreviewMaterial(invalidPlacementMaterial);
    }

    private void CleanupOldestCacheEntry() {
        // Tìm entry đầu tiên (oldest) để xóa
        BuildingPieceSO oldestKey = null;

        foreach (var key in previewCache.Keys) {
            if (key != currentPieceSO) {
                oldestKey = key;
                break;
            }
        }

        if (oldestKey != null) {
            // Hủy gameObject nếu nó không phải là previewInstance hiện tại
            if (previewCache[oldestKey] != previewInstance) {
                Destroy(previewCache[oldestKey]);
            }
            previewCache.Remove(oldestKey);
        }
    }

    void InstantiatePreview() {
        if (currentPieceSO != null) {
            // Sử dụng phương thức GetPreviewPrefab để lấy prefab thích hợp
            GameObject prefabToUse = currentPieceSO.GetPreviewPrefab();

            if (prefabToUse != null) {
                previewInstance = Instantiate(prefabToUse);
                foreach (Collider col in previewInstance.GetComponentsInChildren<Collider>()) {
                    col.enabled = false;
                }
                SetLayerRecursively(previewInstance, LayerMask.NameToLayer("Ignore Raycast"));
                ApplyPreviewMaterial(invalidPlacementMaterial);
            }
        }
    }

    void HandlePreviewUpdate() {
        if (previewInstance == null) return;

        // Lấy vị trí chuột hiện tại và tính toán khoảng cách di chuyển
        Vector3 currentMousePosition = Input.mousePosition;
        float mouseMovementDistance = Vector3.Distance(currentMousePosition, lastMousePosition);

        // Cập nhật vị trí chuột cuối cùng
        lastMousePosition = currentMousePosition;

        // Thực hiện raycast để lấy điểm va chạm
        Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);
        hasValidRaycastHit = Physics.Raycast(ray, out RaycastHit hit, buildDistance, placementLayerMask);

        // Nếu đang ở chế độ đặt tự do, bỏ qua toàn bộ logic snap
        if (freeplacementMode) {
            HandleFreePlacement(hit);
        }
        else if (hasValidRaycastHit) {
            // Tính khoảng cách giữa điểm va chạm mới và vị trí snap hiện tại
            float distToCurrentSnap = isPreviewSnapped && lastSnapTarget != null ?
                Vector3.Distance(hit.point, lastSnapTarget.position) : float.MaxValue;

            // Nếu quá xa snap hiện tại hoặc người dùng di chuyển chuột nhanh, ưu tiên di chuyển theo raycast
            if (distToCurrentSnap > snapPriorityDistance || mouseMovementDistance > mouseMovementThreshold && isPreviewSnapped) {
                forceStableSnap = false;

                // Di chuyển preview đến vị trí raycast
                previewInstance.transform.position = hit.point;
                UpdateSnapState(false, new SnapResult());

                // Tìm snap mới nếu có
                SnapResult newSnapResult = FindBestSnap();
                if (newSnapResult.targetSnap != null && newSnapResult.distance < snapMaxDistance) {
                    ApplySnapResult(newSnapResult);
                    UpdateSnapState(true, newSnapResult);
                    snapStabilityTimer = 0f;
                    forceStableSnap = true;
                }
            }
            // Nếu vẫn gần snap hiện tại, tiếp tục quy trình thông thường
            else {
                ProcessNormalSnapping();
            }
        }
        // Nếu không hit được, vẫn kiểm tra nếu cần giải phóng khỏi snap hiện tại
        else if (mouseMovementDistance > mouseMovementThreshold && isPreviewSnapped) {
            forceStableSnap = false;
            UpdateSnapState(false, new SnapResult());
        }

        // Luôn kiểm tra tính hợp lệ và cập nhật vật liệu
        CheckPlacementValidity();
        UpdatePreviewMaterial();
    }

    // Thêm phương thức xử lý chế độ đặt tự do
    private void HandleFreePlacement(RaycastHit hit) {
        if (!hasValidRaycastHit) return;

        // Đặt vị trí preview tại điểm va chạm với một offset nhỏ theo hướng normal
        Vector3 placementPosition = hit.point + hit.normal * surfaceOffset;
        previewInstance.transform.position = placementPosition;
        
        // Nếu là bề mặt ngang (sàn), giữ nguyên rotation
        // Nếu là bề mặt thẳng đứng (tường), xoay để mặt hướng ra ngoài
        bool isVerticalSurface = Vector3.Dot(hit.normal, Vector3.up) < 0.5f;
        
        // Chỉ tự động xoay nếu người dùng chưa xoay thủ công
        if (!hasUserRotation) {
            if (isVerticalSurface) {
                // Xoay đối tượng để mặt hướng ra ngoài từ tường
                Quaternion wallRotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
                previewInstance.transform.rotation = wallRotation;
            } else {
                // Trên bề mặt nằm ngang, giữ hướng forward nhưng đảm bảo "right" hướng ra ngoài
                previewInstance.transform.rotation = Quaternion.Euler(0, previewInstance.transform.eulerAngles.y, 0);
            }
        }
        
        // Nếu đang được snapped, cần giải phóng snap
        if (isPreviewSnapped) {
            UpdateSnapState(false, new SnapResult());
        }
    }

    void ProcessNormalSnapping() {
        SnapResult snapResult = FindBestSnap();
        bool foundValidSnap = snapResult.targetSnap != null && snapResult.distance < snapMaxDistance;

        // Kiểm tra xem có cần ổn định snap
        if (isPreviewSnapped) {
            // Nếu đã snap, tăng thời gian ổn định
            snapStabilityTimer += Time.deltaTime;

            // Nếu tìm thấy một snap mới tốt hơn, thì áp dụng
            if (foundValidSnap && (snapResult.targetSnap.transform != lastSnapTarget ||
                snapResult.distance < lastSnapQuality * 0.8f)) { // Snap mới tốt hơn 20%

                // Áp dụng snap mới
                ApplySnapResult(snapResult);
                UpdateSnapState(true, snapResult);

                // Reset bộ đếm ổn định
                snapStabilityTimer = 0f;
                forceStableSnap = true;
            }
            // Nếu đã ổn định đủ lâu, cho phép bỏ snap nếu cần
            else if (snapStabilityTimer > snapStabilityThreshold && !foundValidSnap) {
                forceStableSnap = false;
            }
        }
        // Nếu chưa snap và tìm thấy snap hợp lệ
        else if (foundValidSnap) {
            ApplySnapResult(snapResult);
            UpdateSnapState(true, snapResult);
            snapStabilityTimer = 0f;
            forceStableSnap = true;
        }

        // Chỉ dùng raycast khi không có snap nào đang active hoặc snap không ổn định
        if (!isPreviewSnapped || (!forceStableSnap && !foundValidSnap)) {
            // Thực hiện raycast thông thường
            if (hasValidRaycastHit) {
                HandleRaycastPlacement();
            }
        }
    }

    void HandleRaycastPlacement() {
        // Sử dụng lastRaycastHitPoint thay vì thực hiện raycast lại
        Vector3 hitPoint = lastRaycastHitPoint;

        // Nếu snap trước đó vẫn còn trong ngưỡng dung sai, giữ nó
        if (lastSnapTarget != null && Vector3.Distance(hitPoint, lastSnapTarget.position) < snapTolerance) {
            // Giữ snap hiện tại
            previewInstance.transform.position = lastSnapTarget.position;
            previewInstance.transform.rotation = lastSnapTarget.rotation;
        } else {
            // Di chuyển đến vị trí raycast
            previewInstance.transform.position = hitPoint;
            UpdateSnapState(false, new SnapResult());
        }
    }

    void UpdateSnapState(bool isSnapped, SnapResult result) {
        isPreviewSnapped = isSnapped;

        if (isSnapped) {
            lastSnapTarget = result.targetSnap.transform;
            lastUsedSourceSnap = result.sourceSnap;
            lastSnapQuality = result.distance;
        } else {
            lastSnapTarget = null;
            lastUsedSourceSnap = null;
            lastSnapQuality = float.MaxValue;
        }
    }

    private struct SnapResult {
        public SnapPoint sourceSnap; // Điểm snap trên preview
        public SnapPoint targetSnap; // Điểm snap trên đối tượng xây dựng hiện có
        public float distance;
        public Vector3 offset;
    }

    private struct SnapCandidate {
        public SnapPoint sourceSnap;
        public SnapPoint targetSnap;
        public float distance;
        public Vector3 offset;
    }

    /// <summary>
    /// Tìm snap tốt nhất cho đối tượng preview
    /// 
    /// Quy tắc vị trí đặt snap tối ưu:
    /// - SnapPoint tại EDGE (cạnh): đặt tại trung điểm cạnh, vector hướng vuông góc với cạnh
    /// - SnapPoint tại CORNER (góc): đặt chính xác tại góc, vector hướng theo đường chéo
    /// - SnapPoint tại SURFACE: đặt tại vị trí cần gắn, vector hướng vuông góc với bề mặt
    /// - SnapPoint dạng Bottom/Top: đặt tại trung tâm đáy/đỉnh, hướng thẳng xuống/lên
    /// </summary>
    SnapResult FindBestSnap() {
        SnapResult result = new SnapResult {
            sourceSnap = null,
            targetSnap = null,
            distance = snapMaxDistance
        };

        if (previewInstance == null) return result;

        // Giảm bán kính tìm snap khi đã có raycast hit xa
        float searchRadius = isPreviewSnapped && hasValidRaycastHit &&
                            Vector3.Distance(lastRaycastHitPoint, lastSnapTarget.position) > snapPriorityDistance ?
                            snapDetectionRadius * 0.7f : // Giảm bán kính tìm kiếm khi chúng ta muốn thoát khỏi snap
                            (isPreviewSnapped ? snapDetectionRadius * 1.5f : snapPriorityDistance);

        // Ưu tiên tìm snap trong phạm vi từ vị trí hiện tại
        Collider[] colliders = Physics.OverlapSphere(previewInstance.transform.position, searchRadius, placementLayerMask);

        if (colliders.Length == 0)
            return result;

        SnapPoint[] previewSnapPoints = previewInstance.GetComponentsInChildren<SnapPoint>();
        if (previewSnapPoints.Length == 0)
            return result;

        // Debug visualization
        if (showSnapDebug) {
            foreach (var snap in previewSnapPoints) {
                Debug.DrawRay(snap.transform.position, snap.GetDirectionVector(), Color.yellow, 0.1f);
            }
        }

        List<SnapCandidate> candidates = new List<SnapCandidate>();

        foreach (var col in colliders) {
            BuildingPiece bp = col.GetComponent<BuildingPiece>();
            if (bp == null) continue;

            foreach (var targetSnap in bp.snapPoints) {
                // Debug visualization 
                if (showSnapDebug) {
                    Debug.DrawRay(targetSnap.transform.position, targetSnap.GetDirectionVector(), Color.blue, 0.1f);
                    
                    // Hiển thị loại kết nối nếu được bật
                    if (showConnectionTypeInfo) {
                        string connType = targetSnap.connectionType.ToString();
                        Debug.DrawLine(targetSnap.transform.position, 
                            targetSnap.transform.position + Vector3.up * 0.2f, 
                            GetConnectionTypeColor(targetSnap.connectionType), 0.1f);
                    }
                }

                foreach (var sourceSnap in previewSnapPoints) {
                    // Sử dụng phương thức CanSnapTo mới của SnapPoint
                    if (sourceSnap.CanSnapTo(targetSnap)) {
                        float dist = Vector3.Distance(sourceSnap.transform.position, targetSnap.transform.position);

                        // Hiển thị debug visuals nếu cần
                        if (respectSnapDirection && showSnapDebug && dist < snapMaxDistance) {
                            // Hiển thị kết nối với màu tương ứng theo loại kết nối
                            Color lineColor = GetConnectionTypeColor(sourceSnap.connectionType);
                            Debug.DrawLine(sourceSnap.transform.position, targetSnap.transform.position, lineColor, 0.1f);
                        }

                        // Chỉ ưu tiên snap hiện tại nếu chúng ta không đang cố gắng thoát khỏi snap
                        bool isCurrentSnap = (lastSnapTarget != null && targetSnap.transform == lastSnapTarget);
                        bool tryingToBreakSnap = hasValidRaycastHit &&
                                               Vector3.Distance(lastRaycastHitPoint, targetSnap.transform.position) > snapPriorityDistance;

                        float priorityMultiplier = 1.0f;
                        if (isCurrentSnap && !tryingToBreakSnap) {
                            priorityMultiplier = 0.7f; // Giảm 30% khoảng cách cho snap hiện tại nếu không cố thoát
                        }

                        // Thêm vào danh sách ứng cử viên
                        if (dist * priorityMultiplier < result.distance) {
                            candidates.Add(new SnapCandidate {
                                sourceSnap = sourceSnap,
                                targetSnap = targetSnap,
                                distance = dist * priorityMultiplier,
                                offset = targetSnap.transform.position - sourceSnap.transform.position
                            });
                        }
                    }
                }
            }
        }

        // Sắp xếp ứng cử viên theo khoảng cách và chọn cái tốt nhất
        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
        
        if (candidates.Count > 0) {
            var best = candidates[0];
            result.sourceSnap = best.sourceSnap;
            result.targetSnap = best.targetSnap;
            result.distance = best.distance;
            result.offset = best.offset;
        }

        return result;
    }

    private Color GetConnectionTypeColor(ConnectionType type) {
        switch (type) {
            case ConnectionType.Opposite:
                return new Color(1f, 0.5f, 0, 1); // Orange
            case ConnectionType.Perpendicular:
                return new Color(0, 0.8f, 0.8f, 1); // Cyan
            case ConnectionType.Parallel:
                return new Color(1f, 0.8f, 0.2f, 1); // Yellow
            default:
                return new Color(0.8f, 0.8f, 0.8f, 1); // White
        }
    }

    // Thêm phương thức này để phát hiện loại góc giữa hai tường
    private string DetectWallConnectionType(SnapPoint sourceSnap, SnapPoint targetSnap) {
        if (sourceSnap.pointType != SnapType.WallSide || targetSnap.pointType != SnapType.WallSide)
            return "Không phải kết nối tường";
            
        Vector3 sourceDir = sourceSnap.GetDirectionVector();
        Vector3 targetDir = targetSnap.GetDirectionVector();
        float angle = Vector3.Angle(sourceDir, targetDir);
        
        if (angle > 165f) return "Nối tiếp";
        if (angle > 75f && angle < 105f) return "Góc vuông";
        
        return "Góc " + angle.ToString("F0") + "°";
    }

    /// <summary>
    /// Áp dụng kết quả snap vào đối tượng preview
    /// 
    /// Lưu ý về vị trí snap cho các kết nối phổ biến:
    /// 1. Tường chữ T (ba tường gặp nhau):
    ///    - Đặt WallSide tại cả hai cạnh của mỗi tường
    ///    - Khi kết nối, dùng WallSide-WallSide với connectionType = Perpendicular
    ///    
    /// 2. Tường góc 45 độ:
    ///    - Dùng hai WallSide với connectionType = Angle45
    ///    - Vector hướng của WallSide phải hướng ra ngoài vuông góc với cạnh tường
    ///    
    /// 3. Mái ngói góc (hip roof):
    ///    - Điểm RoofRidge đặt tại đỉnh mái, hướng dọc theo sống mái
    ///    - Điểm RoofHip đặt tại góc mái, hướng dọc theo đường mái
    /// </summary>
    void ApplySnapResult(SnapResult result) {
        if (result.sourceSnap != null && result.targetSnap != null) {
            // Lưu lại rotation gốc để duy trì góc xoay do người dùng thiết lập
            Quaternion originalRotation = previewInstance.transform.rotation;

            // Tính toán offset vị trí
            Vector3 offset = result.offset;

            // Di chuyển preview đến vị trí snap
            previewInstance.transform.position = result.targetSnap.transform.position -
                                (result.sourceSnap.transform.position - previewInstance.transform.position);

            // Xử lý góc xoay đặc biệt
            if (applyRotationDuringSnap) {
                // Tính toán sự chênh lệch giữa hướng của hai snap point
                Quaternion sourceToLocalRotation = Quaternion.Inverse(result.sourceSnap.transform.rotation);
                Quaternion rotationDifference = sourceToLocalRotation * previewInstance.transform.rotation;
                
                // Áp dụng rotation mới dựa trên hướng đối tượng đích
                Quaternion targetRotation = result.targetSnap.transform.rotation * rotationDifference;
                
                // Kiểm tra xem người dùng đã điều chỉnh góc xoay hay chưa
                if (hasUserRotation) {
                    // Duy trì góc xoay do người dùng thiết lập (nếu không khóa)
                    previewInstance.transform.rotation = result.sourceSnap.PreserveUserRotation(originalRotation, targetRotation);
                } else {
                    // Hoặc sử dụng rotation được đề xuất từ snap point
                    previewInstance.transform.rotation = result.targetSnap.GetSnappedRotation(result.sourceSnap, targetRotation);
                }
            } else {
                // Nếu không áp dụng rotation trong snap, vẫn cần tuân thủ rotationStep nếu có
                if (result.sourceSnap.lockRotation) {
                    float currentYRotation = originalRotation.eulerAngles.y;
                    float step = result.sourceSnap.rotationStep;
                    float closestStep = Mathf.Round(currentYRotation / step) * step;
                    
                    previewInstance.transform.rotation = Quaternion.Euler(
                        originalRotation.eulerAngles.x,
                        closestStep,
                        originalRotation.eulerAngles.z
                    );
                }
            }

            // Áp dụng các offset trực quan nếu bật chế độ nâng cao
            if (useAdvancedSnap) {
                // Luôn áp dụng offset vị trí
                Vector3 visualPositionOffset = result.targetSnap.transform.rotation * result.targetSnap.visualOffset;
                previewInstance.transform.position += visualPositionOffset;
                
                // Chỉ áp dụng offset góc xoay nếu được phép
                if (applyRotationDuringSnap) {
                    previewInstance.transform.rotation *= result.targetSnap.visualRotationOffset;
                }
            }
            
            // Debug visualization nếu cần
            if (showSnapDebug) {
                // Hiển thị kết nối snap thành công
                Debug.DrawLine(result.sourceSnap.transform.position, result.targetSnap.transform.position, 
                    Color.magenta, 0.5f);
                
                // Hiển thị hướng sau khi snap
                Debug.DrawRay(previewInstance.transform.position, previewInstance.transform.forward * 0.5f, 
                    Color.cyan, 0.5f);
            }

            // Hiển thị loại kết nối nếu là kết nối tường-tường
            if (result.sourceSnap != null && result.targetSnap != null &&
                result.sourceSnap.pointType == SnapType.WallSide && 
                result.targetSnap.pointType == SnapType.WallSide && showSnapDebug) {
                
                string connectionType = DetectWallConnectionType(result.sourceSnap, result.targetSnap);
                Debug.Log("Loại kết nối tường: " + connectionType);
            }
        }
    }

    void UpdatePreviewMaterial() {
        // Đơn giản hóa, chỉ sử dụng 2 material: valid và invalid
        ApplyPreviewMaterial(canPlaceCurrentPreview ? validPlacementMaterial : invalidPlacementMaterial);
    }

    void CheckPlacementValidity() {
        if (previewInstance == null) return;
        
        // Sử dụng collisionCheckMask thay vì placementLayerMask
        Vector3 boxSize = previewInstance.transform.localScale / 2;
        
        // Điều chỉnh kích thước box để phù hợp hơn với hình dạng thực tế
        Renderer[] renderers = previewInstance.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0) {
            // Tính Bounds dựa trên tất cả renderer
            Bounds combinedBounds = new Bounds(renderers[0].bounds.center, Vector3.zero);
            foreach (Renderer rend in renderers) {
                combinedBounds.Encapsulate(rend.bounds);
            }
            
            // Quy đổi kích thước sang local space của preview
            boxSize = combinedBounds.size / 2;
        }
        
        // Kiểm tra va chạm với collisionCheckMask
        Collider[] colliders = Physics.OverlapBox(
            previewInstance.transform.position, 
            boxSize, 
            previewInstance.transform.rotation, 
            collisionCheckMask
        );
        
        // Debug visualization
        if (showCollisionCheck) {
            Matrix4x4 matrix = Matrix4x4.TRS(
                previewInstance.transform.position,
                previewInstance.transform.rotation,
                boxSize * 2
            );
            
            Color debugColor = colliders.Length <= 0 ? Color.green : Color.red;
            Debug.DrawLine(previewInstance.transform.position, previewInstance.transform.position + Vector3.up * 2, debugColor);
            
            // Log số lượng colliders va chạm
            if (colliders.Length > 0) {
                Debug.Log($"Colliding with {colliders.Length} objects: {string.Join(", ", colliders.Select(c => c.name))}");
            }
        }
        
        bool noCollision = colliders.Length <= 0;
        bool hasResources = activeBaseStorage != null && activeBaseStorage.CheckResources(currentPieceSO.requiredResource, currentPieceSO.resourceCost);
        bool hasStructuralSupport = true;

        if (isPreviewSnapped && lastUsedSourceSnap != null) {
            SnapPoint targetSnap = null;
            Transform lastSnapTransform = lastSnapTarget;

            if (lastSnapTransform != null) {
                targetSnap = lastSnapTransform.GetComponent<SnapPoint>();
            }

            if (targetSnap != null && !targetSnap.providesSupport) {
                hasStructuralSupport = false;
            }
        }

        canPlaceCurrentPreview = noCollision && hasResources && hasStructuralSupport;
    }

    public void RotatePreview(float angle) {
        if (previewInstance != null) {
            // Đánh dấu rằng người dùng đã tự điều chỉnh rotation
            hasUserRotation = true;
            
            // Xoay preview theo góc xoay cung cấp
            previewInstance.transform.Rotate(Vector3.up, angle);
            
            // Kiểm tra xem có đang áp dụng snap nào với rotationStep không
            if (isPreviewSnapped && lastUsedSourceSnap != null && lastUsedSourceSnap.lockRotation) {
                // Xoay theo các bậc thang của rotationStep
                float currentYRotation = previewInstance.transform.eulerAngles.y;
                float step = lastUsedSourceSnap.rotationStep;
                float closestStep = Mathf.Round(currentYRotation / step) * step;
                
                // Áp dụng rotation làm tròn theo step
                previewInstance.transform.rotation = Quaternion.Euler(
                    previewInstance.transform.eulerAngles.x,
                    closestStep,
                    previewInstance.transform.eulerAngles.z
                );
            }
            
            // Cập nhật lastPreviewRotation để giữ góc xoay khi đổi pieces
            lastPreviewRotation = previewInstance.transform.rotation;
        }
    }

    public void TryPlacePiece() {
        if (canPlaceCurrentPreview && activeBaseStorage != null) {
            if (activeBaseStorage.ConsumeResources(currentPieceSO.requiredResource, currentPieceSO.resourceCost)) {
                Instantiate(currentPieceSO.prefab, previewInstance.transform.position, previewInstance.transform.rotation);
            }
        }
    }

    BaseStorage FindNearestBaseStorage() {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 20f);
        BaseStorage nearest = null;
        float bestDistance = Mathf.Infinity;
        foreach (var col in colliders) {
            BaseStorage bs = col.GetComponent<BaseStorage>();
            if (bs != null) {
                float dist = Vector3.Distance(transform.position, bs.transform.position);
                if (dist < bestDistance) {
                    bestDistance = dist;
                    nearest = bs;
                }
            }
        }
        return nearest;
    }

    void ApplyPreviewMaterial(Material mat) {
        foreach (Renderer rend in previewInstance.GetComponentsInChildren<Renderer>()) {
            rend.material = mat;
        }
    }

    void SetLayerRecursively(GameObject obj, int newLayer) {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    // Thêm phương thức để dọn dẹp cache khi cần
    public void ClearPreviewCache() {
        foreach (var preview in previewCache.Values) {
            if (preview != previewInstance) { // Không hủy preview đang sử dụng
                Destroy(preview);
            }
        }
        previewCache.Clear();

        // Thêm lại preview hiện tại vào cache nếu có
        if (previewInstance != null && currentPieceSO != null) {
            previewCache[currentPieceSO] = previewInstance;
        }
    }

    void OnDestroy() {
        // Dọn dẹp tất cả prefabs trong cache khi component bị hủy
        foreach (var preview in previewCache.Values) {
            Destroy(preview);
        }
    }

    // Thêm phương thức OnDrawGizmos để hiển thị vùng kiểm tra va chạm trong Editor
    void OnDrawGizmos() {
        if (previewInstance != null && showCollisionCheck) {
            Vector3 boxSize = previewInstance.transform.localScale / 2;
            
            Renderer[] renderers = previewInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0) {
                Bounds combinedBounds = new Bounds(renderers[0].bounds.center, Vector3.zero);
                foreach (Renderer rend in renderers) {
                    combinedBounds.Encapsulate(rend.bounds);
                }
                boxSize = combinedBounds.size / 2;
            }
            
            Gizmos.color = canPlaceCurrentPreview ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
            Gizmos.matrix = Matrix4x4.TRS(
                previewInstance.transform.position,
                previewInstance.transform.rotation,
                Vector3.one
            );
            Gizmos.DrawCube(Vector3.zero, boxSize * 2);
        }
    }

    // Thêm phương thức để bật/tắt tùy chọn tôn trọng hướng snap
    public void ToggleRespectSnapDirection() {
        respectSnapDirection = !respectSnapDirection;
        Debug.Log($"Respect snap direction: {respectSnapDirection}");
    }

    // Thêm phương thức để bật/tắt debug snap
    public void ToggleSnapDebug() {
        showSnapDebug = !showSnapDebug;
        Debug.Log($"Snap debug visualization: {showSnapDebug}");
    }

    // Thêm phương thức để bật/tắt hiển thị thông tin về loại kết nối
    public void ToggleConnectionTypeInfo() {
        showConnectionTypeInfo = !showConnectionTypeInfo;
        Debug.Log($"Connection type info: {showConnectionTypeInfo}");
    }

    // Thêm phương thức để bật/tắt chế độ đặt tự do
    public void ToggleFreePlacementMode() {
        freeplacementMode = !freeplacementMode;
        if (showFreePlacementMessages) {
            Debug.Log("Chế độ đặt tự do: " + (freeplacementMode ? "BẬT" : "TẮT"));
        }
        
        // Nếu đang snapped và chuyển sang free placement, cần giải phóng snap
        if (freeplacementMode && isPreviewSnapped) {
            UpdateSnapState(false, new SnapResult());
        }
    }
}