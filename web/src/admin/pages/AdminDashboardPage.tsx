import React, { useEffect, useState } from 'react'
import {
  TrendingUp,
  Clock,
  AlertTriangle,
  ArrowRight,
  RefreshCw,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { formatVnd } from '@/shared/utils/formatVnd'
import { useRouter } from '@/app/router'

interface DashboardData {
  todayRevenue: number
  last7DaysRevenue: number
  last30DaysRevenue: number
  ordersByStatus: Record<string, number>
  lowStockCount: number
  pendingReviewsCount: number
  paymentAnomaliesCount: number
  topProducts: Array<{
    productId: string
    sku: string
    productName: string
    categoryName: string
    unitsSold: number
    netRevenue: number
  }>
}

export const AdminDashboardPage: React.FC = () => {
  const router = useRouter()
  const [data, setData] = useState<DashboardData | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)

  const fetchDashboard = async () => {
    try {
      const res = await api.GET('/api/v1/admin/reports/dashboard', {})
      if (res.data) {
        setData(res.data as unknown as DashboardData)
      }
    } catch {
      // Handled silently
    } finally {
      setIsLoading(false)
      setIsRefreshing(false)
    }
  }

  useEffect(() => {
    void fetchDashboard()
  }, [])

  const handleRefresh = () => {
    setIsRefreshing(true)
    void fetchDashboard()
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-8 bg-slate-200 rounded-lg w-48 animate-pulse" />
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-28 bg-slate-200 rounded-2xl animate-pulse" />
          ))}
        </div>
      </div>
    )
  }

  const kpis = data ?? {
    todayRevenue: 0,
    last7DaysRevenue: 0,
    last30DaysRevenue: 0,
    ordersByStatus: {},
    lowStockCount: 0,
    pendingReviewsCount: 0,
    paymentAnomaliesCount: 0,
    topProducts: [],
  }

  return (
    <div className="space-y-6">
      {/* Page Title & Actions */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 font-display">Tổng quan kinh doanh</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Dữ liệu doanh thu & chỉ số vận hành cửa hàng theo múi giờ Việt Nam (Asia/Ho_Chi_Minh).
          </p>
        </div>
        <button
          type="button"
          onClick={handleRefresh}
          disabled={isRefreshing}
          className="inline-flex items-center gap-2 px-3 py-1.5 rounded-xl border border-slate-200 text-xs font-medium text-slate-700 bg-white hover:bg-slate-50 transition-colors shadow-2xs"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${isRefreshing ? 'animate-spin' : ''}`} strokeWidth={1.5} />
          Làm mới
        </button>
      </div>

      {/* KPI Band (Single bordered high-density strip per spec) */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs">
          <div className="flex items-center justify-between text-slate-400 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">Doanh thu hôm nay</span>
            <div className="w-8 h-8 rounded-lg bg-emerald-50 text-emerald-600 flex items-center justify-center">
              <TrendingUp className="w-4 h-4" strokeWidth={1.5} />
            </div>
          </div>
          <div className="text-2xl font-extrabold text-slate-900 font-display">
            {formatVnd(kpis.todayRevenue)}
          </div>
          <span className="text-[11px] text-emerald-600 font-medium">Đơn hàng giao thành công</span>
        </div>

        <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs">
          <div className="flex items-center justify-between text-slate-400 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">Doanh thu 7 ngày</span>
            <div className="w-8 h-8 rounded-lg bg-blue-50 text-blue-600 flex items-center justify-center">
              <TrendingUp className="w-4 h-4" strokeWidth={1.5} />
            </div>
          </div>
          <div className="text-2xl font-extrabold text-slate-900 font-display">
            {formatVnd(kpis.last7DaysRevenue)}
          </div>
          <span className="text-[11px] text-slate-400 font-medium">7 ngày gần nhất</span>
        </div>

        <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs">
          <div className="flex items-center justify-between text-slate-400 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">Cần lấy hàng / xác nhận</span>
            <div className="w-8 h-8 rounded-lg bg-purple-50 text-purple-600 flex items-center justify-center">
              <Clock className="w-4 h-4" strokeWidth={1.5} />
            </div>
          </div>
          <div className="text-2xl font-extrabold text-slate-900 font-display">
            {(kpis.ordersByStatus['Placed'] ?? 0) + (kpis.ordersByStatus['Confirmed'] ?? 0)}
          </div>
          <button
            type="button"
            onClick={() => router.push('/admin/orders')}
            className="text-[11px] text-leaf hover:underline font-medium inline-flex items-center gap-1 mt-0.5"
          >
            Xem danh sách đơn <ArrowRight className="w-3 h-3" strokeWidth={1.5} />
          </button>
        </div>

        <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs">
          <div className="flex items-center justify-between text-slate-400 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">Cảnh báo tồn kho & duyệt</span>
            <div className="w-8 h-8 rounded-lg bg-amber-50 text-amber-600 flex items-center justify-center">
              <AlertTriangle className="w-4 h-4" strokeWidth={1.5} />
            </div>
          </div>
          <div className="flex items-center gap-3">
            <div>
              <span className="text-2xl font-extrabold text-amber-600 font-display">{kpis.lowStockCount}</span>
              <span className="text-xs text-slate-400 ml-1">kho thấp</span>
            </div>
            <div className="h-6 w-px bg-slate-200" />
            <div>
              <span className="text-2xl font-extrabold text-indigo-600 font-display">{kpis.pendingReviewsCount}</span>
              <span className="text-xs text-slate-400 ml-1">chờ duyệt</span>
            </div>
          </div>
          <div className="flex items-center gap-3 text-[11px] mt-1">
            <button
              type="button"
              onClick={() => router.push('/admin/inventory')}
              className="text-leaf hover:underline font-medium"
            >
              Kiểm kho
            </button>
            <span>&bull;</span>
            <button
              type="button"
              onClick={() => router.push('/admin/reviews')}
              className="text-leaf hover:underline font-medium"
            >
              Duyệt đánh giá
            </button>
          </div>
        </div>
      </div>

      {/* Orders Breakdown by Status */}
      <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs">
        <h2 className="text-sm font-bold text-slate-900 uppercase tracking-wider mb-4">
          Phân bố đơn hàng theo trạng thái
        </h2>
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-8 gap-3">
          {[
            { key: 'Placed', label: 'Mới đặt' },
            { key: 'Confirmed', label: 'Đã xác nhận' },
            { key: 'Preparing', label: 'Đang chuẩn bị' },
            { key: 'Packed', label: 'Đã đóng gói' },
            { key: 'Shipping', label: 'Đang giao' },
            { key: 'Delivered', label: 'Đã giao' },
            { key: 'Cancelled', label: 'Đã hủy' },
            { key: 'DeliveryFailed', label: 'Thất bại' },
          ].map(status => {
            const count = kpis.ordersByStatus[status.key] ?? 0
            return (
              <div
                key={status.key}
                onClick={() => router.push(`/admin/orders?status=${status.key}`)}
                className="p-3 rounded-xl border border-slate-100 bg-slate-50/60 hover:bg-slate-100/80 transition-colors cursor-pointer text-center"
              >
                <div className="text-xs text-slate-500 font-medium mb-1">{status.label}</div>
                <div className="text-lg font-bold text-slate-800 font-display">{count}</div>
              </div>
            )
          })}
        </div>
      </div>

      {/* Top Selling Products */}
      <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-2xs">
        <div className="p-5 border-b border-slate-100 flex items-center justify-between">
          <div>
            <h2 className="text-sm font-bold text-slate-900 uppercase tracking-wider">
              Sản phẩm bán chạy nhất (30 ngày)
            </h2>
            <p className="text-xs text-slate-400 mt-0.5">Xếp hạng theo số lượng đơn giao thành công</p>
          </div>
          <button
            type="button"
            onClick={() => router.push('/admin/reports/revenue')}
            className="text-xs text-leaf hover:underline font-medium inline-flex items-center gap-1"
          >
            Xem báo cáo chi tiết <ArrowRight className="w-3.5 h-3.5" strokeWidth={1.5} />
          </button>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-slate-600">
            <thead className="bg-slate-50 text-xs uppercase font-semibold text-slate-500 border-b border-slate-200">
              <tr>
                <th className="px-5 py-3">Mã SKU</th>
                <th className="px-5 py-3">Tên sản phẩm</th>
                <th className="px-5 py-3">Danh mục</th>
                <th className="px-5 py-3 text-right">Đã bán</th>
                <th className="px-5 py-3 text-right">Doanh thu thuần</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {kpis.topProducts.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-5 py-8 text-center text-xs text-slate-400">
                    Chưa có dữ liệu bán hàng trong 30 ngày qua
                  </td>
                </tr>
              ) : (
                kpis.topProducts.map(p => (
                  <tr key={p.productId} className="hover:bg-slate-50/80 transition-colors">
                    <td className="px-5 py-3.5 font-mono text-xs text-slate-500">{p.sku}</td>
                    <td className="px-5 py-3.5 font-medium text-slate-800">{p.productName}</td>
                    <td className="px-5 py-3.5 text-xs text-slate-500">{p.categoryName}</td>
                    <td className="px-5 py-3.5 text-right font-semibold text-slate-800">{p.unitsSold}</td>
                    <td className="px-5 py-3.5 text-right font-semibold text-emerald-700">
                      {formatVnd(p.netRevenue)}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
