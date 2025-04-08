using System.Collections.Generic;
using UnityEngine;

public class PlayerBuilder : MonoBehaviour {
    public Camera buildCamera;
    public float buildDistance = 10f;
    public LayerMask placementLayerMask;
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

    void Update() {
        if (Input.GetKeyDown(KeyCode.B)) {
            ToggleBuildMode();
        }

        if (isBuildingMode && currentPieceSO != null) {
            HandlePreviewUpdate();
            if (Input.GetKeyDown(KeyCode.Q)) {
                RotatePreview(-15f);
            }
            if (Input.GetKeyDown(KeyCode.E)) {
                RotatePreview(15f);
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

        // Kiểm tra nếu người dùng di chuyển chuột đủ xa để muốn bỏ snap
        bool shouldBreakSnap = mouseMovementDistance > mouseMovementThreshold && isPreviewSnapped;

        // Thực hiện raycast để lấy điểm va chạm
        Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);
        hasValidRaycastHit = Physics.Raycast(ray, out RaycastHit hit, buildDistance, placementLayerMask);

        if (hasValidRaycastHit) {
            lastRaycastHitPoint = hit.point;

            // Tính khoảng cách giữa điểm va chạm mới và vị trí snap hiện tại
            float distToCurrentSnap = isPreviewSnapped && lastSnapTarget != null ?
                Vector3.Distance(hit.point, lastSnapTarget.position) : float.MaxValue;

            // Nếu quá xa snap hiện tại hoặc người dùng di chuyển chuột nhanh, ưu tiên di chuyển theo raycast
            if (distToCurrentSnap > snapPriorityDistance || shouldBreakSnap) {
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
        else if (shouldBreakSnap) {
            forceStableSnap = false;
            UpdateSnapState(false, new SnapResult());
        }

        // Luôn kiểm tra tính hợp lệ và cập nhật vật liệu
        CheckPlacementValidity();
        UpdatePreviewMaterial();
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

        foreach (var col in colliders) {
            BuildingPiece bp = col.GetComponent<BuildingPiece>();
            if (bp == null) continue;

            foreach (var targetSnap in bp.snapPoints) {
                foreach (var sourceSnap in previewSnapPoints) {
                    if (sourceSnap.CanSnapTo(targetSnap)) {
                        float dist = Vector3.Distance(sourceSnap.transform.position, targetSnap.transform.position);

                        // Chỉ ưu tiên snap hiện tại nếu chúng ta không đang cố gắng thoát khỏi snap
                        bool isCurrentSnap = (lastSnapTarget != null && targetSnap.transform == lastSnapTarget);
                        bool tryingToBreakSnap = hasValidRaycastHit &&
                                               Vector3.Distance(lastRaycastHitPoint, targetSnap.transform.position) > snapPriorityDistance;

                        float priorityMultiplier = 1.0f;
                        if (isCurrentSnap && !tryingToBreakSnap) {
                            priorityMultiplier = 0.7f; // Giảm 30% khoảng cách cho snap hiện tại nếu không cố thoát
                        }

                        if (dist * priorityMultiplier < result.distance) {
                            result.distance = dist;
                            result.sourceSnap = sourceSnap;
                            result.targetSnap = targetSnap;
                            result.offset = targetSnap.transform.position - sourceSnap.transform.position;
                        }
                    }
                }
            }
        }

        return result;
    }

    void ApplySnapResult(SnapResult result) {
        if (result.sourceSnap != null && result.targetSnap != null) {
            Vector3 offset = result.offset;

            // Position alignment always happens
            previewInstance.transform.position = result.targetSnap.transform.position -
                                               (result.sourceSnap.transform.position - previewInstance.transform.position);

            // Only apply rotation if the option is enabled
            if (applyRotationDuringSnap) {
                Quaternion rotationDifference = Quaternion.Inverse(result.sourceSnap.transform.rotation) * previewInstance.transform.rotation;
                previewInstance.transform.rotation = result.targetSnap.transform.rotation * rotationDifference;
            }

            if (useAdvancedSnap) {
                previewInstance.transform.position += result.targetSnap.transform.rotation * result.targetSnap.visualOffset;
                // Only apply advanced rotation if rotation is enabled
                if (applyRotationDuringSnap) {
                    previewInstance.transform.rotation *= result.targetSnap.visualRotationOffset;
                }
            }
        }
    }

    // Add a method to toggle rotation adjustment
    public void ToggleRotationDuringSnap() {
        applyRotationDuringSnap = !applyRotationDuringSnap;
    }

    void UpdatePreviewMaterial() {
        // Đơn giản hóa, chỉ sử dụng 2 material: valid và invalid
        ApplyPreviewMaterial(canPlaceCurrentPreview ? validPlacementMaterial : invalidPlacementMaterial);
    }

    void CheckPlacementValidity() {
        Collider[] colliders = Physics.OverlapBox(previewInstance.transform.position, previewInstance.transform.localScale / 2, previewInstance.transform.rotation, placementLayerMask);
        bool noCollision = colliders.Length <= 1;
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
            previewInstance.transform.Rotate(Vector3.up, angle);
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
}