# ?? API Th?ng Kê Web Th? Vi?n - Phiên B?n Chu?n

## ?? **C?p nh?t m?i:**
? **S? d?ng b?ng VisitLog** - B?ng chuyên d?ng ?? tracking l??t truy c?p  
? **Null-safe queries** - X? lý tr??ng h?p b?ng m?i ch?a có d? li?u  
? **Better error handling** - Tr? v? default values thay vì crash  
? **Proper logging** - Log chi ti?t cho debugging  

---

## ?? **API Endpoints:**

### **1. L?y th?ng kê t?ng quan**
```
GET /api/Statistics/overview
```

**Response (v?i d? li?u th?t t? VisitLog):**
```json
{
  "success": true,
  "data": {
    "tongLuotTruyCap": 1250,      // T? VisitLog table
    "soLuongOnline": 8,      // T?ng online hi?n t?i  
  "thanhVienOnline": 5,         // User có UserId trong 15p g?n ?ây
    "khachOnline": 3,             // IP không có UserId trong 15p g?n ?ây
    "trongNgay": 15,        // Visits hôm nay t? VisitLog
    "homQua": 12,                 // Visits hôm qua t? VisitLog
    "trongThang": 342  // Visits trong tháng t? VisitLog
  },
  "message": "L?y th?ng kê thành công."
}
```

### **2. Ghi nh?n l??t truy c?p** 
```
POST /api/Statistics/visit
```
- ? **T? ??ng insert vào VisitLog table**
- ? **Capture UserId n?u ?ã ??ng nh?p**
- ? **Capture IP address** 
- ? **Timestamp chính xác (gi? Vi?t Nam)**

### **3. C?p nh?t tr?ng thái online**
```
POST /api/Statistics/online-status?isOnline=true
```
- ? **Ghi vào VisitLog khi online**
- ? **C?p nh?t LastActivity n?u có column**

---

## ?? **Database Schema:**

**VisitLog Table Structure:**
```sql
CREATE TABLE VisitLog (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,     -- NULL n?u là khách
    IPAddress NVARCHAR(45) NULL,        -- IPv4/IPv6
    VisitTime DATETIME NOT NULL,      -- Th?i gian visit
    FOREIGN KEY (UserId) REFERENCES NguoiDung(MaNguoiDung)
);
```

**Queries ???c s? d?ng:**
```sql
-- 1. T?ng l??t truy c?p
SELECT COUNT(*) FROM VisitLog

-- 2. Thành viên online (15 phút g?n ?ây)
SELECT COUNT(DISTINCT UserId) 
FROM VisitLog 
WHERE UserId IS NOT NULL 
AND VisitTime >= DATEADD(MINUTE, -15, GETDATE())

-- 3. Khách online (15 phút g?n ?ây)  
SELECT COUNT(DISTINCT IPAddress)
FROM VisitLog
WHERE UserId IS NULL
AND IPAddress IS NOT NULL
AND VisitTime >= DATEADD(MINUTE, -15, GETDATE())

-- 4. Visits trong ngày
SELECT COUNT(*) 
FROM VisitLog
WHERE CAST(VisitTime AS DATE) = CAST(GETDATE() AS DATE)
```

---

## ?? **Implementation Frontend:**

```javascript
// 1. Load th?ng kê v?i error handling
const loadStatistics = async () => {
  try {
    const response = await fetch('/api/Statistics/overview');
    const result = await response.json();
    
    if (result.success) {
      updateStatisticsDisplay(result.data);
    } else {
      console.warn('Th?ng kê không kh? d?ng:', result.message);
      showDefaultStatistics();
    }
  } catch (error) {
    console.error('L?i load th?ng kê:', error);
    showDefaultStatistics();
  }
};

// 2. Ghi nh?n visit khi vào trang (fire and forget)
const recordVisit = async () => {
  try {
    await fetch('/api/Statistics/visit', { 
method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      }
    });
    console.log('? Visit recorded');
  } catch (error) {
    console.log('?? Could not record visit (offline?)');
  }
};

// 3. Auto-refresh th?ng kê
let statsInterval;

const startStatisticsUpdates = () => {
  // Load ngay l?p t?c
  loadStatistics();
  
  // Refresh m?i 60 giây
  statsInterval = setInterval(loadStatistics, 60000);
};

const stopStatisticsUpdates = () => {
  if (statsInterval) {
    clearInterval(statsInterval);
  }
};

// 4. Hi?n th? v?i fallback
function updateStatisticsDisplay(stats) {
  document.getElementById('tongTruyCap').textContent = 
    stats.tongLuotTruyCap?.toLocaleString() || '0';
    
  document.getElementById('soOnline').textContent = 
    stats.soLuongOnline || '0';
    
  document.getElementById('thanhVienOnline').textContent = 
    stats.thanhVienOnline || '0';
    
  document.getElementById('khachOnline').textContent = 
    stats.khachOnline || '0';
 
  document.getElementById('trongNgay').textContent = 
    stats.trongNgay || '0';
    
  document.getElementById('homQua').textContent = 
    stats.homQua || '0';
    
  document.getElementById('trongThang').textContent = 
  stats.trongThang || '0';
}

function showDefaultStatistics() {
  updateStatisticsDisplay({
    tongLuotTruyCap: 0,
    soLuongOnline: 0,
 thanhVienOnline: 0,
    khachOnline: 0,
    trongNgay: 0,
    homQua: 0,
  trongThang: 0
  });
}

// 5. Page lifecycle
window.addEventListener('load', () => {
  recordVisit();  // Ghi nh?n visit
  startStatisticsUpdates();   // B?t ??u auto-refresh
});

window.addEventListener('beforeunload', () => {
  stopStatisticsUpdates();    // D?ng auto-refresh
});

// 6. Update online status khi login/logout
const updateOnlineStatus = async (isOnline) => {
  try {
    await fetch(`/api/Statistics/online-status?isOnline=${isOnline}`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${getJWTToken()}`,
        'Content-Type': 'application/json'
      }
    });
    console.log(`? Online status: ${isOnline ? 'online' : 'offline'}`);
  } catch (error) {
    console.log('?? Could not update online status');
  }
};

// G?i khi login thành công
// updateOnlineStatus(true);

// G?i khi logout
// updateOnlineStatus(false);
```

---

## ?? **C?i ti?n so v?i phiên b?n c?:**

### ? **Database Design:**
- **VisitLog table** chuyên d?ng thay vì dùng b?ng khác
- **Proper indexing** cho performance queries
- **Null-safe** cho các tr??ng h?p edge case

### ? **Code Quality:**
- **Null-safe operators** (`??`) cho t?t c? queries
- **Try-catch** riêng cho t?ng operation
- **Detailed logging** ?? debug d? dàng
- **Graceful degradation** khi có l?i

### ? **Performance:**
- **Efficient queries** v?i proper WHERE clauses
- **Memory caching** s?n sàng (?ã có IMemoryCache)
- **Connection management** t?i ?u v?i using statements

### ? **Reliability:**
- **Default fallback values** thay vì crash
- **Robust error handling** 
- **Backwards compatible** v?i database c?

**?? API Statistics gi? ?ây hoàn toàn chu?n và s?n sàng production!**