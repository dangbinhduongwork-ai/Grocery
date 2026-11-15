import React, { useState } from 'react'
import { useCart } from '@/shared/cart/CartContext'
import { ReceiptSummary } from '@/shared/components/ReceiptSummary'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'
import { vi } from '@/shared/i18n/vi'
import { formatVnd } from '@/shared/utils/formatVnd'
import { useRouter } from '@/app/router'
import {
  ShoppingBag,
  Trash2,
  Plus,
  Minus,
  Tag,
  AlertTriangle,
  ArrowRight,
  Truck,
} from 'lucide-react'

export const CartPage: React.FC = () => {
  const { navigate } = useRouter()
  const { cart, isLoading, updateQuantity, removeItem, applyCoupon, removeCoupon } = useCart()

  const [couponInput, setCouponInput] = useState('')
  const [couponError, setCouponError] = useState<string | null>(null)
  const [couponSuccess, setCouponSuccess] = useState<boolean>(false)
  const [isApplyingCoupon, setIsApplyingCoupon] = useState<boolean>(false)

  const handleApplyCoupon = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!couponInput.trim()) return

    setIsApplyingCoupon(true)
    setCouponError(null)
    setCouponSuccess(false)

    try {
      const ok = await applyCoupon(couponInput.trim())
      if (ok) {
        setCouponSuccess(true)
        setCouponInput('')
      } else {
        setCouponError('Mã giảm giá không hợp lệ hoặc không đủ điều kiện áp dụng')
      }
    } finally {
      setIsApplyingCoupon(false)
    }
  }

  const handleRemoveCoupon = async () => {
    await removeCoupon()
    setCouponSuccess(false)
    setCouponError(null)
  }

  const subtotal = Number(cart?.subtotal ?? 0)
  const discount = Number(cart?.couponDiscount ?? 0)
  const estimatedTotal = Number(cart?.estimatedTotal ?? 0)
  const freeShippingThreshold = 299000
  const freeShippingRemaining = Math.max(0, freeShippingThreshold - subtotal)
  const freeShippingPct = Math.min(100, Math.round((subtotal / freeShippingThreshold) * 100))

  if (!isLoading && (!cart || !cart.items || cart.items.length === 0)) {
    return (
      <div className="text-center py-20 bg-paper-2 rounded-2xl border border-rule max-w-lg mx-auto px-4">
        <div className="w-16 h-16 rounded-full bg-paper border border-rule flex items-center justify-center mx-auto mb-4">
          <ShoppingBag className="w-8 h-8 text-ink-2/60 stroke-[1.5]" />
        </div>
        <h2 className="font-display font-semibold text-xl text-ink mb-1">
          {vi.cart.emptyTitle}
        </h2>
        <p className="text-xs sm:text-sm text-ink-2 mb-6">
          {vi.cart.emptySubtitle}
        </p>
        <Button variant="primary" size="lg" onClick={() => navigate('/c/tat-ca')}>
          <span>{vi.cart.continueShopping}</span>
          <ArrowRight className="w-4 h-4 ml-1 stroke-[1.5]" />
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-8">
      {/* Title */}
      <div>
        <h1 className="font-display font-bold text-2xl sm:text-3xl text-ink">
          {vi.cart.title}
        </h1>
        <p className="text-xs text-ink-2 mt-1">
          Bạn đang có <strong className="text-ink">{cart?.items.length}</strong> loại mặt hàng
          trong giỏ
        </p>
      </div>

      {/* Free Shipping Meter */}
      <div className="p-4 rounded-xl bg-leaf/10 border border-leaf/20 space-y-2">
        <div className="flex items-center justify-between text-xs">
          <span className="font-medium text-leaf flex items-center gap-1.5">
            <Truck className="w-4 h-4 stroke-[1.5]" />
            {freeShippingRemaining === 0 ? (
              <span className="font-bold">Đủ điều kiện Miễn phí vận chuyển toàn quốc!</span>
            ) : (
              <span>
                Mua thêm <strong className="tabular-nums">{formatVnd(freeShippingRemaining)}</strong> để được Miễn phí vận chuyển
              </span>
            )}
          </span>
          <span className="font-semibold text-leaf tabular-nums">{freeShippingPct}%</span>
        </div>
        <div className="h-2 rounded-full bg-paper overflow-hidden border border-leaf/20">
          <div
            className="h-full bg-leaf rounded-full transition-all duration-300"
            style={{ width: `${freeShippingPct}%` }}
          />
        </div>
      </div>

      {/* Warnings Banner */}
      {cart?.warnings && cart.warnings.length > 0 && (
        <div className="space-y-2">
          {cart.warnings.map((w, i) => (
            <div
              key={i}
              className="p-3 rounded-lg bg-[#FFF8EB] border border-[#FAD7A0] text-xs text-[#7D4A00] flex items-center gap-2"
            >
              <AlertTriangle className="w-4 h-4 shrink-0 stroke-[1.5]" />
              <span>{w.message}</span>
            </div>
          ))}
        </div>
      )}

      {/* Cart Grid: Items List + Receipt Summary */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 items-start">
        {/* Items List */}
        <div className="lg:col-span-2 space-y-4">
          <div className="bg-paper-2 rounded-2xl border border-rule divide-y divide-rule overflow-hidden">
            {cart?.items.map((item) => (
              <div
                key={item.id}
                className="p-4 sm:p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-4"
              >
                {/* Product info */}
                <div className="flex items-center gap-4 flex-1">
                  <div className="w-20 h-20 rounded-xl bg-paper border border-rule overflow-hidden p-2 shrink-0 flex items-center justify-center">
                    {item.imageUrl ? (
                      <img
                        src={item.imageUrl}
                        alt={item.name}
                        className="w-full h-full object-contain mix-blend-multiply"
                      />
                    ) : (
                      <ShoppingBag className="w-8 h-8 text-ink-2/30 stroke-[1]" />
                    )}
                  </div>
                  <div className="space-y-1">
                    <h3
                      className="text-sm font-semibold text-ink hover:text-leaf cursor-pointer transition-colors line-clamp-2"
                      onClick={() => navigate(`/p/${item.productId}`)}
                    >
                      {item.name}
                    </h3>
                    <div className="flex items-center gap-3 text-xs text-ink-2">
                      <span>Đơn giá: <strong className="text-ink">{formatVnd(Number(item.unitPrice))}</strong></span>
                      {item.unit && <span>/ {item.unit}</span>}
                    </div>
                  </div>
                </div>

                {/* Quantity Stepper & Line Total */}
                <div className="flex items-center justify-between sm:justify-end gap-6 pt-2 sm:pt-0 border-t sm:border-none border-rule/50">
                  <div className="inline-flex items-center rounded-lg border border-rule bg-paper">
                    <button
                      type="button"
                      disabled={Number(item.quantity) <= 1}
                      onClick={() => void updateQuantity(item.id, Number(item.quantity) - 1)}
                      className="p-1.5 text-ink hover:text-leaf disabled:opacity-40 cursor-pointer"
                      aria-label="Giảm"
                    >
                      <Minus className="w-3.5 h-3.5 stroke-[1.5]" />
                    </button>
                    <span className="w-10 text-center text-xs font-bold text-ink tabular-nums select-none">
                      {item.quantity}
                    </span>
                    <button
                      type="button"
                      onClick={() => void updateQuantity(item.id, Number(item.quantity) + 1)}
                      className="p-1.5 text-ink hover:text-leaf cursor-pointer"
                      aria-label="Tăng"
                    >
                      <Plus className="w-3.5 h-3.5 stroke-[1.5]" />
                    </button>
                  </div>

                  <div className="text-right min-w-[90px]">
                    <span className="text-sm font-bold text-ink tabular-nums block">
                      {formatVnd(Number(item.lineSubtotal))}
                    </span>
                  </div>

                  <button
                    type="button"
                    onClick={() => void removeItem(item.id)}
                    className="p-1.5 text-ink-2 hover:text-tomato transition-colors cursor-pointer"
                    title={vi.cart.remove}
                  >
                    <Trash2 className="w-4 h-4 stroke-[1.5]" />
                  </button>
                </div>
              </div>
            ))}
          </div>

          <div className="flex items-center justify-between pt-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => navigate('/c/tat-ca')}
              className="text-xs text-leaf font-semibold"
            >
              ← Tiếp tục mua sắm
            </Button>
          </div>
        </div>

        {/* Right: Receipt Summary & Coupon Box */}
        <div className="space-y-6">
          {/* Coupon Code Input */}
          <div className="bg-paper-2 p-5 rounded-2xl border border-rule space-y-3">
            <h3 className="font-display font-semibold text-sm text-ink flex items-center gap-2">
              <Tag className="w-4 h-4 text-leaf stroke-[1.5]" />
              {vi.cart.couponCode}
            </h3>

            {cart?.couponCode ? (
              <div className="flex items-center justify-between p-3 rounded-lg bg-leaf/10 border border-leaf/20 text-xs">
                <div>
                  <span className="font-bold text-leaf uppercase">{cart.couponCode}</span>
                  <span className="text-[11px] text-ink-2 block">
                    Đã áp dụng giảm {formatVnd(discount)}
                  </span>
                </div>
                <button
                  type="button"
                  onClick={() => void handleRemoveCoupon()}
                  className="text-xs text-tomato hover:underline cursor-pointer font-medium"
                >
                  Gỡ bỏ
                </button>
              </div>
            ) : (
              <form onSubmit={handleApplyCoupon} className="space-y-2">
                <div className="flex gap-2">
                  <Input
                    placeholder="Nhập mã ưu đãi (ví dụ: FREESHIP, GROCERY10)"
                    value={couponInput}
                    onChange={(e) => setCouponInput(e.target.value.toUpperCase())}
                    className="text-xs h-9 uppercase font-mono"
                  />
                  <Button
                    type="submit"
                    variant="outline"
                    size="sm"
                    disabled={isApplyingCoupon || !couponInput.trim()}
                    className="text-xs h-9 shrink-0 font-semibold"
                  >
                    {isApplyingCoupon ? '...' : vi.cart.apply}
                  </Button>
                </div>
                {couponError && (
                  <p className="text-[11px] text-tomato">{couponError}</p>
                )}
                {couponSuccess && (
                  <p className="text-[11px] text-leaf">Áp dụng mã thành công!</p>
                )}
              </form>
            )}
          </div>

          {/* Receipt Summary Box */}
          <div className="bg-paper-2 p-5 rounded-2xl border border-rule space-y-4">
            <h3 className="font-display font-semibold text-base text-ink">
              Tóm tắt đơn hàng
            </h3>

            <ReceiptSummary
              subtotal={subtotal}
              couponDiscount={discount > 0 ? discount : undefined}
              estimatedTotal={estimatedTotal}
            />

            <Button
              variant="primary"
              size="lg"
              className="w-full font-semibold shadow-xs flex items-center justify-center gap-2"
              onClick={() => navigate('/checkout')}
            >
              <span>{vi.cart.checkout}</span>
              <ArrowRight className="w-4 h-4 stroke-[1.5]" />
            </Button>

            <p className="text-[11px] text-ink-2 text-center">
              Giá đã bao gồm thuế VAT. Miễn phí đổi trả trong 24 giờ.
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
