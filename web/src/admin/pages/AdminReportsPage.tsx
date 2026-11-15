import React, { useEffect, useState, useCallback } from 'react'
import {
  Download,
  Calendar,
  TrendingUp,
  DollarSign,
  Boxes,
  RefreshCw,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { formatVnd } from '@/shared/utils/formatVnd'
import { useAuth } from '@/shared/auth'

interface RevenueReport {
  granularity: string
  totalNetRevenue: number
  shippingFeeTotal: number
  refundsTotal: number
  points: Array<{
    date: string
    netRevenue: number
    ordersCount: number
  }>
}

interface ProfitReport {
  granularity: string
  totalNetRevenue: number
  totalCogsSnapshot: number
  grossProfit: number
  grossMarginPercent: number
  series: Array<{
    date: string
    netRevenue: number
    cogs: number
    grossProfit: number
    marginPercent: number
  }>
}

interface StockValuationReport {
  totalOnHandUnits: number
  totalInventoryValue: number
  items: Array<{
    productId: string
    sku: string
    name: string
    categoryName: string
    onHand: number
    costPrice: number
    totalValue: number
  }>
}

export const AdminReportsPage: React.FC<{ defaultTab?: 'revenue' | 'profit' }> = ({
  defaultTab = 'revenue',
}) => {
  const { hasPermission } = useAuth()
  const canSeeProfit = hasPermission('reports.profit')
  const [activeTab, setActiveTab] = useState<'revenue' | 'profit' | 'valuation'>(
    defaultTab === 'profit' && canSeeProfit ? 'profit' : 'revenue'
  )

  const [from, setFrom] = useState(
    new Date(Date.now() - 30 * 86400000).toISOString().slice(0, 10)
  )
  const [to, setTo] = useState(new Date().toISOString().slice(0, 10))
  const [granularity, setGranularity] = useState<'day' | 'month'>('day')

  const [revenueData, setRevenueData] = useState<RevenueReport | null>(null)
  const [profitData, setProfitData] = useState<ProfitReport | null>(null)
  const [valuationData, setValuationData] = useState<StockValuationReport | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const fetchReports = useCallback(async () => {
    setIsLoading(true)
    try {
      if (activeTab === 'revenue') {
        const res = await api.GET('/api/v1/admin/reports/revenue', {
          params: { query: { from, to, granularity } },
        })
        if (res.data) setRevenueData(res.data as unknown as RevenueReport)
      } else if (activeTab === 'profit' && canSeeProfit) {
        const res = await api.GET('/api/v1/admin/reports/profit', {
          params: { query: { from, to, granularity } },
        })
        if (res.data) setProfitData(res.data as unknown as ProfitReport)
      } else if (activeTab === 'valuation' && canSeeProfit) {
        const res = await api.GET('/api/v1/admin/reports/stock-valuation', {})
        if (res.data) setValuationData(res.data as unknown as StockValuationReport)
      }
    } catch {
      // Silently
    } finally {
      setIsLoading(false)
    }
  }, [activeTab, from, to, granularity, canSeeProfit])

  useEffect(() => {
    void fetchReports()
  }, [fetchReports])

  const handleExportCsv = () => {
    let endpoint = ''
    if (activeTab === 'revenue') {
      endpoint = `/api/v1/admin/reports/revenue?from=${from}&to=${to}&granularity=${granularity}&format=csv`
    } else if (activeTab === 'profit') {
      endpoint = `/api/v1/admin/reports/profit?from=${from}&to=${to}&granularity=${granularity}&format=csv`
    } else {
      endpoint = `/api/v1/admin/reports/stock-valuation?format=csv`
    }

    // Trigger authenticated or direct download
    window.open(endpoint, '_blank')
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 font-display">Báo cáo tài chính & Tồn kho</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Phân tích số liệu kinh doanh, giá vốn snapshot (BR-13) và xuất báo cáo CSV an toàn.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={handleExportCsv}
            className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl bg-slate-800 text-white text-xs font-semibold hover:bg-slate-900 transition-colors shadow-xs"
          >
            <Download className="w-4 h-4" strokeWidth={1.5} />
            Xuất file CSV
          </button>
          <button
            type="button"
            onClick={() => void fetchReports()}
            disabled={isLoading}
            className="p-2 rounded-xl border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 transition-colors shadow-2xs"
            title="Làm mới"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} strokeWidth={1.5} />
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex items-center gap-2 border-b border-slate-200">
        <button
          type="button"
          onClick={() => setActiveTab('revenue')}
          className={`flex items-center gap-2 px-4 py-2.5 border-b-2 text-xs font-bold transition-colors ${
            activeTab === 'revenue'
              ? 'border-leaf text-leaf'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          <TrendingUp className="w-4 h-4" strokeWidth={1.5} />
          <span>Doanh thu thuần</span>
        </button>

        {canSeeProfit && (
          <>
            <button
              type="button"
              onClick={() => setActiveTab('profit')}
              className={`flex items-center gap-2 px-4 py-2.5 border-b-2 text-xs font-bold transition-colors ${
                activeTab === 'profit'
                  ? 'border-leaf text-leaf'
                  : 'border-transparent text-slate-500 hover:text-slate-800'
              }`}
            >
              <DollarSign className="w-4 h-4" strokeWidth={1.5} />
              <span>Lợi nhuận gộp & COGS</span>
            </button>
            <button
              type="button"
              onClick={() => setActiveTab('valuation')}
              className={`flex items-center gap-2 px-4 py-2.5 border-b-2 text-xs font-bold transition-colors ${
                activeTab === 'valuation'
                  ? 'border-leaf text-leaf'
                  : 'border-transparent text-slate-500 hover:text-slate-800'
              }`}
            >
              <Boxes className="w-4 h-4" strokeWidth={1.5} />
              <span>Định giá hàng tồn kho</span>
            </button>
          </>
        )}
      </div>

      {/* Filters Bar (Only for revenue & profit) */}
      {activeTab !== 'valuation' && (
        <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-2xs flex flex-wrap items-center justify-between gap-4 text-xs">
          <div className="flex items-center gap-3">
            <div className="flex items-center gap-2">
              <Calendar className="w-4 h-4 text-slate-400" strokeWidth={1.5} />
              <span className="font-semibold text-slate-700">Từ ngày:</span>
              <input
                type="date"
                value={from}
                onChange={e => setFrom(e.target.value)}
                className="px-2.5 py-1.5 border border-slate-200 rounded-lg bg-slate-50 focus:bg-white"
              />
            </div>
            <div className="flex items-center gap-2">
              <span className="font-semibold text-slate-700">Đến ngày:</span>
              <input
                type="date"
                value={to}
                onChange={e => setTo(e.target.value)}
                className="px-2.5 py-1.5 border border-slate-200 rounded-lg bg-slate-50 focus:bg-white"
              />
            </div>
          </div>

          <div className="flex items-center gap-2">
            <span className="font-semibold text-slate-700">Chu kỳ:</span>
            <select
              value={granularity}
              onChange={e => setGranularity(e.target.value as 'day' | 'month')}
              className="px-3 py-1.5 border border-slate-200 rounded-lg bg-slate-50 focus:bg-white font-medium"
            >
              <option value="day">Theo ngày</option>
              <option value="month">Theo tháng</option>
            </select>
          </div>
        </div>
      )}

      {/* Content */}
      {isLoading ? (
        <div className="h-64 bg-white rounded-2xl animate-pulse" />
      ) : activeTab === 'revenue' && revenueData ? (
        <div className="space-y-6">
          {/* Summary Strip */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-2xs">
              <div className="text-xs font-semibold text-slate-500 uppercase">Doanh thu thuần</div>
              <div className="text-2xl font-bold text-leaf font-display mt-1">
                {formatVnd(revenueData.totalNetRevenue)}
              </div>
              <span className="text-[11px] text-slate-400">Đã trừ coupon & hủy</span>
            </div>
            <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-2xs">
              <div className="text-xs font-semibold text-slate-500 uppercase">Phí vận chuyển thu</div>
              <div className="text-2xl font-bold text-slate-900 font-display mt-1">
                {formatVnd(revenueData.shippingFeeTotal)}
              </div>
              <span className="text-[11px] text-slate-400">Tách riêng doanh thu hàng hóa</span>
            </div>
            <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-2xs">
              <div className="text-xs font-semibold text-slate-500 uppercase">Tổng hoàn tiền</div>
              <div className="text-2xl font-bold text-slate-900 font-display mt-1">
                {formatVnd(revenueData.refundsTotal)}
              </div>
              <span className="text-[11px] text-slate-400">Đơn đã hoàn tiền</span>
            </div>
          </div>

          {/* Revenue Table */}
          <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-2xs">
            <table className="w-full text-left text-sm text-slate-600">
              <thead className="bg-slate-50 text-xs uppercase font-semibold text-slate-500 border-b border-slate-200">
                <tr>
                  <th className="px-5 py-3">Mốc thời gian</th>
                  <th className="px-5 py-3 text-right">Số đơn giao thành công</th>
                  <th className="px-5 py-3 text-right">Doanh thu thuần (VND)</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {revenueData.points.length === 0 ? (
                  <tr>
                    <td colSpan={3} className="px-5 py-8 text-center text-xs text-slate-400">
                      Không có phát sinh doanh thu trong khoảng thời gian đã chọn
                    </td>
                  </tr>
                ) : (
                  revenueData.points.map(p => (
                    <tr key={p.date} className="hover:bg-slate-50">
                      <td className="px-5 py-3.5 font-mono text-xs font-semibold text-slate-700">{p.date}</td>
                      <td className="px-5 py-3.5 text-right font-medium text-slate-800">{p.ordersCount}</td>
                      <td className="px-5 py-3.5 text-right font-bold text-leaf font-display">
                        {formatVnd(p.netRevenue)}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      ) : activeTab === 'profit' && profitData ? (
        <div className="space-y-6">
          <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
            <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-2xs">
              <div className="text-xs font-semibold text-slate-500 uppercase">Doanh thu thuần</div>
              <div className="text-xl font-bold text-slate-900 font-display mt-1">
                {formatVnd(profitData.totalNetRevenue)}
              </div>
            </div>
            <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-2xs">
              <div className="text-xs font-semibold text-slate-500 uppercase">Giá vốn hàng bán (COGS)</div>
              <div className="text-xl font-bold text-amber-700 font-display mt-1">
                {formatVnd(profitData.totalCogsSnapshot)}
              </div>
              <span className="text-[11px] text-slate-400">Snapshot tại thời điểm xuất</span>
            </div>
            <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-2xs">
              <div className="text-xs font-semibold text-slate-500 uppercase">Lợi nhuận gộp</div>
              <div className="text-xl font-bold text-emerald-700 font-display mt-1">
                {formatVnd(profitData.grossProfit)}
              </div>
            </div>
            <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-2xs">
              <div className="text-xs font-semibold text-slate-500 uppercase">Tỷ suất lợi nhuận gộp</div>
              <div className="text-xl font-bold text-indigo-700 font-display mt-1">
                {profitData.grossMarginPercent}%
              </div>
            </div>
          </div>

          <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-2xs">
            <table className="w-full text-left text-sm text-slate-600">
              <thead className="bg-slate-50 text-xs uppercase font-semibold text-slate-500 border-b border-slate-200">
                <tr>
                  <th className="px-5 py-3">Mốc thời gian</th>
                  <th className="px-5 py-3 text-right">Doanh thu thuần</th>
                  <th className="px-5 py-3 text-right">Giá vốn (COGS)</th>
                  <th className="px-5 py-3 text-right">Lợi nhuận gộp</th>
                  <th className="px-5 py-3 text-right">Tỷ suất %</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {profitData.series.map(s => (
                  <tr key={s.date} className="hover:bg-slate-50">
                    <td className="px-5 py-3.5 font-mono text-xs font-semibold text-slate-700">{s.date}</td>
                    <td className="px-5 py-3.5 text-right font-medium text-slate-800">{formatVnd(s.netRevenue)}</td>
                    <td className="px-5 py-3.5 text-right font-mono text-xs text-amber-700">{formatVnd(s.cogs)}</td>
                    <td className="px-5 py-3.5 text-right font-bold text-emerald-700 font-display">
                      {formatVnd(s.grossProfit)}
                    </td>
                    <td className="px-5 py-3.5 text-right font-semibold text-indigo-700">{s.marginPercent}%</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ) : activeTab === 'valuation' && valuationData ? (
        <div className="space-y-6">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-2xs">
              <div className="text-xs font-semibold text-slate-500 uppercase">Tổng số lượng hàng trong kho</div>
              <div className="text-2xl font-bold text-slate-900 font-display mt-1">
                {valuationData.totalOnHandUnits.toLocaleString('vi-VN')} đơn vị
              </div>
            </div>
            <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-2xs">
              <div className="text-xs font-semibold text-slate-500 uppercase">Tổng giá trị tồn kho</div>
              <div className="text-2xl font-bold text-leaf font-display mt-1">
                {formatVnd(valuationData.totalInventoryValue)}
              </div>
              <span className="text-[11px] text-slate-400">Tính theo giá vốn bình quân di động</span>
            </div>
          </div>

          <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-2xs">
            <table className="w-full text-left text-sm text-slate-600">
              <thead className="bg-slate-50 text-xs uppercase font-semibold text-slate-500 border-b border-slate-200">
                <tr>
                  <th className="px-5 py-3">Mã SKU</th>
                  <th className="px-5 py-3">Tên sản phẩm</th>
                  <th className="px-5 py-3">Danh mục</th>
                  <th className="px-5 py-3 text-right">Tồn thực tế</th>
                  <th className="px-5 py-3 text-right">Giá vốn</th>
                  <th className="px-5 py-3 text-right">Tổng giá trị</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {valuationData.items.map(i => (
                  <tr key={i.productId} className="hover:bg-slate-50">
                    <td className="px-5 py-3 font-mono text-xs text-slate-500">{i.sku}</td>
                    <td className="px-5 py-3 font-medium text-slate-800">{i.name}</td>
                    <td className="px-5 py-3 text-xs text-slate-500">{i.categoryName}</td>
                    <td className="px-5 py-3 text-right font-semibold text-slate-800">{i.onHand}</td>
                    <td className="px-5 py-3 text-right font-mono text-xs text-amber-700">{formatVnd(i.costPrice)}</td>
                    <td className="px-5 py-3 text-right font-bold text-emerald-700 font-display">
                      {formatVnd(i.totalValue)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}
    </div>
  )
}
