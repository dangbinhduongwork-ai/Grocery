import React, { useState, useEffect, useRef } from 'react'
import { Search, X, Loader2, ArrowRight, Package, Sparkles } from 'lucide-react'
import { api } from '@/shared/api/client'
import type { components } from '@/shared/api/schema'
import { formatVnd } from '@/shared/utils/formatVnd'
import { cn } from '@/shared/utils/cn'

type ProductCard = components['schemas']['StorefrontProductCardDto']

interface StorefrontSearchBarProps {
  initialValue?: string
  placeholder?: string
  isMobile?: boolean
  onNavigate?: (path: string) => void
}

export const StorefrontSearchBar: React.FC<StorefrontSearchBarProps> = ({
  initialValue = '',
  placeholder = 'Tìm kiếm rau, củ, thịt cá, sữa, gia vị...',
  isMobile = false,
  onNavigate,
}) => {
  const [query, setQuery] = useState(initialValue)
  const [isOpen, setIsOpen] = useState(false)
  const [loading, setLoading] = useState(false)
  const [suggestions, setSuggestions] = useState<ProductCard[]>([])
  const [totalItems, setTotalItems] = useState(0)
  const [selectedIndex, setSelectedIndex] = useState<number>(-1)

  const containerRef = useRef<HTMLDivElement | null>(null)
  const inputRef = useRef<HTMLInputElement | null>(null)

  // Sync external initialValue if changed
  useEffect(() => {
    if (initialValue !== query) {
      setQuery(initialValue)
    }
  }, [initialValue])

  // Click outside to dismiss
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  // Debounce query fetching
  useEffect(() => {
    const trimmed = query.trim()
    if (!trimmed) {
      setSuggestions([])
      setTotalItems(0)
      setLoading(false)
      setIsOpen(false)
      return
    }

    setLoading(true)
    const timer = setTimeout(async () => {
      try {
        const res = await api.GET('/api/v1/products', {
          params: {
            query: {
              q: trimmed,
              pageSize: 6,
            },
          },
        })

        if (res && res.data) {
          const items = res.data.items || []
          setSuggestions(items)
          setTotalItems(Number(res.data.totalItems) || items.length)
          setIsOpen(true)
          setSelectedIndex(-1)
        }
      } catch (err) {
        console.warn('Failed to fetch search suggestions:', err)
        setSuggestions([])
      } finally {
        setLoading(false)
      }
    }, 220)

    return () => clearTimeout(timer)
  }, [query])

  const handleSubmit = (e?: React.FormEvent) => {
    if (e) e.preventDefault()
    setIsOpen(false)
    inputRef.current?.blur()

    if (selectedIndex >= 0 && suggestions[selectedIndex]) {
      const selected = suggestions[selectedIndex]
      onNavigate?.(`/products/${selected.slug}`)
      return
    }

    if (query.trim()) {
      onNavigate?.(`/search?q=${encodeURIComponent(query.trim())}`)
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (!isOpen || suggestions.length === 0) {
      if (e.key === 'Enter') {
        handleSubmit()
      }
      return
    }

    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setSelectedIndex((prev) => (prev < suggestions.length - 1 ? prev + 1 : 0))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setSelectedIndex((prev) => (prev > 0 ? prev - 1 : suggestions.length - 1))
    } else if (e.key === 'Enter') {
      e.preventDefault()
      handleSubmit()
    } else if (e.key === 'Escape') {
      setIsOpen(false)
    }
  }

  const handleSelectProduct = (slug: string) => {
    setIsOpen(false)
    onNavigate?.(`/products/${slug}`)
  }

  // Highlight matching keyword
  const highlightMatch = (text: string, keyword: string) => {
    if (!keyword.trim()) return text
    const escaped = keyword.trim().replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
    const regex = new RegExp(`(${escaped})`, 'gi')
    const parts = text.split(regex)

    return parts.map((part, i) =>
      regex.test(part) ? (
        <span key={i} className="text-leaf font-bold bg-sprout/40 rounded-xs px-0.5">
          {part}
        </span>
      ) : (
        part
      )
    )
  }

  return (
    <div ref={containerRef} className="relative w-full">
      <form onSubmit={handleSubmit} className="relative w-full">
        <input
          ref={inputRef}
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onFocus={() => {
            if (query.trim() && suggestions.length > 0) setIsOpen(true)
          }}
          onKeyDown={handleKeyDown}
          placeholder={placeholder}
          className={cn(
            'w-full rounded-full border border-rule bg-paper-2 text-ink placeholder:text-ink-2/60',
            'focus:outline-none focus:border-leaf focus:bg-white focus:ring-2 focus:ring-leaf/20 transition-all shadow-xs',
            isMobile ? 'h-9 pl-9 pr-8 text-xs' : 'h-10 pl-10 pr-9 text-sm'
          )}
        />

        {/* Left Search Icon */}
        <Search
          className={cn(
            'text-ink-2 absolute pointer-events-none stroke-[1.5]',
            isMobile ? 'left-3 top-2.5 w-3.5 h-3.5' : 'left-3.5 top-3 w-4 h-4'
          )}
        />

        {/* Right Action: Loading Spinner or Clear Button */}
        <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-1">
          {loading && (
            <Loader2
              className={cn('animate-spin text-leaf', isMobile ? 'w-3.5 h-3.5' : 'w-4 h-4')}
            />
          )}
          {query && !loading && (
            <button
              type="button"
              onClick={() => {
                setQuery('')
                setSuggestions([])
                setIsOpen(false)
                inputRef.current?.focus()
              }}
              className="text-ink-2/70 hover:text-ink p-0.5 rounded-full hover:bg-paper-2 transition-colors cursor-pointer"
              title="Xóa tìm kiếm"
            >
              <X className={isMobile ? 'w-3.5 h-3.5' : 'w-4 h-4'} />
            </button>
          )}
        </div>
      </form>

      {/* Autocomplete Dropdown */}
      {isOpen && query.trim() && (
        <div className="absolute left-0 right-0 top-full mt-1.5 bg-white border border-rule rounded-2xl shadow-xl z-50 overflow-hidden divide-y divide-rule/60 animate-in fade-in slide-in-from-top-1 duration-150">
          {/* Header */}
          <div className="px-3.5 py-2 bg-paper-2/60 flex items-center justify-between text-xs text-ink-2">
            <span className="flex items-center gap-1.5 font-medium text-ink">
              <Sparkles className="w-3.5 h-3.5 text-sprout-dark" />
              Gợi ý sản phẩm ({suggestions.length})
            </span>
            <span className="text-[11px] text-ink-2/80">
              Dùng phím <kbd className="px-1 py-0.5 bg-paper rounded border border-rule text-[10px]">↑</kbd> <kbd className="px-1 py-0.5 bg-paper rounded border border-rule text-[10px]">↓</kbd> để chọn
            </span>
          </div>

          {/* Suggestions List */}
          {suggestions.length > 0 ? (
            <div className="max-h-80 overflow-y-auto divide-y divide-rule/40 p-1">
              {suggestions.map((item, idx) => {
                const isSelected = selectedIndex === idx
                const sellingPrice = Number(item.sellingPrice)
                const listPrice = Number(item.listPrice)
                const discount = Number(item.discountPercent)
                const hasDiscount = discount > 0 && listPrice > sellingPrice

                return (
                  <div
                    key={item.id}
                    onClick={() => handleSelectProduct(item.slug)}
                    onMouseEnter={() => setSelectedIndex(idx)}
                    className={cn(
                      'flex items-center gap-3 p-2.5 rounded-xl cursor-pointer transition-colors',
                      isSelected ? 'bg-[#F2F6F0]' : 'hover:bg-paper-2/80'
                    )}
                  >
                    {/* Thumbnail */}
                    <div className="w-11 h-11 shrink-0 rounded-lg bg-paper-2 border border-rule/60 overflow-hidden flex items-center justify-center relative">
                      {item.primaryImageUrl ? (
                        <img
                          src={item.primaryImageUrl}
                          alt={item.name}
                          className="w-full h-full object-cover"
                          loading="lazy"
                        />
                      ) : (
                        <Package className="w-5 h-5 text-ink-2/60 stroke-[1.5]" />
                      )}
                      {!item.inStock && (
                        <div className="absolute inset-0 bg-black/40 flex items-center justify-center">
                          <span className="text-[9px] font-bold text-white bg-red-600/90 px-1 py-0.2 rounded-xs">
                            Hết
                          </span>
                        </div>
                      )}
                    </div>

                    {/* Details */}
                    <div className="flex-1 min-w-0">
                      <div className="text-xs sm:text-sm font-medium text-ink truncate">
                        {highlightMatch(item.name, query)}
                      </div>
                      <div className="flex items-center gap-2 mt-0.5">
                        <span className="text-xs font-bold text-leaf">
                          {formatVnd(sellingPrice)}
                        </span>
                        {hasDiscount && (
                          <span className="text-[10px] text-ink-2 line-through">
                            {formatVnd(listPrice)}
                          </span>
                        )}
                        {hasDiscount && (
                          <span className="text-[9px] font-bold text-red-600 bg-red-50 border border-red-200 px-1 rounded-full">
                            -{discount}%
                          </span>
                        )}
                        <span className="text-[10px] text-ink-2">
                          • {item.unit}
                        </span>
                      </div>
                    </div>

                    {/* Right Chevron / Arrow */}
                    <div className="shrink-0 text-ink-2/60 pr-1">
                      <ArrowRight className="w-3.5 h-3.5" />
                    </div>
                  </div>
                )
              })}
            </div>
          ) : (
            <div className="p-6 text-center text-xs text-ink-2">
              <Package className="w-8 h-8 text-ink-2/40 mx-auto mb-2 stroke-[1.5]" />
              <p>Không tìm thấy mặt hàng nào phù hợp với &ldquo;<strong>{query}</strong>&rdquo;</p>
              <p className="text-[11px] text-ink-2/70 mt-1">Thử tìm theo từ khóa đơn giản hơn như &ldquo;rau&rdquo;, &ldquo;thịt&rdquo;, &ldquo;sữa&rdquo;...</p>
            </div>
          )}

          {/* Bottom All Results Action */}
          <div className="p-2 bg-paper-2/40">
            <button
              type="button"
              onClick={() => handleSubmit()}
              className="w-full py-2 px-3 text-xs font-semibold text-leaf hover:bg-leaf/10 rounded-xl flex items-center justify-center gap-1.5 transition-colors cursor-pointer"
            >
              Xem tất cả {totalItems > 0 ? `${totalItems} ` : ''}kết quả cho &ldquo;{query}&rdquo;
              <ArrowRight className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
