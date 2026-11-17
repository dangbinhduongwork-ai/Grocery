import React, { useState } from 'react'
import { useAuth } from '@/shared/auth/AuthContext'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'
import { CaptchaWidget } from '@/shared/components/CaptchaWidget'
import { vi } from '@/shared/i18n/vi'
import { useRouter } from '@/app/router'
import { LogIn, Lock, Mail, AlertCircle, ArrowRight, CheckCircle2 } from 'lucide-react'

export const LoginPage: React.FC = () => {
  const { navigate, searchParams } = useRouter()
  const { login, sessionNotice, clearSessionNotice } = useAuth()

  const [email, setEmail] = useState(() => searchParams.get('email') || '')
  const [password, setPassword] = useState('')
  const [captchaInput, setCaptchaInput] = useState('')
  const [isCaptchaValid, setIsCaptchaValid] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const redirectUrl = searchParams.get('redirect') || '/'
  const isRegisteredSuccess = searchParams.get('registered') === '1'

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!email || !password) {
      setError('Vui lòng nhập đầy đủ email và mật khẩu')
      return
    }

    if (!isCaptchaValid) {
      setError('Vui lòng nhập đúng mã xác thực Captcha bảo mật bên dưới')
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      const result = await login(email.trim(), password)
      if (result.success) {
        navigate(redirectUrl)
      } else {
        setError(result.error || 'Email hoặc mật khẩu không chính xác')
      }
    } catch {
      setError('Đã có lỗi xảy ra khi kết nối máy chủ')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="max-w-md mx-auto py-8">
      <div className="bg-paper-2 p-8 rounded-3xl border border-rule space-y-6 shadow-xs">
        {/* Header */}
        <div className="text-center space-y-2">
          <div className="w-12 h-12 rounded-2xl bg-leaf/10 border border-leaf/20 text-leaf flex items-center justify-center mx-auto">
            <LogIn className="w-6 h-6 stroke-[1.5]" />
          </div>
          <h1 className="font-display font-bold text-2xl text-ink">
            {vi.auth.loginTitle}
          </h1>
          <p className="text-xs text-ink-2">
            Đăng nhập tài khoản GroceryHub để tiếp tục
          </p>
        </div>

        {isRegisteredSuccess && !error && (
          <div className="p-3.5 rounded-xl bg-emerald-50 border border-emerald-200 text-xs text-emerald-800 flex items-center gap-2">
            <CheckCircle2 className="w-4 h-4 shrink-0 text-emerald-600 stroke-[1.5]" />
            <span>Đăng ký tài khoản thành công! Vui lòng đăng nhập để tiếp tục.</span>
          </div>
        )}

        {sessionNotice && !error && (
          <div className="p-3.5 rounded-xl bg-amber-50 border border-amber-200 text-xs text-amber-800 flex items-center justify-between gap-2">
            <div className="flex items-center gap-2">
              <AlertCircle className="w-4 h-4 shrink-0 text-amber-600 stroke-[1.5]" />
              <span>{sessionNotice}</span>
            </div>
            <button
              type="button"
              onClick={clearSessionNotice}
              className="text-amber-500 hover:text-amber-700 text-xs font-bold p-1 cursor-pointer"
            >
              ✕
            </button>
          </div>
        )}

        {error && (
          <div className="p-3.5 rounded-xl bg-[#FDECE8] border border-[#FADCD5] text-xs text-tomato flex items-center gap-2">
            <AlertCircle className="w-4 h-4 shrink-0 stroke-[1.5]" />
            <span>{error}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4 notranslate" translate="no">
          <div className="space-y-1">
            <label className="text-xs font-semibold text-ink block notranslate" translate="no">
              {vi.auth.email}
            </label>
            <div className="relative">
              <Mail className="w-4 h-4 text-ink-2 absolute left-3 top-1/2 -translate-y-1/2 stroke-[1.5]" />
              <Input
                type="email"
                placeholder="tenban@email.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="pl-9 h-10 text-xs bg-paper"
                autoComplete="email"
                required
              />
            </div>
          </div>

          <div className="space-y-1">
            <div className="flex justify-between items-center">
              <label className="text-xs font-semibold text-ink block notranslate" translate="no">
                {vi.auth.password}
              </label>
              <button
                type="button"
                onClick={() => navigate('/forgot-password')}
                className="text-[11px] text-leaf hover:underline cursor-pointer notranslate"
                translate="no"
              >
                Quên mật khẩu?
              </button>
            </div>
            <div className="relative">
              <Lock className="w-4 h-4 text-ink-2 absolute left-3 top-1/2 -translate-y-1/2 stroke-[1.5]" />
              <Input
                type="password"
                placeholder="Nhập mật khẩu"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="pl-9 h-10 text-xs bg-paper font-mono"
                autoComplete="current-password"
                required
              />
            </div>
          </div>

          <CaptchaWidget
            value={captchaInput}
            onChange={setCaptchaInput}
            onValidate={setIsCaptchaValid}
          />

          <Button
            type="submit"
            variant="primary"
            size="lg"
            disabled={isLoading}
            className="w-full h-11 text-sm font-semibold shadow-xs flex items-center justify-center gap-2"
          >
            <span>{isLoading ? 'Đang xác thực...' : vi.auth.submitLogin}</span>
            <ArrowRight className="w-4 h-4 stroke-[1.5]" />
          </Button>
        </form>

        <div className="border-t border-rule pt-4 text-center text-xs text-ink-2">
          Chưa có tài khoản GroceryHub?{' '}
          <button
            type="button"
            onClick={() => navigate(`/register${redirectUrl !== '/' ? `?redirect=${encodeURIComponent(redirectUrl)}` : ''}`)}
            className="font-semibold text-leaf hover:underline cursor-pointer"
          >
            Đăng ký ngay
          </button>
        </div>
      </div>
    </div>
  )
}
