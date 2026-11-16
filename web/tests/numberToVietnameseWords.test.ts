import { describe, it, expect } from 'vitest'
import { numberToVietnameseWords } from '@/shared/utils/numberToVietnameseWords'

describe('numberToVietnameseWords', () => {
  it('converts small amounts', () => {
    expect(numberToVietnameseWords(0)).toBe('Không đồng')
    expect(numberToVietnameseWords(50000)).toBe('Năm mươi nghìn đồng')
  })

  it('converts typical grocery order amounts', () => {
    expect(numberToVietnameseWords(300000)).toBe('Ba trăm nghìn đồng')
    expect(numberToVietnameseWords(125000)).toBe('Một trăm hai mươi lăm nghìn đồng')
    expect(numberToVietnameseWords(105000)).toBe('Một trăm lẻ năm nghìn đồng')
  })

  it('converts millions for large bulk orders', () => {
    expect(numberToVietnameseWords(1500000)).toBe('Một triệu năm trăm nghìn đồng')
    expect(numberToVietnameseWords(2000000)).toBe('Hai triệu đồng')
  })
})
