import React from 'react'
import {
  Clock,
  CheckCircle2,
  Package,
  Truck,
  XCircle,
  AlertTriangle,
  CreditCard,
  ShieldCheck,
  ShieldAlert,
  Boxes,
} from 'lucide-react'

export interface StatusChipProps {
  status: string
  type?: 'order' | 'payment' | 'review' | 'stock' | 'user'
  className?: string
}

const statusConfig: Record<string, { label: string; bg: string; text: string; icon: React.ElementType }> = {
  // Order statuses
  PendingPayment: { label: 'Chờ thanh toán', bg: 'bg-amber-50', text: 'text-amber-800 border-amber-200', icon: Clock },
  Placed: { label: 'Đã đặt hàng', bg: 'bg-blue-50', text: 'text-blue-800 border-blue-200', icon: Clock },
  Confirmed: { label: 'Đã xác nhận', bg: 'bg-indigo-50', text: 'text-indigo-800 border-indigo-200', icon: CheckCircle2 },
  Preparing: { label: 'Đang chuẩn bị', bg: 'bg-purple-50', text: 'text-purple-800 border-purple-200', icon: Boxes },
  Packed: { label: 'Đã đóng gói', bg: 'bg-cyan-50', text: 'text-cyan-800 border-cyan-200', icon: Package },
  Shipping: { label: 'Đang giao hàng', bg: 'bg-sky-50', text: 'text-sky-800 border-sky-200', icon: Truck },
  Delivered: { label: 'Đã giao hàng', bg: 'bg-emerald-50', text: 'text-emerald-800 border-emerald-200', icon: CheckCircle2 },
  Cancelled: { label: 'Đã hủy', bg: 'bg-rose-50', text: 'text-rose-800 border-rose-200', icon: XCircle },
  DeliveryFailed: { label: 'Giao thất bại', bg: 'bg-rose-50', text: 'text-rose-800 border-rose-200', icon: AlertTriangle },

  // Payment statuses
  Unpaid: { label: 'Chưa thanh toán', bg: 'bg-amber-50', text: 'text-amber-800 border-amber-200', icon: Clock },
  Paid: { label: 'Đã thanh toán', bg: 'bg-emerald-50', text: 'text-emerald-800 border-emerald-200', icon: CreditCard },
  Failed: { label: 'Thất bại', bg: 'bg-rose-50', text: 'text-rose-800 border-rose-200', icon: XCircle },
  Expired: { label: 'Hết hạn', bg: 'bg-slate-50', text: 'text-slate-700 border-slate-200', icon: Clock },
  PartiallyRefunded: { label: 'Hoàn một phần', bg: 'bg-orange-50', text: 'text-orange-800 border-orange-200', icon: AlertTriangle },
  Refunded: { label: 'Đã hoàn tiền', bg: 'bg-purple-50', text: 'text-purple-800 border-purple-200', icon: CheckCircle2 },

  // Review statuses
  Pending: { label: 'Chờ duyệt', bg: 'bg-amber-50', text: 'text-amber-800 border-amber-200', icon: Clock },
  Approved: { label: 'Đã duyệt', bg: 'bg-emerald-50', text: 'text-emerald-800 border-emerald-200', icon: ShieldCheck },
  Rejected: { label: 'Từ chối', bg: 'bg-rose-50', text: 'text-rose-800 border-rose-200', icon: ShieldAlert },

  // Stock statuses
  InStock: { label: 'Còn hàng', bg: 'bg-emerald-50', text: 'text-emerald-800 border-emerald-200', icon: CheckCircle2 },
  LowStock: { label: 'Sắp hết', bg: 'bg-amber-50', text: 'text-amber-800 border-amber-200', icon: AlertTriangle },
  OutOfStock: { label: 'Hết hàng', bg: 'bg-rose-50', text: 'text-rose-800 border-rose-200', icon: XCircle },

  // User statuses
  Active: { label: 'Hoạt động', bg: 'bg-emerald-50', text: 'text-emerald-800 border-emerald-200', icon: CheckCircle2 },
  Locked: { label: 'Bị khóa', bg: 'bg-rose-50', text: 'text-rose-800 border-rose-200', icon: XCircle },
}

export const StatusChip: React.FC<StatusChipProps> = ({ status, className = '' }) => {
  const config = statusConfig[status] ?? {
    label: status,
    bg: 'bg-slate-50',
    text: 'text-slate-700 border-slate-200',
    icon: Clock,
  }

  const Icon = config.icon

  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-medium border ${config.bg} ${config.text} ${className}`}
    >
      <Icon className="w-3.5 h-3.5 shrink-0" strokeWidth={1.5} />
      <span>{config.label}</span>
    </span>
  )
}
