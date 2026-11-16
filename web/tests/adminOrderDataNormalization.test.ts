import { describe, it, expect } from 'vitest'
import { formatVnd } from '@/shared/utils/formatVnd'
import { formatDateTime } from '@/shared/utils/dateTime'
import { numberToVietnameseWords } from '@/shared/utils/numberToVietnameseWords'

describe('Admin Order Data Normalization & Invoice Formatting', () => {
  const backendSampleOrder = {
    id: 'b16727be-4a4c-4206-a2a7-a0d674fbb8d2',
    orderNumber: 'ORD-20261116-000008',
    status: 'Confirmed',
    paymentMethod: 'SEPAY_QR',
    paymentStatus: 'Paid',
    subtotal: 18000,
    couponDiscount: 0,
    shippingFee: 35000,
    total: 53000,
    address: {
      recipientName: 'dang binh duong',
      phone: '0865087784',
      provinceName: 'Tỉnh Bình Dương',
      wardName: 'Phường An Thạnh',
      street: 'làhjasddk',
      note: null,
    },
    items: [
      {
        id: '6a8673f6-f8e7-45dd-bc4f-8b4de9c99056',
        productId: '46bd709e-8ae1-49dc-9f09-03263eb6ed3f',
        sku: 'GH-DU-0009',
        name: 'Trà xanh Không Độ chai 455ml vị chanh',
        unit: 'Chai',
        unitPrice: 9000,
        quantity: 2,
        lineSubtotal: 18000,
        discountAllocated: 0,
        lineTotal: 18000,
      },
    ],
    timeline: [
      {
        toStatus: 'PendingPayment',
        reason: 'Order placed with SePay VietQR payment method; pending customer transfer',
        actorRole: 'Customer',
        createdAt: '2026-11-16T12:14:22.913864+00:00',
      },
      {
        fromStatus: 'PendingPayment',
        toStatus: 'Confirmed',
        reason: 'Thanh toán thành công qua SePay VietQR',
        actorRole: 'System',
        createdAt: '2026-11-16T12:14:58.1458202+00:00',
      },
    ],
    createdAt: '2026-11-16T12:14:22.9190241+00:00',
  }

  it('correctly extracts recipient and address fields without throwing', () => {
    const recipient = backendSampleOrder.address?.recipientName || 'Khách hàng'
    const phone = backendSampleOrder.address?.phone || ''
    const street = backendSampleOrder.address?.street || ''
    const wardName = backendSampleOrder.address?.wardName || ''
    const provinceName = backendSampleOrder.address?.provinceName || ''
    const fullAddress = [street, wardName, provinceName].filter(Boolean).join(', ')

    expect(recipient).toBe('dang binh duong')
    expect(phone).toBe('0865087784')
    expect(fullAddress).toBe('làhjasddk, Phường An Thạnh, Tỉnh Bình Dương')
  })

  it('maps timeline entries safely without crashing on missing statusHistory', () => {
    const timeline = backendSampleOrder.timeline ?? []
    expect(timeline.length).toBe(2)
    expect(timeline[0].toStatus).toBe('PendingPayment')
    expect(timeline[1].toStatus).toBe('Confirmed')
  })

  it('formats total amount and words correctly for invoice', () => {
    expect(formatVnd(backendSampleOrder.total)).toContain('53.000')
    const words = numberToVietnameseWords(backendSampleOrder.total)
    expect(words.toLowerCase()).toContain('năm mươi ba nghìn')
  })

  it('formats item name and sku correctly without undefined', () => {
    const item = backendSampleOrder.items[0]
    const name = item.name ?? 'Sản phẩm'
    const sku = item.sku ?? '—'
    const unitPrice = formatVnd(item.unitPrice)
    const lineTotal = formatVnd(item.lineTotal)

    expect(name).toBe('Trà xanh Không Độ chai 455ml vị chanh')
    expect(sku).toBe('GH-DU-0009')
    expect(unitPrice).toContain('9.000')
    expect(lineTotal).toContain('18.000')
  })
})
