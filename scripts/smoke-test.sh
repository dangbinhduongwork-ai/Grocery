#!/usr/bin/env bash
# =============================================================================
# GroceryHub — Post-Deployment Smoke Test (Bash / Linux)
# Verifies health, static SPA delivery, category listing, user auth flow,
# product catalog query, and webhook signature rejection on a deployed stack.
# =============================================================================
set -euo pipefail

BASE_URL="${1:-http://localhost:8080}"
PASSED=0
FAILED=0

echo "============================================================"
echo " GroceryHub Post-Deployment Smoke Test (Linux/Bash)"
echo " Base URL: ${BASE_URL}"
echo " Time:     $(date -u +"%Y-%m-%d %H:%M:%S UTC")"
echo "============================================================"

assert_step() {
    local name="$1"
    shift
    printf "[TEST] %s ... " "${name}"
    if "$@"; then
        echo "PASS"
        PASSED=$((PASSED + 1))
    else
        echo "FAIL"
        FAILED=$((FAILED + 1))
    fi
}

check_health_live() {
    local res
    res=$(curl -s -f "${BASE_URL}/health/live")
    echo "${res}" | grep -q "Healthy"
}

check_health_ready() {
    local res
    res=$(curl -s -f "${BASE_URL}/health/ready")
    echo "${res}" | grep -q "Healthy"
}

check_spa_and_headers() {
    local headers
    headers=$(curl -s -I "${BASE_URL}/")
    echo "${headers}" | grep -qi "X-Content-Type-Options: nosniff" && \
    echo "${headers}" | grep -qi "X-Frame-Options: DENY" && \
    echo "${headers}" | grep -qi "Content-Security-Policy:"
}

check_categories() {
    local res
    res=$(curl -s -f "${BASE_URL}/api/v1/categories")
    echo "${res}" | grep -q '^\['
}

check_products() {
    local res
    res=$(curl -s -f "${BASE_URL}/api/v1/products?pageSize=1")
    echo "${res}" | grep -q '"items":\['
}

RANDOM_ID=$(head /dev/urandom | tr -dc a-z0-9 | head -c 8 || echo "smoke$$")
TEST_EMAIL="smoke_${RANDOM_ID}@groceryhub.test"
TEST_PASS="Smoke_Password_2026!"

check_register_and_login() {
    # 1. Register
    local reg_code
    reg_code=$(curl -s -o /dev/null -w "%{http_code}" -X POST "${BASE_URL}/api/v1/auth/register" \
        -H "Content-Type: application/json" \
        -d "{\"email\":\"${TEST_EMAIL}\",\"password\":\"${TEST_PASS}\",\"fullName\":\"Smoke Customer\"}")
    if [[ "${reg_code}" != "200" && "${reg_code}" != "201" ]]; then
        return 1
    fi

    # 2. Login
    local login_res
    login_res=$(curl -s -f -X POST "${BASE_URL}/api/v1/auth/login" \
        -H "Content-Type: application/json" \
        -d "{\"email\":\"${TEST_EMAIL}\",\"password\":\"${TEST_PASS}\"}")
    echo "${login_res}" | grep -q '"accessToken":'
}

check_webhook_unauthorized() {
    local code
    code=$(curl -s -o /dev/null -w "%{http_code}" -X POST "${BASE_URL}/api/v1/payments/sepay/webhook" \
        -H "Content-Type: application/json" \
        -d '{"id":999999,"gateway":"MBBank","accountNumber":"0000000000","code":"GH9999999999","transferAmount":50000}')
    # Must reject without valid signature / key (401 or 403)
    [[ "${code}" == "401" || "${code}" == "403" ]]
}

assert_step "Health Live Endpoint (GET /health/live)" check_health_live
assert_step "Health Ready Endpoint (GET /health/ready)" check_health_ready
assert_step "Static SPA & Security Headers (GET /)" check_spa_and_headers
assert_step "Public Categories (GET /api/v1/categories)" check_categories
assert_step "Public Products (GET /api/v1/products?pageSize=1)" check_products
assert_step "Customer Register & Login Auth Flow" check_register_and_login
assert_step "SePay Webhook Rejection of Unsigned Request" check_webhook_unauthorized

echo "============================================================"
echo " Smoke Test Results: ${PASSED} Passed, ${FAILED} Failed"
echo "============================================================"

if [ "${FAILED}" -gt 0 ]; then
    exit 1
fi
