// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

/// @title CertificateRegistry (v1.0.0)
/// @notice Luu ma bam (hash) cua chung chi len blockchain.
///         KHONG luu du lieu ca nhan (ho ten, ngay sinh...) tren chuoi, chi luu hash.
///         Hash la SHA-256 cua file PDF chung chi cuoi cung (xem QUYET_DINH_THIET_KE.md, muc 3.1).
/// @dev    Contract khong sua duoc sau khi trien khai: giao dien ham nay la ban CHOT.
contract CertificateRegistry {
    // ----------------------------- Hang so ----------------------------------
    uint256 public constant MAX_BATCH = 100;   // so hash toi da moi lan cap theo lo

    // ----------------------------- Kieu du lieu -----------------------------
    enum Status { NotFound, Valid, Revoked }

    struct Record {
        address issuer;     // to chuc da cap
        uint64 issuedAt;    // thoi diem cap (giay, theo block)
        bool revoked;       // da thu hoi chua
        uint64 revokedAt;   // thoi diem thu hoi (0 neu chua)
    }

    // ----------------------------- Trang thai -------------------------------
    address public owner;                         // quan tri contract
    mapping(address => bool) public isIssuer;     // cac vi duoc phep cap chung chi
    mapping(bytes32 => Record) private records;   // hash -> thong tin

    // ----------------------------- Su kien ----------------------------------
    event OwnershipTransferred(address indexed previousOwner, address indexed newOwner);
    event IssuerUpdated(address indexed account, bool allowed);
    event CertificateIssued(bytes32 indexed certHash, address indexed issuer, uint64 issuedAt);
    event CertificateRevoked(bytes32 indexed certHash, address indexed issuer, uint64 revokedAt);

    // ----------------------------- Phan quyen -------------------------------
    modifier onlyOwner() {
        require(msg.sender == owner, "Chi quan tri moi duoc phep");
        _;
    }

    modifier onlyIssuer() {
        require(isIssuer[msg.sender], "Vi nay khong phai to chuc cap chung chi");
        _;
    }

    constructor() {
        owner = msg.sender;
        isIssuer[msg.sender] = true;
        emit OwnershipTransferred(address(0), msg.sender);
        emit IssuerUpdated(msg.sender, true);
    }

    /// @notice Chuyen quyen quan tri sang vi khac (tranh mat khoa la mat he thong).
    function transferOwnership(address newOwner) external onlyOwner {
        require(newOwner != address(0), "Dia chi khong hop le");
        emit OwnershipTransferred(owner, newOwner);
        owner = newOwner;
    }

    /// @notice Them hoac go quyen cap chung chi cua mot vi.
    function setIssuer(address account, bool allowed) external onlyOwner {
        require(account != address(0), "Dia chi khong hop le");
        isIssuer[account] = allowed;
        emit IssuerUpdated(account, allowed);
    }

    /// @notice Tra ve phien ban cua contract.
    function version() external pure returns (string memory) {
        return "1.0.0";
    }

    // ----------------------------- Nghiep vu --------------------------------
    /// @notice Ghi hash cua mot chung chi moi len chuoi.
    function issue(bytes32 certHash) external onlyIssuer {
        _issue(certHash, uint64(block.timestamp));
    }

    /// @notice Ghi nhieu hash trong mot giao dich (cap theo lo).
    ///         Neu co bat ky hash nao khong hop le hoac da ton tai, CA LAN bi huy.
    function issueBatch(bytes32[] calldata certHashes) external onlyIssuer {
        uint256 n = certHashes.length;
        require(n > 0, "Danh sach rong");
        require(n <= MAX_BATCH, "Vuot qua so luong toi da moi lan");

        uint64 nowTs = uint64(block.timestamp);
        for (uint256 i = 0; i < n; i++) {
            _issue(certHashes[i], nowTs);
        }
    }

    /// @notice Thu hoi chung chi. Chi to chuc da cap chung chi do moi duoc thu hoi.
    function revoke(bytes32 certHash) external onlyIssuer {
        Record storage r = records[certHash];
        require(r.issuedAt != 0, "Chung chi khong ton tai");
        require(r.issuer == msg.sender, "Chi to chuc da cap moi duoc thu hoi");
        require(!r.revoked, "Chung chi da bi thu hoi truoc do");

        r.revoked = true;
        r.revokedAt = uint64(block.timestamp);
        emit CertificateRevoked(certHash, msg.sender, r.revokedAt);
    }

    /// @notice Tra cuu mot hash. Ai cung goi duoc, khong ton gas.
    /// @return status   0 = khong tim thay, 1 = hop le, 2 = da thu hoi
    function verify(bytes32 certHash)
        external
        view
        returns (Status status, address issuer, uint64 issuedAt, uint64 revokedAt)
    {
        Record memory r = records[certHash];
        if (r.issuedAt == 0) {
            return (Status.NotFound, address(0), 0, 0);
        }
        return (r.revoked ? Status.Revoked : Status.Valid, r.issuer, r.issuedAt, r.revokedAt);
    }

    // ----------------------------- Noi bo -----------------------------------
    function _issue(bytes32 certHash, uint64 ts) private {
        require(certHash != bytes32(0), "Hash rong");
        require(records[certHash].issuedAt == 0, "Chung chi da ton tai");

        records[certHash] = Record({issuer: msg.sender, issuedAt: ts, revoked: false, revokedAt: 0});
        emit CertificateIssued(certHash, msg.sender, ts);
    }
}
