USE [ChungChiBlockchain];
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.vai_tro WITH (UPDLOCK, HOLDLOCK) WHERE ma_vai_tro = 'nguoi_hoc')
    INSERT INTO dbo.vai_tro (ma_vai_tro, ten_vai_tro, mo_ta)
    VALUES ('nguoi_hoc', N'Người học', N'Xem, tải PDF và chia sẻ chứng chỉ của chính mình');

IF OBJECT_ID(N'dbo.tai_khoan_nguoi_hoc', N'U') IS NULL
    CREATE TABLE dbo.tai_khoan_nguoi_hoc (
        nguoi_dung_id INT NOT NULL PRIMARY KEY REFERENCES dbo.nguoi_dung(id),
        nguoi_hoc_id INT NOT NULL UNIQUE REFERENCES dbo.nguoi_hoc(id)
    );

COMMIT TRANSACTION;
