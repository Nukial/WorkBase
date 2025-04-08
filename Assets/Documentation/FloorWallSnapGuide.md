# Hướng dẫn thiết lập Snap Point cho kết nối Sàn-Tường

## 1. Kết nối Sàn đặt trên Tường

### Thiết lập trên Tường:
- **Vị trí**: Đặt SnapPoint tại cạnh trên (đỉnh) của tường
- **PointType**: `WallTop`
- **AcceptedTypes**: `{ FloorEdge }`
- **SnapDirection**: `Up` (hướng lên trên)
- **ConnectionType**: `Opposite` hoặc `Perpendicular` (tùy theo thiết kế)

### Thiết lập trên Sàn:
- **Vị trí**: Đặt SnapPoint tại cạnh dưới (cạnh) của sàn
- **PointType**: `FloorEdge`
- **AcceptedTypes**: `{ WallTop }`
- **SnapDirection**: `Down` (hướng xuống dưới)
- **ConnectionType**: `Opposite` hoặc `Perpendicular` (phải tương ứng với tường)

![Minh họa kết nối sàn-tường kiểu 1](Images/FloorOnWallSnap.png)

## 2. Kết nối Sàn gắn vào cạnh Tường

### Thiết lập trên Tường:
- **Vị trí**: Đặt SnapPoint tại vị trí bên hông tường nơi sàn sẽ gắn vào
- **PointType**: `WallSide`
- **AcceptedTypes**: `{ FloorEdge }`
- **SnapDirection**: `Left/Right` (hướng ra ngoài từ tường)
- **ConnectionType**: `Perpendicular`

### Thiết lập trên Sàn:
- **Vị trí**: Đặt SnapPoint tại cạnh của sàn sẽ gắn vào tường
- **PointType**: `FloorEdge`
- **AcceptedTypes**: `{ WallSide }`
- **SnapDirection**: `Left/Right` (hướng vào tường)
- **ConnectionType**: `Perpendicular`

![Minh họa kết nối sàn-tường kiểu 2](Images/FloorToWallSideSnap.png)
