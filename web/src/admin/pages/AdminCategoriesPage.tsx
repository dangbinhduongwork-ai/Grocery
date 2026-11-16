import React, { useEffect, useState, useCallback } from 'react'
import {
  Plus,
  RefreshCw,
  FolderTree,
  Building2,
  Edit,
  Trash2,
  AlertCircle,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { DataTable, Column } from '../components/DataTable'

interface CategoryItem {
  id: string
  parentId?: string | null
  name: string
  slug: string
  sortOrder: number
  isActive: boolean
}

interface BrandItem {
  id: string
  name: string
  slug: string
  isActive: boolean
}

export const AdminCategoriesPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'categories' | 'brands'>('categories')
  const [categories, setCategories] = useState<CategoryItem[]>([])
  const [brands, setBrands] = useState<BrandItem[]>([])
  const [isLoading, setIsLoading] = useState(true)

  // Create Dialog state
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [newName, setNewName] = useState('')
  const [newSlug, setNewSlug] = useState('')

  // Edit Dialog state
  const [editingCategory, setEditingCategory] = useState<CategoryItem | null>(null)
  const [editCatName, setEditCatName] = useState('')
  const [editCatSlug, setEditCatSlug] = useState('')
  const [editCatSortOrder, setEditCatSortOrder] = useState(0)
  const [editCatIsActive, setEditCatIsActive] = useState(true)

  const [editingBrand, setEditingBrand] = useState<BrandItem | null>(null)
  const [editBrandName, setEditBrandName] = useState('')
  const [editBrandSlug, setEditBrandSlug] = useState('')
  const [editBrandIsActive, setEditBrandIsActive] = useState(true)

  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  const fetchData = useCallback(async () => {
    setIsLoading(true)
    setErrorMsg(null)
    try {
      const [catRes, brandRes] = await Promise.all([
        api.GET('/api/v1/admin/categories'),
        api.GET('/api/v1/admin/brands'),
      ])
      if (catRes.data) setCategories(catRes.data as unknown as CategoryItem[])
      if (brandRes.data) setBrands(brandRes.data as unknown as BrandItem[])
    } catch {
      setErrorMsg('Không thể tải dữ liệu danh mục & thương hiệu.')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void fetchData()
  }, [fetchData])

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!newName.trim()) return

    try {
      if (activeTab === 'categories') {
        const res = await api.POST('/api/v1/admin/categories', {
          body: {
            parentId: null,
            name: newName.trim(),
            slug: newSlug.trim() || null,
            description: null,
            imageKey: null,
            sortOrder: 0,
            isActive: true,
          },
        })
        if (res.error) {
          alert((res.error as { detail?: string })?.detail || 'Không thể tạo mới danh mục.')
          return
        }
      } else {
        const res = await api.POST('/api/v1/admin/brands', {
          body: {
            name: newName.trim(),
            slug: newSlug.trim() || null,
            logoKey: null,
            isActive: true,
          },
        })
        if (res.error) {
          alert((res.error as { detail?: string })?.detail || 'Không thể tạo mới thương hiệu.')
          return
        }
      }
      setIsDialogOpen(false)
      setNewName('')
      setNewSlug('')
      await fetchData()
    } catch {
      alert('Không thể tạo mới.')
    }
  }

  // Edit Category
  const openEditCategory = (c: CategoryItem) => {
    setEditingCategory(c)
    setEditCatName(c.name)
    setEditCatSlug(c.slug)
    setEditCatSortOrder(c.sortOrder)
    setEditCatIsActive(c.isActive)
  }

  const handleUpdateCategory = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingCategory) return

    try {
      const res = await api.PUT('/api/v1/admin/categories/{id}', {
        params: { path: { id: editingCategory.id } },
        body: {
          parentId: editingCategory.parentId ?? null,
          name: editCatName.trim(),
          slug: editCatSlug.trim(),
          description: null,
          imageKey: null,
          sortOrder: editCatSortOrder,
          isActive: editCatIsActive,
        },
      })

      if (res.error) {
        alert((res.error as { detail?: string })?.detail || 'Cập nhật danh mục thất bại.')
        return
      }

      setEditingCategory(null)
      await fetchData()
    } catch {
      alert('Lỗi cập nhật danh mục.')
    }
  }

  // Delete Category
  const handleDeleteCategory = async (id: string, name: string) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa danh mục "${name}"?`)) return

    try {
      const res = await api.DELETE('/api/v1/admin/categories/{id}', {
        params: { path: { id } },
      })

      if (res.error) {
        alert((res.error as { detail?: string })?.detail || 'Không thể xóa danh mục (có thể đang chứa sản phẩm hoặc danh mục con).')
        return
      }

      await fetchData()
    } catch {
      alert('Lỗi khi xóa danh mục.')
    }
  }

  // Edit Brand
  const openEditBrand = (b: BrandItem) => {
    setEditingBrand(b)
    setEditBrandName(b.name)
    setEditBrandSlug(b.slug)
    setEditBrandIsActive(b.isActive)
  }

  const handleUpdateBrand = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingBrand) return

    try {
      const res = await api.PUT('/api/v1/admin/brands/{id}', {
        params: { path: { id: editingBrand.id } },
        body: {
          name: editBrandName.trim(),
          slug: editBrandSlug.trim(),
          logoKey: null,
          isActive: editBrandIsActive,
        },
      })

      if (res.error) {
        alert((res.error as { detail?: string })?.detail || 'Cập nhật thương hiệu thất bại.')
        return
      }

      setEditingBrand(null)
      await fetchData()
    } catch {
      alert('Lỗi cập nhật thương hiệu.')
    }
  }

  // Delete Brand
  const handleDeleteBrand = async (id: string, name: string) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa thương hiệu "${name}"?`)) return

    try {
      const res = await api.DELETE('/api/v1/admin/brands/{id}', {
        params: { path: { id } },
      })

      if (res.error) {
        alert((res.error as { detail?: string })?.detail || 'Không thể xóa thương hiệu (có thể đang gắn với sản phẩm).')
        return
      }

      await fetchData()
    } catch {
      alert('Lỗi khi xóa thương hiệu.')
    }
  }

  const categoryColumns: Column<CategoryItem>[] = [
    {
      key: 'name',
      title: 'Tên danh mục',
      sortable: true,
      render: c => (
        <div className="font-semibold text-slate-800 flex items-center gap-2">
          <FolderTree className="w-4 h-4 text-leaf shrink-0" strokeWidth={1.5} />
          <span>{c.name}</span>
        </div>
      ),
    },
    {
      key: 'slug',
      title: 'Đường dẫn (Slug)',
      render: c => <span className="font-mono text-xs text-slate-400">{c.slug}</span>,
    },
    {
      key: 'sortOrder',
      title: 'Thứ tự',
      sortable: true,
      render: c => <span className="text-xs font-semibold text-slate-600">{c.sortOrder}</span>,
    },
    {
      key: 'isActive',
      title: 'Trạng thái',
      render: c => (
        <span className={`px-2 py-0.5 rounded-full text-xs font-semibold ${c.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
          {c.isActive ? 'Hoạt động' : 'Ẩn'}
        </span>
      ),
    },
    {
      key: 'actions',
      title: 'Thao tác',
      render: c => (
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => openEditCategory(c)}
            className="p-1.5 text-slate-500 hover:text-leaf hover:bg-slate-100 rounded-lg transition-colors"
            title="Chỉnh sửa danh mục"
          >
            <Edit className="w-4 h-4" strokeWidth={1.5} />
          </button>
          <button
            type="button"
            onClick={() => void handleDeleteCategory(c.id, c.name)}
            className="p-1.5 text-slate-500 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors"
            title="Xóa danh mục"
          >
            <Trash2 className="w-4 h-4" strokeWidth={1.5} />
          </button>
        </div>
      ),
    },
  ]

  const brandColumns: Column<BrandItem>[] = [
    {
      key: 'name',
      title: 'Tên thương hiệu',
      sortable: true,
      render: b => (
        <div className="font-semibold text-slate-800 flex items-center gap-2">
          <Building2 className="w-4 h-4 text-indigo-600 shrink-0" strokeWidth={1.5} />
          <span>{b.name}</span>
        </div>
      ),
    },
    {
      key: 'slug',
      title: 'Đường dẫn (Slug)',
      render: b => <span className="font-mono text-xs text-slate-400">{b.slug}</span>,
    },
    {
      key: 'isActive',
      title: 'Trạng thái',
      render: b => (
        <span className={`px-2 py-0.5 rounded-full text-xs font-semibold ${b.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
          {b.isActive ? 'Hoạt động' : 'Ẩn'}
        </span>
      ),
    },
    {
      key: 'actions',
      title: 'Thao tác',
      render: b => (
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => openEditBrand(b)}
            className="p-1.5 text-slate-500 hover:text-leaf hover:bg-slate-100 rounded-lg transition-colors"
            title="Chỉnh sửa thương hiệu"
          >
            <Edit className="w-4 h-4" strokeWidth={1.5} />
          </button>
          <button
            type="button"
            onClick={() => void handleDeleteBrand(b.id, b.name)}
            className="p-1.5 text-slate-500 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors"
            title="Xóa thương hiệu"
          >
            <Trash2 className="w-4 h-4" strokeWidth={1.5} />
          </button>
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 font-display">Danh mục & Thương hiệu</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Phân loại cây quầy hàng (Aisles) và quản lý nhà sản xuất phân phối.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setIsDialogOpen(true)}
            className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl bg-leaf text-white text-xs font-semibold hover:bg-leaf-2 transition-colors shadow-xs"
          >
            <Plus className="w-4 h-4" strokeWidth={1.5} />
            {activeTab === 'categories' ? 'Thêm danh mục mới' : 'Thêm thương hiệu mới'}
          </button>
          <button
            type="button"
            onClick={() => void fetchData()}
            disabled={isLoading}
            className="p-2 rounded-xl border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 transition-colors shadow-2xs"
            title="Làm mới"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} strokeWidth={1.5} />
          </button>
        </div>
      </div>

      {errorMsg && (
        <div className="p-3 bg-rose-50 border border-rose-200 text-rose-700 text-xs rounded-xl flex items-center gap-2">
          <AlertCircle className="w-4 h-4 shrink-0" strokeWidth={1.5} />
          <span>{errorMsg}</span>
        </div>
      )}

      {/* Tabs */}
      <div className="flex items-center gap-2 border-b border-slate-200">
        <button
          type="button"
          onClick={() => setActiveTab('categories')}
          className={`flex items-center gap-2 px-4 py-2 border-b-2 text-xs font-bold transition-colors ${
            activeTab === 'categories'
              ? 'border-leaf text-leaf'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          <FolderTree className="w-4 h-4" strokeWidth={1.5} />
          <span>Danh mục hàng ({categories.length})</span>
        </button>
        <button
          type="button"
          onClick={() => setActiveTab('brands')}
          className={`flex items-center gap-2 px-4 py-2 border-b-2 text-xs font-bold transition-colors ${
            activeTab === 'brands'
              ? 'border-leaf text-leaf'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          <Building2 className="w-4 h-4" strokeWidth={1.5} />
          <span>Thương hiệu ({brands.length})</span>
        </button>
      </div>

      {/* Tables */}
      {activeTab === 'categories' ? (
        <DataTable
          data={categories}
          columns={categoryColumns}
          keyExtractor={c => c.id}
          searchPlaceholder="Tìm theo tên danh mục..."
          searchableKey={c => c.name}
          pageSize={15}
          isLoading={isLoading}
          emptyMessage="Chưa có danh mục hàng hóa nào"
        />
      ) : (
        <DataTable
          data={brands}
          columns={brandColumns}
          keyExtractor={b => b.id}
          searchPlaceholder="Tìm theo tên thương hiệu..."
          searchableKey={b => b.name}
          pageSize={15}
          isLoading={isLoading}
          emptyMessage="Chưa có thương hiệu nào"
        />
      )}

      {/* Create Modal Dialog */}
      {isDialogOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-xs">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-200 space-y-4">
            <h2 className="text-base font-bold text-slate-900 font-display">
              {activeTab === 'categories' ? 'Thêm danh mục hàng mới' : 'Thêm thương hiệu mới'}
            </h2>
            <form onSubmit={handleCreate} className="space-y-4 text-xs">
              <div>
                <label className="block font-semibold text-slate-700 mb-1">Tên *</label>
                <input
                  type="text"
                  required
                  value={newName}
                  onChange={e => setNewName(e.target.value)}
                  placeholder="Nhập tên..."
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>
              <div>
                <label className="block font-semibold text-slate-700 mb-1">Đường dẫn (Slug)</label>
                <input
                  type="text"
                  value={newSlug}
                  onChange={e => setNewSlug(e.target.value)}
                  placeholder="de-trong-tu-dong-tao"
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>
              <div className="flex items-center justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setIsDialogOpen(false)}
                  className="px-3 py-1.5 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 rounded-lg bg-leaf text-white font-semibold hover:bg-leaf-2 shadow-xs"
                >
                  Lưu
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit Category Modal */}
      {editingCategory && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-xs">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-200 space-y-4">
            <h2 className="text-base font-bold text-slate-900 font-display">
              Chỉnh sửa danh mục
            </h2>
            <form onSubmit={handleUpdateCategory} className="space-y-4 text-xs">
              <div>
                <label className="block font-semibold text-slate-700 mb-1">Tên danh mục *</label>
                <input
                  type="text"
                  required
                  value={editCatName}
                  onChange={e => setEditCatName(e.target.value)}
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>
              <div>
                <label className="block font-semibold text-slate-700 mb-1">Đường dẫn (Slug) *</label>
                <input
                  type="text"
                  required
                  value={editCatSlug}
                  onChange={e => setEditCatSlug(e.target.value)}
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>
              <div>
                <label className="block font-semibold text-slate-700 mb-1">Thứ tự hiển thị</label>
                <input
                  type="number"
                  value={editCatSortOrder}
                  onChange={e => setEditCatSortOrder(Number(e.target.value))}
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>
              <div className="flex items-center gap-2 pt-1">
                <input
                  type="checkbox"
                  id="catIsActive"
                  checked={editCatIsActive}
                  onChange={e => setEditCatIsActive(e.target.checked)}
                  className="w-4 h-4 text-leaf rounded focus:ring-leaf"
                />
                <label htmlFor="catIsActive" className="font-semibold text-slate-700 cursor-pointer">
                  Kích hoạt danh mục này
                </label>
              </div>
              <div className="flex items-center justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setEditingCategory(null)}
                  className="px-3 py-1.5 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 rounded-lg bg-leaf text-white font-semibold hover:bg-leaf-2 shadow-xs"
                >
                  Lưu thay đổi
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit Brand Modal */}
      {editingBrand && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-xs">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-200 space-y-4">
            <h2 className="text-base font-bold text-slate-900 font-display">
              Chỉnh sửa thương hiệu
            </h2>
            <form onSubmit={handleUpdateBrand} className="space-y-4 text-xs">
              <div>
                <label className="block font-semibold text-slate-700 mb-1">Tên thương hiệu *</label>
                <input
                  type="text"
                  required
                  value={editBrandName}
                  onChange={e => setEditBrandName(e.target.value)}
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>
              <div>
                <label className="block font-semibold text-slate-700 mb-1">Đường dẫn (Slug) *</label>
                <input
                  type="text"
                  required
                  value={editBrandSlug}
                  onChange={e => setEditBrandSlug(e.target.value)}
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>
              <div className="flex items-center gap-2 pt-1">
                <input
                  type="checkbox"
                  id="brandIsActive"
                  checked={editBrandIsActive}
                  onChange={e => setEditBrandIsActive(e.target.checked)}
                  className="w-4 h-4 text-leaf rounded focus:ring-leaf"
                />
                <label htmlFor="brandIsActive" className="font-semibold text-slate-700 cursor-pointer">
                  Kích hoạt thương hiệu này
                </label>
              </div>
              <div className="flex items-center justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setEditingBrand(null)}
                  className="px-3 py-1.5 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 rounded-lg bg-leaf text-white font-semibold hover:bg-leaf-2 shadow-xs"
                >
                  Lưu thay đổi
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
