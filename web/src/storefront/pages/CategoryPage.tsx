import React, { useEffect, useState, useMemo } from 'react'
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
  SlidersHorizontal,
  ChevronRight,
  Filter,
  PackageOpen,
  ArrowUpDown,
  Check,
} from 'lucide-react'

interface CategoryPageProps {
  categorySlug: string
}

export const CategoryPage: React.FC<CategoryPageProps> = ({ categorySlug }) => {
  const { navigate, searchParams } = useRouter()
  const { addToCart } = useCart()

  const [categories, setCategories] = useState<CategoryTreeItem[]>([])
  const [products, setProducts] = useState<StorefrontProductCard[]>([])
  const [totalCount, setTotalCount] = useState<number>(0)
  const [totalPages, setTotalPages] = useState<number>(1)
  const [isLoading, setIsLoading] = useState<boolean>(true)
  const [mobileFilterOpen, setMobileFilterOpen] = useState<boolean>(false)

  // Filters from URL
  const page = parseInt(searchParams.get('page') || '1', 10)
  const sort = searchParams.get('sort') || 'relevance'
  const inStock = searchParams.get('inStock') === 'true'
  const minPrice = searchParams.get('minPrice') || ''
  const maxPrice = searchParams.get('maxPrice') || ''

  // Local filter states for inputs
  const [localMinPrice, setLocalMinPrice] = useState(minPrice)
  const [localMaxPrice, setLocalMaxPrice] = useState(maxPrice)

  // Sync local inputs when URL changes
  useEffect(() => {
    setLocalMinPrice(minPrice)
    setLocalMaxPrice(maxPrice)
  }, [minPrice, maxPrice])

  // Fetch categories tree
  useEffect(() => {
    async function fetchCategories() {
      try {
        const { data } = await api.GET('/api/v1/categories')
        if (data) setCategories(data)
      } catch {
        // silent fail for sidebar
      }
    }
    void fetchCategories()
  }, [])

  // Find current category
  const currentCategory = useMemo(() => {
    if (categorySlug === 'tat-ca') return null
    for (const cat of categories) {
      if (cat.slug === categorySlug) return cat
      if (cat.children && cat.children.length > 0) {
        const sub = cat.children.find((s) => s.slug === categorySlug)
        if (sub) return sub
      }
    }
    return null
  }, [categories, categorySlug])

  // Fetch products
  useEffect(() => {
    async function fetchProducts() {
      setIsLoading(true)
      try {
        const queryParams: Record<string, string> = {
          page: page.toString(),
          pageSize: '16',
          sort,
        }
        if (currentCategory) {
          queryParams.categorySlug = currentCategory.slug
          queryParams.categoryId = currentCategory.id
        } else if (categorySlug && categorySlug !== 'tat-ca') {
          queryParams.categorySlug = categorySlug
        }
        if (inStock) {
          queryParams.inStockOnly = 'true'
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
    void fetchProducts()
  }, [currentCategory, page, sort, inStock, minPrice, maxPrice])

  const updateFilters = (newParams: Record<string, string | null>) => {
    const nextParams = new URLSearchParams(searchParams.toString())
    for (const [key, val] of Object.entries(newParams)) {
      if (val === null || val === '') {
        nextParams.delete(key)
      } else {
        nextParams.set(key, val)
      }
    }
    // Always reset to page 1 when filters change (unless updating page itself)
    if (!('page' in newParams)) {
      nextParams.delete('page')
    }
    const query = nextParams.toString()
    navigate(`/c/${categorySlug}${query ? `?${query}` : ''}`)
  }

  const handleApplyPriceFilter = (e: React.FormEvent) => {
    e.preventDefault()
    updateFilters({
      minPrice: localMinPrice || null,
      maxPrice: localMaxPrice || null,
    })
  }

  const handleClearFilters = () => {
    setLocalMinPrice('')
    setLocalMaxPrice('')
    navigate(`/c/${categorySlug}`)
  }

  const hasActiveFilters = inStock || minPrice || maxPrice || sort !== 'relevance'

  return (
    <div className="space-y-6">
      {/* Breadcrumbs */}
      <nav aria-label="Breadcrumb" className="flex items-center gap-1.5 text-xs text-ink-2">
        <button
          type="button"
          onClick={() => navigate('/')}
          className="hover:text-leaf transition-colors cursor-pointer"
        >
          Trang chủ
        </button>
        <ChevronRight className="w-3.5 h-3.5 stroke-[1.5]" />
        <button
          type="button"
          onClick={() => navigate('/c/tat-ca')}
          className="hover:text-leaf transition-colors cursor-pointer"
        >
          Sản phẩm
        </button>
        {currentCategory && (
          <>
            <ChevronRight className="w-3.5 h-3.5 stroke-[1.5]" />
            <span className="font-semibold text-ink">{currentCategory.name}</span>
          </>
        )}
      </nav>

      {/* Category Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-rule pb-4">
        <div>
          <h1 className="font-display font-bold text-2xl sm:text-3xl text-ink">
            {currentCategory?.name || 'Tất cả sản phẩm'}
          </h1>
          <p className="text-xs sm:text-sm text-ink-2 mt-1">
            Hiển thị <span className="font-semibold text-ink tabular-nums">{totalCount}</span> sản
            phẩm tươi ngon
          </p>
        </div>

        {/* Sort Dropdown & Mobile Filter Trigger */}
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setMobileFilterOpen(!mobileFilterOpen)}
            className="md:hidden flex items-center gap-1.5 text-xs"
          >
            <Filter className="w-3.5 h-3.5 stroke-[1.5]" />
            <span>Bộ lọc</span>
            {hasActiveFilters && (
              <span className="w-2 h-2 rounded-full bg-leaf ml-0.5" />
            )}
          </Button>

          <div className="flex items-center gap-2">
            <span className="text-xs text-ink-2 hidden sm:inline flex items-center gap-1">
              <ArrowUpDown className="w-3.5 h-3.5 stroke-[1.5]" />
              Sắp xếp:
            </span>
            <Select
              value={sort}
              onValueChange={(val) => updateFilters({ sort: val === 'relevance' ? null : val })}
            >
              <SelectTrigger className="w-[180px] h-9 text-xs">
                <SelectValue placeholder="Sắp xếp theo" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="relevance">Phù hợp nhất</SelectItem>
                <SelectItem value="price_asc">Giá: Thấp đến Cao</SelectItem>
                <SelectItem value="price_desc">Giá: Cao đến Thấp</SelectItem>
                <SelectItem value="newest">Hàng mới về</SelectItem>
                <SelectItem value="best_selling">Bán chạy nhất</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-8 items-start">
        {/* Desktop Sidebar Filters */}
        <aside
          className={`space-y-6 bg-paper-2 p-5 rounded-2xl border border-rule ${
            mobileFilterOpen ? 'block' : 'hidden md:block'
          }`}
        >
          <div className="flex items-center justify-between border-b border-rule pb-3">
            <div className="flex items-center gap-2">
              <SlidersHorizontal className="w-4 h-4 text-leaf stroke-[1.5]" />
              <h3 className="font-display font-semibold text-sm text-ink">Bộ lọc tìm kiếm</h3>
            </div>
            {hasActiveFilters && (
              <button
                type="button"
                onClick={handleClearFilters}
                className="text-xs text-tomato hover:underline cursor-pointer"
              >
                Xóa tất cả
              </button>
            )}
          </div>

          {/* Categories tree */}
          <div className="space-y-2">
            <h4 className="text-xs font-semibold text-ink-2 uppercase tracking-wider">Danh mục</h4>
            <ul className="space-y-1 text-xs">
              <li>
                <button
                  type="button"
                  onClick={() => navigate('/c/tat-ca')}
                  className={`w-full text-left py-1.5 px-2 rounded-md transition-colors cursor-pointer flex items-center justify-between ${
                    categorySlug === 'tat-ca'
                      ? 'bg-leaf text-paper font-semibold'
                      : 'text-ink hover:bg-paper'
                  }`}
                >
                  <span>Tất cả sản phẩm</span>
                </button>
              </li>
              {categories.map((c) => (
                <li key={c.id}>
                  <button
                    type="button"
                    onClick={() => navigate(`/c/${c.slug}`)}
                    className={`w-full text-left py-1.5 px-2 rounded-md transition-colors cursor-pointer flex items-center justify-between ${
                      categorySlug === c.slug
                        ? 'bg-leaf text-paper font-semibold'
                        : 'text-ink hover:bg-paper'
                    }`}
                  >
                    <span>{c.name}</span>
                    {categorySlug === c.slug && <Check className="w-3.5 h-3.5 stroke-[2]" />}
                  </button>
                </li>
              ))}
            </ul>
          </div>

          {/* In Stock Filter */}
          <div className="space-y-2 border-t border-rule pt-4">
            <h4 className="text-xs font-semibold text-ink-2 uppercase tracking-wider">Tồn kho</h4>
            <label className="flex items-center gap-2 cursor-pointer text-xs text-ink select-none">
              <input
                type="checkbox"
                checked={inStock}
                onChange={(e) => updateFilters({ inStock: e.target.checked ? 'true' : null })}
                className="rounded border-rule text-leaf focus:ring-leaf w-4 h-4"
              />
              <span>Chỉ hiện sản phẩm còn hàng</span>
            </label>
          </div>

          {/* Price Range Filter */}
          <form onSubmit={handleApplyPriceFilter} className="space-y-3 border-t border-rule pt-4">
            <h4 className="text-xs font-semibold text-ink-2 uppercase tracking-wider">Khoảng giá (₫)</h4>
            <div className="grid grid-cols-2 gap-2">
              <div>
                <label className="text-[11px] text-ink-2 block mb-1">Từ</label>
                <Input
                  type="number"
                  placeholder="0"
                  value={localMinPrice}
                  onChange={(e) => setLocalMinPrice(e.target.value)}
                  className="h-8 text-xs tabular-nums"
                  min="0"
                  step="1000"
                />
              </div>
              <div>
                <label className="text-[11px] text-ink-2 block mb-1">Đến</label>
                <Input
                  type="number"
                  placeholder="500000"
                  value={localMaxPrice}
                  onChange={(e) => setLocalMaxPrice(e.target.value)}
                  className="h-8 text-xs tabular-nums"
                  min="0"
                  step="1000"
                />
              </div>
            </div>
            <Button type="submit" variant="outline" size="sm" className="w-full text-xs h-8">
              Áp dụng giá
            </Button>
          </form>
        </aside>

        {/* Product Grid Area */}
        <main className="md:col-span-3 space-y-6">
          {isLoading ? (
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
              {Array.from({ length: 6 }).map((_, i) => (
                <div key={i} className="h-72 rounded-xl bg-paper-2 border border-rule p-4 space-y-3 animate-pulse">
                  <Skeleton className="h-40 w-full rounded-lg" />
                  <Skeleton className="h-4 w-3/4" />
                  <Skeleton className="h-4 w-1/2" />
                  <Skeleton className="h-8 w-full rounded-md" />
                </div>
              ))}
            </div>
          ) : products.length === 0 ? (
            <div className="text-center py-16 bg-paper-2 rounded-2xl border border-rule px-4">
              <PackageOpen className="w-12 h-12 text-ink-2/60 mx-auto mb-3 stroke-[1.5]" />
              <h3 className="font-display font-semibold text-base text-ink mb-1">
                Không tìm thấy sản phẩm phù hợp
              </h3>
              <p className="text-xs text-ink-2 max-w-sm mx-auto mb-4">
                Hãy thử nới lỏng bộ lọc khoảng giá hoặc xem qua các danh mục hàng tươi sống khác.
              </p>
              {hasActiveFilters && (
                <Button variant="outline" size="sm" onClick={handleClearFilters}>
                  Xóa bộ lọc
                </Button>
              )}
            </div>
          ) : (
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
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
                className="text-xs h-8"
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
                className="text-xs h-8"
              >
                Trang sau
              </Button>
            </div>
          )}
        </main>
      </div>
    </div>
  )
}
