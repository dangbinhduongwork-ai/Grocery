import React, { useEffect, useState, useCallback } from 'react'
import {
  RefreshCw,
  Building2,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { formatVnd } from '@/shared/utils/formatVnd'
import { formatDateTime } from '@/shared/utils/dateTime'
import { DataTable, Column } from '../components/DataTable'

interface PurchaseOrderItem {
  id: string
  poNumber: string
  supplierName: string
  orderDate: string
  status: string
  totalAmount: number
  linesCount: number
}

export const AdminPurchasingPage: React.FC = () => {
  const [orders, setOrders] = useState<PurchaseOrderItem[]>([])
  const [isLoading, setIsLoading] = useState(true)

  const fetchOrders = useCallback(async () => {
    setIsLoading(true)
    try {
      const res = await api.GET('/api/v1/admin/purchase-orders', {
        params: { query: { pageSize: 100 } },
      })
      if (res.data?.items) {
        setOrders(res.data.items as unknown as PurchaseOrderItem[])
      } else {
        setOrders([])
      }
    } catch {
      setOrders([])
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void fetchOrders()
  }, [fetchOrders])

  const columns: Column<PurchaseOrderItem>[] = [
    {
      key: 'poNumber',
      title: 'Mã đơn PO',
      sortable: true,
      render: p => <span className="font-mono text-xs font-semibold text-leaf">{p.poNumber}</span>,
    },
    {
      key: 'supplierName',
      title: 'Nhà cung cấp',
      sortable: true,
      render: p => (
        <div className="font-medium text-slate-800 flex items-center gap-1.5">
          <Building2 className="w-3.5 h-3.5 text-slate-400" />
          <span>{p.supplierName}</span>
        </div>
      ),
    },
    {
      key: 'orderDate',
      title: 'Ngày đặt',
      sortable: true,
      render: p => <span className="text-xs text-slate-500">{formatDateTime(p.orderDate)}</span>,
    },
    {
      key: 'totalAmount',
      title: 'Tổng giá trị',
      sortable: true,
      className: 'text-right',
      render: p => <span className="font-semibold text-slate-900 font-display">{formatVnd(p.totalAmount)}</span>,
    },
    {
      key: 'status',
      title: 'Trạng thái',
      render: p => (
        <span
          className={`px-2 py-0.5 rounded-full text-xs font-semibold ${
            p.status === 'Received'
              ? 'bg-emerald-50 text-emerald-700'
              : p.status === 'Ordered'
              ? 'bg-blue-50 text-blue-700'
              : p.status === 'Draft'
              ? 'bg-slate-100 text-slate-600'
              : 'bg-rose-50 text-rose-700'
          }`}
        >
          {p.status}
        </span>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 font-display">Đơn mua hàng (Purchasing)</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Quản lý đơn đặt hàng nhà cung cấp, quy trình nhập kho và tính toán giá vốn bình quân di động (BR-11).
          </p>
        </div>
        <div className="flex items-center gap-2">
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

      <DataTable
        data={orders}
        columns={columns}
        keyExtractor={p => p.id}
        searchPlaceholder="Tìm theo mã đơn PO hoặc nhà cung cấp..."
        searchableKey={p => `${p.poNumber} ${p.supplierName}`}
        pageSize={15}
        isLoading={isLoading}
        emptyMessage="Chưa có đơn mua hàng nào"
      />
    </div>
  )
}
