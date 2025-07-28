# WorkBase - Unity Building System

## Giới thiệu

WorkBase là một dự án Unity về hệ thống xây dựng với khả năng kết nối các thành phần building thông qua snap points. Người chơi có thể xây dựng các cấu trúc phức tạp từ các thành phần cơ bản như nền móng, tường, sàn, mái nhà và các tiện ích.

## Tính năng chính

### 🏗️ Hệ thống xây dựng
- **Snap System**: Hệ thống kết nối thông minh giữa các thành phần building
- **Đa dạng thành phần**: Nền móng, tường, sàn, mái nhà, cột dầm và tiện ích
- **Kết nối linh hoạt**: Hỗ trợ kết nối thẳng, góc 90°, góc 45°

### 📦 Quản lý tài nguyên
- **Resource System**: Hệ thống quản lý tài nguyên với ScriptableObjects
- **Storage Management**: Lưu trữ và quản lý tài nguyên trong kho
- **Resource Requirements**: Yêu cầu tài nguyên cho từng thành phần xây dựng

### 🎮 Giao diện và điều khiển
- **Custom Editor**: Editor tùy chỉnh cho quản lý storage
- **Input System**: Hệ thống input hiện đại với Input System Package
- **UI System**: Giao diện người dùng cho key binding và quản lý

## Cấu trúc dự án

```
Assets/
├── Scripts/
│   ├── Building/           # Hệ thống building và snap points
│   │   ├── BuildingPiece.cs
│   │   └── SnapPoint.cs
│   ├── Player/             # Logic người chơi
│   │   └── PlayerBuilder.cs
│   ├── Storage/            # Hệ thống quản lý tài nguyên
│   │   └── BaseStorage.cs
│   ├── ScriptableObjects/  # Scriptable Objects
│   │   ├── BuildingPieceSO.cs
│   │   └── ResourceTypeSO.cs
│   ├── Input/              # Quản lý input
│   │   └── InputManager.cs
│   ├── UI/                 # Giao diện người dùng
│   │   └── KeyBindingUI.cs
│   ├── Events/             # Hệ thống events
│   │   └── EventDispatcher.cs
│   └── Editor/             # Custom editors
│       ├── BaseStorageEditor.cs
│       └── BuildingManagerWindow.cs
├── Scenes/                 # Các scene của game
├── Resources/              # Resources folder
├── Settings/               # Cài đặt dự án
└── Data/                   # Data files
```

## Hệ thống Snap Points

### Các loại kết nối được hỗ trợ:

1. **Tường - Nền móng**:
   - `WallBottom` (đáy tường, hướng xuống) ↔ `FoundationTopEdge` (cạnh trên nền, hướng lên)

2. **Tường - Tường**:
   - Thẳng hàng: `WallSide` ↔ `WallSide` (connectionType = Opposite)
   - Góc 90°: `WallSide` ↔ `WallSide` (connectionType = Perpendicular)
   - Góc 45°: `WallSide` ↔ `WallSide` (connectionType = Angle45)

3. **Sàn - Tường**:
   - `FloorEdge` (cạnh sàn, hướng ra) ↔ `WallTop` (đỉnh tường, hướng lên)

4. **Mái - Tường**:
   - `RoofBottomEdge` (cạnh dưới mái, hướng xuống) ↔ `WallTop` (đỉnh tường, hướng lên)

5. **Cửa - Tường**:
   - `DoorFrameSide` (cạnh khung cửa, hướng ra) ↔ `WallSide` (cạnh tường, hướng ra)

## Các thành phần chính

### Building Pieces
- **Foundation** (Nền móng): Nền tảng cho các cấu trúc
- **Wall** (Tường): Tường bao và phân chia không gian
- **Floor** (Sàn): Sàn nhà và platform
- **Roof** (Mái nhà): Mái che và bảo vệ
- **Pillar_and_Beam** (Cột và dầm): Cấu trúc chịu lực
- **Utility** (Tiện ích): Các thành phần bổ trợ

### Resource System
- **ResourceTypeSO**: Định nghĩa các loại tài nguyên
- **BaseStorage**: Quản lý kho lưu trữ tài nguyên
- **ResourceRequirement**: Yêu cầu tài nguyên cho building pieces

## Yêu cầu hệ thống

- **Unity Version**: 2021.3 hoặc mới hơn
- **Input System Package**: Cần thiết cho hệ thống input
- **TextMeshPro**: Cho UI text rendering

## Cài đặt và chạy

1. **Clone repository**:
   ```bash
   git clone [repository-url]
   ```

2. **Mở project trong Unity**:
   - Mở Unity Hub
   - Chọn "Open Project"
   - Navigate đến thư mục WorkBase

3. **Install dependencies**:
   - Unity sẽ tự động import các packages cần thiết
   - Đảm bảo Input System Package được cài đặt

4. **Chạy game**:
   - Mở scene chính trong Assets/Scenes/
   - Nhấn Play để test

## Hướng dẫn sử dụng Editor

### BaseStorage Editor
- Mở Inspector của GameObject có component BaseStorage
- Sử dụng section "Quản lý tài nguyên" để:
  - Xem danh sách tài nguyên hiện có
  - Chỉnh sửa số lượng tài nguyên
  - Xóa tài nguyên không cần thiết

### Building Manager Window
- Truy cập qua menu Window trong Unity Editor
- Quản lý các building pieces và cấu hình snap points

## Đóng góp

Chào mừng các đóng góp cho dự án! Vui lòng:
1. Fork repository
2. Tạo feature branch
3. Commit changes
4. Tạo Pull Request

## License

[Specify your license here]

## Liên hệ

[Thêm thông tin liên hệ của bạn]

---

**Lưu ý**: Dự án này đang trong quá trình phát triển. Một số tính năng có thể chưa hoàn thiện hoặc đang được cải tiến.
