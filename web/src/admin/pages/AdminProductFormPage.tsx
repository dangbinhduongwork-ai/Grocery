import React, { useEffect, useState } from 'react'
import {
  ArrowLeft,
  Save,
  AlertCircle,
  CheckCircle2,
} from 'lucide-react'
import { api } from '@/shared/api/client'
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
        }
      } catch {
        setErrorMsg('Không thể tải thông tin sản phẩm.')
      } finally {
        setIsLoading(false)
      }
    }
    void loadProduct()
  }, [id])

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
        <div className="flex items-center justify-end gap-3 pt-2">
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
      </form>
    </div>
  )
}
