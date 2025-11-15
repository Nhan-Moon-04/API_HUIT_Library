using HUIT_Library.Models;

namespace HUIT_Library.Services.TaiNguyen.IServices
{
    public interface ITaiNguyenLoaiPhongServices
    {
        /// <summary>
        /// Lấy tất cả tài nguyên
        /// </summary>
        Task<List<Models.TaiNguyen>> GetAllTaiNguyenAsync();
    }
}
