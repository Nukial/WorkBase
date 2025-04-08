# Hướng dẫn thiết lập Cầu thang

## Tổng quan về Cầu thang
Cầu thang là cấu trúc quan trọng để kết nối giữa các tầng. Trong hệ thống xây dựng, cầu thang cần có các điểm kết nối (Snap Point) ở đầu và cuối, cũng như có thể có các điểm kết nối ở hai bên để gắn lan can.

## Các loại Snap Point cho Cầu thang

### 1. Điểm kết nối chính
- **StairBottom**: Đặt ở chân cầu thang, kết nối với sàn tầng dưới
- **StairTop**: Đặt ở đỉnh cầu thang, kết nối với sàn tầng trên
- **StairSide**: Đặt ở hai bên cầu thang, kết nối với lan can hoặc tường

## Thiết lập Snap Point cho Cầu thang

### Tại chân cầu thang (StairBottom):
- **PointType**: `StairBottom`
- **AcceptedTypes**: `{ FloorEdge, FoundationTopEdge }`
- **SnapDirection**: `Down` (hướng xuống dưới)
- **ConnectionType**: `Opposite`
- **ProvidesSupport**: `false` (cầu thang cần được hỗ trợ, không phải là kết cấu hỗ trợ)

### Tại đỉnh cầu thang (StairTop):
- **PointType**: `StairTop`
- **AcceptedTypes**: `{ FloorEdge }`
- **SnapDirection**: `Up` (hướng lên trên)
- **ConnectionType**: `Opposite`
- **ProvidesSupport**: `false`

### Tại cạnh cầu thang (StairSide):
- **PointType**: `StairSide`
- **AcceptedTypes**: `{ FencePostBottom, WallSide }`
- **SnapDirection**: `Left/Right` (hướng ra ngoài từ cầu thang)
- **ConnectionType**: `Perpendicular`
- **ProvidesSupport**: `true` (hỗ trợ cho lan can)

## Cách kết nối Cầu thang với các kết cấu khác

### 1. Kết nối Cầu thang với Sàn tầng dưới
![Minh họa kết nối cầu thang với sàn tầng dưới](Images/StairToLowerFloor.png)

#### Thiết lập trên Cầu thang:
- **Vị trí**: Đặt SnapPoint tại chân cầu thang
- **PointType**: `StairBottom`
- **AcceptedTypes**: `{ FloorEdge }`
- **SnapDirection**: `Down`
- **ConnectionType**: `Opposite`

#### Thiết lập trên Sàn:
- **Vị trí**: Đặt SnapPoint tại cạnh sàn nơi chân cầu thang sẽ kết nối
- **PointType**: `FloorEdge`
- **AcceptedTypes**: `{ StairBottom }`
- **SnapDirection**: `Up`
- **ConnectionType**: `Opposite`

### 2. Kết nối Cầu thang với Sàn tầng trên
![Minh họa kết nối cầu thang với sàn tầng trên](Images/StairToUpperFloor.png)

#### Thiết lập trên Cầu thang:
- **Vị trí**: Đặt SnapPoint tại đỉnh cầu thang
- **PointType**: `StairTop`
- **AcceptedTypes**: `{ FloorEdge }`
- **SnapDirection**: `Up`
- **ConnectionType**: `Opposite`

#### Thiết lập trên Sàn:
- **Vị trí**: Đặt SnapPoint tại cạnh sàn nơi đỉnh cầu thang sẽ kết nối
- **PointType**: `FloorEdge`
- **AcceptedTypes**: `{ StairTop }`
- **SnapDirection**: `Down`
- **ConnectionType**: `Opposite`

### 3. Kết nối Cầu thang với Lan can
![Minh họa kết nối cầu thang với lan can](Images/StairToRailing.png)

#### Thiết lập trên Cầu thang:
- **Vị trí**: Đặt SnapPoint tại cạnh bên cầu thang
- **PointType**: `StairSide`
- **AcceptedTypes**: `{ FencePostBottom }`
- **SnapDirection**: `Left/Right` (hướng ra ngoài từ cầu thang)
- **ConnectionType**: `Perpendicular`

#### Thiết lập trên Lan can:
- **Vị trí**: Đặt SnapPoint tại chân cột lan can
- **PointType**: `FencePostBottom`
- **AcceptedTypes**: `{ StairSide }`
- **SnapDirection**: `Down`
- **ConnectionType**: `Perpendicular`

## Lưu ý khi thiết kế Cầu thang

### Tỷ lệ và Kích thước
- Chiều cao mỗi bậc thang nên nhất quán, thông thường từ 15-20cm
- Chiều sâu mỗi bậc thang thông thường từ 25-30cm
- Chiều rộng cầu thang nên từ 80-120cm tùy theo mục đích sử dụng

### Góc nghiêng
- Góc nghiêng cầu thang thường từ 30-45 độ
- Cầu thang dốc hơn khó sử dụng, nguy hiểm cho người sử dụng

### Chiều cao giữa các tầng
- Khi thiết kế cầu thang, cần biết chính xác chiều cao giữa các tầng
- Thông thường, chiều cao giữa các tầng là 280-320cm

### Các loại Cầu thang
1. **Cầu thang thẳng**: Đơn giản, không có bậc xoay hay đổi hướng
2. **Cầu thang chữ L**: Có một đoạn rẽ 90 độ, thường có chiếu nghỉ
3. **Cầu thang chữ U**: Có hai đoạn rẽ, ngược chiều nhau, thường có chiếu nghỉ
4. **Cầu thang xoắn ốc**: Xoay quanh một trục trung tâm, tiết kiệm không gian

## Hướng dẫn xây dựng Cầu thang hoàn chỉnh

### Bước 1: Xác định vị trí và loại cầu thang
- Đo chiều cao giữa các tầng
- Chọn loại cầu thang phù hợp với không gian và mục đích sử dụng

### Bước 2: Đặt sàn tầng dưới và tầng trên
- Đảm bảo sàn các tầng đã được đặt chính xác
- Kiểm tra các điểm kết nối trên sàn cho cầu thang

### Bước 3: Đặt cầu thang
- Canh chỉnh để chân cầu thang trùng với điểm kết nối trên sàn tầng dưới
- Điều chỉnh góc nghiêng và vị trí để đỉnh cầu thang trùng với sàn tầng trên

### Bước 4: Thêm lan can (nếu cần)
- Đặt các cột lan can dọc theo cạnh cầu thang
- Kết nối thanh ngang giữa các cột lan can

### Bước 5: Kiểm tra và hoàn thiện
- Đảm bảo tất cả các kết nối đều chắc chắn
- Kiểm tra không có vật cản trên đường di chuyển dọc cầu thang
