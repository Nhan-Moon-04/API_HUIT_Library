-- Test data ?? ki?m tra API m?i
-- Gi? s? b?n có các lo?i phòng sau trong database:

-- MaLoaiPhong = 1: Phòng h?c nhóm (Min: 5, Max: 7)
-- MaLoaiPhong = 2: Phòng h?i th?o (Min: 50, Max: 90)  
-- MaLoaiPhong = 3: Phòng thuy?t trình (Min: 8, Max: 20)
-- MaLoaiPhong = 4: Phòng nghiên c?u (Min: 1, Max: 15)

-- Test API calls:
-- GET /api/Room/capacity-limits/1  -> Phòng h?c nhóm
-- GET /api/Room/capacity-limits/2  -> Phòng h?i th?o
-- GET /api/Room/capacity-limits/3  -> Phòng thuy?t trình
-- GET /api/Room/capacity-limits/4  -> Phòng nghiên c?u

-- Expected responses:
/*
{
  "maLoaiPhong": 1,
  "tenLoaiPhong": "Phòng h?c nhóm",
  "sucChuaToiDa": 7,
  "soLuongToiThieu": 5,
  "soLuongToiDa": 7,
  "moTa": "Phòng h?c nhóm ch? cho phép t? 5 ??n 7 ng??i tham gia."
}
*/