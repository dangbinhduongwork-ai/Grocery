# GroceryHub — Final RBAC & Permissions Matrix Audit Report

**Date:** 2026-11-15  
**Auditor:** `security-reviewer` & `project-manager`  
**Phase:** 11 — Testing & Security Hardening  
**Status:** Verified — Default-Deny Active, 100% Endpoints Governed by Named Policies or Explicit AllowAnonymous

---

## 1. Executive Summary

This report documents the exhaustive verification of server-side Role-Based Access Control (RBAC) and object-level permissions across all API endpoints in GroceryHub. Enforced through ASP.NET Core endpoint authorization policies, the application adheres strictly to the **Default-Deny** security posture verified by architecture tests (`ArchitectureTests.Default_Deny_Endpoints_Require_Authorization_Or_AllowAnonymous`).

### Persona Roles:
1. **Anonymous (`Anon`):** Unauthenticated visitor without bearer token.
2. **Customer (`Cust`):** Authenticated shopper with standard retail customer privileges (no administrative permissions).
3. **Staff (`Stf`):** Operational store personnel with operational permissions (`orders.process`, `orders.ship`, `inventory.read`, `catalog.read`, `purchasing.read`, `customers.read`).
4. **Manager (`Mgr`):** Store manager with elevated operational, managerial, and reporting permissions (`staff.read`, `refunds.approve`, `payments.review`, `reports.read`, etc.).
5. **Admin (`Adm`):** System administrator possessing full access across all 29 permissions.

---

## 2. Complete Endpoint Authorization Matrix

| # | Endpoint Route | HTTP | Policy / Requirement | Anon | Cust | Stf | Mgr | Adm | Verification Evidence |
|---|---|:---:|---|:---:|:---:|:---:|:---:|:---:|---|
| **Identity & Authentication** |
| 1 | `/api/v1/auth/register` | POST | `AllowAnonymous` | 201 | 201 | 201 | 201 | 201 | `AuthLifecycleIntegrationTests` |
| 2 | `/api/v1/auth/verify-email` | POST | `AllowAnonymous` | 200 | 200 | 200 | 200 | 200 | `AuthLifecycleIntegrationTests` |
| 3 | `/api/v1/auth/resend-verification` | POST | `AllowAnonymous` | 202 | 202 | 202 | 202 | 202 | `NonEnumerationIntegrationTests` |
| 4 | `/api/v1/auth/login` | POST | `AllowAnonymous` | 200 | 200 | 200 | 200 | 200 | `AuthLifecycleIntegrationTests` |
| 5 | `/api/v1/auth/refresh` | POST | `AllowAnonymous` | 200 | 200 | 200 | 200 | 200 | `RefreshTokenReuseIntegrationTests` |
| 6 | `/api/v1/auth/forgot-password` | POST | `AllowAnonymous` | 202 | 202 | 202 | 202 | 202 | `NonEnumerationIntegrationTests` |
| 7 | `/api/v1/auth/reset-password` | POST | `AllowAnonymous` | 200 | 200 | 200 | 200 | 200 | `AuthLifecycleIntegrationTests` |
| 8 | `/api/v1/auth/me` | GET | `auth` | 401 | 200 | 200 | 200 | 200 | `RbacMatrixIntegrationTests` |
| 9 | `/api/v1/auth/logout` | POST | `auth` | 401 | 204 | 204 | 204 | 204 | `RbacMatrixIntegrationTests` |
| 10 | `/api/v1/auth/change-password` | POST | `auth` | 401 | 200 | 200 | 200 | 200 | Unit & Integration suite |
| **Catalog Storefront (Public)** |
| 11 | `/api/v1/categories` | GET | `AllowAnonymous` | 200 | 200 | 200 | 200 | 200 | Catalog Integration Tests |
| 12 | `/api/v1/brands` | GET | `AllowAnonymous` | 200 | 200 | 200 | 200 | 200 | Catalog Integration Tests |
| 13 | `/api/v1/products` | GET | `AllowAnonymous` | 200 | 200 | 200 | 200 | 200 | `E2EJourneysIntegrationTests (J1)` |
| 14 | `/api/v1/products/{slug}` | GET | `AllowAnonymous` | 200 | 200 | 200 | 200 | 200 | Catalog Integration Tests |
| 15 | `/api/v1/home` | GET | `AllowAnonymous` | 200 | 200 | 200 | 200 | 200 | Catalog Integration Tests |
| 16 | `/api/v1/banners` | GET | `AllowAnonymous` | 200 | 200 | 200 | 200 | 200 | Catalog Integration Tests |
| 17 | `/api/v1/products/{id}/reviews` | GET | `AllowAnonymous` | 200 | 200 | 200 | 200 | 200 | `E2EJourneysIntegrationTests (J1)` |
| **Customer Shopping & Checkout** |
| 18 | `/api/v1/cart` | GET | `AllowAnonymous` / Session | 200 | 200 | 200 | 200 | 200 | `CustomerAndCartIntegrationTests` |
| 19 | `/api/v1/cart/items` | POST | `AllowAnonymous` / Session | 200 | 200 | 200 | 200 | 200 | `E2EJourneysIntegrationTests (J1)` |
| 20 | `/api/v1/cart/items/{id}` | PUT | `AllowAnonymous` / Session | 200 | 200 | 200 | 200 | 200 | `CustomerAndCartIntegrationTests` |
| 21 | `/api/v1/cart/coupon` | POST | `auth` | 401 | 200 | 200 | 200 | 200 | `CustomerAndCartIntegrationTests` |
| 22 | `/api/v1/me/addresses` | GET/POST | `auth` + IDOR check | 401 | 200 | 200 | 200 | 200 | `E2EJourneysIntegrationTests (J7)` |
| 23 | `/api/v1/checkout/quote` | POST | `auth` | 401 | 200 | 200 | 200 | 200 | `PriceTamperingIntegrationTests` |
| 24 | `/api/v1/checkout/orders` | POST | `auth` | 401 | 201 | 201 | 201 | 201 | `E2EJourneysIntegrationTests (J1, J2)` |
| 25 | `/api/v1/me/orders` | GET | `auth` + IDOR check | 401 | 200 | 200 | 200 | 200 | `OrderOwnershipIntegrationTests` |
| 26 | `/api/v1/me/orders/{id}` | GET | `auth` + IDOR check | 401 | 200*| 200*| 200*| 200*| `OrderOwnershipIntegrationTests` |
| 27 | `/api/v1/products/{id}/reviews` | POST | `auth` + Purchase Check | 401 | 201 | 201 | 201 | 201 | `E2EJourneysIntegrationTests (J1)` |
| 28 | `/api/v1/payments/{code}/status` | GET | `auth` + IDOR check | 401 | 200 | 200 | 200 | 200 | `E2EJourneysIntegrationTests (J2)` |
| 29 | `/api/v1/payments/sepay/webhook`| POST | Webhook HMAC Sign | 401 | 401 | 401 | 401 | 401 | `SePaySignatureAndUrlTests` |
| **Admin Operations & Catalog Management** |
| 30 | `/api/v1/admin/categories` | POST/PUT | `catalog.manage` | 401 | 403 | 403 | 200 | 200 | `E2EJourneysIntegrationTests (J6)` |
| 31 | `/api/v1/admin/brands` | POST/PUT | `catalog.manage` | 401 | 403 | 403 | 200 | 200 | Catalog Admin Tests |
| 32 | `/api/v1/admin/products` | POST/PUT | `catalog.manage` | 401 | 403 | 403 | 201 | 201 | `E2EJourneysIntegrationTests (J4)` |
| 33 | `/api/v1/admin/products/{id}/status` | PUT | `catalog.manage` | 401 | 403 | 403 | 200 | 200 | Catalog Admin Tests |
| 34 | `/api/v1/admin/products/{id}/images` | POST | `catalog.manage` | 401 | 403 | 403 | 201 | 201 | Security File Upload Audit |
| 35 | `/api/v1/admin/inventory` | GET | `inventory.read` | 401 | 403 | 200 | 200 | 200 | Inventory Admin Tests |
| 36 | `/api/v1/admin/inventory/adjustments`| POST | `inventory.manage` | 401 | 403 | 403 | 201 | 201 | Inventory Admin Tests |
| 37 | `/api/v1/admin/suppliers` | GET/POST | `purchasing.read/manage` | 401 | 403 | 403 | 200 | 200 | Purchasing Integration Tests |
| 38 | `/api/v1/admin/purchasing/orders` | GET/POST | `purchasing.read/manage` | 401 | 403 | 403 | 200 | 200 | `E2EJourneysIntegrationTests (J4)` |
| 39 | `/api/v1/admin/purchasing/orders/{id}/receive` | POST | `purchasing.receive` | 401 | 403 | 200 | 200 | 200 | `E2EJourneysIntegrationTests (J4)` |
| 40 | `/api/v1/admin/orders` | GET | `orders.read` | 401 | 403 | 200 | 200 | 200 | `E2EJourneysIntegrationTests (J6)` |
| 41 | `/api/v1/admin/orders/{id}/transition` | POST | `orders.process`/`orders.ship` | 401 | 403 | 200 | 200 | 200 | `E2EJourneysIntegrationTests (J1)` |
| 42 | `/api/v1/admin/orders/{id}/cancel` | POST | `orders.cancel` | 401 | 403 | 403 | 200 | 200 | `OrderStateMachineIntegrationTests` |
| 43 | `/api/v1/admin/coupons` | GET/POST | `promotions.manage` | 401 | 403 | 403 | 200 | 200 | Promotions Integration Tests |
| 44 | `/api/v1/admin/reviews` | GET | `reviews.moderate` | 401 | 403 | 403 | 200 | 200 | Reviews Admin Tests |
| 45 | `/api/v1/admin/reviews/{id}/approve` | POST | `reviews.moderate` | 401 | 403 | 403 | 200 | 200 | `E2EJourneysIntegrationTests (J1)` |
| 46 | `/api/v1/admin/reviews/{id}/reject` | POST | `reviews.moderate` | 401 | 403 | 403 | 200 | 200 | Reviews Admin Tests |
| **Financial, Payment Anomaly & Refunds** |
| 47 | `/api/v1/admin/payments` | GET | `payments.read` | 401 | 403 | 403 | 200 | 200 | Payments Admin Tests |
| 48 | `/api/v1/admin/payment-transactions` | GET | `payments.review` | 401 | 403 | 403 | 200 | 200 | `E2EJourneysIntegrationTests (J5)` |
| 49 | `/api/v1/admin/payment-transactions/{id}/resolve` | POST | `payments.review` | 401 | 403 | 403 | 200 | 200 | `E2EJourneysIntegrationTests (J5)` |
| 50 | `/api/v1/admin/refunds` | GET/POST | `refunds.request` | 401 | 403 | 403 | 200 | 200 | `E2EJourneysIntegrationTests (J5)` |
| 51 | `/api/v1/admin/refunds/{id}/approve` | POST | `refunds.approve` (Two-person rule) | 401 | 403 | 403 | 200*| 200*| `SePayPaymentIntegrationTests (T_PAY_18)` |
| 52 | `/api/v1/admin/refunds/{id}/complete`| POST | `refunds.approve` | 401 | 403 | 403 | 200 | 200 | `E2EJourneysIntegrationTests (J5)` |
| **Executive Reports & Governance** |
| 53 | `/api/v1/admin/reports/sales` | GET | `reports.read` | 401 | 403 | 403 | 200 | 200 | Reporting Integration Tests |
| 54 | `/api/v1/admin/reports/profit` | GET | `profit.read` (Admin-only) | 401 | 403 | 403 | 403 | 200 | `E2EJourneysIntegrationTests (J6)` |
| 55 | `/api/v1/admin/reports/stock-valuation` | GET | `cost.read` (Admin-only) | 401 | 403 | 403 | 403 | 200 | `E2EJourneysIntegrationTests (J6)` |
| 56 | `/api/v1/admin/staff` | GET/POST | `staff.read`/`staff.manage` | 401 | 403 | 403 | 200/403 | 200 | `RbacMatrixIntegrationTests` |
| 57 | `/api/v1/admin/staff/{id}/roles` | PUT | `staff.manage` | 401 | 403 | 403 | 403 | 200 | `RbacMatrixIntegrationTests` |
| 58 | `/api/v1/admin/roles` | GET | `roles.manage` | 401 | 403 | 403 | 403 | 200 | `RbacMatrixIntegrationTests` |
| 59 | `/api/v1/admin/permissions` | GET | `roles.manage` | 401 | 403 | 403 | 403 | 200 | `RbacMatrixIntegrationTests` |
| 60 | `/api/v1/admin/audit-logs` | GET | `audit.read` | 401 | 403 | 403 | 403 | 200 | Audit Integration Tests |
| 61 | `/api/v1/admin/settings` | GET/PUT | `settings.manage` | 401 | 403 | 403 | 403 | 200 | `E2EJourneysIntegrationTests (J6)` |

*\* Notes on Object-Level Checks:*
- Line 26: Returns `200` only if the order belongs to the requesting user (`order.UserId == userId`); otherwise returns `403 Forbidden` (`OrderOwnershipIntegrationTests`).
- Line 51: Two-person rule prevents self-approval. Returns `409 Conflict` if requester attempts approval (`T_PAY_18`, `Journey_J5`).
