# BookingServices Module Structure

## ?? C?u trúc th? m?c Services/BookingServices/

### ?? M?c ?ích
Tách BookingService ban ??u (600+ dòng) thành các module nh?, d? qu?n lý theo ch?c n?ng c? th?.

### ?? Các module chính:

#### 1. **BookingManagementService** - Qu?n lý ??t phòng
- ? T?o yêu c?u ??t phòng (`CreateBookingRequestAsync`)
- ? Gia h?n ??t phòng (`ExtendBookingAsync`) 
- ? Xác nh?n tr? phòng (`CompleteBookingAsync`)
- ? H?y ??t phòng (`CancelBookingAsync`)

#### 2. **BookingViewService** - Xem l?ch s? và tr?ng thái
- ? L?y l?ch s? ??t phòng (`GetBookingHistoryAsync`)
- ? L?y danh sách ??t phòng hi?n t?i (`GetCurrentBookingsAsync`)
- ? Chi ti?t m?t ??t phòng (`GetBookingDetailsAsync`)
- ? Tìm ki?m l?ch ??t phòng (`SearchBookingHistoryAsync`)

#### 3. **ViolationService** - Qu?n lý vi ph?m
- ? L?y danh sách vi ph?m (`GetUserViolationsAsync`)
- ? Ki?m tra vi ph?m g?n ?ây (`CheckRecentViolationsAsync`)
- ? Chi ti?t vi ph?m (`GetViolationDetailAsync`)

#### 4. **RoomUsageService** - Qu?n lý s? d?ng phòng
- ? B?t ??u s? d?ng phòng / Check-in (`StartRoomUsageAsync`)
- ? Tr?ng thái s? d?ng phòng (`GetRoomUsageStatusAsync`)
- ? C?p nh?t tình tr?ng phòng (`UpdateRoomConditionAsync`)
- ? L?ch s? s? d?ng phòng (`GetRoomUsageHistoryAsync`)

## ?? API Endpoints (v2)

### BookingManagement (`/api/v2/BookingManagement`)
```
POST /create         - T?o ??t phòng
POST /extend     - Gia h?n phòng
POST /complete/{id}   - Tr? phòng
POST /cancel/{id}         - H?y ??t phòng
```

### BookingView (`/api/v2/BookingView`)
```
GET /history        - L?ch s? ??t phòng
GET /current             - ??t phòng hi?n t?i  
GET /details/{id}        - Chi ti?t ??t phòng
GET /search?term=...     - Tìm ki?m l?ch s?
```

### Violation (`/api/v2/Violation`)
```
GET /my-violations       - Vi ph?m c?a tôi
GET /recent-check        - Ki?m tra vi ph?m g?n ?ây
GET /details/{id}        - Chi ti?t vi ph?m
```

### RoomUsage (`/api/v2/RoomUsage`)
```
POST /start/{id}         - Check-in phòng
GET /status/{id}         - Tr?ng thái s? d?ng
PUT /update-condition/{id} - C?p nh?t tình tr?ng
GET /history             - L?ch s? s? d?ng
```

## ?? Dependency Injection Setup

Trong `Program.cs`:
```csharp
// Modular booking services
builder.Services.AddScoped<IBookingManagementService, BookingManagementService>();
builder.Services.AddScoped<IBookingViewService, BookingViewService>();
builder.Services.AddScoped<IViolationService, ViolationService>();
builder.Services.AddScoped<IRoomUsageService, RoomUsageService>();

// Backward compatibility
builder.Services.AddScoped<IBookingService, BookingService>();
```

## ?? L?i ích c?a c?u trúc m?i:

### ? **Single Responsibility Principle**
- M?i service ch? ??m nh?n 1 nhóm ch?c n?ng c? th?
- Code d? ??c, d? hi?u, d? maintain

### ? **Modular và Scalable** 
- Thêm tính n?ng m?i không ?nh h??ng các module khác
- Test riêng t?ng module ??c l?p

### ? **API Versioning**
- API v2 v?i c?u trúc rõ ràng h?n
- API v1 v?n ho?t ??ng (backward compatibility)

### ? **Better Error Handling**
- Log chi ti?t cho developer
- Message thân thi?n cho user
- Consistent response format

## ?? Migration Path

1. **Phase 1**: S? d?ng song song v1 và v2 API
2. **Phase 2**: Frontend chuy?n d?n sang v2  
3. **Phase 3**: Deprecate v1 API (sau 6 tháng)

## ?? Usage Examples

### T?o ??t phòng m?i:
```javascript
// v2 API
POST /api/v2/BookingManagement/create
{
  "maLoaiPhong": 1,
  "thoiGianBatDau": "2024-01-15T09:00:00",
  "lyDo": "H?c nhóm",
  "soLuong": 5
}
```

### Check-in phòng:
```javascript
POST /api/v2/RoomUsage/start/123
```

### Xem vi ph?m g?n ?ây:
```javascript
GET /api/v2/Violation/recent-check?monthsBack=6
```

## ?? File Structure Overview:
```
Services/BookingServices/
??? IBookingManagementService.cs
??? BookingManagementService.cs
??? IBookingViewService.cs  
??? BookingViewService.cs
??? IViolationService.cs
??? ViolationService.cs
??? IRoomUsageService.cs
??? RoomUsageService.cs
??? README.md

Controllers/
??? BookingManagementController.cs (v2)
??? BookingViewController.cs (v2) 
??? ViolationController.cs (v2)
??? RoomUsageController.cs (v2)
??? BookingController.cs (v1 - deprecated)
```

**?? T?t c? ch?c n?ng c?n thi?t ?ã ???c implement và s?n sàng s? d?ng!**