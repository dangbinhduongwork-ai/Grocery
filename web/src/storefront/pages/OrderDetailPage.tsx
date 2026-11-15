import React, { useEffect, useState } from 'react'
import { api } from '@/shared/api/client'
import type { CustomerOrderDetail } from '@/shared/api/types'
import { useCart } from '@/shared/cart/CartContext'
import { Button } from '@/shared/ui/Button'
import { Badge } from '@/shared/ui/Badge'
import { Skeleton } from '@/shared/ui/Skeleton'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/ui/Dialog'
import { formatVnd } from '@/shared/utils/formatVnd'
import { formatDateTime } from '@/shared/utils/dateTime'
import { useRouter } from '@/app/router'
import {
  MapPin,
  Banknote,
  RotateCcw,
  ShoppingBag,
  Star,
  ChevronLeft,
  AlertCircle,
} from 'lucide-react'

interface OrderDetailPageProps {
  orderId: string
}

export const OrderDetailPage: React.FC<OrderDetailPageProps> = ({ orderId }) => {
  const { navigate } = useRouter()
  const { refreshCart } = useCart()

  const [order, setOrder] = useState<CustomerOrderDetail | null>(null)
  const [isLoading, setIsLoading] = useState<boolean>(true)
  const [error, setError] = useState<string | null>(null)

  // Cancel Dialog
  const [cancelDialogOpen, setCancelDialogOpen] = useState<boolean>(false)
  const [cancelReason, setCancelReason] = useState<string>('Tôi muốn thay đổi địa chỉ hoặc đơn hàng')
  const [isCancelling, setIsCancelling] = useState<boolean>(false)
  const [cancelError, setCancelError] = useState<string | null>(null)

  // Reorder
  const [isReordering, setIsReordering] = useState<boolean>(false)

  const fetchOrder = async () => {
    setIsLoading(true)
    setError(null)
    try {
      const { data, error: err } = await api.GET('/api/v1/me/orders/{id}', {
        params: { path: { id: orderId } },
      })
      if (err || !data) {
        setError('Không tìm thấy chi tiết đơn hàng')
      } else {
        setOrder(data)
      }
    } catch {
      setError('Lỗi kết nối khi tải chi tiết đơn hàng')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void fetchOrder()
  }, [orderId])

  const handleCancelOrder = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsCancelling(true)
    setCancelError(null)
    try {
      const { error: err } = await api.POST('/api/v1/me/orders/{id}/cancel', {
        params: { path: { id: orderId } },
        body: { reason: cancelReason },
      })

      if (err) {
        const msg = (err as any)?.detail || (err as any)?.title || 'Không thể hủy đơn hàng'
        setCancelError(msg)
      } else {
        setCancelDialogOpen(false)
        await fetchOrder()
      }
    } catch {
      setCancelError('Lỗi kết nối khi gửi yêu cầu hủy đơn')
    } finally {
      setIsCancelling(false)
    }
  }

  const handleReorder = async () => {
    setIsReordering(true)
    try {
      const { data } = await api.POST('/api/v1/me/orders/{id}/reorder', {
        params: { path: { id: orderId } },
      })
      if (data) {
        await refreshCart()
        navigate('/cart')
      }
    } catch {
      // silent
    } finally {
      setIsReordering(false)
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-6 max-w-4xl mx-auto animate-pulse">
        <Skeleton className="h-6 w-32" />
        <Skeleton className="h-40 w-full rounded-2xl" />
        <Skeleton className="h-64 w-full rounded-2xl" />
      </div>
    )
  }

  if (error || !order) {
    return (
      <div className="text-center py-16 bg-paper-2 rounded-2xl border border-rule max-w-lg mx-auto px-4">
        <AlertCircle className="w-12 h-12 text-tomato mx-auto mb-3 stroke-[1.5]" />
        <h3 className="font-display font-semibold text-lg text-ink mb-1">{error}</h3>
        <Button variant="primary" onClick={() => navigate('/orders')}>
          Quay lại danh sách đơn hàng
        </Button>
      </div>
    )
  }

  const isCancellable = ['Placed', 'Confirmed', 'PendingPayment'].includes(order.status)
  const isDelivered = order.status === 'Delivered'

  return (
    <div className="space-y-8 max-w-4xl mx-auto">
      {/* Top Bar with Back Button */}
      <div className="flex items-center justify-between gap-4 border-b border-rule pb-4">
        <button
          type="button"
          onClick={() => navigate('/orders')}
          className="flex items-center gap-1.5 text-xs font-semibold text-ink hover:text-leaf transition-colors cursor-pointer"
        >
          <ChevronLeft className="w-4 h-4 stroke-[1.5]" />
          <span>Danh sách đơn hàng</span>
        </button>

        <div className="flex items-center gap-2">
          {isCancellable && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setCancelDialogOpen(true)}
              className="text-xs text-tomato border-tomato hover:bg-tomato/10"
            >
              Hủy đơn hàng
            </Button>
          )}

          <Button
            variant="outline"
            size="sm"
            disabled={isReordering}
            onClick={() => void handleReorder()}
            className="text-xs flex items-center gap-1.5 text-leaf border-leaf hover:bg-leaf/10 font-semibold"
          >
            <RotateCcw className="w-3.5 h-3.5 stroke-[1.5]" />
            <span>Mua lại</span>
          </Button>
        </div>
      </div>

      {/* Order Status & Timeline Header */}
      <div className="bg-paper-2 p-6 rounded-2xl border border-rule space-y-6">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-rule pb-4">
          <div>
            <h1 className="font-display font-bold text-xl sm:text-2xl text-ink">
              Đơn hàng #{order.orderNumber}
            </h1>
            <p className="text-xs text-ink-2 mt-0.5">
              Đặt lúc {formatDateTime(order.placedAt || '')}
            </p>
          </div>
          <Badge
            variant={
              order.status === 'Delivered'
                ? 'ok'
                : order.status === 'Shipping'
                ? 'info'
                : order.status === 'Cancelled'
                ? 'destructive'
                : 'warn'
            }
            className="text-xs px-3 py-1"
          >
            {order.status === 'Delivered'
              ? 'Đã giao hàng thành công'
              : order.status === 'Shipping'
              ? 'Đang vận chuyển'
              : order.status === 'Processing'
              ? 'Đang đóng gói'
              : order.status === 'Cancelled'
              ? 'Đã hủy đơn'
              : 'Đã tiếp nhận'}
          </Badge>
        </div>

        {/* Timeline Events */}
        {order.timeline && order.timeline.length > 0 && (
          <div className="space-y-3">
            <h3 className="font-display font-semibold text-xs uppercase tracking-wider text-ink-2">
              Lịch sử trạng thái
            </h3>
            <div className="relative pl-6 space-y-4 before:absolute before:left-2 before:top-2 before:bottom-2 before:w-0.5 before:bg-rule">
              {order.timeline.map((event, idx) => (
                <div key={idx} className="relative text-xs">
                  <div className="absolute -left-6 top-1 w-2.5 h-2.5 rounded-full bg-leaf ring-4 ring-paper" />
                  <div className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-4">
                    <span className="font-semibold text-ink">
                      {event.toStatus}
                    </span>
                    <span className="text-[11px] text-ink-2">
                      {formatDateTime(event.createdAt)}
                    </span>
                  </div>
                  {event.reason && (
                    <p className="text-[11px] text-ink-2 mt-0.5 italic">{event.reason}</p>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Recipient & Payment Details */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="bg-paper-2 p-5 rounded-2xl border border-rule space-y-2 text-xs">
          <span className="font-semibold text-ink flex items-center gap-1.5">
            <MapPin className="w-4 h-4 text-leaf stroke-[1.5]" /> Địa chỉ nhận hàng
          </span>
          <p className="font-medium text-ink">
            {order.address?.recipientName} - {order.address?.phone}
          </p>
          <p className="text-ink-2 leading-relaxed">
            {order.address?.street}, {order.address?.wardName}, {order.address?.provinceName}
          </p>
          {order.customerNote && (
            <p className="text-[11px] text-ink-2/80 italic pt-1">
              Ghi chú: {order.customerNote}
            </p>
          )}
        </div>

        <div className="bg-paper-2 p-5 rounded-2xl border border-rule space-y-2 text-xs">
          <span className="font-semibold text-ink flex items-center gap-1.5">
            <Banknote className="w-4 h-4 text-leaf stroke-[1.5]" /> Thanh toán
          </span>
          <p className="font-medium text-ink">
            {order.paymentMethod === 'COD' ? 'Tiền mặt khi nhận hàng (COD)' : order.paymentMethod}
          </p>
          <p className="text-ink-2">
            Trạng thái:{' '}
            <strong className="text-ink">
              {order.paymentStatus === 'Paid' ? 'Đã thanh toán' : 'Chờ thanh toán khi nhận hàng'}
            </strong>
          </p>
        </div>
      </div>

      {/* Order Items Table */}
      <div className="bg-paper-2 p-6 rounded-2xl border border-rule space-y-4">
        <h3 className="font-display font-semibold text-base text-ink">
          Mặt hàng trong đơn ({order.items?.length ?? 0})
        </h3>

        <div className="divide-y divide-rule border border-rule rounded-xl bg-paper overflow-hidden">
          {order.items?.map((item) => (
            <div
              key={item.id}
              className="p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 text-xs"
            >
              <div className="flex items-center gap-3">
                <div className="w-12 h-12 rounded-lg bg-paper-2 border border-rule overflow-hidden p-1 shrink-0 flex items-center justify-center">
                  {item.imageUrl ? (
                    <img src={item.imageUrl} alt="" className="w-full h-full object-contain" />
                  ) : (
                    <ShoppingBag className="w-5 h-5 text-ink-2/30 stroke-[1]" />
                  )}
                </div>
                <div>
                  <span className="font-semibold text-ink block">{item.name}</span>
                  <span className="text-[11px] text-ink-2">
                    SKU: {item.sku} | Đơn giá: {formatVnd(Number(item.unitPrice))} x{' '}
                    {item.quantity} {item.unit}
                  </span>
                </div>
              </div>

              <div className="flex items-center justify-between sm:justify-end gap-4">
                <span className="font-bold text-ink tabular-nums">
                  {formatVnd(Number(item.lineTotal))}
                </span>

                {isDelivered && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => navigate(`/p/${item.productId}`)}
                    className="text-xs h-8 flex items-center gap-1 border-leaf text-leaf"
                  >
                    <Star className="w-3 h-3 stroke-[1.5]" />
                    <span>Đánh giá</span>
                  </Button>
                )}
              </div>
            </div>
          ))}
        </div>

        {/* Receipt Totals */}
        <div className="border-t border-rule pt-4 space-y-2 text-xs max-w-sm ml-auto">
          <div className="flex justify-between text-ink-2">
            <span>Tạm tính tiền hàng:</span>
            <span className="tabular-nums font-medium text-ink">
              {formatVnd(Number(order.subtotal))}
            </span>
          </div>
          {Number(order.couponDiscount) > 0 && (
            <div className="flex justify-between text-tomato">
              <span>Giảm giá ({order.couponCode}):</span>
              <span className="tabular-nums font-medium">
                -{formatVnd(Number(order.couponDiscount))}
              </span>
            </div>
          )}
          <div className="flex justify-between text-ink-2">
            <span>Phí vận chuyển:</span>
            <span className="tabular-nums font-medium text-ink">
              {formatVnd(Number(order.shippingFee))}
            </span>
          </div>
          <div className="flex justify-between text-sm font-bold text-ink border-t border-rule pt-2">
            <span>Tổng thanh toán:</span>
            <span className="font-display text-base text-leaf tabular-nums">
              {formatVnd(Number(order.total))}
            </span>
          </div>
        </div>
      </div>

      {/* Cancel Order Dialog */}
      <Dialog open={cancelDialogOpen} onOpenChange={setCancelDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Xác nhận hủy đơn hàng #{order.orderNumber}</DialogTitle>
            <DialogDescription>
              Bạn có chắc chắn muốn hủy đơn hàng này? Toàn bộ các sản phẩm đã giữ chỗ trong kho sẽ được hoàn lại.
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleCancelOrder} className="space-y-4 py-2">
            {cancelError && (
              <p className="text-xs text-tomato bg-[#FDECE8] p-2.5 rounded-lg border border-[#FADCD5]">
                {cancelError}
              </p>
            )}

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-ink block">
                Lý do hủy đơn hàng <span className="text-tomato">*</span>
              </label>
              <select
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                className="w-full rounded-md border border-rule bg-paper p-2.5 text-xs text-ink focus:outline-none focus:ring-2 focus:ring-leaf"
              >
                <option value="Tôi muốn thay đổi địa chỉ hoặc đơn hàng">
                  Tôi muốn thay đổi địa chỉ hoặc đơn hàng
                </option>
                <option value="Tìm thấy giá rẻ hơn ở nơi khác">
                  Tìm thấy giá rẻ hơn ở nơi khác
                </option>
                <option value="Thời gian giao hàng không phù hợp">
                  Thời gian giao hàng không phù hợp
                </option>
                <option value="Đổi ý, không muốn mua nữa">
                  Đổi ý, không muốn mua nữa
                </option>
              </select>
            </div>

            <DialogFooter className="pt-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setCancelDialogOpen(false)}
              >
                Giữ đơn hàng
              </Button>
              <Button
                type="submit"
                variant="primary"
                size="sm"
                disabled={isCancelling}
                className="bg-tomato hover:bg-tomato/90 text-white border-none"
              >
                {isCancelling ? 'Đang hủy...' : 'Xác nhận hủy'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  )
}
