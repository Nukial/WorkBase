using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Quản lý việc đặt các đối tượng có thể xây dựng với chức năng khớp nối.
/// Cho phép các đối tượng khớp nối với các điểm kết nối tương thích, có tính đến các layer.
/// </summary>
public class ObjectPlacer : MonoBehaviour
{
    [Header("Cài Đặt Đặt Vật Thể")]
    public GameObject objectToPlacePrefab;

    [Header("Cài Đặt Layer")]
    [Tooltip("Các layer mà tia raycast có thể bắn trúng để xác định vị trí ban đầu (ví dụ: Ground, PlaceableStructure).")]
    public LayerMask placementSurfaceMask;

    [Tooltip("Các layer chứa SnapPoints mà đối tượng xem trước có thể khớp nối (ví dụ: PlaceableStructure). QUAN TRỌNG: Loại trừ layer Player!")]
    public LayerMask snapLayerMask;

    [Tooltip("Các layer mà đối tượng cuối cùng không được va chạm khi đặt (ví dụ: Player, CriticalObjects).")]
    public LayerMask collisionCheckMask;

    [Header("Cài Đặt Khớp Nối")]
    public float snapDistance = 1.0f;
    public float gridSize = 1.0f;
    public bool useGridSnapping = true;

    [Header("Phản Hồi Trực Quan")]
    public Material previewMaterial;
    public Material snappedPreviewMaterial;
    public Material collisionPreviewMaterial;

    [Header("Chế Độ Xây Dựng")]
    public bool buildModeActive = false;
    public KeyCode toggleBuildModeKey = KeyCode.B;

    [Header("Cài Đặt Góc Nhìn Thứ Ba")]
    [Tooltip("Khoảng cách tối đa từ người chơi mà các đối tượng có thể được đặt")]
    public float maxPlacementDistance = 5.0f;
    [Tooltip("Tham chiếu đến transform của người chơi để tính toán khoảng cách")]
    public Transform playerTransform;
    [Tooltip("Chỉ báo trực quan tùy chọn để hiển thị vị trí người chơi đang nhắm tới")]
    public GameObject placementReticle;
    [Tooltip("Layer mask cho tia raycast của reticle (thường giống với placementSurfaceMask)")]
    public LayerMask reticleMask;
    [Tooltip("Hệ thống có nên sử dụng reticle ở giữa màn hình thay vì con trỏ chuột không?")]
    public bool useScreenCenterReticle = true;
    [Tooltip("Phản hồi trực quan cho việc đặt hợp lệ/không hợp lệ")]
    public bool showPlacementRadius = true;

    // Biến nội bộ
    private GameObject previewObject;
    private List<SnapPoint> previewSnapPoints = new List<SnapPoint>();
    private bool isSnapped = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool placementBlockedByCollision = false;

    // Tham chiếu đến các điểm khớp nối cho khớp nối tốt nhất hiện tại
    private SnapPoint bestSourcePoint;
    private SnapPoint bestTargetPoint;

    // Thông tin collider được lưu cache để kiểm tra va chạm
    private Collider previewObjectCollider;

    // Các biến nội bộ bổ sung
    private bool isInPlacementRange = true;
    private GameObject placementRadiusIndicator;

    void Start()
    {
        // Đăng ký các tag/layer mặc định cho bề mặt hợp lệ
        RegisterDefaultValidSurfaces();
        
        if (buildModeActive && objectToPlacePrefab != null)
        {
            CreatePreviewObject();
        }

        // Tạo chỉ báo bán kính đặt nếu cần
        if (showPlacementRadius && playerTransform != null)
        {
            CreatePlacementRadiusIndicator();
        }
    }

    /// <summary>
    /// Đăng ký các bề mặt hợp lệ mặc định từ project settings
    /// </summary>
    private void RegisterDefaultValidSurfaces()
    {
        // Xóa danh sách hiện tại
        SnapPoint.ClearValidSurfaces();
        
        // Thêm các tag/layer thường sử dụng
        // Chỉ thêm nếu tồn tại trong project
        if (IsTagDefined("Ground")) SnapPoint.AddValidSurfaceTag("Ground");
        if (IsTagDefined("Terrain")) SnapPoint.AddValidSurfaceTag("Terrain");
        if (IsTagDefined("Foundation")) SnapPoint.AddValidSurfaceTag("Foundation");
        if (IsTagDefined("Default")) SnapPoint.AddValidSurfaceTag("Default"); // Tag mặc định luôn tồn tại
        
        // Thêm các layer thường sử dụng
        if (LayerMask.NameToLayer("Ground") != -1) SnapPoint.AddValidSurfaceLayer("Ground");
        if (LayerMask.NameToLayer("BuildingSurface") != -1) SnapPoint.AddValidSurfaceLayer("BuildingSurface");
        if (LayerMask.NameToLayer("Default") != -1) SnapPoint.AddValidSurfaceLayer("Default");
    }
    
    /// <summary>
    /// Kiểm tra xem một tag có được định nghĩa trong project không
    /// </summary>
    private bool IsTagDefined(string tagName)
    {
        try
        {
            // Tạo GameObject tạm để kiểm tra tag
            GameObject tempObject = new GameObject();
            tempObject.tag = tagName;
            bool isDefined = true;
            Destroy(tempObject);
            return isDefined;
        }
        catch
        {
            return false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleBuildModeKey))
        {
            ToggleBuildMode();
        }

        if (!buildModeActive)
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }

            // Ẩn chỉ báo bán kính khi không ở chế độ xây dựng
            if (placementRadiusIndicator != null)
            {
                placementRadiusIndicator.SetActive(false);
            }
            return;
        }

        // Hiển thị chỉ báo bán kính khi ở chế độ xây dựng
        if (placementRadiusIndicator != null)
        {
            placementRadiusIndicator.SetActive(true);
            UpdatePlacementRadiusIndicator();
        }

        if (buildModeActive && previewObject == null && objectToPlacePrefab != null)
        {
            CreatePreviewObject();
        }

        if (previewObject == null) return;

        UpdatePreviewPosition();

        // Kiểm tra xem có nằm trong phạm vi đặt từ người chơi không
        CheckPlacementRange();

        // Chỉ tiếp tục với khớp nối và đặt nếu trong phạm vi
        if (isInPlacementRange)
        {
            FindBestSnapPoint();
            ApplyTargetTransform();
            CheckPlacementCollision();

            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceObject();
            }
        }

        // Cập nhật trực quan dựa trên tất cả các điều kiện
        UpdatePreviewVisuals();
    }

    /// <summary>
    /// Bật/tắt chế độ xây dựng
    /// </summary>
    public void ToggleBuildMode()
    {
        buildModeActive = !buildModeActive;

        if (buildModeActive)
        {
            if (previewObject == null && objectToPlacePrefab != null)
            {
                CreatePreviewObject();
            }
        }
        else
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }
        }
    }

    /// <summary>
    /// Tạo đối tượng xem trước từ prefab
    /// </summary>
    private void CreatePreviewObject()
    {
        previewObject = Instantiate(objectToPlacePrefab, Vector3.zero, Quaternion.identity);
        previewObject.name = "Preview_" + objectToPlacePrefab.name;

        DisableUnnecessaryComponents(keepCollidersEnabled: true);

        previewSnapPoints.Clear();
        previewSnapPoints.AddRange(previewObject.GetComponentsInChildren<SnapPoint>());

        previewObjectCollider = previewObject.GetComponentInChildren<Collider>();
        if (previewObjectCollider == null)
        {
            Debug.LogWarning("Đối tượng xem trước thiếu Collider, kiểm tra va chạm có thể không chính xác.");
        }
    }

    /// <summary>
    /// Vô hiệu hóa các thành phần không cần thiết trong chế độ xem trước
    /// </summary>
    private void DisableUnnecessaryComponents(bool keepCollidersEnabled = false)
    {
        Rigidbody rb = previewObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        MonoBehaviour[] scripts = previewObject.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (!(script is SnapPoint) && !(script is ObjectPlacer))
            {
                script.enabled = false;
            }
        }

        Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            if (collider is MeshCollider meshCollider)
            {
                if (keepCollidersEnabled)
                {
                    if (meshCollider.convex)
                    {
                        meshCollider.isTrigger = true;
                    }
                    else
                    {
                        // Đối với MeshCollider lõm, ta không thể đặt thành trigger
                        meshCollider.enabled = false;
                    }
                }
                else
                {
                    meshCollider.enabled = false;
                }
            }
            else
            {
                // Đối với các loại collider khác (box, sphere, capsule, v.v.)
                if (keepCollidersEnabled)
                {
                    collider.isTrigger = true;
                }
                else
                {
                    collider.enabled = false;
                }
            }
        }
    }

    /// <summary>
    /// Áp dụng material được chỉ định cho tất cả renderers trong preview
    /// </summary>
    private void ApplyMaterialToPreview(Material material)
    {
        if (material == null || previewObject == null) return;

        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0)
            {
                Material[] materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }
                renderer.materials = materials;
            }
        }
    }

    /// <summary>
    /// Cập nhật vị trí preview dựa trên con trỏ chuột hoặc điểm giữa màn hình
    /// </summary>
    private void UpdatePreviewPosition()
    {
        Ray ray;

        if (useScreenCenterReticle)
        {
            // Sử dụng điểm giữa màn hình cho tia thay vì vị trí chuột
            ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (placementReticle != null)
            {
                UpdateReticlePosition(ray);
            }
        }
        else
        {
            // Sử dụng vị trí con trỏ chuột (hành vi ban đầu)
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        }

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, placementSurfaceMask))
        {
            // Đây có phải là bề mặt đặt hợp lệ không?
            if (SnapPoint.IsValidPlacementSurface(hit.collider.gameObject))
            {
                targetPosition = hit.point;

                if (useGridSnapping && !isSnapped)
                {
                    ApplyGridSnapping(ref targetPosition);
                }

                if (!isSnapped)
                {
                    targetRotation = previewObject.transform.rotation;
                }
            }
        }
    }

    /// <summary>
    /// Áp dụng căn chỉnh lưới cho vector vị trí
    /// </summary>
    private void ApplyGridSnapping(ref Vector3 position)
    {
        position.x = Mathf.Round(position.x / gridSize) * gridSize;
        position.y = Mathf.Round(position.y / gridSize) * gridSize;
        position.z = Mathf.Round(position.z / gridSize) * gridSize;
    }

    /// <summary>
    /// Tìm các điểm khớp nối tốt nhất giữa đối tượng xem trước và đối tượng thế giới
    /// </summary>
    private void FindBestSnapPoint()
    {
        isSnapped = false;
        bestSourcePoint = null;
        bestTargetPoint = null;
        float bestDistance = snapDistance;

        if (previewSnapPoints.Count == 0) return;

        Vector3 currentPreviewPosition = previewObject.transform.position;

        Collider[] hitColliders = Physics.OverlapSphere(currentPreviewPosition, snapDistance, snapLayerMask);

        foreach (Collider collider in hitColliders)
        {
            SnapPoint targetPoint = collider.GetComponent<SnapPoint>();
            if (targetPoint == null) continue;

            // Không khớp nối với các điểm trên chính đối tượng xem trước
            if (targetPoint.transform.IsChildOf(previewObject.transform)) continue;

            foreach (SnapPoint sourcePoint in previewSnapPoints)
            {
                if (IsCompatibleSnapPoint(sourcePoint, targetPoint))
                {
                    // Tính khoảng cách giữa các vị trí thế giới của điểm khớp nối
                    float distance = Vector3.Distance(sourcePoint.transform.position, targetPoint.transform.position);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestSourcePoint = sourcePoint;
                        bestTargetPoint = targetPoint;
                        isSnapped = true;
                    }
                }
            }
        }

        // Nếu đã khớp nối, tính toán vị trí và xoay chính xác
        if (isSnapped)
        {
            CalculateSnapTransform();
        }
    }

    /// <summary>
    /// Kiểm tra xem hai điểm khớp nối có tương thích để kết nối không
    /// </summary>
    private bool IsCompatibleSnapPoint(SnapPoint source, SnapPoint target)
    {
        return source.IsValidConnection(target);
    }

    /// <summary>
    /// Tính toán vị trí và hướng đối tượng khi khớp nối
    /// </summary>
    private void CalculateSnapTransform()
    {
        if (!isSnapped || bestSourcePoint == null || bestTargetPoint == null) return;

        // Tính toán offset vị trí để các điểm khớp nối thẳng hàng
        Vector3 sourcePointWorldPos = bestSourcePoint.transform.position;
        Vector3 previewRootPos = previewObject.transform.position;
        Vector3 offset = sourcePointWorldPos - previewRootPos;

        // Tính vị trí đích
        targetPosition = bestTargetPoint.transform.position - offset;

        // Tính xoay để căn chỉnh các hướng
        CalculateSnapRotation();
    }

    /// <summary>
    /// Áp dụng vị trí và xoay đã tính toán cho đối tượng xem trước
    /// </summary>
    private void ApplyTargetTransform()
    {
        if (previewObject != null)
        {
            previewObject.transform.position = targetPosition;
            previewObject.transform.rotation = targetRotation;
        }
    }

    /// <summary>
    /// Tính toán góc xoay cần thiết để căn chỉnh các điểm khớp nối
    /// </summary>
    private void CalculateSnapRotation()
    {
        if (!isSnapped || bestSourcePoint == null || bestTargetPoint == null) return;

        // Lấy hướng trong không gian thế giới
        Vector3 sourceDir = bestSourcePoint.transform.TransformDirection(bestSourcePoint.snapDirection);
        Vector3 targetDir = bestTargetPoint.transform.TransformDirection(bestTargetPoint.snapDirection);

        // Xoay preview để sourceDir trỏ ngược hướng với targetDir
        Quaternion currentRotation = previewObject.transform.rotation;
        Quaternion rotationToApply = Quaternion.FromToRotation(currentRotation * bestSourcePoint.snapDirection.normalized, -targetDir.normalized);

        targetRotation = rotationToApply * currentRotation;
    }

    /// <summary>
    /// Kiểm tra xem đối tượng xem trước ở vị trí đích có va chạm với các layer cấm không
    /// </summary>
    private void CheckPlacementCollision()
    {
        placementBlockedByCollision = false;
        if (previewObjectCollider == null || collisionCheckMask.value == 0)
        {
            return;
        }

        if (previewObjectCollider is BoxCollider boxCollider)
        {
            // Tính lại tâm và kích thước thế giới dựa trên transform ĐÍCH
            Vector3 worldCenter = targetPosition + targetRotation * boxCollider.center;
            Vector3 worldHalfExtents = Vector3.Scale(boxCollider.size / 2, previewObject.transform.lossyScale);

            // Kiểm tra các va chạm CHỈ trên các layer được chỉ định trong collisionCheckMask
            Collider[] overlaps = Physics.OverlapBox(worldCenter, worldHalfExtents, targetRotation, collisionCheckMask, QueryTriggerInteraction.Ignore);

            // Kiểm tra xem có va chạm nào KHÔNG phải là con của chính đối tượng xem trước
            if (overlaps.Any(col => !col.transform.IsChildOf(previewObject.transform) && col.gameObject != previewObject))
            {
                placementBlockedByCollision = true;
            }
        }
        else if (previewObjectCollider is SphereCollider sphereCollider)
        {
            Vector3 worldCenter = targetPosition + targetRotation * sphereCollider.center;
            float worldRadius = sphereCollider.radius * Mathf.Max(previewObject.transform.lossyScale.x, previewObject.transform.lossyScale.y, previewObject.transform.lossyScale.z);
            Collider[] overlaps = Physics.OverlapSphere(worldCenter, worldRadius, collisionCheckMask, QueryTriggerInteraction.Ignore);
            if (overlaps.Any(col => !col.transform.IsChildOf(previewObject.transform) && col.gameObject != previewObject))
            {
                placementBlockedByCollision = true;
            }
        }
        else
        {
            Debug.LogWarning("Kiểm tra va chạm chỉ được triển khai cho BoxCollider và SphereCollider trên gốc preview.");
        }
    }

    /// <summary>
    /// Cập nhật hiển thị của đối tượng xem trước dựa trên trạng thái
    /// </summary>
    private void UpdatePreviewVisuals()
    {
        if (previewObject == null) return;

        // Ẩn preview nếu ngoài phạm vi
        if (!isInPlacementRange)
        {
            previewObject.SetActive(false);
            return;
        }

        previewObject.SetActive(true);

        if (placementBlockedByCollision)
        {
            ApplyMaterialToPreview(collisionPreviewMaterial); // Đỏ -> Không thể đặt
        }
        else if (isSnapped)
        {
            ApplyMaterialToPreview(snappedPreviewMaterial); // Xanh -> Sẽ khớp nối
        }
        else
        {
            ApplyMaterialToPreview(previewMaterial); // Trắng/Trong suốt -> Đặt tự do/lưới
        }
    }

    /// <summary>
    /// Cố gắng đặt đối tượng cuối cùng nếu các điều kiện được đáp ứng
    /// </summary>
    private void TryPlaceObject()
    {
        if (placementBlockedByCollision)
        {
            Debug.LogWarning("Đặt thất bại: Vị trí bị chặn.");
            return;
        }

        if (!isInPlacementRange)
        {
            Debug.LogWarning("Đặt thất bại: Quá xa người chơi.");
            return;
        }

        GameObject placedObject = Instantiate(objectToPlacePrefab, targetPosition, targetRotation);
        placedObject.name = objectToPlacePrefab.name;

        EnableComponentsOnPlacedObject(placedObject);

        Debug.Log($"Đã đặt {placedObject.name} tại {targetPosition}");
    }

    /// <summary>
    /// Kích hoạt các thành phần cần thiết trên đối tượng mới đặt
    /// </summary>
    private void EnableComponentsOnPlacedObject(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.isTrigger = false; // Đặt lại thành vật thể rắn
        }

        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
        {
            script.enabled = true; // Kích hoạt lại tất cả scripts
        }

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
        }
    }

    /// <summary>
    /// Tạo chỉ báo bán kính đặt
    /// </summary>
    private void CreatePlacementRadiusIndicator()
    {
        placementRadiusIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        placementRadiusIndicator.name = "ChỉBáoBánKính";

        DestroyImmediate(placementRadiusIndicator.GetComponent<Collider>());

        placementRadiusIndicator.transform.localScale = new Vector3(
            maxPlacementDistance * 2,
            0.02f, // Rất mỏng
            maxPlacementDistance * 2
        );

        Renderer renderer = placementRadiusIndicator.GetComponent<Renderer>();
        Material radiusMaterial = new Material(Shader.Find("Transparent/Diffuse"));
        radiusMaterial.color = new Color(0.2f, 0.8f, 1f, 0.2f); // Xanh nhạt, trong suốt
        renderer.material = radiusMaterial;

        placementRadiusIndicator.SetActive(buildModeActive);
    }

    /// <summary>
    /// Cập nhật vị trí chỉ báo bán kính đặt để theo người chơi
    /// </summary>
    private void UpdatePlacementRadiusIndicator()
    {
        if (placementRadiusIndicator != null && playerTransform != null)
        {
            // Đặt ở chân người chơi, hơi cao hơn mặt đất
            Vector3 indicatorPos = playerTransform.position;
            indicatorPos.y = playerTransform.position.y + 0.05f; // Hơi cao hơn mặt đất để tránh z-fighting
            placementRadiusIndicator.transform.position = indicatorPos;
        }
    }

    /// <summary>
    /// Kiểm tra xem vị trí đặt hiện tại có nằm trong phạm vi của người chơi không
    /// </summary>
    private void CheckPlacementRange()
    {
        if (playerTransform == null)
        {
            isInPlacementRange = true; // Không có tham chiếu người chơi, giả sử luôn trong phạm vi
            return;
        }

        // Lấy khoảng cách ngang (bỏ qua sự khác biệt Y để cho phép xây dựng lên/xuống)
        Vector2 playerPos2D = new Vector2(playerTransform.position.x, playerTransform.position.z);
        Vector2 targetPos2D = new Vector2(targetPosition.x, targetPosition.z);
        float horizontalDistance = Vector2.Distance(playerPos2D, targetPos2D);

        isInPlacementRange = horizontalDistance <= maxPlacementDistance;
    }

    /// <summary>
    /// Cập nhật vị trí reticle dựa trên nơi tia bắn trúng
    /// </summary>
    private void UpdateReticlePosition(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, reticleMask))
        {
            placementReticle.transform.position = hit.point;
            placementReticle.transform.up = hit.normal;
            placementReticle.SetActive(true);
        }
        else
        {
            placementReticle.SetActive(false);
        }
    }

    /// <summary>
    /// Đặt transform của người chơi để kiểm tra khoảng cách
    /// </summary>
    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
        if (placementRadiusIndicator != null && showPlacementRadius)
        {
            placementRadiusIndicator.SetActive(player != null && buildModeActive);
        }
    }

    /// <summary>
    /// Thay đổi đối tượng cần đặt
    /// </summary>
    public void ChangeObjectToPlace(GameObject newPrefab)
    {
        if (newPrefab != objectToPlacePrefab)
        {
            objectToPlacePrefab = newPrefab;

            // Nếu đang ở chế độ xây dựng, tạo lại đối tượng xem trước với prefab mới
            if (buildModeActive && previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;

                if (objectToPlacePrefab != null)
                {
                    CreatePreviewObject();
                }
            }
        }
    }

    /// <summary>
    /// Dọn dẹp khi thành phần bị hủy
    /// </summary>
    private void OnDestroy()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }
    }
}
