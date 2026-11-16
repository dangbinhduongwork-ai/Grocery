import React, { useEffect, useState, useMemo } from 'react'
import {
  Search,
  ShoppingCart,
  Package,
  Boxes,
  Users,
  Shield,
  FileText,
  BarChart3,
  Settings,
  Tag,
  Star,
  PlusCircle,
  Truck,
  Building2,
  X,
  Store,
  Palette,
} from 'lucide-react'
import { useRouter } from '@/app/router'
import { useAuth } from '@/shared/auth'

export interface CommandItem {
  id: string
  title: string
  category: string
  path: string
  icon: React.ElementType
  permission?: string
}

export const CommandPalette: React.FC<{ isOpen: boolean; onClose: () => void }> = ({
  isOpen,
  onClose,
}) => {
  const router = useRouter()
  const { hasPermission } = useAuth()
  const [query, setQuery] = useState('')

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault()
        if (isOpen) {
          onClose()
        } else {
          // Open
        }
      }
      if (e.key === 'Escape' && isOpen) {
        onClose()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, onClose])

  const allItems: CommandItem[] = useMemo(
    () => [
      { id: 'orders', title: 'Danh sách đơn hàng', category: 'Bán hàng', path: '/admin/orders', icon: ShoppingCart, permission: 'orders.read' },
      { id: 'picking', title: 'Chế độ lấy hàng (Picking Mode)', category: 'Bán hàng', path: '/admin/picking', icon: Truck, permission: 'orders.read' },
      { id: 'customers', title: 'Quản lý khách hàng', category: 'Bán hàng', path: '/admin/customers', icon: Users, permission: 'customers.read' },

      { id: 'inventory', title: 'Quản lý tồn kho', category: 'Kho vận', path: '/admin/inventory', icon: Boxes, permission: 'inventory.read' },
      { id: 'purchasing', title: 'Đơn mua hàng (PO)', category: 'Kho vận', path: '/admin/purchasing', icon: Package, permission: 'purchasing.read' },
      { id: 'suppliers', title: 'Nhà cung cấp', category: 'Kho vận', path: '/admin/suppliers', icon: Building2, permission: 'purchasing.read' },

      { id: 'products', title: 'Danh sách sản phẩm', category: 'Danh mục', path: '/admin/products', icon: Package, permission: 'catalog.read' },
      { id: 'new-product', title: 'Thêm sản phẩm mới', category: 'Danh mục', path: '/admin/products/new', icon: PlusCircle, permission: 'catalog.write' },
      { id: 'categories', title: 'Danh mục & Thương hiệu', category: 'Danh mục', path: '/admin/categories', icon: Tag, permission: 'catalog.read' },
      { id: 'reviews', title: 'Kiểm duyệt đánh giá', category: 'Danh mục', path: '/admin/reviews', icon: Star, permission: 'reviews.moderate' },

      { id: 'revenue-report', title: 'Báo cáo doanh thu', category: 'Báo cáo', path: '/admin/reports/revenue', icon: BarChart3, permission: 'reports.revenue' },
      { id: 'profit-report', title: 'Báo cáo lợi nhuận & COGS', category: 'Báo cáo', path: '/admin/reports/profit', icon: BarChart3, permission: 'reports.profit' },

      { id: 'coupons', title: 'Mã giảm giá', category: 'Khuyến mãi', path: '/admin/coupons', icon: Tag, permission: 'promotions.manage' },
      { id: 'ui-design', title: 'Banner & Giao diện Storefront', category: 'Khuyến mãi', path: '/admin/ui-design', icon: Palette, permission: 'catalog.write' },

      { id: 'back-to-shop', title: 'Quay lại Cửa hàng (Shop)', category: 'Hệ thống', path: '/', icon: Store },
      { id: 'staff', title: 'Quản lý nhân viên', category: 'Hệ thống', path: '/admin/staff', icon: Users, permission: 'staff.read' },
      { id: 'roles', title: 'Phân quyền & Vai trò', category: 'Hệ thống', path: '/admin/roles', icon: Shield, permission: 'roles.manage' },
      { id: 'audit', title: 'Nhật ký kiểm toán (Audit Logs)', category: 'Hệ thống', path: '/admin/audit-logs', icon: FileText, permission: 'audit.read' },
      { id: 'settings', title: 'Cài đặt hệ thống', category: 'Hệ thống', path: '/admin/settings', icon: Settings, permission: 'settings.manage' },
    ],
    []
  )

  const allowedItems = useMemo(() => {
    return allItems.filter(item => !item.permission || hasPermission(item.permission))
  }, [allItems, hasPermission])

  const filteredItems = useMemo(() => {
    if (!query.trim()) return allowedItems
    const q = query.toLowerCase()
    return allowedItems.filter(item =>
      item.title.toLowerCase().includes(q) || item.category.toLowerCase().includes(q)
    )
  }, [allowedItems, query])

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center pt-20 px-4 bg-black/40 backdrop-blur-xs animate-in fade-in-0 duration-200">
      <div className="bg-white w-full max-w-xl rounded-2xl shadow-2xl border border-slate-200 overflow-hidden">
        {/* Input */}
        <div className="p-3.5 border-b border-slate-100 flex items-center gap-3">
          <Search className="w-5 h-5 text-slate-400 shrink-0" strokeWidth={1.5} />
          <input
            type="text"
            value={query}
            onChange={e => setQuery(e.target.value)}
            placeholder="Tìm tính năng hoặc điều hướng nhanh..."
            autoFocus
            className="w-full bg-transparent text-sm text-slate-800 placeholder-slate-400 focus:outline-hidden"
          />
          <button
            type="button"
            onClick={onClose}
            className="p-1 rounded-md text-slate-400 hover:text-slate-600 hover:bg-slate-100"
          >
            <X className="w-4 h-4" strokeWidth={1.5} />
          </button>
        </div>

        {/* Results */}
        <div className="max-h-80 overflow-y-auto p-2 divide-y divide-slate-50">
          {filteredItems.length === 0 ? (
            <div className="p-8 text-center text-xs text-slate-400">
              Không tìm thấy thao tác phù hợp với &ldquo;{query}&rdquo;
            </div>
          ) : (
            filteredItems.map(item => {
              const Icon = item.icon
              return (
                <button
                  key={item.id}
                  type="button"
                  onClick={() => {
                    onClose()
                    router.push(item.path)
                  }}
                  className="w-full p-2.5 flex items-center justify-between text-left rounded-lg hover:bg-slate-50 transition-colors group"
                >
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 rounded-lg bg-slate-100 group-hover:bg-leaf/10 text-slate-600 group-hover:text-leaf flex items-center justify-center transition-colors">
                      <Icon className="w-4 h-4" strokeWidth={1.5} />
                    </div>
                    <span className="text-sm font-medium text-slate-700 group-hover:text-slate-900">
                      {item.title}
                    </span>
                  </div>
                  <span className="text-xs text-slate-400 font-medium px-2 py-0.5 rounded-full bg-slate-100">
                    {item.category}
                  </span>
                </button>
              )
            })
          )}
        </div>

        {/* Footer */}
        <div className="p-2.5 bg-slate-50 border-t border-slate-100 flex items-center justify-between text-[11px] text-slate-400">
          <span>Dùng phím Esc để đóng</span>
          <span>GroceryHub Back-Office</span>
        </div>
      </div>
    </div>
  )
}
