import React, { useEffect, useState } from 'react'
import { useRouter } from '@/app/router'
import { api } from '@/shared/api/client'
import { Button } from '@/shared/ui/Button'
import { CheckCircle2, AlertCircle, Loader2 } from 'lucide-react'

export const VerifyEmailPage: React.FC = () => {
  const { searchParams, navigate } = useRouter()
  const userId = searchParams.get('userId')
  const token = searchParams.get('token')

  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading')
  const [errorMessage, setErrorMessage] = useState<string>('')

  useEffect(() => {
    if (!userId || !token) {
      setStatus('error')
      setErrorMessage('Đường dẫn xác thực không hợp lệ (thiếu mã người dùng hoặc token).')
      return
    }

    let isMounted = true

    const doVerify = async () => {
      try {
        const res = await (api as any).POST('/api/v1/auth/verify-email', {
          body: { userId, token },
        })

        if (!isMounted) return

        if (res && res.error) {
          setStatus('error')
          const detail = (res.error as any)?.detail || (res.error as any)?.title || 'Liên kết xác thực đã hết hạn hoặc không chính xác.'
          setErrorMessage(detail)
        } else {
          setStatus('success')
        }
      } catch (err) {
        if (!isMounted) return
        setStatus('error')
        setErrorMessage(err instanceof Error ? err.message : 'Đã có lỗi xảy ra khi xác thực email.')
      }
    }

    void doVerify()

    return () => {
      isMounted = false
    }
  }, [userId, token])

  return (
    <div className="max-w-md mx-auto py-16 px-4">
      <div className="bg-paper-2 p-8 rounded-3xl border border-rule text-center space-y-6 shadow-xs">
        {status === 'loading' && (
          <div className="space-y-4 py-8">
            <Loader2 className="w-12 h-12 text-leaf animate-spin mx-auto stroke-[1.5]" />
            <h1 className="font-display font-bold text-xl text-ink">Đang xác thực email...</h1>
            <p className="text-xs text-ink-2">Vui lòng chờ trong giây lát trong khi chúng tôi kích hoạt tài khoản của bạn.</p>
          </div>
        )}

        {status === 'success' && (
          <div className="space-y-4 py-4">
            <div className="w-16 h-16 rounded-full bg-emerald-50 border border-emerald-200 text-emerald-600 flex items-center justify-center mx-auto">
              <CheckCircle2 className="w-9 h-9 stroke-[1.5]" />
            </div>
            <h1 className="font-display font-bold text-2xl text-ink">Xác Thực Thành Công!</h1>
            <p className="text-xs text-ink-2 leading-relaxed">
              Tài khoản GroceryHub của bạn đã được kích hoạt thành công. Bạn đã có thể đăng nhập để đặt mua thực phẩm tươi ngon mỗi ngày!
            </p>
            <div className="pt-2">
              <Button
                variant="primary"
                className="w-full h-11 text-sm font-semibold rounded-xl bg-leaf hover:bg-leaf/90 text-white cursor-pointer"
                onClick={() => navigate('/login')}
              >
                Đăng Nhập Ngay
              </Button>
            </div>
          </div>
        )}

        {status === 'error' && (
          <div className="space-y-4 py-4">
            <div className="w-16 h-16 rounded-full bg-[#FDECE8] border border-[#FADCD5] text-tomato flex items-center justify-center mx-auto">
              <AlertCircle className="w-9 h-9 stroke-[1.5]" />
            </div>
            <h1 className="font-display font-bold text-2xl text-ink">Xác Thực Thất Bại</h1>
            <p className="text-xs text-tomato leading-relaxed bg-[#FDECE8]/50 p-3 rounded-xl border border-[#FADCD5]">
              {errorMessage}
            </p>
            <p className="text-xs text-ink-2">
              Liên kết có thể đã quá 24 giờ hoặc đã được sử dụng trước đó.
            </p>
            <div className="pt-2 flex gap-3">
              <Button
                variant="outline"
                className="flex-1 h-10 text-xs rounded-xl cursor-pointer"
                onClick={() => navigate('/')}
              >
                Về Trang Chủ
              </Button>
              <Button
                variant="primary"
                className="flex-1 h-10 text-xs rounded-xl bg-leaf hover:bg-leaf/90 text-white cursor-pointer"
                onClick={() => navigate('/login')}
              >
                Đăng Nhập
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
