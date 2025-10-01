# HUIT Library API - Authentication System

## T?ng quan h? th?ng

H? th?ng xác th?c cho th? vi?n HUIT v?i 4 lo?i vai trò:
- **QUAN_TRI**: Qu?n tr? viên
- **NHAN_VIEN**: Nhân viên  
- **GIANG_VIEN**: Gi?ng viên
- **SINH_VIEN**: Sinh viên

## Phân chia API

### 1. API cho UI (Web/Mobile) - `/api/auth`
Dành cho **gi?ng viên** và **sinh viên**:
- **POST** `/api/auth/login` - ??ng nh?p
- **POST** `/api/auth/register` - ??ng ký

### 2. API cho WinForm (Qu?n lý) - `/api/adminauth`  
Dành cho **qu?n tr? viên** và **nhân viên**:
- **POST** `/api/adminauth/login` - ??ng nh?p qu?n lý

## Cách s? d?ng

### ??ng nh?p (UI)
**Sinh viên ??ng nh?p b?ng mã sinh viên:**POST /api/auth/login
Content-Type: application/json

{
  "maDangNhap": "2021603001", // Mã sinh viên
  "matKhau": "123456"
}
**Gi?ng viên ??ng nh?p b?ng mã nhân viên:**POST /api/auth/login
Content-Type: application/json

{
  "maDangNhap": "GV001", // Mã nhân viên
  "matKhau": "123456"
}
### ??ng ký sinh viên
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
**L?u ý**: Khi ??ng ký sinh viên, các tr??ng `maSinhVien`, `lop`, và `khoaHoc` là b?t bu?c.

### ??ng ký gi?ng viên
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
**L?u ý**: Khi ??ng ký gi?ng viên, tr??ng `maNhanVien` là b?t bu?c.

### ??ng nh?p qu?n lý (WinForm)POST /api/adminauth/login
Content-Type: application/json

{
  "maDangNhap": "ADMIN001", // Mã nhân viên admin
  "matKhau": "admin123"
}