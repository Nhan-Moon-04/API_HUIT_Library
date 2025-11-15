-- =============================================
-- Stored Procedure: sp_KiemTraGiaHanPhong
-- M?c ?ích: Ki?m tra xem có th? gia h?n phòng hay không
-- ?i?u ki?n: Không có ng??i khác ??t phòng trong kho?ng th?i gian gia h?n
-- =============================================

CREATE PROCEDURE [dbo].[sp_KiemTraGiaHanPhong]
    @MaPhong INT,
    @MaDangKyHienTai INT,
    @ThoiGianBatDauGiaHan DATETIME,
    @ThoiGianKetThucMoi DATETIME,
  @KetQua INT OUTPUT,
    @ThongBao NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ConflictCount INT = 0;
    DECLARE @NextBookingInfo NVARCHAR(200) = '';
    
    BEGIN TRY
        -- Ki?m tra các ??ng ký khác trong cùng phòng có xung ??t th?i gian không
        -- Ch? ki?m tra các ??ng ký ?ã ???c duy?t ho?c ?ang ch? duy?t
        SELECT @ConflictCount = COUNT(*),
      @NextBookingInfo = STRING_AGG(
      CONCAT('??ng ký #', MaDangKy, ' t? ', 
       FORMAT(ThoiGianBatDau, 'HH:mm dd/MM/yyyy'), 
  ' ??n ', 
               FORMAT(ThoiGianKetThuc, 'HH:mm dd/MM/yyyy')), 
     '; ')
        FROM DangKyPhong 
        WHERE MaPhong = @MaPhong 
  AND MaDangKy != @MaDangKyHienTai
          AND MaTrangThai IN (1, 2, 4) -- Ch? duy?t, ?ã duy?t, ?ang s? d?ng
          AND NOT (
            @ThoiGianKetThucMoi <= ThoiGianBatDau 
        OR @ThoiGianBatDauGiaHan >= ThoiGianKetThuc
   );
        
        -- Ki?m tra l?ch tr?ng thái phòng (n?u có)
        DECLARE @ScheduleConflictCount INT = 0;
        
        SELECT @ScheduleConflictCount = COUNT(*)
        FROM LichTrangThaiPhong
        WHERE MaPhong = @MaPhong
          AND Ngay >= CAST(@ThoiGianBatDauGiaHan AS DATE)
      AND Ngay <= CAST(@ThoiGianKetThucMoi AS DATE)
  AND NOT (
     CAST(@ThoiGianKetThucMoi AS TIME) <= GioBatDau 
   OR CAST(@ThoiGianBatDauGiaHan AS TIME) >= GioKetThuc
          );
        
    -- Xác ??nh k?t qu?
    IF @ConflictCount > 0
   BEGIN
    SET @KetQua = 1;
            SET @ThongBao = 'Không th? gia h?n: Có ??t phòng khác trong th?i gian này. ' + @NextBookingInfo;
      END
        ELSE IF @ScheduleConflictCount > 0
    BEGIN
          SET @KetQua = 2;
         SET @ThongBao = 'Không th? gia h?n: Phòng ?ã có l?ch ho?t ??ng khác trong th?i gian này.';
   END
        ELSE
        BEGIN
          SET @KetQua = 0;
 SET @ThongBao = 'Có th? gia h?n phòng.';
        END
        
    END TRY
    BEGIN CATCH
        SET @KetQua = -1;
    SET @ThongBao = 'L?i h? th?ng khi ki?m tra gia h?n: ' + ERROR_MESSAGE();
    END CATCH
END

-- Cách s? d?ng:
-- DECLARE @KetQua INT, @ThongBao NVARCHAR(500);
-- EXEC sp_KiemTraGiaHanPhong 
--     @MaPhong = 12,
--     @MaDangKyHienTai = 100,
--     @ThoiGianBatDauGiaHan = '2025-01-15 14:00:00',
--     @ThoiGianKetThucMoi = '2025-01-15 16:00:00',
--     @KetQua = @KetQua OUTPUT,
--     @ThongBao = @ThongBao OUTPUT;
-- SELECT @KetQua as KetQua, @ThongBao as ThongBao;

-- K?t qu?:
-- @KetQua = 0: Có th? gia h?n
-- @KetQua = 1: Có xung ??t v?i ??ng ký khác
-- @KetQua = 2: Có xung ??t v?i l?ch phòng
-- @KetQua = -1: L?i h? th?ng