import React, { useEffect, useState, useCallback } from 'react'
import {
  Plus,
  RefreshCw,
  X,
  Edit,
  Trash2,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { formatVnd } from '@/shared/utils/formatVnd'
import { formatDateTime } from '@/shared/utils/dateTime'
import { DataTable, Column } from '../components/DataTable'

interface CouponItem {
  id: string
  code: string
  name: string
  type: string
  value: number
  maxDiscount?: number | null
  minSubtotal: number
  startsAt: string
  endsAt: string
  usageLimit?: number | null
  usedCount: number
  isActive: boolean
}

export const AdminCouponsPage: React.FC = () => {
  const [coupons, setCoupons] = useState<CouponItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isDialogOpen, setIsDialogOpen] = useState(false)

  // Create Form State
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [type, setType] = useState<'Percent' | 'FixedAmount' | 'FreeShipping'>('Percent')
  const [value, setValue] = useState(10)
  const [minSubtotal, setMinSubtotal] = useState(200000)
  const [startsAt, setStartsAt] = useState(new Date().toISOString().slice(0, 16))
  const [endsAt, setEndsAt] = useState(new Date(Date.now() + 30 * 86400000).toISOString().slice(0, 16))
  const [usageLimit, setUsageLimit] = useState(100)

  // Edit Form State
  const [editingCoupon, setEditingCoupon] = useState<CouponItem | null>(null)
  const [editCode, setEditCode] = useState('')
  const [editName, setEditName] = useState('')
  const [editType, setEditType] = useState<'Percent' | 'FixedAmount' | 'FreeShipping'>('Percent')
  const [editValue, setEditValue] = useState(10)
  const [editMinSubtotal, setEditMinSubtotal] = useState(200000)
  const [editStartsAt, setEditStartsAt] = useState('')
  const [editEndsAt, setEditEndsAt] = useState('')
  const [editUsageLimit, setEditUsageLimit] = useState(100)
  const [editIsActive, setEditIsActive] = useState(true)

  const fetchCoupons = useCallback(async () => {
    setIsLoading(true)
    try {
      const res = await api.GET('/api/v1/admin/coupons', {
        params: { query: { pageSize: 100 } },
      })
      if (res.data?.items) {
        setCoupons(res.data.items as unknown as CouponItem[])
      } else {
        setCoupons([])
      }
    } catch {
      setCoupons([])
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void fetchCoupons()
  }, [fetchCoupons])

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!code.trim() || !name.trim()) return

    try {
      const res = await api.POST('/api/v1/admin/coupons', {
        body: {
          code: code.toUpperCase().trim(),
          name: name.trim(),
          description: null,
          type,
          value,
          maxDiscount: null,
          minSubtotal,
          startsAt: new Date(startsAt).toISOString(),
          endsAt: new Date(endsAt).toISOString(),
          usageLimit: usageLimit > 0 ? usageLimit : null,
          perUserLimit: null,
          isActive: true,
          scopes: null,
        },
      })

      if (res.error) {
        alert((res.error as { detail?: string })?.detail ?? 'Tạo mã thất bại.')
      } else {
        setIsDialogOpen(false)
        setCode('')
        setName('')
        await fetchCoupons()
      }
    } catch {
      alert('Lỗi kết nối máy chủ.')
    }
  }

  // Open Edit Dialog
  const openEditCoupon = (c: CouponItem) => {
    setEditingCoupon(c)
    setEditCode(c.code)
    setEditName(c.name)
    setEditType(c.type as 'Percent' | 'FixedAmount' | 'FreeShipping')
    setEditValue(c.value)
    setEditMinSubtotal(c.minSubtotal)
    setEditStartsAt(new Date(c.startsAt).toISOString().slice(0, 16))
    setEditEndsAt(new Date(c.endsAt).toISOString().slice(0, 16))
    setEditUsageLimit(c.usageLimit ?? 0)
    setEditIsActive(c.isActive)
  }

  // Handle Update Coupon
  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingCoupon || !editCode.trim() || !editName.trim()) return

    try {
      const res = await api.PUT('/api/v1/admin/coupons/{id}', {
        params: { path: { id: editingCoupon.id } },
        body: {
          code: editCode.toUpperCase().trim(),
          name: editName.trim(),
          description: null,
          type: editType,
          value: editValue,
          maxDiscount: null,
          minSubtotal: editMinSubtotal,
          startsAt: new Date(editStartsAt).toISOString(),
          endsAt: new Date(editEndsAt).toISOString(),
          usageLimit: editUsageLimit > 0 ? editUsageLimit : null,
          perUserLimit: null,
          isActive: editIsActive,
          scopes: null,
        },
      })

      if (res.error) {
        alert((res.error as { detail?: string })?.detail ?? 'Cập nhật mã thất bại.')
      } else {
        setEditingCoupon(null)
        await fetchCoupons()
      }
    } catch {
      alert('Lỗi khi cập nhật mã giảm giá.')
    }
  }

  // Handle Delete Coupon
  const handleDelete = async (id: string, code: string) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa mã giảm giá "${code}"?`)) return

    try {
      const res = await api.DELETE('/api/v1/admin/coupons/{id}', {
        params: { path: { id } },
      })

      if (res.error) {
        alert((res.error as { detail?: string })?.detail ?? 'Không thể xóa mã giảm giá.')
      } else {
        await fetchCoupons()
      }
    } catch {
      alert('Lỗi khi xóa mã giảm giá.')
    }
  }

  const columns: Column<CouponItem>[] = [
    {
      key: 'code',
      title: 'Mã giảm giá',
      sortable: true,
      render: c => <span className="font-mono font-bold text-xs text-leaf bg-leaf/10 px-2.5 py-1 rounded-md">{c.code}</span>,
    },
    {
      key: 'name',
      title: 'Tên chương trình',
      sortable: true,
      render: c => (
        <div>
          <div className="font-semibold text-slate-800">{c.name}</div>
          <div className="text-xs text-slate-400">Đơn tối thiểu: {formatVnd(c.minSubtotal)}</div>
        </div>
      ),
    },
    {
      key: 'type',
      title: 'Mức giảm',
      render: c => (
        <span className="font-semibold text-slate-900 font-display">
          {c.type === 'Percent' ? `${c.value}%` : c.type === 'FixedAmount' ? formatVnd(c.value) : 'Freeship'}
        </span>
      ),
    },
    {
      key: 'period',
      title: 'Thời hạn áp dụng',
      render: c => (
        <div className="text-xs text-slate-500 space-y-0.5">
          <div>Từ: {formatDateTime(c.startsAt)}</div>
          <div>Đến: {formatDateTime(c.endsAt)}</div>
        </div>
      ),
    },
    {
      key: 'usage',
      title: 'Lượt sử dụng',
      render: c => (
        <span className="text-xs font-mono">
          <b className="text-slate-800">{c.usedCount}</b> / {c.usageLimit ?? '∞'}
        </span>
      ),
    },
    {
      key: 'isActive',
      title: 'Trạng thái',
      render: c => (
        <span className={`px-2 py-0.5 rounded-full text-xs font-semibold ${c.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
          {c.isActive ? 'Đang kích hoạt' : 'Tạm dừng'}
        </span>
      ),
    },
    {
      key: 'actions',
      title: 'Thao tác',
      render: c => (
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => openEditCoupon(c)}
            className="p-1.5 text-slate-500 hover:text-leaf hover:bg-slate-100 rounded-lg transition-colors"
            title="Chỉnh sửa mã giảm giá"
          >
            <Edit className="w-4 h-4" strokeWidth={1.5} />
          </button>
          <button
            type="button"
            onClick={() => void handleDelete(c.id, c.code)}
            className="p-1.5 text-slate-500 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors"
            title="Xóa mã giảm giá"
          >
            <Trash2 className="w-4 h-4" strokeWidth={1.5} />
          </button>
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 font-display">Mã giảm giá & Khuyến mãi</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Quản lý phiếu giảm giá, điều kiện áp dụng và giới hạn lượt sử dụng.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setIsDialogOpen(true)}
            className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl bg-leaf text-white text-xs font-semibold hover:bg-leaf-2 transition-colors shadow-xs"
          >
            <Plus className="w-4 h-4" strokeWidth={1.5} />
            Tạo mã giảm giá
          </button>
          <button
            type="button"
            onClick={() => void fetchCoupons()}
            disabled={isLoading}
            className="p-2 rounded-xl border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 transition-colors shadow-2xs"
            title="Làm mới"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} strokeWidth={1.5} />
          </button>
        </div>
      </div>

      <DataTable
        data={coupons}
        columns={columns}
        keyExtractor={c => c.id}
        searchPlaceholder="Tìm theo mã hoặc tên chương trình..."
        searchableKey={c => `${c.code} ${c.name}`}
        pageSize={10}
        isLoading={isLoading}
        emptyMessage="Chưa có mã giảm giá nào"
      />

      {/* Create Modal Dialog */}
      {isDialogOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-xs">
          <div className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-slate-200 space-y-4 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between">
              <h2 className="text-base font-bold text-slate-900 font-display">Tạo mã giảm giá mới</h2>
              <button
                type="button"
                onClick={() => setIsDialogOpen(false)}
                className="p-1 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100"
              >
                <X className="w-5 h-5" strokeWidth={1.5} />
              </button>
            </div>

            <form onSubmit={handleCreate} className="space-y-4 text-xs">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Mã Coupon *</label>
                  <input
                    type="text"
                    required
                    value={code}
                    onChange={e => setCode(e.target.value.toUpperCase())}
                    placeholder="VD: FREESHIP50"
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono uppercase focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Loại giảm giá</label>
                  <select
                    value={type}
                    onChange={e => setType(e.target.value as 'Percent' | 'FixedAmount' | 'FreeShipping')}
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  >
                    <option value="Percent">Phần trăm (%)</option>
                    <option value="FixedAmount">Số tiền cố định (₫)</option>
                    <option value="FreeShipping">Miễn phí vận chuyển</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block font-semibold text-slate-700 mb-1">Tên chương trình khuyến mãi *</label>
                <input
                  type="text"
                  required
                  value={name}
                  onChange={e => setName(e.target.value)}
                  placeholder="VD: Tri ân khách hàng tháng 11"
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">
                    {type === 'Percent' ? 'Mức giảm (%)' : 'Số tiền giảm (₫)'} *
                  </label>
                  <input
                    type="number"
                    min="1"
                    required
                    value={value}
                    onChange={e => setValue(Number(e.target.value))}
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Đơn tối thiểu (₫) *</label>
                  <input
                    type="number"
                    min="0"
                    step="1000"
                    required
                    value={minSubtotal}
                    onChange={e => setMinSubtotal(Number(e.target.value))}
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Ngày bắt đầu *</label>
                  <input
                    type="datetime-local"
                    required
                    value={startsAt}
                    onChange={e => setStartsAt(e.target.value)}
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Ngày kết thúc *</label>
                  <input
                    type="datetime-local"
                    required
                    value={endsAt}
                    onChange={e => setEndsAt(e.target.value)}
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
              </div>

              <div>
                <label className="block font-semibold text-slate-700 mb-1">Giới hạn tổng lượt dùng (để 0 nếu không giới hạn)</label>
                <input
                  type="number"
                  min="0"
                  value={usageLimit}
                  onChange={e => setUsageLimit(Number(e.target.value))}
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>

              <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setIsDialogOpen(false)}
                  className="px-3.5 py-1.5 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 font-medium"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 rounded-lg bg-leaf text-white font-semibold hover:bg-leaf-2 shadow-xs"
                >
                  Tạo mã
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit Modal Dialog */}
      {editingCoupon && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-xs">
          <div className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-slate-200 space-y-4 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between">
              <h2 className="text-base font-bold text-slate-900 font-display">Chỉnh sửa mã giảm giá</h2>
              <button
                type="button"
                onClick={() => setEditingCoupon(null)}
                className="p-1 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100"
              >
                <X className="w-5 h-5" strokeWidth={1.5} />
              </button>
            </div>

            <form onSubmit={handleUpdate} className="space-y-4 text-xs">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Mã Coupon *</label>
                  <input
                    type="text"
                    required
                    value={editCode}
                    onChange={e => setEditCode(e.target.value.toUpperCase())}
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono uppercase focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Loại giảm giá</label>
                  <select
                    value={editType}
                    onChange={e => setEditType(e.target.value as 'Percent' | 'FixedAmount' | 'FreeShipping')}
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  >
                    <option value="Percent">Phần trăm (%)</option>
                    <option value="FixedAmount">Số tiền cố định (₫)</option>
                    <option value="FreeShipping">Miễn phí vận chuyển</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block font-semibold text-slate-700 mb-1">Tên chương trình khuyến mãi *</label>
                <input
                  type="text"
                  required
                  value={editName}
                  onChange={e => setEditName(e.target.value)}
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">
                    {editType === 'Percent' ? 'Mức giảm (%)' : 'Số tiền giảm (₫)'} *
                  </label>
                  <input
                    type="number"
                    min="1"
                    required
                    value={editValue}
                    onChange={e => setEditValue(Number(e.target.value))}
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Đơn tối thiểu (₫) *</label>
                  <input
                    type="number"
                    min="0"
                    step="1000"
                    required
                    value={editMinSubtotal}
                    onChange={e => setEditMinSubtotal(Number(e.target.value))}
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Ngày bắt đầu *</label>
                  <input
                    type="datetime-local"
                    required
                    value={editStartsAt}
                    onChange={e => setEditStartsAt(e.target.value)}
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Ngày kết thúc *</label>
                  <input
                    type="datetime-local"
                    required
                    value={editEndsAt}
                    onChange={e => setEditEndsAt(e.target.value)}
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
              </div>

              <div>
                <label className="block font-semibold text-slate-700 mb-1">Giới hạn tổng lượt dùng (để 0 nếu không giới hạn)</label>
                <input
                  type="number"
                  min="0"
                  value={editUsageLimit}
                  onChange={e => setEditUsageLimit(Number(e.target.value))}
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>

              <div className="flex items-center gap-2 pt-1">
                <input
                  type="checkbox"
                  id="editIsActive"
                  checked={editIsActive}
                  onChange={e => setEditIsActive(e.target.checked)}
                  className="w-4 h-4 text-leaf rounded focus:ring-leaf"
                />
                <label htmlFor="editIsActive" className="font-semibold text-slate-700 cursor-pointer">
                  Kích hoạt mã giảm giá này
                </label>
              </div>

              <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setEditingCoupon(null)}
                  className="px-3.5 py-1.5 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 font-medium"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 rounded-lg bg-leaf text-white font-semibold hover:bg-leaf-2 shadow-xs"
                >
                  Lưu thay đổi
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
