import React, { useState } from 'react'
import { DeliveryStrip } from '@/shared/components/DeliveryStrip'
import { ZaloContactButton } from '@/shared/components/ZaloContactButton'
import { AIChatBot } from '@/shared/components/AIChatBot'
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
  X,
  ShieldCheck,
  Phone,
  Mail,
  MapPin,
  Clock,
  CheckCircle2,
  QrCode,
  Truck,
  RotateCcw,
  Home,
  LayoutGrid,
  ChevronRight,
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
  const { user, isAuthenticated, isStaff, logout } = useAuth()
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
            {/* Design Route Pill (Dev tool - only for staff) */}
            {isStaff && (
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
            )}

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

            {/* Admin Back-Office Link */}
            {isStaff && (
              <button
                type="button"
                onClick={() => onNavigate?.('/admin')}
                className={cn(
                  "flex items-center gap-1.5 px-2.5 py-1.5 rounded-full text-xs font-semibold transition-colors cursor-pointer",
                  currentPath.startsWith('/admin')
                    ? "bg-leaf text-paper"
                    : "bg-tomato/10 text-tomato hover:bg-tomato/20"
                )}
                title="Trang quản trị Admin"
              >
                <ShieldCheck className="w-3.5 h-3.5 stroke-[1.5]" />
                <span>Quản trị</span>
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

      {/* Mobile Slide-in Drawer Menu (md:hidden) */}
      {mobileMenuOpen && (
        <div className="fixed inset-0 z-50 md:hidden">
          {/* Backdrop */}
          <div
            onClick={() => setMobileMenuOpen(false)}
            className="fixed inset-0 bg-black/60 backdrop-blur-xs transition-opacity animate-fade-in"
          />

          {/* Drawer Panel */}
          <div className="fixed inset-y-0 left-0 w-[84vw] max-w-xs bg-paper border-r border-rule shadow-2xl flex flex-col justify-between overflow-y-auto animate-in slide-in-from-left duration-200">
            <div>
              {/* Drawer Header */}
              <div className="p-4 bg-leaf text-paper flex items-center justify-between shadow-xs">
                <div className="flex items-center gap-2.5">
                  <div className="w-9 h-9 rounded-xl bg-white/20 text-white flex items-center justify-center font-display font-bold text-lg">
                    G
                  </div>
                  <div>
                    <h3 className="font-display font-bold text-base leading-tight">GroceryHub</h3>
                    <p className="text-[11px] text-paper/80 leading-tight">Nông sản tươi sạch mỗi ngày</p>
                  </div>
                </div>
                <button
                  type="button"
                  onClick={() => setMobileMenuOpen(false)}
                  className="p-1 rounded-lg text-white/80 hover:text-white hover:bg-white/10 cursor-pointer"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>

              {/* User Bar */}
              <div className="p-4 border-b border-rule bg-paper-2">
                {isAuthenticated ? (
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2.5">
                      <div className="w-9 h-9 rounded-full bg-leaf/15 text-leaf font-bold flex items-center justify-center text-sm">
                        {user?.fullName?.charAt(0).toUpperCase() || 'U'}
                      </div>
                      <div className="text-xs">
                        <p className="font-semibold text-ink line-clamp-1">{user?.fullName}</p>
                        <p className="text-[11px] text-ink-2 line-clamp-1">{user?.email}</p>
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={() => {
                        setMobileMenuOpen(false)
                        void logout()
                      }}
                      className="p-1.5 text-ink-2 hover:text-tomato cursor-pointer"
                      title="Đăng xuất"
                    >
                      <LogOut className="w-4 h-4" />
                    </button>
                  </div>
                ) : (
                  <div className="grid grid-cols-2 gap-2">
                    <button
                      type="button"
                      onClick={() => {
                        setMobileMenuOpen(false)
                        onNavigate?.('/login')
                      }}
                      className="h-9 rounded-xl bg-leaf text-paper font-semibold text-xs flex items-center justify-center gap-1.5 shadow-2xs cursor-pointer"
                    >
                      <User className="w-3.5 h-3.5" />
                      <span>Đăng nhập</span>
                    </button>
                    <button
                      type="button"
                      onClick={() => {
                        setMobileMenuOpen(false)
                        onNavigate?.('/register')
                      }}
                      className="h-9 rounded-xl border border-rule bg-white text-ink font-semibold text-xs flex items-center justify-center cursor-pointer"
                    >
                      Đăng ký
                    </button>
                  </div>
                )}
              </div>

              {/* Navigation Items */}
              <div className="p-3 space-y-1 text-xs">
                <button
                  type="button"
                  onClick={() => {
                    setMobileMenuOpen(false)
                    onNavigate?.('/')
                  }}
                  className={cn(
                    "w-full flex items-center justify-between px-3 py-2.5 rounded-xl font-medium transition-colors text-left cursor-pointer",
                    currentPath === '/' ? "bg-leaf text-paper font-semibold" : "text-ink hover:bg-paper-2"
                  )}
                >
                  <span className="flex items-center gap-2.5">
                    <Home className="w-4 h-4 stroke-[1.8]" />
                    <span>Trang chủ</span>
                  </span>
                  <ChevronRight className="w-4 h-4 opacity-50" />
                </button>

                <button
                  type="button"
                  onClick={() => {
                    setMobileMenuOpen(false)
                    onNavigate?.('/c/tat-ca')
                  }}
                  className={cn(
                    "w-full flex items-center justify-between px-3 py-2.5 rounded-xl font-medium transition-colors text-left cursor-pointer",
                    currentPath.startsWith('/c/') ? "bg-leaf text-paper font-semibold" : "text-ink hover:bg-paper-2"
                  )}
                >
                  <span className="flex items-center gap-2.5">
                    <LayoutGrid className="w-4 h-4 stroke-[1.8]" />
                    <span>Tất cả gian hàng</span>
                  </span>
                  <ChevronRight className="w-4 h-4 opacity-50" />
                </button>

                <div className="pt-2 pb-1 px-3 text-[10px] font-bold uppercase tracking-wider text-ink-2/70">
                  Gian hàng nổi bật
                </div>

                {[
                  { name: 'Rau củ quả sạch VietGAP', slug: 'rau-cu-qua', icon: '🥦' },
                  { name: 'Thịt tươi, Cá & Trứng gà', slug: 'thit-ca-trung', icon: '🥩' },
                  { name: 'Sữa & Chế phẩm dinh dưỡng', slug: 'sua-che-pham', icon: '🥛' },
                  { name: 'Gạo ST25 & Gia vị truyền thống', slug: 'do-kho-gia-vi', icon: '🌾' },
                  { name: 'Trái cây đặc sản nhiệt đới', slug: 'trai-cay-nhap-khau', icon: '🍎' },
                ].map((cat) => (
                  <button
                    key={cat.slug}
                    type="button"
                    onClick={() => {
                      setMobileMenuOpen(false)
                      onNavigate?.(`/c/${cat.slug}`)
                    }}
                    className="w-full flex items-center justify-between px-3 py-2 rounded-xl text-xs text-ink hover:bg-paper-2 transition-colors text-left cursor-pointer"
                  >
                    <span className="flex items-center gap-2">
                      <span>{cat.icon}</span>
                      <span>{cat.name}</span>
                    </span>
                    <ChevronRight className="w-3.5 h-3.5 opacity-40" />
                  </button>
                ))}

                <div className="pt-2 pb-1 px-3 text-[10px] font-bold uppercase tracking-wider text-ink-2/70 border-t border-rule/50 mt-2">
                  Tiện ích khách hàng
                </div>

                {isAuthenticated && (
                  <button
                    type="button"
                    onClick={() => {
                      setMobileMenuOpen(false)
                      onNavigate?.('/orders')
                    }}
                    className="w-full flex items-center justify-between px-3 py-2.5 rounded-xl font-medium text-ink hover:bg-paper-2 transition-colors text-left cursor-pointer"
                  >
                    <span className="flex items-center gap-2.5">
                      <Package className="w-4 h-4 text-leaf stroke-[1.8]" />
                      <span>Đơn hàng của tôi</span>
                    </span>
                    <ChevronRight className="w-4 h-4 opacity-50" />
                  </button>
                )}

                {isStaff && (
                  <button
                    type="button"
                    onClick={() => {
                      setMobileMenuOpen(false)
                      onNavigate?.('/admin')
                    }}
                    className="w-full flex items-center justify-between px-3 py-2.5 rounded-xl font-medium bg-tomato/10 text-tomato hover:bg-tomato/20 transition-colors text-left cursor-pointer"
                  >
                    <span className="flex items-center gap-2.5">
                      <ShieldCheck className="w-4 h-4 stroke-[1.8]" />
                      <span className="font-semibold">Trang quản trị Admin</span>
                    </span>
                    <ChevronRight className="w-4 h-4 opacity-50" />
                  </button>
                )}
              </div>
            </div>

            {/* Drawer Footer */}
            <div className="p-4 border-t border-rule bg-paper-2 text-xs space-y-2">
              <a
                href="https://zalo.me/0865087784"
                target="_blank"
                rel="noopener noreferrer"
                className="w-full h-9 rounded-xl bg-[#0068FF] hover:bg-[#0057d6] text-white font-semibold flex items-center justify-center gap-2 shadow-2xs"
              >
                <Phone className="w-3.5 h-3.5" />
                <span>Chat Zalo: 0865 087 784</span>
              </a>
              <p className="text-[11px] text-ink-2 text-center">
                Thôn 9, Suối Hai, Ba Vì, Hà Nội
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Main Page Content */}
      <main className="flex-1 max-w-[1240px] w-full mx-auto px-4 py-4 sm:py-6 pb-24 md:pb-6">
        {children}
      </main>

      {/* Storefront Footer */}
      <footer className="border-t border-rule bg-paper-2 mt-16 text-ink-2 text-xs">
        {/* Core Commitments Bar */}
        <div className="border-b border-rule bg-paper py-6">
          <div className="max-w-[1240px] mx-auto px-4 grid grid-cols-2 md:grid-cols-4 gap-6">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-leaf/10 text-leaf flex items-center justify-center shrink-0">
                <ShieldCheck className="w-5 h-5 stroke-[1.5]" />
              </div>
              <div>
                <h5 className="font-semibold text-ink text-xs">100% Nông sản VietGAP</h5>
                <p className="text-[11px] text-ink-2">Nguồn gốc rõ ràng, kiểm định nghiêm ngặt</p>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-leaf/10 text-leaf flex items-center justify-center shrink-0">
                <Truck className="w-5 h-5 stroke-[1.5]" />
              </div>
              <div>
                <h5 className="font-semibold text-ink text-xs">Giao hỏa tốc 2 giờ</h5>
                <p className="text-[11px] text-ink-2">Thùng giữ lạnh chuyên dụng tận cửa</p>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-leaf/10 text-leaf flex items-center justify-center shrink-0">
                <RotateCcw className="w-5 h-5 stroke-[1.5]" />
              </div>
              <div>
                <h5 className="font-semibold text-ink text-xs">Đổi trả trong 24 giờ</h5>
                <p className="text-[11px] text-ink-2">1 đổi 1 hoặc hoàn tiền nếu không tươi ngon</p>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-leaf/10 text-leaf flex items-center justify-center shrink-0">
                <QrCode className="w-5 h-5 stroke-[1.5]" />
              </div>
              <div>
                <h5 className="font-semibold text-ink text-xs">Thanh toán VietQR 24/7</h5>
                <p className="text-[11px] text-ink-2">Khớp lệnh tự động & bảo mật tuyệt đối</p>
              </div>
            </div>
          </div>
        </div>

        {/* Main Footer Links & Info */}
        <div className="max-w-[1240px] mx-auto px-4 py-12 grid grid-cols-1 md:grid-cols-4 gap-8">
          {/* Column 1: Brand & Contact Info */}
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 rounded-lg bg-leaf text-paper flex items-center justify-center font-display font-bold text-lg shadow-xs">
                G
              </div>
              <span className="font-display font-bold text-xl text-leaf tracking-tight">
                {vi.appName}
              </span>
            </div>
            <p className="text-xs text-ink-2 leading-relaxed">
              Chuỗi cung ứng thực phẩm tươi sạch hàng đầu — mang nguồn thực phẩm an toàn, tươi ngon từ nông trại hữu cơ chuẩn VietGAP đến bàn ăn gia đình bạn.
            </p>

            <div className="space-y-2 pt-2 border-t border-rule/60 text-xs">
              <div className="flex items-center gap-2">
                <Phone className="w-4 h-4 text-leaf shrink-0" />
                <span>Hotline / Zalo:</span>
                <a href="tel:0865087784" className="font-bold text-ink hover:text-leaf font-mono">0865 087 784</a>
              </div>
              <div className="flex items-center gap-2">
                <Mail className="w-4 h-4 text-leaf shrink-0" />
                <span>Email CSKH:</span>
                <a href="mailto:dangbinhduong.work@gmail.com" className="font-semibold text-ink hover:text-leaf">dangbinhduong.work@gmail.com</a>
              </div>
              <div className="flex items-start gap-2">
                <MapPin className="w-4 h-4 text-leaf shrink-0 mt-0.5" />
                <span>Địa chỉ: Thôn 9, Xã Suối Hai, Huyện Ba Vì, Hà Nội (Giao hàng toàn quốc)</span>
              </div>
              <div className="flex items-center gap-2">
                <Clock className="w-4 h-4 text-leaf shrink-0" />
                <span>Giờ mở cửa: 7:00 - 22:00 (Cả Thứ 7, CN)</span>
              </div>
            </div>
          </div>

          {/* Column 2: Product Categories */}
          <div>
            <h4 className="font-semibold text-ink mb-3 text-xs uppercase tracking-wider font-display">Danh mục sản phẩm</h4>
            <ul className="space-y-2 text-xs text-ink-2">
              <li className="hover:text-leaf cursor-pointer transition-colors" onClick={() => onNavigate?.('/c/rau-cu-qua')}>
                • Rau củ quả hữu cơ VietGAP
              </li>
              <li className="hover:text-leaf cursor-pointer transition-colors" onClick={() => onNavigate?.('/c/thit-ca-trung')}>
                • Thịt heo, bò tươi & thủy hải sản sạch
              </li>
              <li className="hover:text-leaf cursor-pointer transition-colors" onClick={() => onNavigate?.('/c/sua-che-pham')}>
                • Sữa hạt & chế phẩm sữa dinh dưỡng
              </li>
              <li className="hover:text-leaf cursor-pointer transition-colors" onClick={() => onNavigate?.('/c/do-kho-gia-vi')}>
                • Gạo ST25, đồ khô & gia vị truyền thống
              </li>
              <li className="hover:text-leaf cursor-pointer transition-colors" onClick={() => onNavigate?.('/c/trai-cay-nhap-khau')}>
                • Trái cây đặc sản nhiệt đới 3 miền
              </li>
            </ul>
          </div>

          {/* Column 3: Customer Care & Policies */}
          <div>
            <h4 className="font-semibold text-ink mb-3 text-xs uppercase tracking-wider font-display">Chính sách & Hỗ trợ</h4>
            <ul className="space-y-2 text-xs text-ink-2">
              <li className="hover:text-leaf cursor-pointer transition-colors">Chính sách giao hàng siêu tốc 2h</li>
              <li className="hover:text-leaf cursor-pointer transition-colors">Chính sách đổi trả trong vòng 24h</li>
              <li className="hover:text-leaf cursor-pointer transition-colors">Chính sách bảo mật thông tin khách hàng</li>
              <li className="hover:text-leaf cursor-pointer transition-colors">Quy định giải quyết khiếu nại & đối soát</li>
              <li className="hover:text-leaf cursor-pointer transition-colors">Hướng dẫn thanh toán quét mã SePay VietQR</li>
            </ul>
          </div>

          {/* Column 4: Certifications & Payment Partners */}
          <div className="space-y-4">
            <div>
              <h4 className="font-semibold text-ink mb-2 text-xs uppercase tracking-wider font-display">Phương thức thanh toán</h4>
              <div className="flex flex-wrap gap-2">
                <span className="px-2.5 py-1.5 rounded-lg border border-rule bg-white text-xs font-semibold text-ink flex items-center gap-1.5 shadow-2xs">
                  <QrCode className="w-3.5 h-3.5 text-leaf" /> VietQR Napas 24/7
                </span>
                <span className="px-2.5 py-1.5 rounded-lg border border-rule bg-white text-xs font-semibold text-ink flex items-center gap-1.5 shadow-2xs">
                  COD (Tiền mặt)
                </span>
                <span className="px-2.5 py-1.5 rounded-lg border border-rule bg-white text-xs font-semibold text-ink flex items-center gap-1.5 shadow-2xs">
                  MBBank
                </span>
              </div>
            </div>

            <div>
              <h4 className="font-semibold text-ink mb-2 text-xs uppercase tracking-wider font-display">Chứng nhận & An toàn</h4>
              <div className="flex flex-wrap gap-2">
                <div className="px-2.5 py-1 rounded-md bg-leaf/10 border border-leaf/20 text-[11px] font-semibold text-leaf flex items-center gap-1">
                  <CheckCircle2 className="w-3.5 h-3.5 stroke-[2]" /> VietGAP Certified
                </div>
                <div className="px-2.5 py-1 rounded-md bg-sprout/20 border border-sprout text-[11px] font-semibold text-leaf flex items-center gap-1">
                  <CheckCircle2 className="w-3.5 h-3.5 stroke-[2]" /> ISO 22000:2018
                </div>
                <div className="px-2.5 py-1 rounded-md bg-blue-50 border border-blue-200 text-[11px] font-semibold text-blue-700 flex items-center gap-1">
                  <ShieldCheck className="w-3.5 h-3.5 stroke-[2]" /> Đã Đăng Ký Bộ Công Thương
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Bottom Copyright Bar */}
        <div className="border-t border-rule bg-paper py-4 text-center text-xs text-ink-2">
          <div className="max-w-[1240px] mx-auto px-4 flex items-center justify-center">
            <p>© 2026 GroceryHub</p>
          </div>
        </div>
      </footer>

      {/* Sticky Mobile Bottom Navigation Bar (md:hidden, hidden on PDP where sticky buy bar takes over) */}
      {!currentPath.startsWith('/p/') && (
        <nav
          aria-label="Thanh điều hướng nhanh trên di động"
          className="md:hidden fixed bottom-0 left-0 right-0 z-40 bg-white/95 backdrop-blur-md border-t border-rule px-1.5 py-1 flex items-center justify-around shadow-lg pb-[max(0.5rem,env(safe-area-inset-bottom))]"
        >
          <button
            type="button"
            onClick={() => onNavigate?.('/')}
            className={cn(
              "flex flex-col items-center justify-center py-1 px-2 rounded-xl transition-colors cursor-pointer min-w-[56px]",
              currentPath === '/' ? "text-leaf font-bold" : "text-ink-2 hover:text-ink"
            )}
          >
            <Home className="w-5 h-5 stroke-[1.8]" />
            <span className="text-[10px] mt-0.5">Trang chủ</span>
          </button>

          <button
            type="button"
            onClick={() => setMobileMenuOpen(true)}
            className={cn(
              "flex flex-col items-center justify-center py-1 px-2 rounded-xl transition-colors cursor-pointer min-w-[56px]",
              mobileMenuOpen ? "text-leaf font-bold" : "text-ink-2 hover:text-ink"
            )}
          >
            <LayoutGrid className="w-5 h-5 stroke-[1.8]" />
            <span className="text-[10px] mt-0.5">Danh mục</span>
          </button>

          <button
            type="button"
            onClick={() => {
              if (currentPath !== '/c/tat-ca') {
                onNavigate?.('/c/tat-ca')
              }
            }}
            className={cn(
              "flex flex-col items-center justify-center py-1 px-2 rounded-xl transition-colors cursor-pointer min-w-[56px]",
              currentPath.startsWith('/c/') ? "text-leaf font-bold" : "text-ink-2 hover:text-ink"
            )}
          >
            <Search className="w-5 h-5 stroke-[1.8]" />
            <span className="text-[10px] mt-0.5">Tìm kiếm</span>
          </button>

          <button
            type="button"
            onClick={() => onNavigate?.('/cart')}
            className={cn(
              "relative flex flex-col items-center justify-center py-1 px-2 rounded-xl transition-colors cursor-pointer min-w-[56px]",
              currentPath === '/cart' ? "text-leaf font-bold" : "text-ink-2 hover:text-ink"
            )}
          >
            <div className="relative">
              <ShoppingBag className="w-5 h-5 stroke-[1.8]" />
              {itemCount > 0 && (
                <span className="absolute -top-1.5 -right-2.5 bg-tomato text-white text-[10px] font-bold rounded-full min-w-4 h-4 px-1 flex items-center justify-center tabular-nums shadow-xs">
                  {itemCount > 99 ? '99+' : itemCount}
                </span>
              )}
            </div>
            <span className="text-[10px] mt-0.5">Giỏ hàng</span>
          </button>

          <button
            type="button"
            onClick={() => onNavigate?.(isAuthenticated ? '/account' : '/login')}
            className={cn(
              "flex flex-col items-center justify-center py-1 px-2 rounded-xl transition-colors cursor-pointer min-w-[56px]",
              currentPath.startsWith('/account') || currentPath === '/login'
                ? "text-leaf font-bold"
                : "text-ink-2 hover:text-ink"
            )}
          >
            <User className="w-5 h-5 stroke-[1.8]" />
            <span className="text-[10px] mt-0.5">
              {isAuthenticated ? 'Tài khoản' : 'Đăng nhập'}
            </span>
          </button>
        </nav>
      )}

      {/* Floating Action Buttons */}
      <ZaloContactButton phone="0865087784" />
      <AIChatBot />
    </div>
  )
}
