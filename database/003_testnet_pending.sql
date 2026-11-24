-- Chạy trong SSMS sau khi sao lưu CSDL. Chạy lại được; không xóa dữ liệu.
USE [ChungChiBlockchain];
GO
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT chung_chi_id FROM dbo.giao_dich_blockchain WHERE trang_thai='pending'
               GROUP BY chung_chi_id HAVING COUNT(*)>1)
        THROW 50003, N'Có chứng chỉ có nhiều giao dịch pending. Cần đối soát trước khi cập nhật.', 1;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='ux_giaodich_pending_certificate' AND object_id=OBJECT_ID('dbo.giao_dich_blockchain'))
        CREATE UNIQUE INDEX ux_giaodich_pending_certificate ON dbo.giao_dich_blockchain(chung_chi_id)
        WHERE trang_thai='pending';
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
GO
