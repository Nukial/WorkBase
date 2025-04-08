# Hướng Dẫn Thiết Lập và Kiểm Tra Snap Point

## Luồng Kiểm Tra Kết Nối

Khi người chơi di chuyển preview của một building piece, hệ thống sẽ thực hiện các bước sau để xác định nếu có thể snap:

1. **Tìm kiếm và thu thập**:
   - PlayerBuilder thu thập tất cả các collider trong phạm vi `snapDetectionRadius`
   - Tìm tất cả `SnapPoint` trên preview và các building pieces hiện có

2. **Kiểm tra tính phù hợp**:
   - Cho mỗi cặp (sourceSnap, targetSnap):
     - Gọi `sourceSnap.CanSnapTo(targetSnap)`
     - Nếu match, thêm vào danh sách ứng cử viên

3. **Ưu tiên và sắp xếp**:
   - Sắp xếp các ứng cử viên theo khoảng cách
   - Đặc biệt ưu tiên snap hiện tại nếu không có ý định thoát khỏi nó

4. **Áp dụng snap tốt nhất**:
   - Áp dụng vị trí dựa trên snap tốt nhất
   - Áp dụng rotation nếu tùy chọn được bật

## Luồng Dữ Liệu và Thứ Tự Kiểm Tra

1. **Kiểm tra loại snap**:
   ```csharp
   bool typeMatch = sourceSnap.acceptedTypes.Contains(targetSnap.pointType);
   ```

2. **Kiểm tra hướng** (nếu respectSnapDirection = true):
   - Lấy hướng của cả hai snap point
   - Kiểm tra tương thích theo connectionType:
     ```csharp
     bool directionMatch = AreDirectionsCompatible(otherPoint);
     ```

3. **Kiểm tra khoảng cách**:
   ```csharp
   float dist = Vector3.Distance(sourceSnap.transform.position, targetSnap.transform.position);
   bool distanceMatch = dist < snapMaxDistance;
   ```

4. **Áp dụng snap**:
   - Chỉ khi cả ba điều kiện trên đều thỏa mãn

## Chi Tiết Về Loại Kết Nối (ConnectionType)

ConnectionType xác định cách hai snap point có thể kết nối với nhau theo hướng:

| ConnectionType | Mô tả | Dot Product | Ví dụ Sử Dụng |
|----------------|-------|-------------|---------------|
| Opposite | Ngược hướng nhau | < -0.7 | Nối hai thanh thẳng, nối móng với tường |
| Perpendicular | Vuông góc (90°) | -0.3 đến 0.3 | Góc tường, giao giữa cột và dầm ngang |
| Parallel | Cùng hướng | > 0.7 | Sàn với mái, hai mảnh sàn cùng độ cao |
| Any | Không quan tâm đến hướng | Bất kỳ | Điểm gắn đa năng, điểm trang trí |

## Thứ Tự Ưu Tiên Điều Kiện

Khi nhiều snap có thể kết nối, hệ thống ưu tiên theo thứ tự:

1. **Snap hiện tại**: Nếu đã snap và người chơi không cố thoát khỏi snap đó
2. **Khoảng cách**: Snap gần nhất có ưu tiên cao nhất
3. **Góc phù hợp**: Nếu hai snap có khoảng cách tương đương, snap có góc phù hợp hơn sẽ được ưu tiên

## Hướng Dẫn Thiết Lập Snap Point Cho Prefab

### 1. Thiết lập cấu trúc prefab hợp lý:

```
Foundation Prefab
|-- Mesh
|-- Collider
|-- BuildingPiece (Component)
|-- SnapPoints
    |-- TopEdge_01 (SnapPoint - Forward)
    |-- TopEdge_02 (SnapPoint - Back)
    |-- Side_01 (SnapPoint - Left)
    |-- Side_02 (SnapPoint - Right)
```

### 2. Thiết lập SnapPoint component:

- **PointType**: Chọn loại tương ứng với vị trí (ví dụ: FoundationTopEdge)
- **AcceptedTypes**: Danh sách các loại có thể snap với điểm này (ví dụ: WallBottom)
- **SnapDirection**: Hướng mà snap point hướng ra (nên hướng ra ngoài khối)
- **ConnectionType**: Cách snap point kết nối với snap point khác
- **ProvidesSupport**: Có cung cấp hỗ trợ cấu trúc hay không

### 3. Thiết lập các loại kết nối phổ biến:

#### Kết nối móng với tường:
- **Móng**: PointType = FoundationTopEdge, SnapDirection = Up, ConnectionType = Opposite
- **Tường**: PointType = WallBottom, SnapDirection = Down, ConnectionType = Opposite

#### Kết nối hai tường vuông góc:
- **Tường 1**: PointType = WallSide, SnapDirection = Left/Right, ConnectionType = Perpendicular
- **Tường 2**: PointType = WallSide, SnapDirection = Forward/Back, ConnectionType = Perpendicular

#### Kết nối sàn với sàn:
- **Sàn 1**: PointType = FloorEdge, SnapDirection = Forward/Back/Left/Right, ConnectionType = Parallel
- **Sàn 2**: PointType = FloorEdge, SnapDirection = Forward/Back/Left/Right, ConnectionType = Parallel

### 4. Debug và điều chỉnh:

- Bật `showSnapDebug` và `showConnectionTypeInfo` để thấy trực quan các liên kết
- Điều chỉnh `visualOffset` và `visualRotationOffset` để tinh chỉnh vị trí sau khi snap

## Mẹo và Lưu ý

- Luôn đặt snap point với hướng hợp lý, hướng ra ngoài khối building
- Sử dụng đủ snap point để hỗ trợ đa dạng cách xây dựng
- Đặt snap point ở cạnh/góc chính xác, không đặt quá sâu trong mesh
- Cẩn thận khi sử dụng ConnectionType.Any vì có thể dẫn đến kết nối không mong muốn
- Thiết lập acceptedTypes một cách hợp lý để tránh kết nối giữa các loại không tương thích
