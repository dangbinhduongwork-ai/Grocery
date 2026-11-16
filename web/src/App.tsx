import React, { Suspense, lazy } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AuthProvider } from '@/shared/auth/AuthContext'
import { CartProvider } from '@/shared/cart/CartContext'
import { RouterProvider, useRouter, matchPath } from '@/app/router'
import { StorefrontLayout } from '@/storefront/layout/StorefrontLayout'
import { Skeleton } from '@/shared/ui/Skeleton'
import { Button } from '@/shared/ui/Button'
import { ErrorBoundary } from '@/shared/ui/ErrorBoundary'
import { AlertCircle } from 'lucide-react'

// Code-split Storefront route pages
const HomePage = lazy(() => import('@/storefront/pages/HomePage').then((m) => ({ default: m.HomePage })))
const CategoryPage = lazy(() => import('@/storefront/pages/CategoryPage').then((m) => ({ default: m.CategoryPage })))
const SearchPage = lazy(() => import('@/storefront/pages/SearchPage').then((m) => ({ default: m.SearchPage })))
const ProductDetailPage = lazy(() => import('@/storefront/pages/ProductDetailPage').then((m) => ({ default: m.ProductDetailPage })))
const CartPage = lazy(() => import('@/storefront/pages/CartPage').then((m) => ({ default: m.CartPage })))
const CheckoutPage = lazy(() => import('@/storefront/pages/CheckoutPage').then((m) => ({ default: m.CheckoutPage })))
const OrderConfirmationPage = lazy(() => import('@/storefront/pages/OrderConfirmationPage').then((m) => ({ default: m.OrderConfirmationPage })))
const PaymentPage = lazy(() => import('@/storefront/pages/PaymentPage').then((m) => ({ default: m.PaymentPage })))
const OrdersPage = lazy(() => import('@/storefront/pages/OrdersPage').then((m) => ({ default: m.OrdersPage })))
const OrderDetailPage = lazy(() => import('@/storefront/pages/OrderDetailPage').then((m) => ({ default: m.OrderDetailPage })))
const AccountPage = lazy(() => import('@/storefront/pages/AccountPage').then((m) => ({ default: m.AccountPage })))
const LoginPage = lazy(() => import('@/storefront/pages/LoginPage').then((m) => ({ default: m.LoginPage })))
const RegisterPage = lazy(() => import('@/storefront/pages/RegisterPage').then((m) => ({ default: m.RegisterPage })))
const DesignRoute = lazy(() => import('@/app/routes/DesignRoute').then((m) => ({ default: m.DesignRoute })))

// Code-split Admin Back-Office route pages
const AdminLayout = lazy(() => import('@/admin/layout/AdminLayout').then((m) => ({ default: m.AdminLayout })))
const AdminRouteGuard = lazy(() => import('@/admin/components/AdminRouteGuard').then((m) => ({ default: m.AdminRouteGuard })))
const AdminDashboardPage = lazy(() => import('@/admin/pages/AdminDashboardPage').then((m) => ({ default: m.AdminDashboardPage })))
const AdminOrdersPage = lazy(() => import('@/admin/pages/AdminOrdersPage').then((m) => ({ default: m.AdminOrdersPage })))
const AdminOrderDetailPage = lazy(() => import('@/admin/pages/AdminOrderDetailPage').then((m) => ({ default: m.AdminOrderDetailPage })))
const AdminPickingModePage = lazy(() => import('@/admin/pages/AdminPickingModePage').then((m) => ({ default: m.AdminPickingModePage })))
const AdminPaymentsPage = lazy(() => import('@/admin/pages/AdminPaymentsPage').then((m) => ({ default: m.AdminPaymentsPage })))
const AdminRefundsPage = lazy(() => import('@/admin/pages/AdminRefundsPage').then((m) => ({ default: m.AdminRefundsPage })))
const AdminProductsPage = lazy(() => import('@/admin/pages/AdminProductsPage').then((m) => ({ default: m.AdminProductsPage })))
const AdminProductFormPage = lazy(() => import('@/admin/pages/AdminProductFormPage').then((m) => ({ default: m.AdminProductFormPage })))
const AdminCategoriesPage = lazy(() => import('@/admin/pages/AdminCategoriesPage').then((m) => ({ default: m.AdminCategoriesPage })))
const AdminInventoryPage = lazy(() => import('@/admin/pages/AdminInventoryPage').then((m) => ({ default: m.AdminInventoryPage })))
const AdminSuppliersPage = lazy(() => import('@/admin/pages/AdminSuppliersPage').then((m) => ({ default: m.AdminSuppliersPage })))
const AdminPurchasingPage = lazy(() => import('@/admin/pages/AdminPurchasingPage').then((m) => ({ default: m.AdminPurchasingPage })))
const AdminCustomersPage = lazy(() => import('@/admin/pages/AdminCustomersPage').then((m) => ({ default: m.AdminCustomersPage })))
const AdminStaffPage = lazy(() => import('@/admin/pages/AdminStaffPage').then((m) => ({ default: m.AdminStaffPage })))
const AdminRolesPage = lazy(() => import('@/admin/pages/AdminRolesPage').then((m) => ({ default: m.AdminRolesPage })))
const AdminCouponsPage = lazy(() => import('@/admin/pages/AdminCouponsPage').then((m) => ({ default: m.AdminCouponsPage })))
const AdminReviewsPage = lazy(() => import('@/admin/pages/AdminReviewsPage').then((m) => ({ default: m.AdminReviewsPage })))
const AdminReportsPage = lazy(() => import('@/admin/pages/AdminReportsPage').then((m) => ({ default: m.AdminReportsPage })))
const AdminAuditLogsPage = lazy(() => import('@/admin/pages/AdminAuditLogsPage').then((m) => ({ default: m.AdminAuditLogsPage })))
const AdminSettingsPage = lazy(() => import('@/admin/pages/AdminSettingsPage').then((m) => ({ default: m.AdminSettingsPage })))
const AdminUiDesignPage = lazy(() => import('@/admin/pages/AdminUiDesignPage').then((m) => ({ default: m.AdminUiDesignPage })))

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

  // Admin routes matching
  if (pathname.startsWith('/admin')) {
    const renderAdminRoute = () => {
      if (pathname === '/admin' || pathname === '/admin/') {
        return (
          <AdminRouteGuard>
            <AdminDashboardPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/orders') {
        return (
          <AdminRouteGuard requiredPermission="orders.read">
            <AdminOrdersPage />
          </AdminRouteGuard>
        )
      }

      const adminOrderMatch = matchPath('/admin/orders/:id', pathname)
      if (adminOrderMatch.matched && adminOrderMatch.params.id) {
        return (
          <AdminRouteGuard requiredPermission="orders.read">
            <AdminOrderDetailPage id={adminOrderMatch.params.id} />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/picking') {
        return (
          <AdminRouteGuard requiredPermission="orders.read">
            <AdminPickingModePage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/products') {
        return (
          <AdminRouteGuard requiredPermission="catalog.read">
            <AdminProductsPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/products/new') {
        return (
          <AdminRouteGuard requiredPermission="catalog.write">
            <AdminProductFormPage />
          </AdminRouteGuard>
        )
      }

      const adminProductEditMatch = matchPath('/admin/products/:id/edit', pathname)
      if (adminProductEditMatch.matched && adminProductEditMatch.params.id) {
        return (
          <AdminRouteGuard requiredPermission="catalog.write">
            <AdminProductFormPage id={adminProductEditMatch.params.id} />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/categories') {
        return (
          <AdminRouteGuard requiredPermission="catalog.read">
            <AdminCategoriesPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/inventory') {
        return (
          <AdminRouteGuard requiredPermission="inventory.read">
            <AdminInventoryPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/suppliers') {
        return (
          <AdminRouteGuard requiredPermission="purchasing.read">
            <AdminSuppliersPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/purchasing') {
        return (
          <AdminRouteGuard requiredPermission="purchasing.read">
            <AdminPurchasingPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/customers') {
        return (
          <AdminRouteGuard requiredPermission="customers.read">
            <AdminCustomersPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/staff') {
        return (
          <AdminRouteGuard requiredPermission="staff.read">
            <AdminStaffPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/roles') {
        return (
          <AdminRouteGuard requiredPermission="roles.manage">
            <AdminRolesPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/coupons') {
        return (
          <AdminRouteGuard requiredPermission="promotions.manage">
            <AdminCouponsPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/reviews') {
        return (
          <AdminRouteGuard requiredPermission="reviews.moderate">
            <AdminReviewsPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/reports/revenue') {
        return (
          <AdminRouteGuard requiredPermission="reports.revenue">
            <AdminReportsPage defaultTab="revenue" />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/reports/profit') {
        return (
          <AdminRouteGuard requiredPermission="reports.profit">
            <AdminReportsPage defaultTab="profit" />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/audit-logs') {
        return (
          <AdminRouteGuard requiredPermission="audit.read">
            <AdminAuditLogsPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/payments') {
        return (
          <AdminRouteGuard requiredPermission="payments.review">
            <AdminPaymentsPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/refunds') {
        return (
          <AdminRouteGuard requiredPermission="refunds.request">
            <AdminRefundsPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/settings') {
        return (
          <AdminRouteGuard requiredPermission="settings.manage">
            <AdminSettingsPage />
          </AdminRouteGuard>
        )
      }

      if (pathname === '/admin/ui-design' || pathname === '/admin/banners') {
        return (
          <AdminRouteGuard requiredPermission="catalog.write">
            <AdminUiDesignPage />
          </AdminRouteGuard>
        )
      }

      return (
        <div className="p-12 text-center text-slate-500">
          <h2 className="text-lg font-bold text-slate-800">404 - Không tìm thấy trang quản trị</h2>
          <button
            type="button"
            onClick={() => navigate('/admin')}
            className="mt-3 text-xs text-leaf font-semibold hover:underline"
          >
            Về Dashboard quản trị
          </button>
        </div>
      )
    }

    return (
      <Suspense fallback={<RouteLoading />}>
        <AdminLayout>
          <ErrorBoundary>
            {renderAdminRoute()}
          </ErrorBoundary>
        </AdminLayout>
      </Suspense>
    )
  }

  // Storefront routes rendering
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

    // Match /payment/:paymentCode
    const paymentMatch = matchPath('/payment/:paymentCode', pathname)
    if (paymentMatch.matched && paymentMatch.params.paymentCode) {
      return <PaymentPage paymentCode={paymentMatch.params.paymentCode} />
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
        <ErrorBoundary>
          {renderCurrentRoute()}
        </ErrorBoundary>
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
