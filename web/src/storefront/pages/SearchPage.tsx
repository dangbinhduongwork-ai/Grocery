import React, { useEffect, useState } from 'react'
import { api } from '@/shared/api/client'
import type { StorefrontProductCard } from '@/shared/api/types'
import { ProductTile } from '@/shared/components/ProductTile'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/shared/ui/Select'
import { Skeleton } from '@/shared/ui/Skeleton'
import { useCart } from '@/shared/cart/CartContext'
import { useRouter } from '@/app/router'
import { Search, PackageOpen, ArrowUpDown, Tag } from 'lucide-react'

export const SearchPage: React.FC = () => {
  const { navigate, searchParams } = useRouter()
  const { addToCart } = useCart()

  const query = searchParams.get('q') || ''
  const sort = searchParams.get('sort') || 'relevance'
  const page = parseInt(searchParams.get('page') || '1', 10)

  const [searchInput, setSearchInput] = useState(query)
  const [products, setProducts] = useState<StorefrontProductCard[]>([])
  const [totalCount, setTotalCount] = useState<number>(0)
  const [totalPages, setTotalPages] = useState<number>(1)
  const [isLoading, setIsLoading] = useState<boolean>(true)

  useEffect(() => {
    setSearchInput(query)
  }, [query])

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
        const { data } = await api.GET('/api/v1/products', {
          params: {
            query: {
              search: query,
              sort,
              page: page.toString(),
              pageSize: '16',
            } as any,
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
  }, [query, sort, page])

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (searchInput.trim()) {
      navigate(`/search?q=${encodeURIComponent(searchInput.trim())}`)
    }
  }

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
          <Button type="submit" variant="primary" className="h-10 px-5 text-sm font-semibold">
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
              onClick={() => navigate(`/search?q=${encodeURIComponent(kw)}`)}
              className="px-2.5 py-1 rounded-full bg-paper hover:bg-[#E8EDE5] text-ink hover:text-leaf border border-rule transition-colors cursor-pointer"
            >
              {kw}
            </button>
          ))}
        </div>
      </div>

      {/* Results Header */}
      {query && (
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-rule pb-4">
          <div>
            <h1 className="font-display font-semibold text-xl text-ink">
              Kết quả tìm kiếm cho: <span className="text-leaf">"{query}"</span>
            </h1>
            <p className="text-xs text-ink-2 mt-1">
              Tìm thấy <span className="font-semibold text-ink tabular-nums">{totalCount}</span> sản
              phẩm
            </p>
          </div>

          <div className="flex items-center gap-2">
            <span className="text-xs text-ink-2 flex items-center gap-1">
              <ArrowUpDown className="w-3.5 h-3.5 stroke-[1.5]" />
              Sắp xếp:
            </span>
            <Select
              value={sort}
              onValueChange={(val) =>
                navigate(`/search?q=${encodeURIComponent(query)}&sort=${val}`)
              }
            >
              <SelectTrigger className="w-[180px] h-9 text-xs">
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
        <div className="text-center py-16 bg-paper-2 rounded-2xl border border-rule px-4">
          <PackageOpen className="w-12 h-12 text-ink-2/60 mx-auto mb-3 stroke-[1.5]" />
          <h3 className="font-display font-semibold text-base text-ink mb-1">
            Không tìm thấy sản phẩm nào khớp với "{query}"
          </h3>
          <p className="text-xs text-ink-2 max-w-sm mx-auto mb-4">
            Hãy thử kiểm tra lại chính tả hoặc tìm theo từ khóa rộng hơn (ví dụ: "rau", "thịt", "trứng").
          </p>
          <div className="flex flex-wrap justify-center gap-2">
            {suggestedKeywords.slice(0, 4).map((kw) => (
              <Button
                key={kw}
                variant="outline"
                size="sm"
                onClick={() => navigate(`/search?q=${encodeURIComponent(kw)}`)}
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
            onClick={() =>
              navigate(
                `/search?q=${encodeURIComponent(query)}&sort=${sort}&page=${(page - 1).toString()}`
              )
            }
            className="text-xs h-8"
          >
            Trang trước
          </Button>
          <div className="flex items-center gap-1">
            {Array.from({ length: totalPages }, (_, i) => i + 1).map((pNum) => (
              <button
                key={pNum}
                type="button"
                onClick={() =>
                  navigate(
                    `/search?q=${encodeURIComponent(query)}&sort=${sort}&page=${pNum.toString()}`
                  )
                }
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
            onClick={() =>
              navigate(
                `/search?q=${encodeURIComponent(query)}&sort=${sort}&page=${(page + 1).toString()}`
              )
            }
            className="text-xs h-8"
          >
            Trang sau
          </Button>
        </div>
      )}
    </div>
  )
}
