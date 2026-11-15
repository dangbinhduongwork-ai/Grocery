import React from 'react'
import { formatVnd } from '@/shared/utils/formatVnd'
import { cn } from '@/shared/utils/cn'

interface PriceTagProps {
  sellingPrice?: number | string
  listPrice?: number | string | null
  price?: number | string
  originalPrice?: number | string | null
  unit?: string | null
  size?: 'sm' | 'md' | 'lg'
  className?: string
}

export const PriceTag: React.FC<PriceTagProps> = ({
  sellingPrice,
  listPrice,
  price,
  originalPrice,
  unit,
  size = 'md',
  className,
}) => {
  const finalSelling = Number(sellingPrice ?? price ?? 0)
  const finalList = listPrice != null ? Number(listPrice) : (originalPrice != null ? Number(originalPrice) : undefined)

  const hasDiscount = finalList !== undefined && finalList > finalSelling
  const discountPercent = hasDiscount && finalList
    ? Math.round(((finalList - finalSelling) / finalList) * 100)
    : 0

  return (
    <div className={cn('flex flex-wrap items-baseline gap-1.5', className)}>
      <span
        className={cn(
          'tabular-nums font-bold tracking-tight',
          hasDiscount ? 'text-tomato' : 'text-ink',
          size === 'sm' && 'text-sm',
          size === 'md' && 'text-base sm:text-lg',
          size === 'lg' && 'text-xl sm:text-2xl'
        )}
      >
        {formatVnd(finalSelling)}
      </span>

      {unit && (
        <span className="text-xs text-ink-2 font-normal">
          / {unit}
        </span>
      )}

      {hasDiscount && finalList !== undefined && (
        <>
          <span className="text-xs sm:text-sm text-ink-2/70 line-through tabular-nums">
            {formatVnd(finalList)}
          </span>
          <span className="rounded-[4px] bg-tomato/10 px-1 py-0.2 text-[11px] font-bold text-tomato tabular-nums">
            -{discountPercent}%
          </span>
        </>
      )}
    </div>
  )
}
