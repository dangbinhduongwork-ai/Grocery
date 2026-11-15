import React, { useState } from 'react'
import { DeliveryStrip } from '@/shared/components/DeliveryStrip'
import { useAuth } from '@/shared/auth/AuthContext'
import { useCart } from '@/shared/cart/CartContext'
import { vi } from '@/shared/i18n/vi'
import { cn } from '@/shared/utils/cn'
import {
  ShoppingBag,
  Search,
  User,
  Package,
  Layers,
  LogOut,
  Menu,
  X
} from 'lucide-react'

interface StorefrontLayoutProps {
  children: React.ReactNode
  currentPath?: string
  onNavigate?: (path: string) => void
}

export const StorefrontLayout: React.FC<StorefrontLayoutProps> = ({
  children,
  currentPath = '/',
  onNavigate,
}) => {
  const { user, isAuthenticated, logout } = useAuth()
  const { itemCount } = useCart()
  const [searchQuery, setSearchQuery] = useState('')
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (searchQuery.trim()) {
      onNavigate?.(`/search?q=${encodeURIComponent(searchQuery.trim())}`)
    }
  }

  return (
    <div className="min-h-screen flex flex-col bg-paper text-ink selection:bg-sprout/40">
      {/* Top Banner & Promise */}
      <DeliveryStrip />

      {/* Main Header */}
      <header className="sticky top-0 z-40 bg-paper/95 backdrop-blur-md border-b border-rule">
        <div className="max-w-[1240px] mx-auto px-4 h-16 flex items-center justify-between gap-4">
          {/* Logo */}
          <div
            onClick={() => onNavigate?.('/')}
            className="flex items-center gap-2 cursor-pointer select-none"
          >
            <div className="w-9 h-9 rounded-[8px] bg-leaf text-paper flex items-center justify-center font-display font-bold text-xl shadow-xs">
              G
            </div>
            <div>
              <span className="font-display font-bold text-xl text-leaf tracking-tight block leading-none">
                {vi.appName}
              </span>
              <span className="text-[10px] text-ink-2 font-medium tracking-wide">
                {vi.tagline}
              </span>
            </div>
          </div>

          {/* Search Bar (Desktop) */}
          <form
            onSubmit={handleSearchSubmit}
            className="hidden md:flex flex-1 max-w-md relative"
          >
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder={vi.nav.searchPlaceholder}
              className="w-full h-10 pl-10 pr-4 rounded-full border border-rule bg-paper-2 text-sm text-ink placeholder:text-ink-2/60 focus:outline-none focus:border-leaf focus:bg-white focus:ring-1 focus:ring-leaf transition-all"
            />
            <Search className="w-4 h-4 text-ink-2 absolute left-3.5 top-3 pointer-events-none stroke-[1.5]" />
          </form>

          {/* Actions */}
          <div className="flex items-center gap-2 sm:gap-4">
            {/* Design Route Pill (Dev tool) */}
            <button
              type="button"
              onClick={() => onNavigate?.('/__design')}
              title={vi.nav.designSystem}
              className={cn(
                "hidden lg:flex items-center gap-1.5 px-2.5 py-1.5 rounded-full text-xs font-semibold transition-colors cursor-pointer",
                currentPath === '/__design'
                  ? "bg-leaf text-paper"
                  : "bg-[#E8EDE5] text-leaf hover:bg-[#D8E3D4]"
              )}
            >
              <Layers className="w-3.5 h-3.5 stroke-[1.5]" />
              <span>UI Design</span>
            </button>

            {/* My Orders */}
            {isAuthenticated && (
              <button
                type="button"
                onClick={() => onNavigate?.('/orders')}
                className={cn(
                  "hidden sm:flex items-center gap-1.5 text-sm font-medium transition-colors cursor-pointer p-1.5 rounded-md",
                  currentPath.startsWith('/orders')
                    ? "text-leaf font-semibold bg-paper-2"
                    : "text-ink hover:text-leaf"
                )}
              >
                <Package className="w-4 h-4 text-leaf stroke-[1.5]" />
                <span>{vi.nav.orders}</span>
              </button>
            )}

            {/* User / Auth */}
            {isAuthenticated ? (
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => onNavigate?.('/account')}
                  className={cn(
                    "flex items-center gap-1.5 text-sm font-medium transition-colors cursor-pointer p-1.5 rounded-md",
                    currentPath.startsWith('/account')
                      ? "text-leaf font-semibold bg-paper-2"
                      : "text-ink hover:text-leaf"
                  )}
                >
                  <User className="w-4 h-4 text-leaf stroke-[1.5]" />
                  <span className="hidden sm:inline max-w-[100px] truncate">
                    {user?.fullName ?? 'Tài khoản'}
                  </span>
                </button>
                <button
                  type="button"
                  onClick={() => void logout()}
                  title={vi.nav.logout}
                  className="p-1.5 text-ink-2 hover:text-tomato transition-colors cursor-pointer"
                >
                  <LogOut className="w-4 h-4 stroke-[1.5]" />
                </button>
              </div>
            ) : (
              <button
                type="button"
                onClick={() => onNavigate?.('/login')}
                className={cn(
                  "flex items-center gap-1.5 text-sm font-medium transition-colors cursor-pointer p-1.5 rounded-md",
                  currentPath === '/login'
                    ? "text-leaf font-semibold bg-paper-2"
                    : "text-ink hover:text-leaf"
                )}
              >
                <User className="w-4 h-4 text-leaf stroke-[1.5]" />
                <span>{vi.nav.login}</span>
              </button>
            )}

            {/* Cart Button */}
            <button
              type="button"
              onClick={() => onNavigate?.('/cart')}
              className={cn(
                "relative p-2 rounded-full border border-rule text-ink transition-colors cursor-pointer flex items-center justify-center",
                currentPath === '/cart'
                  ? "bg-leaf/10 border-leaf"
                  : "bg-paper-2 hover:bg-[#E8E2D5]"
              )}
              aria-label="Xem giỏ hàng"
            >
              <ShoppingBag className="w-5 h-5 text-leaf stroke-[1.5]" />
              {itemCount > 0 && (
                <span className="absolute -top-1 -right-1 bg-tomato text-white text-[11px] font-bold rounded-full w-5 h-5 flex items-center justify-center tabular-nums shadow-xs">
                  {itemCount > 99 ? '99+' : itemCount}
                </span>
              )}
            </button>

            {/* Mobile menu trigger */}
            <button
              type="button"
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              className="md:hidden p-1.5 text-ink cursor-pointer"
            >
              {mobileMenuOpen ? (
                <X className="w-6 h-6 stroke-[1.5]" />
              ) : (
                <Menu className="w-6 h-6 stroke-[1.5]" />
              )}
            </button>
          </div>
        </div>

        {/* Mobile Search Bar */}
        <div className="md:hidden px-4 pb-3">
          <form onSubmit={handleSearchSubmit} className="relative">
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder={vi.nav.searchPlaceholder}
              className="w-full h-9 pl-9 pr-4 rounded-full border border-rule bg-paper-2 text-xs text-ink placeholder:text-ink-2/60 focus:outline-none focus:border-leaf focus:bg-white"
            />
            <Search className="w-3.5 h-3.5 text-ink-2 absolute left-3 top-2.5 pointer-events-none stroke-[1.5]" />
          </form>
        </div>
      </header>

      {/* Main Page Content */}
      <main className="flex-1 max-w-[1240px] w-full mx-auto px-4 py-6">
        {children}
      </main>

      {/* Storefront Footer */}
      <footer className="border-t border-rule bg-paper-2 mt-12 py-10 text-ink-2 text-xs sm:text-sm">
        <div className="max-w-[1240px] mx-auto px-4 grid grid-cols-1 md:grid-cols-4 gap-8">
          <div>
            <div className="flex items-center gap-2 mb-3">
              <div className="w-7 h-7 rounded-[6px] bg-leaf text-paper flex items-center justify-center font-display font-bold text-base">
                G
              </div>
              <span className="font-display font-bold text-lg text-leaf">
                {vi.appName}
              </span>
            </div>
            <p className="text-xs text-ink-2/80 leading-relaxed mb-3">
              Hệ thống cung cấp nông sản, thực phẩm tươi sạch đạt chuẩn an toàn vệ sinh thực phẩm cho gia đình Việt.
            </p>
            <p className="text-xs font-semibold text-ink">Hotline: 1900 8888 (7:00 - 21:00)</p>
          </div>

          <div>
            <h4 className="font-semibold text-ink mb-3 font-display">Danh mục sản phẩm</h4>
            <ul className="space-y-1.5 text-xs text-ink-2">
              <li className="hover:text-leaf cursor-pointer" onClick={() => onNavigate?.('/c/rau-cu-qua')}>Rau củ hữu cơ</li>
              <li className="hover:text-leaf cursor-pointer" onClick={() => onNavigate?.('/c/thit-trung-hai-san')}>Thịt tươi & Hải sản</li>
              <li className="hover:text-leaf cursor-pointer" onClick={() => onNavigate?.('/c/trai-cay-tuoi')}>Trái cây bốn mùa</li>
              <li className="hover:text-leaf cursor-pointer" onClick={() => onNavigate?.('/c/gao-va-ngu-coc')}>Gạo sạch & Ngũ cốc</li>
            </ul>
          </div>

          <div>
            <h4 className="font-semibold text-ink mb-3 font-display">Chính sách & Hỗ trợ</h4>
            <ul className="space-y-1.5 text-xs text-ink-2">
              <li className="hover:text-leaf cursor-pointer">Chính sách giao hàng trong 2h</li>
              <li className="hover:text-leaf cursor-pointer">Chính sách đổi trả trong 24h</li>
              <li className="hover:text-leaf cursor-pointer">Phương thức thanh toán</li>
              <li className="hover:text-leaf cursor-pointer">Cam kết nguồn gốc xuất xứ</li>
            </ul>
          </div>

          <div>
            <h4 className="font-semibold text-ink mb-3 font-display">Phương thức thanh toán</h4>
            <div className="flex flex-wrap gap-2 mb-4">
              <span className="px-2 py-1 rounded-[4px] border border-rule bg-white text-xs font-medium text-ink">
                COD (Tiền mặt)
              </span>
              <span className="px-2 py-1 rounded-[4px] border border-rule bg-white text-xs font-medium text-ink">
                VietQR (SePay)
              </span>
            </div>
            <p className="text-xs text-ink-2/70">
              © 2026 GroceryHub Vietnam. Tất cả quyền được bảo lưu.
            </p>
          </div>
        </div>
      </footer>
    </div>
  )
}
