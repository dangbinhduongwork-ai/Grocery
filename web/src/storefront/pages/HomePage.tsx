import React, { useEffect, useState } from 'react'
import { api } from '@/shared/api/client'
import type { StorefrontHomeData, CategoryTreeItem } from '@/shared/api/types'
import { ProductTile } from '@/shared/components/ProductTile'
import { AisleNav } from '@/shared/components/AisleNav'
import { Skeleton } from '@/shared/ui/Skeleton'
import { Button } from '@/shared/ui/Button'
import { vi } from '@/shared/i18n/vi'
import { useCart } from '@/shared/cart/CartContext'
import { useRouter } from '@/app/router'
import { useUiSettings, UI_SETTINGS_EVENT } from '@/shared/hooks/useUiSettings'
import {
  Sparkles,
  TrendingUp,
  Percent,
  Clock,
  ShieldCheck,
  Truck,
  RotateCcw,
  Headphones,
  ChevronRight,
  AlertCircle,
} from 'lucide-react'

interface BannerItem {
  id: string
  title: string
  subtitle?: string | null
  imageUrl: string
  linkUrl?: string | null
}

export const HomePage: React.FC = () => {
  const { navigate } = useRouter()
  const { addToCart } = useCart()
  const { settings: uiSettings } = useUiSettings()

  const [homeData, setHomeData] = useState<StorefrontHomeData | null>(null)
  const [categories, setCategories] = useState<CategoryTreeItem[]>([])
  const [banners, setBanners] = useState<BannerItem[]>([])
  const [activeBannerIdx, setActiveBannerIdx] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadData = async () => {
    setIsLoading(true)
    setError(null)
    try {
      const { data, error: err } = await api.GET('/api/v1/home')

      if (data && !err) {
        setHomeData(data)
        if (data.categories) {
          setCategories(data.categories)
        }
        if (data.banners && data.banners.length > 0) {
          setBanners(data.banners as any)
        } else {
          setBanners([
            {
              id: '1',
              title: uiSettings.heroTitle || 'Nông Sản Tươi Ngon Chuẩn VietGAP',
              subtitle: uiSettings.heroSubtitle || 'Giao nhanh 2h tại Hà Nội & TP. Hồ Chí Minh. Miễn phí vận chuyển từ 299.000đ',
              imageUrl: '',
              linkUrl: '/c/rau-cu-qua',
            },
            {
              id: '2',
              title: 'Trái Cây Nhập Khẩu & Đặc Sản Vùng Miền',
              subtitle: 'Tươi ngọt tự nhiên, cam kết đổi trả trong 24h nếu không hài lòng',
              imageUrl: '',
              linkUrl: '/c/trai-cay-tuoi',
            },
          ])
        }
      } else {
        setError('Không thể tải dữ liệu trang chủ')
      }
    } catch {
      setError('Đã có lỗi xảy ra khi kết nối máy chủ')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadData()

    const handleUpdate = () => {
      void loadData()
    }

    window.addEventListener(UI_SETTINGS_EVENT, handleUpdate)
    window.addEventListener('groceryhub:banners-updated', handleUpdate)
    return () => {
      window.removeEventListener(UI_SETTINGS_EVENT, handleUpdate)
      window.removeEventListener('groceryhub:banners-updated', handleUpdate)
    }
  }, [])

  // Auto rotate banner every 6 seconds
  useEffect(() => {
    if (banners.length <= 1) return
    const timer = setInterval(() => {
      setActiveBannerIdx((prev) => (prev + 1) % banners.length)
    }, 6000)
    return () => clearInterval(timer)
  }, [banners.length])

  if (isLoading) {
    return (
      <div className="space-y-8 animate-pulse">
        {/* Banner Skeleton */}
        <div className="h-64 sm:h-80 md:h-96 rounded-2xl bg-paper-2 border border-rule" />
        {/* Aisle Skeleton */}
        <div className="flex gap-4 overflow-hidden py-2">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-10 w-32 rounded-full" />
          ))}
        </div>
        {/* Product Grid Skeleton */}
        <div className="space-y-4">
          <Skeleton className="h-8 w-48" />
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
            {Array.from({ length: 8 }).map((_, i) => (
              <div key={i} className="h-72 rounded-xl bg-paper-2 border border-rule p-4 space-y-3">
                <Skeleton className="h-40 w-full rounded-lg" />
                <Skeleton className="h-4 w-3/4" />
                <Skeleton className="h-4 w-1/2" />
                <Skeleton className="h-8 w-full rounded-md" />
              </div>
            ))}
          </div>
        </div>
      </div>
    )
  }

  if (error && !homeData) {
    return (
      <div className="text-center py-16 bg-paper-2 rounded-2xl border border-rule max-w-lg mx-auto">
        <AlertCircle className="w-12 h-12 text-tomato mx-auto mb-3 stroke-[1.5]" />
        <h3 className="font-display font-semibold text-lg text-ink mb-1">{error}</h3>
        <p className="text-sm text-ink-2 mb-6">Vui lòng kiểm tra lại kết nối mạng hoặc thử lại.</p>
        <Button variant="primary" onClick={() => void loadData()}>
          Thử lại
        </Button>
      </div>
    )
  }

  const currentBanner = banners[activeBannerIdx] || banners[0]

  return (
    <div className="space-y-10 sm:space-y-12">
      {/* Hero Banner Section */}
      <section className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-leaf to-[#144723] text-paper shadow-md min-h-[320px] sm:min-h-[380px] md:min-h-[420px] flex items-center">
        {currentBanner?.imageUrl && (
          <div className="absolute inset-0 z-0 overflow-hidden">
            <img
              src={currentBanner.imageUrl}
              alt={currentBanner.title}
              className="w-full h-full object-cover object-center scale-100 transition-all duration-700"
            />
            {/* Gradient phủ mờ nhẹ bên trái để chữ trắng dễ đọc, còn lại 2/3 diện tích banner hiển thị nguyên bản 100% màu sắc ảnh tải lên */}
            <div className="absolute inset-0 bg-gradient-to-r from-slate-950/85 via-slate-900/40 to-transparent sm:w-3/5 w-full" />
          </div>
        )}
        <div className="p-6 sm:p-10 md:p-14 max-w-2xl relative z-10 space-y-4">
          <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-paper/20 text-paper text-xs font-semibold backdrop-blur-xs">
            <Sparkles className="w-3.5 h-3.5 stroke-[1.5]" />
            <span>{uiSettings.heroBadge || 'Nông sản tươi sạch mỗi ngày'}</span>
          </div>
          <h1 className="font-display font-bold text-2xl sm:text-4xl md:text-5xl leading-tight text-paper drop-shadow-md">
            {currentBanner?.title ?? uiSettings.heroTitle ?? 'Nông Sản Tươi Ngon Chuẩn VietGAP'}
          </h1>
          {currentBanner?.subtitle ? (
            <p className="text-sm sm:text-base text-paper/95 leading-relaxed font-sans max-w-xl drop-shadow-sm">
              {currentBanner.subtitle}
            </p>
          ) : uiSettings.heroSubtitle ? (
            <p className="text-sm sm:text-base text-paper/95 leading-relaxed font-sans max-w-xl drop-shadow-sm">
              {uiSettings.heroSubtitle}
            </p>
          ) : null}
          <div className="pt-2 flex flex-wrap items-center gap-3">
            <Button
              variant="outline"
              size="lg"
              className="bg-paper text-leaf border-none hover:bg-paper-2 font-semibold shadow-xs cursor-pointer"
              onClick={() => navigate(currentBanner?.linkUrl || '/c/rau-cu-qua')}
            >
              <span>{vi.home.shopNow}</span>
              <ChevronRight className="w-4 h-4 ml-1 stroke-[1.5]" />
            </Button>
            <Button
              variant="ghost"
              size="lg"
              className="text-paper hover:bg-paper/20 border border-paper/40 cursor-pointer backdrop-blur-xs"
              onClick={() => navigate('/c/tat-ca')}
            >
              Xem tất cả danh mục
            </Button>
          </div>
        </div>

        {/* Carousel indicator dots */}
        {banners.length > 1 && (
          <div className="absolute bottom-4 right-6 z-10 flex gap-2">
            {banners.map((b, idx) => (
              <button
                key={b.id}
                type="button"
                aria-label={`Banner ${idx + 1}`}
                onClick={() => setActiveBannerIdx(idx)}
                className={`h-2 rounded-full transition-all cursor-pointer ${
                  activeBannerIdx === idx ? 'w-6 bg-paper' : 'w-2 bg-paper/40'
                }`}
              />
            ))}
          </div>
        )}
      </section>

      {/* Trust Badges Bar */}
      <section className="grid grid-cols-2 md:grid-cols-4 gap-3 sm:gap-4 py-2">
        <div className="flex items-center gap-3 p-3.5 rounded-xl bg-paper-2 border border-rule">
          <Truck className="w-6 h-6 text-leaf shrink-0 stroke-[1.5]" />
          <div>
            <h4 className="text-xs sm:text-sm font-semibold text-ink">Giao nhanh 2 giờ</h4>
            <p className="text-[11px] text-ink-2">Áp dụng nội thành Hà Nội & TP.HCM</p>
          </div>
        </div>
        <div className="flex items-center gap-3 p-3.5 rounded-xl bg-paper-2 border border-rule">
          <ShieldCheck className="w-6 h-6 text-leaf shrink-0 stroke-[1.5]" />
          <div>
            <h4 className="text-xs sm:text-sm font-semibold text-ink">100% VietGAP / Organic</h4>
            <p className="text-[11px] text-ink-2">Rõ xuất xứ, đạt kiểm định</p>
          </div>
        </div>
        <div className="flex items-center gap-3 p-3.5 rounded-xl bg-paper-2 border border-rule">
          <RotateCcw className="w-6 h-6 text-leaf shrink-0 stroke-[1.5]" />
          <div>
            <h4 className="text-xs sm:text-sm font-semibold text-ink">Đổi trả 24 giờ</h4>
            <p className="text-[11px] text-ink-2">Đổi mới nếu không hài lòng</p>
          </div>
        </div>
        <div className="flex items-center gap-3 p-3.5 rounded-xl bg-paper-2 border border-rule">
          <Headphones className="w-6 h-6 text-leaf shrink-0 stroke-[1.5]" />
          <div>
            <h4 className="text-xs sm:text-sm font-semibold text-ink">Hỗ trợ 7:00 - 22:00</h4>
            <p className="text-[11px] text-ink-2">Hotline tư vấn: 0865 087 784</p>
          </div>
        </div>
      </section>

      {/* Aisle Category Navigation */}
      {categories.length > 0 && (
        <section className="space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="font-display font-semibold text-xl sm:text-2xl text-ink">
              Đi chợ theo gian hàng
            </h2>
            <button
              type="button"
              onClick={() => navigate('/c/tat-ca')}
              className="text-xs sm:text-sm font-semibold text-leaf hover:underline flex items-center gap-1 cursor-pointer"
            >
              Xem tất cả <ChevronRight className="w-3.5 h-3.5 stroke-[1.5]" />
            </button>
          </div>
          <AisleNav
            categories={categories.map((c) => ({
              id: c.id,
              name: c.name,
              slug: c.slug,
            }))}
            onSelect={(slug) => navigate(`/c/${slug}`)}
          />
        </section>
      )}

      {/* Featured Products Section */}
      {homeData?.featuredProducts && homeData.featuredProducts.length > 0 && (
        <section className="space-y-4">
          <div className="flex items-center justify-between border-b border-rule pb-3">
            <div className="flex items-center gap-2">
              <Sparkles className="w-5 h-5 text-leaf stroke-[1.5]" />
              <h2 className="font-display font-semibold text-xl sm:text-2xl text-ink">
                {vi.home.featuredProducts}
              </h2>
            </div>
            <button
              type="button"
              onClick={() => navigate('/c/tat-ca?sort=relevance')}
              className="text-xs sm:text-sm font-semibold text-leaf hover:underline flex items-center gap-1 cursor-pointer"
            >
              Xem thêm <ChevronRight className="w-3.5 h-3.5 stroke-[1.5]" />
            </button>
          </div>
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
            {homeData.featuredProducts.map((p) => (
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
        </section>
      )}

      {/* On-Sale / Promotions Section */}
      {homeData?.onSaleProducts && homeData.onSaleProducts.length > 0 && (
        <section className="space-y-4 p-5 sm:p-6 rounded-2xl bg-[#FFF6F3] border border-[#FADCD5]">
          <div className="flex items-center justify-between border-b border-[#FADCD5] pb-3">
            <div className="flex items-center gap-2">
              <Percent className="w-5 h-5 text-tomato stroke-[1.5]" />
              <h2 className="font-display font-semibold text-xl sm:text-2xl text-ink">
                {vi.home.onSaleProducts}
              </h2>
            </div>
            <button
              type="button"
              onClick={() => navigate('/c/tat-ca?onSale=true')}
              className="text-xs sm:text-sm font-semibold text-tomato hover:underline flex items-center gap-1 cursor-pointer"
            >
              Xem ưu đãi <ChevronRight className="w-3.5 h-3.5 stroke-[1.5]" />
            </button>
          </div>
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
            {homeData.onSaleProducts.map((p) => (
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
        </section>
      )}

      {/* Best Sellers Section */}
      {homeData?.bestSellers && homeData.bestSellers.length > 0 && (
        <section className="space-y-4">
          <div className="flex items-center justify-between border-b border-rule pb-3">
            <div className="flex items-center gap-2">
              <TrendingUp className="w-5 h-5 text-leaf stroke-[1.5]" />
              <h2 className="font-display font-semibold text-xl sm:text-2xl text-ink">
                {vi.home.bestSellers}
              </h2>
            </div>
            <button
              type="button"
              onClick={() => navigate('/c/tat-ca?sort=best_selling')}
              className="text-xs sm:text-sm font-semibold text-leaf hover:underline flex items-center gap-1 cursor-pointer"
            >
              Xem thêm <ChevronRight className="w-3.5 h-3.5 stroke-[1.5]" />
            </button>
          </div>
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
            {homeData.bestSellers.map((p) => (
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
        </section>
      )}

      {/* New Arrivals Section */}
      {homeData?.newArrivals && homeData.newArrivals.length > 0 && (
        <section className="space-y-4">
          <div className="flex items-center justify-between border-b border-rule pb-3">
            <div className="flex items-center gap-2">
              <Clock className="w-5 h-5 text-leaf stroke-[1.5]" />
              <h2 className="font-display font-semibold text-xl sm:text-2xl text-ink">
                {vi.home.newArrivals}
              </h2>
            </div>
            <button
              type="button"
              onClick={() => navigate('/c/tat-ca?sort=newest')}
              className="text-xs sm:text-sm font-semibold text-leaf hover:underline flex items-center gap-1 cursor-pointer"
            >
              Xem thêm <ChevronRight className="w-3.5 h-3.5 stroke-[1.5]" />
            </button>
          </div>
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
            {homeData.newArrivals.map((p) => (
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
        </section>
      )}
    </div>
  )
}
