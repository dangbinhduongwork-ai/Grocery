import { describe, it, expect } from 'vitest'
import { formatVnd } from '@/shared/utils/formatVnd'

describe('formatVnd', () => {
  it('formats positive integers as Vietnamese currency with dot grouping', () => {
    const formatted = formatVnd(150000)
    // Non-breaking space or standard space before ₫
    expect(formatted).toMatch(/150\.000[\s\u00A0]*₫/)
  })

  it('formats zero correctly', () => {
    const formatted = formatVnd(0)
    expect(formatted).toMatch(/0[\s\u00A0]*₫/)
  })

  it('formats millions and billions correctly without decimal places', () => {
    const formatted = formatVnd(12500000)
    expect(formatted).toMatch(/12\.500\.000[\s\u00A0]*₫/)
    expect(formatted).not.toContain(',00')
  })
})
