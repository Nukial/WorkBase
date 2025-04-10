# Quy Ước Snap Point Cho Hệ Thống Building

Tài liệu này định nghĩa các quy ước chuẩn cho việc đặt và cấu hình Snap Point cho từng loại Building Piece. Tuân thủ các quy ước này sẽ đảm bảo các mảnh ghép hoạt động nhất quán và kết nối chính xác với nhau.

## Quy ước chung

- **Pivot Point**: Trừ khi có ghi chú khác, pivot point nên đặt tại góc dưới cùng (Min X, Min Y, Min Z) của đối tượng.
- **Tên điểm snap**: Nên đặt theo format `SnapPoint_{PointType}_{Direction}` để dễ quản lý.
- **Scale**: Tất cả prefab nên dùng scale (1,1,1). Điều chỉnh kích thước qua model.
- **Kiểm tra kết nối**: Sử dụng SnapPointEditor để kiểm tra các kết nối.

## Foundation (Móng - Khối vuông/chữ nhật)

### Pivot
- Góc đáy (Min X, Min Y, Min Z)

### SnapPoints

#### FoundationTopCorner (4 điểm)
- **Vị trí**: Các góc trên mặt trên cùng
- **snapDirection**: Up (+Y)
- **connectionType**: Opposite hoặc Perpendicular
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() {
      SnapType.WallBottom,
      SnapType.FloorCorner, 
      SnapType.PillarBottom,
      SnapType.StairBottom
  }
  ```

#### FoundationTopEdge (4 điểm)
- **Vị trí**: Trung điểm các cạnh trên mặt trên cùng
- **snapDirection**: Up (+Y)
- **connectionType**: Opposite
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() {
      SnapType.WallBottom,
      SnapType.FloorEdge
  }
  ```

#### FoundationSide (Tùy chọn, 4+ điểm)
- **Vị trí**: Các cạnh/góc mặt bên
- **snapDirection**: Hướng ra ngoài (Left/Right/Forward/Back)
- **connectionType**: Opposite
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() { SnapType.FoundationSide }
  ```

## Wall (Tường - Tấm phẳng)

### Pivot
- Góc đáy (Min X, Min Y, Min Z)

### SnapPoints

#### WallBottom (1-2+ điểm)
- **Vị trí**: Dọc theo cạnh dưới cùng (ít nhất 2 góc, có thể thêm điểm giữa)
- **snapDirection**: Down (-Y)
- **connectionType**: Opposite
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() {
      SnapType.FoundationTopEdge,
      SnapType.FoundationTopCorner,
      SnapType.FloorEdge,
      SnapType.WallTop
  }
  ```

#### WallTop (1-2+ điểm)
- **Vị trí**: Dọc theo cạnh trên cùng (tương tự đáy)
- **snapDirection**: Up (+Y)
- **connectionType**: Opposite
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() {
      SnapType.WallBottom,
      SnapType.RoofBottomEdge,
      SnapType.FloorEdge,
      SnapType.BeamEnd
  }
  ```

#### WallSide (2 điểm)
- **Vị trí**: Trung điểm chiều cao của hai cạnh đứng hai bên
- **snapDirection**: Hướng ra ngoài cạnh (Left/Right)
- **connectionType**: Perpendicular (quan trọng cho việc tạo góc 90 độ)
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() {
      SnapType.WallSide,
      SnapType.FloorEdge,
      SnapType.BeamEnd,
      SnapType.DoorFrameSide,
      SnapType.WindowFrameSide
  }
  ```

#### WallSurfaceMount (Tùy chọn, 1+ điểm)
- **Vị trí**: Trên mặt phẳng của tường
- **snapDirection**: Hướng ra ngoài mặt tường (Forward/Back)
- **connectionType**: Opposite hoặc Any
- **acceptedTypes**: Các điểm tương ứng trên đồ trang trí

## Floor/Ceiling (Sàn/Trần - Tấm phẳng)

### Pivot
- Góc đáy hoặc tâm đáy

### SnapPoints

#### FloorEdge (4+ điểm)
- **Vị trí**: Dọc các cạnh trên mặt trên cùng (góc và/hoặc trung điểm)
- **snapDirection & connectionType**: Phụ thuộc vào kết nối mong muốn:
  - **Nối sàn-sàn**: snapDirection hướng ra cạnh (L/R/F/B), connectionType = Opposite
  - **Nối sàn-cạnh tường**: snapDirection hướng ra cạnh (L/R/F/B), connectionType = Perpendicular
  - **Làm sàn (đặt trên WallTop)**: snapDirection = Up (+Y), connectionType = Opposite
  - **Làm trần (đặt dưới WallBottom)**: snapDirection = Down (-Y), connectionType = Opposite
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() {
      SnapType.FloorEdge,
      SnapType.WallBottom,
      SnapType.WallTop,
      SnapType.WallSide,
      SnapType.StairTop,
      SnapType.StairBottom,
      SnapType.PillarTop
  }
  ```

#### FloorCorner (4 điểm)
- **Vị trí**: Các góc trên mặt trên cùng
- Logic tương tự FloorEdge nhưng cho các mảnh ghép góc
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() {
      SnapType.FloorCorner,
      SnapType.WallSide,
      SnapType.PillarTop
  }
  ```

#### CeilingMount/FloorMount (Tùy chọn)
- **Vị trí**: Trên bề mặt trần/sàn
- **snapDirection**: Down/Up
- **connectionType**: Any hoặc Opposite

## Roof (Mái - Thường là dốc)

### Pivot
- Góc dưới cùng của cạnh tiếp xúc tường

### SnapPoints

#### RoofBottomEdge (Nhiều điểm)
- **Vị trí**: Dọc theo cạnh dưới cùng (nơi đặt lên tường)
- **snapDirection**: Hướng xuống/hơi ra ngoài
- **connectionType**: Opposite (so với WallTop)
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() { SnapType.WallTop }
  ```

#### RoofRidge (Nhiều điểm)
- **Vị trí**: Dọc theo đỉnh mái
- **snapDirection**: Hướng lên/ra ngoài
- **connectionType**: Opposite
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() { SnapType.RoofRidge }
  ```

#### RoofGableEdge (Nhiều điểm)
- **Vị trí**: Dọc theo cạnh dốc ở đầu hồi
- **snapDirection**: Hướng ra ngoài, vuông góc với cạnh
- **connectionType**: Opposite hoặc Perpendicular
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() {
      SnapType.RoofGableEdge,
      SnapType.WallSide
  }
  ```

## Pillar/Beam (Cột/Dầm)

### Pivot
- Tâm đáy (Cột) hoặc tâm một đầu (Dầm)

### SnapPoints

#### PillarBottom/BeamEnd (1 điểm)
- **Vị trí**: Tâm mặt đáy/đầu dầm
- **snapDirection**: Down (-Y) / Ra ngoài theo trục dầm
- **connectionType**: Opposite
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() {
      SnapType.FoundationTopEdge,
      SnapType.FoundationTopCorner,
      SnapType.FloorEdge,
      SnapType.FloorCorner,
      SnapType.WallTop,
      SnapType.PillarTop,
      SnapType.BeamEnd
  }
  ```

#### PillarTop/BeamEnd (1 điểm)
- **Vị trí**: Tâm mặt đỉnh/đầu còn lại
- **snapDirection**: Up (+Y) / Ra ngoài theo trục dầm
- **connectionType**: Opposite
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() {
      SnapType.WallBottom,
      SnapType.FloorEdge,
      SnapType.BeamEnd,
      SnapType.PillarBottom
  }
  ```

## Stairs (Cầu Thang)

### Pivot
- Góc/cạnh dưới cùng của bậc thang đầu tiên

### SnapPoints

#### StairBottom (1+ điểm)
- **Vị trí**: Cạnh/góc dưới cùng của cầu thang
- **snapDirection**: Down (-Y) hoặc Forward (theo hướng đi lên)
- **connectionType**: Opposite
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() {
      SnapType.FoundationTopEdge,
      SnapType.FoundationTopCorner,
      SnapType.FloorEdge
  }
  ```

#### StairTop (1+ điểm)
- **Vị trí**: Cạnh/góc trên cùng của cầu thang (mặt sàn trên)
- **snapDirection**: Up (+Y) hoặc Forward
- **connectionType**: Opposite
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() { SnapType.FloorEdge }
  ```

## Door/Window (Cửa/Cửa sổ)

### Pivot
- Góc đáy của khung

### SnapPoints

#### DoorFrameSide/WindowFrameSide (2+ điểm)
- **Vị trí**: Cạnh bên của khung cửa/cửa sổ
- **snapDirection**: Hướng vào trong khung
- **connectionType**: Opposite
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() { SnapType.WallSide }
  ```

#### DoorFrameTop/WindowFrameTop (1+ điểm)
- **Vị trí**: Cạnh trên của khung cửa/cửa sổ
- **snapDirection**: Hướng vào trong khung hoặc Up
- **connectionType**: Opposite
- **acceptedTypes**:
  ```csharp
  new List<SnapType>() {
      SnapType.WallSide,
      SnapType.BeamEnd
  }
  ```

## Lưu ý quan trọng

1. Đây là những quy ước gợi ý "tốt nhất" cho các trường hợp phổ biến. Bạn có thể điều chỉnh dựa trên thiết kế cụ thể.

2. Luôn kiểm tra trực quan bằng Gizmos trong Scene View để đảm bảo vị trí và hướng snapDirection là chính xác.

3. Kiểm tra thực tế: Sau khi cấu hình, hãy thử nghiệm việc snap các loại mảnh ghép với nhau để đảm bảo chúng hoạt động như mong đợi.

4. Đối với kết nối phức tạp (như góc nhà), có thể cần sử dụng nhiều điểm snap với các hướng và loại kết nối khác nhau.

5. autoAdjustConnection = true có thể hữu ích cho các điểm snap linh hoạt, nhưng hãy thận trọng và đảm bảo allowedConnectionTypes được thiết lập đúng.
