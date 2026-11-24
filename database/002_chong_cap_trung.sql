USE [ChungChiBlockchain];
GO
-- Chạy một lần trên CSDL đang dùng, sau khi sao lưu. Không chạy lại script khởi tạo.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS (SELECT 1 FROM chung_chi WHERE trang_thai <> 'thu_hoi'
           GROUP BY nguoi_hoc_id, khoa_hoc_id HAVING COUNT(*) > 1)
BEGIN
    ROLLBACK;
    THROW 50001, N'Có chứng chỉ trùng người học/khóa học chưa thu hồi. Kiểm tra dữ liệu trước khi tạo chỉ mục.', 1;
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='ux_chungchi_nguoihoc_khoahoc_con_hieu_luc' AND object_id=OBJECT_ID('chung_chi'))
    CREATE UNIQUE INDEX ux_chungchi_nguoihoc_khoahoc_con_hieu_luc
    ON chung_chi(nguoi_hoc_id,khoa_hoc_id) WHERE trang_thai <> 'thu_hoi';
COMMIT;
