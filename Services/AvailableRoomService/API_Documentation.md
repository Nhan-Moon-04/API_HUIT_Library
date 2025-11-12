# ?? API Tìm Ki?m Phòng Tr?ng Theo Th?i Gian

## ?? T?ng quan
API tìm ki?m phòng tr?ng v?i th?i gian b?t ??u và m?c ??nh 2 gi? s? d?ng, kèm theo lo?i phòng.

---

## ?? **API Chính: Tìm ki?m phòng tr?ng**

### `POST /api/AvailableRoom/search`

**Tìm phòng tr?ng theo th?i gian và lo?i phòng**

**Request Body:**
```json
{
  "thoiGianBatDau": "2025-01-15T14:00:00",
  "maLoaiPhong": 1,
  "thoiGianSuDung": 2,
  "sucChuaToiThieu": 20
}
```

**Parameters:**
- **`thoiGianBatDau`** *(b?t bu?c)*: Th?i gian b?t ??u s? d?ng
- **`maLoaiPhong`** *(b?t bu?c)*: Mã lo?i phòng
- **`thoiGianSuDung`** *(tùy ch?n)*: S? gi? s? d?ng (m?c ??nh 2 gi?, t?i ?a 8 gi?)
- **`sucChuaToiThieu`** *(tùy ch?n)*: S?c ch?a t?i thi?u

**Response thành công:**
```json
{
  "success": true,
  "message": "Tìm th?y 3 phòng tr?ng t? 15/01/2025 14:00 ??n 15/01/2025 16:00.",
  "data": [
    {
      "maPhong": 101,
      "tenPhong": "Phòng h?c 101",
      "tenLoaiPhong": "Phòng h?c l?n",
      "sucChua": "30",
      "viTri": "T?ng 1 - Tòa A",
      "moTa": "Phòng h?c có ??y ?? thi?t b?",
      "thoiGianBatDau": "2025-01-15T14:00:00",
      "thoiGianKetThuc": "2025-01-15T16:00:00",
   "isAvailable": true,
      "thietBiChinh": ["Máy chi?u", "Máy tính", "?i?u hòa"]
    },
    {
"maPhong": 102,
      "tenPhong": "Phòng h?c 102",
      "tenLoaiPhong": "Phòng h?c l?n",
      "sucChua": "25",
      "viTri": "T?ng 1 - Tòa A",
      "moTa": "Phòng h?c âm thanh t?t",
      "thoiGianBatDau": "2025-01-15T14:00:00",
  "thoiGianKetThuc": "2025-01-15T16:00:00",
      "isAvailable": true,
      "thietBiChinh": ["Máy chi?u", "Loa", "B?ng tr?ng"]
    }
  ],
  "total": 2
}
```

**Response không có phòng:**
```json
{
  "success": true,
  "message": "Không có phòng tr?ng cho lo?i phòng này t? 15/01/2025 14:00 trong 2 gi?.",
  "data": [],
  "total": 0
}
```

---

## ? **API Ki?m tra phòng c? th?**

### `GET /api/AvailableRoom/check/{maPhong}`

**Ki?m tra m?t phòng c? th? có tr?ng không**

**URL:** `GET /api/AvailableRoom/check/101?thoiGianBatDau=2025-01-15T14:00:00&thoiGianSuDung=2`

**Parameters:**
- **`maPhong`** *(path)*: Mã phòng c?n ki?m tra
- **`thoiGianBatDau`** *(query)*: Th?i gian b?t ??u
- **`thoiGianSuDung`** *(query)*: S? gi? s? d?ng (m?c ??nh 2)

**Response:**
```json
{
  "success": true,
  "data": {
    "maPhong": 101,
    "thoiGianBatDau": "2025-01-15T14:00:00",
    "thoiGianKetThuc": "2025-01-15T16:00:00",
    "isAvailable": true
  },
  "message": "Phòng có s?n trong th?i gian này."
}
```

---

## ?? **API Danh sách lo?i phòng**

### `GET /api/AvailableRoom/room-types`

**L?y danh sách t?t c? lo?i phòng**

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "maLoaiPhong": 1,
      "tenLoaiPhong": "Phòng h?c l?n",
  "moTa": "Phòng dành cho l?p h?c ?ông ng??i",
      "soLuongChoNgoi": "30",
    "soPhongKhaDung": 5
    },
  {
      "maLoaiPhong": 2,
      "tenLoaiPhong": "Phòng h?p nhóm",
      "moTa": "Phòng nh? cho h?p nhóm",
      "soLuongChoNgoi": "10",
      "soPhongKhaDung": 8
    }
  ],
  "total": 2,
  "message": "L?y thành công 2 lo?i phòng."
}
```

---

## ?? **API Tìm ki?m nhanh**

### `GET /api/AvailableRoom/quick-search`

**Tìm ki?m nhanh v?i tham s? URL**

**URL:** `GET /api/AvailableRoom/quick-search?maLoaiPhong=1&thoiGianBatDau=2025-01-15T14:00:00&thoiGianSuDung=2&sucChuaToiThieu=20`

**Parameters:**
- **`maLoaiPhong`** *(query)*: Mã lo?i phòng
- **`thoiGianBatDau`** *(query)*: Th?i gian b?t ??u
- **`thoiGianSuDung`** *(query)*: S? gi? (m?c ??nh 2)
- **`sucChuaToiThieu`** *(query)*: S?c ch?a t?i thi?u (tùy ch?n)

---

## ?? **Ví d? s? d?ng**

### **1. Tìm phòng h?c l?n cho 25 ng??i t? 2h chi?u:**
```javascript
const findRooms = async () => {
  const response = await fetch('/api/AvailableRoom/search', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      thoiGianBatDau: '2025-01-15T14:00:00',
      maLoaiPhong: 1, // Phòng h?c l?n
      thoiGianSuDung: 2, // 2 ti?ng
      sucChuaToiThieu: 25
    })
  });
  
  const result = await response.json();
  if (result.success && result.data.length > 0) {
    console.log(`Tìm th?y ${result.total} phòng:`, result.data);
  } else {
    console.log('Không có phòng tr?ng');
  }
};
```

### **2. Ki?m tra phòng 101 có tr?ng không:**
```javascript
const checkRoom = async (roomId, startTime, hours) => {
  const url = `/api/AvailableRoom/check/${roomId}?thoiGianBatDau=${startTime}&thoiGianSuDung=${hours}`;
  const response = await fetch(url);
  const result = await response.json();
  
  return result.data.isAvailable;
};

// S? d?ng
const isAvailable = await checkRoom(101, '2025-01-15T14:00:00', 2);
console.log(isAvailable ? 'Phòng tr?ng' : 'Phòng ?ã ???c ??t');
```

### **3. Tìm ki?m nhanh v?i URL:**
```javascript
const quickSearch = () => {
  const params = new URLSearchParams({
    maLoaiPhong: 2, // Phòng h?p nhóm
    thoiGianBatDau: '2025-01-15T09:00:00',
    thoiGianSuDung: 3, // 3 ti?ng
    sucChuaToiThieu: 15
  });
  
  window.open(`/api/AvailableRoom/quick-search?${params}`);
};
```

### **4. Form tìm ki?m HTML:**
```html
<form id="roomSearchForm">
  <div>
    <label>Lo?i phòng:</label>
    <select id="roomType" required>
  <option value="">Ch?n lo?i phòng</option>
      <option value="1">Phòng h?c l?n</option>
      <option value="2">Phòng h?p nhóm</option>
    </select>
  </div>
  
  <div>
    <label>Th?i gian b?t ??u:</label>
    <input type="datetime-local" id="startTime" required>
  </div>
  
  <div>
    <label>Th?i gian s? d?ng (gi?):</label>
    <input type="number" id="duration" value="2" min="1" max="8" required>
  </div>
  
  <div>
    <label>S?c ch?a t?i thi?u:</label>
    <input type="number" id="minCapacity" min="1" placeholder="Tùy ch?n">
  </div>
  
  <button type="submit">Tìm phòng tr?ng</button>
</form>

<div id="results"></div>

<script>
document.getElementById('roomSearchForm').addEventListener('submit', async function(e) {
  e.preventDefault();
  
  const formData = {
    maLoaiPhong: parseInt(document.getElementById('roomType').value),
    thoiGianBatDau: document.getElementById('startTime').value,
    thoiGianSuDung: parseInt(document.getElementById('duration').value),
    sucChuaToiThieu: document.getElementById('minCapacity').value ? 
       parseInt(document.getElementById('minCapacity').value) : null
  };
  
  try {
    const response = await fetch('/api/AvailableRoom/search', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(formData)
    });
    
    const result = await response.json();
    
    if (result.success) {
      displayResults(result.data, result.message);
  } else {
      alert(result.message);
    }
} catch (error) {
    alert('L?i khi tìm ki?m phòng');
  }
});

function displayResults(rooms, message) {
  const resultsDiv = document.getElementById('results');
  
  if (rooms.length === 0) {
    resultsDiv.innerHTML = `<p>${message}</p>`;
    return;
  }
  
  let html = `<h3>${message}</h3><div class="room-list">`;
  
  rooms.forEach(room => {
    html += `
      <div class="room-card">
    <h4>${room.tenPhong} (${room.tenLoaiPhong})</h4>
     <p><strong>V? trí:</strong> ${room.viTri}</p>
        <p><strong>S?c ch?a:</strong> ${room.sucChua} ng??i</p>
      <p><strong>Th?i gian:</strong> ${new Date(room.thoiGianBatDau).toLocaleString()} - ${new Date(room.thoiGianKetThuc).toLocaleString()}</p>
        <p><strong>Thi?t b?:</strong> ${room.thietBiChinh.join(', ')}</p>
    <p>${room.moTa}</p>
        <button onclick="selectRoom(${room.maPhong})">Ch?n phòng này</button>
      </div>
    `;
  });
  
  html += '</div>';
  resultsDiv.innerHTML = html;
}

function selectRoom(roomId) {
  alert(`B?n ?ã ch?n phòng ${roomId}. Chuy?n ??n trang ??ng ký...`);
  // Redirect to booking page with selected room
  window.location.href = `/booking?roomId=${roomId}`;
}
</script>

<style>
.room-list {
  display: grid;
  gap: 15px;
  margin-top: 20px;
}

.room-card {
  border: 1px solid #ddd;
  padding: 15px;
  border-radius: 8px;
  background-color: #f9f9f9;
}

.room-card h4 {
  color: #2c5aa0;
  margin-top: 0;
}

.room-card button {
  background-color: #28a745;
  color: white;
  padding: 8px 16px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.room-card button:hover {
  background-color: #218838;
}
</style>
```

---

## ?? **??c ?i?m chính**

? **Tìm ki?m thông minh** - Lo?i b? phòng có xung ??t ??t tr??c và b?o trì  
? **M?c ??nh 2 gi?** - T? ??ng tính th?i gian k?t thúc  
? **L?c theo lo?i phòng** - Tìm ?úng lo?i phòng c?n thi?t  
? **Hi?n th? thi?t b?** - Li?t kê thi?t b? chính c?a m?i phòng  
? **Validation ch?t ch?** - Ki?m tra d? li?u ??u vào  
? **Multiple API endpoints** - POST cho search chi ti?t, GET cho tìm ki?m nhanh  
? **Error handling** - X? lý l?i t?t v?i thông báo thân thi?n  

**?? API hoàn ch?nh ?? tìm phòng tr?ng theo th?i gian và lo?i phòng v?i m?c ??nh 2 gi? s? d?ng!**