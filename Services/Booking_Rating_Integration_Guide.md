# ?? L?ch s? Booking v?i Tích h?p ?ánh giá

## ?? T?ng quan

?ã c?p nh?t h? th?ng l?ch s? booking ?? hi?n th? tr?ng thái ?ánh giá phòng, cho phép ng??i dùng d? dàng theo dõi và qu?n lý ?ánh giá c?a mình.

## ?? Tính n?ng m?i

### 1. **L?ch s? Booking Enhanced**

M?i booking trong l?ch s? gi? ?ây s? hi?n th?:

- ? **Thông tin vi ph?m** (?ã có)
- ? **Thông tin ?ánh giá** (M?I)
- ? **Tr?ng thái ?ánh giá** (M?I)
- ? **Thao tác ?ánh giá** (M?I)

### 2. **Tr?ng thái ?ánh giá**

| Tr?ng thái Booking | Có th? ?ánh giá? | Hi?n th? |
|-------------------|------------------|----------|
| **?ã tr? phòng (6)** - Trong 7 ngày | ? Có | **"?ánh giá ngay"** |
| **?ã tr? phòng (6)** - ?ã ?ánh giá | ? Không | **"Xem ?ánh giá"** |
| **?ã tr? phòng (6)** - Quá 7 ngày | ? Không | **"H?t h?n ?ánh giá"** |
| **Khác (1,2,3,4,5)** | ? Không | **"Không th? ?ánh giá"** |

## ??? C?p nh?t API Response

### **BookingHistoryDto** (Updated)
```json
{
  "maDangKy": 1034,
  "tenPhong": "Phòng h?c 101",
  "trangThai": 6,
  
  // ? Thông tin ?ánh giá m?i
  "daDanhGia": false,
  "maDanhGia": null,
  "diemDanhGia": null,
  "coTheDanhGia": true,
  "trangThaiDanhGia": "?ánh giá ngay",
  "soNgayConLaiDeDanhGia": 5,
  
  // Thông tin vi ph?m (?ã có)
  "coBienBan": false,
  "soLuongBienBan": 0
}
```

### **CurrentBookingDto** (Updated)
```json
{
  "maDangKy": 1035,
  "tenPhong": "Phòng h?p A",
  "maTrangThai": 6,
  
  // ? Thông tin ?ánh giá cho booking ?ã completed
  "daDanhGia": true,
  "coTheDanhGia": false, 
  "trangThaiDanhGia": "Xem ?ánh giá"
}
```

## ?? User Experience Flow

### 1. **Xem l?ch s? booking**
```http
GET /api/Booking/history?pageNumber=1&pageSize=10
```

**Response s? có thêm:**
- `trangThaiDanhGia`: Text hi?n th? cho user
- `coTheDanhGia`: Boolean ?? enable/disable button
- `soNgayConLaiDeDanhGia`: Countdown timer

### 2. **User click "?ánh giá ngay"**
```javascript
// Frontend logic
if (booking.trangThaiDanhGia === "?ánh giá ngay") {
  // Navigate to rating form
  window.open(`/rating-form?maDangKy=${booking.maDangKy}&maPhong=${booking.maPhong}`);
}
```

### 3. **User click "Xem ?ánh giá"**
```http
GET /api/Rating/booking-rating/1034
```

**Response:**
```json
{
  "success": true,
  "hasRating": true,
  "rating": {
    "maDanhGia": 25,
    "diemDanhGia": 4,
    "noiDung": "Phòng t?t, s?ch s?",
    "ngayDanhGia": "2024-11-18T10:30:00",
"tenPhong": "Phòng h?c 101"
  }
}
```

## ?? Technical Implementation

### 1. **Database Queries Enhanced**

```sql
-- L?y booking history v?i rating info
SELECT 
    dk.*,
    p.TenPhong,
  -- Rating information
    dg.MaDanhGia,
    dg.DiemDanhGia,
    dg.NgayDanhGia,
    -- Usage information for time calculation
    sd.GioKetThucThucTe
FROM DangKyPhong dk
LEFT JOIN Phong p ON dk.MaPhong = p.MaPhong  
LEFT JOIN DanhGia dg ON dk.MaDangKy = dg.MaDangKy AND dk.MaPhong = dg.MaPhong
LEFT JOIN SuDungPhong sd ON dk.MaDangKy = sd.MaDangKy
WHERE dk.MaNguoiDung = @UserId 
  AND dk.MaTrangThai IN (3,5,6) -- History statuses
```

### 2. **Rating Status Logic**
```csharp
private async Task EnhanceBookingHistoryWithRatingsAsync(List<BookingHistoryDto> bookings)
{
    foreach (var booking in bookings)
    {
        if (booking.MaTrangThai != 6) // Not completed
     {
            booking.TrangThaiDanhGia = "Không th? ?ánh giá";
            continue;
 }

        var existingRating = await GetBookingRating(booking.MaDangKy);
  
        if (existingRating != null)
        {
 // Already rated
        booking.DaDanhGia = true;
     booking.TrangThaiDanhGia = "Xem ?ánh giá";
     }
        else
     {
      // Check time limit
     var daysSince = CalculateDaysSinceCompletion(booking);
       if (daysSince <= 7)
            {
        booking.CoTheDanhGia = true;
          booking.TrangThaiDanhGia = "?ánh giá ngay";
          booking.SoNgayConLaiDeDanhGia = 7 - daysSince;
    }
       else
            {
  booking.TrangThaiDanhGia = "H?t h?n ?ánh giá";
         }
        }
    }
}
```

## ?? Frontend Integration Guide

### 1. **Booking History Component**
```html
<div class="booking-history-item" *ngFor="let booking of bookingHistory">
  <div class="booking-info">
    <h4>{{booking.tenPhong}}</h4>
    <p>{{booking.thoiGianBatDau | date:'dd/MM/yyyy HH:mm'}}</p>
    
    <!-- Violation info -->
    <div *ngIf="booking.coBienBan" class="violation-warning">
   ?? {{booking.soLuongBienBan}} vi ph?m
    </div>
    
    <!-- ? Rating status -->
    <div class="rating-status">
      <button 
     [class]="getRatingButtonClass(booking.trangThaiDanhGia)"
        [disabled]="!booking.coTheDanhGia"
     (click)="handleRatingAction(booking)">
        {{booking.trangThaiDanhGia}}
      </button>
  
      <small *ngIf="booking.soNgayConLaiDeDanhGia > 0">
        Còn {{booking.soNgayConLaiDeDanhGia}} ngày
      </small>
    </div>
  </div>
</div>
```

### 2. **Rating Action Handler**
```typescript
handleRatingAction(booking: BookingHistoryDto) {
  switch(booking.trangThaiDanhGia) {
    case "?ánh giá ngay":
      this.openRatingForm(booking);
      break;
    case "Xem ?ánh giá":
      this.viewRating(booking.maDangKy);
      break;
  }
}

openRatingForm(booking: BookingHistoryDto) {
  // Navigate to rating form with pre-filled data
  this.router.navigate(['/rate-room'], {
    queryParams: {
      maDangKy: booking.maDangKy,
      maPhong: booking.maPhong,
 tenPhong: booking.tenPhong
    }
  });
}

async viewRating(maDangKy: number) {
  const rating = await this.ratingService.getBookingRating(maDangKy);
  // Show rating in modal or navigate to detail page
  this.showRatingModal(rating);
}
```

### 3. **CSS Styles**
```css
.rating-status button {
  padding: 8px 16px;
  border-radius: 20px;
  border: none;
  font-weight: 500;
}

.rating-status button.can-rate {
  background: #28a745;
  color: white;
}

.rating-status button.view-rating {
  background: #17a2b8;
  color: white;
}

.rating-status button.expired {
  background: #6c757d;
  color: white;
  cursor: not-allowed;
}

.rating-status small {
  display: block;
  color: #6c757d;
  margin-top: 4px;
}
```

## ?? Mobile Considerations

### Responsive Design:
```css
@media (max-width: 768px) {
  .booking-history-item {
    flex-direction: column;
  }
  
  .rating-status {
    margin-top: 10px;
 text-align: center;
  }
  
  .rating-status button {
    width: 100%;
  }
}
```

## ?? Analytics & Tracking

### 1. **Rating Completion Rate**
```sql
-- Tính t? l? ?ánh giá
SELECT 
  COUNT(CASE WHEN dg.MaDanhGia IS NOT NULL THEN 1 END) as DaDanhGia,
  COUNT(*) as TongBooking,
  ROUND(
    COUNT(CASE WHEN dg.MaDanhGia IS NOT NULL THEN 1 END) * 100.0 / COUNT(*), 2
  ) as TiLeDanhGia
FROM DangKyPhong dk
LEFT JOIN DanhGia dg ON dk.MaDangKy = dg.MaDangKy
WHERE dk.MaTrangThai = 6 
  AND dk.ThoiGianKetThuc >= DATEADD(month, -1, GETDATE())
```

### 2. **Rating Timeline**
```sql
-- Th?i gian trung bình t? tr? phòng ??n ?ánh giá
SELECT AVG(DATEDIFF(hour, sd.GioKetThucThucTe, dg.NgayDanhGia)) as AvgHoursToRate
FROM SuDungPhong sd
JOIN DanhGia dg ON sd.MaDangKy = dg.MaDangKy
WHERE dg.NgayDanhGia >= DATEADD(month, -1, GETDATE())
```

## ?? Business Benefits

1. **Increased Rating Participation**: Prominently displayed rating status
2. **Better User Experience**: Clear visual cues and easy access
3. **Timely Feedback**: Countdown timer encourages quick rating
4. **Transparency**: Users can easily track their rating history

## ?? Future Enhancements

1. **Rating Reminders**: Email/push notifications before deadline
2. **Rating Templates**: Pre-defined rating categories
3. **Photo Upload**: Allow users to attach photos to ratings
4. **Response System**: Allow staff to respond to ratings

---

## ? Implementation Status

- ? **BookingHistoryDto enhanced** with rating fields
- ? **CurrentBookingDto enhanced** with rating status  
- ? **Business logic implemented** for rating status calculation
- ? **API endpoint added** for booking-specific rating info
- ? **Time-based validation** for 7-day rule
- ? **Database queries optimized** for rating information

## ?? Ready for Frontend Integration!

H? th?ng backend ?ã s?n sàng. Frontend team có th? b?t ??u tích h?p UI components theo design guide trên! ??