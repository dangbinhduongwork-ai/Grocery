import React, { useEffect, useState, useCallback, useMemo } from 'react'
import {
  ArrowLeft,
  Check,
  CheckCircle2,
  Package,
  Truck,
  XCircle,
  Clock,
  User,
  MapPin,
  AlertCircle,
  AlertTriangle,
  RotateCcw,
  Send,
  Printer,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { formatVnd } from '@/shared/utils/formatVnd'
import { formatDateTime } from '@/shared/utils/dateTime'
import { useRouter } from '@/app/router'
import { useAuth } from '@/shared/auth'
import { StatusChip } from '../components/StatusChip'
import { OrderInvoiceModal, InvoiceOrderData } from '../components/OrderInvoiceModal'

interface OrderItem {
  id?: string
  productId?: string
  sku?: string
  productSku?: string
  name?: string
  productName?: string
  unit?: string
  productUnit?: string
  packSize?: string | null
  unitPrice: number | string
  quantity: number | string
  subtotal?: number | string
  lineSubtotal?: number | string
  lineTotal?: number | string
  costPrice?: number | string
  unitCostSnapshot?: number | string
}

interface OrderHistoryItem {
  id?: string
  fromStatus?: string | null
  toStatus: string
  reason?: string | null
  actorRole?: string | null
  createdAt: string
}

interface OrderAddress {
  recipientName?: string
  phone?: string
  provinceName?: string
  wardName?: string
  street?: string
  note?: string | null
}

interface OrderDetail {
  id: string
  orderNumber: string
  status: string
  paymentMethod: string
  paymentStatus: string
  subtotal: number | string
  couponCode?: string | null
  couponDiscount: number | string
  shippingFee: number | string
  total: number | string
  address?: OrderAddress
  shippingRecipientName?: string
  shippingPhone?: string
  shippingProvinceName?: string
  shippingWardName?: string
  shippingStreet?: string
  shippingNote?: string
  customerNote?: string | null
  internalNote?: string | null
  cancellationReason?: string | null
  cancelledAt?: string | null
  items?: OrderItem[]
  timeline?: OrderHistoryItem[]
  statusHistory?: OrderHistoryItem[]
  createdAt?: string
  placedAt?: string
}

export const AdminOrderDetailPage: React.FC<{ id: string }> = ({ id }) => {
  const router = useRouter()
  const { hasPermission } = useAuth()
  const [order, setOrder] = useState<OrderDetail | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [internalNote, setInternalNote] = useState('')
  const [errorMsg, setErrorMsg] = useState<string | null>(null)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)
  const [isInvoiceOpen, setIsInvoiceOpen] = useState(false)

  const ORDER_STEPS = useMemo(
    () => [
      { key: 'Placed', label: '1. Đã đặt hàng' },
      { key: 'Confirmed', label: '2. Đã xác nhận' },
      { key: 'Preparing', label: '3. Chuẩn bị hàng' },
      { key: 'Packed', label: '4. Đã đóng gói' },
      { key: 'Shipping', label: '5. Đang giao hàng' },
      { key: 'Delivered', label: '6. Giao thành công' },
    ],
    []
  )

  const currentStepIndex = useMemo(() => {
    if (!order) return 0
    switch (order.status) {
      case 'PendingPayment':
      case 'Placed':
        return 0
      case 'Confirmed':
        return 1
      case 'Preparing':
        return 2
      case 'Packed':
        return 3
      case 'Shipping':
        return 4
      case 'Delivered':
        return 5
      default:
        return -1
    }
  }, [order])

  const fetchDetail = useCallback(async () => {
    setIsLoading(true)
    setErrorMsg(null)
    try {
      const res = await api.GET('/api/v1/admin/orders/{id}', {
        params: { path: { id } },
      })
      if (res.data) {
        const d = res.data as unknown as OrderDetail
        setOrder(d)
        setInternalNote(d.internalNote ?? '')
      } else if (res.error) {
        setErrorMsg('Không tìm thấy đơn hàng hoặc phiên làm việc đã hết hạn.')
      }
    } catch {
      setErrorMsg('Không thể kết nối đến máy chủ để tải thông tin đơn hàng.')
    } finally {
      setIsLoading(false)
    }
  }, [id])

  useEffect(() => {
    void fetchDetail()
  }, [fetchDetail])

  const handleTransition = async (toStatus: string, reason?: string) => {
    if (!order) return
    setIsSubmitting(true)
    setErrorMsg(null)
    setSuccessMsg(null)

    try {
      const totalAmount = Number(order.total) || 0
      const isCod = order.paymentMethod?.toUpperCase() === 'COD'
      const body = {
        to: toStatus,
        reason: reason ?? null,
        codAmount: toStatus === 'Delivered' && isCod ? totalAmount : null,
      }

      const res = await api.POST('/api/v1/admin/orders/{id}/transition', {
        params: { path: { id: order.id } },
        body,
      })

      if (res.error) {
        const detail = (res.error as { detail?: string })?.detail ?? 'Chuyển trạng thái thất bại.'
        setErrorMsg(detail)
      } else {
        setSuccessMsg(`Đã cập nhật đơn hàng sang trạng thái ${toStatus}.`)
        if (res.data) {
          setOrder(res.data as unknown as OrderDetail)
        }
        await fetchDetail()
      }
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : 'Lỗi kết nối máy chủ.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleSaveNote = async () => {
    if (!order) return
    setIsSubmitting(true)
    setErrorMsg(null)
    setSuccessMsg(null)
    try {
      await api.PUT('/api/v1/admin/orders/{id}/note', {
        params: { path: { id: order.id } },
        body: { internalNote },
      })
      setSuccessMsg('Đã lưu ghi chú nội bộ thành công.')
    } catch {
      setErrorMsg('Không thể lưu ghi chú nội bộ.')
    } finally {
      setIsSubmitting(false)
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-6 bg-slate-200 rounded-md w-32 animate-pulse" />
        <div className="h-64 bg-white rounded-2xl border border-slate-200 animate-pulse" />
      </div>
    )
  }

  if (!order) {
    return (
      <div className="p-12 text-center bg-white rounded-2xl border border-slate-200 shadow-2xs">
        <AlertCircle className="w-8 h-8 text-rose-500 mx-auto mb-2" strokeWidth={1.5} />
        <h2 className="text-base font-bold text-slate-800">Không tìm thấy đơn hàng</h2>
        <p className="text-xs text-slate-500 mt-1">Đơn hàng không tồn tại hoặc bạn không có quyền xem.</p>
        <button
          type="button"
          onClick={() => router.push('/admin/orders')}
          className="mt-4 inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-slate-800 text-white text-xs font-semibold hover:bg-slate-700 transition-colors cursor-pointer"
        >
          <ArrowLeft className="w-4 h-4" />
          Quay lại danh sách đơn hàng
        </button>
      </div>
    )
  }

  const canProcess = hasPermission('orders.process')
  const canShip = hasPermission('orders.ship')
  const canCancel = hasPermission('orders.cancel')
  const canSeeCost = hasPermission('catalog.cost.read')

  // Normalization
  const recipientName = order.address?.recipientName || order.shippingRecipientName || 'Khách hàng'
  const recipientPhone = order.address?.phone || order.shippingPhone || '—'
  const street = order.address?.street || order.shippingStreet || ''
  const wardName = order.address?.wardName || order.shippingWardName || ''
  const provinceName = order.address?.provinceName || order.shippingProvinceName || ''
  const fullAddress = [street, wardName, provinceName].filter(Boolean).join(', ') || 'Chưa cập nhật địa chỉ'
  const customerNote = order.customerNote || order.address?.note || order.shippingNote || null
  const orderDate = order.createdAt || order.placedAt || ''
  const timeline = order.timeline || order.statusHistory || []
  const items = order.items ?? []
  const subtotalAmount = Number(order.subtotal) || 0
  const couponDiscountAmount = Number(order.couponDiscount) || 0
  const shippingFeeAmount = Number(order.shippingFee) || 0
  const totalAmount = Number(order.total) || 0

  return (
    <div className="space-y-6">
      {/* Back button & Title */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => router.push('/admin/orders')}
            className="p-2 rounded-xl border border-slate-200 bg-white text-slate-600 hover:bg-slate-50 transition-colors cursor-pointer shadow-2xs"
            title="Quay lại danh sách"
          >
            <ArrowLeft className="w-4 h-4" strokeWidth={1.5} />
          </button>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl font-bold text-slate-900 font-mono">{order.orderNumber}</h1>
              <StatusChip status={order.status} type="order" />
              <StatusChip status={order.paymentStatus} type="payment" />
            </div>
            <p className="text-xs text-slate-400 mt-0.5">Đặt lúc {formatDateTime(orderDate)}</p>
          </div>
        </div>

        {/* Transition actions bar (Strictly only valid transitions per spec) */}
        <div className="flex items-center flex-wrap gap-2">
          {/* Print Invoice Button */}
          <button
            type="button"
            onClick={() => setIsInvoiceOpen(true)}
            className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-slate-800 text-white text-xs font-semibold hover:bg-slate-700 transition-all shadow-2xs cursor-pointer"
            title="In phiếu giao hàng và hóa đơn bán lẻ"
          >
            <Printer className="w-4 h-4" />
            <span>In phiếu giao hàng</span>
          </button>

          {order.status === 'Placed' && canProcess && (
            <button
              type="button"
              disabled={isSubmitting}
              onClick={() => handleTransition('Confirmed')}
              className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-indigo-600 text-white text-xs font-semibold hover:bg-indigo-700 transition-colors shadow-2xs cursor-pointer"
            >
              <CheckCircle2 className="w-4 h-4" strokeWidth={1.5} />
              Xác nhận đơn
            </button>
          )}

          {order.status === 'Confirmed' && canProcess && (
            <button
              type="button"
              disabled={isSubmitting}
              onClick={() => handleTransition('Preparing')}
              className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-purple-600 text-white text-xs font-semibold hover:bg-purple-700 transition-colors shadow-2xs cursor-pointer"
            >
              <Package className="w-4 h-4" strokeWidth={1.5} />
              Chuẩn bị hàng
            </button>
          )}

          {order.status === 'Preparing' && canProcess && (
            <button
              type="button"
              disabled={isSubmitting}
              onClick={() => handleTransition('Packed')}
              className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-cyan-700 text-white text-xs font-semibold hover:bg-cyan-800 transition-colors shadow-2xs cursor-pointer"
            >
              <Package className="w-4 h-4" strokeWidth={1.5} />
              Đã đóng gói
            </button>
          )}

          {order.status === 'Packed' && canShip && (
            <button
              type="button"
              disabled={isSubmitting}
              onClick={() => handleTransition('Shipping')}
              className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-sky-600 text-white text-xs font-semibold hover:bg-sky-700 transition-colors shadow-2xs cursor-pointer"
            >
              <Truck className="w-4 h-4" strokeWidth={1.5} />
              Giao cho vận chuyển
            </button>
          )}

          {order.status === 'Shipping' && canShip && (
            <>
              <button
                type="button"
                disabled={isSubmitting}
                onClick={() => handleTransition('Delivered')}
                className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-emerald-600 text-white text-xs font-semibold hover:bg-emerald-700 transition-colors shadow-2xs cursor-pointer"
              >
                <CheckCircle2 className="w-4 h-4" strokeWidth={1.5} />
                Giao thành công {order.paymentMethod?.toUpperCase() === 'COD' && `(Thu ${formatVnd(totalAmount)})`}
              </button>

              <button
                type="button"
                disabled={isSubmitting}
                onClick={() => {
                  const reason = prompt('Nhập lý do giao hàng thất bại (ví dụ: Không nghe máy, khách hẹn lại):')
                  if (reason) void handleTransition('DeliveryFailed', reason)
                }}
                className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-amber-600 text-white text-xs font-semibold hover:bg-amber-700 transition-colors shadow-2xs cursor-pointer"
              >
                <AlertTriangle className="w-4 h-4" strokeWidth={1.5} />
                Giao thất bại
              </button>
            </>
          )}

          {order.status === 'DeliveryFailed' && canShip && (
            <button
              type="button"
              disabled={isSubmitting}
              onClick={() => handleTransition('Shipping')}
              className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-sky-600 text-white text-xs font-semibold hover:bg-sky-700 transition-colors shadow-2xs cursor-pointer"
            >
              <RotateCcw className="w-4 h-4" strokeWidth={1.5} />
              Giao lại lần 2
            </button>
          )}

          {!['Delivered', 'Cancelled'].includes(order.status) && canCancel && (
            <button
              type="button"
              disabled={isSubmitting}
              onClick={() => {
                const reason = prompt('Nhập lý do hủy đơn hàng:')
                if (reason) void handleTransition('Cancelled', reason)
              }}
              className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl border border-rose-200 text-rose-600 text-xs font-semibold hover:bg-rose-50 transition-colors cursor-pointer"
            >
              <XCircle className="w-4 h-4" strokeWidth={1.5} />
              Hủy đơn
            </button>
          )}
        </div>
      </div>

      {/* Visual Order Lifecycle Stepper */}
      <div className="bg-white rounded-2xl border border-slate-200 p-4 sm:p-5 shadow-2xs">
        <div className="flex items-center justify-between mb-3">
          <span className="text-xs font-bold uppercase tracking-wider text-slate-700">
            Tiến độ xử lý đơn hàng
          </span>
          <span className="text-xs font-medium text-slate-500">
            Trạng thái hiện tại:{' '}
            <strong className="text-leaf uppercase">{order.status}</strong>
          </span>
        </div>

        {order.status === 'Cancelled' ? (
          <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl flex items-center gap-2 text-rose-800 text-xs">
            <XCircle className="w-5 h-5 text-rose-600 shrink-0" />
            <div>
              <strong className="font-semibold">Đơn hàng này đã bị hủy.</strong>
              {order.cancellationReason && (
                <span className="ml-1 italic">&ldquo;{order.cancellationReason}&rdquo;</span>
              )}
            </div>
          </div>
        ) : order.status === 'DeliveryFailed' ? (
          <div className="p-3 bg-amber-50 border border-amber-200 rounded-xl flex items-center gap-2 text-amber-800 text-xs">
            <AlertTriangle className="w-5 h-5 text-amber-600 shrink-0" />
            <div>
              <strong className="font-semibold">Giao hàng lần trước không thành công.</strong> Bạn có thể bấm nút &ldquo;Giao lại lần 2&rdquo; để tiếp tục xử lý vận chuyển.
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-6 gap-2 pt-1">
            {ORDER_STEPS.map((step, idx) => {
              const isDone = currentStepIndex > idx
              const isCurrent = currentStepIndex === idx
              return (
                <div
                  key={step.key}
                  className={`flex flex-col items-center text-center p-2.5 rounded-xl border transition-all ${
                    isCurrent
                      ? 'bg-leaf/10 border-leaf text-leaf font-bold shadow-2xs'
                      : isDone
                      ? 'bg-slate-50 border-slate-200 text-slate-700 font-medium'
                      : 'bg-white border-dashed border-slate-200 text-slate-300'
                  }`}
                >
                  <div
                    className={`w-6 h-6 rounded-full flex items-center justify-center text-xs mb-1 font-bold ${
                      isCurrent
                        ? 'bg-leaf text-white'
                        : isDone
                        ? 'bg-slate-200 text-slate-700'
                        : 'bg-slate-100 text-slate-300'
                    }`}
                  >
                    {isDone ? <Check className="w-3 h-3 stroke-[2.5]" /> : idx + 1}
                  </div>
                  <span className="text-[11px] leading-tight">{step.label}</span>
                </div>
              )
            })}
          </div>
        )}
      </div>

      {errorMsg && (
        <div className="p-3 bg-rose-50 border border-rose-200 text-rose-700 rounded-xl text-xs flex items-center gap-2">
          <AlertCircle className="w-4 h-4 shrink-0" strokeWidth={1.5} />
          <span>{errorMsg}</span>
        </div>
      )}

      {successMsg && (
        <div className="p-3 bg-emerald-50 border border-emerald-200 text-emerald-700 rounded-xl text-xs flex items-center gap-2">
          <CheckCircle2 className="w-4 h-4 shrink-0" strokeWidth={1.5} />
          <span>{successMsg}</span>
        </div>
      )}

      {/* Grid: Left Items + Timeline | Right Customer + Notes */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left 2 Cols: Products & Breakdown */}
        <div className="lg:col-span-2 space-y-6">
          <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-2xs">
            <div className="p-4 border-b border-slate-100 flex items-center justify-between">
              <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">
                Chi tiết sản phẩm ({items.length})
              </h2>
            </div>
            <div className="divide-y divide-slate-100">
              {items.map((item, idx) => {
                const itemName = item.name || item.productName || 'Sản phẩm'
                const itemSku = item.sku || item.productSku || '—'
                const itemUnit = item.unit || item.productUnit || 'Món'
                const itemPrice = Number(item.unitPrice) || 0
                const itemQty = Number(item.quantity) || 1
                const itemLineTotal = Number(item.lineTotal ?? item.subtotal ?? item.lineSubtotal ?? (itemPrice * itemQty)) || 0
                const cost = item.costPrice ?? item.unitCostSnapshot

                return (
                  <div key={item.id || idx} className="p-4 flex items-center justify-between text-sm">
                    <div className="flex-1">
                      <div className="font-semibold text-slate-900">{itemName}</div>
                      <div className="text-xs text-slate-400 font-mono mt-0.5">
                        SKU: {itemSku} &bull; ĐVT: {itemUnit}
                      </div>
                      {canSeeCost && cost !== undefined && (
                        <div className="text-[11px] text-amber-700 font-mono mt-0.5">
                          Giá vốn: {formatVnd(cost)}
                        </div>
                      )}
                    </div>
                    <div className="text-right ml-4">
                      <div className="font-semibold text-slate-900">{formatVnd(itemLineTotal)}</div>
                      <div className="text-xs text-slate-400 font-mono">
                        {formatVnd(itemPrice)} &times; {itemQty}
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>

            {/* Receipt Summary */}
            <div className="p-4 bg-slate-50/70 border-t border-slate-100 space-y-1.5 text-xs text-slate-600">
              <div className="flex justify-between">
                <span>Tạm tính hàng hóa:</span>
                <span className="font-medium text-slate-800 font-mono">{formatVnd(subtotalAmount)}</span>
              </div>
              {couponDiscountAmount > 0 && (
                <div className="flex justify-between text-leaf font-medium">
                  <span>Giảm giá khuyến mãi:</span>
                  <span className="font-mono">-{formatVnd(couponDiscountAmount)}</span>
                </div>
              )}
              <div className="flex justify-between">
                <span>Phí vận chuyển:</span>
                <span className="font-medium text-slate-800 font-mono">
                  {shippingFeeAmount === 0 ? 'Miễn phí' : formatVnd(shippingFeeAmount)}
                </span>
              </div>
              <div className="flex justify-between text-sm font-bold text-slate-900 pt-2 border-t border-slate-200">
                <span>Tổng thanh toán:</span>
                <span className="text-base text-leaf font-mono">{formatVnd(totalAmount)}</span>
              </div>
            </div>
          </div>

          {/* Timeline */}
          <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs">
            <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700 mb-4">
              Lịch sử trạng thái đơn hàng
            </h2>
            {timeline.length === 0 ? (
              <p className="text-xs text-slate-400 italic">Chưa có lịch sử trạng thái.</p>
            ) : (
              <div className="space-y-4">
                {timeline.map((h, idx) => (
                  <div key={h.id || `${h.toStatus}-${h.createdAt}-${idx}`} className="flex items-start gap-3 text-xs">
                    <div className="w-6 h-6 rounded-full bg-slate-100 text-slate-500 flex items-center justify-center shrink-0 mt-0.5">
                      <Clock className="w-3.5 h-3.5" strokeWidth={1.5} />
                    </div>
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <span className="font-semibold text-slate-800">
                          {h.fromStatus ? `${h.fromStatus} → ` : ''}
                          {h.toStatus}
                        </span>
                        {h.actorRole && (
                          <span className="text-[10px] bg-slate-100 text-slate-500 px-1.5 py-0.5 rounded-sm uppercase font-mono">
                            {h.actorRole}
                          </span>
                        )}
                      </div>
                      {h.reason && <p className="text-slate-500 mt-0.5 italic">&ldquo;{h.reason}&rdquo;</p>}
                      <span className="text-[10px] text-slate-400 mt-0.5 block">{formatDateTime(h.createdAt)}</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Right 1 Col: Customer & Delivery Info & Notes */}
        <div className="space-y-6">
          <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs space-y-4">
            <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Thông tin giao hàng</h2>
            <div className="space-y-3 text-xs text-slate-600">
              <div className="flex items-start gap-2.5">
                <User className="w-4 h-4 text-slate-400 shrink-0 mt-0.5" strokeWidth={1.5} />
                <div>
                  <div className="font-semibold text-slate-800">{recipientName}</div>
                  <div className="text-slate-500 font-mono">{recipientPhone}</div>
                </div>
              </div>
              <div className="flex items-start gap-2.5">
                <MapPin className="w-4 h-4 text-slate-400 shrink-0 mt-0.5" strokeWidth={1.5} />
                <div>
                  <div className="text-slate-800 font-medium">{fullAddress}</div>
                </div>
              </div>
              {customerNote && (
                <div className="p-2.5 bg-amber-50/60 rounded-xl border border-amber-100 text-amber-800 text-xs">
                  <span className="font-semibold">Lời dặn của khách:</span> {customerNote}
                </div>
              )}
            </div>
          </div>

          {/* Internal Notes */}
          <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs space-y-3">
            <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700">Ghi chú nội bộ</h2>
            <textarea
              rows={3}
              value={internalNote}
              onChange={e => setInternalNote(e.target.value)}
              placeholder="Ghi chú riêng cho nhân viên cửa hàng..."
              className="w-full text-xs p-3 rounded-xl border border-slate-200 focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
            />
            <button
              type="button"
              disabled={isSubmitting}
              onClick={handleSaveNote}
              className="w-full inline-flex items-center justify-center gap-1.5 px-3 py-2 rounded-xl bg-slate-800 text-white text-xs font-medium hover:bg-slate-900 transition-colors cursor-pointer"
            >
              <Send className="w-3.5 h-3.5" strokeWidth={1.5} />
              Lưu ghi chú
            </button>
          </div>
        </div>
      </div>

      {/* Delivery Invoice & Receipt Print Modal */}
      <OrderInvoiceModal
        order={order as unknown as InvoiceOrderData}
        isOpen={isInvoiceOpen}
        onClose={() => setIsInvoiceOpen(false)}
      />
    </div>
  )
}
