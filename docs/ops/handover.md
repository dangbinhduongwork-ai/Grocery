# GroceryHub — System Handover & Operational Manual
**Tài liệu Bàn giao Hệ thống & Vận hành Sản phẩm**

**Document ID:** `OPS-HDO-001`  
**Version:** `1.0.0` (Production Release)  
**Date:** 2026-11-16  
**Target Domain:** `grocery.dangbinhduong.io.vn`  
**Classification:** Confidential — System Operations & Client Handover  

---

## 1. Tổng quan Bàn giao (Executive Handover Summary)

Dự án **GroceryHub** là nền tảng thương mại điện tử chuyên biệt cho ngành bán lẻ thực phẩm và bách hóa tại thị trường Việt Nam. Hệ thống đã hoàn thành 100% các giai đoạn phát triển, kiểm thử, tối ưu hiệu năng và đóng gói triển khai (Phases 00 đến 13) theo đúng cam kết trong Hồ sơ Yêu cầu Kỹ thuật (`docs/spec/02-PROJECT-REQUIREMENTS.md`).

Tài liệu này cung cấp hướng dẫn toàn diện dành cho Quản trị viên hệ thống (DevOps / SysAdmin) và Chủ sở hữu sản phẩm (Product Owner) để tiếp nhận, cấu hình, vận hành và bảo trì hệ thống trong môi trường Production.

---

## 2. Kiến trúc & Cấu trúc Mã nguồn (Architecture & Source Layout)

### 2.1. Ngăn xếp Công nghệ (Technology Stack)
- **Backend API:** C# trên .NET 10 LTS, ASP.NET Core Web API (Minimal API + Modular Monolith), EF Core 10.
- **Cơ sở dữ liệu:** Microsoft SQL Server 2022+ / Azure SQL Database với tính năng Read Committed Snapshot Isolation (RCSI) bắt buộc.
- **Frontend Storefront & Admin:** React 19, TypeScript (Strict mode), Vite, Tailwind CSS v4, shadcn/ui ("Sổ chợ" design system), TanStack Router & TanStack Query.
- **Thanh toán:** Tiền mặt khi nhận hàng (COD) và Chuyển khoản ngân hàng tự động qua SePay VietQR (xác thực HMAC-SHA256, đối soát tự động, cảnh báo sai lệch, quy trình hoàn tiền 2 bước).
- **Hạ tầng Container:** Docker, Docker Compose, Unprivileged Nginx Reverse Proxy với Rate Limiting và SePay IP Allowlist.

### 2.2. Sơ đồ Cấu trúc Thư mục Hệ thống
```
GroceryHub/
├── src/
│   ├── GroceryHub.Api/               # ASP.NET Core API Host, Endpoints, Rate Limiting, OpenAPI
│   ├── GroceryHub.SharedKernel/      # Domain Primitives (Money, VietnamesePhone, Guard, Result)
│   ├── GroceryHub.Persistence/       # EF Core DbContext, Schema-per-module, Interceptors, Migrations
│   └── GroceryHub.Modules.*/         # 12 Modular Monolith Modules (Identity, Catalog, Inventory,
│                                     # Purchasing, Cart, Ordering, Payments, Promotions, Customers,
│                                     # Reviews, Reporting, Audit)
├── web/
│   ├── src/
│   │   ├── admin/                    # Admin Back-Office (Dashboard, Orders, Inventory, Reports)
│   │   ├── storefront/               # Khách hàng Storefront (Aisle browsing, Cart, Checkout, QR)
│   │   └── shared/                   # Design Tokens, i18n vi.ts, UI Primitives, Formatters
│   ├── nginx/default.conf            # Hardened Nginx Gateway configuration
│   └── Dockerfile                    # Multi-stage unprivileged web container build
├── scripts/
│   ├── backup-db.ps1 / .sh           # Database Backup automation (Full + Differential)
│   ├── restore-drill.sh              # Disaster Recovery Restore Drill script
│   ├── smoke-test.ps1 / .sh          # Post-deployment health verification script
│   ├── verify-ports.ps1              # Port security isolation verifier
│   └── sql/                          # Production DB setup & RCSI scripts
├── docs/
│   ├── spec/                         # Specifications & ADRs (01 đến 12)
│   ├── ops/                          # Operational manuals (deployment, runbook, backup-restore)
│   └── progress/                     # Audit trails, TRACEABILITY.md, FINAL-REPORT.md
└── docker-compose*.yml               # Production & Dev compose manifests
```

---

## 3. Danh mục Tài liệu Vận hành (Operational Runbooks Index)

Tất cả các tình huống vận hành và bảo trì đã được chuẩn hóa thành các quy trình chi tiết:

| Mã Tài liệu | Đường dẫn File | Mục đích & Nội dung |
|---|---|---|
| **OPS-DEP-001** | [`docs/ops/deployment-guide.md`](file:///e:/25.Mykey/GroceryHub/docs/ops/deployment-guide.md) | Hướng dẫn triển khai Production (Kịch bản Cloud Container & Kịch bản Linux VPS với Docker Compose, cấu hình SSL, Ingress). |
| **OPS-RUN-001** | [`docs/ops/runbook.md`](file:///e:/25.Mykey/GroceryHub/docs/ops/runbook.md) | Sổ tay vận hành hàng ngày: kiểm tra Health check, quản lý Log, giám sát hiệu năng, xử lý hàng đợi thanh toán SePay. |
| **OPS-BCK-001** | [`docs/ops/backup-restore.md`](file:///e:/25.Mykey/GroceryHub/docs/ops/backup-restore.md) | Quy trình sao lưu & khôi phục thảm họa (Disaster Recovery): RPO = 1 giờ, RTO = 30 phút, kiểm thử phục hồi định kỳ. |
| **OPS-INC-001** | [`docs/ops/incident-checklist.md`](file:///e:/25.Mykey/GroceryHub/docs/ops/incident-checklist.md) | Checklist ứng phó sự cố khẩn cấp: Gián đoạn thanh toán SePay, treo DB deadlock, rò rỉ secret, DDoS Ingress. |
| **OPS-ROT-001** | [`scripts/rotate-secrets.md`](file:///e:/25.Mykey/GroceryHub/scripts/rotate-secrets.md) | Quy trình xoay vòng khóa bí mật định kỳ: JWT Signing Key, SePay Webhook Secret, SQL Password, SMTP Auth. |

---

## 4. Bàn giao Tài khoản & Dịch vụ Bên ngoài (External Integrations Handover)

Trước khi chính thức Go-Live, người tiếp nhận hệ thống (Khách hàng / DevOps) cần thực hiện các hành động sau:

### 4.1. Cấu hình Cổng thanh toán SePay VietQR
1. **Đăng nhập Dashboard SePay:** [https://my.sepay.vn](https://my.sepay.vn)
2. **Liên kết tài khoản Ngân hàng:**
   - Ngân hàng: Khai báo tài khoản doanh nghiệp (ví dụ: MBBank, Vietcombank, Techcombank).
   - Kiểm tra định dạng số tài khoản (`account_number`) khớp với biến `SePay__BankAccounts__0__AccountNumber` trong `.env`.
3. **Cấu hình Webhook:**
   - Webhook URL: `https://grocery.dangbinhduong.io.vn/api/v1/payments/sepay/webhook`
   - Phương thức xác thực: **HMAC-SHA256 (Khuyến nghị cao nhất)**.
   - Sao chép khóa bí mật HMAC từ SePay Dashboard và điền vào `SePay__Webhook__HmacSecret`.
   - Nếu dùng API Key: điền API key vào `SePay__Webhook__ApiKey`.
4. **IP Allowlist:** Đảm bảo Nginx hoặc Cloudflare cho phép các IP của SePay (`103.179.189.0/24` và các dải công bố bởi SePay) truy cập đường dẫn webhook.

### 4.2. Cấu hình Tên miền & Chứng chỉ SSL/TLS
1. **Bản ghi DNS:**
   - Cấu hình bản ghi `A` hoặc `CNAME` trỏ tên miền `grocery.dangbinhduong.io.vn` về IP máy chủ VPS hoặc Cloud Ingress IP.
2. **Cấp phát Chứng chỉ SSL:**
   - Sử dụng Let's Encrypt (qua Caddy, Certbot hoặc Cloudflare Flexible/Full SSL).
   - Đảm bảo HTTPS hoạt động chuẩn với TLS 1.3 và cấu hình HSTS.

### 4.3. Dịch vụ Email Giao dịch (Transactional Email SMTP)
1. Đăng ký tài khoản nhà cung cấp SMTP uy tín: **SendGrid, Resend, Brevo, hoặc AWS SES**.
2. Xác thực tên miền gửi email (`grocery.dangbinhduong.io.vn`) bằng SPF và DKIM DNS records.
3. Cập nhật thông tin SMTP vào file `.env`:
   - `Smtp__Host`, `Smtp__Port` (587), `Smtp__User`, `Smtp__Password`, `Smtp__FromEmail`, `Smtp__FromName`.

### 4.4. Bản quyền Cơ sở dữ liệu Microsoft SQL Server
- **Lưu ý pháp lý đặc biệt quan trọng:** Phiên bản SQL Server `Developer Edition` hoặc `Express Edition` không được phép sử dụng cho mục đích thương mại sản xuất (Production).
- **Khuyến nghị:**
  - Lựa chọn 1: Sử dụng **Azure SQL Database (General Purpose vCore)** hoặc **AWS RDS for SQL Server** (đã bao gồm phí bản quyền Microsoft trong chi phí hàng tháng).
  - Lựa chọn 2: Mua giấy phép **SQL Server Standard Edition** nếu tự vận hành trên máy chủ vật lý/VPS riêng.

---

## 5. Quy trình Khởi động Nhanh trên Môi trường Mới (Fresh Deployment Drill)

Để kiểm chứng tính độc lập và khả năng phục hồi của hệ thống, quy trình sau đã được thực nghiệm thành công từ bản mã nguồn sạch:

```bash
# 1. Clone mã nguồn từ repository
git clone https://github.com/mykey-repository/GroceryHub.git
cd GroceryHub

# 2. Khởi tạo file cấu hình môi trường
cp .env.example .env
# Biên tập .env: cập nhật SA_PASSWORD, JWT_SIGNING_KEY, SEPAY_SECRET

# 3. Khởi động hạ tầng Docker Compose
docker compose up -d --build

# 4. Kiểm tra trạng thái các container
docker compose ps
# Yêu cầu: groceryhub-db (healthy), groceryhub-api (healthy), groceryhub-web (healthy)

# 5. Chạy kịch bản kiểm thử Smoke Test tự động
./scripts/smoke-test.sh https://grocery.dangbinhduong.io.vn
```

---

## 6. Biên bản Nghiệm thu & Bàn giao (Handover Acceptance Sign-off)

| Hạng mục Kiểm tra | Trạng thái | Ghi chú Xác nhận |
|---|:---:|---|
| Toàn bộ 473 bài kiểm thử tự động (Unit, Architecture, Integration, E2E) đạt 100% | **ĐẠT** | Báo cáo chi tiết tại `docs/evidence/phase-13/clean-checkout-run.txt` |
| Quét rò rỉ mã nguồn & Reality Audit đạt 0 vi phạm | **ĐẠT** | 0 TODO, 0 NotImplementedException, 0 token trong storage |
| Ma trận phân quyền 61 endpoints đạt chuẩn Default-Deny RBAC | **ĐẠT** | Kiểm thử độc lập tại `docs/security/review-final.md` |
| Giao diện người dùng Storefront & Admin đáp ứng WCAG 2.1 AA | **ĐẠT** | Kiểm thử độc lập tại `docs/evidence/phase-13/ui-review-final.md` |
| Toàn bộ kịch bản nghiệp vụ (Đặt hàng, SePay QR, Đối soát, Hoàn tiền) chạy thực tế | **ĐẠT** | Báo cáo tại `docs/evidence/phase-13/business-flow-rehearsal.md` |
| Hồ sơ kiến trúc, đặc tả API OpenAPI và Sổ tay vận hành bàn giao đầy đủ | **ĐẠT** | Toàn bộ tài liệu nằm trong thư mục `docs/` |

**Đại diện Đội ngũ Phát triển (Project Manager):** Đã hoàn tất & Ký xác nhận  
**Đại diện Tiếp nhận Hệ thống (Product Owner / Client):** Sẵn sàng tiếp nhận Go-Live
