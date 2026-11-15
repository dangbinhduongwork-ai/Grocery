import React from 'react'
import { useAuth } from '@/shared/auth'
import { useRouter } from '@/app/router'
import { AdminForbiddenPage } from './AdminForbiddenPage'

export interface AdminRouteGuardProps {
  requiredPermission?: string
  children: React.ReactNode
}

export const AdminRouteGuard: React.FC<AdminRouteGuardProps> = ({
  requiredPermission,
  children,
}) => {
  const { isLoading, isAuthenticated, isStaff, hasPermission } = useAuth()
  const router = useRouter()

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[#F6F7F5]">
        <div className="w-8 h-8 rounded-full border-2 border-leaf border-t-transparent animate-spin" />
      </div>
    )
  }

  if (!isAuthenticated) {
    router.push('/login')
    return null
  }

  if (!isStaff) {
    return <AdminForbiddenPage missingPermission="Staff Role" />
  }

  if (requiredPermission && !hasPermission(requiredPermission)) {
    return <AdminForbiddenPage missingPermission={requiredPermission} />
  }

  return <>{children}</>
}
