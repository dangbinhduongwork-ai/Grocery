import React, { useEffect, useState } from 'react'
import {
  Save,
  CheckCircle2,
  AlertCircle,
  Building,
  CreditCard,
  Truck,
  Boxes,
  Bot,
} from 'lucide-react'
import { api } from '@/shared/api/client'

interface StoreSettings {
  storeName: string
  supportPhone: string
  supportEmail: string
  sePayEnabled: boolean
  bankShortName: string
  bankAccountNumber: string
  bankAccountName: string
  transferContentPrefix: string | null
  qrExpiryMinutes: number
  freeShippingThreshold: number
  codMaxAmount: number
  maxQuantityPerLine: number
  defaultLowStockThreshold: number
}

export const AdminSettingsPage: React.FC = () => {
  const [settings, setSettings] = useState<StoreSettings>({
    storeName: 'GroceryHub Vietnam',
    supportPhone: '0865 087 784',
    supportEmail: 'dangbinhduong.work@gmail.com',
    sePayEnabled: false,
    bankShortName: 'MBBANK',
    bankAccountNumber: '0987654321',
    bankAccountName: 'CONG TY GROCERYHUB VIETNAM',
    transferContentPrefix: 'GH',
    qrExpiryMinutes: 15,
    freeShippingThreshold: 300000,
    codMaxAmount: 5000000,
    maxQuantityPerLine: 20,
    defaultLowStockThreshold: 10,
  })

  // AI Chatbot Settings (persisted in localStorage)
  const envKey = (import.meta.env.VITE_AI_API_KEY as string) || (import.meta.env.VITE_GEMINI_API_KEY as string) || ''
  const isSkKey = envKey.startsWith('sk-')

  const [aiProvider, setAiProvider] = useState<'gemini' | 'openai' | 'custom'>(() => {
    try {
      const cfg = localStorage.getItem('groceryhub_ai_config')
      if (cfg) {
        const parsed = JSON.parse(cfg)
        const key = parsed.apiKey || envKey
        if (key.startsWith('sk-') && parsed.provider === 'gemini') return 'openai'
        return parsed.provider || (isSkKey ? 'openai' : 'openai')
      }
    } catch {}
    return 'openai'
  })
  const [aiEndpoint, setAiEndpoint] = useState(() => {
    try {
      const cfg = localStorage.getItem('groceryhub_ai_config')
      if (cfg) return JSON.parse(cfg).endpointUrl || 'https://api.xingchiapi.com'
    } catch {}
    return (import.meta.env.VITE_AI_ENDPOINT as string) || 'https://api.xingchiapi.com'
  })
  const [aiModel, setAiModel] = useState(() => {
    try {
      const cfg = localStorage.getItem('groceryhub_ai_config')
      if (cfg) {
        const parsed = JSON.parse(cfg)
        if (parsed.model && parsed.model !== 'gemini-2.5-flash' && parsed.model !== 'gpt-4o-mini') {
          return parsed.model
        }
      }
    } catch {}
    return (import.meta.env.VITE_AI_MODEL as string) || 'gpt-6.1-sol'
  })
  const [aiApiKey, setAiApiKey] = useState(() => {
    try {
      const cfg = localStorage.getItem('groceryhub_ai_config')
      if (cfg) return JSON.parse(cfg).apiKey || envKey
    } catch {}
    return envKey
  })

  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  useEffect(() => {
    async function loadSettings() {
      try {
        const res = await api.GET('/api/v1/admin/settings')
        if (res.data) {
          setSettings(res.data as unknown as StoreSettings)
        }
      } catch {
        // Silently
      } finally {
        setIsLoading(false)
      }
    }
    void loadSettings()
  }, [])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsSaving(true)
    setErrorMsg(null)
    setSuccessMsg(null)

    try {
      // Save AI Chatbot configuration
      const cleanAiConfig = {
        provider: aiProvider,
        endpointUrl:
          aiEndpoint.trim() ||
          (aiProvider === 'openai'
            ? 'https://api.openai.com/v1/chat/completions'
            : 'https://generativelanguage.googleapis.com/v1beta/models'),
        model:
          aiModel.trim() ||
          (aiProvider === 'openai' ? 'gpt-6.1-sol' : 'gemini-2.5-flash'),
        apiKey: aiApiKey.trim(),
      }
      localStorage.setItem('groceryhub_ai_config', JSON.stringify(cleanAiConfig))

      const res = await api.PUT('/api/v1/admin/settings', {
        body: {
          ...settings,
          transferContentPrefix: settings.transferContentPrefix ?? null,
        },
      })

      if (res.error) {
        setErrorMsg((res.error as { detail?: string })?.detail ?? 'Cập nhật cài đặt thất bại.')
      } else {
        setSuccessMsg('Đã lưu cấu hình hệ thống thành công!')
      }
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : 'Lỗi hệ thống.')
    } finally {
      setIsSaving(false)
    }
  }

  if (isLoading) {
    return <div className="h-64 bg-white rounded-2xl animate-pulse" />
  }

  return (
    <div className="space-y-6 max-w-4xl">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900 font-display">Cài đặt hệ thống</h1>
        <p className="text-xs text-slate-500 mt-0.5">
          Cấu hình hạn mức thanh toán, thông tin cửa hàng, ngưỡng miễn phí vận chuyển và tài khoản ngân hàng.
        </p>
      </div>

      {errorMsg && (
        <div className="p-3 bg-rose-50 border border-rose-200 text-rose-700 text-xs rounded-xl flex items-center gap-2">
          <AlertCircle className="w-4 h-4 shrink-0" strokeWidth={1.5} />
          <span>{errorMsg}</span>
        </div>
      )}

      {successMsg && (
        <div className="p-3 bg-emerald-50 border border-emerald-200 text-emerald-700 text-xs rounded-xl flex items-center gap-2">
          <CheckCircle2 className="w-4 h-4 shrink-0" strokeWidth={1.5} />
          <span>{successMsg}</span>
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Store Info */}
        <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-2xs space-y-4">
          <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700 border-b border-slate-100 pb-2 flex items-center gap-2">
            <Building className="w-4 h-4 text-leaf" strokeWidth={1.5} />
            Thông tin cửa hàng
          </h2>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-xs">
            <div>
              <label className="block font-semibold text-slate-700 mb-1">Tên cửa hàng *</label>
              <input
                type="text"
                required
                value={settings.storeName}
                onChange={e => setSettings({ ...settings, storeName: e.target.value })}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>
            <div>
              <label className="block font-semibold text-slate-700 mb-1">Hotline hỗ trợ *</label>
              <input
                type="text"
                required
                value={settings.supportPhone}
                onChange={e => setSettings({ ...settings, supportPhone: e.target.value })}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>
            <div>
              <label className="block font-semibold text-slate-700 mb-1">Email hỗ trợ *</label>
              <input
                type="email"
                required
                value={settings.supportEmail}
                onChange={e => setSettings({ ...settings, supportEmail: e.target.value })}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>
          </div>
        </div>

        {/* Shipping & Limits */}
        <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-2xs space-y-4">
          <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700 border-b border-slate-100 pb-2 flex items-center gap-2">
            <Truck className="w-4 h-4 text-sky-600" strokeWidth={1.5} />
            Chính sách giao hàng & Giới hạn giỏ hàng
          </h2>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-xs">
            <div>
              <label className="block font-semibold text-slate-700 mb-1">
                Ngưỡng miễn phí giao hàng (VND) *
              </label>
              <input
                type="number"
                required
                min={0}
                value={settings.freeShippingThreshold}
                onChange={e =>
                  setSettings({ ...settings, freeShippingThreshold: Number(e.target.value) })
                }
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono font-semibold text-leaf focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
              <span className="text-[11px] text-slate-400 mt-1 block">Mặc định: 300,000 đ</span>
            </div>

            <div>
              <label className="block font-semibold text-slate-700 mb-1">Hạn mức COD tối đa (VND) *</label>
              <input
                type="number"
                required
                min={0}
                value={settings.codMaxAmount}
                onChange={e => setSettings({ ...settings, codMaxAmount: Number(e.target.value) })}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono font-semibold focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
              <span className="text-[11px] text-slate-400 mt-1 block">Mặc định: 5,000,000 đ</span>
            </div>

            <div>
              <label className="block font-semibold text-slate-700 mb-1">
                Số lượng tối đa / dòng sản phẩm *
              </label>
              <input
                type="number"
                required
                min={1}
                value={settings.maxQuantityPerLine}
                onChange={e =>
                  setSettings({ ...settings, maxQuantityPerLine: Number(e.target.value) })
                }
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
              <span className="text-[11px] text-slate-400 mt-1 block">Mặc định: 20</span>
            </div>
          </div>
        </div>

        {/* Banking & VietQR */}
        <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-2xs space-y-4">
          <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700 border-b border-slate-100 pb-2 flex items-center gap-2">
            <CreditCard className="w-4 h-4 text-indigo-600" strokeWidth={1.5} />
            Tài khoản nhận tiền VietQR (SePay)
          </h2>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-xs">
            <div>
              <label className="block font-semibold text-slate-700 mb-1">Ngân hàng (Mã viết tắt)</label>
              <input
                type="text"
                required
                value={settings.bankShortName}
                onChange={e => setSettings({ ...settings, bankShortName: e.target.value.toUpperCase() })}
                placeholder="MBBANK, VCB, TCB..."
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>
            <div>
              <label className="block font-semibold text-slate-700 mb-1">Số tài khoản</label>
              <input
                type="text"
                required
                value={settings.bankAccountNumber}
                onChange={e => setSettings({ ...settings, bankAccountNumber: e.target.value })}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>
            <div>
              <label className="block font-semibold text-slate-700 mb-1">Tên chủ tài khoản</label>
              <input
                type="text"
                required
                value={settings.bankAccountName}
                onChange={e => setSettings({ ...settings, bankAccountName: e.target.value.toUpperCase() })}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono uppercase focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>
          </div>
        </div>

        {/* AI Chatbot Configuration (Universal for Any AI Provider) */}
        <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-2xs space-y-4">
          <div className="flex items-center justify-between border-b border-slate-100 pb-2">
            <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700 flex items-center gap-2">
              <Bot className="w-4 h-4 text-leaf" strokeWidth={1.5} />
              Cấu hình Trợ lý ảo AI Chatbot (Hỗ trợ mọi nhà cung cấp AI)
            </h2>
            <span className="text-[11px] px-2.5 py-0.5 rounded-full bg-leaf/10 text-leaf font-semibold">
              Admin Only
            </span>
          </div>

          <p className="text-xs text-slate-500 leading-relaxed">
            Hệ thống hỗ trợ linh hoạt <b>mọi nhà cung cấp AI</b> (Google Gemini, OpenAI ChatGPT, DeepSeek, Groq, OpenRouter hoặc Custom Proxy). Chỉ Quản trị viên mới có thể xem và cấu hình API này.
          </p>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
            <div>
              <label className="block font-semibold text-slate-700 mb-1">Nhà cung cấp AI (Provider)</label>
              <select
                value={aiProvider}
                onChange={(e) => {
                  const p = e.target.value as 'gemini' | 'openai' | 'custom'
                  setAiProvider(p)
                  if (p === 'openai') {
                    setAiEndpoint('https://api.xingchiapi.com')
                    setAiModel('gpt-6.1-sol')
                  } else if (p === 'gemini') {
                    setAiEndpoint('https://generativelanguage.googleapis.com/v1beta/models')
                    setAiModel('gemini-2.5-flash')
                  }
                }}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-medium focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              >
                <option value="openai">OpenAI / Proxy tương thích (Mặc định: gpt-6.1-sol)</option>
                <option value="gemini">Google Gemini (Model: gemini-2.5-flash / gemini-2.0-flash)</option>
                <option value="custom">Endpoint Tùy Chỉnh (Custom Proxy / Ollama / Local AI)</option>
              </select>
            </div>

            <div>
              <label className="block font-semibold text-slate-700 mb-1">Tên mô hình AI (Model Name)</label>
              <input
                type="text"
                value={aiModel}
                onChange={(e) => setAiModel(e.target.value)}
                placeholder={aiProvider === 'gemini' ? 'gemini-2.5-flash' : 'gpt-6.1-sol'}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
            </div>

            {aiProvider !== 'gemini' && (
              <div className="sm:col-span-2">
                <label className="block font-semibold text-slate-700 mb-1">Endpoint URL (POST)</label>
                <input
                  type="text"
                  value={aiEndpoint}
                  onChange={(e) => setAiEndpoint(e.target.value)}
                  placeholder="https://api.xingchiapi.com hoặc https://api.openai.com/v1/chat/completions"
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>
            )}

            <div className="sm:col-span-2">
              <label className="block font-semibold text-slate-700 mb-1">Khóa API Key</label>
              <input
                type="password"
                value={aiApiKey}
                onChange={(e) => setAiApiKey(e.target.value)}
                placeholder="Dán mã API Key của bạn tại đây (AIzaSy..., sk-proj..., sk-...). Bỏ trống để dùng kịch bản offline."
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
              />
              <span className="text-[11px] text-slate-400 mt-1 block">
                🔒 Chỉ Quản trị viên mới có thể xem và lưu khóa này. Khách hàng thông thường chưa đăng nhập không thể sử dụng chatbot và không thể truy cập khóa này.
              </span>
            </div>
          </div>
        </div>

        {/* Inventory Defaults */}
        <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-2xs space-y-4">
          <h2 className="text-xs font-bold uppercase tracking-wider text-slate-700 border-b border-slate-100 pb-2 flex items-center gap-2">
            <Boxes className="w-4 h-4 text-amber-600" strokeWidth={1.5} />
            Ngưỡng tồn kho mặc định
          </h2>

          <div className="max-w-xs text-xs">
            <label className="block font-semibold text-slate-700 mb-1">
              Ngưỡng cảnh báo sắp hết hàng mặc định (Reorder Level)
            </label>
            <input
              type="number"
              required
              min={1}
              value={settings.defaultLowStockThreshold}
              onChange={e =>
                setSettings({ ...settings, defaultLowStockThreshold: Number(e.target.value) })
              }
              className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
            />
            <span className="text-[11px] text-slate-400 mt-1 block">Áp dụng khi tạo sản phẩm mới</span>
          </div>
        </div>

        {/* Submit */}
        <div className="flex items-center justify-end pt-2">
          <button
            type="submit"
            disabled={isSaving}
            className="inline-flex items-center gap-2 px-6 py-2.5 rounded-xl bg-leaf text-white text-xs font-semibold hover:bg-leaf-2 transition-colors shadow-xs disabled:opacity-50"
          >
            <Save className="w-4 h-4" strokeWidth={1.5} />
            {isSaving ? 'Đang lưu cài đặt...' : 'Lưu toàn bộ cài đặt'}
          </button>
        </div>
      </form>
    </div>
  )
}
