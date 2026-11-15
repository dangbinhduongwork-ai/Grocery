import React, { useState, useEffect, useCallback } from 'react'
import {
  CreditCard,
  CheckCircle2,
  AlertTriangle,
  Check,
  RefreshCw,
  ChevronLeft,
  ChevronRight,
  ShieldAlert,
  X,
} from 'lucide-react'
import { api } from '@/shared/api'
import { formatVnd } from '@/shared/utils/formatVnd'
import { formatDateTime } from '@/shared/utils/dateTime'
import { Button } from '@/shared/ui/Button'
import { Badge } from '@/shared/ui/Badge'
import { Input } from '@/shared/ui/Input'
import { Card, CardContent } from '@/shared/ui/Card'
import { Skeleton } from '@/shared/ui/Skeleton'

interface PaymentTransactionItem {
  id: string
  webhookEventId: string
  paymentAttemptId?: string | null
  orderId?: string | null
  orderNumber?: string | null
  direction: string
  amount: number
  accountNumber?: string | null
  gateway?: string | null
  referenceCode?: string | null
  content?: string | null
  transactionTime: string
  matchStatus: string
  reviewStatus: string
  resolutionNote?: string | null
  resolvedBy?: string | null
  resolvedAt?: string | null
}

interface TransactionsResponse {
  items: PaymentTransactionItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export const AdminPaymentsPage: React.FC = () => {
  const [transactions, setTransactions] = useState<PaymentTransactionItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reviewFilter, setReviewFilter] = useState<string>('all')
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)

  // Resolution modal state
  const [selectedTx, setSelectedTx] = useState<PaymentTransactionItem | null>(null)
  const [actionType, setActionType] = useState<'AttachToOrder' | 'Refund' | 'Dismiss'>('AttachToOrder')
  const [targetOrderId, setTargetOrderId] = useState('')
  const [resolutionNote, setResolutionNote] = useState('')
  const [submittingResolution, setSubmittingResolution] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  const fetchTransactions = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const reviewParam = reviewFilter === 'all' ? undefined : reviewFilter
      const response = await api.GET('/api/v1/admin/payment-transactions', {
        params: {
          query: {
            reviewStatus: reviewParam,
            page,
            pageSize: 20,
          },
        },
      })

      if (response.data) {
        const data = response.data as unknown as TransactionsResponse
        setTransactions(data.items || [])
        setTotalPages(data.totalPages || 1)
        setTotalCount(data.totalCount || 0)
      } else {
        setError('Không thể tải danh sách giao dịch SePay.')
      }
    } catch {
      setError('Lỗi kết nối khi tải danh sách giao dịch.')
    } finally {
      setLoading(false)
    }
  }, [reviewFilter, page])

  useEffect(() => {
    void fetchTransactions()
  }, [fetchTransactions])

  const openResolutionModal = (tx: PaymentTransactionItem) => {
    setSelectedTx(tx)
    setTargetOrderId(tx.orderId || '')
    setResolutionNote('')
    setActionType(tx.matchStatus === 'Underpaid' || tx.matchStatus === 'Overpaid' ? 'Refund' : 'AttachToOrder')
    setActionError(null)
  }

  const handleResolve = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedTx) return

    setSubmittingResolution(true)
    setActionError(null)
    try {
      const response = await api.POST('/api/v1/admin/payment-transactions/{id}/resolve', {
        params: { path: { id: selectedTx.id } },
        body: {
          action: actionType,
          orderId: actionType === 'AttachToOrder' && targetOrderId.trim() ? targetOrderId.trim() : null,
          note: resolutionNote.trim(),
        },
      })

      if (!response.data) {
        const errorData = (response as { error?: { detail?: string; title?: string } }).error
        const errDetail = errorData?.detail || 'Lỗi khi xử lý giao dịch.'
        setActionError(errDetail)
        return
      }

      setSelectedTx(null)
      await fetchTransactions()
    } catch {
      setActionError('Lỗi kết nối khi thực hiện hành động.')
    } finally {
      setSubmittingResolution(false)
    }
  }

  const getMatchBadge = (status: string) => {
    switch (status) {
      case 'Matched':
        return <Badge variant="ok" className="text-[10px]">Đã khớp (Matched)</Badge>
      case 'Underpaid':
        return <Badge variant="warn" className="text-[10px] bg-amber-500/10 text-amber-700">Thiếu tiền (Underpaid)</Badge>
      case 'Overpaid':
        return <Badge variant="warn" className="text-[10px] bg-purple-500/10 text-purple-700">Thừa tiền (Overpaid)</Badge>
      case 'UnknownCode':
        return <Badge variant="destructive" className="text-[10px]">Sai mã GH (UnknownCode)</Badge>
      case 'WrongAccount':
        return <Badge variant="destructive" className="text-[10px]">Sai tài khoản (WrongAccount)</Badge>
      case 'LateAfterExpiry':
        return <Badge variant="warn" className="text-[10px] bg-amber-500/10 text-amber-700">Chuyển muộn (LateAfterExpiry)</Badge>
      case 'DuplicatePayment':
        return <Badge variant="destructive" className="text-[10px]">Trùng lặp (Duplicate)</Badge>
      default:
        return <Badge variant="secondary" className="text-[10px]">{status}</Badge>
    }
  }

  const getReviewBadge = (status: string) => {
    switch (status) {
      case 'Matched':
        return <span className="inline-flex items-center gap-1 text-leaf font-semibold text-xs"><CheckCircle2 className="w-3.5 h-3.5" /> Khớp chuẩn</span>
      case 'NeedsReview':
        return <span className="inline-flex items-center gap-1 text-tomato font-bold text-xs"><AlertTriangle className="w-3.5 h-3.5" /> Cần đối soát</span>
      case 'Resolved':
        return <span className="inline-flex items-center gap-1 text-slate-500 font-semibold text-xs"><Check className="w-3.5 h-3.5" /> Đã xử lý</span>
      default:
        return <span className="text-xs text-ink-2">{status}</span>
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-ink flex items-center gap-2">
            <CreditCard className="w-6 h-6 text-leaf stroke-[1.75]" />
            Giao dịch & Đối soát SePay VietQR
          </h1>
          <p className="text-xs text-ink-2 mt-0.5">
            Quản lý và giải quyết các giao dịch webhook chuyển khoản ngân hàng tự động (Tổng số: {totalCount} giao dịch)
          </p>
        </div>

        <Button
          variant="outline"
          size="sm"
          onClick={() => void fetchTransactions()}
          disabled={loading}
          className="text-xs"
        >
          <RefreshCw className={`w-3.5 h-3.5 mr-1.5 ${loading ? 'animate-spin' : ''}`} /> Làm mới
        </Button>
      </div>

      {/* Filter Tabs */}
      <div className="flex border-b border-rule gap-1 text-xs font-semibold">
        <button
          type="button"
          onClick={() => { setReviewFilter('all'); setPage(1) }}
          className={`px-4 py-2.5 border-b-2 transition-colors ${
            reviewFilter === 'all'
              ? 'border-leaf text-leaf'
              : 'border-transparent text-ink-2 hover:text-ink'
          }`}
        >
          Tất cả giao dịch
        </button>
        <button
          type="button"
          onClick={() => { setReviewFilter('NeedsReview'); setPage(1) }}
          className={`px-4 py-2.5 border-b-2 transition-colors flex items-center gap-1.5 ${
            reviewFilter === 'NeedsReview'
              ? 'border-tomato text-tomato'
              : 'border-transparent text-ink-2 hover:text-ink'
          }`}
        >
          <AlertTriangle className="w-3.5 h-3.5" /> Cần đối soát (NeedsReview)
        </button>
        <button
          type="button"
          onClick={() => { setReviewFilter('Matched'); setPage(1) }}
          className={`px-4 py-2.5 border-b-2 transition-colors ${
            reviewFilter === 'Matched'
              ? 'border-leaf text-leaf'
              : 'border-transparent text-ink-2 hover:text-ink'
          }`}
        >
          Đã khớp chuẩn (Matched)
        </button>
        <button
          type="button"
          onClick={() => { setReviewFilter('Resolved'); setPage(1) }}
          className={`px-4 py-2.5 border-b-2 transition-colors ${
            reviewFilter === 'Resolved'
              ? 'border-leaf text-leaf'
              : 'border-transparent text-ink-2 hover:text-ink'
          }`}
        >
          Đã xử lý (Resolved)
        </button>
      </div>

      {/* Transactions Table */}
      <Card className="border-rule shadow-sm overflow-hidden">
        <CardContent className="p-0">
          {loading ? (
            <div className="p-6 space-y-3">
              {[...Array(5)].map((_, i) => (
                <Skeleton key={i} className="h-12 w-full rounded-lg" />
              ))}
            </div>
          ) : error ? (
            <div className="p-8 text-center text-tomato text-xs">
              <AlertTriangle className="w-8 h-8 mx-auto mb-2 opacity-80" />
              {error}
            </div>
          ) : transactions.length === 0 ? (
            <div className="p-12 text-center text-ink-2 text-xs">
              Không có giao dịch nào phù hợp với bộ lọc hiện tại.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs text-ink border-collapse">
                <thead className="bg-paper-2 text-ink-2 border-b border-rule font-semibold">
                  <tr>
                    <th className="py-3 px-4">Thời gian</th>
                    <th className="py-3 px-4">Số tiền</th>
                    <th className="py-3 px-4">Số TK / Ngân hàng</th>
                    <th className="py-3 px-4">Nội dung CK</th>
                    <th className="py-3 px-4">Khớp giao dịch</th>
                    <th className="py-3 px-4">Trạng thái</th>
                    <th className="py-3 px-4 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-rule/60">
                  {transactions.map((tx) => (
                    <tr key={tx.id} className="hover:bg-paper-2/50 transition-colors">
                      <td className="py-3 px-4 whitespace-nowrap">
                        <div className="font-mono text-ink text-[11px]">{formatDateTime(tx.transactionTime)}</div>
                        <div className="text-[10px] text-ink-2 font-mono">Ref: {tx.referenceCode || 'N/A'}</div>
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap">
                        <span className="font-bold font-mono text-leaf text-sm">
                          {formatVnd(tx.amount)}
                        </span>
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap">
                        <div className="font-mono font-bold text-ink">{tx.accountNumber || '—'}</div>
                        <div className="text-[10px] text-ink-2">{tx.gateway || 'SePay'}</div>
                      </td>
                      <td className="py-3 px-4 max-w-xs">
                        <div className="font-mono text-[11px] truncate text-ink font-semibold" title={tx.content || ''}>
                          {tx.content || '—'}
                        </div>
                        {tx.orderNumber && (
                          <div className="text-[10px] text-ink-2 flex items-center gap-1 mt-0.5">
                            Đơn: <span className="font-mono font-bold text-ink">{tx.orderNumber}</span>
                          </div>
                        )}
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap">
                        {getMatchBadge(tx.matchStatus)}
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap">
                        {getReviewBadge(tx.reviewStatus)}
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap text-right">
                        {tx.reviewStatus === 'NeedsReview' ? (
                          <Button
                            size="sm"
                            onClick={() => openResolutionModal(tx)}
                            className="h-7 text-xs bg-tomato hover:bg-tomato/90 text-white font-semibold shadow-xs"
                          >
                            Xử lý đối soát
                          </Button>
                        ) : tx.resolutionNote ? (
                          <span className="text-[10px] text-ink-2 italic" title={tx.resolutionNote}>
                            Ghi chú: {tx.resolutionNote.length > 20 ? `${tx.resolutionNote.substring(0, 20)}...` : tx.resolutionNote}
                          </span>
                        ) : (
                          <span className="text-ink-2 text-[10px]">—</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between p-3 border-t border-rule bg-paper-2 text-xs">
              <span className="text-ink-2">
                Trang {page} / {totalPages} (Tổng số {totalCount})
              </span>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((p) => Math.max(p - 1, 1))}
                  disabled={page <= 1}
                  className="h-8"
                >
                  <ChevronLeft className="w-3.5 h-3.5 mr-1" /> Trước
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((p) => Math.min(p + 1, totalPages))}
                  disabled={page >= totalPages}
                  className="h-8"
                >
                  Sau <ChevronRight className="w-3.5 h-3.5 ml-1" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Resolution Modal */}
      {selectedTx && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-ink/40 backdrop-blur-xs p-4 animate-fade-in">
          <div className="bg-paper border border-rule rounded-2xl max-w-lg w-full shadow-2xl overflow-hidden">
            <div className="p-4 bg-paper-2 border-b border-rule flex items-center justify-between">
              <h3 className="font-bold text-sm text-ink flex items-center gap-2">
                <ShieldAlert className="w-4 h-4 text-tomato" />
                Xử lý đối soát giao dịch bất thường
              </h3>
              <button
                type="button"
                onClick={() => setSelectedTx(null)}
                className="text-ink-2 hover:text-ink text-sm font-bold p-1 rounded hover:bg-paper-2"
                aria-label="Đóng"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <form onSubmit={handleResolve} className="p-5 space-y-4 text-xs">
              {/* Transaction Summary */}
              <div className="p-3 bg-paper-2 rounded-xl border border-rule space-y-1.5 font-mono text-[11px]">
                <div className="flex justify-between">
                  <span className="text-ink-2 font-sans">Số tiền:</span>
                  <span className="font-bold text-leaf">{formatVnd(selectedTx.amount)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-ink-2 font-sans">Nội dung CK:</span>
                  <span className="text-ink font-bold">{selectedTx.content || '—'}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-ink-2 font-sans">Vấn đề phát hiện:</span>
                  <span>{getMatchBadge(selectedTx.matchStatus)}</span>
                </div>
              </div>

              {actionError && (
                <div className="p-2.5 bg-tomato/10 border border-tomato/20 rounded-lg text-tomato text-xs flex items-center gap-2">
                  <AlertTriangle className="w-4 h-4 shrink-0" />
                  {actionError}
                </div>
              )}

              {/* Action Selection */}
              <div>
                <label className="font-bold text-ink block mb-1.5">Chọn hành động xử lý:</label>
                <div className="grid grid-cols-3 gap-2">
                  <button
                    type="button"
                    onClick={() => setActionType('AttachToOrder')}
                    className={`p-2.5 rounded-xl border text-center transition-all ${
                      actionType === 'AttachToOrder'
                        ? 'border-leaf bg-leaf/5 text-leaf font-bold ring-2 ring-leaf/20'
                        : 'border-rule bg-paper-2 text-ink-2 hover:text-ink'
                    }`}
                  >
                    Gán vào đơn hàng
                  </button>
                  <button
                    type="button"
                    onClick={() => setActionType('Refund')}
                    className={`p-2.5 rounded-xl border text-center transition-all ${
                      actionType === 'Refund'
                        ? 'border-tomato bg-tomato/5 text-tomato font-bold ring-2 ring-tomato/20'
                        : 'border-rule bg-paper-2 text-ink-2 hover:text-ink'
                    }`}
                  >
                    Yêu cầu hoàn tiền
                  </button>
                  <button
                    type="button"
                    onClick={() => setActionType('Dismiss')}
                    className={`p-2.5 rounded-xl border text-center transition-all ${
                      actionType === 'Dismiss'
                        ? 'border-slate-600 bg-slate-100 text-slate-800 font-bold ring-2 ring-slate-300'
                        : 'border-rule bg-paper-2 text-ink-2 hover:text-ink'
                    }`}
                  >
                    Bỏ qua giao dịch
                  </button>
                </div>
              </div>

              {/* Order ID Input if AttachToOrder */}
              {actionType === 'AttachToOrder' && (
                <div>
                  <label className="font-bold text-ink block mb-1">Mã đơn hàng (Order ID / GUID):</label>
                  <Input
                    value={targetOrderId}
                    onChange={(e) => setTargetOrderId(e.target.value)}
                    placeholder="Nhập GUID đơn hàng cần gán thanh toán..."
                    className="font-mono text-xs"
                    required
                  />
                  <p className="text-[10px] text-ink-2 mt-1">
                    Nếu đơn hàng đã bị hủy do chuyển tiền muộn, hệ thống sẽ tự động khôi phục và xác nhận lại đơn nếu còn đủ tồn kho.
                  </p>
                </div>
              )}

              {/* Note */}
              <div>
                <label className="font-bold text-ink block mb-1">Ghi chú quyết định đối soát:</label>
                <textarea
                  value={resolutionNote}
                  onChange={(e) => setResolutionNote(e.target.value)}
                  placeholder="Ghi rõ lý do xử lý để lưu nhật ký kiểm toán..."
                  rows={3}
                  className="w-full rounded-xl border border-rule bg-paper p-2.5 text-xs text-ink focus:outline-hidden focus:ring-2 focus:ring-leaf/20 resize-none"
                  required
                />
              </div>

              {/* Modal Buttons */}
              <div className="flex justify-end gap-2 pt-2 border-t border-rule">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setSelectedTx(null)}
                  disabled={submittingResolution}
                  className="text-xs"
                >
                  Hủy bỏ
                </Button>
                <Button
                  type="submit"
                  disabled={submittingResolution}
                  className={`text-xs text-white ${
                    actionType === 'AttachToOrder'
                      ? 'bg-leaf hover:bg-leaf/90'
                      : actionType === 'Refund'
                      ? 'bg-tomato hover:bg-tomato/90'
                      : 'bg-slate-700 hover:bg-slate-800'
                  }`}
                >
                  {submittingResolution ? 'Đang xử lý...' : 'Xác nhận xử lý'}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
