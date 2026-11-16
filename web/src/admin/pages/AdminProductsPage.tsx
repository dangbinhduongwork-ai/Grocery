import React, { useEffect, useState, useCallback } from 'react'
import {
  Plus,
  RefreshCw,
  Edit,
  Trash2,
  Package,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { formatVnd } from '@/shared/utils/formatVnd'
import { useRouter } from '@/app/router'
import { useAuth } from '@/shared/auth'
import { DataTable, Column } from '../components/DataTable'

interface ProductItem {
  id: string
  sku: string
  barcode?: string
  name: string
  categoryName: string
  brandName?: string
  unit: string
  sellingPrice: number
  listPrice: number
  costPrice?: number | null
  status: string
  onHand?: number
  available?: number
  primaryImageUrl?: string | null
}

export const AdminProductsPage: React.FC = () => {
  const router = useRouter()
  const { hasPermission } = useAuth()
  const [products, setProducts] = useState<ProductItem[]>([])
  const [isLoading, setIsLoading] = useState(true)

  const canWrite = hasPermission('catalog.write')
  const canSeeCost = hasPermission('catalog.cost.read')

  const fetchProducts = useCallback(async () => {
    setIsLoading(true)
    try {
      const res = await api.GET('/api/v1/admin/products', {
        params: { query: { pageSize: 100 } },
      })
      if (res.data?.items) {
        setProducts(res.data.items as unknown as ProductItem[])
      } else {
        setProducts([])
      }
    } catch {
      setProducts([])
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void fetchProducts()
  }, [fetchProducts])

  const handleDeleteProduct = async (id: string, name: string) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa sản phẩm "${name}"? Sản phẩm sẽ được chuyển sang trạng thái lưu trữ và ngừng kinh doanh.`)) {
      return
    }

    try {
      const res = await (api.DELETE as any)('/api/v1/admin/products/{id}', {
        params: { path: { id } },
      })

      if (res.error) {
        // Fallback: update status to Archived if DELETE isn't ready
        await api.PUT('/api/v1/admin/products/{id}/status', {
          params: { path: { id } },
          body: { status: 'Archived' },
        })
      }

      await fetchProducts()
    } catch {
      alert('Đã có lỗi xảy ra khi xóa sản phẩm.')
    }
  }

  const columns: Column<ProductItem>[] = [
    {
      key: 'image',
      title: 'Ảnh',
      className: 'w-14 text-center',
      render: p => (
        <div className="w-11 h-11 rounded-lg bg-slate-100 border border-slate-200 overflow-hidden shrink-0 flex items-center justify-center">
          {p.primaryImageUrl ? (
            <img
              src={p.primaryImageUrl}
              alt={p.name}
              className="w-full h-full object-cover"
              loading="lazy"
            />
          ) : (
            <Package className="w-5 h-5 text-slate-300" strokeWidth={1.5} />
          )}
        </div>
      ),
    },
    {
      key: 'sku',
      title: 'Mã SKU',
      sortable: true,
      render: p => <span className="font-mono text-xs font-semibold text-slate-700">{p.sku}</span>,
    },
    {
      key: 'name',
      title: 'Tên sản phẩm',
      sortable: true,
      render: p => (
        <div>
          <div className="font-semibold text-slate-900">{p.name}</div>
          <div className="text-xs text-slate-400">
            {p.categoryName} {p.brandName ? `• ${p.brandName}` : ''}
          </div>
        </div>
      ),
    },
    {
      key: 'unit',
      title: 'Đơn vị',
      render: p => <span className="text-xs text-slate-500">{p.unit}</span>,
    },
    {
      key: 'sellingPrice',
      title: 'Giá bán',
      sortable: true,
      className: 'text-right',
      render: p => (
        <div className="text-right">
          <div className="font-semibold text-slate-900">{formatVnd(p.sellingPrice)}</div>
          {p.listPrice > p.sellingPrice && (
            <div className="text-[11px] text-slate-400 line-through">{formatVnd(p.listPrice)}</div>
          )}
        </div>
      ),
    },
    ...(canSeeCost
      ? [
          {
            key: 'costPrice',
            title: 'Giá vốn',
            className: 'text-right',
            render: (p: ProductItem) => (
              <div className="text-right font-mono text-xs text-amber-700">
                {p.costPrice !== undefined && p.costPrice !== null ? formatVnd(p.costPrice) : '—'}
              </div>
            ),
          },
        ]
      : []),
    {
      key: 'status',
      title: 'Trạng thái',
      render: p => (
        <span
          className={`px-2 py-0.5 rounded-full text-xs font-semibold ${
            p.status === 'Active'
              ? 'bg-emerald-50 text-emerald-700'
              : p.status === 'Draft'
              ? 'bg-amber-50 text-amber-700'
              : 'bg-slate-100 text-slate-500'
          }`}
        >
          {p.status === 'Active' ? 'Đang bán' : p.status === 'Draft' ? 'Bản nháp' : 'Lưu trữ'}
        </span>
      ),
    },
    {
      key: 'actions',
      title: 'Thao tác',
      className: 'text-right',
      render: p => (
        <div className="flex items-center justify-end gap-1.5">
          {canWrite && (
            <>
              <button
                type="button"
                onClick={() => router.push(`/admin/products/${p.id}/edit`)}
                className="p-1.5 rounded-lg border border-slate-200 text-slate-600 hover:text-leaf hover:bg-leaf/5 transition-colors"
                title="Chỉnh sửa sản phẩm"
              >
                <Edit className="w-4 h-4" strokeWidth={1.5} />
              </button>
              <button
                type="button"
                onClick={() => void handleDeleteProduct(p.id, p.name)}
                className="p-1.5 rounded-lg border border-slate-200 text-slate-400 hover:text-rose-600 hover:bg-rose-50 transition-colors"
                title="Xóa sản phẩm"
              >
                <Trash2 className="w-4 h-4" strokeWidth={1.5} />
              </button>
            </>
          )}
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 font-display">Danh mục sản phẩm</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Quản lý danh sách sản phẩm, quy cách đóng gói, giá niêm yết và trạng thái kinh doanh.
          </p>
        </div>
        <div className="flex items-center gap-2">
          {canWrite && (
            <button
              type="button"
              onClick={() => router.push('/admin/products/new')}
              className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl bg-leaf text-white text-xs font-semibold hover:bg-leaf-2 transition-colors shadow-xs"
            >
              <Plus className="w-4 h-4" strokeWidth={1.5} />
              Thêm sản phẩm mới
            </button>
          )}
          <button
            type="button"
            onClick={() => void fetchProducts()}
            disabled={isLoading}
            className="p-2 rounded-xl border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 transition-colors shadow-2xs"
            title="Làm mới"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} strokeWidth={1.5} />
          </button>
        </div>
      </div>

      <DataTable
        data={products}
        columns={columns}
        keyExtractor={p => p.id}
        searchPlaceholder="Tìm theo tên sản phẩm, SKU hoặc danh mục..."
        searchableKey={p => `${p.name} ${p.sku} ${p.categoryName} ${p.barcode ?? ''}`}
        pageSize={15}
        isLoading={isLoading}
        emptyMessage="Không tìm thấy sản phẩm nào trong danh mục"
      />
    </div>
  )
}
