import React, { useState } from 'react'
import { useAuth } from '@/shared/auth/AuthContext'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'
import { CaptchaWidget } from '@/shared/components/CaptchaWidget'
import { vi } from '@/shared/i18n/vi'
import { useRouter } from '@/app/router'
import { cn } from '@/shared/utils/cn'
import { UserPlus, Lock, Mail, Phone, User, AlertCircle, ArrowRight, Check, X } from 'lucide-react'

export const RegisterPage: React.FC = () => {
  const { navigate, searchParams } = useRouter()
  const { register, activateAndLogin } = useAuth()

  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [captchaInput, setCaptchaInput] = useState('')
  const [isCaptchaValid, setIsCaptchaValid] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const redirectUrl = searchParams.get('redirect') || '/'

  const passwordRules = [
    { id: 'len', label: 'Tối thiểu 8 ký tự', valid: password.length >= 8, required: true },
    { id: 'strength', label: 'Khuyên dùng kết hợp chữ và số để tăng bảo mật', valid: (/[a-zA-Z]/.test(password) && /[0-9]/.test(password)) || password.length >= 10, required: false },
  ]
  const isPasswordValid = password.length >= 8

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!fullName || !email || !phone || !password) {
      setError('Vui lòng điền đầy đủ các trường thông tin.')
      return
    }

    if (!isPasswordValid) {
      const missing = passwordRules.filter((r) => !r.valid).map((r) => r.label)
      setError(`Mật khẩu chưa đạt yêu cầu: ${missing.join(', ')}`)
      return
    }

    if (password !== confirmPassword) {
      setError('Mật khẩu xác nhận không khớp.')
      return
    }

    if (!isCaptchaValid) {
      setError('Vui lòng nhập đúng mã xác thực Captcha bảo mật.')
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      const result = await register(email.trim(), password, fullName.trim(), phone.trim())
      if (result.success) {
        // Tự động kích hoạt tài khoản ngay sau khi đăng ký
        try {
          await activateAndLogin(email.trim(), password)
        } catch {
          // ignore, user can log in or activate on login page
        }

        // Tự động chuyển hướng sang giao diện đăng nhập với thông tin email đã điền sẵn
        const loginUrl = `/login?registered=1&email=${encodeURIComponent(email.trim())}${
          redirectUrl !== '/' ? `&redirect=${encodeURIComponent(redirectUrl)}` : ''
        }`
        navigate(loginUrl)
      } else {
        setError(result.error || 'Đăng ký không thành công. Email hoặc số điện thoại có thể đã tồn tại.')
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
            <UserPlus className="w-6 h-6 stroke-[1.5]" />
          </div>
          <h1 className="font-display font-bold text-2xl text-ink">
            {vi.auth.registerTitle}
          </h1>
          <p className="text-xs text-ink-2">
            Đăng ký để nhận ngay ưu đãi thành viên và miễn phí vận chuyển
          </p>
        </div>

        {error && (
          <div className="p-3.5 rounded-xl bg-[#FDECE8] border border-[#FADCD5] text-xs text-tomato flex items-center gap-2">
            <AlertCircle className="w-4 h-4 shrink-0 stroke-[1.5]" />
            <span>{error}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1">
            <label className="text-xs font-semibold text-ink block">
              {vi.auth.fullName} <span className="text-tomato">*</span>
            </label>
            <div className="relative">
              <User className="w-4 h-4 text-ink-2 absolute left-3 top-1/2 -translate-y-1/2 stroke-[1.5]" />
              <Input
                placeholder="Nguyễn Văn A"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                className="pl-9 h-10 text-xs bg-paper"
                required
              />
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-ink block">
              {vi.auth.email} <span className="text-tomato">*</span>
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
            <label className="text-xs font-semibold text-ink block">
              {vi.auth.phone} <span className="text-tomato">*</span>
            </label>
            <div className="relative">
              <Phone className="w-4 h-4 text-ink-2 absolute left-3 top-1/2 -translate-y-1/2 stroke-[1.5]" />
              <Input
                type="tel"
                placeholder="0912345678"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                className="pl-9 h-10 text-xs bg-paper font-mono"
                required
              />
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-ink block">
              {vi.auth.password} <span className="text-tomato">*</span>
            </label>
            <div className="relative">
              <Lock className="w-4 h-4 text-ink-2 absolute left-3 top-1/2 -translate-y-1/2 stroke-[1.5]" />
              <Input
                type="password"
                placeholder="Tối thiểu 8 ký tự trở lên"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="pl-9 h-10 text-xs bg-paper font-mono"
                autoComplete="new-password"
                required
              />
            </div>

            {/* Live Password Checklist & Feedback */}
            {password.length > 0 && (
              <div className="mt-2 p-2.5 rounded-xl bg-paper border border-rule space-y-1 animate-fade-in">
                {passwordRules.map((rule) => (
                  <div
                    key={rule.id}
                    className={cn(
                      "flex items-center gap-1.5 text-[11px] transition-colors",
                      rule.valid ? "text-leaf font-medium" : (rule.required ? "text-tomato font-medium" : "text-ink-2")
                    )}
                  >
                    {rule.valid ? (
                      <Check className="w-3.5 h-3.5 text-leaf shrink-0 stroke-[2.5]" />
                    ) : (
                      <X className="w-3.5 h-3.5 text-tomato shrink-0 stroke-[2.5]" />
                    )}
                    <span>{rule.label}</span>
                  </div>
                ))}
              </div>
            )}
            {password.length === 0 && (
              <p className="text-[11px] text-ink-2 mt-1">
                Yêu cầu mật khẩu: tối thiểu từ 8 ký tự trở lên.
              </p>
            )}
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-ink block">
              Xác nhận mật khẩu <span className="text-tomato">*</span>
            </label>
            <div className="relative">
              <Lock className="w-4 h-4 text-ink-2 absolute left-3 top-1/2 -translate-y-1/2 stroke-[1.5]" />
              <Input
                type="password"
                placeholder="Nhập lại mật khẩu"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                className="pl-9 h-10 text-xs bg-paper font-mono"
                autoComplete="new-password"
                required
              />
            </div>
            {confirmPassword.length > 0 && (
              <div className="flex items-center gap-1.5 text-[11px] mt-1.5">
                {password === confirmPassword ? (
                  <>
                    <Check className="w-3.5 h-3.5 text-leaf stroke-[2.5]" />
                    <span className="text-leaf font-medium">Mật khẩu xác nhận đã khớp</span>
                  </>
                ) : (
                  <>
                    <X className="w-3.5 h-3.5 text-tomato stroke-[2.5]" />
                    <span className="text-tomato font-medium">Mật khẩu xác nhận chưa khớp</span>
                  </>
                )}
              </div>
            )}
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
            <span>{isLoading ? 'Đang đăng ký...' : vi.auth.submitRegister}</span>
            <ArrowRight className="w-4 h-4 stroke-[1.5]" />
          </Button>
        </form>

        <div className="border-t border-rule pt-4 text-center text-xs text-ink-2">
          Đã có tài khoản?{' '}
          <button
            type="button"
            onClick={() => navigate(`/login${redirectUrl !== '/' ? `?redirect=${encodeURIComponent(redirectUrl)}` : ''}`)}
            className="font-semibold text-leaf hover:underline cursor-pointer"
          >
            Đăng nhập ngay
          </button>
        </div>
      </div>
    </div>
  )
}
