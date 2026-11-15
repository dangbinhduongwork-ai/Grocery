import React, { useEffect, useState } from 'react'
import { api } from '@/shared/api/client'
import type { CustomerOrderDetail } from '@/shared/api/types'
import { Button } from '@/shared/ui/Button'
import { Skeleton } from '@/shared/ui/Skeleton'
import { formatVnd } from '@/shared/utils/formatVnd'
import { useRouter } from '@/app/router'
import {
  CheckCircle2,
  MapPin,
  ArrowRight,
  ShoppingBag,
  Banknote,
} from 'lucide-react'

interface OrderConfirmationPageProps {
  orderId: string
}

export const OrderConfirmationPage: React.FC<OrderConfirmationPageProps> = ({ orderId }) => {
  const { navigate } = useRouter()
  const [order, setOrder] = useState<CustomerOrderDetail | null>(null)
  const [isLoading, setIsLoading] = useState<boolean>(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    async function fetchOrder() {
      setIsLoading(true)
      try {
        const { data, error: err } = await api.GET('/api/v1/me/orders/{id}', {
          params: { path: { id: orderId } },
        })

        if (err || !data) {
          setError('Không thể tải thông tin đơn hàng vừa đặt')
        } else {
          setOrder(data)
        }
      } catch {
        setError('Lỗi kết nối khi tải đơn hàng')
      } finally {
        setIsLoading(false)
      }
    }
    void fetchOrder()
  }, [orderId])

  if (isLoading) {
    return (
      <div className="max-w-2xl mx-auto py-12 space-y-6 animate-pulse">
        <Skeleton className="h-20 w-full rounded-2xl" />
        <Skeleton className="h-64 w-full rounded-2xl" />
      </div>
    )
  }

  if (error || !order) {
    return (
      <div className="text-center py-16 bg-paper-2 rounded-2xl border border-rule max-w-lg mx-auto px-4">
        <CheckCircle2 className="w-12 h-12 text-leaf mx-auto mb-3 stroke-[1.5]" />
        <h2 className="font-display font-semibold text-lg text-ink mb-1">
          Đơn hàng của bạn đã được tiếp nhận
        </h2>
        <p className="text-xs text-ink-2 mb-6">
          Hệ thống đang xử lý và chuẩn bị đơn. Bạn có thể kiểm tra danh sách đơn hàng để theo dõi tiến độ.
        </p>
        <Button variant="primary" onClick={() => navigate('/orders')}>
          Xem danh sách đơn hàng
        </Button>
      </div>
    )
  }

  return (
    <div className="max-w-2xl mx-auto space-y-8 py-4">
      {/* Success Hero Banner */}
      <div className="text-center bg-paper-2 p-8 rounded-3xl border border-rule space-y-3 shadow-xs">
        <div className="w-16 h-16 rounded-full bg-leaf/10 border border-leaf/30 text-leaf flex items-center justify-center mx-auto shadow-xs">
          <CheckCircle2 className="w-10 h-10 stroke-[2]" />
        </div>
        <h1 className="font-display font-bold text-2xl sm:text-3xl text-ink">
          Đặt hàng thành công!
        </h1>
        <p className="text-xs sm:text-sm text-ink-2 max-w-md mx-auto leading-relaxed">
          Cảm ơn bạn đã mua sắm tại GroceryHub. Đơn hàng của bạn đã được ghi nhận và nhân viên sẽ liên hệ đóng gói sớm nhất.
        </p>
        <div className="inline-block bg-paper px-4 py-2 rounded-full border border-rule text-xs">
          Mã đơn hàng: <strong className="font-mono text-leaf">{order.orderNumber}</strong>
        </div>
      </div>

      {/* Delivery & Payment Details Card */}
      <div className="bg-paper-2 p-6 rounded-2xl border border-rule space-y-5">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pb-4 border-b border-rule text-xs">
          <div className="space-y-1">
            <span className="font-semibold text-ink flex items-center gap-1.5">
              <MapPin className="w-4 h-4 text-leaf stroke-[1.5]" /> Địa chỉ nhận hàng
            </span>
            <p className="font-medium text-ink">{order.address?.recipientName} - {order.address?.phone}</p>
            <p className="text-ink-2 leading-relaxed">
              {order.address?.street}, {order.address?.wardName}, {order.address?.provinceName}
            </p>
          </div>

          <div className="space-y-1">
            <span className="font-semibold text-ink flex items-center gap-1.5">
              <Banknote className="w-4 h-4 text-leaf stroke-[1.5]" /> Phương thức thanh toán
            </span>
            <p className="font-medium text-ink">Thanh toán tiền mặt khi nhận hàng (COD)</p>
            <span className="inline-block text-[11px] bg-amber-50 text-amber-700 border border-amber-200 px-2 py-0.5 rounded-md font-medium">
              Chờ thanh toán khi nhận hàng
            </span>
          </div>
        </div>

        {/* Purchased Items List */}
        <div className="space-y-3">
          <h3 className="font-display font-semibold text-sm text-ink">Danh sách sản phẩm</h3>
          <div className="divide-y divide-rule border border-rule rounded-xl bg-paper overflow-hidden">
            {order.items?.map((item) => (
              <div key={item.id} className="p-3.5 flex items-center justify-between text-xs gap-3">
                <div className="flex items-center gap-3 truncate">
                  <div className="w-10 h-10 rounded-lg bg-paper-2 border border-rule overflow-hidden p-1 shrink-0 flex items-center justify-center">
                    {item.imageUrl ? (
                      <img src={item.imageUrl} alt="" className="w-full h-full object-contain" />
                    ) : (
                      <ShoppingBag className="w-4 h-4 text-ink-2/30 stroke-[1]" />
                    )}
                  </div>
                  <div className="truncate">
                    <span className="font-medium text-ink block truncate">{item.name}</span>
                    <span className="text-[11px] text-ink-2 tabular-nums">
                      {formatVnd(Number(item.unitPrice))} x {item.quantity} {item.unit}
                    </span>
                  </div>
                </div>

                <span className="font-bold text-ink tabular-nums shrink-0">
                  {formatVnd(Number(item.lineTotal))}
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* Pricing Summary */}
        <div className="border-t border-rule pt-4 space-y-2 text-xs">
          <div className="flex justify-between text-ink-2">
            <span>Tạm tính tiền hàng:</span>
            <span className="tabular-nums font-medium text-ink">{formatVnd(Number(order.subtotal))}</span>
          </div>
          {Number(order.couponDiscount) > 0 && (
            <div className="flex justify-between text-tomato">
              <span>Giảm giá khuyến mại:</span>
              <span className="tabular-nums font-medium">-{formatVnd(Number(order.couponDiscount))}</span>
            </div>
          )}
          <div className="flex justify-between text-ink-2">
            <span>Phí giao hàng:</span>
            <span className="tabular-nums font-medium text-ink">{formatVnd(Number(order.shippingFee))}</span>
          </div>
          <div className="flex justify-between text-sm font-bold text-ink border-t border-rule pt-2">
            <span>Tổng thanh toán:</span>
            <span className="font-display text-base text-leaf tabular-nums">
              {formatVnd(Number(order.total))}
            </span>
          </div>
        </div>
      </div>

      {/* Action Buttons */}
      <div className="flex flex-col sm:flex-row gap-3 justify-center pt-2">
        <Button
          variant="primary"
          size="lg"
          onClick={() => navigate(`/orders/${order.id}`)}
          className="font-semibold shadow-xs"
        >
          <span>Theo dõi đơn hàng</span>
          <ArrowRight className="w-4 h-4 ml-1 stroke-[1.5]" />
        </Button>
        <Button
          variant="outline"
          size="lg"
          onClick={() => navigate('/')}
          className="font-medium"
        >
          Tiếp tục mua sắm
        </Button>
      </div>
    </div>
  )
}
