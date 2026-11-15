import React, { useEffect, useState, useCallback } from 'react'
import {
  Star,
  RefreshCw,
  CheckCircle2,
  XCircle,
  MessageSquare,
  X,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { formatDateTime } from '@/shared/utils/dateTime'
import { DataTable, Column } from '../components/DataTable'
import { StatusChip } from '../components/StatusChip'

interface ReviewItem {
  id: string
  productId: string
  productName: string
  authorName: string
  rating: number
  body: string
  status: string
  replyBody?: string | null
  replyAt?: string | null
  createdAt: string
}

export const AdminReviewsPage: React.FC = () => {
  const [reviews, setReviews] = useState<ReviewItem[]>([])
  const [isLoading, setIsLoading] = useState(true)

  // Reply modal
  const [replyTarget, setReplyTarget] = useState<ReviewItem | null>(null)
  const [replyBody, setReplyBody] = useState('')
  const [isReplying, setIsReplying] = useState(false)

  const fetchReviews = useCallback(async () => {
    setIsLoading(true)
    try {
      const res = await api.GET('/api/v1/admin/reviews', {
        params: { query: { pageSize: 100 } },
      })
      if (res.data?.items) {
        setReviews(res.data.items as unknown as ReviewItem[])
      } else {
        setReviews([])
      }
    } catch {
      setReviews([])
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void fetchReviews()
  }, [fetchReviews])

  const handleApprove = async (id: string) => {
    try {
      await api.POST('/api/v1/admin/reviews/{id}/approve', {
        params: { path: { id } },
      })
      await fetchReviews()
    } catch {
      alert('Không thể duyệt đánh giá.')
    }
  }

  const handleReject = async (id: string) => {
    const reason = prompt('Nhập lý do từ chối đánh giá:')
    if (reason === null) return

    try {
      await api.POST('/api/v1/admin/reviews/{id}/reject', {
        params: { path: { id } },
        body: { reason: reason.trim() || null },
      })
      await fetchReviews()
    } catch {
      alert('Không thể từ chối đánh giá.')
    }
  }

  const handleReplySubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!replyTarget || !replyBody.trim()) return

    setIsReplying(true)
    try {
      await api.POST('/api/v1/admin/reviews/{id}/reply', {
        params: { path: { id: replyTarget.id } },
        body: { replyBody },
      })
      setReplyTarget(null)
      setReplyBody('')
      await fetchReviews()
    } catch {
      alert('Không thể đăng phản hồi.')
    } finally {
      setIsReplying(false)
    }
  }

  const columns: Column<ReviewItem>[] = [
    {
      key: 'productName',
      title: 'Sản phẩm',
      sortable: true,
      render: r => (
        <div>
          <div className="font-semibold text-slate-800">{r.productName}</div>
          <div className="text-xs text-slate-400">Khách: {r.authorName}</div>
        </div>
      ),
    },
    {
      key: 'rating',
      title: 'Đánh giá',
      sortable: true,
      render: r => (
        <div className="flex items-center gap-1 text-amber-500 font-bold text-xs">
          <Star className="w-4 h-4 fill-amber-400 text-amber-400" />
          <span>{r.rating} / 5</span>
        </div>
      ),
    },
    {
      key: 'body',
      title: 'Nội dung nhận xét & Phản hồi',
      render: r => (
        <div className="space-y-1 max-w-md">
          <p className="text-xs text-slate-800 italic">&ldquo;{r.body}&rdquo;</p>
          {r.replyBody && (
            <div className="p-2 rounded-lg bg-slate-50 border border-slate-100 text-[11px] text-slate-600">
              <span className="font-semibold text-leaf">Phản hồi của shop:</span> {r.replyBody}
            </div>
          )}
          <span className="text-[10px] text-slate-400 block">{formatDateTime(r.createdAt)}</span>
        </div>
      ),
    },
    {
      key: 'status',
      title: 'Trạng thái',
      render: r => <StatusChip status={r.status} type="review" />,
    },
    {
      key: 'actions',
      title: 'Thao tác',
      className: 'text-right',
      render: r => (
        <div className="flex items-center justify-end gap-1.5">
          {r.status === 'Pending' && (
            <>
              <button
                type="button"
                onClick={() => void handleApprove(r.id)}
                className="p-1.5 rounded-lg border border-emerald-200 text-emerald-700 bg-emerald-50 hover:bg-emerald-100 transition-colors"
                title="Duyệt hiển thị"
              >
                <CheckCircle2 className="w-4 h-4" strokeWidth={1.5} />
              </button>
              <button
                type="button"
                onClick={() => void handleReject(r.id)}
                className="p-1.5 rounded-lg border border-rose-200 text-rose-700 bg-rose-50 hover:bg-rose-100 transition-colors"
                title="Từ chối"
              >
                <XCircle className="w-4 h-4" strokeWidth={1.5} />
              </button>
            </>
          )}

          <button
            type="button"
            onClick={() => {
              setReplyTarget(r)
              setReplyBody(r.replyBody ?? '')
            }}
            className="p-1.5 rounded-lg border border-slate-200 text-slate-600 hover:text-leaf hover:bg-slate-50 transition-colors"
            title="Trả lời khách hàng"
          >
            <MessageSquare className="w-4 h-4" strokeWidth={1.5} />
          </button>
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 font-display">Kiểm duyệt đánh giá</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Duyệt nhận xét người mua thực tế (BR-09), ngăn chặn ngôn từ xúc phạm và phản hồi chăm sóc khách hàng.
          </p>
        </div>
        <button
          type="button"
          onClick={() => void fetchReviews()}
          disabled={isLoading}
          className="p-2 rounded-xl border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 transition-colors shadow-2xs"
          title="Làm mới"
        >
          <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} strokeWidth={1.5} />
        </button>
      </div>

      <DataTable
        data={reviews}
        columns={columns}
        keyExtractor={r => r.id}
        searchPlaceholder="Tìm theo tên sản phẩm hoặc nội dung đánh giá..."
        searchableKey={r => `${r.productName} ${r.body} ${r.authorName}`}
        pageSize={15}
        isLoading={isLoading}
        emptyMessage="Chưa có đánh giá nào cần xử lý"
      />

      {replyTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-xs">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-200 space-y-4">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <h2 className="text-base font-bold text-slate-900 font-display">Phản hồi khách hàng</h2>
              <button type="button" onClick={() => setReplyTarget(null)} className="text-slate-400 hover:text-slate-600">
                <X className="w-5 h-5" strokeWidth={1.5} />
              </button>
            </div>

            <div className="p-3 bg-slate-50 rounded-xl border border-slate-100 text-xs space-y-1">
              <div>Sản phẩm: <span className="font-semibold text-slate-800">{replyTarget.productName}</span></div>
              <div className="italic text-slate-600">&ldquo;{replyTarget.body}&rdquo;</div>
            </div>

            <form onSubmit={handleReplySubmit} className="space-y-4 text-xs">
              <div>
                <label className="block font-semibold text-slate-700 mb-1">Nội dung phản hồi từ Cửa hàng *</label>
                <textarea
                  required
                  rows={4}
                  value={replyBody}
                  onChange={e => setReplyBody(e.target.value)}
                  placeholder="Cảm ơn bạn đã tin tưởng mua sắm tại GroceryHub..."
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>

              <div className="flex items-center justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setReplyTarget(null)}
                  className="px-4 py-2 rounded-xl border border-slate-200 text-slate-600 hover:bg-slate-50"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={isReplying || !replyBody.trim()}
                  className="px-5 py-2 rounded-xl bg-leaf text-white font-semibold hover:bg-leaf-2 shadow-xs disabled:opacity-50"
                >
                  {isReplying ? 'Đang gửi...' : 'Đăng phản hồi'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
