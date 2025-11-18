using HUIT_Library.DTOs.Request;
using HUIT_Library.DTOs.Response;
using HUIT_Library.Models;
using HUIT_Library.Services.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HUIT_Library.Services
{
    public class RatingService : IRatingService
    {
        private readonly HuitThuVienContext _context;
        private readonly ILogger<RatingService> _logger;

        // Trạng thái đăng ký phòng
        private const int STATUS_PENDING = 1;      // Chờ duyệt
        private const int STATUS_APPROVED = 2;     // Đã duyệt
        private const int STATUS_REJECTED = 3;     // Từ chối
        private const int STATUS_INUSE = 4;    // Đang sử dụng
        private const int STATUS_CANCELLED = 5;    // Hủy
        private const int STATUS_COMPLETED = 6;    // Đã trả phòng

        // Helper method to get Vietnam timezone
        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        private DateTime GetVietnamTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }

        public RatingService(HuitThuVienContext context, ILogger<RatingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, int? MaDanhGia)> CreateRatingAsync(int userId, CreateRatingRequest request)
        {
            try
            {
                _logger.LogInformation("User {UserId} creating rating for room {RoomId} with booking {BookingId}",
      userId, request.MaDoiTuong, request.MaDangKy);

                // 1️⃣ Validate input - chỉ hỗ trợ đánh giá phòng với model mới
                var validationResult = ValidateRatingRequest(request);
                if (!validationResult.IsValid)
                    return (false, validationResult.Message, null);

                // 2️⃣ Kiểm tra quyền đánh giá
                var canRate = await CanUserRateAsync(userId, "PHONG", request.MaDoiTuong, request.MaDangKy);
                if (!canRate.CanRate)
                    return (false, canRate.Message, null);

                // 3️⃣ Kiểm tra đã đánh giá chưa
                var existingRating = await _context.DanhGia
                .FirstOrDefaultAsync(d => d.MaNguoiDung == userId &&
               d.MaPhong == request.MaDoiTuong &&
             d.MaDangKy == request.MaDangKy);

                if (existingRating != null)
                    return (false, "Bạn đã đánh giá phòng này rồi. Vui lòng sử dụng chức năng chỉnh sửa.", null);

                // 4️⃣ Tạo đánh giá mới
                var rating = new DanhGium
                {
                    MaNguoiDung = userId,
                    MaDangKy = request.MaDangKy ?? 0,
                    MaPhong = request.MaDoiTuong,
                    DiemDanhGia = (byte)request.DiemDanhGia,
                    NoiDung = request.NoiDung?.Trim(),
                    NgayDanhGia = GetVietnamTime()
                };

                _context.DanhGia.Add(rating);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created rating {RatingId} for user {UserId}", rating.MaDanhGia, userId);
                return (true, "Cảm ơn bạn đã đánh giá! Ý kiến của bạn rất quan trọng với chúng tôi.", rating.MaDanhGia);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rating for user {UserId}", userId);
                return (false, "Đã xảy ra lỗi khi gửi đánh giá. Vui lòng thử lại.", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateRatingAsync(int userId, int maDanhGia, UpdateRatingRequest request)
        {
            try
            {
                _logger.LogInformation("User {UserId} updating rating {RatingId}", userId, maDanhGia);

                // 1️⃣ Validate điểm đánh giá
                if (request.DiemDanhGia < 1 || request.DiemDanhGia > 5)
                    return (false, "Điểm đánh giá phải từ 1 đến 5 sao.");

                // 2️⃣ Tìm đánh giá
                var rating = await _context.DanhGia.FindAsync(maDanhGia);
                if (rating == null)
                    return (false, "Không tìm thấy đánh giá.");

                if (rating.MaNguoiDung != userId)
                    return (false, "Bạn không có quyền chỉnh sửa đánh giá này.");

                // 3️⃣ Kiểm tra thời gian cập nhật dựa trên thời gian trả phòng
                var canUpdate = await CanUpdateRatingAsync(userId, rating);
                if (!canUpdate.CanUpdate)
                    return (false, canUpdate.Message);

                // 4️⃣ Cập nhật
                rating.DiemDanhGia = (byte)request.DiemDanhGia;
                rating.NoiDung = request.NoiDung?.Trim();

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated rating {RatingId} for user {UserId}", maDanhGia, userId);
                return (true, "Đánh giá đã được cập nhật thành công!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rating {RatingId} for user {UserId}", maDanhGia, userId);
                return (false, "Đã xảy ra lỗi khi cập nhật đánh giá. Vui lòng thử lại.");
            }
        }

        public async Task<List<RatingDto>> GetUserRatingsAsync(int userId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Getting ratings for user {UserId}, page {PageNumber}", userId, pageNumber);

                var query = from r in _context.DanhGia
                            join u in _context.NguoiDungs on r.MaNguoiDung equals u.MaNguoiDung
                            join p in _context.Phongs on r.MaPhong equals p.MaPhong
                            join dk in _context.DangKyPhongs on r.MaDangKy equals dk.MaDangKy
                            where r.MaNguoiDung == userId
                            orderby r.NgayDanhGia descending
                            select new RatingDto
                            {
                                MaDanhGia = r.MaDanhGia,
                                MaNguoiDung = r.MaNguoiDung,
                                TenNguoiDung = u.HoTen,
                                LoaiDoiTuong = "PHONG",
                                MaDoiTuong = r.MaPhong,
                                TenDoiTuong = p.TenPhong,
                                DiemDanhGia = r.DiemDanhGia,
                                NoiDung = r.NoiDung,
                                NgayDanhGia = r.NgayDanhGia,
                                CoTheChinhSua = false // Sẽ được tính sau
                            };

                var result = await query
                 .Skip((pageNumber - 1) * pageSize)
                  .Take(pageSize)
                 .ToListAsync();

                // Kiểm tra quyền chỉnh sửa
                await SetEditPermissions(userId, result);

                _logger.LogInformation("Found {Count} ratings for user {UserId}", result.Count, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ratings for user {UserId}", userId);
                return new List<RatingDto>();
            }
        }

        public async Task<PagedRatingResponse> GetRatingsByObjectAsync(string loaiDoiTuong, int maDoiTuong, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Getting ratings for room {RoomId}, page {PageNumber}", maDoiTuong, pageNumber);

                // Model mới chỉ hỗ trợ đánh giá phòng
                if (loaiDoiTuong.ToUpper() != "PHONG")
                    return new PagedRatingResponse();

                var query = from r in _context.DanhGia
                            join u in _context.NguoiDungs on r.MaNguoiDung equals u.MaNguoiDung
                            join p in _context.Phongs on r.MaPhong equals p.MaPhong
                            where r.MaPhong == maDoiTuong
                            orderby r.NgayDanhGia descending
                            select new RatingDto
                            {
                                MaDanhGia = r.MaDanhGia,
                                MaNguoiDung = r.MaNguoiDung,
                                TenNguoiDung = u.HoTen,
                                LoaiDoiTuong = "PHONG",
                                MaDoiTuong = r.MaPhong,
                                TenDoiTuong = p.TenPhong,
                                DiemDanhGia = r.DiemDanhGia,
                                NoiDung = r.NoiDung,
                                NgayDanhGia = r.NgayDanhGia,
                                CoTheChinhSua = false // User khác không thể chỉnh sửa
                            };

                var totalCount = await query.CountAsync();
                var ratings = await query
                     .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                        .ToListAsync();

                // Lấy thống kê
                var statistics = await GetRatingStatisticsAsync("PHONG", maDoiTuong);

                return new PagedRatingResponse
                {
                    Data = ratings,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    ThongKe = statistics
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ratings for room {RoomId}", maDoiTuong);
                return new PagedRatingResponse();
            }
        }

        public async Task<RatingStatisticsDto?> GetRatingStatisticsAsync(string loaiDoiTuong, int maDoiTuong)
        {
            try
            {
                if (loaiDoiTuong.ToUpper() != "PHONG")
                    return null;

                var ratings = await _context.DanhGia
         .Where(r => r.MaPhong == maDoiTuong)
              .ToListAsync();

                if (!ratings.Any())
                    return null;

                var roomName = await _context.Phongs
       .Where(p => p.MaPhong == maDoiTuong)
            .Select(p => p.TenPhong)
             .FirstOrDefaultAsync();

                var stats = new RatingStatisticsDto
                {
                    LoaiDoiTuong = "PHONG",
                    MaDoiTuong = maDoiTuong,
                    TenDoiTuong = roomName,
                    TongSoDanhGia = ratings.Count,
                    DiemTrungBinh = Math.Round(ratings.Average(r => (double)r.DiemDanhGia), 1),
                    Sao1 = ratings.Count(r => r.DiemDanhGia == 1),
                    Sao2 = ratings.Count(r => r.DiemDanhGia == 2),
                    Sao3 = ratings.Count(r => r.DiemDanhGia == 3),
                    Sao4 = ratings.Count(r => r.DiemDanhGia == 4),
                    Sao5 = ratings.Count(r => r.DiemDanhGia == 5)
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rating statistics for room {RoomId}", maDoiTuong);
                return null;
            }
        }

        public async Task<PagedRatingResponse> SearchRatingsAsync(RatingFilterRequest filter, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Searching ratings with filter, page {PageNumber}", pageNumber);

                var query = from r in _context.DanhGia
                            join u in _context.NguoiDungs on r.MaNguoiDung equals u.MaNguoiDung
                            join p in _context.Phongs on r.MaPhong equals p.MaPhong
                            select new { r, u, p };

                // Apply filters
                if (filter.MaDoiTuong.HasValue)
                    query = query.Where(x => x.r.MaPhong == filter.MaDoiTuong.Value);

                if (filter.DiemToiThieu.HasValue)
                    query = query.Where(x => x.r.DiemDanhGia >= filter.DiemToiThieu.Value);

                if (filter.DiemToiDa.HasValue)
                    query = query.Where(x => x.r.DiemDanhGia <= filter.DiemToiDa.Value);

                if (filter.TuNgay.HasValue)
                    query = query.Where(x => x.r.NgayDanhGia >= filter.TuNgay.Value);

                if (filter.DenNgay.HasValue)
                    query = query.Where(x => x.r.NgayDanhGia <= filter.DenNgay.Value);

                var orderedQuery = query.OrderByDescending(x => x.r.NgayDanhGia);

                var totalCount = await orderedQuery.CountAsync();
                var results = await orderedQuery
                   .Skip((pageNumber - 1) * pageSize)
               .Take(pageSize)
             .Select(x => new RatingDto
             {
                 MaDanhGia = x.r.MaDanhGia,
                 MaNguoiDung = x.r.MaNguoiDung,
                 TenNguoiDung = x.u.HoTen,
                 LoaiDoiTuong = "PHONG",
                 MaDoiTuong = x.r.MaPhong,
                 TenDoiTuong = x.p.TenPhong,
                 DiemDanhGia = x.r.DiemDanhGia,
                 NoiDung = x.r.NoiDung,
                 NgayDanhGia = x.r.NgayDanhGia,
                 CoTheChinhSua = false
             })
             .ToListAsync();

                return new PagedRatingResponse
                {
                    Data = results,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching ratings with filter");
                return new PagedRatingResponse();
            }
        }

        public async Task<RatingDetailDto?> GetRatingDetailAsync(int maDanhGia)
        {
            try
            {
                var rating = await (from r in _context.DanhGia
                                    join u in _context.NguoiDungs on r.MaNguoiDung equals u.MaNguoiDung
                                    join p in _context.Phongs on r.MaPhong equals p.MaPhong
                                    join dk in _context.DangKyPhongs on r.MaDangKy equals dk.MaDangKy
                                    where r.MaDanhGia == maDanhGia
                                    select new RatingDetailDto
                                    {
                                        MaDanhGia = r.MaDanhGia,
                                        MaNguoiDung = r.MaNguoiDung,
                                        TenNguoiDung = u.HoTen,
                                        LoaiDoiTuong = "PHONG",
                                        MaDoiTuong = r.MaPhong,
                                        TenDoiTuong = p.TenPhong,
                                        DiemDanhGia = r.DiemDanhGia,
                                        NoiDung = r.NoiDung,
                                        NgayDanhGia = r.NgayDanhGia,
                                        CoTheChinhSua = false,
                                        MaDangKy = r.MaDangKy,
                                        NgaySuDung = dk.ThoiGianBatDau,
                                        DaXacThuc = true
                                    }).FirstOrDefaultAsync();

                if (rating != null)
                {
                    // Kiểm tra quyền chỉnh sửa
                    var canUpdate = await CanUpdateRatingByIdAsync(rating.MaNguoiDung, maDanhGia);
                    rating.CoTheChinhSua = canUpdate.CanUpdate;
                }

                return rating;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rating detail for {RatingId}", maDanhGia);
                return null;
            }
        }

        public async Task<(bool CanRate, string Message)> CanUserRateAsync(int userId, string loaiDoiTuong, int maDoiTuong, int? maDangKy = null)
        {
            try
            {
                // Model mới chỉ hỗ trợ đánh giá phòng
                if (loaiDoiTuong.ToUpper() != "PHONG")
                    return (false, "Hiện tại chỉ hỗ trợ đánh giá phòng.");

                if (!maDangKy.HasValue)
                    return (false, "Cần có mã đăng ký để đánh giá phòng.");

                // Kiểm tra user đã trả phòng này chưa và trong thời gian cho phép
                var booking = await _context.DangKyPhongs
            .FirstOrDefaultAsync(dk => dk.MaDangKy == maDangKy.Value &&
            dk.MaNguoiDung == userId &&
                 dk.MaPhong == maDoiTuong &&
              dk.MaTrangThai == STATUS_COMPLETED); // Đã trả phòng

                if (booking == null)
                    return (false, "Bạn chỉ có thể đánh giá phòng sau khi đã sử dụng và trả phòng.");

                // Kiểm tra thời gian - chỉ cho phép đánh giá trong vòng 1 tuần sau khi trả phòng
                var usage = await _context.SuDungPhongs
                    .FirstOrDefaultAsync(su => su.MaDangKy == maDangKy.Value);

                if (usage?.GioKetThucThucTe == null)
                    return (false, "Không tìm thấy thông tin sử dụng phòng.");

                var daysSinceCompleted = (GetVietnamTime() - usage.GioKetThucThucTe.Value).TotalDays;
                if (daysSinceCompleted > 7)
                    return (false, $"Chỉ có thể đánh giá trong vòng 1 tuần sau khi trả phòng. Bạn đã trả phòng {daysSinceCompleted:0} ngày trước.");

                // Kiểm tra phòng có tồn tại không
                var roomExists = await _context.Phongs.AnyAsync(p => p.MaPhong == maDoiTuong);
                if (!roomExists)
                    return (false, "Phòng không tồn tại.");

                return (true, "Có thể đánh giá.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} can rate room {RoomId}", userId, maDoiTuong);
                return (false, "Không thể kiểm tra quyền đánh giá.");
            }
        }

        // Helper methods
        private (bool IsValid, string Message) ValidateRatingRequest(CreateRatingRequest request)
        {
            // Model mới chỉ hỗ trợ đánh giá phòng
            if (request.LoaiDoiTuong.ToUpper() != "PHONG")
                return (false, "Hiện tại chỉ hỗ trợ đánh giá phòng.");

            if (request.MaDoiTuong <= 0)
                return (false, "Mã phòng không hợp lệ.");

            if (!request.MaDangKy.HasValue || request.MaDangKy.Value <= 0)
                return (false, "Cần có mã đăng ký để đánh giá phòng.");

            if (request.DiemDanhGia < 1 || request.DiemDanhGia > 5)
                return (false, "Điểm đánh giá phải từ 1 đến 5 sao.");

            if (!string.IsNullOrEmpty(request.NoiDung) && request.NoiDung.Length > 1000)
                return (false, "Nội dung đánh giá không được vượt quá 1000 ký tự.");

            return (true, "Hợp lệ");
        }

        private async Task<(bool CanUpdate, string Message)> CanUpdateRatingAsync(int userId, DanhGium rating)
        {
            // Lấy thời gian trả phòng thực tế
            var usage = await _context.SuDungPhongs
                   .FirstOrDefaultAsync(su => su.MaDangKy == rating.MaDangKy);

            if (usage?.GioKetThucThucTe == null)
                return (false, "Không tìm thấy thông tin sử dụng phòng.");

            var daysSinceCompleted = (GetVietnamTime() - usage.GioKetThucThucTe.Value).TotalDays;
            if (daysSinceCompleted > 7)
                return (false, $"Chỉ có thể chỉnh sửa đánh giá trong vòng 1 tuần sau khi trả phòng. Bạn đã trả phòng {daysSinceCompleted:0} ngày trước.");

            return (true, "Có thể chỉnh sửa.");
        }

        private async Task<(bool CanUpdate, string Message)> CanUpdateRatingByIdAsync(int userId, int maDanhGia)
        {
            var rating = await _context.DanhGia.FindAsync(maDanhGia);
            if (rating == null || rating.MaNguoiDung != userId)
                return (false, "Không có quyền chỉnh sửa đánh giá này.");

            return await CanUpdateRatingAsync(userId, rating);
        }

        private async Task SetEditPermissions(int userId, List<RatingDto> ratings)
        {
            foreach (var rating in ratings)
            {
                var canUpdate = await CanUpdateRatingByIdAsync(userId, rating.MaDanhGia);
                rating.CoTheChinhSua = canUpdate.CanUpdate;
            }
        }
    }
}