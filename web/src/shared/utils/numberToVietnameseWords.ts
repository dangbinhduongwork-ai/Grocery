/**
 * Convert number to Vietnamese text for financial receipts and invoices.
 * Example: 250000 -> "Hai trăm năm mươi nghìn đồng"
 */
const DIGITS = ['không', 'một', 'hai', 'ba', 'bốn', 'năm', 'sáu', 'bảy', 'tám', 'chín']

function readThreeDigits(n: number, readZeroHundred = false): string {
  const hundred = Math.floor(n / 100)
  const remainder = n % 100
  const ten = Math.floor(remainder / 10)
  const unit = remainder % 10
  const parts: string[] = []

  if (hundred > 0 || readZeroHundred) {
    parts.push(DIGITS[hundred] + ' trăm')
  }

  if (ten > 1) {
    parts.push(DIGITS[ten] + ' mươi')
    if (unit === 1) parts.push('mốt')
    else if (unit === 5) parts.push('lăm')
    else if (unit > 0) parts.push(DIGITS[unit])
  } else if (ten === 1) {
    parts.push('mười')
    if (unit === 5) parts.push('lăm')
    else if (unit > 0) parts.push(DIGITS[unit])
  } else if (unit > 0) {
    if (hundred > 0 || readZeroHundred) parts.push('lẻ')
    parts.push(DIGITS[unit])
  }

  return parts.join(' ')
}

export function numberToVietnameseWords(amount: number): string {
  if (amount <= 0 || isNaN(amount)) return 'Không đồng'

  const units = ['', 'nghìn', 'triệu', 'tỷ', 'nghìn tỷ']
  let num = Math.floor(amount)
  const groups: number[] = []

  while (num > 0) {
    groups.push(num % 1000)
    num = Math.floor(num / 1000)
  }

  const parts: string[] = []
  for (let i = groups.length - 1; i >= 0; i--) {
    const g = groups[i]
    if (g > 0) {
      const readZero = i < groups.length - 1
      const text = readThreeDigits(g, readZero)
      parts.push(text)
      if (units[i]) {
        parts.push(units[i])
      }
    }
  }

  const result = parts.join(' ').trim()
  if (!result) return 'Không đồng'

  // Capitalize first letter and append 'đồng'
  const capitalized = result.charAt(0).toUpperCase() + result.slice(1)
  return `${capitalized} đồng`
}
