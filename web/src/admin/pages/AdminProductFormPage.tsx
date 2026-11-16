import React, { useEffect, useState } from 'react'
import {
  ArrowLeft,
  Save,
  AlertCircle,
  CheckCircle2,
  Upload,
  Trash2,
  Star,
  Image as ImageIcon,
} from 'lucide-react'
import { api, getAccessToken } from '@/shared/api/client'
import { useRouter } from '@/app/router'
import { useAuth } from '@/shared/auth'

interface CategoryOption {
  id: string
  name: string
}

interface BrandOption {
  id: string
  name: string
}

interface ProductImageItem {
  id: string
  url: string
  altText?: string | null
  sortOrder: number
  isPrimary: boolean
}

export const AdminProductFormPage: React.FC<{ id?: string }> = ({ id }) => {
  const router = useRouter()
  const { hasPermission } = useAuth()
  const isEdit = Boolean(id)

  const [categories, setCategories] = useState<CategoryOption[]>([])
  const [brands, setBrands] = useState<BrandOption[]>([])
  const [isLoading, setIsLoading] = useState(isEdit)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)

  // Form fields
  const [name, setName] = useState('')
  const [sku, setSku] = useState('')
  const [barcode, setBarcode] = useState('')
  const [slug, setSlug] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [brandId, setBrandId] = useState('')
  const [unit, setUnit] = useState('Gói')
  const [packSize, setPackSize] = useState('')
  const [listPrice, setListPrice] = useState(100000)
  const [sellingPrice, setSellingPrice] = useState(90000)
  const [initialCostPrice, setInitialCostPrice] = useState(70000)
  const [status, setStatus] = useState('Active')
  const [shortDescription, setShortDescription] = useState('')
  const [description, setDescription] = useState('')
  const [isFeatured, setIsFeatured] = useState(false)
  const [images, setImages] = useState<ProductImageItem[]>([])
  const [isUploadingImage, setIsUploadingImage] = useState(false)
  const [imageMsg, setImageMsg] = useState<{ type: 'ok' | 'err'; text: string } | null>(null)

  const canSeeCost = hasPermission('catalog.cost.read')

  useEffect(() => {
    async function initOptions() {
      try {
        const [catRes, brandRes] = await Promise.all([
          api.GET('/api/v1/admin/categories'),
          api.GET('/api/v1/admin/brands'),
        ])
        if (catRes.data) setCategories(catRes.data as unknown as CategoryOption[])
        if (brandRes.data) setBrands(brandRes.data as unknown as BrandOption[])
      } catch {
        // Silently
      }
    }
    void initOptions()
  }, [])

  useEffect(() => {
    if (!id) return
    async function loadProduct() {
      try {
        const res = await api.GET('/api/v1/admin/products/{id}', {
          params: { path: { id: id! } },
        })
        if (res.data) {
          const p = res.data as Record<string, unknown>
          setName(String(p.name ?? ''))
          setSku(String(p.sku ?? ''))
          setBarcode(String(p.barcode ?? ''))
          setSlug(String(p.slug ?? ''))
          setCategoryId(String(p.categoryId ?? ''))
          setBrandId(String(p.brandId ?? ''))
          setUnit(String(p.unit ?? 'Gói'))
          setPackSize(String(p.packSize ?? ''))
          setListPrice(Number(p.listPrice ?? 0))
          setSellingPrice(Number(p.sellingPrice ?? 0))
          if (p.costPrice) setInitialCostPrice(Number(p.costPrice))
          setStatus(String(p.status ?? 'Active'))
          setShortDescription(String(p.shortDescription ?? ''))
          setDescription(String(p.description ?? ''))
          setIsFeatured(Boolean(p.isFeatured))
          if (Array.isArray(p.images)) {
            setImages(p.images as unknown as ProductImageItem[])
          }
        }
      } catch {
        setErrorMsg('Không thể tải thông tin sản phẩm.')
      } finally {
        setIsLoading(false)
      }
    }
    void loadProduct()
  }, [id])

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file || !id) return

    if (file.size > 5 * 1024 * 1024) {
      setImageMsg({ type: 'err', text: 'Kích thước file ảnh không được vượt quá 5MB.' })
      return
    }

    const validTypes = ['image/jpeg', 'image/png', 'image/webp']
    if (!validTypes.includes(file.type)) {
      setImageMsg({ type: 'err', text: 'Định dạng ảnh không hỗ trợ. Vui lòng chọn file JPEG, PNG hoặc WebP.' })
      return
    }

    setIsUploadingImage(true)
    setImageMsg(null)

    try {
      const token = getAccessToken()
      const formData = new FormData()
      formData.append('file', file)
      formData.append('isPrimary', String(images.length === 0))

      const res = await fetch(`/api/v1/admin/products/${id}/images`, {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: formData,
        credentials: 'include',
      })

      if (!res.ok) {
        const errJson = await res.json().catch(() => null)
        setImageMsg({ type: 'err', text: errJson?.detail || 'Không thể tải ảnh lên. Vui lòng thử lại.' })
      } else {
        const uploadedImg = await res.json() as ProductImageItem
        setImages(prev => [...prev, uploadedImg])
        setImageMsg({ type: 'ok', text: 'Tải ảnh sản phẩm thành công!' })
      }
    } catch {
      setImageMsg({ type: 'err', text: 'Lỗi kết nối khi tải ảnh lên máy chủ.' })
    } finally {
      setIsUploadingImage(false)
      e.target.value = ''
    }
  }

  const handleSetPrimary = async (imageId: string) => {
    if (!id) return
    try {
      const token = getAccessToken()
      const body = JSON.stringify({
        imageOrders: images.map((img, idx) => ({
          imageId: img.id,
          sortOrder: img.id === imageId ? 0 : idx + 1,
          isPrimary: img.id === imageId,
        })),
      })
      const res = await fetch(`/api/v1/admin/products/${id}/images/order`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body,
        credentials: 'include',
      })
      if (res.ok) {
        setImages(prev =>
          prev.map(img => ({
            ...img,
            isPrimary: img.id === imageId,
          }))
        )
        setImageMsg({ type: 'ok', text: 'Đã đặt làm ảnh đại diện sản phẩm.' })
      }
    } catch {
      setImageMsg({ type: 'err', text: 'Không thể cập nhật ảnh đại diện.' })
    }
  }

  const handleDeleteImage = async (imageId: string) => {
    if (!id) return
    if (!window.confirm('Bạn có chắc chắn muốn xóa ảnh này?')) return

    try {
      const token = getAccessToken()
      const res = await fetch(`/api/v1/admin/products/${id}/images/${imageId}`, {
        method: 'DELETE',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        credentials: 'include',
      })
      if (res.ok) {
        setImages(prev => prev.filter(img => img.id !== imageId))
        setImageMsg({ type: 'ok', text: 'Đã xóa ảnh thành công.' })
      } else {
        setImageMsg({ type: 'err', text: 'Không thể xóa ảnh này.' })
      }
    } catch {
      setImageMsg({ type: 'err', text: 'Lỗi kết nối khi xóa ảnh.' })
    }
  }

  const handleDeleteProduct = async () => {
    if (!id || !window.confirm(`Bạn có chắc chắn muốn xóa sản phẩm "${name}"? Sản phẩm sẽ được chuyển vào mục lưu trữ và gỡ khỏi gian hàng.`)) {
      return
    }

    try {
      const res = await (api.DELETE as any)('/api/v1/admin/products/{id}', {
        params: { path: { id } },
      })

      if (res.error) {
        // Fallback: update status to Archived
        await api.PUT('/api/v1/admin/products/{id}/status', {
          params: { path: { id } },
          body: { status: 'Archived' },
        })
      }

      router.push('/admin/products')
    } catch {
      alert('Đã có lỗi xảy ra khi xóa sản phẩm.')
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsSubmitting(true)
    setErrorMsg(null)
    setSuccessMsg(null)

    if (sellingPrice > listPrice) {
      setErrorMsg('Giá bán khuyến mãi không được lớn hơn giá niêm yết (Quy tắc BR-01).')
      setIsSubmitting(false)
      return
    }

    try {
      if (isEdit) {
        const res = await api.PUT('/api/v1/admin/products/{id}', {
          params: { path: { id: id! } },
          body: {
            sku,
            barcode: barcode.trim() || null,
            name,
            slug,
            categoryId,
            brandId: brandId || null,
            unit,
            packSize: packSize.trim() || null,
            shortDescription: shortDescription.trim() || null,
            description: description.trim() || null,
            specsJson: null,
            isFeatured,
            expiryTracked: false,
          },
        })

        if (res.error) {
          setErrorMsg((res.error as { detail?: string })?.detail ?? 'Cập nhật thất bại.')
        } else {
          setSuccessMsg('Đã cập nhật sản phẩm thành công!')
        }
      } else {
        const res = await api.POST('/api/v1/admin/products', {
          body: {
            sku,
            barcode: barcode.trim() || null,
            name,
            slug: slug.trim() || null,
            categoryId,
            brandId: brandId || null,
            unit,
            packSize: packSize.trim() || null,
            listPrice,
            sellingPrice,
            initialCostPrice: canSeeCost && initialCostPrice ? initialCostPrice : null,
            status: status || null,
            shortDescription: shortDescription.trim() || null,
            description: description.trim() || null,
            specsJson: null,
            isFeatured,
            expiryTracked: false,
            initialReorderLevel: null,
          },
        })

        if (res.error) {
          setErrorMsg((res.error as { detail?: string })?.detail ?? 'Tạo sản phẩm thất bại.')
        } else {
          setSuccessMsg('Tạo sản phẩm mới thành công!')
          setTimeout(() => router.push('/admin/products'), 1000)
        }
      }
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : 'Lỗi hệ thống.')
    } finally {
      setIsSubmitting(false)
    }
  }

  if (isLoading) {
    return <div className="h-64 bg-white rounded-2xl animate-pulse" />
  }

  return (
    <div className="space-y-6 max-w-4xl">
      {/* Header */}
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={() => router.push('/admin/products')}
          className="p-2 rounded-xl border border-slate-200 bg-white text-slate-600 hover:bg-slate-50 transition-colors"
        >
          <ArrowLeft className="w-4 h-4" strokeWidth={1.5} />
        </button>
        <div>
          <h1 className="text-xl font-bold text-slate-900 font-display">
            {isEdit ? 'Chỉnh sửa sản phẩm' : 'Thêm sản phẩm mới'}
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Điền thông tin quy cách, phân loại và giá niêm yết theo chuẩn GroceryHub.
          </p>
        </div>
      </div>

      {errorMsg && (
        <div className="p-3 bg-rose-50 border border-rose-200 text-rose-700 text-xs rounded-xl flex items-center gap-2">
          <AlertCircle className="w-4 h-4 shrink-0" strokeWidth={1.5} />
          <span>{errorMsg}</span>
        </div>
      )}

      {successMsg && (
        <div className="p-3 bg-emerald-50 border border-emerald-200 text-emerald-700 text-xs rounded-xl flex items-center gap-2">
          <CheckCircle2 className="w-4 h-4 shrink-0" strokeWidth={1.5} />
          <span>{successMsg}</span>
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Basic Information */}
        <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-2xs space-y-4">
          <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700 border-b border-slate-100 pb-2">
            Thông tin cơ bản
          </h2>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="sm:col-span-2">
              <label className="block text-xs font-semibold text-slate-700 mb-1">Tên sản phẩm *</label>
              <input
                type="text"
                required
                value={name}
                onChange={e => setName(e.target.value)}
                placeholder="VD: Gạo ST25 Ông Cua Túi 5kg"
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Mã SKU *</label>
              <input
                type="text"
                required
                value={sku}
                onChange={e => setSku(e.target.value)}
                placeholder="SKU-GAO-ST25"
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Mã vạch Barcode</label>
              <input
                type="text"
                value={barcode}
                onChange={e => setBarcode(e.target.value)}
                placeholder="8938501234567"
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Danh mục *</label>
              <select
                required
                value={categoryId}
                onChange={e => setCategoryId(e.target.value)}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              >
                <option value="">-- Chọn danh mục --</option>
                {categories.map(c => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Thương hiệu</label>
              <select
                value={brandId}
                onChange={e => setBrandId(e.target.value)}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              >
                <option value="">-- Không có / Khác --</option>
                {brands.map(b => (
                  <option key={b.id} value={b.id}>
                    {b.name}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Đơn vị tính *</label>
              <input
                type="text"
                required
                value={unit}
                onChange={e => setUnit(e.target.value)}
                placeholder="Gói, Hộp, Chai, Kg..."
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Quy cách đóng gói</label>
              <input
                type="text"
                value={packSize}
                onChange={e => setPackSize(e.target.value)}
                placeholder="5kg, 1L, Thùng 24 lon..."
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>
          </div>
        </div>

        {/* Pricing Block */}
        <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-2xs space-y-4">
          <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700 border-b border-slate-100 pb-2">
            Chính sách giá & Chi phí (VND)
          </h2>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Giá niêm yết (Gốc) *</label>
              <input
                type="number"
                required
                min={0}
                value={listPrice}
                onChange={e => setListPrice(Number(e.target.value))}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs font-mono font-semibold focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Giá bán hiện tại *</label>
              <input
                type="number"
                required
                min={0}
                value={sellingPrice}
                onChange={e => setSellingPrice(Number(e.target.value))}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs font-mono font-semibold text-leaf focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>

            {canSeeCost && (
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Giá vốn ban đầu</label>
                <input
                  type="number"
                  min={0}
                  value={initialCostPrice}
                  onChange={e => setInitialCostPrice(Number(e.target.value))}
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs font-mono text-amber-700 focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>
            )}
          </div>

          {canSeeCost && sellingPrice > 0 && initialCostPrice > 0 && (
            <div className="p-3 bg-slate-50 rounded-xl border border-slate-100 flex items-center justify-between text-xs text-slate-600">
              <span>Lợi nhuận gộp dự kiến:</span>
              <span className="font-semibold text-emerald-700 font-mono">
                {((sellingPrice - initialCostPrice)).toLocaleString('vi-VN')} đ (
                {Math.round(((sellingPrice - initialCostPrice) / sellingPrice) * 100)}%)
              </span>
            </div>
          )}
        </div>

        {/* Product Images (Gallery & Upload) */}
        {isEdit && (
          <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-2xs space-y-4">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-slate-100 pb-3">
              <div>
                <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700 flex items-center gap-2">
                  <ImageIcon className="w-4 h-4 text-leaf" strokeWidth={1.5} />
                  Hình ảnh sản phẩm ({images.length})
                </h2>
                <p className="text-[11px] text-slate-400 mt-0.5">
                  Định dạng hỗ trợ: JPEG, PNG, WebP (Tối đa 5MB). Ảnh có nhãn "Ảnh chính" sẽ được ưu tiên hiển thị trên storefront.
                </p>
              </div>

              {/* Upload Button */}
              <div>
                <label className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-leaf text-white text-xs font-semibold hover:bg-leaf-2 transition-colors cursor-pointer shadow-2xs">
                  <Upload className="w-3.5 h-3.5" strokeWidth={1.5} />
                  <span>{isUploadingImage ? 'Đang tải lên...' : 'Tải ảnh mới từ máy'}</span>
                  <input
                    type="file"
                    accept="image/jpeg,image/png,image/webp"
                    disabled={isUploadingImage}
                    onChange={handleImageUpload}
                    className="hidden"
                  />
                </label>
              </div>
            </div>

            {/* Image Notification Message */}
            {imageMsg && (
              <div
                className={`p-3 rounded-xl text-xs flex items-center gap-2 ${
                  imageMsg.type === 'ok'
                    ? 'bg-emerald-50 text-emerald-800 border border-emerald-200'
                    : 'bg-rose-50 text-rose-800 border border-rose-200'
                }`}
              >
                {imageMsg.type === 'ok' ? (
                  <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
                ) : (
                  <AlertCircle className="w-4 h-4 text-rose-600 shrink-0" />
                )}
                <span>{imageMsg.text}</span>
              </div>
            )}

            {/* Images Grid */}
            {images.length === 0 ? (
              <div className="py-8 text-center border-2 border-dashed border-slate-200 rounded-xl bg-slate-50/50">
                <ImageIcon className="w-8 h-8 text-slate-300 mx-auto mb-2" strokeWidth={1.5} />
                <p className="text-xs font-medium text-slate-600">Sản phẩm này chưa có hình ảnh nào.</p>
                <p className="text-[11px] text-slate-400 mt-0.5">Bấm nút "Tải ảnh mới từ máy" ở trên để bổ sung hình ảnh cho sản phẩm.</p>
              </div>
            ) : (
              <div className="grid grid-cols-2 sm:grid-cols-4 md:grid-cols-5 gap-4">
                {images.map((img) => (
                  <div
                    key={img.id}
                    className={`relative rounded-xl border overflow-hidden group bg-slate-50 ${
                      img.isPrimary ? 'border-leaf ring-2 ring-leaf/20' : 'border-slate-200'
                    }`}
                  >
                    <div className="aspect-square w-full overflow-hidden bg-white flex items-center justify-center p-1">
                      <img
                        src={img.url}
                        alt={img.altText || name}
                        className="w-full h-full object-cover rounded-lg"
                      />
                    </div>

                    {/* Primary Badge */}
                    {img.isPrimary && (
                      <span className="absolute top-1.5 left-1.5 px-2 py-0.5 bg-leaf text-white text-[10px] font-bold rounded-md shadow-xs flex items-center gap-1">
                        <Star className="w-2.5 h-2.5 fill-current" />
                        Ảnh chính
                      </span>
                    )}

                    {/* Controls */}
                    <div className="p-2 bg-white border-t border-slate-100 flex items-center justify-between gap-1">
                      {!img.isPrimary ? (
                        <button
                          type="button"
                          onClick={() => handleSetPrimary(img.id)}
                          className="text-[10px] text-slate-600 hover:text-leaf hover:underline font-semibold cursor-pointer"
                        >
                          Đặt làm ảnh chính
                        </button>
                      ) : (
                        <span className="text-[10px] text-slate-400 font-medium">Mặc định</span>
                      )}

                      <button
                        type="button"
                        onClick={() => handleDeleteImage(img.id)}
                        className="p-1 rounded text-slate-400 hover:text-rose-600 hover:bg-rose-50 transition-colors ml-auto cursor-pointer"
                        title="Xóa ảnh"
                      >
                        <Trash2 className="w-3.5 h-3.5" strokeWidth={1.5} />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Descriptions & Options */}
        <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-2xs space-y-4">
          <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700 border-b border-slate-100 pb-2">
            Mô tả & Tùy chọn hiển thị
          </h2>

          <div className="space-y-4">
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Mô tả ngắn</label>
              <input
                type="text"
                value={shortDescription}
                onChange={e => setShortDescription(e.target.value)}
                placeholder="Tóm tắt đặc tính nổi bật của sản phẩm..."
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">Mô tả chi tiết</label>
              <textarea
                rows={4}
                value={description}
                onChange={e => setDescription(e.target.value)}
                placeholder="Xuất xứ, thành phần dinh dưỡng, hướng dẫn bảo quản..."
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>

            <div className="flex items-center gap-6 pt-2">
              <label className="flex items-center gap-2 cursor-pointer text-xs font-semibold text-slate-700">
                <input
                  type="checkbox"
                  checked={isFeatured}
                  onChange={e => setIsFeatured(e.target.checked)}
                  className="rounded-md border-slate-300 text-leaf focus:ring-leaf w-4 h-4"
                />
                <span>Đánh dấu sản phẩm nổi bật (Trang chủ)</span>
              </label>

              <div className="flex items-center gap-2">
                <label className="text-xs font-semibold text-slate-700">Trạng thái:</label>
                <select
                  value={status}
                  onChange={e => setStatus(e.target.value)}
                  className="px-3 py-1.5 bg-slate-50 border border-slate-200 rounded-xl text-xs font-semibold"
                >
                  <option value="Active">Đang bán (Active)</option>
                  <option value="Draft">Bản nháp (Draft)</option>
                  <option value="Archived">Lưu trữ (Archived)</option>
                </select>
              </div>
            </div>
          </div>
        </div>

        {/* Submit Bar */}
        <div className="flex items-center justify-between gap-3 pt-2">
          {isEdit ? (
            <button
              type="button"
              onClick={handleDeleteProduct}
              className="inline-flex items-center gap-1.5 px-4 py-2.5 rounded-xl border border-rose-200 text-rose-600 hover:bg-rose-50 text-xs font-semibold transition-colors cursor-pointer"
            >
              <Trash2 className="w-4 h-4" strokeWidth={1.5} />
              Xóa sản phẩm này
            </button>
          ) : (
            <div />
          )}
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => router.push('/admin/products')}
              className="px-4 py-2.5 rounded-xl border border-slate-200 text-slate-700 text-xs font-semibold hover:bg-slate-50 transition-colors"
            >
              Hủy bỏ
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-leaf text-white text-xs font-semibold hover:bg-leaf-2 transition-colors shadow-xs disabled:opacity-50"
            >
              <Save className="w-4 h-4" strokeWidth={1.5} />
              {isSubmitting ? 'Đang lưu...' : isEdit ? 'Cập nhật sản phẩm' : 'Tạo sản phẩm'}
            </button>
          </div>
        </div>
      </form>
    </div>
  )
}
