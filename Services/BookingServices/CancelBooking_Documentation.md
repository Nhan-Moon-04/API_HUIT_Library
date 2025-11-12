# ?? API H?y ??ng Ký Phòng V?i Lý Do - UPDATED

## ?? API ?ã ???c c?p nh?t

### ? **API m?i (yêu c?u lý do):**
```
POST /api/v2/BookingManagement/cancel/{maDangKy}
```

---

## ?? **Request Format**

### **URL:**
```
POST /api/v2/BookingManagement/cancel/1041
```

### **Headers:**
```
Authorization: Bearer {JWT_TOKEN}
Content-Type: application/json
```

### **Request Body:**
```json
{
  "lyDoHuy": "Có vi?c ??t xu?t, không th? tham gia",
  "ghiChu": "S? ??ng ký l?i vào tu?n sau"
}
```

### **Parameters:**
- **`maDangKy`** *(path, b?t bu?c)*: Mã ??ng ký c?n h?y
- **`lyDoHuy`** *(body, b?t bu?c)*: Lý do h?y ??ng ký 
- **`ghiChu`** *(body, tùy ch?n)*: Ghi chú thêm

---

## ?? **Response**

### **Thành công:**
```json
{
  "success": true,
  "message": "H?y ??ng ký thành công. Lý do: Có vi?c ??t xu?t, không th? tham gia"
}
```

### **L?i thi?u lý do:**
```json
{
  "success": false,
  "message": "Vui lòng nh?p lý do h?y ??ng ký."
}
```

---

## ?? **Ví d? s? d?ng UPDATED**

### **cURL:**
```bash
curl -X 'POST' \
  'https://localhost:7100/api/v2/BookingManagement/cancel/1041' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer YOUR_JWT_TOKEN' \
  -H 'Content-Type: application/json' \
  -d '{
  "lyDoHuy": "Có vi?c ??t xu?t, không th? tham gia",
  "ghiChu": "S? ??ng ký l?i vào tu?n sau"
}'
```

### **JavaScript:**
```javascript
const cancelBooking = async (maDangKy, lyDoHuy, ghiChu) => {
  try {
    const response = await fetch(`/api/v2/BookingManagement/cancel/${maDangKy}`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        lyDoHuy: lyDoHuy,
        ghiChu: ghiChu
      })
    });

    const result = await response.json();
    
    if (result.success) {
      alert(result.message);
      // Refresh booking list
      loadCurrentBookings();
    } else {
   alert(`L?i: ${result.message}`);
    }
  } catch (error) {
    alert('L?i k?t n?i. Vui lòng th? l?i.');
  }
};

// S? d?ng
cancelBooking(1041, 'Có vi?c ??t xu?t', 'S? ??ng ký l?i sau');
```

### **Form HTML:**
```html
<form id="cancelForm">
  <input type="hidden" id="maDangKy" value="1041">
  
  <div class="form-group">
    <label for="lyDoHuy">Lý do h?y: *</label>
    <textarea id="lyDoHuy" required 
     placeholder="Vui lòng nh?p lý do h?y ??ng ký..."></textarea>
  </div>
  
  <div class="form-group">
    <label for="ghiChu">Ghi chú thêm:</label>
 <textarea id="ghiChu" 
          placeholder="Ghi chú b? sung (tùy ch?n)..."></textarea>
  </div>
  
  <button type="submit">H?y ??ng ký</button>
</form>

<script>
document.getElementById('cancelForm').addEventListener('submit', function(e) {
  e.preventDefault();
  
  const maDangKy = document.getElementById('maDangKy').value;
  const lyDoHuy = document.getElementById('lyDoHuy').value;
  const ghiChu = document.getElementById('ghiChu').value;
  
  if (!lyDoHuy.trim()) {
    alert('Vui lòng nh?p lý do h?y!');
 return;
  }
  
  cancelBooking(maDangKy, lyDoHuy, ghiChu);
});
</script>
```

---

**? API ?ã ???c c?p nh?t thành công ?? b?t bu?c nh?p lý do khi h?y ??ng ký!**

**?? Format m?i: `POST /api/v2/BookingManagement/cancel/{maDangKy}` v?i lý do trong request body.**