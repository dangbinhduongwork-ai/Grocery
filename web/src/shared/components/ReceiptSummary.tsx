import React from 'react'
import { formatVnd } from '@/shared/utils/formatVnd'
import { vi } from '@/shared/i18n/vi'
import { cn } from '@/shared/utils/cn'

interface ReceiptSummaryProps {
  subtotal: number
  shippingFee?: number
  couponDiscount?: number
  couponCode?: string | null
  total?: number
  estimatedTotal?: number
  className?: string
}

export const ReceiptSummary: React.FC<ReceiptSummaryProps> = ({
  subtotal,
  shippingFee = 0,
  couponDiscount = 0,
  couponCode,
  total,
  estimatedTotal,
  className,
}) => {
  const finalTotal = total ?? estimatedTotal ?? Math.max(0, subtotal - couponDiscount + shippingFee)
  return (
    <div
      className={cn(
        'rounded-[12px] border border-rule bg-paper p-5 space-y-3 font-sans shadow-xs',
        className
      )}
    >
      <h3 className="text-base font-semibold text-ink border-b border-rule pb-2 font-display">
        {vi.cart.title}
      </h3>

      <div className="space-y-2 text-sm">
        <div className="flex justify-between text-ink-2">
          <span>{vi.cart.subtotal}</span>
          <span className="tabular-nums font-medium text-ink">
            {formatVnd(subtotal)}
          </span>
        </div>

        {couponDiscount > 0 && (
          <div className="flex justify-between text-tomato">
            <span className="flex items-center gap-1">
              <span>{vi.cart.discount}</span>
              {couponCode && (
                <span className="text-xs bg-tomato/10 px-1 rounded-[4px] font-mono">
                  {couponCode}
                </span>
              )}
            </span>
            <span className="tabular-nums font-medium">
              -{formatVnd(couponDiscount)}
            </span>
          </div>
        )}

        <div className="flex justify-between text-ink-2">
          <span>{vi.cart.shipping}</span>
          <span className="tabular-nums font-medium text-ink">
            {shippingFee === 0 ? vi.cart.freeShipping : formatVnd(shippingFee)}
          </span>
        </div>
      </div>

      <div className="border-t border-rule pt-3 flex justify-between items-baseline">
        <span className="text-base font-bold text-ink">{vi.cart.total}</span>
        <div className="text-right">
          <span className="text-xl sm:text-2xl font-bold text-tomato tabular-nums">
            {formatVnd(finalTotal)}
          </span>
          <p className="text-[11px] text-ink-2/70 font-normal">
            (Đã bao gồm VAT nếu có)
          </p>
        </div>
      </div>
    </div>
  )
}
