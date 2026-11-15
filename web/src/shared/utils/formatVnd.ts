/**
 * Formats an integer VND amount according to Vietnamese locale standards.
 * Example: 120000 -> "120.000 ₫"
 */
export function formatVnd(amount: number | bigint): string {
  const numeric = typeof amount === 'bigint' ? Number(amount) : amount
  if (isNaN(numeric)) {
    return '0 ₫'
  }

  const formatted = new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(numeric)

  return formatted
}
