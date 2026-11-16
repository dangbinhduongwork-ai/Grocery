import React, { useEffect, useState } from 'react'
import { api } from '@/shared/api/client'
import type { StorefrontProductCard, CategoryTreeItem } from '@/shared/api/types'
import { ProductTile } from '@/shared/components/ProductTile'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/shared/ui/Select'
import { Skeleton } from '@/shared/ui/Skeleton'
import { useCart } from '@/shared/cart/CartContext'
import { useRouter } from '@/app/router'
import {
  Search,
  PackageOpen,
  ArrowUpDown,
  Tag,
  Filter,
  SlidersHorizontal,
  X,
} from 'lucide-react'

export const SearchPage: React.FC = () => {
  const { navigate, searchParams } = useRouter()
  const { addToCart } = useCart()

  const query = searchParams.get('q') || ''
  const categorySlug = searchParams.get('category') || ''
  const sort = searchParams.get('sort') || 'relevance'
  const inStock = searchParams.get('inStock') === 'true'
  const minPrice = searchParams.get('minPrice') || ''
  const maxPrice = searchParams.get('maxPrice') || ''
  const page = parseInt(searchParams.get('page') || '1', 10)

  const [searchInput, setSearchInput] = useState(query)
  const [categories, setCategories] = useState<CategoryTreeItem[]>([])
  const [products, setProducts] = useState<StorefrontProductCard[]>([])
  const [totalCount, setTotalCount] = useState<number>(0)
  const [totalPages, setTotalPages] = useState<number>(1)
  const [isLoading, setIsLoading] = useState<boolean>(true)
  const [showFilters, setShowFilters] = useState<boolean>(false)

  // Local price filter state
  const [localMinPrice, setLocalMinPrice] = useState(minPrice)
  const [localMaxPrice, setLocalMaxPrice] = useState(maxPrice)

  useEffect(() => {
    setSearchInput(query)
  }, [query])

  useEffect(() => {
    setLocalMinPrice(minPrice)
    setLocalMaxPrice(maxPrice)
  }, [minPrice, maxPrice])

  // Fetch categories for filtering
  useEffect(() => {
    async function fetchCategories() {
      try {
        const { data } = await api.GET('/api/v1/categories')
        if (data) setCategories(data)
      } catch {
        // silent fallback
      }
    }
    void fetchCategories()
  }, [])

  // Execute search and filtering
  useEffect(() => {
    async function executeSearch() {
      if (!query.trim()) {
        setProducts([])
        setTotalCount(0)
        setIsLoading(false)
        return
      }

      setIsLoading(true)
      try {
        const queryParams: Record<string, string> = {
          q: query.trim(),
          search: query.trim(),
          sort,
          page: page.toString(),
          pageSize: '16',
        }

        if (categorySlug) {
          queryParams.categorySlug = categorySlug
        }
        if (inStock) {
          queryParams.inStockOnly = 'true'
          queryParams.inStock = 'true'
        }
        if (minPrice) {
          queryParams.minPrice = minPrice
        }
        if (maxPrice) {
          queryParams.maxPrice = maxPrice
        }

        const { data } = await api.GET('/api/v1/products', {
          params: {
            query: queryParams as any,
          },
        })

        if (data) {
          setProducts(data.items || [])
          setTotalCount(Number(data.totalItems || 0))
          setTotalPages(Number(data.totalPages || 1))
        }
      } catch {
        setProducts([])
      } finally {
        setIsLoading(false)
      }
    }

    void executeSearch()
  }, [query, categorySlug, sort, inStock, minPrice, maxPrice, page])

  const updateFilters = (newParams: Record<string, string | null>) => {
    const nextParams = new URLSearchParams(searchParams.toString())
    for (const [key, val] of Object.entries(newParams)) {
      if (val === null || val === '') {
        nextParams.delete(key)
      } else {
        nextParams.set(key, val)
      }
    }
    if (!('page' in newParams)) {
      nextParams.delete('page')
    }
    const qStr = nextParams.toString()
    navigate(`/search${qStr ? `?${qStr}` : ''}`)
  }

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (searchInput.trim()) {
      updateFilters({ q: searchInput.trim(), page: null })
    }
  }

  const handleApplyPrice = (e: React.FormEvent) => {
    e.preventDefault()
    updateFilters({
      minPrice: localMinPrice || null,
      maxPrice: localMaxPrice || null,
    })
  }

  const handleClearAllFilters = () => {
    setLocalMinPrice('')
    setLocalMaxPrice('')
    navigate(`/search?q=${encodeURIComponent(query)}`)
  }

  const hasActiveFilters = Boolean(categorySlug || inStock || minPrice || maxPrice || sort !== 'relevance')

  const pricePresets = [
    { label: 'Dưới 50.000 ₫', min: '', max: '50000' },
    { label: '50k - 100k', min: '50000', max: '100000' },
    { label: '100k - 200k', min: '100000', max: '200000' },
    { label: 'Trên 200.000 ₫', min: '200000', max: '' },
  ]

  const suggestedKeywords = [
    'Rau cải xanh',
    'Cà chua beef',
    'Thịt ba chỉ heo',
    'Trứng gà ta',
    'Gạo ST25',
    'Bơ sáp Đắk Lắk',
  ]

  return (
    <div className="space-y-6">
      {/* Search Bar Banner */}
      <div className="bg-paper-2 p-6 rounded-2xl border border-rule space-y-4">
        <form onSubmit={handleSearchSubmit} className="flex gap-2 max-w-2xl">
          <div className="relative flex-1">
            <Search className="w-4 h-4 text-ink-2 absolute left-3.5 top-1/2 -translate-y-1/2 stroke-[1.5]" />
            <Input
              type="text"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder="Tìm kiếm rau củ quả, thịt cá, đồ tươi sống..."
              className="pl-10 h-10 text-sm bg-paper"
            />
          </div>
          <Button type="submit" variant="primary" className="h-10 px-5 text-sm font-semibold cursor-pointer">
            Tìm kiếm
          </Button>
        </form>

        {/* Suggestion Chips */}
        <div className="flex flex-wrap items-center gap-2 pt-1 text-xs text-ink-2">
          <span className="flex items-center gap-1 font-semibold text-ink">
            <Tag className="w-3.5 h-3.5 text-leaf stroke-[1.5]" /> Gợi ý:
          </span>
          {suggestedKeywords.map((kw) => (
            <button
              key={kw}
              type="button"
              onClick={() => updateFilters({ q: kw, page: null })}
              className="px-2.5 py-1 rounded-full bg-paper hover:bg-[#E8EDE5] text-ink hover:text-leaf border border-rule transition-colors cursor-pointer"
            >
              {kw}
            </button>
          ))}
        </div>
      </div>

      {/* Results Header with Sorting and Filter Controls */}
      {query && (
        <div className="space-y-4">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-rule pb-4">
            <div>
              <h1 className="font-display font-semibold text-xl text-ink">
                Kết quả tìm kiếm cho: <span className="text-leaf">"{query}"</span>
              </h1>
              <p className="text-xs text-ink-2 mt-1">
                Tìm thấy <span className="font-semibold text-ink tabular-nums">{totalCount}</span> sản phẩm
                {categorySlug && (
                  <span className="ml-1 text-leaf font-medium">
                    (thuộc danh mục: {categories.find((c) => c.slug === categorySlug)?.name || categorySlug})
                  </span>
                )}
              </p>
            </div>

            <div className="flex items-center gap-2 flex-wrap">
              <Button
                variant={showFilters ? 'primary' : 'outline'}
                size="sm"
                onClick={() => setShowFilters(!showFilters)}
                className="h-9 text-xs flex items-center gap-1.5 cursor-pointer"
              >
                <SlidersHorizontal className="w-3.5 h-3.5" />
                <span>Bộ lọc nâng cao</span>
                {hasActiveFilters && (
                  <span className="w-2 h-2 rounded-full bg-amber-400 ml-0.5" />
                )}
              </Button>

              <div className="flex items-center gap-1.5">
                <span className="text-xs text-ink-2 flex items-center gap-1">
                  <ArrowUpDown className="w-3.5 h-3.5 stroke-[1.5]" />
                  Sắp xếp:
                </span>
                <Select
                  value={sort}
                  onValueChange={(val) => updateFilters({ sort: val })}
                >
                  <SelectTrigger className="w-[170px] h-9 text-xs bg-paper">
                    <SelectValue placeholder="Sắp xếp theo" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="relevance">Phù hợp nhất</SelectItem>
                    <SelectItem value="price_asc">Giá: Thấp đến Cao</SelectItem>
                    <SelectItem value="price_desc">Giá: Cao đến Thấp</SelectItem>
                    <SelectItem value="newest">Mới nhất</SelectItem>
                    <SelectItem value="best_selling">Bán chạy nhất</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </div>

          {/* Collapsible Filter Bar */}
          {(showFilters || hasActiveFilters) && (
            <div className="p-4 bg-paper-2 rounded-2xl border border-rule space-y-4">
              <div className="flex items-center justify-between border-b border-rule/60 pb-2">
                <div className="flex items-center gap-2 text-xs font-bold text-ink">
                  <Filter className="w-4 h-4 text-leaf" />
                  <span>Bộ lọc tìm kiếm sản phẩm</span>
                </div>
                {hasActiveFilters && (
                  <button
                    type="button"
                    onClick={handleClearAllFilters}
                    className="text-xs text-tomato hover:underline flex items-center gap-1 cursor-pointer font-medium"
                  >
                    <X className="w-3.5 h-3.5" />
                    Xóa tất cả bộ lọc
                  </button>
                )}
              </div>

              {/* Category Filter Chips */}
              <div className="space-y-1.5">
                <label className="text-[11px] font-semibold text-ink-2 uppercase tracking-wider block">
                  Danh mục sản phẩm:
                </label>
                <div className="flex flex-wrap gap-1.5">
                  <button
                    type="button"
                    onClick={() => updateFilters({ category: null })}
                    className={`px-3 py-1.5 rounded-xl text-xs font-medium transition-all cursor-pointer ${
                      !categorySlug
                        ? 'bg-leaf text-white font-semibold shadow-2xs'
                        : 'bg-paper text-ink hover:bg-[#E8EDE5] border border-rule'
                    }`}
                  >
                    Tất cả danh mục
                  </button>
                  {categories.map((cat) => (
                    <button
                      key={cat.id}
                      type="button"
                      onClick={() => updateFilters({ category: cat.slug })}
                      className={`px-3 py-1.5 rounded-xl text-xs font-medium transition-all cursor-pointer ${
                        categorySlug === cat.slug
                          ? 'bg-leaf text-white font-semibold shadow-2xs'
                          : 'bg-paper text-ink hover:bg-[#E8EDE5] border border-rule'
                      }`}
                    >
                      {cat.name}
                    </button>
                  ))}
                </div>
              </div>

              {/* Price Filter & Stock Toggle */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-2 border-t border-rule/60">
                {/* Price Presets & Range */}
                <div className="space-y-2">
                  <label className="text-[11px] font-semibold text-ink-2 uppercase tracking-wider block">
                    Khoảng giá (VNĐ):
                  </label>
                  <div className="flex flex-wrap gap-1.5">
                    {pricePresets.map((preset) => {
                      const isActive = minPrice === preset.min && maxPrice === preset.max
                      return (
                        <button
                          key={preset.label}
                          type="button"
                          onClick={() => {
                            if (isActive) {
                              updateFilters({ minPrice: null, maxPrice: null })
                            } else {
                              updateFilters({ minPrice: preset.min || null, maxPrice: preset.max || null })
                            }
                          }}
                          className={`px-2.5 py-1 rounded-lg text-xs font-medium transition-colors cursor-pointer ${
                            isActive
                              ? 'bg-leaf text-white font-semibold'
                              : 'bg-paper text-ink-2 hover:text-ink border border-rule'
                          }`}
                        >
                          {preset.label}
                        </button>
                      )
                    })}
                  </div>

                  <form onSubmit={handleApplyPrice} className="flex items-center gap-2 pt-1 max-w-sm">
                    <Input
                      type="number"
                      placeholder="Từ (₫)"
                      value={localMinPrice}
                      onChange={(e) => setLocalMinPrice(e.target.value)}
                      className="h-8 text-xs bg-paper font-mono"
                    />
                    <span className="text-xs text-ink-2">—</span>
                    <Input
                      type="number"
                      placeholder="Đến (₫)"
                      value={localMaxPrice}
                      onChange={(e) => setLocalMaxPrice(e.target.value)}
                      className="h-8 text-xs bg-paper font-mono"
                    />
                    <Button type="submit" variant="outline" size="sm" className="h-8 text-xs px-3 cursor-pointer">
                      Áp dụng
                    </Button>
                  </form>
                </div>

                {/* In Stock Only Option */}
                <div className="space-y-2 flex flex-col justify-center">
                  <label className="text-[11px] font-semibold text-ink-2 uppercase tracking-wider block">
                    Tình trạng hàng hóa:
                  </label>
                  <label className="inline-flex items-center gap-2 cursor-pointer select-none text-xs text-ink">
                    <input
                      type="checkbox"
                      checked={inStock}
                      onChange={(e) => updateFilters({ inStock: e.target.checked ? 'true' : null })}
                      className="w-4 h-4 rounded text-leaf focus:ring-leaf accent-leaf cursor-pointer"
                    />
                    <span className="font-medium">Chỉ hiển thị sản phẩm còn hàng trong kho</span>
                  </label>
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Main Content Area */}
      {isLoading ? (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <div
              key={i}
              className="h-72 rounded-xl bg-paper-2 border border-rule p-4 space-y-3 animate-pulse"
            >
              <Skeleton className="h-40 w-full rounded-lg" />
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-4 w-1/2" />
              <Skeleton className="h-8 w-full rounded-md" />
            </div>
          ))}
        </div>
      ) : !query.trim() ? (
        <div className="text-center py-16 bg-paper-2 rounded-2xl border border-rule px-4">
          <Search className="w-12 h-12 text-ink-2/40 mx-auto mb-3 stroke-[1.5]" />
          <h3 className="font-display font-semibold text-base text-ink mb-1">
            Nhập từ khóa để bắt đầu tìm kiếm
          </h3>
          <p className="text-xs text-ink-2 max-w-sm mx-auto">
            GroceryHub cung cấp đầy đủ các mặt hàng nông sản tươi ngon, thịt cá an toàn và gia vị hàng ngày.
          </p>
        </div>
      ) : products.length === 0 ? (
        <div className="text-center py-16 bg-paper-2 rounded-2xl border border-rule px-4 space-y-3">
          <PackageOpen className="w-12 h-12 text-ink-2/60 mx-auto stroke-[1.5]" />
          <h3 className="font-display font-semibold text-base text-ink">
            Không tìm thấy sản phẩm nào phù hợp với bộ lọc hiện tại
          </h3>
          <p className="text-xs text-ink-2 max-w-sm mx-auto">
            Hãy thử nới lỏng bộ lọc khoảng giá, chọn "Tất cả danh mục" hoặc tìm theo từ khóa rộng hơn.
          </p>
          <div className="flex flex-wrap justify-center gap-2 pt-2">
            {hasActiveFilters && (
              <Button variant="primary" size="sm" onClick={handleClearAllFilters} className="text-xs">
                Xóa tất cả bộ lọc
              </Button>
            )}
            {suggestedKeywords.slice(0, 3).map((kw) => (
              <Button
                key={kw}
                variant="outline"
                size="sm"
                onClick={() => updateFilters({ q: kw, category: null, minPrice: null, maxPrice: null, page: null })}
                className="text-xs"
              >
                {kw}
              </Button>
            ))}
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
          {products.map((p) => (
            <ProductTile
              key={p.id}
              product={p}
              onSelect={() => navigate(`/p/${p.slug}`)}
              onAddToCart={(item) => {
                void addToCart(item.id, 1)
              }}
            />
          ))}
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2 pt-6 border-t border-rule">
          <Button
            variant="outline"
            size="sm"
            disabled={page <= 1}
            onClick={() => updateFilters({ page: (page - 1).toString() })}
            className="text-xs h-8 cursor-pointer"
          >
            Trang trước
          </Button>
          <div className="flex items-center gap-1">
            {Array.from({ length: totalPages }, (_, i) => i + 1).map((pNum) => (
              <button
                key={pNum}
                type="button"
                onClick={() => updateFilters({ page: pNum.toString() })}
                className={`w-8 h-8 rounded-md text-xs font-semibold tabular-nums cursor-pointer transition-colors ${
                  page === pNum
                    ? 'bg-leaf text-paper'
                    : 'text-ink hover:bg-paper-2 border border-rule'
                }`}
              >
                {pNum}
              </button>
            ))}
          </div>
          <Button
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => updateFilters({ page: (page + 1).toString() })}
            className="text-xs h-8 cursor-pointer"
          >
            Trang sau
          </Button>
        </div>
      )}
    </div>
  )
}
