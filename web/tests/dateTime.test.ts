import { describe, it, expect } from 'vitest'
import { formatVnDateTime, formatVnDate } from '@/shared/utils/dateTime'

describe('dateTime utils', () => {
  it('formats ISO date to Vietnamese date string', () => {
    // 2026-11-15T12:00:00Z
    const result = formatVnDate('2026-11-15T12:00:00Z')
    expect(result).toBe('15/11/2026')
  })

  it('formats ISO date to Vietnamese date and time in Asia/Ho_Chi_Minh', () => {
    // 2026-11-15T03:00:00Z is 10:00:00 in UTC+7 (Asia/Ho_Chi_Minh)
    const result = formatVnDateTime('2026-11-15T03:00:00Z')
    expect(result).toContain('15/11/2026')
    expect(result).toContain('10:00')
  })

  it('handles null and undefined gracefully', () => {
    expect(formatVnDate(null)).toBe('')
    expect(formatVnDateTime(undefined)).toBe('')
    expect(formatVnDateTime('invalid-date')).toBe('')
  })
})
