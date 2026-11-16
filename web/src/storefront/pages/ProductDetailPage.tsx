import React, { useEffect, useState } from 'react'
import { api } from '@/shared/api/client'
import type {
  StorefrontProductDetail,
  StorefrontProductCard,
  ProductReviewsData,
} from '@/shared/api/types'
import { PriceTag } from '@/shared/components/PriceTag'
import { StockBadge } from '@/shared/components/StockBadge'
import { DeliveryStrip } from '@/shared/components/DeliveryStrip'
import { ProductTile } from '@/shared/components/ProductTile'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'
import { Skeleton } from '@/shared/ui/Skeleton'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/ui/Dialog'
import { useCart } from '@/shared/cart/CartContext'
import { useAuth } from '@/shared/auth/AuthContext'
import { useRouter } from '@/app/router'
import { vi } from '@/shared/i18n/vi'
import { formatVnd } from '@/shared/utils/formatVnd'
import { formatDateTime } from '@/shared/utils/dateTime'
import {
  Star,
  Plus,
  Minus,
  ShoppingBag,
  ShieldCheck,
  Truck,
  RotateCcw,
  CheckCircle2,
  ChevronRight,
  MessageSquare,
  AlertCircle,
  Sparkles,
} from 'lucide-react'

interface ProductDetailPageProps {
  productSlug: string
}

export const ProductDetailPage: React.FC<ProductDetailPageProps> = ({ productSlug }) => {
  const { navigate } = useRouter()
  const { addToCart } = useCart()
  const { isAuthenticated } = useAuth()

  const [product, setProduct] = useState<StorefrontProductDetail | null>(null)
  const [reviewsData, setReviewsData] = useState<ProductReviewsData | null>(null)
  const [relatedProducts, setRelatedProducts] = useState<StorefrontProductCard[]>([])
  const [selectedImage, setSelectedImage] = useState<string>('')
  const [quantity, setQuantity] = useState<number>(1)
  const [isLoading, setIsLoading] = useState<boolean>(true)
  const [isAdding, setIsAdding] = useState<boolean>(false)
  const [error, setError] = useState<string | null>(null)

  // Write Review Dialog State
  const [reviewDialogOpen, setReviewDialogOpen] = useState<boolean>(false)
  const [reviewRating, setReviewRating] = useState<number>(5)
  const [reviewTitle, setReviewTitle] = useState<string>('')
  const [reviewBody, setReviewBody] = useState<string>('')
  const [reviewOrderItemId, setReviewOrderItemId] = useState<string>('')
  const [isSubmittingReview, setIsSubmittingReview] = useState<boolean>(false)
  const [reviewError, setReviewError] = useState<string | null>(null)
  const [reviewSuccess, setReviewSuccess] = useState<boolean>(false)

  // Fetch product detail and reviews
  useEffect(() => {
    async function fetchProduct() {
      setIsLoading(true)
      setError(null)
      try {
        const { data, error: err } = await api.GET('/api/v1/products/{slug}', {
          params: { path: { slug: productSlug } },
        })

        if (err || !data) {
          setError('Không tìm thấy thông tin sản phẩm')
          return
        }

        setProduct(data)
        const primaryImg = data.images?.find((img) => img.isPrimary)?.url || data.images?.[0]?.url || ''
        setSelectedImage(primaryImg)

        // Fetch reviews
        if (data.id) {
          const revRes = await api.GET('/api/v1/products/{id}/reviews', {
            params: { path: { id: data.id } },
          })
          if (revRes.data) {
            setReviewsData(revRes.data)
          }

          // Fetch related
          const relRes = await api.GET('/api/v1/products/{id}/related', {
            params: { path: { id: data.id } },
          })
          if (relRes.data) {
            setRelatedProducts(relRes.data)
          }
        }
      } catch {
        setError('Đã có lỗi xảy ra khi tải dữ liệu sản phẩm')
      } finally {
        setIsLoading(false)
      }
    }

    void fetchProduct()
  }, [productSlug])

  const handleAddToCart = async (andCheckout = false) => {
    if (!product || !product.inStock) return
    setIsAdding(true)
    try {
      const ok = await addToCart(product.id, quantity)
      if (ok && andCheckout) {
        navigate('/checkout')
      }
    } finally {
      setIsAdding(false)
    }
  }

  const handleSubmitReview = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!product) return

    if (!reviewOrderItemId.trim()) {
      setReviewError('Vui lòng nhập mã chi tiết đơn hàng (OrderItemId) đã mua.')
      return
    }

    if (reviewBody.trim().length < 10) {
      setReviewError('Nội dung đánh giá phải có ít nhất 10 ký tự.')
      return
    }

    setIsSubmittingReview(true)
    setReviewError(null)

    try {
      const { data, error: err } = await api.POST('/api/v1/products/{id}/reviews', {
        params: { path: { id: product.id } },
        body: {
          orderItemId: reviewOrderItemId.trim(),
          rating: reviewRating,
          title: reviewTitle.trim() || null,
          body: reviewBody.trim(),
        },
      })

      if (err) {
        const msg = (err as any).detail || (err as any).title || 'Không thể gửi đánh giá'
        setReviewError(msg)
      } else if (data) {
        setReviewSuccess(true)
        setTimeout(() => {
          setReviewDialogOpen(false)
          setReviewSuccess(false)
          setReviewBody('')
          setReviewTitle('')
          setReviewOrderItemId('')
        }, 1500)
      }
    } catch {
      setReviewError('Lỗi kết nối khi gửi đánh giá')
    } finally {
      setIsSubmittingReview(false)
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-8 animate-pulse">
        <Skeleton className="h-6 w-48" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          <Skeleton className="h-96 w-full rounded-2xl" />
          <div className="space-y-4">
            <Skeleton className="h-8 w-3/4" />
            <Skeleton className="h-6 w-1/3" />
            <Skeleton className="h-12 w-1/2" />
            <Skeleton className="h-24 w-full" />
            <Skeleton className="h-12 w-full" />
          </div>
        </div>
      </div>
    )
  }

  if (error || !product) {
    return (
      <div className="text-center py-16 bg-paper-2 rounded-2xl border border-rule max-w-lg mx-auto">
        <AlertCircle className="w-12 h-12 text-tomato mx-auto mb-3 stroke-[1.5]" />
        <h3 className="font-display font-semibold text-lg text-ink mb-1">
          {error || 'Sản phẩm không tồn tại'}
        </h3>
        <p className="text-sm text-ink-2 mb-6">Sản phẩm có thể đã ngừng kinh doanh hoặc đường dẫn không đúng.</p>
        <Button variant="primary" onClick={() => navigate('/c/tat-ca')}>
          Về danh mục sản phẩm
        </Button>
      </div>
    )
  }

  const imagesList = product.images?.length
    ? product.images.map((img) => img.url)
    : []

  const ratingAvg = Number(reviewsData?.summary?.ratingAvg ?? product.ratingAvg ?? 0)
  const ratingCount = Number(reviewsData?.summary?.ratingCount ?? product.ratingCount ?? 0)
  const starCounts = reviewsData?.summary?.starCounts ?? {}

  return (
    <div className="space-y-12">
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
        {product.category && (
          <>
            <button
              type="button"
              onClick={() => navigate(`/c/${product.category?.slug}`)}
              className="hover:text-leaf transition-colors cursor-pointer"
            >
              {product.category.name}
            </button>
            <ChevronRight className="w-3.5 h-3.5 stroke-[1.5]" />
          </>
        )}
        <span className="font-semibold text-ink line-clamp-1">{product.name}</span>
      </nav>

      {/* Main PDP Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-8 lg:gap-12 items-start">
        {/* Left: Image Gallery */}
        <div className="space-y-4">
          <div className="aspect-square rounded-2xl bg-paper-2 border border-rule overflow-hidden flex items-center justify-center p-4">
            {selectedImage ? (
              <img
                src={selectedImage}
                alt={product.name}
                className="w-full h-full object-contain mix-blend-multiply"
              />
            ) : (
              <div className="text-ink-2/40 text-center">
                <ShoppingBag className="w-16 h-16 mx-auto mb-2 stroke-[1]" />
                <span className="text-xs">Ảnh sản phẩm tươi sạch</span>
              </div>
            )}
          </div>

          {/* Thumbnails */}
          {imagesList.length > 1 && (
            <div className="flex gap-2 overflow-x-auto pb-1">
              {imagesList.map((imgUrl, i) => (
                <button
                  key={i}
                  type="button"
                  onClick={() => setSelectedImage(imgUrl)}
                  className={`w-16 h-16 rounded-lg border overflow-hidden p-1 bg-paper-2 shrink-0 cursor-pointer transition-all ${
                    selectedImage === imgUrl ? 'border-leaf ring-2 ring-leaf/20' : 'border-rule'
                  }`}
                >
                  <img src={imgUrl} alt="" className="w-full h-full object-contain" />
                </button>
              ))}
            </div>
          )}

          {/* Value props strip */}
          <div className="grid grid-cols-3 gap-2 pt-2 text-center text-xs text-ink-2">
            <div className="p-2.5 rounded-lg bg-paper-2 border border-rule">
              <ShieldCheck className="w-4 h-4 text-leaf mx-auto mb-1 stroke-[1.5]" />
              <span className="font-medium text-[11px] block text-ink">100% An toàn</span>
              <span className="text-[10px] text-ink-2">Chứng nhận VietGAP</span>
            </div>
            <div className="p-2.5 rounded-lg bg-paper-2 border border-rule">
              <Truck className="w-4 h-4 text-leaf mx-auto mb-1 stroke-[1.5]" />
              <span className="font-medium text-[11px] block text-ink">Giao nhanh 2h</span>
              <span className="text-[10px] text-ink-2">Bảo quản lạnh</span>
            </div>
            <div className="p-2.5 rounded-lg bg-paper-2 border border-rule">
              <RotateCcw className="w-4 h-4 text-leaf mx-auto mb-1 stroke-[1.5]" />
              <span className="font-medium text-[11px] block text-ink">Đổi trả 24h</span>
              <span className="text-[10px] text-ink-2">Miễn phí tận nơi</span>
            </div>
          </div>
        </div>

        {/* Right: Product Buy Box */}
        <div className="space-y-6">
          {/* Brand & Stock */}
          <div className="flex items-center justify-between gap-2">
            <span className="text-xs font-semibold text-leaf uppercase tracking-wider">
              {product.brand?.name || 'Nông sản nội địa'}
            </span>
            <StockBadge inStock={product.inStock} lowStock={product.lowStock} />
          </div>

          {/* Title & SKU */}
          <div>
            <h1 className="font-display font-bold text-2xl sm:text-3xl text-ink leading-snug">
              {product.name}
            </h1>
            <div className="flex items-center gap-4 mt-2 text-xs text-ink-2">
              <span>Mã SKU: <strong className="text-ink">{product.sku}</strong></span>
              {product.unit && <span>Quy cách: <strong className="text-ink">{product.unit}</strong></span>}
            </div>
          </div>

          {/* Rating Snapshot */}
          <div className="flex items-center gap-2 pt-1">
            <div className="flex items-center text-amber-500">
              {Array.from({ length: 5 }).map((_, i) => (
                <Star
                  key={i}
                  className={`w-4 h-4 ${
                    i < Math.round(ratingAvg)
                      ? 'fill-amber-400 text-amber-400'
                      : 'text-ink-2/30'
                  } stroke-[1.5]`}
                />
              ))}
            </div>
            <span className="text-xs font-bold text-ink tabular-nums">
              {ratingAvg > 0 ? ratingAvg.toFixed(1) : 'Chưa có'}
            </span>
            <span className="text-xs text-ink-2">
              ({ratingCount} {vi.reviews.customerReviews})
            </span>
          </div>

          {/* Price Box */}
          <div className="p-4 rounded-xl bg-paper-2 border border-rule space-y-2">
            <PriceTag
              price={Number(product.sellingPrice)}
              originalPrice={product.listPrice ? Number(product.listPrice) : undefined}
              unit={product.unit}
              size="lg"
            />
            {product.listPrice && Number(product.listPrice) > Number(product.sellingPrice) && (
              <p className="text-xs text-tomato font-medium">
                Tiết kiệm {formatVnd(Number(product.listPrice) - Number(product.sellingPrice))} so với giá gốc
              </p>
            )}
          </div>

          {/* Delivery Promise */}
          <DeliveryStrip marquee={false} />

          {/* Quantity & Actions */}
          <div className="space-y-3 pt-2">
            <label className="text-xs font-semibold text-ink block">Số lượng mua</label>
            <div className="flex items-center gap-3">
              <div className="inline-flex items-center rounded-lg border border-rule bg-paper">
                <button
                  type="button"
                  disabled={quantity <= 1 || !product.inStock}
                  onClick={() => setQuantity((q) => Math.max(1, q - 1))}
                  className="p-2 text-ink hover:text-leaf disabled:opacity-40 cursor-pointer"
                  aria-label="Giảm số lượng"
                >
                  <Minus className="w-4 h-4 stroke-[1.5]" />
                </button>
                <span className="w-12 text-center text-sm font-bold text-ink tabular-nums select-none">
                  {quantity}
                </span>
                <button
                  type="button"
                  disabled={!product.inStock || (Number(product.availableQuantityCap) > 0 && quantity >= Number(product.availableQuantityCap))}
                  onClick={() => setQuantity((q) => q + 1)}
                  className="p-2 text-ink hover:text-leaf disabled:opacity-40 cursor-pointer"
                  aria-label="Tăng số lượng"
                >
                  <Plus className="w-4 h-4 stroke-[1.5]" />
                </button>
              </div>

              {Number(product.availableQuantityCap) > 0 && (
                <span className="text-xs text-ink-2">
                  Tối đa: <strong className="text-ink tabular-nums">{product.availableQuantityCap}</strong> {product.unit || 'sản phẩm'}
                </span>
              )}
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-3">
              <Button
                variant="outline"
                size="lg"
                disabled={!product.inStock || isAdding}
                onClick={() => void handleAddToCart(false)}
                className="w-full flex items-center justify-center gap-2 border-leaf text-leaf hover:bg-leaf/10 font-semibold"
              >
                <ShoppingBag className="w-4 h-4 stroke-[1.5]" />
                <span>{vi.product.addToCart}</span>
              </Button>
              <Button
                variant="primary"
                size="lg"
                disabled={!product.inStock || isAdding}
                onClick={() => void handleAddToCart(true)}
                className="w-full font-semibold shadow-xs"
              >
                {vi.product.buyNow}
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* Description & Technical Specs */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-8 border-t border-rule pt-8">
        <div className="md:col-span-2 space-y-4">
          <h2 className="font-display font-semibold text-xl text-ink">
            {vi.product.description}
          </h2>
          <div className="prose prose-sm text-ink-2 leading-relaxed space-y-3 font-sans">
            <p>{product.description || 'Sản phẩm tươi ngon được tuyển chọn kỹ lưỡng từ các hợp tác xã nông sản sạch, đạt chuẩn an toàn vệ sinh thực phẩm.'}</p>
          </div>
        </div>

        {/* Specifications table */}
        <div className="space-y-4 bg-paper-2 p-5 rounded-2xl border border-rule">
          <h3 className="font-display font-semibold text-sm text-ink">Thông tin chi tiết</h3>
          <dl className="text-xs space-y-2.5">
            <div className="flex justify-between border-b border-rule pb-1.5">
              <dt className="text-ink-2">Danh mục</dt>
              <dd className="font-semibold text-ink">{product.category?.name || 'Nông sản'}</dd>
            </div>
            <div className="flex justify-between border-b border-rule pb-1.5">
              <dt className="text-ink-2">Thương hiệu</dt>
              <dd className="font-semibold text-ink">{product.brand?.name || 'Chính hãng'}</dd>
            </div>
            <div className="flex justify-between border-b border-rule pb-1.5">
              <dt className="text-ink-2">Quy cách đóng gói</dt>
              <dd className="font-semibold text-ink">{product.unit || 'Theo phân loại'}</dd>
            </div>
            <div className="flex justify-between border-b border-rule pb-1.5">
              <dt className="text-ink-2">Hạn sử dụng</dt>
              <dd className="font-semibold text-ink">Xem trên bao bì</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-ink-2">Bảo quản</dt>
              <dd className="font-semibold text-ink">Ngăn mát tủ lạnh (2-6°C)</dd>
            </div>
          </dl>
        </div>
      </div>

      {/* Reviews Section */}
      <section className="border-t border-rule pt-8 space-y-6">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h2 className="font-display font-bold text-xl sm:text-2xl text-ink flex items-center gap-2">
              <MessageSquare className="w-5 h-5 text-leaf stroke-[1.5]" />
              {vi.reviews.customerReviews}
            </h2>
            <p className="text-xs text-ink-2 mt-1">Đánh giá thực tế từ khách hàng đã mua và nhận hàng</p>
          </div>

          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              if (!isAuthenticated) {
                navigate('/login')
              } else {
                setReviewDialogOpen(true)
              }
            }}
            className="flex items-center gap-1.5 border-leaf text-leaf hover:bg-leaf/10 font-semibold"
          >
            <Sparkles className="w-3.5 h-3.5 stroke-[1.5]" />
            <span>{vi.reviews.writeReview}</span>
          </Button>
        </div>

        {/* Rating Breakdown Card */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 p-6 rounded-2xl bg-paper-2 border border-rule items-center">
          <div className="text-center md:border-r border-rule md:pr-6 space-y-1">
            <span className="font-display font-bold text-4xl sm:text-5xl text-leaf tabular-nums">
              {ratingAvg > 0 ? ratingAvg.toFixed(1) : '5.0'}
            </span>
            <div className="flex justify-center text-amber-500 py-1">
              {Array.from({ length: 5 }).map((_, i) => (
                <Star
                  key={i}
                  className={`w-5 h-5 ${
                    i < Math.round(ratingAvg || 5)
                      ? 'fill-amber-400 text-amber-400'
                      : 'text-ink-2/30'
                  } stroke-[1.5]`}
                />
              ))}
            </div>
            <p className="text-xs text-ink-2 tabular-nums">{ratingCount} đánh giá đã xác thực</p>
          </div>

          {/* Star Distribution Progress */}
          <div className="md:col-span-2 space-y-1.5">
            {[5, 4, 3, 2, 1].map((star) => {
              const count = Number(starCounts[star.toString()] || 0)
              const pct = ratingCount > 0 ? (count / ratingCount) * 100 : 0
              return (
                <div key={star} className="flex items-center gap-3 text-xs">
                  <span className="w-12 font-medium text-ink tabular-nums flex items-center gap-1">
                    {star} <Star className="w-3 h-3 fill-amber-400 text-amber-400 stroke-none" />
                  </span>
                  <div className="flex-1 h-2 rounded-full bg-paper overflow-hidden border border-rule">
                    <div
                      className="h-full bg-leaf rounded-full transition-all duration-300"
                      style={{ width: `${pct}%` }}
                    />
                  </div>
                  <span className="w-10 text-right text-ink-2 tabular-nums">{count}</span>
                </div>
              )
            })}
          </div>
        </div>

        {/* Reviews List */}
        <div className="space-y-4">
          {!reviewsData?.reviews?.items?.length ? (
            <div className="text-center py-10 bg-paper-2 rounded-xl border border-rule text-xs text-ink-2">
              <p className="mb-2">Chưa có đánh giá nào cho sản phẩm này.</p>
              <p>Hãy là người đầu tiên trải nghiệm và chia sẻ cảm nhận!</p>
            </div>
          ) : (
            reviewsData.reviews.items.map((rev) => (
              <div
                key={rev.id}
                className="p-4 rounded-xl bg-paper-2 border border-rule space-y-2.5 transition-colors"
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="text-xs font-semibold text-ink">{rev.authorName}</span>
                    <span className="inline-flex items-center gap-1 text-[11px] font-medium text-leaf bg-leaf/10 px-2 py-0.5 rounded-full">
                      <CheckCircle2 className="w-3 h-3 stroke-[2]" />
                      {vi.reviews.verifiedPurchase}
                    </span>
                  </div>
                  <span className="text-[11px] text-ink-2">
                    {formatDateTime(rev.createdAt)}
                  </span>
                </div>

                <div className="flex items-center text-amber-500">
                  {Array.from({ length: 5 }).map((_, i) => (
                    <Star
                      key={i}
                      className={`w-3.5 h-3.5 ${
                        i < Number(rev.rating)
                          ? 'fill-amber-400 text-amber-400'
                          : 'text-ink-2/30'
                      } stroke-[1.5]`}
                    />
                  ))}
                </div>

                {rev.title && (
                  <h4 className="text-xs font-bold text-ink">{rev.title}</h4>
                )}

                <p className="text-xs text-ink-2 leading-relaxed">{rev.body}</p>

                {/* Admin Reply */}
                {rev.replyBody && (
                  <div className="mt-3 p-3 rounded-lg bg-paper border border-rule text-xs space-y-1">
                    <span className="font-semibold text-leaf block">
                      Phản hồi từ GroceryHub
                    </span>
                    <p className="text-ink-2 leading-relaxed">{rev.replyBody}</p>
                  </div>
                )}
              </div>
            ))
          )}
        </div>
      </section>

      {/* Related Products Carousel */}
      {relatedProducts.length > 0 && (
        <section className="border-t border-rule pt-8 space-y-4">
          <h2 className="font-display font-semibold text-xl sm:text-2xl text-ink">
            {vi.product.relatedProducts}
          </h2>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            {relatedProducts.slice(0, 4).map((p) => (
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

      {/* Write Review Dialog */}
      <Dialog open={reviewDialogOpen} onOpenChange={setReviewDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Đánh giá sản phẩm</DialogTitle>
            <DialogDescription>
              Chia sẻ trải nghiệm thực tế của bạn về sản phẩm "{product.name}" để giúp cộng đồng mua sắm tốt hơn.
            </DialogDescription>
          </DialogHeader>

          {reviewSuccess ? (
            <div className="py-6 text-center space-y-2">
              <CheckCircle2 className="w-12 h-12 text-leaf mx-auto stroke-[1.5]" />
              <h4 className="font-semibold text-ink text-sm">Gửi đánh giá thành công!</h4>
              <p className="text-xs text-ink-2">Cảm ơn bạn. Đánh giá sẽ được hiển thị sau khi qua kiểm duyệt.</p>
            </div>
          ) : (
            <form onSubmit={handleSubmitReview} className="space-y-4 py-2">
              {reviewError && (
                <div className="p-3 rounded-lg bg-[#FDECE8] border border-[#FADCD5] text-xs text-tomato">
                  {reviewError}
                </div>
              )}

              {/* Star Picker */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-ink block">Đánh giá sao</label>
                <div className="flex items-center gap-1.5">
                  {[1, 2, 3, 4, 5].map((s) => (
                    <button
                      key={s}
                      type="button"
                      onClick={() => setReviewRating(s)}
                      className="p-1 cursor-pointer hover:scale-110 transition-transform"
                    >
                      <Star
                        className={`w-6 h-6 ${
                          s <= reviewRating
                            ? 'fill-amber-400 text-amber-400'
                            : 'text-ink-2/30'
                        } stroke-[1.5]`}
                      />
                    </button>
                  ))}
                  <span className="text-xs text-ink-2 ml-2">
                    {reviewRating === 5
                      ? 'Tuyệt vời'
                      : reviewRating === 4
                      ? 'Rất tốt'
                      : reviewRating === 3
                      ? 'Bình thường'
                      : reviewRating === 2
                      ? 'Chưa hài lòng'
                      : 'Rất tệ'}
                  </span>
                </div>
              </div>

              {/* OrderItemId */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-ink block">
                  Mã xác nhận mua hàng (Đơn đã giao thành công) <span className="text-tomato">*</span>
                </label>
                <Input
                  placeholder="Nhập mã đơn hàng hoặc mã dòng hàng đã nhận"
                  value={reviewOrderItemId}
                  onChange={(e) => setReviewOrderItemId(e.target.value)}
                  className="text-xs h-9"
                  required
                />
                <span className="text-[11px] text-ink-2/80 block leading-relaxed">
                  💡 <strong>Gợi ý:</strong> Bạn có thể vào <strong>Đơn hàng của tôi</strong> → Chọn đơn hàng đã nhận thành công → Bấm đánh giá trực tiếp để tự động điền mã xác nhận.
                </span>
              </div>

              {/* Title */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-ink block">Tiêu đề (Tùy chọn)</label>
                <Input
                  placeholder="Ví dụ: Rau rất tươi, giao đúng giờ"
                  value={reviewTitle}
                  onChange={(e) => setReviewTitle(e.target.value)}
                  className="text-xs h-9"
                />
              </div>

              {/* Body */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-ink block">
                  Nội dung đánh giá <span className="text-tomato">*</span>
                </label>
                <textarea
                  rows={4}
                  placeholder="Chia sẻ chất lượng sản phẩm, độ tươi, hương vị, cách bảo quản..."
                  value={reviewBody}
                  onChange={(e) => setReviewBody(e.target.value)}
                  className="w-full rounded-md border border-rule bg-paper p-3 text-xs text-ink focus:outline-none focus:ring-2 focus:ring-leaf"
                  required
                />
              </div>

              <DialogFooter className="pt-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setReviewDialogOpen(false)}
                >
                  Hủy
                </Button>
                <Button
                  type="submit"
                  variant="primary"
                  size="sm"
                  disabled={isSubmittingReview}
                >
                  {isSubmittingReview ? 'Đang gửi...' : 'Gửi đánh giá'}
                </Button>
              </DialogFooter>
            </form>
          )}
        </DialogContent>
      </Dialog>

      {/* Mobile Sticky Buy / Add to Cart Bar (md:hidden) */}
      {product && (
        <div className="md:hidden fixed bottom-0 left-0 right-0 z-40 bg-white/95 backdrop-blur-md border-t border-rule px-4 py-2.5 flex items-center justify-between gap-3 shadow-2xl pb-[max(0.6rem,env(safe-area-inset-bottom))]">
          <div className="flex flex-col min-w-0">
            <div className="flex items-baseline gap-1">
              <span className="text-base font-bold text-leaf font-mono leading-tight">
                {formatVnd(Number(product.sellingPrice))}
              </span>
              {product.unit && (
                <span className="text-[10px] text-ink-2">/{product.unit}</span>
              )}
            </div>
            {product.listPrice && Number(product.listPrice) > Number(product.sellingPrice) ? (
              <span className="text-[10px] text-ink-2 line-through leading-tight">
                {formatVnd(Number(product.listPrice))}
              </span>
            ) : (
              <span className="text-[10px] text-leaf font-medium leading-tight">
                {product.inStock ? 'Còn hàng' : 'Tạm hết'}
              </span>
            )}
          </div>

          <div className="flex items-center gap-2 shrink-0">
            <button
              type="button"
              disabled={!product.inStock || isAdding}
              onClick={() => void handleAddToCart(false)}
              className="h-10 px-3 rounded-xl border border-leaf text-leaf font-semibold text-xs flex items-center gap-1.5 hover:bg-leaf/10 active:scale-95 disabled:opacity-40 cursor-pointer transition-transform"
            >
              <ShoppingBag className="w-4 h-4 stroke-[1.8]" />
              <span>Thêm giỏ</span>
            </button>

            <button
              type="button"
              disabled={!product.inStock || isAdding}
              onClick={() => void handleAddToCart(true)}
              className="h-10 px-4 rounded-xl bg-leaf text-white font-bold text-xs shadow-md hover:bg-leaf/90 active:scale-95 disabled:opacity-40 cursor-pointer flex items-center gap-1 transition-transform"
            >
              <span>Mua ngay</span>
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
