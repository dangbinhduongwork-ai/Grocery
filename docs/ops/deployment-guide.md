# GroceryHub — Production Deployment Guide / Hướng dẫn triển khai Production

**Document ID:** `OPS-DEP-001`  
**Version:** 1.0.0  
**Phase:** 12 — Docker & Deployment  
**Domain:** `grocery.dangbinhduong.io.vn` (Confirmed in `DEC-018`)  
**Target Environments:**
1. **Primary (Production):** Managed Container Platform (Azure Container Apps / AWS ECS / GCP Cloud Run) + Managed SQL Server (Azure SQL / AWS RDS).
2. **Alternative / Staging:** Single Linux VPS (Ubuntu 22.04/24.04 LTS) with Docker Compose + Reverse Proxy (Caddy / Nginx).

---

## MỤC LỤC / TABLE OF CONTENTS
- [1. Tiếng Việt: Hướng dẫn triển khai chi tiết](#1-tiếng-việt-hướng-dẫn-triển-khai-chi-tiết)
  - [1.1. Kiến trúc & Quyết định triển khai](#11-kiến-trúc--quyết-định-triển-khai)
  - [1.2. Kịch bản A: Triển khai trên Cloud Container Platform (Azure Container Apps / AWS ECS)](#12-kịch-bản-a-triển-khai-trên-cloud-container-platform-azure-container-apps--aws-ecs)
  - [1.3. Kịch bản B: Triển khai trên Linux VPS với Docker Compose](#13-kịch-bản-b-triển-khai-trên-linux-vps-với-docker-compose)
  - [1.4. Chạy Migration Cơ sở dữ liệu](#14-chạy-migration-cơ-sở-dữ-liệu)
  - [1.5. Cấu hình Tên miền & Chứng chỉ SSL](#15-cấu-hình-tên-miền--chứng-chỉ-ssl)
  - [1.6. Kiểm thử Smoke Test sau triển khai](#16-kiểm-thử-smoke-test-sau-triển-khai)
- [2. English: Detailed Production Deployment Guide](#2-english-detailed-production-deployment-guide)
  - [2.1. Architecture & Confirmed Decisions](#21-architecture--confirmed-decisions)
  - [2.2. Scenario A: Cloud Container Platform Deployment](#22-scenario-a-cloud-container-platform-deployment)
  - [2.3. Scenario B: Single Linux VPS Deployment with Docker Compose](#23-scenario-b-single-linux-vps-deployment-with-docker-compose)
  - [2.4. Running Database Migrations](#24-running-database-migrations)
  - [2.5. DNS & TLS Configuration](#25-dns--tls-configuration)
  - [2.6. Post-Deployment Verification](#26-post-deployment-verification)

---

# 1. TIẾNG VIỆT: HƯỚNG DẪN TRIỂN KHAI CHI TIẾT

## 1.1. Kiến trúc & Quyết định triển khai
Căn cứ quyết định **DEC-018**:
- **Tên miền chính thức:** `grocery.dangbinhduong.io.vn`
- **Cơ sở dữ liệu Production:** Azure SQL Database hoặc AWS RDS for SQL Server (đảm bảo tuân thủ bản quyền Microsoft; bản `Developer` bị cấm chạy Production).
- **Hạ tầng Container:** Managed Container Platform (Azure Container Apps / AWS ECS) cho Production; Docker Compose cho Staging/Dev.
- **Email Transactional:** SMTP Server chuẩn (Resend, SendGrid, Brevo, AWS SES) trên Production; Mailpit cho Staging.

---

## 1.2. Kịch bản A: Triển khai trên Cloud Container Platform (Azure Container Apps / AWS ECS)

### Bước 1: Chuẩn bị Cơ sở dữ liệu Managed SQL
1. Tạo instance Azure SQL Database (hoặc AWS RDS for SQL Server) với chế độ xác thực SQL Authentication.
2. Thiết lập Firewall Rule cho phép dải IP của Container Platform kết nối đến cổng 1433.
3. Chạy script tạo Login và phân quyền ứng dụng tối thiểu:
   ```bash
   sqlcmd -S <server-name>.database.windows.net -U <admin-user> -P <admin-pass> -d GroceryHub -i scripts/sql/01-create-app-login.sql
   ```
4. Xác nhận cơ chế non-blocking reader:
   ```bash
   sqlcmd -S <server-name>.database.windows.net -U <admin-user> -P <admin-pass> -d master -i scripts/sql/02-verify-rcsi.sql
   ```

### Bước 2: Build & Đẩy Container Images lên GitHub Container Registry (GHCR)
Tạo git tag phiên bản phát hành để kích hoạt tự động GitHub Actions workflow `.github/workflows/release.yml`:
```bash
git tag -a v1.0.0 -m "Release v1.0.0 for Production"
git push origin v1.0.0
```
Workflow sẽ tự động build, scan bảo mật bằng Trivy, và phát hành images:
- `ghcr.io/<owner>/groceryhub/api:v1.0.0`
- `ghcr.io/<owner>/groceryhub/web:v1.0.0`

### Bước 3: Chạy Database Migration Bundle
Trước khi khởi động API, chạy migration bundle `efbundle` dưới dạng một job ngắn hạn (Azure Container App Job hoặc AWS ECS Task một lần):
```bash
az containerapp job create \
  --name groceryhub-migrator \
  --resource-group groceryhub-rg \
  --image ghcr.io/<owner>/groceryhub/api:v1.0.0 \
  --command-line "./efbundle" \
  --env-vars "ConnectionStrings__DefaultConnection=Server=tcp:<server>.database.windows.net,1433;Initial Catalog=GroceryHub;User ID=groceryhub_app;Password=<pass>;Encrypt=True;TrustServerCertificate=False;"
```

### Bước 4: Triển khai Backend API Container
Triển khai service `groceryhub-api` trên mạng nội bộ (Internal Ingress, cổng 8080):
- **Health check path:** `/health/live` và `/health/ready`
- **Biến môi trường:**
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `App__PublicBaseUrl=https://grocery.dangbinhduong.io.vn`
  - `Jwt__Issuer=https://grocery.dangbinhduong.io.vn`
  - `Jwt__Audience=https://grocery.dangbinhduong.io.vn`
  - `Jwt__SigningKey=<Chuỗi ngẫu nhiên tối thiểu 32 bytes>`
  - `SePay__Environment=Production`
  - `SePay__Webhook__AuthMode=Hmac`
  - `SePay__Webhook__HmacSecret=<Secret từ SePay Dashboard>`
  - `Smtp__Host=<smtp.sendgrid.net hoặc tương đương>`
  - `Storage__ImagesPath=/app/var/product-images` (gắn Azure Files / AWS EFS volume)

### Bước 5: Triển khai Web Gateway Container
Triển khai service `groceryhub-web` với External Ingress (cổng 8080):
- Nginx sẽ tự động phục vụ React 19 SPA, đồng thời reverse proxy đường dẫn `/api/` và `/health/` về `http://groceryhub-api:8080`.
- Đường dẫn webhook `/api/v1/payments/sepay/webhook` được bảo vệ bằng rate limit và kiểm tra IP allowlist.

---

## 1.3. Kịch bản B: Triển khai trên Linux VPS với Docker Compose

### Bước 1: Cài đặt Docker & Docker Compose trên Ubuntu VPS
```bash
sudo apt update && sudo apt install -y curl git ufw
curl -fsSL https://get.docker.com -o get-docker.sh && sudo sh get-docker.sh
sudo usermod -aG docker $USER
```

### Bước 2: Clone source code & Cấu hình môi trường
```bash
git clone https://github.com/dangbinhduongwork-ai/Grocery.git /opt/groceryhub
cd /opt/groceryhub
cp .env.example .env
```
Mở file `.env` và điền các giá trị thực tế:
- Đổi `MSSQL_SA_PASSWORD` thành mật khẩu mạnh.
- Đổi `MSSQL_PID=Express` (miễn phí thương mại).
- Đổi `Jwt__SigningKey` bằng chuỗi 32 bytes: `openssl rand -hex 32`.
- Điền thông tin SePay nhận thanh toán và SMTP gửi email.

### Bước 3: Khởi động hệ thống với Docker Compose
```bash
# Triển khai bản production (chỉ mở cổng web 8080 ra host, SQL Server và API được cô lập)
docker compose -f docker-compose.yml up -d --build
```
Kiểm tra trạng thái các container:
```bash
docker compose ps
# Đảm bảo các service sqlserver, api, web đều có trạng thái "healthy"
```

---

## 1.4. Chạy Migration Cơ sở dữ liệu
Trong Docker Compose, service `migrator` tự động thực thi bundle trước khi `api` khởi động nhờ cấu hình:
```yaml
depends_on:
  migrator:
    condition: service_completed_successfully
```
Nếu cần chạy migration thủ công từ máy quản trị:
```bash
docker compose run --rm migrator
```

---

## 1.5. Cấu hình Tên miền & Chứng chỉ SSL
Trỏ bản ghi DNS của domain `grocery.dangbinhduong.io.vn` về IP của Cloud Load Balancer hoặc VPS.

Trên VPS, sử dụng Caddy hoặc Nginx + Certbot làm Reverse Proxy SSL:
**Ví dụ cấu hình Caddyfile (`/etc/caddy/Caddyfile`):**
```caddy
grocery.dangbinhduong.io.vn {
    reverse_proxy 127.0.0.1:8080 {
        header_up Host {host}
        header_up X-Real-IP {remote_host}
        header_up X-Forwarded-For {remote_host}
        header_up X-Forwarded-Proto {scheme}
    }
}
```
Khởi động Caddy: `sudo systemctl restart caddy`. Caddy sẽ tự động cấp chứng chỉ Let's Encrypt SSL miễn phí.

---

## 1.6. Kiểm thử Smoke Test sau triển khai
Sau khi triển khai xong, chạy script smoke test để kiểm tra toàn diện 6 tính năng cốt lõi:
```bash
# Trên Linux VPS:
bash scripts/smoke-test.sh https://grocery.dangbinhduong.io.vn

# Hoặc trên Windows PowerShell:
pwsh ./scripts/smoke-test.ps1 -BaseUrl https://grocery.dangbinhduong.io.vn
```
Kết quả kỳ vọng: **7/7 tests Passed** (Health, Headers, Categories, Products, Register, Login, Webhook 401 Rejection).

---

# 2. ENGLISH: DETAILED PRODUCTION DEPLOYMENT GUIDE

## 2.1. Architecture & Confirmed Decisions
Per Architectural Decision **DEC-018**:
- **Official Domain:** `grocery.dangbinhduong.io.vn`
- **Production Database:** Azure SQL Database or AWS RDS for SQL Server (Microsoft licensing compliance; `Developer` edition prohibited in production).
- **Runtime Compute:** Managed Container Platform (Azure Container Apps / AWS ECS) for production; Docker Compose for Staging.
- **Transactional SMTP:** Production SMTP relay (Resend, SendGrid, Brevo, AWS SES); Mailpit for Staging.

## 2.2. Scenario A: Cloud Container Platform Deployment

### Step 1: Managed SQL Database Provisioning
1. Create an Azure SQL or AWS RDS database instance.
2. Restrict firewall rules to accept connections exclusively from container subnets on port 1433.
3. Run least-privilege login provisioning: `scripts/sql/01-create-app-login.sql`.
4. Validate non-blocking reader isolation: `scripts/sql/02-verify-rcsi.sql`.

### Step 2: Build & Push Images to GHCR
Tag and push release to trigger CI/CD pipeline:
```bash
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0
```

### Step 3: Run Database Migrator Job
Execute the self-contained `efbundle` as an isolated pre-deployment job:
```bash
az containerapp job start --name groceryhub-migrator --resource-group groceryhub-rg
```

### Step 4: Deploy API & Web Containers
1. Deploy `api` container in internal network mode with `/health/live` and `/health/ready` probes.
2. Deploy `web` container in external ingress mode with port 8080 exposed.

## 2.3. Scenario B: Single Linux VPS Deployment with Docker Compose
1. Install Docker Engine and Docker Compose v2.
2. Clone repository to `/opt/groceryhub`.
3. Configure `.env` from template: `cp .env.example .env`.
4. Launch production stack:
   ```bash
   docker compose -f docker-compose.yml up -d --build
   ```
5. Confirm port isolation (SQL Server port 1433 is internal only):
   ```bash
   pwsh ./scripts/verify-ports.ps1
   ```

## 2.4. Running Database Migrations
Migrations execute via the standalone bundle `Dockerfile.api` (target `migrator`). In compose, it runs automatically before the API starts.

## 2.5. DNS & TLS Configuration
Point A record `grocery.dangbinhduong.io.vn` to host/load-balancer IP. Enable automatic HTTPS with Caddy, Certbot, or Cloudflare.

## 2.6. Post-Deployment Verification
Execute automated smoke test:
```bash
bash scripts/smoke-test.sh https://grocery.dangbinhduong.io.vn
```
Verify zero failures and HTTP 200 on all core endpoints.
