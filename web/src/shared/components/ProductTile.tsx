import React from 'react'
import { PriceTag } from './PriceTag'
import { StockBadge } from './StockBadge'
import { Button } from '@/shared/ui/Button'
import { Star, ShoppingBag } from 'lucide-react'
import { cn } from '@/shared/utils/cn'

export interface ProductTileData {
  id: string
  name: string
  slug: string
  sku: string
  listPrice?: number | string | null
  sellingPrice: number | string
  unit: string
  packSize?: string | null
  primaryImageUrl?: string | null
  ratingAvg?: number | string | null
  ratingCount?: number | string | null
  inStock: boolean
  lowStock?: boolean
}

interface ProductTileProps {
  product: ProductTileData
  onSelect?: (slug: string) => void
  onAddToCart?: (product: ProductTileData) => void
  className?: string
}

export const ProductTile: React.FC<ProductTileProps> = ({
  product,
  onSelect,
  onAddToCart,
  className,
}) => {
  return (
    <article
      className={cn(
        'group relative flex flex-col justify-between rounded-[10px] border border-rule bg-paper p-3 transition-all hover:border-leaf/50 hover:shadow-xs',
        className
      )}
    >
      <div>
        <div
          onClick={() => onSelect?.(product.slug)}
          className="relative aspect-square w-full overflow-hidden rounded-[8px] bg-paper-2 cursor-pointer flex items-center justify-center mb-2.5"
        >
          {product.primaryImageUrl ? (
            <img
              src={product.primaryImageUrl}
              alt={product.name}
              loading="lazy"
              className="h-full w-full object-cover object-center transition-transform duration-300 group-hover:scale-105"
            />
          ) : (
            <div className="flex flex-col items-center justify-center text-ink-2/40">
              <ShoppingBag className="w-8 h-8 stroke-[1.5] mb-1" />
              <span className="text-[11px]">Ảnh sản phẩm</span>
            </div>
          )}

          {(!product.inStock || product.lowStock) && (
            <div className="absolute bottom-1.5 left-1.5">
              <StockBadge inStock={product.inStock} lowStock={product.lowStock} />
            </div>
          )}
        </div>

        {/* Rating */}
        {product.ratingCount != null && Number(product.ratingCount) > 0 && (
          <div className="flex items-center gap-1 text-xs text-ink-2 mb-1">
            <Star className="w-3.5 h-3.5 fill-turmeric text-turmeric stroke-0" />
            <span className="font-semibold text-ink tabular-nums">
              {Number(product.ratingAvg ?? 0).toFixed(1)}
            </span>
            <span className="text-[11px] text-ink-2/60">
              ({product.ratingCount})
            </span>
          </div>
        )}

        {/* Title */}
        <h3
          onClick={() => onSelect?.(product.slug)}
          className="text-sm font-medium text-ink line-clamp-2 hover:text-leaf transition-colors cursor-pointer leading-snug mb-1"
          title={product.name}
        >
          {product.name}
        </h3>

        {/* Pack size or unit */}
        {product.packSize && (
          <p className="text-xs text-ink-2/70 mb-2">{product.packSize}</p>
        )}
      </div>

      <div className="mt-3 pt-2 border-t border-rule/50 flex flex-col gap-2">
        <PriceTag
          sellingPrice={product.sellingPrice}
          listPrice={product.listPrice}
          unit={product.unit}
          size="sm"
        />

        <Button
          size="sm"
          variant="outline"
          disabled={!product.inStock}
          onClick={(e) => {
            e.stopPropagation()
            onAddToCart?.(product)
          }}
          className="w-full text-xs hover:bg-leaf hover:text-paper hover:border-leaf"
        >
          <ShoppingBag className="w-3.5 h-3.5 stroke-[1.5]" />
          Thêm vào giỏ
        </Button>
      </div>
    </article>
  )
}
