import React, { useEffect, useState, useCallback } from 'react'
import {
  ArrowLeft,
  CheckCircle2,
  Package,
  Truck,
  XCircle,
  Clock,
  User,
  MapPin,
  AlertCircle,
  Send,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { formatVnd } from '@/shared/utils/formatVnd'
import { formatDateTime } from '@/shared/utils/dateTime'
import { useRouter } from '@/app/router'
import { useAuth } from '@/shared/auth'
import { StatusChip } from '../components/StatusChip'

interface OrderDetail {
  id: string
  orderNumber: string
  placedAt: string
  status: string
  subtotal: number
  couponDiscount: number
  shippingFee: number
  total: number
  paymentMethod: string
  paymentStatus: string
  shippingRecipientName: string
  shippingPhone: string
  shippingProvinceName: string
  shippingWardName: string
  shippingStreet: string
  shippingNote?: string
  internalNote?: string
  cancellationReason?: string
  items: Array<{
    id: string
    productId: string
    productSku: string
    productName: string
    productUnit: string
    unitPrice: number
    quantity: number
    subtotal: number
    unitCostSnapshot?: number
  }>
  statusHistory: Array<{
    id: string
    fromStatus?: string
    toStatus: string
    reason?: string
    actorRole?: string
    createdAt: string
  }>
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

  const fetchDetail = useCallback(async () => {
    setIsLoading(true)
    try {
      const res = await api.GET('/api/v1/admin/orders/{id}', {
        params: { path: { id } },
      })
      if (res.data) {
        const d = res.data as unknown as OrderDetail
        setOrder(d)
        setInternalNote(d.internalNote ?? '')
      }
    } catch {
      setErrorMsg('Không thể tải thông tin đơn hàng.')
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
      const body = {
        to: toStatus,
        reason: reason ?? null,
        codAmount: toStatus === 'Delivered' && order.paymentMethod === 'COD' ? order.total : null,
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
    try {
      await api.PUT('/api/v1/admin/orders/{id}/note', {
        params: { path: { id: order.id } },
        body: { internalNote },
      })
      setSuccessMsg('Đã lưu ghi chú nội bộ.')
    } catch {
      setErrorMsg('Không thể lưu ghi chú.')
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
      <div className="p-12 text-center bg-white rounded-2xl border border-slate-200">
        <AlertCircle className="w-8 h-8 text-rose-500 mx-auto mb-2" strokeWidth={1.5} />
        <h2 className="text-base font-bold text-slate-800">Không tìm thấy đơn hàng</h2>
        <button
          type="button"
          onClick={() => router.push('/admin/orders')}
          className="mt-4 text-xs text-leaf hover:underline font-medium"
        >
          &larr; Quay lại danh sách đơn hàng
        </button>
      </div>
    )
  }

  const canProcess = hasPermission('orders.process')
  const canShip = hasPermission('orders.ship')
  const canCancel = hasPermission('orders.cancel')
  const canSeeCost = hasPermission('catalog.cost.read')

  return (
    <div className="space-y-6">
      {/* Back button & Title */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => router.push('/admin/orders')}
            className="p-2 rounded-xl border border-slate-200 bg-white text-slate-600 hover:bg-slate-50 transition-colors"
          >
            <ArrowLeft className="w-4 h-4" strokeWidth={1.5} />
          </button>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl font-bold text-slate-900 font-mono">{order.orderNumber}</h1>
              <StatusChip status={order.status} type="order" />
              <StatusChip status={order.paymentStatus} type="payment" />
            </div>
            <p className="text-xs text-slate-400 mt-0.5">Đặt lúc {formatDateTime(order.placedAt)}</p>
          </div>
        </div>

        {/* Transition actions bar (Strictly only valid transitions per spec) */}
        <div className="flex items-center flex-wrap gap-2">
          {order.status === 'Placed' && canProcess && (
            <button
              type="button"
              disabled={isSubmitting}
              onClick={() => handleTransition('Confirmed')}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-indigo-600 text-white text-xs font-semibold hover:bg-indigo-700 transition-colors shadow-2xs"
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
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-purple-600 text-white text-xs font-semibold hover:bg-purple-700 transition-colors shadow-2xs"
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
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-cyan-700 text-white text-xs font-semibold hover:bg-cyan-800 transition-colors shadow-2xs"
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
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-sky-600 text-white text-xs font-semibold hover:bg-sky-700 transition-colors shadow-2xs"
            >
              <Truck className="w-4 h-4" strokeWidth={1.5} />
              Giao cho vận chuyển
            </button>
          )}

          {order.status === 'Shipping' && canShip && (
            <button
              type="button"
              disabled={isSubmitting}
              onClick={() => handleTransition('Delivered')}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-emerald-600 text-white text-xs font-semibold hover:bg-emerald-700 transition-colors shadow-2xs"
            >
              <CheckCircle2 className="w-4 h-4" strokeWidth={1.5} />
              Giao thành công {order.paymentMethod === 'COD' && `(Thu ${formatVnd(order.total)})`}
            </button>
          )}

          {['PendingPayment', 'Placed', 'Confirmed'].includes(order.status) && canCancel && (
            <button
              type="button"
              disabled={isSubmitting}
              onClick={() => {
                const reason = prompt('Nhập lý do hủy đơn:')
                if (reason) void handleTransition('Cancelled', reason)
              }}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl border border-rose-200 text-rose-600 text-xs font-semibold hover:bg-rose-50 transition-colors"
            >
              <XCircle className="w-4 h-4" strokeWidth={1.5} />
              Hủy đơn
            </button>
          )}
        </div>
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
                Chi tiết sản phẩm ({order.items.length})
              </h2>
            </div>
            <div className="divide-y divide-slate-100">
              {order.items.map(item => (
                <div key={item.id} className="p-4 flex items-center justify-between text-sm">
                  <div className="flex-1">
                    <div className="font-semibold text-slate-900">{item.productName}</div>
                    <div className="text-xs text-slate-400 font-mono mt-0.5">
                      SKU: {item.productSku} &bull; Đơn vị: {item.productUnit}
                    </div>
                    {canSeeCost && item.unitCostSnapshot !== undefined && (
                      <div className="text-[11px] text-amber-700 font-mono mt-0.5">
                        Giá vốn: {formatVnd(item.unitCostSnapshot)}
                      </div>
                    )}
                  </div>
                  <div className="text-right ml-4">
                    <div className="font-semibold text-slate-900">{formatVnd(item.subtotal)}</div>
                    <div className="text-xs text-slate-400">
                      {formatVnd(item.unitPrice)} &times; {item.quantity}
                    </div>
                  </div>
                </div>
              ))}
            </div>

            {/* Receipt Summary */}
            <div className="p-4 bg-slate-50/70 border-t border-slate-100 space-y-1.5 text-xs text-slate-600">
              <div className="flex justify-between">
                <span>Tạm tính hàng hóa:</span>
                <span className="font-medium text-slate-800">{formatVnd(order.subtotal)}</span>
              </div>
              {order.couponDiscount > 0 && (
                <div className="flex justify-between text-leaf font-medium">
                  <span>Giảm giá khuyến mãi:</span>
                  <span>-{formatVnd(order.couponDiscount)}</span>
                </div>
              )}
              <div className="flex justify-between">
                <span>Phí vận chuyển:</span>
                <span className="font-medium text-slate-800">
                  {order.shippingFee === 0 ? 'Miễn phí' : formatVnd(order.shippingFee)}
                </span>
              </div>
              <div className="flex justify-between text-sm font-bold text-slate-900 pt-2 border-t border-slate-200">
                <span>Tổng thanh toán:</span>
                <span className="text-base text-leaf">{formatVnd(order.total)}</span>
              </div>
            </div>
          </div>

          {/* Timeline */}
          <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs">
            <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700 mb-4">
              Lịch sử trạng thái đơn hàng
            </h2>
            <div className="space-y-4">
              {order.statusHistory.map(h => (
                <div key={h.id} className="flex items-start gap-3 text-xs">
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
                  <div className="font-semibold text-slate-800">{order.shippingRecipientName}</div>
                  <div className="text-slate-500 font-mono">{order.shippingPhone}</div>
                </div>
              </div>
              <div className="flex items-start gap-2.5">
                <MapPin className="w-4 h-4 text-slate-400 shrink-0 mt-0.5" strokeWidth={1.5} />
                <div>
                  <div>{order.shippingStreet}</div>
                  <div className="text-slate-500">
                    {order.shippingWardName}, {order.shippingProvinceName}
                  </div>
                </div>
              </div>
              {order.shippingNote && (
                <div className="p-2.5 bg-amber-50/60 rounded-xl border border-amber-100 text-amber-800 text-xs">
                  <span className="font-semibold">Lời dặn của khách:</span> {order.shippingNote}
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
              className="w-full inline-flex items-center justify-center gap-1.5 px-3 py-2 rounded-xl bg-slate-800 text-white text-xs font-medium hover:bg-slate-900 transition-colors"
            >
              <Send className="w-3.5 h-3.5" strokeWidth={1.5} />
              Lưu ghi chú
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
