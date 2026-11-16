import React, { useState, useMemo } from 'react'
import {
  LayoutDashboard,
  ShoppingCart,
  Boxes,
  Package,
  Users,
  Shield,
  FileText,
  BarChart3,
  Settings,
  Tag,
  Star,
  Search,
  LogOut,
  Menu,
  Building2,
  Truck,
  CreditCard,
  RotateCcw,
  Palette,
  Store,
} from 'lucide-react'
import { useRouter } from '@/app/router'
import { useAuth } from '@/shared/auth'
import { CommandPalette } from '../components/CommandPalette'

interface NavItem {
  id: string
  title: string
  path: string
  icon: React.ElementType
  permission?: string
}

interface NavGroup {
  id: string
  title: string
  items: NavItem[]
}

export const AdminLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const router = useRouter()
  const { user, logout, hasPermission } = useAuth()
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const [isCommandOpen, setIsCommandOpen] = useState(false)

  const navGroups: NavGroup[] = useMemo(
    () => [
      {
        id: 'sales',
        title: 'Bán hàng',
        items: [
          { id: 'orders', title: 'Đơn hàng', path: '/admin/orders', icon: ShoppingCart, permission: 'orders.read' },
          { id: 'picking', title: 'Lấy hàng (Picking)', path: '/admin/picking', icon: Truck, permission: 'orders.read' },
          { id: 'customers', title: 'Khách hàng', path: '/admin/customers', icon: Users, permission: 'customers.read' },
        ],
      },
      {
        id: 'warehouse',
        title: 'Kho vận',
        items: [
          { id: 'inventory', title: 'Tồn kho', path: '/admin/inventory', icon: Boxes, permission: 'inventory.read' },
          { id: 'purchasing', title: 'Nhập hàng (PO)', path: '/admin/purchasing', icon: Package, permission: 'purchasing.read' },
          { id: 'suppliers', title: 'Nhà cung cấp', path: '/admin/suppliers', icon: Building2, permission: 'purchasing.read' },
        ],
      },
      {
        id: 'catalog',
        title: 'Danh mục',
        items: [
          { id: 'products', title: 'Sản phẩm', path: '/admin/products', icon: Package, permission: 'catalog.read' },
          { id: 'categories', title: 'Danh mục & Brand', path: '/admin/categories', icon: Tag, permission: 'catalog.read' },
          { id: 'reviews', title: 'Đánh giá', path: '/admin/reviews', icon: Star, permission: 'reviews.moderate' },
        ],
      },
      {
        id: 'marketing',
        title: 'Marketing & Giao diện',
        items: [
          { id: 'ui-design', title: 'Banner & Giao diện', path: '/admin/ui-design', icon: Palette, permission: 'catalog.write' },
          { id: 'coupons', title: 'Mã giảm giá', path: '/admin/coupons', icon: Tag, permission: 'promotions.manage' },
        ],
      },
      {
        id: 'analytics',
        title: 'Báo cáo',
        items: [
          { id: 'reports-revenue', title: 'Báo cáo doanh thu', path: '/admin/reports/revenue', icon: BarChart3, permission: 'reports.revenue' },
          { id: 'reports-profit', title: 'Lợi nhuận & COGS', path: '/admin/reports/profit', icon: BarChart3, permission: 'reports.profit' },
        ],
      },
      {
        id: 'finance',
        title: 'Thanh toán & Thu chi',
        items: [
          { id: 'payments', title: 'Giao dịch SePay', path: '/admin/payments', icon: CreditCard, permission: 'payments.review' },
          { id: 'refunds', title: 'Yêu cầu hoàn tiền', path: '/admin/refunds', icon: RotateCcw, permission: 'refunds.request' },
        ],
      },
      {
        id: 'system',
        title: 'Hệ thống',
        items: [
          { id: 'staff', title: 'Nhân viên', path: '/admin/staff', icon: Users, permission: 'staff.read' },
          { id: 'roles', title: 'Phân quyền', path: '/admin/roles', icon: Shield, permission: 'roles.manage' },
          { id: 'audit', title: 'Nhật ký kiểm toán', path: '/admin/audit-logs', icon: FileText, permission: 'audit.read' },
          { id: 'settings', title: 'Cài đặt hệ thống', path: '/admin/settings', icon: Settings, permission: 'settings.manage' },
        ],
      },
    ],
    []
  )

  const filteredGroups = useMemo(() => {
    return navGroups
      .map(group => ({
        ...group,
        items: group.items.filter(item => !item.permission || hasPermission(item.permission)),
      }))
      .filter(group => group.items.length > 0)
  }, [navGroups, hasPermission])

  const userRole = user?.roles[0] ?? 'Staff'

  return (
    <div className="min-h-screen bg-[#F6F7F5] flex flex-col font-sans text-slate-800">
      {/* Top Header */}
      <header className="sticky top-0 z-40 bg-white border-b border-slate-200 h-16 flex items-center justify-between px-4 sm:px-6 shadow-2xs">
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => setSidebarOpen(!sidebarOpen)}
            className="p-2 rounded-lg text-slate-600 hover:bg-slate-100 transition-colors"
            title="Đóng / mở menu"
          >
            <Menu className="w-5 h-5" strokeWidth={1.5} />
          </button>
          <div
            onClick={() => router.push('/admin')}
            className="flex items-center gap-2 cursor-pointer select-none"
          >
            <span className="font-display font-extrabold text-xl text-leaf tracking-tight">GroceryHub</span>
            <span className="text-[11px] font-bold uppercase tracking-wider bg-slate-100 text-slate-700 px-2 py-0.5 rounded-md border border-slate-200">
              Back-Office
            </span>
          </div>
        </div>

        {/* Command palette search bar */}
        <div className="flex-1 max-w-md mx-4 hidden md:block">
          <button
            type="button"
            onClick={() => setIsCommandOpen(true)}
            className="w-full flex items-center justify-between px-3.5 py-1.5 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-400 hover:border-slate-300 hover:bg-slate-100/80 transition-all cursor-pointer"
          >
            <div className="flex items-center gap-2">
              <Search className="w-4 h-4 text-slate-400" strokeWidth={1.5} />
              <span>Tìm kiếm thao tác, báo cáo hoặc phím tắt...</span>
            </div>
            <kbd className="px-1.5 py-0.5 text-[10px] bg-white border border-slate-200 rounded-md shadow-2xs font-mono text-slate-500">
              Ctrl K
            </kbd>
          </button>
        </div>

        {/* User profile & actions */}
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => router.push('/')}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-leaf/10 text-leaf hover:bg-leaf hover:text-white font-medium text-xs transition-all cursor-pointer border border-leaf/20 shadow-2xs"
            title="Quay lại giao diện cửa hàng (Shop)"
          >
            <Store className="w-4 h-4" strokeWidth={1.75} />
            <span className="font-semibold">Quay lại Shop</span>
          </button>

          <div className="h-6 w-px bg-slate-200 hidden sm:block" />

          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-full bg-leaf/10 text-leaf font-bold flex items-center justify-center text-xs">
              {user?.fullName?.charAt(0) ?? 'A'}
            </div>
            <div className="hidden sm:block text-left">
              <p className="text-xs font-semibold text-slate-800 leading-tight">{user?.fullName ?? 'Nhân viên'}</p>
              <span className="text-[10px] font-medium text-slate-500 uppercase">{userRole}</span>
            </div>
          </div>

          <button
            type="button"
            onClick={async () => {
              await logout()
              router.push('/login')
            }}
            className="p-2 rounded-lg text-slate-400 hover:text-rose-600 hover:bg-rose-50 transition-colors ml-1"
            title="Đăng xuất"
          >
            <LogOut className="w-4 h-4" strokeWidth={1.5} />
          </button>
        </div>
      </header>

      {/* Main Container */}
      <div className="flex flex-1 overflow-hidden">
        {/* Left Sidebar */}
        <aside
          className={`${
            sidebarOpen ? 'w-64' : 'w-0 -translate-x-full md:w-16 md:translate-x-0'
          } shrink-0 bg-white border-r border-slate-200 transition-all duration-200 overflow-y-auto flex flex-col justify-between`}
        >
          <div className="p-3 space-y-4">
            {/* Quick Back to Store Button */}
            <div>
              <button
                type="button"
                onClick={() => router.push('/')}
                className="w-full flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-semibold text-leaf bg-leaf/10 hover:bg-leaf hover:text-white transition-all cursor-pointer border border-leaf/20 shadow-2xs"
                title="Quay lại giao diện cửa hàng"
              >
                <Store className="w-4 h-4 shrink-0" strokeWidth={1.75} />
                {sidebarOpen && <span>Quay lại Shop</span>}
              </button>
            </div>

            {/* Dashboard Link */}
            <div>
              <button
                type="button"
                onClick={() => router.push('/admin')}
                className={`w-full flex items-center gap-3 px-3 py-2 rounded-xl text-sm font-medium transition-colors ${
                  router.path === '/admin'
                    ? 'bg-leaf text-white font-semibold shadow-xs'
                    : 'text-slate-700 hover:bg-slate-100'
                }`}
              >
                <LayoutDashboard className="w-4 h-4 shrink-0" strokeWidth={1.5} />
                {sidebarOpen && <span>Tổng quan</span>}
              </button>
            </div>

            {/* Navigation Groups */}
            {filteredGroups.map(group => (
              <div key={group.id} className="space-y-1">
                {sidebarOpen && (
                  <h3 className="px-3 text-[11px] font-bold uppercase tracking-wider text-slate-400 select-none">
                    {group.title}
                  </h3>
                )}
                {group.items.map(item => {
                  const Icon = item.icon
                  const isActive = router.path === item.path || router.path.startsWith(`${item.path}/`)

                  return (
                    <button
                      key={item.id}
                      type="button"
                      onClick={() => router.push(item.path)}
                      className={`w-full flex items-center gap-3 px-3 py-2 rounded-xl text-xs font-medium transition-colors ${
                        isActive
                          ? 'bg-leaf/10 text-leaf font-semibold'
                          : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
                      }`}
                      title={!sidebarOpen ? item.title : undefined}
                    >
                      <Icon className="w-4 h-4 shrink-0" strokeWidth={1.5} />
                      {sidebarOpen && <span>{item.title}</span>}
                    </button>
                  )
                })}
              </div>
            ))}
          </div>

          {/* Sidebar Footer */}
          {sidebarOpen && (
            <div className="p-3 border-t border-slate-100 text-[11px] text-slate-400 flex items-center justify-between">
              <span>GroceryHub v1.0</span>
              <span className="font-mono text-[10px]">VN-2LEVEL</span>
            </div>
          )}
        </aside>

        {/* Content Area */}
        <main className="flex-1 overflow-y-auto p-4 sm:p-6 lg:p-8">
          <div className="max-w-7xl mx-auto">{children}</div>
        </main>
      </div>

      {/* Global Command Palette */}
      <CommandPalette isOpen={isCommandOpen} onClose={() => setIsCommandOpen(false)} />
    </div>
  )
}
