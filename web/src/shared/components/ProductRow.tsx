import React from 'react'
import { formatVnd } from '@/shared/utils/formatVnd'
import { Trash2, Plus, Minus } from 'lucide-react'
import { cn } from '@/shared/utils/cn'

export interface ProductRowItem {
  id: string
  productId: string
  name: string
  sku: string
  unit: string
  packSize?: string | null
  unitPrice: number
  quantity: number
  lineSubtotal: number
  imageUrl?: string | null
}

interface ProductRowProps {
  item: ProductRowItem
  readOnly?: boolean
  onUpdateQuantity?: (id: string, newQuantity: number) => void
  onRemove?: (id: string) => void
  className?: string
}

export const ProductRow: React.FC<ProductRowProps> = ({
  item,
  readOnly = false,
  onUpdateQuantity,
  onRemove,
  className,
}) => {
  return (
    <div
      className={cn(
        'flex items-center gap-3 py-3 border-b border-rule last:border-b-0 text-sm',
        className
      )}
    >
      {/* Thumbnail */}
      <div className="h-16 w-16 shrink-0 overflow-hidden rounded-[6px] border border-rule bg-paper-2 flex items-center justify-center">
        {item.imageUrl ? (
          <img
            src={item.imageUrl}
            alt={item.name}
            className="h-full w-full object-cover"
          />
        ) : (
          <span className="text-[10px] text-ink-2/50 text-center px-1">Ảnh SP</span>
        )}
      </div>

      {/* Details */}
      <div className="flex-1 min-w-0">
        <h4 className="font-medium text-ink truncate text-sm leading-tight">
          {item.name}
        </h4>
        <div className="flex items-center gap-2 text-xs text-ink-2 mt-0.5">
          <span className="tabular-nums">{formatVnd(item.unitPrice)}</span>
          <span>/</span>
          <span>{item.unit}</span>
          {item.packSize && <span>({item.packSize})</span>}
        </div>
      </div>

      {/* Quantity & Controls */}
      {readOnly ? (
        <div className="text-right">
          <p className="text-xs text-ink-2 tabular-nums">Số lượng: x{item.quantity}</p>
          <p className="font-semibold text-ink tabular-nums mt-0.5">
            {formatVnd(item.lineSubtotal)}
          </p>
        </div>
      ) : (
        <div className="flex items-center gap-3 shrink-0">
          <div className="flex items-center rounded-[6px] border border-rule bg-paper-2">
            <button
              type="button"
              disabled={item.quantity <= 1}
              onClick={() => onUpdateQuantity?.(item.id, item.quantity - 1)}
              className="p-1 hover:bg-rule/30 text-ink disabled:opacity-30 cursor-pointer disabled:cursor-not-allowed"
              title="Giảm số lượng"
            >
              <Minus className="h-3.5 w-3.5 stroke-[1.5]" />
            </button>
            <span className="w-8 text-center text-xs font-semibold tabular-nums text-ink">
              {item.quantity}
            </span>
            <button
              type="button"
              disabled={item.quantity >= 20}
              onClick={() => onUpdateQuantity?.(item.id, item.quantity + 1)}
              className="p-1 hover:bg-rule/30 text-ink disabled:opacity-30 cursor-pointer disabled:cursor-not-allowed"
              title="Tăng số lượng"
            >
              <Plus className="h-3.5 w-3.5 stroke-[1.5]" />
            </button>
          </div>

          <span className="font-semibold text-ink tabular-nums w-24 text-right">
            {formatVnd(item.lineSubtotal)}
          </span>

          <button
            type="button"
            onClick={() => onRemove?.(item.id)}
            className="p-1.5 text-ink-2 hover:text-tomato transition-colors cursor-pointer"
            title="Xóa sản phẩm"
          >
            <Trash2 className="h-4 w-4 stroke-[1.5]" />
          </button>
        </div>
      )}
    </div>
  )
}
