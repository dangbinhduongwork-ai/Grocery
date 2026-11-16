import React, { useEffect, useState, useCallback } from 'react'
import {
  Eye,
  RefreshCw,
  Truck,
  Printer,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { formatVnd } from '@/shared/utils/formatVnd'
import { formatDateTime } from '@/shared/utils/dateTime'
import { useRouter } from '@/app/router'
import { StatusChip } from '../components/StatusChip'
import { DataTable, Column } from '../components/DataTable'
import { OrderInvoiceModal, InvoiceOrderData } from '../components/OrderInvoiceModal'

interface OrderListItem {
  id: string
  orderNumber: string
  placedAt: string
  status: string
  paymentMethod: string
  paymentStatus: string
  recipientName: string
  recipientPhone: string
  total: number
  itemsCount: number
}

const statusTabs = [
  { key: 'ALL', label: 'Tất cả' },
  { key: 'Placed', label: 'Mới đặt' },
  { key: 'Confirmed', label: 'Đã xác nhận' },
  { key: 'Preparing', label: 'Đang chuẩn bị' },
  { key: 'Packed', label: 'Đã đóng gói' },
  { key: 'Shipping', label: 'Đang giao' },
  { key: 'Delivered', label: 'Đã giao' },
  { key: 'Cancelled', label: 'Đã hủy' },
]

export const AdminOrdersPage: React.FC = () => {
  const router = useRouter()
  const [orders, setOrders] = useState<OrderListItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [selectedTab, setSelectedTab] = useState('ALL')
  const [invoiceOrder, setInvoiceOrder] = useState<InvoiceOrderData | null>(null)
  const [isInvoiceOpen, setIsInvoiceOpen] = useState(false)
  const [loadingInvoiceId, setLoadingInvoiceId] = useState<string | null>(null)

  const handleOpenInvoice = async (orderId: string) => {
    try {
      setLoadingInvoiceId(orderId)
      const res = await api.GET('/api/v1/admin/orders/{id}', {
        params: { path: { id: orderId } },
      })
      if (res.data) {
        setInvoiceOrder(res.data as unknown as InvoiceOrderData)
        setIsInvoiceOpen(true)
      }
    } catch {
      // Fallback
    } finally {
      setLoadingInvoiceId(null)
    }
  }

  const fetchOrders = useCallback(async () => {
    setIsLoading(true)
    try {
      const query: Record<string, string | number> = { pageSize: 100 }
      if (selectedTab !== 'ALL') {
        query.status = selectedTab
      }

      const res = await api.GET('/api/v1/admin/orders', {
        params: { query },
      })

      if (res.data?.items) {
        setOrders(res.data.items as unknown as OrderListItem[])
      } else {
        setOrders([])
      }
    } catch {
      setOrders([])
    } finally {
      setIsLoading(false)
    }
  }, [selectedTab])

  useEffect(() => {
    void fetchOrders()
  }, [fetchOrders])

  const columns: Column<OrderListItem>[] = [
    {
      key: 'orderNumber',
      title: 'Mã đơn hàng',
      sortable: true,
      render: o => (
        <span className="font-mono font-semibold text-leaf hover:underline cursor-pointer" onClick={() => router.push(`/admin/orders/${o.id}`)}>
          {o.orderNumber}
        </span>
      ),
    },
    {
      key: 'placedAt',
      title: 'Ngày đặt',
      sortable: true,
      render: o => (
        <span className="text-xs text-slate-500 whitespace-nowrap">
          {formatDateTime(o.placedAt)}
        </span>
      ),
    },
    {
      key: 'recipientName',
      title: 'Khách hàng',
      sortable: true,
      render: o => (
        <div>
          <div className="font-medium text-slate-800">{o.recipientName}</div>
          <div className="text-xs text-slate-400 font-mono">{o.recipientPhone}</div>
        </div>
      ),
    },
    {
      key: 'status',
      title: 'Trạng thái đơn',
      render: o => <StatusChip status={o.status} type="order" />,
    },
    {
      key: 'paymentStatus',
      title: 'Thanh toán',
      render: o => (
        <div className="flex flex-col gap-1 items-start">
          <StatusChip status={o.paymentStatus} type="payment" />
          <span className="text-[11px] text-slate-400 font-mono uppercase">{o.paymentMethod}</span>
        </div>
      ),
    },
    {
      key: 'total',
      title: 'Tổng tiền',
      sortable: true,
      className: 'text-right',
      render: o => (
        <div className="text-right">
          <div className="font-semibold text-slate-900">{formatVnd(o.total)}</div>
          <div className="text-[11px] text-slate-400">{o.itemsCount} sản phẩm</div>
        </div>
      ),
    },
    {
      key: 'actions',
      title: 'Thao tác',
      className: 'text-right',
      render: o => (
        <div className="flex items-center justify-end gap-1.5">
          <button
            type="button"
            onClick={() => void handleOpenInvoice(o.id)}
            disabled={loadingInvoiceId === o.id}
            className="p-1.5 rounded-lg border border-slate-200 text-slate-600 hover:text-leaf hover:border-leaf/30 hover:bg-leaf/5 transition-colors cursor-pointer"
            title="In phiếu giao hàng & hóa đơn"
          >
            <Printer className={`w-4 h-4 ${loadingInvoiceId === o.id ? 'animate-spin' : ''}`} strokeWidth={1.5} />
          </button>
          <button
            type="button"
            onClick={() => router.push(`/admin/orders/${o.id}`)}
            className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg border border-slate-200 text-slate-600 hover:text-leaf hover:border-leaf/30 hover:bg-leaf/5 transition-colors text-xs font-semibold cursor-pointer"
            title="Xem chi tiết đơn hàng"
          >
            <Eye className="w-3.5 h-3.5" strokeWidth={1.5} />
            <span>Chi tiết</span>
          </button>
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 font-display">Quản lý đơn hàng</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Xử lý quy trình đơn hàng: xác nhận, lấy hàng, đóng gói, giao hàng và thu hộ COD.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => router.push('/admin/picking')}
            className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl bg-leaf text-white text-xs font-semibold hover:bg-leaf-2 transition-colors shadow-xs"
          >
            <Truck className="w-4 h-4" strokeWidth={1.5} />
            Chế độ lấy hàng (Picking)
          </button>
          <button
            type="button"
            onClick={() => void fetchOrders()}
            disabled={isLoading}
            className="p-2 rounded-xl border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 transition-colors shadow-2xs"
            title="Làm mới"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} strokeWidth={1.5} />
          </button>
        </div>
      </div>

      {/* Status Filter Tabs */}
      <div className="flex items-center gap-1.5 overflow-x-auto pb-1 border-b border-slate-200">
        {statusTabs.map(tab => (
          <button
            key={tab.key}
            type="button"
            onClick={() => setSelectedTab(tab.key)}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium whitespace-nowrap transition-colors ${
              selectedTab === tab.key
                ? 'bg-slate-800 text-white font-semibold'
                : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Orders Table */}
      <DataTable
        data={orders}
        columns={columns}
        keyExtractor={o => o.id}
        searchPlaceholder="Tìm theo mã đơn, tên hoặc số điện thoại khách hàng..."
        searchableKey={o => `${o.orderNumber} ${o.recipientName} ${o.recipientPhone}`}
        pageSize={15}
        isLoading={isLoading}
        emptyMessage="Không có đơn hàng nào phù hợp với bộ lọc hiện tại"
      />

      {/* Delivery Invoice Modal */}
      <OrderInvoiceModal
        order={invoiceOrder}
        isOpen={isInvoiceOpen}
        onClose={() => setIsInvoiceOpen(false)}
      />
    </div>
  )
}
