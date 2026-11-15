import React, { useEffect, useState, useCallback } from 'react'
import {
  RefreshCw,
  Lock,
  Unlock,
  UserX,
  Phone,
  Mail,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { formatVnd } from '@/shared/utils/formatVnd'
import { useAuth } from '@/shared/auth'
import { DataTable, Column } from '../components/DataTable'

interface CustomerItem {
  id: string
  fullName: string
  email: string
  phone?: string | null
  status: string
  totalOrdersCount: number
  totalSpent: number
}

export const AdminCustomersPage: React.FC = () => {
  const { hasPermission, isAdmin } = useAuth()
  const [customers, setCustomers] = useState<CustomerItem[]>([])
  const [isLoading, setIsLoading] = useState(true)

  const canLock = hasPermission('customers.lock')

  const fetchCustomers = useCallback(async () => {
    setIsLoading(true)
    try {
      const res = await api.GET('/api/v1/admin/customers', {
        params: { query: { pageSize: 100 } },
      })
      if (res.data?.items) {
        setCustomers(res.data.items as unknown as CustomerItem[])
      } else {
        setCustomers([])
      }
    } catch {
      setCustomers([])
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void fetchCustomers()
  }, [fetchCustomers])

  const handleToggleLock = async (customer: CustomerItem) => {
    const isLocking = customer.status === 'Active'
    const reason = prompt(`Nhập lý do ${isLocking ? 'khóa' : 'mở khóa'} tài khoản:`)
    if (!reason) return

    try {
      if (isLocking) {
        await api.POST('/api/v1/admin/customers/{id}/lock', {
          params: { path: { id: customer.id } },
          body: { reason },
        })
      } else {
        await api.POST('/api/v1/admin/customers/{id}/unlock', {
          params: { path: { id: customer.id } },
          body: { reason },
        })
      }
      await fetchCustomers()
    } catch {
      alert('Thao tác khóa/mở khóa thất bại.')
    }
  }

  const handleAnonymize = async (customer: CustomerItem) => {
    if (!confirm(`Bạn có chắc muốn ẩn danh hóa dữ liệu khách hàng "${customer.fullName}" theo quy định bảo mật NFR-010? Thao tác này không thể hoàn tác.`)) {
      return
    }

    const reason = prompt('Nhập lý do ẩn danh hóa dữ liệu:')
    if (!reason) return

    try {
      const res = await api.POST('/api/v1/admin/customers/{id}/anonymize', {
        params: { path: { id: customer.id } },
        body: { reason },
      })

      if (res.error) {
        alert((res.error as { detail?: string })?.detail ?? 'Không thể ẩn danh hóa.')
      } else {
        alert('Đã ẩn danh hóa dữ liệu khách hàng thành công!')
        await fetchCustomers()
      }
    } catch {
      alert('Lỗi kết nối máy chủ.')
    }
  }

  const columns: Column<CustomerItem>[] = [
    {
      key: 'fullName',
      title: 'Họ và tên',
      sortable: true,
      render: c => (
        <div className="font-semibold text-slate-900 flex items-center gap-2">
          <div className="w-7 h-7 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center text-xs font-bold shrink-0">
            {c.fullName.charAt(0)}
          </div>
          <span>{c.fullName}</span>
        </div>
      ),
    },
    {
      key: 'contact',
      title: 'Liên hệ',
      render: c => (
        <div className="text-xs space-y-0.5 text-slate-500">
          <div className="flex items-center gap-1.5">
            <Mail className="w-3.5 h-3.5 text-slate-400" />
            <span>{c.email}</span>
          </div>
          {c.phone && (
            <div className="flex items-center gap-1.5 font-mono">
              <Phone className="w-3.5 h-3.5 text-slate-400" />
              <span>{c.phone}</span>
            </div>
          )}
        </div>
      ),
    },
    {
      key: 'orders',
      title: 'Đơn hàng & Doanh số',
      sortable: true,
      className: 'text-right',
      render: c => (
        <div className="text-right">
          <div className="font-semibold text-slate-900 font-display">{formatVnd(c.totalSpent)}</div>
          <div className="text-[11px] text-slate-400">{c.totalOrdersCount} đơn hàng</div>
        </div>
      ),
    },
    {
      key: 'status',
      title: 'Trạng thái',
      render: c => (
        <span
          className={`px-2 py-0.5 rounded-full text-xs font-semibold ${
            c.status === 'Active'
              ? 'bg-emerald-50 text-emerald-700'
              : 'bg-rose-50 text-rose-700 border border-rose-200'
          }`}
        >
          {c.status === 'Active' ? 'Hoạt động' : 'Bị khóa'}
        </span>
      ),
    },
    {
      key: 'actions',
      title: 'Thao tác',
      className: 'text-right',
      render: c => (
        <div className="flex items-center justify-end gap-1.5">
          {canLock && (
            <button
              type="button"
              onClick={() => void handleToggleLock(c)}
              className={`p-1.5 rounded-lg border transition-colors ${
                c.status === 'Active'
                  ? 'border-slate-200 text-slate-600 hover:text-rose-600 hover:border-rose-200 hover:bg-rose-50'
                  : 'border-emerald-200 text-emerald-700 bg-emerald-50 hover:bg-emerald-100'
              }`}
              title={c.status === 'Active' ? 'Khóa tài khoản' : 'Mở khóa'}
            >
              {c.status === 'Active' ? <Lock className="w-4 h-4" strokeWidth={1.5} /> : <Unlock className="w-4 h-4" strokeWidth={1.5} />}
            </button>
          )}

          {isAdmin && (
            <button
              type="button"
              onClick={() => void handleAnonymize(c)}
              className="p-1.5 rounded-lg border border-slate-200 text-slate-500 hover:text-amber-700 hover:border-amber-200 hover:bg-amber-50 transition-colors"
              title="Ẩn danh hóa dữ liệu (NFR-010)"
            >
              <UserX className="w-4 h-4" strokeWidth={1.5} />
            </button>
          )}
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 font-display">Khách hàng</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Quản lý tài khoản người tiêu dùng, số lượng đơn hàng, lịch sử mua sắm và chính sách ẩn danh hóa (NFR-010).
          </p>
        </div>
        <button
          type="button"
          onClick={() => void fetchCustomers()}
          disabled={isLoading}
          className="p-2 rounded-xl border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 transition-colors shadow-2xs"
          title="Làm mới"
        >
          <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} strokeWidth={1.5} />
        </button>
      </div>

      <DataTable
        data={customers}
        columns={columns}
        keyExtractor={c => c.id}
        searchPlaceholder="Tìm theo tên, email hoặc số điện thoại..."
        searchableKey={c => `${c.fullName} ${c.email} ${c.phone ?? ''}`}
        pageSize={15}
        isLoading={isLoading}
        emptyMessage="Chưa có khách hàng nào"
      />
    </div>
  )
}
