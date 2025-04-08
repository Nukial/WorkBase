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

## 3. Kết nối Sàn cao tầng

### Kết nối Sàn với Sàn (cùng độ cao):
- **Vị trí**: Đặt SnapPoint tại cạnh của hai sàn
- **PointType**: `FloorEdge`
- **AcceptedTypes**: `{ FloorEdge }`
- **SnapDirection**: `Left/Right/Forward/Back` (hướng ra ngoài từ cạnh sàn)
- **ConnectionType**: `Parallel`

### Kết nối Sàn với Cầu thang:
- **Vị trí**: Đặt SnapPoint tại cạnh của sàn nơi cầu thang kết thúc
- **PointType**: `FloorEdge`
- **AcceptedTypes**: `{ StairTop }`
- **SnapDirection**: `Down` (hướng xuống dưới)
- **ConnectionType**: `Opposite`

### Kết nối Sàn tầng trên với Tường tầng dưới:
- **Cấu hình giống mục 1**, nhưng cần đảm bảo:
  - Chiều cao tường phải phù hợp với độ cao giữa các tầng
  - Các tường nối cần được đặt trực tiếp bên dưới vị trí sàn tầng trên

![Minh họa kết nối sàn cao tầng](Images/ElevatedFloorSnap.png)

## 4. Kết nối Sàn với Cầu thang

### Thiết lập trên Sàn (Tầng trên):
- **Vị trí**: Đặt SnapPoint tại cạnh sàn nơi đỉnh cầu thang kết nối
- **PointType**: `FloorEdge`
- **AcceptedTypes**: `{ StairTop }`
- **SnapDirection**: `Down` (hướng xuống dưới)
- **ConnectionType**: `Opposite`

### Thiết lập trên Sàn (Tầng dưới):
- **Vị trí**: Đặt SnapPoint tại cạnh sàn nơi chân cầu thang kết nối
- **PointType**: `FloorEdge`
- **AcceptedTypes**: `{ StairBottom }`
- **SnapDirection**: `Up` (hướng lên trên)
- **ConnectionType**: `Opposite`

### Lưu ý về đặt Sàn cao tầng:
- Luôn đảm bảo có đủ tường hoặc cột hỗ trợ tại các góc sàn
- Sàn cao tầng nên có ít nhất 3-4 điểm kết nối với kết cấu hỗ trợ
- Kết nối với cầu thang cần được đặt chính xác để đảm bảo tính liên tục

Chi tiết hơn về cách thiết lập cầu thang, vui lòng tham khảo [Hướng dẫn Cầu thang](StairGuide.md).
