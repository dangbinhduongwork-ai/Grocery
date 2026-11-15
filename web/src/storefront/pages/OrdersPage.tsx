import React, { useEffect, useState } from 'react'
import { api } from '@/shared/api/client'
import type { CustomerOrderItem } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { Button } from '@/shared/ui/Button'
import { Badge } from '@/shared/ui/Badge'
import { Skeleton } from '@/shared/ui/Skeleton'
import { formatVnd } from '@/shared/utils/formatVnd'
import { formatDateTime } from '@/shared/utils/dateTime'
import { useRouter } from '@/app/router'
import {
  Package,
  Clock,
  Truck,
  CheckCircle2,
  XCircle,
  ChevronRight,
} from 'lucide-react'

export const OrdersPage: React.FC = () => {
  const { navigate } = useRouter()
  const { isAuthenticated, isLoading: authLoading } = useAuth()

  const [orders, setOrders] = useState<CustomerOrderItem[]>([])
  const [activeTab, setActiveTab] = useState<string>('all')
  const [isLoading, setIsLoading] = useState<boolean>(true)

  useEffect(() => {
    async function fetchOrders() {
      if (!isAuthenticated) return
      setIsLoading(true)
      try {
        const queryParams: Record<string, string> = {
          page: '1',
          pageSize: '20',
        }
        if (activeTab !== 'all') {
          queryParams.status = activeTab
        }

        const { data } = await api.GET('/api/v1/me/orders', {
          params: { query: queryParams as any },
        })

        if (data?.items) {
          setOrders(data.items)
        }
      } catch {
        setOrders([])
      } finally {
        setIsLoading(false)
      }
    }

    void fetchOrders()
  }, [isAuthenticated, activeTab])

  if (authLoading) {
    return (
      <div className="space-y-4 max-w-4xl mx-auto animate-pulse">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-40 w-full rounded-2xl" />
        <Skeleton className="h-40 w-full rounded-2xl" />
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="text-center py-20 bg-paper-2 rounded-2xl border border-rule max-w-lg mx-auto px-4">
        <Package className="w-12 h-12 text-leaf mx-auto mb-3 stroke-[1.5]" />
        <h2 className="font-display font-semibold text-lg text-ink mb-1">
          Vui lòng đăng nhập để xem đơn hàng
        </h2>
        <p className="text-xs text-ink-2 mb-6">
          Theo dõi lịch sử mua sắm và hành trình giao hàng của bạn.
        </p>
        <Button variant="primary" onClick={() => navigate('/login')}>
          Đăng nhập ngay
        </Button>
      </div>
    )
  }

  const renderStatusBadge = (status: string) => {
    switch (status) {
      case 'Delivered':
        return (
          <Badge variant="ok" className="flex items-center gap-1">
            <CheckCircle2 className="w-3 h-3 stroke-[2]" /> Đã giao hàng
          </Badge>
        )
      case 'Shipping':
        return (
          <Badge variant="info" className="flex items-center gap-1">
            <Truck className="w-3 h-3 stroke-[1.5]" /> Đang giao hàng
          </Badge>
        )
      case 'Processing':
      case 'Confirmed':
        return (
          <Badge variant="warn" className="flex items-center gap-1">
            <Clock className="w-3 h-3 stroke-[1.5]" /> Đang chuẩn bị hàng
          </Badge>
        )
      case 'Cancelled':
        return (
          <Badge variant="destructive" className="flex items-center gap-1">
            <XCircle className="w-3 h-3 stroke-[1.5]" /> Đã hủy
          </Badge>
        )
      default:
        return (
          <Badge variant="outline" className="flex items-center gap-1">
            <Clock className="w-3 h-3 stroke-[1.5]" /> Chờ xử lý
          </Badge>
        )
    }
  }

  const statusTabs = [
    { id: 'all', label: 'Tất cả' },
    { id: 'Pending', label: 'Chờ xử lý' },
    { id: 'Processing', label: 'Đang chuẩn bị' },
    { id: 'Shipping', label: 'Đang giao' },
    { id: 'Delivered', label: 'Đã giao' },
    { id: 'Cancelled', label: 'Đã hủy' },
  ]

  return (
    <div className="space-y-6 max-w-4xl mx-auto">
      {/* Title */}
      <div>
        <h1 className="font-display font-bold text-2xl sm:text-3xl text-ink">
          Lịch sử đơn hàng
        </h1>
        <p className="text-xs text-ink-2 mt-1">
          Quản lý và theo dõi tiến độ các đơn hàng của bạn
        </p>
      </div>

      {/* Status Filter Tabs */}
      <div className="flex gap-2 overflow-x-auto pb-1 border-b border-rule">
        {statusTabs.map((tab) => (
          <button
            key={tab.id}
            type="button"
            onClick={() => setActiveTab(tab.id)}
            className={`px-3 py-2 text-xs font-semibold whitespace-nowrap border-b-2 transition-colors cursor-pointer ${
              activeTab === tab.id
                ? 'border-leaf text-leaf'
                : 'border-transparent text-ink-2 hover:text-ink'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Orders List */}
      {isLoading ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <div
              key={i}
              className="p-5 rounded-2xl bg-paper-2 border border-rule space-y-3 animate-pulse"
            >
              <Skeleton className="h-6 w-1/3" />
              <Skeleton className="h-16 w-full" />
              <Skeleton className="h-8 w-1/4 ml-auto" />
            </div>
          ))}
        </div>
      ) : orders.length === 0 ? (
        <div className="text-center py-16 bg-paper-2 rounded-2xl border border-rule px-4">
          <Package className="w-12 h-12 text-ink-2/40 mx-auto mb-3 stroke-[1.5]" />
          <h3 className="font-display font-semibold text-base text-ink mb-1">
            Không có đơn hàng nào
          </h3>
          <p className="text-xs text-ink-2 mb-4">
            Bạn chưa có đơn hàng nào ở trạng thái này.
          </p>
          <Button variant="outline" size="sm" onClick={() => navigate('/c/tat-ca')}>
            Mua sắm ngay
          </Button>
        </div>
      ) : (
        <div className="space-y-4">
          {orders.map((order) => (
            <div
              key={order.id}
              className="p-5 rounded-2xl bg-paper-2 border border-rule hover:border-leaf/40 transition-colors space-y-4"
            >
              {/* Order header */}
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-rule pb-3">
                <div className="flex items-center gap-3">
                  <span className="font-mono font-bold text-xs text-ink">
                    #{order.orderNumber}
                  </span>
                  <span className="text-[11px] text-ink-2">
                    {formatDateTime(order.placedAt)}
                  </span>
                </div>
                <div>{renderStatusBadge(order.status)}</div>
              </div>

              {/* Order quick overview */}
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <div className="text-xs text-ink-2 space-y-1">
                  <p>
                    Phương thức thanh toán:{' '}
                    <strong className="text-ink">
                      {order.paymentMethod === 'COD' ? 'Tiền mặt khi nhận hàng (COD)' : order.paymentMethod}
                    </strong>
                  </p>
                  <p>
                    Trạng thái thanh toán:{' '}
                    <span className="font-medium text-ink">
                      {order.paymentStatus === 'Paid' ? 'Đã thanh toán' : 'Chờ thanh toán khi nhận'}
                    </span>
                  </p>
                </div>

                <div className="text-right sm:border-l border-rule sm:pl-6 space-y-1">
                  <span className="text-xs text-ink-2 block">Tổng tiền đơn hàng</span>
                  <span className="font-display font-bold text-lg text-leaf tabular-nums">
                    {formatVnd(Number(order.total))}
                  </span>
                </div>
              </div>

              {/* Order actions */}
              <div className="flex items-center justify-end gap-3 pt-2 border-t border-rule/60">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => navigate(`/orders/${order.id}`)}
                  className="text-xs h-8 flex items-center gap-1 font-semibold"
                >
                  <span>Xem chi tiết</span>
                  <ChevronRight className="w-3.5 h-3.5 stroke-[1.5]" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
