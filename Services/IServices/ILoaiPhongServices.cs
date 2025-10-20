using HUIT_Library.Models;

namespace HUIT_Library.Services.IServices
{
    public interface ILoaiPhongServices
    {
        Task<List<LoaiPhong>> GetAllLoaiPhong();
        Task<LoaiPhong> GetLoaiPhongById(int id);
    }
}