import React from 'react'
import { Printer, X, CheckCircle2, AlertCircle } from 'lucide-react'
import { formatVnd } from '@/shared/utils/formatVnd'
import { formatDateTime } from '@/shared/utils/dateTime'
import { numberToVietnameseWords } from '@/shared/utils/numberToVietnameseWords'

export interface InvoiceOrderData {
  id: string
  orderNumber: string
  placedAt?: string
  createdAt?: string
  status: string
  subtotal: number | string
  couponDiscount?: number | string
  shippingFee: number | string
  total: number | string
  paymentMethod: string
  paymentStatus: string
  address?: {
    recipientName?: string
    phone?: string
    provinceName?: string
    wardName?: string
    street?: string
    note?: string | null
  }
  shippingRecipientName?: string
  shippingPhone?: string
  shippingProvinceName?: string
  shippingWardName?: string
  shippingStreet?: string
  shippingNote?: string
  customerNote?: string | null
  items: Array<{
    id?: string
    sku?: string
    productSku?: string
    name?: string
    productName?: string
    unit?: string
    productUnit?: string
    unitPrice: number | string
    quantity: number | string
    subtotal?: number | string
    lineSubtotal?: number | string
    lineTotal?: number | string
  }>
}

interface OrderInvoiceModalProps {
  order: InvoiceOrderData | null
  isOpen: boolean
  onClose: () => void
}

export const OrderInvoiceModal: React.FC<OrderInvoiceModalProps> = ({
  order,
  isOpen,
  onClose,
}) => {
  if (!isOpen || !order) return null

  const handlePrint = () => {
    window.print()
  }

  const isCod = order.paymentMethod?.toUpperCase() === 'COD'
  const isPaid = order.paymentStatus?.toLowerCase() === 'paid'

  // Normalize recipient and address fields
  const recipientName = order.address?.recipientName || order.shippingRecipientName || 'Khách hàng'
  const recipientPhone = order.address?.phone || order.shippingPhone || ''
  const street = order.address?.street || order.shippingStreet || ''
  const wardName = order.address?.wardName || order.shippingWardName || ''
  const provinceName = order.address?.provinceName || order.shippingProvinceName || ''
  const fullAddress = [street, wardName, provinceName].filter(Boolean).join(', ') || 'Chưa cập nhật địa chỉ'
  const note = order.customerNote || order.address?.note || order.shippingNote || ''
  const orderDate = order.createdAt || order.placedAt || ''

  const totalNum = Number(order.total) || 0
  const subtotalNum = Number(order.subtotal) || 0
  const shippingFeeNum = Number(order.shippingFee) || 0
  const couponDiscountNum = Number(order.couponDiscount) || 0

  return (
    <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-2 sm:p-4 overflow-y-auto">
      {/* Modal Box */}
      <div className="bg-white w-full max-w-4xl rounded-2xl shadow-2xl border border-slate-200 overflow-hidden flex flex-col max-h-[95vh] my-auto">
        {/* Modal Top Control Bar (Hidden on print) */}
        <div className="no-print px-5 py-3.5 bg-slate-50 border-b border-slate-200 flex items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <Printer className="w-5 h-5 text-leaf" />
            <span className="font-bold text-slate-800 text-sm">
              In Phiếu Giao Hàng & Hóa Đơn — #{order.orderNumber}
            </span>
          </div>

          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={handlePrint}
              className="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-leaf text-white font-bold text-xs hover:bg-leaf/90 transition-all shadow-xs cursor-pointer"
            >
              <Printer className="w-4 h-4" />
              <span>In Phiếu Ngay (Ctrl + P)</span>
            </button>
            <button
              type="button"
              onClick={onClose}
              className="p-2 text-slate-400 hover:text-slate-600 rounded-lg hover:bg-slate-200 transition-colors"
              title="Đóng cửa sổ"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Printable Area (Styled for A4 print and on-screen preview) */}
        <div className="p-6 sm:p-10 overflow-y-auto bg-white text-slate-900 font-sans invoice-printable-area">
          {/* Header */}
          <div className="border-b-2 border-slate-800 pb-5 mb-5 flex flex-col sm:flex-row sm:items-start justify-between gap-4">
            <div>
              <div className="flex items-center gap-2">
                <div className="w-7 h-7 rounded-lg bg-leaf text-white font-bold flex items-center justify-center text-sm font-display">
                  GH
                </div>
                <h1 className="text-xl font-bold tracking-tight text-slate-900 uppercase">
                  GroceryHub Vietnam
                </h1>
              </div>
              <p className="text-xs text-slate-600 mt-1">
                Chuỗi Thực phẩm Tươi sạch & Nông sản Chuẩn VietGAP
              </p>
              <p className="text-xs text-slate-500 mt-0.5">
                Hotline: <strong className="text-slate-800">0865 097 784</strong> &bull; Email: dangbinhduong.work@gmail.com
              </p>
              <p className="text-xs text-slate-500">
                Địa chỉ: Tòa nhà GreenHub, Cầu Giấy, Hà Nội &bull; Web: groceryhub.vn
              </p>
            </div>

            <div className="text-left sm:text-right">
              <h2 className="text-lg font-black tracking-wide text-slate-900 uppercase font-display">
                PHIẾU GIAO HÀNG
              </h2>
              <p className="text-[11px] font-semibold uppercase text-slate-500 tracking-wider">
                Kiêm Hóa Đơn Bán Lẻ
              </p>
              <div className="mt-2 text-xs text-slate-700 space-y-0.5">
                <div>
                  Mã đơn: <strong className="font-mono text-sm text-leaf">#{order.orderNumber}</strong>
                </div>
                <div>Ngày đặt: {formatDateTime(orderDate)}</div>
                <div>Ngày in phiếu: {formatDateTime(new Date().toISOString())}</div>
              </div>
            </div>
          </div>

          {/* Delivery & Customer Info Box */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 p-4 rounded-xl border border-slate-200 bg-slate-50/50 mb-6 text-xs">
            <div className="space-y-1.5">
              <h3 className="font-bold text-slate-800 uppercase tracking-wider text-[11px] border-b border-slate-200 pb-1">
                Thông tin người nhận hàng
              </h3>
              <div>
                Họ và tên: <strong className="text-slate-900 text-sm">{recipientName}</strong>
              </div>
              <div>
                Số điện thoại: <strong className="font-mono text-slate-900 text-sm">{recipientPhone || '—'}</strong>
              </div>
              <div>
                Địa chỉ giao hàng:{' '}
                <span className="font-medium text-slate-800">
                  {fullAddress}
                </span>
              </div>
              {note && (
                <div className="pt-1 text-amber-900">
                  <strong>Lời dặn của khách:</strong> <em>&ldquo;{note}&rdquo;</em>
                </div>
              )}
            </div>

            <div className="space-y-1.5">
              <h3 className="font-bold text-slate-800 uppercase tracking-wider text-[11px] border-b border-slate-200 pb-1">
                Thanh toán & Thu tiền
              </h3>
              <div>
                Hình thức thanh toán:{' '}
                <strong className="text-slate-900">
                  {isCod ? 'Thanh toán tiền mặt khi nhận hàng (COD)' : 'Chuyển khoản VietQR'}
                </strong>
              </div>
              <div>
                Trạng thái:{' '}
                {isPaid ? (
                  <span className="inline-flex items-center gap-1 font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded border border-emerald-200">
                    <CheckCircle2 className="w-3.5 h-3.5" />
                    ĐÃ THANH TOÁN (Không thu thêm tiền)
                  </span>
                ) : (
                  <span className="inline-flex items-center gap-1 font-bold text-amber-800 bg-amber-50 px-2 py-0.5 rounded border border-amber-200">
                    <AlertCircle className="w-3.5 h-3.5" />
                    Chưa thanh toán
                  </span>
                )}
              </div>
              <div className="p-2.5 rounded-lg border border-slate-300 bg-white mt-2">
                <span className="text-[11px] text-slate-500 uppercase font-semibold block">
                  Tiền Shipper cần thu khách:
                </span>
                <span className="text-lg font-black text-rose-600 font-mono">
                  {isCod ? formatVnd(totalNum) : '0 ₫ (Đã thanh toán)'}
                </span>
              </div>
            </div>
          </div>

          {/* Items Table */}
          <div className="mb-6">
            <table className="w-full text-xs border-collapse">
              <thead>
                <tr className="border-y-2 border-slate-800 bg-slate-100/80 text-slate-800">
                  <th className="py-2.5 px-2 text-center w-10 font-bold">STT</th>
                  <th className="py-2.5 px-2 text-left w-24 font-bold">Mã SKU</th>
                  <th className="py-2.5 px-3 text-left font-bold">Tên sản phẩm & Quy cách</th>
                  <th className="py-2.5 px-2 text-center w-16 font-bold">ĐVT</th>
                  <th className="py-2.5 px-2 text-center w-14 font-bold">SL</th>
                  <th className="py-2.5 px-3 text-right w-24 font-bold">Đơn giá</th>
                  <th className="py-2.5 px-3 text-right w-28 font-bold">Thành tiền</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {(order.items ?? []).map((item, idx) => {
                  const sku = item.sku || item.productSku || '—'
                  const name = item.name || item.productName || 'Sản phẩm'
                  const unit = item.unit || item.productUnit || 'Món'
                  const unitPrice = Number(item.unitPrice) || 0
                  const qty = Number(item.quantity) || 1
                  const lineTotal = Number(item.lineTotal ?? item.subtotal ?? item.lineSubtotal ?? (unitPrice * qty)) || 0

                  return (
                    <tr key={item.id || idx} className="hover:bg-slate-50/50">
                      <td className="py-2.5 px-2 text-center text-slate-500">{idx + 1}</td>
                      <td className="py-2.5 px-2 font-mono text-slate-600">{sku}</td>
                      <td className="py-2.5 px-3 font-semibold text-slate-900">{name}</td>
                      <td className="py-2.5 px-2 text-center text-slate-600">{unit}</td>
                      <td className="py-2.5 px-2 text-center font-bold text-slate-900">{qty}</td>
                      <td className="py-2.5 px-3 text-right font-mono text-slate-700">{formatVnd(unitPrice)}</td>
                      <td className="py-2.5 px-3 text-right font-bold text-slate-900 font-mono">
                        {formatVnd(lineTotal)}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          {/* Totals Summary */}
          <div className="flex flex-col sm:flex-row justify-between items-start gap-4 mb-8">
            <div className="flex-1 text-xs text-slate-600 italic">
              <strong>Số tiền bằng chữ:</strong> {numberToVietnameseWords(totalNum)}.
            </div>

            <div className="w-full sm:w-72 space-y-1.5 text-xs text-slate-700">
              <div className="flex justify-between py-1 border-b border-slate-100">
                <span>Tiền hàng (Tạm tính):</span>
                <span className="font-mono font-medium">{formatVnd(subtotalNum)}</span>
              </div>
              {couponDiscountNum > 0 && (
                <div className="flex justify-between py-1 border-b border-slate-100 text-emerald-700">
                  <span>Giảm giá khuyến mãi:</span>
                  <span className="font-mono font-medium">-{formatVnd(couponDiscountNum)}</span>
                </div>
              )}
              <div className="flex justify-between py-1 border-b border-slate-100">
                <span>Phí vận chuyển:</span>
                <span className="font-mono font-medium">
                  {shippingFeeNum === 0 ? '0 ₫ (Freeship)' : formatVnd(shippingFeeNum)}
                </span>
              </div>
              <div className="flex justify-between py-2 border-t-2 border-slate-800 text-sm font-bold text-slate-900">
                <span className="uppercase">Tổng thanh toán:</span>
                <span className="text-base text-leaf font-mono">{formatVnd(totalNum)}</span>
              </div>
            </div>
          </div>

          {/* Signatures */}
          <div className="grid grid-cols-3 gap-4 text-center text-xs pt-4 border-t border-slate-200 mt-6 mb-8">
            <div className="space-y-12">
              <div>
                <strong className="block text-slate-800 uppercase tracking-wider text-[11px]">
                  Người lập phiếu
                </strong>
                <span className="text-slate-400 text-[10px]">(Ký, ghi rõ họ tên)</span>
              </div>
              <div className="text-slate-500 font-medium text-[11px] pt-4">Nhân viên kho</div>
            </div>

            <div className="space-y-12">
              <div>
                <strong className="block text-slate-800 uppercase tracking-wider text-[11px]">
                  Người giao hàng
                </strong>
                <span className="text-slate-400 text-[10px]">(Ký, ghi rõ họ tên)</span>
              </div>
              <div className="text-slate-500 font-medium text-[11px] pt-4">Nhân viên giao vận</div>
            </div>

            <div className="space-y-12">
              <div>
                <strong className="block text-slate-800 uppercase tracking-wider text-[11px]">
                  Người nhận hàng
                </strong>
                <span className="text-slate-400 text-[10px]">(Ký xác nhận đã nhận đủ hàng)</span>
              </div>
              <div className="text-slate-400 text-[11px] pt-4">
                <em>{recipientName}</em>
              </div>
            </div>
          </div>

          {/* Guarantee & Thank you */}
          <div className="text-center text-[11px] text-slate-500 border-t border-dashed border-slate-300 pt-4 space-y-1">
            <p>
              Quý khách vui lòng kiểm tra kỹ số lượng và chất lượng sản phẩm khi nhận hàng.
            </p>
            <p className="font-semibold text-leaf">
              Cam kết đổi trả trong vòng 24 giờ đối với thực phẩm tươi sống nếu không đạt chuẩn VietGAP!
            </p>
            <p className="italic">
              Cảm ơn Quý khách đã tin tưởng và đồng hành cùng GroceryHub!
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
