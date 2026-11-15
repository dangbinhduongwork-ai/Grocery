import React, { useEffect, useState, useCallback } from 'react'
import {
  RefreshCw,
  Sliders,
  History,
  AlertTriangle,
  X,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { formatDateTime } from '@/shared/utils/dateTime'
import { useAuth } from '@/shared/auth'
import { DataTable, Column } from '../components/DataTable'

interface StockItem {
  productId: string
  sku: string
  productName: string
  categoryName?: string
  unit: string
  onHand: number
  reserved: number
  available: number
  reorderLevel: number
  isLowStock: boolean
  costPrice?: number | null
}

interface StockMovement {
  id: string
  type: string
  quantity: number
  unitCost?: number | null
  reason?: string
  createdAt: string
}

export const AdminInventoryPage: React.FC = () => {
  const { hasPermission } = useAuth()
  const [items, setItems] = useState<StockItem[]>([])
  const [isLoading, setIsLoading] = useState(true)

  // Adjustment modal
  const [adjustTarget, setAdjustTarget] = useState<StockItem | null>(null)
  const [adjustDelta, setAdjustDelta] = useState<number>(0)
  const [adjustReason, setAdjustReason] = useState('')
  const [isAdjusting, setIsAdjusting] = useState(false)

  // Movements modal
  const [movementTarget, setMovementTarget] = useState<StockItem | null>(null)
  const [movements, setMovements] = useState<StockMovement[]>([])
  const [isLoadingMovements, setIsLoadingMovements] = useState(false)

  const canAdjust = hasPermission('inventory.adjust')

  const fetchInventory = useCallback(async () => {
    setIsLoading(true)
    try {
      const res = await api.GET('/api/v1/admin/inventory', {
        params: { query: { pageSize: 100 } },
      })
      if (res.data?.items) {
        setItems(res.data.items as unknown as StockItem[])
      } else {
        setItems([])
      }
    } catch {
      setItems([])
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void fetchInventory()
  }, [fetchInventory])

  const handleAdjustSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!adjustTarget || adjustDelta === 0 || !adjustReason.trim()) return

    setIsAdjusting(true)
    try {
      const res = await api.POST('/api/v1/admin/inventory/{productId}/adjust', {
        params: { path: { productId: adjustTarget.productId } },
        body: { delta: adjustDelta, reason: adjustReason },
      })

      if (res.error) {
        alert((res.error as { detail?: string })?.detail ?? 'Điều chỉnh tồn kho thất bại.')
      } else {
        setAdjustTarget(null)
        setAdjustDelta(0)
        setAdjustReason('')
        await fetchInventory()
      }
    } catch {
      alert('Lỗi kết nối máy chủ.')
    } finally {
      setIsAdjusting(false)
    }
  }

  const handleOpenMovements = async (item: StockItem) => {
    setMovementTarget(item)
    setIsLoadingMovements(true)
    try {
      const res = await api.GET('/api/v1/admin/inventory/{productId}/movements', {
        params: { path: { productId: item.productId } },
      })
      if (res.data?.items) {
        setMovements(res.data.items as unknown as StockMovement[])
      } else {
        setMovements([])
      }
    } catch {
      setMovements([])
    } finally {
      setIsLoadingMovements(false)
    }
  }

  const columns: Column<StockItem>[] = [
    {
      key: 'sku',
      title: 'Mã SKU',
      sortable: true,
      render: s => <span className="font-mono text-xs font-semibold text-slate-700">{s.sku}</span>,
    },
    {
      key: 'productName',
      title: 'Tên sản phẩm',
      sortable: true,
      render: s => (
        <div>
          <div className="font-semibold text-slate-900">{s.productName}</div>
          <div className="text-xs text-slate-400">Đơn vị: {s.unit}</div>
        </div>
      ),
    },
    {
      key: 'onHand',
      title: 'Thực tế (On-hand)',
      sortable: true,
      className: 'text-right',
      render: s => <span className="font-bold text-slate-900 font-display">{s.onHand}</span>,
    },
    {
      key: 'reserved',
      title: 'Đang giữ chỗ',
      sortable: true,
      className: 'text-right',
      render: s => <span className="font-medium text-amber-600 font-mono">{s.reserved}</span>,
    },
    {
      key: 'available',
      title: 'Khả dụng bán',
      sortable: true,
      className: 'text-right',
      render: s => (
        <span className={`font-bold font-display ${s.available <= s.reorderLevel ? 'text-rose-600' : 'text-leaf'}`}>
          {s.available}
        </span>
      ),
    },
    {
      key: 'isLowStock',
      title: 'Trạng thái kho',
      render: s =>
        s.available <= 0 ? (
          <span className="px-2 py-0.5 rounded-full text-xs font-semibold bg-rose-50 text-rose-700 border border-rose-200">
            Hết hàng
          </span>
        ) : s.isLowStock ? (
          <span className="px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-50 text-amber-700 border border-amber-200 flex items-center gap-1 w-fit">
            <AlertTriangle className="w-3 h-3" strokeWidth={1.5} /> Sắp hết
          </span>
        ) : (
          <span className="px-2 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700">
            Đủ hàng
          </span>
        ),
    },
    {
      key: 'actions',
      title: 'Thao tác',
      className: 'text-right',
      render: s => (
        <div className="flex items-center justify-end gap-1.5">
          {canAdjust && (
            <button
              type="button"
              onClick={() => {
                setAdjustTarget(s)
                setAdjustDelta(0)
                setAdjustReason('')
              }}
              className="p-1.5 rounded-lg border border-slate-200 text-slate-600 hover:text-leaf hover:bg-leaf/5 transition-colors"
              title="Điều chỉnh tồn kho"
            >
              <Sliders className="w-4 h-4" strokeWidth={1.5} />
            </button>
          )}
          <button
            type="button"
            onClick={() => void handleOpenMovements(s)}
            className="p-1.5 rounded-lg border border-slate-200 text-slate-600 hover:text-indigo-600 hover:bg-indigo-50 transition-colors"
            title="Lịch sử xuất nhập kho"
          >
            <History className="w-4 h-4" strokeWidth={1.5} />
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
          <h1 className="text-2xl font-bold text-slate-900 font-display">Quản lý kho hàng</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Theo dõi số lượng tồn kho khả dụng (Available = OnHand - Reserved), kiểm soát thất thoát và cảnh báo ngưỡng tối thiểu.
          </p>
        </div>
        <button
          type="button"
          onClick={() => void fetchInventory()}
          disabled={isLoading}
          className="p-2 rounded-xl border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 transition-colors shadow-2xs"
          title="Làm mới"
        >
          <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} strokeWidth={1.5} />
        </button>
      </div>

      <DataTable
        data={items}
        columns={columns}
        keyExtractor={s => s.productId}
        searchPlaceholder="Tìm kiếm theo mã SKU hoặc tên sản phẩm..."
        searchableKey={s => `${s.sku} ${s.productName}`}
        pageSize={15}
        isLoading={isLoading}
        emptyMessage="Chưa có dữ liệu tồn kho"
      />

      {/* Adjust Modal */}
      {adjustTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-xs">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-200 space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-base font-bold text-slate-900 font-display">
                Điều chỉnh tồn kho: {adjustTarget.sku}
              </h2>
              <button
                type="button"
                onClick={() => setAdjustTarget(null)}
                className="text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" strokeWidth={1.5} />
              </button>
            </div>

            <div className="p-3 bg-slate-50 rounded-xl border border-slate-100 text-xs space-y-1">
              <div>Sản phẩm: <span className="font-semibold text-slate-800">{adjustTarget.productName}</span></div>
              <div>Tồn thực tế hiện tại: <span className="font-bold font-mono">{adjustTarget.onHand}</span></div>
              <div>Đang giữ chỗ cho đơn: <span className="font-bold font-mono text-amber-600">{adjustTarget.reserved}</span></div>
            </div>

            <form onSubmit={handleAdjustSubmit} className="space-y-4 text-xs">
              <div>
                <label className="block font-semibold text-slate-700 mb-1">
                  Số lượng thay đổi (Delta) *
                </label>
                <input
                  type="number"
                  required
                  value={adjustDelta}
                  onChange={e => setAdjustDelta(Number(e.target.value))}
                  placeholder="Nhập số âm để giảm (vd: -2), số dương để tăng (vd: 5)"
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono text-sm focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
                <span className="text-[11px] text-slate-400 mt-1 block">
                  Tồn mới sau điều chỉnh: <b className="font-mono text-slate-800">{adjustTarget.onHand + adjustDelta}</b>
                </span>
              </div>

              <div>
                <label className="block font-semibold text-slate-700 mb-1">
                  Lý do điều chỉnh (Bắt buộc theo BR-12) *
                </label>
                <textarea
                  required
                  rows={3}
                  value={adjustReason}
                  onChange={e => setAdjustReason(e.target.value)}
                  placeholder="VD: Kiểm kê định kỳ phát hiện thừa/thiếu, hàng dập nát hủy bỏ..."
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>

              <div className="flex items-center justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setAdjustTarget(null)}
                  className="px-4 py-2 rounded-xl border border-slate-200 text-slate-600 hover:bg-slate-50"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={isAdjusting || adjustDelta === 0 || !adjustReason.trim()}
                  className="px-5 py-2 rounded-xl bg-leaf text-white font-semibold hover:bg-leaf-2 shadow-xs disabled:opacity-50"
                >
                  {isAdjusting ? 'Đang lưu...' : 'Lưu điều chỉnh'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Movements Modal */}
      {movementTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-xs">
          <div className="bg-white rounded-2xl max-w-2xl w-full p-6 shadow-2xl border border-slate-200 space-y-4 max-h-[85vh] flex flex-col">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <div>
                <h2 className="text-base font-bold text-slate-900 font-display">
                  Lịch sử biến động: {movementTarget.sku}
                </h2>
                <p className="text-xs text-slate-400">{movementTarget.productName}</p>
              </div>
              <button
                type="button"
                onClick={() => setMovementTarget(null)}
                className="text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" strokeWidth={1.5} />
              </button>
            </div>

            <div className="flex-1 overflow-y-auto">
              {isLoadingMovements ? (
                <div className="p-8 text-center text-xs text-slate-400">Đang tải lịch sử...</div>
              ) : movements.length === 0 ? (
                <div className="p-8 text-center text-xs text-slate-400">Chưa có phát sinh biến động kho nào</div>
              ) : (
                <table className="w-full text-left text-xs text-slate-600">
                  <thead className="bg-slate-50 font-semibold text-slate-500 border-b border-slate-200">
                    <tr>
                      <th className="px-3 py-2">Thời gian</th>
                      <th className="px-3 py-2">Loại giao dịch</th>
                      <th className="px-3 py-2 text-right">Số lượng</th>
                      <th className="px-3 py-2">Lý do / Diễn giải</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {movements.map(m => (
                      <tr key={m.id} className="hover:bg-slate-50">
                        <td className="px-3 py-2.5 font-mono text-slate-400 whitespace-nowrap">
                          {formatDateTime(m.createdAt)}
                        </td>
                        <td className="px-3 py-2.5 font-medium text-slate-800">{m.type}</td>
                        <td className={`px-3 py-2.5 text-right font-mono font-bold ${m.quantity > 0 ? 'text-leaf' : 'text-rose-600'}`}>
                          {m.quantity > 0 ? `+${m.quantity}` : m.quantity}
                        </td>
                        <td className="px-3 py-2.5 text-slate-500 italic">{m.reason ?? '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
