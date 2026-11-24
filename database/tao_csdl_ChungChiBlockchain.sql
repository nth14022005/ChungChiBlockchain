/* =====================================================================
   DO AN 2 - He thong xac thuc chung chi, bang cap bang Blockchain
   CSDL: SQL Server (chay trong SSMS)
   So bang: 19

   Nguyen tac thiet ke:
   - Du lieu ca nhan (ho ten, ngay sinh...) CHI luu o CSDL nay.
   - Tren blockchain chi ghi ma bam (hash) cua chung chi.
   ===================================================================== */

IF DB_ID(N'ChungChiBlockchain') IS NULL
    CREATE DATABASE ChungChiBlockchain;
GO

USE ChungChiBlockchain;
GO

/* ---------- Xoa bang cu (neu chay lai), theo thu tu khoa ngoai ---------- */
DROP TABLE IF EXISTS nhat_ky_hoat_dong;
DROP TABLE IF EXISTS thong_bao;
DROP TABLE IF EXISTS lien_ket_chia_se;
DROP TABLE IF EXISTS lich_su_xac_minh;
DROP TABLE IF EXISTS doanh_nghiep_xac_minh;
DROP TABLE IF EXISTS thu_hoi_chung_chi;
DROP TABLE IF EXISTS giao_dich_blockchain;
DROP TABLE IF EXISTS tep_chung_chi;
DROP TABLE IF EXISTS chung_chi;
DROP TABLE IF EXISTS lo_phat_hanh;
DROP TABLE IF EXISTS nguoi_hoc;
DROP TABLE IF EXISTS mau_chung_chi;
DROP TABLE IF EXISTS loai_chung_chi;
DROP TABLE IF EXISTS khoa_hoc;
DROP TABLE IF EXISTS phien_ban_hop_dong;
DROP TABLE IF EXISTS vi_blockchain;
DROP TABLE IF EXISTS nguoi_dung;
DROP TABLE IF EXISTS to_chuc;
DROP TABLE IF EXISTS vai_tro;
GO

/* ===================== NHOM TO CHUC VA NGUOI DUNG ===================== */

-- 1. Vai tro trong he thong
CREATE TABLE vai_tro (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    ma_vai_tro      VARCHAR(30)    NOT NULL UNIQUE,      -- quan_tri, to_chuc_cap, doanh_nghiep
    ten_vai_tro     NVARCHAR(100)  NOT NULL,
    mo_ta           NVARCHAR(255)  NULL
);

-- 2. To chuc cap chung chi (truong, trung tam dao tao)
CREATE TABLE to_chuc (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    ma_to_chuc      VARCHAR(30)    NOT NULL UNIQUE,
    ten_to_chuc     NVARCHAR(200)  NOT NULL,
    dia_chi         NVARCHAR(300)  NULL,
    email_lien_he   VARCHAR(100)   NULL,
    website         VARCHAR(200)   NULL,
    trang_thai      VARCHAR(20)    NOT NULL DEFAULT 'hoat_dong'
                    CHECK (trang_thai IN ('cho_duyet', 'hoat_dong', 'tam_khoa')),
    ngay_tao        DATETIME       NOT NULL DEFAULT GETDATE()
);

-- 3. Nguoi dung dang nhap he thong
CREATE TABLE nguoi_dung (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    ten_dang_nhap   VARCHAR(50)    NOT NULL UNIQUE,
    mat_khau_hash   VARCHAR(255)   NOT NULL,             -- chi luu ma bam
    ho_ten          NVARCHAR(100)  NOT NULL,
    email           VARCHAR(100)   NULL,
    vai_tro_id      INT            NOT NULL REFERENCES vai_tro(id),
    to_chuc_id      INT            NULL REFERENCES to_chuc(id),   -- chi co voi nhan vien to chuc cap
    trang_thai      BIT            NOT NULL DEFAULT 1,
    ngay_tao        DATETIME       NOT NULL DEFAULT GETDATE()
);

/* ========================= NHOM BLOCKCHAIN ============================= */

-- 4. Vi blockchain duoc phep cap chung chi (khop voi ham setIssuer cua contract)
CREATE TABLE vi_blockchain (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    to_chuc_id      INT            NOT NULL REFERENCES to_chuc(id),
    dia_chi_vi      CHAR(42)       NOT NULL UNIQUE,      -- 0x + 40 ky tu hex
    nhan            NVARCHAR(100)  NULL,
    trang_thai      VARCHAR(20)    NOT NULL DEFAULT 'cho_duyet'
                    CHECK (trang_thai IN ('cho_duyet', 'hoat_dong', 'bi_khoa')),
    ngay_them       DATETIME       NOT NULL DEFAULT GETDATE()
);

-- 5. Cac phien ban smart contract da trien khai (contract khong sua duoc nen co the phai deploy lai)
CREATE TABLE phien_ban_hop_dong (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    ten_mang        VARCHAR(50)    NOT NULL,             -- vi du: sepolia
    chain_id        BIGINT         NOT NULL,
    dia_chi_hop_dong CHAR(42)      NOT NULL,
    tx_trien_khai   CHAR(66)       NULL,
    phien_ban       VARCHAR(20)    NOT NULL,
    dang_su_dung    BIT            NOT NULL DEFAULT 0,
    ngay_trien_khai DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT uq_hopdong UNIQUE (chain_id, dia_chi_hop_dong)
);

/* ======================== NHOM DANH MUC CHUNG CHI ====================== */

-- 6. Khoa hoc / chuong trinh dao tao
CREATE TABLE khoa_hoc (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    to_chuc_id      INT            NOT NULL REFERENCES to_chuc(id),
    ma_khoa_hoc     VARCHAR(30)    NOT NULL,
    ten_khoa_hoc    NVARCHAR(200)  NOT NULL,
    thoi_luong      NVARCHAR(50)   NULL,
    mo_ta           NVARCHAR(500)  NULL,
    CONSTRAINT uq_khoahoc UNIQUE (to_chuc_id, ma_khoa_hoc)
);

-- 7. Loai chung chi (bang tot nghiep, chung chi hoan thanh khoa hoc...)
CREATE TABLE loai_chung_chi (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    ten_loai        NVARCHAR(100)  NOT NULL UNIQUE,
    mo_ta           NVARCHAR(255)  NULL
);

-- 8. Mau chung chi cua tung to chuc
CREATE TABLE mau_chung_chi (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    to_chuc_id          INT            NOT NULL REFERENCES to_chuc(id),
    loai_chung_chi_id   INT            NOT NULL REFERENCES loai_chung_chi(id),
    ten_mau             NVARCHAR(150)  NOT NULL,
    duong_dan_mau       NVARCHAR(500)  NULL,
    trang_thai          BIT            NOT NULL DEFAULT 1
);

/* ========================= NHOM CAP CHUNG CHI ========================== */

-- 9. Nguoi hoc (du lieu ca nhan: CHI luu o day, khong dua len blockchain)
CREATE TABLE nguoi_hoc (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    to_chuc_id      INT            NOT NULL REFERENCES to_chuc(id),
    ma_nguoi_hoc    VARCHAR(30)    NOT NULL,
    ho_ten          NVARCHAR(100)  NOT NULL,
    ngay_sinh       DATE           NULL,
    email           VARCHAR(100)   NULL,
    so_dien_thoai   VARCHAR(20)    NULL,
    ngay_tao        DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT uq_nguoihoc UNIQUE (to_chuc_id, ma_nguoi_hoc)
);

-- 10. Lo phat hanh (cap nhieu chung chi cung luc)
CREATE TABLE lo_phat_hanh (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    to_chuc_id      INT            NOT NULL REFERENCES to_chuc(id),
    nguoi_tao_id    INT            NOT NULL REFERENCES nguoi_dung(id),
    ten_lo          NVARCHAR(150)  NOT NULL,
    so_luong        INT            NOT NULL DEFAULT 0,
    trang_thai      VARCHAR(20)    NOT NULL DEFAULT 'nhap'
                    CHECK (trang_thai IN ('nhap', 'dang_xu_ly', 'hoan_tat', 'loi')),
    ngay_tao        DATETIME       NOT NULL DEFAULT GETDATE()
);

-- 11. Chung chi
CREATE TABLE chung_chi (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    ma_chung_chi        VARCHAR(40)    NOT NULL UNIQUE,   -- ma cong khai de tra cuu / in tren QR
    nguoi_hoc_id        INT            NOT NULL REFERENCES nguoi_hoc(id),
    khoa_hoc_id         INT            NOT NULL REFERENCES khoa_hoc(id),
    mau_chung_chi_id    INT            NOT NULL REFERENCES mau_chung_chi(id),
    lo_phat_hanh_id     INT            NULL REFERENCES lo_phat_hanh(id),
    nguoi_cap_id        INT            NOT NULL REFERENCES nguoi_dung(id),
    ngay_cap            DATE           NOT NULL,
    xep_loai            NVARCHAR(50)   NULL,
    hash_chung_chi      CHAR(66)       NULL,              -- 0x + SHA-256 (64 hex), NULL khi con la ban nhap
    phien_ban_bam       TINYINT        NOT NULL DEFAULT 1, -- phien ban quy tac bam (de sau nay doi cach bam van xac minh duoc cung cu)
    du_lieu_chung_chi   NVARCHAR(MAX)  NOT NULL,          -- JSON: ban chup du lieu tai thoi diem cap
    trang_thai          VARCHAR(20)    NOT NULL DEFAULT 'nhap'
                        CHECK (trang_thai IN ('nhap', 'cho_xac_nhan', 'da_ghi_chuoi', 'thu_hoi', 'loi')),
    ngay_tao            DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT ck_chungchi_json CHECK (ISJSON(du_lieu_chung_chi) = 1)
);

-- Hash duy nhat nhung cho phep nhieu dong NULL (ban nhap)
CREATE UNIQUE INDEX ux_chungchi_hash ON chung_chi (hash_chung_chi) WHERE hash_chung_chi IS NOT NULL;

-- 12. Tep chung chi (PDF luu o server, khong dua len blockchain/IPFS)
CREATE TABLE tep_chung_chi (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    chung_chi_id    INT            NOT NULL REFERENCES chung_chi(id) ON DELETE CASCADE,
    ten_tep         NVARCHAR(200)  NOT NULL,
    duong_dan       NVARCHAR(500)  NOT NULL,
    kich_thuoc      BIGINT         NULL,
    sha256          CHAR(64)       NOT NULL,
    ngay_tao        DATETIME       NOT NULL DEFAULT GETDATE()
);

/* ===================== NHOM GIAO DICH VA THU HOI ======================= */

-- 13. Giao dich blockchain (cap hoac thu hoi); co trang thai Pending -> Success
CREATE TABLE giao_dich_blockchain (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    chung_chi_id        INT            NOT NULL REFERENCES chung_chi(id),
    hop_dong_id         INT            NOT NULL REFERENCES phien_ban_hop_dong(id),
    vi_id               INT            NULL REFERENCES vi_blockchain(id),
    loai                VARCHAR(10)    NOT NULL CHECK (loai IN ('cap', 'thu_hoi')),
    tx_hash             CHAR(66)       NULL,              -- NULL cho den khi nguoi dung ky va gui
    so_khoi             BIGINT         NULL,
    trang_thai          VARCHAR(10)    NOT NULL DEFAULT 'pending'
                        CHECK (trang_thai IN ('pending', 'success', 'failed')),
    thong_bao_loi       NVARCHAR(500)  NULL,
    ngay_gui            DATETIME       NOT NULL DEFAULT GETDATE(),
    ngay_xac_nhan       DATETIME       NULL
);

CREATE UNIQUE INDEX ux_giaodich_tx ON giao_dich_blockchain (tx_hash) WHERE tx_hash IS NOT NULL;

-- 14. Thu hoi chung chi
CREATE TABLE thu_hoi_chung_chi (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    chung_chi_id        INT            NOT NULL REFERENCES chung_chi(id),
    nguoi_thu_hoi_id    INT            NOT NULL REFERENCES nguoi_dung(id),
    giao_dich_id        INT            NULL REFERENCES giao_dich_blockchain(id),
    ly_do               NVARCHAR(500)  NOT NULL,
    ngay_thu_hoi        DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT uq_thuhoi_chungchi UNIQUE (chung_chi_id)      -- moi chung chi chi thu hoi 1 lan
);

/* ========================= NHOM XAC MINH ============================== */

-- 15. Doanh nghiep / ben xac minh da dang ky tai khoan
CREATE TABLE doanh_nghiep_xac_minh (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    nguoi_dung_id       INT            NOT NULL REFERENCES nguoi_dung(id),
    ten_doanh_nghiep    NVARCHAR(200)  NOT NULL,
    ma_so_thue          VARCHAR(20)    NULL,
    nguoi_lien_he       NVARCHAR(100)  NULL,
    ngay_dang_ky        DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT uq_dn_nguoidung UNIQUE (nguoi_dung_id)
);

-- 16. Lich su xac minh (ai cung xac minh duoc, dang nhap hoac khong)
CREATE TABLE lich_su_xac_minh (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    chung_chi_id        INT            NULL REFERENCES chung_chi(id),     -- NULL neu khong tim thay
    doanh_nghiep_id     INT            NULL REFERENCES doanh_nghiep_xac_minh(id),
    hash_tra_cuu        CHAR(66)       NOT NULL,
    ket_qua             VARCHAR(20)    NOT NULL
                        CHECK (ket_qua IN ('hop_le', 'khong_khop', 'da_thu_hoi', 'khong_tim_thay')),
    phuong_thuc         VARCHAR(20)    NOT NULL
                        CHECK (phuong_thuc IN ('tai_pdf', 'ma_qr', 'nhap_ma')),
    dia_chi_ip          VARCHAR(45)    NULL,
    thoi_gian           DATETIME       NOT NULL DEFAULT GETDATE()
);

-- 17. Lien ket chia se de nguoi hoc gui cho nha tuyen dung
CREATE TABLE lien_ket_chia_se (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    chung_chi_id    INT            NOT NULL REFERENCES chung_chi(id) ON DELETE CASCADE,
    token           VARCHAR(64)    NOT NULL UNIQUE,
    het_han         DATETIME       NULL,
    so_lan_xem      INT            NOT NULL DEFAULT 0,
    ngay_tao        DATETIME       NOT NULL DEFAULT GETDATE()
);

/* ============================ HE THONG ================================= */

-- 18. Thong bao cho nguoi dung
CREATE TABLE thong_bao (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    nguoi_dung_id   INT            NOT NULL REFERENCES nguoi_dung(id) ON DELETE CASCADE,
    tieu_de         NVARCHAR(200)  NOT NULL,
    noi_dung        NVARCHAR(1000) NULL,
    da_doc          BIT            NOT NULL DEFAULT 0,
    ngay_tao        DATETIME       NOT NULL DEFAULT GETDATE()
);

-- 19. Nhat ky hoat dong
CREATE TABLE nhat_ky_hoat_dong (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    nguoi_dung_id   INT            NULL REFERENCES nguoi_dung(id),
    hanh_dong       NVARCHAR(100)  NOT NULL,
    doi_tuong       NVARCHAR(100)  NULL,
    chi_tiet        NVARCHAR(500)  NULL,
    dia_chi_ip      VARCHAR(45)    NULL,
    thoi_gian       DATETIME       NOT NULL DEFAULT GETDATE()
);
GO

/* ---------- Chi muc cho cac truy van hay dung ---------- */
CREATE INDEX ix_chungchi_nguoihoc ON chung_chi (nguoi_hoc_id);
CREATE INDEX ix_chungchi_trangthai ON chung_chi (trang_thai);
CREATE INDEX ix_giaodich_trangthai ON giao_dich_blockchain (trang_thai, ngay_gui);
CREATE INDEX ix_xacminh_thoigian ON lich_su_xac_minh (thoi_gian);
GO

/* ---------- Du lieu khoi tao ---------- */
INSERT INTO vai_tro (ma_vai_tro, ten_vai_tro, mo_ta) VALUES
    ('quan_tri',    N'Quản trị hệ thống', N'Quản lý tổ chức, ví, hợp đồng'),
    ('to_chuc_cap', N'Nhân viên tổ chức cấp', N'Cấp và thu hồi chứng chỉ'),
    ('doanh_nghiep',N'Bên xác minh', N'Doanh nghiệp tra cứu chứng chỉ');

INSERT INTO loai_chung_chi (ten_loai, mo_ta) VALUES
    (N'Chứng chỉ hoàn thành khóa học', NULL),
    (N'Bằng tốt nghiệp', NULL),
    (N'Giấy chứng nhận', NULL);
GO

/* ---------- Kiem tra: phai ra 19 ---------- */
SELECT COUNT(*) AS so_bang
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE';
GO
