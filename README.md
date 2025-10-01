# HUIT Library API - Authentication System

## T?ng quan h? th?ng

H? th?ng x�c th?c cho th? vi?n HUIT v?i 4 lo?i vai tr�:
- **QUAN_TRI**: Qu?n tr? vi�n
- **NHAN_VIEN**: Nh�n vi�n  
- **GIANG_VIEN**: Gi?ng vi�n
- **SINH_VIEN**: Sinh vi�n

## Ph�n chia API

### 1. API cho UI (Web/Mobile) - `/api/auth`
D�nh cho **gi?ng vi�n** v� **sinh vi�n**:
- **POST** `/api/auth/login` - ??ng nh?p
- **POST** `/api/auth/register` - ??ng k�

### 2. API cho WinForm (Qu?n l�) - `/api/adminauth`  
D�nh cho **qu?n tr? vi�n** v� **nh�n vi�n**:
- **POST** `/api/adminauth/login` - ??ng nh?p qu?n l�

## C�ch s? d?ng

### ??ng nh?p (UI)
**Sinh vi�n ??ng nh?p b?ng m� sinh vi�n:**POST /api/auth/login
Content-Type: application/json

{
  "maDangNhap": "2021603001", // M� sinh vi�n
  "matKhau": "123456"
}
**Gi?ng vi�n ??ng nh?p b?ng m� nh�n vi�n:**POST /api/auth/login
Content-Type: application/json

{
  "maDangNhap": "GV001", // M� nh�n vi�n
  "matKhau": "123456"
}
### ??ng k� sinh vi�n
POST /api/auth/register
Content-Type: application/json

{
  "hoTen": "Nguy?n V?n A",
  "email": "nguyenvana@student.huit.edu.vn",
  "matKhau": "123456",
  "soDienThoai": "0901234567",
  "maSinhVien": "2021603001",
  "lop": "DHCNTT15A",
  "khoaHoc": "2021-2025",
  "ngaySinh": "2003-01-15",
  "vaiTro": "SINH_VIEN"
}
**L?u �**: Khi ??ng k� sinh vi�n, c�c tr??ng `maSinhVien`, `lop`, v� `khoaHoc` l� b?t bu?c.

### ??ng k� gi?ng vi�n
POST /api/auth/register
Content-Type: application/json

{
  "hoTen": "Nguy?n Th? B",
  "email": "nguyenthib@huit.edu.vn",
  "matKhau": "123456",
  "soDienThoai": "0901234568",
  "maNhanVien": "GV001",
  "ngaySinh": "1980-05-20",
  "vaiTro": "GIANG_VIEN"
}
**L?u �**: Khi ??ng k� gi?ng vi�n, tr??ng `maNhanVien` l� b?t bu?c.

### ??ng nh?p qu?n l� (WinForm)POST /api/adminauth/login
Content-Type: application/json

{
  "maDangNhap": "ADMIN001", // M� nh�n vi�n admin
  "matKhau": "admin123"
}