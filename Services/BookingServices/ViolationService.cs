using HUIT_Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HUIT_Library.Services.BookingServices
{
    public class ViolationService : IViolationService
    {
        private readonly HuitThuVienContext _context;
        private readonly ILogger<ViolationService> _logger;

        public ViolationService(HuitThuVienContext context, ILogger<ViolationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ViolationDto>> GetUserViolationsAsync(int userId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Getting violations for user {UserId}, page {PageNumber}, size {PageSize}",
                    userId, pageNumber, pageSize);

                var query = from v in _context.ViPhams
                            join sd in _context.SuDungPhongs on v.MaSuDung equals sd.MaSuDung
                            join dk in _context.DangKyPhongs on sd.MaDangKy equals dk.MaDangKy
                            join qd in _context.QuyDinhViPhams on v.MaQuyDinh equals qd.MaQuyDinh into qdGroup
                            from quyDinh in qdGroup.DefaultIfEmpty()
                            join phong in _context.Phongs on dk.MaPhong equals phong.MaPhong into phongGroup
                            from p in phongGroup.DefaultIfEmpty()
                            where dk.MaNguoiDung == userId
                            orderby v.NgayLap descending
                            select new ViolationDto
                            {
                                MaViPham = v.MaViPham,
                                TenViPham = quyDinh != null ? quyDinh.TenViPham : "Không xác định",
                                MoTa = quyDinh != null ? quyDinh.MoTa : null,
                                NgayLap = v.NgayLap ?? DateTime.MinValue, // Fix nullable DateTime
                                TrangThaiXuLy = v.TrangThaiXuLy,
                                MaDangKy = dk.MaDangKy,
                                TenPhong = p != null ? p.TenPhong : "Chưa xác định",
                                ThoiGianViPham = sd.GioKetThucThucTe ?? sd.GioBatDauThucTe
                            };

                var result = await query
                   .Skip((pageNumber - 1) * pageSize)
              .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} violations for user {UserId}", result.Count, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting violations for user {UserId}", userId);
                return new List<ViolationDto>();
            }
        }

        public async Task<(bool HasViolations, int ViolationCount, string Message)> CheckRecentViolationsAsync(int userId, int monthsBack = 6)
        {
            try
            {
                _logger.LogInformation("Checking recent violations for user {UserId}, {MonthsBack} months back",
                   userId, monthsBack);

                var cutoff = DateTime.UtcNow.AddMonths(-monthsBack);

                var violationCount = await (from v in _context.ViPhams
                                            join sd in _context.SuDungPhongs on v.MaSuDung equals sd.MaSuDung
                                            join dk in _context.DangKyPhongs on sd.MaDangKy equals dk.MaDangKy
                                            where dk.MaNguoiDung == userId && v.NgayLap >= cutoff
                                            select v).CountAsync();

                var hasViolations = violationCount > 0;
                var message = violationCount switch
                {
                    0 => "Không có vi phạm trong thời gian gần đây.",
                    1 => "Có 1 vi phạm trong 6 tháng gần đây.",
                    <= 3 => $"Có {violationCount} vi phạm trong 6 tháng gần đây.",
                    _ => $"Có {violationCount} vi phạm trong 6 tháng gần đây. Tài khoản bị hạn chế đăng ký."
                };

                _logger.LogInformation("User {UserId} has {ViolationCount} violations in last {MonthsBack} months",
                     userId, violationCount, monthsBack);

                return (hasViolations, violationCount, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking recent violations for user {UserId}", userId);
                return (false, 0, "Không thể kiểm tra vi phạm. Vui lòng thử lại.");
            }
        }



        public async Task<ViolationDetailDto?> GetViolationDetailAsync(int userId, int maViPham)
        {
            try
            {
                _logger.LogInformation("Getting violation detail for user {UserId}, violation {MaViPham}",
       userId, maViPham);

                var query = from v in _context.ViPhams
                            join sd in _context.SuDungPhongs on v.MaSuDung equals sd.MaSuDung
                            join dk in _context.DangKyPhongs on sd.MaDangKy equals dk.MaDangKy
                            join qd in _context.QuyDinhViPhams on v.MaQuyDinh equals qd.MaQuyDinh into qdGroup
                            from quyDinh in qdGroup.DefaultIfEmpty()
                            join phong in _context.Phongs on dk.MaPhong equals phong.MaPhong into phongGroup
                            from p in phongGroup.DefaultIfEmpty()
                            where dk.MaNguoiDung == userId && v.MaViPham == maViPham
                            select new ViolationDetailDto
                            {
                                MaViPham = v.MaViPham,
                                TenViPham = quyDinh != null ? quyDinh.TenViPham : "Không xác định",
                                MoTa = quyDinh != null ? quyDinh.MoTa : null,
                                NgayLap = v.NgayLap ?? DateTime.MinValue, // Fix nullable DateTime
                                TrangThaiXuLy = v.TrangThaiXuLy,
                                MaDangKy = dk.MaDangKy,
                                TenPhong = p != null ? p.TenPhong : "Chưa xác định",
                                ThoiGianViPham = sd.GioKetThucThucTe ?? sd.GioBatDauThucTe,
                                GhiChu = v.GhiChu,
                                NguoiLapBienBan = "Hệ thống" // Only keep existing fields
                            };

                var result = await query.FirstOrDefaultAsync();

                if (result != null)
                {
                    _logger.LogInformation("Found violation detail for user {UserId}, violation {MaViPham}",
                             userId, maViPham);
                }
                else
                {
                    _logger.LogWarning("Violation {MaViPham} not found for user {UserId}", maViPham, userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting violation detail for user {UserId}, violation {MaViPham}",
       userId, maViPham);
                return null;
            }
        }

        public async Task<List<BookingViolationDto>> GetBookingViolationsAsync(int userId, int maDangKy)
        {
            try
            {
                _logger.LogInformation("Getting violations for user {UserId}, booking {MaDangKy}", userId, maDangKy);

                // Kiểm tra user có quyền xem booking này không
                var booking = await _context.DangKyPhongs
        .Where(dk => dk.MaDangKy == maDangKy && dk.MaNguoiDung == userId)
        .FirstOrDefaultAsync();

                if (booking == null)
                {
                    _logger.LogWarning("Booking {MaDangKy} not found for user {UserId}", maDangKy, userId);
                    return new List<BookingViolationDto>();
                }

                // Lấy danh sách vi phạm của phiếu đăng ký này
                var violations = await (from v in _context.ViPhams
                                        join sd in _context.SuDungPhongs on v.MaSuDung equals sd.MaSuDung
                                        join qd in _context.QuyDinhViPhams on v.MaQuyDinh equals qd.MaQuyDinh into qdGroup
                                        from quyDinh in qdGroup.DefaultIfEmpty()
                                        where sd.MaDangKy == maDangKy
                                        orderby v.NgayLap descending
                                        select new
                                        {
                                            MaViPham = v.MaViPham,
                                            TenViPham = quyDinh != null ? quyDinh.TenViPham : "Không xác định",
                                            NgayLap = v.NgayLap,
                                            TrangThaiXuLy = v.TrangThaiXuLy,
                                            MoTa = quyDinh != null ? quyDinh.MoTa : null
                                        }).ToListAsync();

                // Xử lý mô tả ngắn ở phía C#
                var result = violations.Select(v => new BookingViolationDto
                {
                    MaViPham = v.MaViPham,
                    TenViPham = v.TenViPham,
                    NgayLap = v.NgayLap,
                    TrangThaiXuLy = v.TrangThaiXuLy,
                    MoTaNgan = !string.IsNullOrEmpty(v.MoTa) && v.MoTa.Length > 100
              ? v.MoTa.Substring(0, 100) + "..."
                : v.MoTa
                }).ToList();

                _logger.LogInformation("Found {Count} violations for booking {MaDangKy}, user {UserId}",
                          result.Count, maDangKy, userId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting violations for booking {MaDangKy}, user {UserId}", maDangKy, userId);
                return new List<BookingViolationDto>();
            }
        }
    }
}