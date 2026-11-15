import React, { Suspense, lazy } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AuthProvider } from '@/shared/auth/AuthContext'
import { CartProvider } from '@/shared/cart/CartContext'
import { RouterProvider, useRouter, matchPath } from '@/app/router'
import { StorefrontLayout } from '@/storefront/layout/StorefrontLayout'
import { Skeleton } from '@/shared/ui/Skeleton'
import { Button } from '@/shared/ui/Button'
import { AlertCircle } from 'lucide-react'

// Code-split route pages
const HomePage = lazy(() => import('@/storefront/pages/HomePage').then((m) => ({ default: m.HomePage })))
const CategoryPage = lazy(() => import('@/storefront/pages/CategoryPage').then((m) => ({ default: m.CategoryPage })))
const SearchPage = lazy(() => import('@/storefront/pages/SearchPage').then((m) => ({ default: m.SearchPage })))
const ProductDetailPage = lazy(() => import('@/storefront/pages/ProductDetailPage').then((m) => ({ default: m.ProductDetailPage })))
const CartPage = lazy(() => import('@/storefront/pages/CartPage').then((m) => ({ default: m.CartPage })))
const CheckoutPage = lazy(() => import('@/storefront/pages/CheckoutPage').then((m) => ({ default: m.CheckoutPage })))
const OrderConfirmationPage = lazy(() => import('@/storefront/pages/OrderConfirmationPage').then((m) => ({ default: m.OrderConfirmationPage })))
const OrdersPage = lazy(() => import('@/storefront/pages/OrdersPage').then((m) => ({ default: m.OrdersPage })))
const OrderDetailPage = lazy(() => import('@/storefront/pages/OrderDetailPage').then((m) => ({ default: m.OrderDetailPage })))
const AccountPage = lazy(() => import('@/storefront/pages/AccountPage').then((m) => ({ default: m.AccountPage })))
const LoginPage = lazy(() => import('@/storefront/pages/LoginPage').then((m) => ({ default: m.LoginPage })))
const RegisterPage = lazy(() => import('@/storefront/pages/RegisterPage').then((m) => ({ default: m.RegisterPage })))
const DesignRoute = lazy(() => import('@/app/routes/DesignRoute').then((m) => ({ default: m.DesignRoute })))

const RouteLoading: React.FC = () => (
  <div className="py-12 space-y-6 max-w-4xl mx-auto animate-pulse">
    <Skeleton className="h-8 w-48" />
    <Skeleton className="h-64 w-full rounded-2xl" />
  </div>
)

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 2, // 2 minutes
      retry: 1,
    },
  },
})

const AppContent: React.FC = () => {
  const { pathname, navigate } = useRouter()

  const renderCurrentRoute = () => {
    if (pathname === '/') {
      return <HomePage />
    }

    if (pathname === '/__design') {
      return <DesignRoute />
    }

    if (pathname === '/search') {
      return <SearchPage />
    }

    if (pathname === '/cart') {
      return <CartPage />
    }

    if (pathname === '/checkout') {
      return <CheckoutPage />
    }

    if (pathname === '/orders') {
      return <OrdersPage />
    }

    if (pathname === '/account') {
      return <AccountPage />
    }

    if (pathname === '/login') {
      return <LoginPage />
    }

    if (pathname === '/register') {
      return <RegisterPage />
    }

    // Match /c/:slug
    const catMatch = matchPath('/c/:slug', pathname)
    if (catMatch.matched && catMatch.params.slug) {
      return <CategoryPage categorySlug={catMatch.params.slug} />
    }

    // Match /p/:slug
    const prodMatch = matchPath('/p/:slug', pathname)
    if (prodMatch.matched && prodMatch.params.slug) {
      return <ProductDetailPage productSlug={prodMatch.params.slug} />
    }

    // Match /orders/:id/confirmed
    const confirmMatch = matchPath('/orders/:id/confirmed', pathname)
    if (confirmMatch.matched && confirmMatch.params.id) {
      return <OrderConfirmationPage orderId={confirmMatch.params.id} />
    }

    // Match /orders/:id
    const orderDetailMatch = matchPath('/orders/:id', pathname)
    if (orderDetailMatch.matched && orderDetailMatch.params.id) {
      return <OrderDetailPage orderId={orderDetailMatch.params.id} />
    }

    // 404 Fallback
    return (
      <div className="text-center py-20 bg-paper-2 rounded-2xl border border-rule max-w-lg mx-auto px-4 space-y-4">
        <AlertCircle className="w-12 h-12 text-tomato mx-auto stroke-[1.5]" />
        <h2 className="font-display font-semibold text-xl text-ink">
          404 - Không tìm thấy trang
        </h2>
        <p className="text-xs text-ink-2">
          Đường dẫn bạn yêu cầu không tồn tại hoặc đã được thay đổi.
        </p>
        <Button variant="primary" onClick={() => navigate('/')}>
          Về trang chủ
        </Button>
      </div>
    )
  }

  return (
    <StorefrontLayout currentPath={pathname} onNavigate={(to) => navigate(to)}>
      <Suspense fallback={<RouteLoading />}>
        {renderCurrentRoute()}
      </Suspense>
    </StorefrontLayout>
  )
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <CartProvider>
          <RouterProvider>
            <AppContent />
          </RouterProvider>
        </CartProvider>
      </AuthProvider>
    </QueryClientProvider>
  )
}
