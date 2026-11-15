import React from 'react'
import { cn } from '@/shared/utils/cn'

export interface AisleCategory {
  id: string
  name: string
  slug: string
  productCount?: number
}

interface AisleNavProps {
  categories: AisleCategory[]
  selectedSlug?: string
  onSelectCategory?: (slug: string) => void
  onSelect?: (slug: string) => void
  className?: string
}

export const AisleNav: React.FC<AisleNavProps> = ({
  categories,
  selectedSlug,
  onSelectCategory,
  onSelect,
  className,
}) => {
  const handleSelect = (slug: string) => {
    onSelect?.(slug)
    onSelectCategory?.(slug)
  }

  return (
    <nav
      aria-label="Điều hướng danh mục sản phẩm"
      className={cn(
        'sticky top-[64px] z-30 flex items-center gap-2 overflow-x-auto bg-paper/95 backdrop-blur-sm py-2 px-4 border-b border-rule scrollbar-none',
        className
      )}
    >
      <button
        type="button"
        onClick={() => handleSelect('')}
        className={cn(
          'shrink-0 rounded-full px-3.5 py-1.5 text-xs sm:text-sm font-medium transition-colors cursor-pointer select-none border',
          !selectedSlug
            ? 'bg-leaf text-paper border-leaf font-semibold shadow-xs'
            : 'bg-paper-2 text-ink border-rule hover:border-leaf/50'
        )}
      >
        Tất cả gian hàng
      </button>

      {categories.map((cat) => {
        const isSelected = selectedSlug === cat.slug
        return (
          <button
            key={cat.id}
            type="button"
            onClick={() => handleSelect(cat.slug)}
            className={cn(
              'shrink-0 rounded-full px-3.5 py-1.5 text-xs sm:text-sm font-medium transition-colors cursor-pointer select-none border flex items-center gap-1.5',
              isSelected
                ? 'bg-leaf text-paper border-leaf font-semibold shadow-xs'
                : 'bg-paper-2 text-ink border-rule hover:border-leaf/50'
            )}
          >
            <span>{cat.name}</span>
            {cat.productCount !== undefined && cat.productCount > 0 && (
              <span
                className={cn(
                  'text-[10px] tabular-nums rounded-full px-1.5 py-0.2',
                  isSelected ? 'bg-leaf-2 text-paper' : 'bg-rule/40 text-ink-2'
                )}
              >
                {cat.productCount}
              </span>
            )}
          </button>
        )
      })}
    </nav>
  )
}
