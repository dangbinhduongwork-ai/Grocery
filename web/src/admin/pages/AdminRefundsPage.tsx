import React, { useState, useEffect, useCallback } from 'react'
import {
  RotateCcw,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  Clock,
  RefreshCw,
  Plus,
  Building2,
  ChevronLeft,
  ChevronRight,
  Info,
} from 'lucide-react'
import { api } from '@/shared/api'
import { useAuth } from '@/shared/auth'
import { formatVnd } from '@/shared/utils/formatVnd'
import { formatDateTime } from '@/shared/utils/dateTime'
import { Button } from '@/shared/ui/Button'
import { Badge } from '@/shared/ui/Badge'
import { Input } from '@/shared/ui/Input'
import { Card, CardContent } from '@/shared/ui/Card'
import { Skeleton } from '@/shared/ui/Skeleton'

interface RefundItem {
  id: string
  paymentId: string
  orderId: string
  orderNumber?: string | null
  amount: number
  reason: string
  status: string
  requestedBy: string
  requestedAt: string
  decidedBy?: string | null
  decidedAt?: string | null
  decisionNote?: string | null
  completedBy?: string | null
  completedAt?: string | null
  bankReference?: string | null
}

interface RefundsResponse {
  items: RefundItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export const AdminRefundsPage: React.FC = () => {
  const { user } = useAuth()
  const [refunds, setRefunds] = useState<RefundItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)

  // Create Refund Modal
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [createOrderId, setCreateOrderId] = useState('')
  const [createAmount, setCreateAmount] = useState<number>(0)
  const [createReason, setCreateReason] = useState('')
  const [creatingRefund, setCreatingRefund] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)

  // Action Modal State (Approve, Reject, Complete)
  const [actionModal, setActionModal] = useState<{
    type: 'approve' | 'reject' | 'complete'
    refund: RefundItem
  } | null>(null)
  const [actionNote, setActionNote] = useState('')
  const [bankRefCode, setBankRefCode] = useState('')
  const [submittingAction, setSubmittingAction] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  const fetchRefunds = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const statusParam = statusFilter === 'all' ? undefined : statusFilter
      const response = await api.GET('/api/v1/admin/refunds', {
        params: {
          query: {
            status: statusParam,
            page,
            pageSize: 20,
          },
        },
      })

      if (response.data) {
        const data = response.data as unknown as RefundsResponse
        setRefunds(data.items || [])
        setTotalPages(data.totalPages || 1)
        setTotalCount(data.totalCount || 0)
      } else {
        setError('Không thể tải danh sách yêu cầu hoàn tiền.')
      }
    } catch {
      setError('Lỗi kết nối khi tải danh sách hoàn tiền.')
    } finally {
      setLoading(false)
    }
  }, [statusFilter, page])

  useEffect(() => {
    void fetchRefunds()
  }, [fetchRefunds])

  const handleCreateRefund = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!createOrderId.trim() || createAmount <= 0 || !createReason.trim()) {
      setCreateError('Vui lòng nhập đầy đủ mã đơn hàng, số tiền dương và lý do hoàn tiền.')
      return
    }

    setCreatingRefund(true)
    setCreateError(null)
    try {
      const response = await api.POST('/api/v1/admin/refunds', {
        body: {
          orderId: createOrderId.trim(),
          amount: Number(createAmount),
          reason: createReason.trim(),
        },
      })

      const errorData = (response as { error?: { detail?: string; title?: string } }).error
      if (errorData) {
        const errDetail = errorData.detail || 'Lỗi khi tạo yêu cầu hoàn tiền.'
        setCreateError(errDetail)
        return
      }

      setShowCreateModal(false)
      setCreateOrderId('')
      setCreateAmount(0)
      setCreateReason('')
      await fetchRefunds()
    } catch {
      setCreateError('Lỗi kết nối khi gửi yêu cầu hoàn tiền.')
    } finally {
      setCreatingRefund(false)
    }
  }

  const handlePerformAction = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!actionModal) return

    setSubmittingAction(true)
    setActionError(null)
    try {
      if (actionModal.type === 'approve') {
        const response = await api.POST('/api/v1/admin/refunds/{id}/approve', {
          params: { path: { id: actionModal.refund.id } },
          body: { note: actionNote.trim() || null },
        })
        const errData = (response as { error?: { detail?: string } }).error
        if (errData) {
          setActionError(errData.detail || 'Lỗi khi duyệt hoàn tiền.')
          return
        }
      } else if (actionModal.type === 'reject') {
        if (!actionNote.trim()) {
          setActionError('Vui lòng nhập lý do từ chối.')
          setSubmittingAction(false)
          return
        }
        const response = await api.POST('/api/v1/admin/refunds/{id}/reject', {
          params: { path: { id: actionModal.refund.id } },
          body: { reason: actionNote.trim() },
        })
        const errData = (response as { error?: { detail?: string } }).error
        if (errData) {
          setActionError(errData.detail || 'Lỗi khi từ chối hoàn tiền.')
          return
        }
      } else if (actionModal.type === 'complete') {
        if (!bankRefCode.trim()) {
          setActionError('Vui lòng nhập mã tham chiếu giao dịch ngân hàng (Bank Reference).')
          setSubmittingAction(false)
          return
        }
        const response = await api.POST('/api/v1/admin/refunds/{id}/complete', {
          params: { path: { id: actionModal.refund.id } },
          body: {
            bankReference: bankRefCode.trim(),
            note: actionNote.trim() || null,
          },
        })
        const errData = (response as { error?: { detail?: string } }).error
        if (errData) {
          setActionError(errData.detail || 'Lỗi khi hoàn tất hoàn tiền.')
          return
        }
      }

      setActionModal(null)
      setActionNote('')
      setBankRefCode('')
      await fetchRefunds()
    } catch {
      setActionError('Lỗi kết nối khi thực hiện hành động.')
    } finally {
      setSubmittingAction(false)
    }
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Requested':
        return <Badge variant="warn" className="text-[10px] bg-amber-500/10 text-amber-700">Chờ duyệt (Requested)</Badge>
      case 'Approved':
        return <Badge variant="secondary" className="text-[10px] bg-blue-500/10 text-blue-700">Đã duyệt (Approved)</Badge>
      case 'Completed':
        return <Badge variant="ok" className="text-[10px]">Đã hoàn tất (Completed)</Badge>
      case 'Rejected':
        return <Badge variant="destructive" className="text-[10px]">Đã từ chối (Rejected)</Badge>
      default:
        return <Badge variant="secondary" className="text-[10px]">{status}</Badge>
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-ink flex items-center gap-2">
            <RotateCcw className="w-6 h-6 text-leaf stroke-[1.75]" />
            Quy trình hoàn tiền (Two-Person Rule)
          </h1>
          <p className="text-xs text-ink-2 mt-0.5">
            Quản lý và giải ngân hoàn tiền theo nguyên tắc 2 người kiểm soát độc lập (Yêu cầu & Duyệt/Chi trả)
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => void fetchRefunds()}
            disabled={loading}
            className="text-xs"
          >
            <RefreshCw className={`w-3.5 h-3.5 mr-1.5 ${loading ? 'animate-spin' : ''}`} /> Làm mới
          </Button>
          <Button
            size="sm"
            onClick={() => setShowCreateModal(true)}
            className="text-xs bg-leaf hover:bg-leaf/90 text-white font-semibold shadow-xs"
          >
            <Plus className="w-3.5 h-3.5 mr-1" /> Yêu cầu hoàn tiền mới
          </Button>
        </div>
      </div>

      {/* Two-Person Rule Banner */}
      <div className="p-3 bg-blue-500/5 border border-blue-500/20 rounded-xl flex items-start gap-3 text-xs text-ink-2">
        <Info className="w-4 h-4 text-blue-600 shrink-0 mt-0.5" />
        <div>
          <span className="font-bold text-blue-900 block">Quy tắc bảo mật 2 người (Two-Person Authorization):</span>
          Nhân viên tạo yêu cầu hoàn tiền không thể tự duyệt hoặc phê duyệt yêu cầu của chính mình. Phê duyệt phải do một nhân viên khác hoặc Quản lý thực hiện.
        </div>
      </div>

      {/* Filter Tabs */}
      <div className="flex border-b border-rule gap-1 text-xs font-semibold">
        <button
          type="button"
          onClick={() => { setStatusFilter('all'); setPage(1) }}
          className={`px-4 py-2.5 border-b-2 transition-colors ${
            statusFilter === 'all'
              ? 'border-leaf text-leaf'
              : 'border-transparent text-ink-2 hover:text-ink'
          }`}
        >
          Tất cả yêu cầu
        </button>
        <button
          type="button"
          onClick={() => { setStatusFilter('Requested'); setPage(1) }}
          className={`px-4 py-2.5 border-b-2 transition-colors flex items-center gap-1.5 ${
            statusFilter === 'Requested'
              ? 'border-amber-500 text-amber-700'
              : 'border-transparent text-ink-2 hover:text-ink'
          }`}
        >
          <Clock className="w-3.5 h-3.5" /> Chờ duyệt (Requested)
        </button>
        <button
          type="button"
          onClick={() => { setStatusFilter('Approved'); setPage(1) }}
          className={`px-4 py-2.5 border-b-2 transition-colors ${
            statusFilter === 'Approved'
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-ink-2 hover:text-ink'
          }`}
        >
          Đã duyệt / Chờ chuyển khoản (Approved)
        </button>
        <button
          type="button"
          onClick={() => { setStatusFilter('Completed'); setPage(1) }}
          className={`px-4 py-2.5 border-b-2 transition-colors ${
            statusFilter === 'Completed'
              ? 'border-leaf text-leaf'
              : 'border-transparent text-ink-2 hover:text-ink'
          }`}
        >
          Đã hoàn tất (Completed)
        </button>
        <button
          type="button"
          onClick={() => { setStatusFilter('Rejected'); setPage(1) }}
          className={`px-4 py-2.5 border-b-2 transition-colors ${
            statusFilter === 'Rejected'
              ? 'border-tomato text-tomato'
              : 'border-transparent text-ink-2 hover:text-ink'
          }`}
        >
          Đã từ chối (Rejected)
        </button>
      </div>

      {/* Table */}
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
          ) : refunds.length === 0 ? (
            <div className="p-12 text-center text-ink-2 text-xs">
              Không có yêu cầu hoàn tiền nào trong danh mục này.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs text-ink border-collapse">
                <thead className="bg-paper-2 text-ink-2 border-b border-rule font-semibold">
                  <tr>
                    <th className="py-3 px-4">Thời gian</th>
                    <th className="py-3 px-4">Mã đơn hàng</th>
                    <th className="py-3 px-4">Số tiền hoàn</th>
                    <th className="py-3 px-4">Lý do</th>
                    <th className="py-3 px-4">Người tạo</th>
                    <th className="py-3 px-4">Trạng thái</th>
                    <th className="py-3 px-4 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-rule/60">
                  {refunds.map((ref) => {
                    const isSelfRequested = user?.id && ref.requestedBy === user.id
                    return (
                      <tr key={ref.id} className="hover:bg-paper-2/50 transition-colors">
                        <td className="py-3 px-4 whitespace-nowrap">
                          <div className="font-mono text-ink text-[11px]">{formatDateTime(ref.requestedAt)}</div>
                          {ref.bankReference && (
                            <div className="text-[10px] text-leaf font-mono">Ref: {ref.bankReference}</div>
                          )}
                        </td>
                        <td className="py-3 px-4 whitespace-nowrap">
                          <span className="font-mono font-bold text-ink">
                            {ref.orderNumber || ref.orderId.substring(0, 8)}
                          </span>
                        </td>
                        <td className="py-3 px-4 whitespace-nowrap">
                          <span className="font-bold font-mono text-tomato text-sm">
                            {formatVnd(ref.amount)}
                          </span>
                        </td>
                        <td className="py-3 px-4 max-w-xs">
                          <div className="truncate text-ink" title={ref.reason}>
                            {ref.reason}
                          </div>
                          {ref.decisionNote && (
                            <div className="text-[10px] text-ink-2 italic mt-0.5">
                              Duyệt: {ref.decisionNote}
                            </div>
                          )}
                        </td>
                        <td className="py-3 px-4 whitespace-nowrap font-mono text-[11px] text-ink-2">
                          {isSelfRequested ? (
                            <span className="text-amber-700 font-bold">Chính bạn</span>
                          ) : (
                            <span>{ref.requestedBy.substring(0, 8)}...</span>
                          )}
                        </td>
                        <td className="py-3 px-4 whitespace-nowrap">
                          {getStatusBadge(ref.status)}
                        </td>
                        <td className="py-3 px-4 whitespace-nowrap text-right space-x-1">
                          {ref.status === 'Requested' && (
                            <>
                              <Button
                                size="sm"
                                disabled={Boolean(isSelfRequested)}
                                onClick={() => {
                                  setActionModal({ type: 'approve', refund: ref })
                                  setActionNote('')
                                  setActionError(null)
                                }}
                                className={`h-7 text-xs font-semibold ${
                                  isSelfRequested
                                    ? 'opacity-40 cursor-not-allowed bg-slate-300 text-slate-600'
                                    : 'bg-leaf hover:bg-leaf/90 text-white'
                                }`}
                                title={isSelfRequested ? 'Không thể tự duyệt yêu cầu do bạn tạo (Quy tắc 2 người)' : 'Duyệt hoàn tiền'}
                              >
                                Duyệt
                              </Button>
                              <Button
                                size="sm"
                                variant="outline"
                                onClick={() => {
                                  setActionModal({ type: 'reject', refund: ref })
                                  setActionNote('')
                                  setActionError(null)
                                }}
                                className="h-7 text-xs text-tomato border-tomato/40 hover:bg-tomato/5"
                              >
                                Từ chối
                              </Button>
                            </>
                          )}

                          {ref.status === 'Approved' && (
                            <Button
                              size="sm"
                              onClick={() => {
                                setActionModal({ type: 'complete', refund: ref })
                                setBankRefCode('')
                                setActionNote('')
                                setActionError(null)
                              }}
                              className="h-7 text-xs bg-blue-600 hover:bg-blue-700 text-white font-semibold"
                            >
                              Hoàn tất giải ngân
                            </Button>
                          )}
                        </td>
                      </tr>
                    )
                  })}
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

      {/* Create Refund Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-ink/40 backdrop-blur-xs p-4 animate-fade-in">
          <div className="bg-paper border border-rule rounded-2xl max-w-md w-full shadow-2xl overflow-hidden">
            <div className="p-4 bg-paper-2 border-b border-rule flex items-center justify-between">
              <h3 className="font-bold text-sm text-ink flex items-center gap-2">
                <Plus className="w-4 h-4 text-leaf" />
                Yêu cầu hoàn tiền mới
              </h3>
              <button
                type="button"
                onClick={() => setShowCreateModal(false)}
                className="text-ink-2 hover:text-ink text-sm font-bold"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleCreateRefund} className="p-5 space-y-4 text-xs">
              {createError && (
                <div className="p-2.5 bg-tomato/10 border border-tomato/20 rounded-lg text-tomato text-xs flex items-center gap-2">
                  <AlertTriangle className="w-4 h-4 shrink-0" />
                  {createError}
                </div>
              )}

              <div>
                <label className="font-bold text-ink block mb-1">Mã đơn hàng (Order ID / GUID):</label>
                <Input
                  value={createOrderId}
                  onChange={(e) => setCreateOrderId(e.target.value)}
                  placeholder="Nhập GUID của đơn hàng cần hoàn tiền..."
                  className="font-mono text-xs"
                  required
                />
              </div>

              <div>
                <label className="font-bold text-ink block mb-1">Số tiền cần hoàn (VND):</label>
                <Input
                  type="number"
                  min={1000}
                  step={1000}
                  value={createAmount || ''}
                  onChange={(e) => setCreateAmount(Number(e.target.value))}
                  placeholder="VD: 150000"
                  className="font-mono text-xs"
                  required
                />
                <span className="text-[10px] text-ink-2 mt-0.5 block">
                  Định dạng: {formatVnd(createAmount)}
                </span>
              </div>

              <div>
                <label className="font-bold text-ink block mb-1">Lý do hoàn tiền:</label>
                <textarea
                  value={createReason}
                  onChange={(e) => setCreateReason(e.target.value)}
                  placeholder="Ghi rõ lý do (VD: Khách trả hàng hỏng, chuyển thừa tiền...)"
                  rows={3}
                  className="w-full rounded-xl border border-rule bg-paper p-2.5 text-xs text-ink focus:outline-hidden focus:ring-2 focus:ring-leaf/20 resize-none"
                  required
                />
              </div>

              <div className="flex justify-end gap-2 pt-2 border-t border-rule">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setShowCreateModal(false)}
                  disabled={creatingRefund}
                  className="text-xs"
                >
                  Hủy
                </Button>
                <Button
                  type="submit"
                  disabled={creatingRefund}
                  className="text-xs bg-leaf hover:bg-leaf/90 text-white font-semibold"
                >
                  {creatingRefund ? 'Đang tạo...' : 'Tạo yêu cầu'}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Action Modal (Approve / Reject / Complete) */}
      {actionModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-ink/40 backdrop-blur-xs p-4 animate-fade-in">
          <div className="bg-paper border border-rule rounded-2xl max-w-md w-full shadow-2xl overflow-hidden">
            <div className="p-4 bg-paper-2 border-b border-rule flex items-center justify-between">
              <h3 className="font-bold text-sm text-ink flex items-center gap-2">
                {actionModal.type === 'approve' && <CheckCircle2 className="w-4 h-4 text-leaf" />}
                {actionModal.type === 'reject' && <XCircle className="w-4 h-4 text-tomato" />}
                {actionModal.type === 'complete' && <Building2 className="w-4 h-4 text-blue-600" />}
                {actionModal.type === 'approve' && 'Phê duyệt hoàn tiền'}
                {actionModal.type === 'reject' && 'Từ chối hoàn tiền'}
                {actionModal.type === 'complete' && 'Xác nhận chuyển khoản hoàn tất'}
              </h3>
              <button
                type="button"
                onClick={() => setActionModal(null)}
                className="text-ink-2 hover:text-ink text-sm font-bold"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handlePerformAction} className="p-5 space-y-4 text-xs">
              <div className="p-3 bg-paper-2 rounded-xl border border-rule space-y-1 font-mono text-[11px]">
                <div className="flex justify-between">
                  <span className="text-ink-2 font-sans">Số tiền:</span>
                  <span className="font-bold text-tomato">{formatVnd(actionModal.refund.amount)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-ink-2 font-sans">Lý do:</span>
                  <span className="text-ink">{actionModal.refund.reason}</span>
                </div>
              </div>

              {actionError && (
                <div className="p-2.5 bg-tomato/10 border border-tomato/20 rounded-lg text-tomato text-xs flex items-center gap-2">
                  <AlertTriangle className="w-4 h-4 shrink-0" />
                  {actionError}
                </div>
              )}

              {actionModal.type === 'complete' && (
                <div>
                  <label className="font-bold text-ink block mb-1">Mã tham chiếu ngân hàng (Bank Reference):</label>
                  <Input
                    value={bankRefCode}
                    onChange={(e) => setBankRefCode(e.target.value)}
                    placeholder="VD: FT261115123456..."
                    className="font-mono text-xs"
                    required
                  />
                  <p className="text-[10px] text-ink-2 mt-1">
                    Nhập mã giao dịch chuyển tiền từ app ngân hàng thực tế để hoàn tất.
                  </p>
                </div>
              )}

              <div>
                <label className="font-bold text-ink block mb-1">
                  {actionModal.type === 'reject' ? 'Lý do từ chối (bắt buộc):' : 'Ghi chú quyết định:'}
                </label>
                <textarea
                  value={actionNote}
                  onChange={(e) => setActionNote(e.target.value)}
                  placeholder="Nhập ghi chú..."
                  rows={3}
                  className="w-full rounded-xl border border-rule bg-paper p-2.5 text-xs text-ink focus:outline-hidden focus:ring-2 focus:ring-leaf/20 resize-none"
                  required={actionModal.type === 'reject'}
                />
              </div>

              <div className="flex justify-end gap-2 pt-2 border-t border-rule">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setActionModal(null)}
                  disabled={submittingAction}
                  className="text-xs"
                >
                  Hủy
                </Button>
                <Button
                  type="submit"
                  disabled={submittingAction}
                  className={`text-xs text-white font-semibold ${
                    actionModal.type === 'approve'
                      ? 'bg-leaf hover:bg-leaf/90'
                      : actionModal.type === 'reject'
                      ? 'bg-tomato hover:bg-tomato/90'
                      : 'bg-blue-600 hover:bg-blue-700'
                  }`}
                >
                  {submittingAction ? 'Đang gửi...' : 'Xác nhận'}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
