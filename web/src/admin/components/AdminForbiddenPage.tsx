import React from 'react'
import { ShieldAlert, ArrowLeft } from 'lucide-react'
import { useRouter } from '@/app/router'

export const AdminForbiddenPage: React.FC<{ missingPermission?: string }> = ({ missingPermission }) => {
  const router = useRouter()

  return (
    <div className="min-h-[70vh] flex items-center justify-center p-6">
      <div className="max-w-md w-full bg-white rounded-2xl border border-slate-200 p-8 text-center shadow-xs">
        <div className="w-16 h-16 rounded-2xl bg-rose-50 text-rose-600 flex items-center justify-center mx-auto mb-4 border border-rose-100">
          <ShieldAlert className="w-8 h-8" strokeWidth={1.5} />
        </div>
        <h1 className="text-xl font-bold text-slate-900 mb-2 font-display">403 — Không có quyền truy cập</h1>
        <p className="text-sm text-slate-500 mb-6">
          Tài khoản của bạn không được phân quyền để truy cập tính năng này.
          {missingPermission && (
            <span className="block mt-1 text-xs text-slate-400 font-mono">
              (Yêu cầu quyền: {missingPermission})
            </span>
          )}
        </p>
        <div className="flex flex-col sm:flex-row gap-3 justify-center">
          <button
            type="button"
            onClick={() => router.push('/admin')}
            className="inline-flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl bg-leaf text-white font-medium text-sm hover:bg-leaf-2 transition-colors shadow-xs"
          >
            <ArrowLeft className="w-4 h-4" strokeWidth={1.5} />
            Về Dashboard
          </button>
          <button
            type="button"
            onClick={() => router.push('/')}
            className="inline-flex items-center justify-center px-4 py-2.5 rounded-xl border border-slate-200 text-slate-700 font-medium text-sm hover:bg-slate-50 transition-colors"
          >
            Về Storefront
          </button>
        </div>
      </div>
    </div>
  )
}
