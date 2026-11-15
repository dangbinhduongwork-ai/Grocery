import React, { useEffect, useState } from 'react'
import {
  Save,
  CheckCircle2,
  AlertCircle,
  Building,
  CreditCard,
  Truck,
  Boxes,
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
    supportPhone: '1900 1234',
    supportEmail: 'hotro@groceryhub.vn',
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
