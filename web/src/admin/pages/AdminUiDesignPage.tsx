import React, { useState, useEffect } from 'react'
import {
  Palette,
  Image as ImageIcon,
  Plus,
  Trash2,
  Edit2,
  CheckCircle2,
  AlertCircle,
  ArrowUp,
  ArrowDown,
  Sparkles,
  ExternalLink,
  Upload,
  RefreshCw,
  Store,
  PartyPopper,
  Eye,
  EyeOff,
} from 'lucide-react'
import { api, getAccessToken } from '@/shared/api/client'
import { useUiSettings, DEFAULT_UI_SETTINGS, type HolidayTheme, type HolidayIntensity } from '@/shared/hooks/useUiSettings'
import { HolidayEffectOverlay } from '@/shared/components/HolidayEffectOverlay'
import { useRouter } from '@/app/router'

interface AdminBanner {
  id: string
  title: string
  subtitle?: string | null
  imageKey: string
  imageUrl: string
  linkUrl?: string | null
  placement: string
  sortOrder: number
  isActive: boolean
  createdAt: string
}

const PRESET_BANNERS = [
  {
    title: 'Nông Sản Tươi Ngon Chuẩn VietGAP',
    subtitle: 'Giao nhanh 2h tại Hà Nội & TP. Hồ Chí Minh. Miễn phí vận chuyển từ 299.000đ',
    imageKey: 'https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=1200&q=80',
    linkUrl: '/c/rau-cu-qua',
  },
  {
    title: 'Trái Cây Nhập Khẩu & Đặc Sản Vùng Miền',
    subtitle: 'Tươi ngọt tự nhiên, cam kết đổi trả trong 24h nếu không hài lòng',
    imageKey: 'https://images.unsplash.com/photo-1619566636858-adf3ef46400b?auto=format&fit=crop&w=1200&q=80',
    linkUrl: '/c/trai-cay-tuoi',
  },
  {
    title: 'Thịt & Hải Sản Tươi Sống Mỗi Ngày',
    subtitle: 'Nguồn gốc rõ ràng, kiểm dịch nghiêm ngặt, giữ trọn độ tươi ngon',
    imageKey: 'https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?auto=format&fit=crop&w=1200&q=80',
    linkUrl: '/c/thit-trung',
  },
]

export const AdminUiDesignPage: React.FC = () => {
  const router = useRouter()
  const { settings: uiSettings, saveSettings } = useUiSettings()

  const [activeTab, setActiveTab] = useState<'banners' | 'marquee' | 'slogans' | 'holiday'>('banners')
  const [banners, setBanners] = useState<AdminBanner[]>([])
  const [loadingBanners, setLoadingBanners] = useState(true)
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingBanner, setEditingBanner] = useState<AdminBanner | null>(null)

  // Banner Form State
  const [bannerTitle, setBannerTitle] = useState('')
  const [bannerSubtitle, setBannerSubtitle] = useState('')
  const [bannerImageKey, setBannerImageKey] = useState('')
  const [bannerLinkUrl, setBannerLinkUrl] = useState('')
  const [bannerPlacement, setBannerPlacement] = useState('HomeHero')
  const [bannerSortOrder, setBannerSortOrder] = useState(0)
  const [bannerIsActive, setBannerIsActive] = useState(true)
  const [uploadingImage, setUploadingImage] = useState(false)

  // Marquee & Slogans Form State
  const [announcements, setAnnouncements] = useState<string[]>([])
  const [newAnnouncement, setNewAnnouncement] = useState('')
  const [heroBadge, setHeroBadge] = useState('')
  const [heroTitle, setHeroTitle] = useState('')
  const [heroSubtitle, setHeroSubtitle] = useState('')

  // Holiday Festival Effects State
  const [holidayEnabled, setHolidayEnabled] = useState(uiSettings.holidayEffect?.enabled ?? true)
  const [holidayTheme, setHolidayTheme] = useState<HolidayTheme>(uiSettings.holidayEffect?.theme ?? 'tet')
  const [holidayIntensity, setHolidayIntensity] = useState<HolidayIntensity>(uiSettings.holidayEffect?.intensity ?? 'medium')
  const [holidayBannerText, setHolidayBannerText] = useState(uiSettings.holidayEffect?.bannerText || '')
  const [previewEffect, setPreviewEffect] = useState(false)

  // Notifications
  const [successMsg, setSuccessMsg] = useState<string | null>(null)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  const showSuccess = (msg: string) => {
    setSuccessMsg(msg)
    setErrorMsg(null)
    setTimeout(() => setSuccessMsg(null), 4000)
  }

  const showError = (msg: string) => {
    setErrorMsg(msg)
    setSuccessMsg(null)
    setTimeout(() => setErrorMsg(null), 5000)
  }

  // Load Banners
  const loadBanners = async () => {
    try {
      setLoadingBanners(true)
      const res = await (api as any).GET('/api/v1/admin/banners')
      if (res && res.data) {
        setBanners(res.data as AdminBanner[])
      }
    } catch (err: any) {
      showError('Không thể tải danh sách banner: ' + (err?.message || 'Lỗi kết nối'))
    } finally {
      setLoadingBanners(false)
    }
  }

  useEffect(() => {
    void loadBanners()
  }, [])

  // Sync initial UI settings
  useEffect(() => {
    setAnnouncements(uiSettings.announcements || DEFAULT_UI_SETTINGS.announcements)
    setHeroBadge(uiSettings.heroBadge || DEFAULT_UI_SETTINGS.heroBadge)
    setHeroTitle(uiSettings.heroTitle || DEFAULT_UI_SETTINGS.heroTitle)
    setHeroSubtitle(uiSettings.heroSubtitle || DEFAULT_UI_SETTINGS.heroSubtitle)
    if (uiSettings.holidayEffect) {
      setHolidayEnabled(uiSettings.holidayEffect.enabled)
      setHolidayTheme(uiSettings.holidayEffect.theme)
      setHolidayIntensity(uiSettings.holidayEffect.intensity)
      setHolidayBannerText(uiSettings.holidayEffect.bannerText || '')
    }
  }, [uiSettings])

  // Open modal for Create or Edit
  const openBannerModal = (banner?: AdminBanner) => {
    if (banner) {
      setEditingBanner(banner)
      setBannerTitle(banner.title)
      setBannerSubtitle(banner.subtitle || '')
      setBannerImageKey(banner.imageKey)
      setBannerLinkUrl(banner.linkUrl || '')
      setBannerPlacement(banner.placement || 'HomeHero')
      setBannerSortOrder(banner.sortOrder)
      setBannerIsActive(banner.isActive)
    } else {
      setEditingBanner(null)
      setBannerTitle('')
      setBannerSubtitle('')
      setBannerImageKey('')
      setBannerLinkUrl('/c/rau-cu-qua')
      setBannerPlacement('HomeHero')
      setBannerSortOrder(banners.length)
      setBannerIsActive(true)
    }
    setIsModalOpen(true)
  }

  // Handle Image Upload
  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    if (!file.type.startsWith('image/')) {
      showError('Vui lòng chọn tệp định dạng hình ảnh (JPEG, PNG, WebP).')
      return
    }

    if (file.size > 5 * 1024 * 1024) {
      showError('Dung lượng ảnh tối đa là 5MB.')
      return
    }

    try {
      setUploadingImage(true)
      const formData = new FormData()
      formData.append('file', file)

      const token = getAccessToken()
      const response = await fetch('/api/v1/admin/banners/upload-image', {
        method: 'POST',
        headers: {
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: formData,
      })

      if (!response.ok) {
        const errorData = await response.json().catch(() => null)
        throw new Error(errorData?.detail || 'Không thể tải ảnh lên.')
      }

      const data = await response.json()
      setBannerImageKey(data.imageUrl || data.imageKey)
      showSuccess('Tải ảnh banner lên thành công!')
    } catch (err: any) {
      showError(err.message || 'Lỗi khi tải ảnh lên.')
    } finally {
      setUploadingImage(false)
    }
  }

  // Save Banner (Create or Update)
  const handleSaveBanner = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!bannerTitle.trim()) {
      showError('Tiêu đề banner không được để trống.')
      return
    }
    if (!bannerImageKey.trim()) {
      showError('Vui lòng tải ảnh lên hoặc nhập URL hình ảnh.')
      return
    }

    try {
      setIsSaving(true)
      const payload = {
        title: bannerTitle.trim(),
        subtitle: bannerSubtitle.trim() || null,
        imageKey: bannerImageKey.trim(),
        linkUrl: bannerLinkUrl.trim() || null,
        placement: bannerPlacement,
        sortOrder: Number(bannerSortOrder) || 0,
        isActive: bannerIsActive,
      }

      if (editingBanner) {
        const res = await (api as any).PUT(`/api/v1/admin/banners/${editingBanner.id}`, {
          body: payload,
        })
        if (res && res.error) {
          throw new Error(res.error.detail || 'Không thể cập nhật banner.')
        }
        showSuccess('Cập nhật banner thành công! Cửa hàng đã được đồng bộ.')
      } else {
        const res = await (api as any).POST('/api/v1/admin/banners', {
          body: payload,
        })
        if (res && res.error) {
          throw new Error(res.error.detail || 'Không thể tạo banner mới.')
        }
        showSuccess('Tạo banner mới thành công! Cửa hàng đã được đồng bộ.')
      }

      setIsModalOpen(false)
      window.dispatchEvent(new CustomEvent('groceryhub:banners-updated'))
      await loadBanners()
    } catch (err: any) {
      showError(err?.message || 'Lỗi khi lưu banner.')
    } finally {
      setIsSaving(false)
    }
  }

  // Toggle Banner Active
  const handleToggleActive = async (banner: AdminBanner) => {
    try {
      const res = await (api as any).PUT(`/api/v1/admin/banners/${banner.id}`, {
        body: {
          title: banner.title,
          subtitle: banner.subtitle,
          imageKey: banner.imageKey,
          linkUrl: banner.linkUrl,
          placement: banner.placement,
          sortOrder: banner.sortOrder,
          isActive: !banner.isActive,
        },
      })
      if (res && res.error) {
        throw new Error(res.error.detail || 'Không thể đổi trạng thái banner.')
      }
      showSuccess(`Đã ${!banner.isActive ? 'kích hoạt' : 'tắt'} banner "${banner.title}".`)
      window.dispatchEvent(new CustomEvent('groceryhub:banners-updated'))
      await loadBanners()
    } catch (err: any) {
      showError(err.message || 'Lỗi khi đổi trạng thái.')
    }
  }

  // Delete Banner
  const handleDeleteBanner = async (banner: AdminBanner) => {
    if (!window.confirm(`Bạn có chắc muốn xóa banner "${banner.title}" không?`)) {
      return
    }

    try {
      const res = await (api as any).DELETE(`/api/v1/admin/banners/${banner.id}`)
      if (res && res.error) {
        throw new Error(res.error.detail || 'Không thể xóa banner.')
      }
      showSuccess('Đã xóa banner thành công.')
      window.dispatchEvent(new CustomEvent('groceryhub:banners-updated'))
      await loadBanners()
    } catch (err: any) {
      showError(err.message || 'Lỗi khi xóa banner.')
    }
  }

  // Marquee handlers
  const handleAddAnnouncement = () => {
    if (!newAnnouncement.trim()) return
    setAnnouncements((prev) => [...prev, newAnnouncement.trim()])
    setNewAnnouncement('')
  }

  const handleRemoveAnnouncement = (index: number) => {
    setAnnouncements((prev) => prev.filter((_, i) => i !== index))
  }

  const handleMoveAnnouncement = (index: number, direction: 'up' | 'down') => {
    const targetIdx = direction === 'up' ? index - 1 : index + 1
    if (targetIdx < 0 || targetIdx >= announcements.length) return
    const updated = [...announcements]
    const temp = updated[index]
    updated[index] = updated[targetIdx]
    updated[targetIdx] = temp
    setAnnouncements(updated)
  }

  // Save UI Settings (Marquee & Slogans)
  const handleSaveUiSettings = async () => {
    try {
      setIsSaving(true)
      const res = await saveSettings({
        ...uiSettings,
        announcements: announcements.length > 0 ? announcements : DEFAULT_UI_SETTINGS.announcements,
        heroBadge: heroBadge.trim() || DEFAULT_UI_SETTINGS.heroBadge,
        heroTitle: heroTitle.trim() || DEFAULT_UI_SETTINGS.heroTitle,
        heroSubtitle: heroSubtitle.trim() || DEFAULT_UI_SETTINGS.heroSubtitle,
      })

      if (res.success) {
        showSuccess('Đã lưu thiết kế giao diện! Cửa hàng đã cập nhật tức thì.')
      } else {
        showError(res.error || 'Không thể lưu cài đặt giao diện.')
      }
    } catch (err: any) {
      showError(err.message || 'Lỗi khi lưu cài đặt.')
    } finally {
      setIsSaving(false)
    }
  }

  // Save Holiday Festival Effects
  const handleSaveHolidaySettings = async () => {
    try {
      setIsSaving(true)
      const res = await saveSettings({
        ...uiSettings,
        holidayEffect: {
          enabled: holidayEnabled,
          theme: holidayTheme,
          intensity: holidayIntensity,
          bannerText: holidayBannerText.trim(),
        },
      })

      if (res.success) {
        showSuccess('Đã lưu hiệu ứng ngày lễ thành công! Cửa hàng đã hiển thị hiệu ứng mới.')
      } else {
        showError(res.error || 'Không thể lưu hiệu ứng ngày lễ.')
      }
    } catch (err: any) {
      showError(err.message || 'Lỗi khi lưu hiệu ứng.')
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className="space-y-6 max-w-6xl mx-auto pb-12">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-5 rounded-2xl border border-slate-200 shadow-2xs">
        <div>
          <div className="flex items-center gap-2 text-leaf font-semibold text-xs tracking-wide uppercase">
            <Palette className="w-4 h-4" />
            <span>Tùy biến cửa hàng</span>
          </div>
          <h1 className="text-xl font-bold text-slate-800 mt-1">Banner & Thiết Kế Giao Diện</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Quản lý banner nổi bật, thanh chạy thông báo marquee và slogan trang chủ theo thời gian thực.
          </p>
        </div>

        <div className="flex items-center gap-2.5">
          <button
            type="button"
            onClick={() => router.push('/')}
            className="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-leaf/10 text-leaf hover:bg-leaf hover:text-white font-medium text-xs transition-colors cursor-pointer border border-leaf/20"
            title="Mở trang chủ cửa hàng để kiểm tra thay đổi"
          >
            <Store className="w-4 h-4" />
            <span>Xem Shop Thực tế</span>
            <ExternalLink className="w-3.5 h-3.5 opacity-70" />
          </button>
        </div>
      </div>

      {/* Notifications */}
      {successMsg && (
        <div className="p-3.5 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-800 text-xs flex items-center gap-2">
          <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
          <span>{successMsg}</span>
        </div>
      )}
      {errorMsg && (
        <div className="p-3.5 rounded-xl bg-rose-50 border border-rose-200 text-rose-800 text-xs flex items-center gap-2">
          <AlertCircle className="w-4 h-4 text-rose-600 shrink-0" />
          <span>{errorMsg}</span>
        </div>
      )}

      {/* Tabs */}
      <div className="flex border-b border-slate-200 gap-2">
        <button
          type="button"
          onClick={() => setActiveTab('banners')}
          className={`px-4 py-2.5 text-xs font-semibold border-b-2 transition-colors cursor-pointer flex items-center gap-2 ${
            activeTab === 'banners'
              ? 'border-leaf text-leaf'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          <ImageIcon className="w-4 h-4" />
          <span>Banner Quảng Cáo ({banners.length})</span>
        </button>

        <button
          type="button"
          onClick={() => setActiveTab('marquee')}
          className={`px-4 py-2.5 text-xs font-semibold border-b-2 transition-colors cursor-pointer flex items-center gap-2 ${
            activeTab === 'marquee'
              ? 'border-leaf text-leaf'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          <Sparkles className="w-4 h-4" />
          <span>Thanh Chạy Marquee ({announcements.length})</span>
        </button>

        <button
          type="button"
          onClick={() => setActiveTab('slogans')}
          className={`px-4 py-2.5 text-xs font-semibold border-b-2 transition-colors cursor-pointer flex items-center gap-2 ${
            activeTab === 'slogans'
              ? 'border-leaf text-leaf'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          <Palette className="w-4 h-4" />
          <span>Slogan & Thông Điệp</span>
        </button>

        <button
          type="button"
          onClick={() => setActiveTab('holiday')}
          className={`px-4 py-2.5 text-xs font-semibold border-b-2 transition-colors cursor-pointer flex items-center gap-2 ${
            activeTab === 'holiday'
              ? 'border-leaf text-leaf'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          <PartyPopper className="w-4 h-4 text-amber-500" />
          <span>Hiệu Ứng Ngày Lễ & Ngày Hội</span>
          {holidayEnabled && holidayTheme !== 'none' && (
            <span className="w-2 h-2 rounded-full bg-emerald-500 inline-block" />
          )}
        </button>
      </div>

      {/* Tab 1: Banners */}
      {activeTab === 'banners' && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-bold text-slate-800">Danh sách Banner trình chiếu</h2>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => void loadBanners()}
                className="p-2 rounded-lg text-slate-500 hover:bg-slate-100 transition-colors"
                title="Làm mới danh sách"
              >
                <RefreshCw className="w-4 h-4" />
              </button>
              <button
                type="button"
                onClick={() => openBannerModal()}
                className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-leaf text-white font-medium text-xs hover:bg-leaf/90 transition-colors shadow-2xs cursor-pointer"
              >
                <Plus className="w-4 h-4" />
                <span>Thêm Banner mới</span>
              </button>
            </div>
          </div>

          {loadingBanners ? (
            <div className="bg-white p-8 rounded-2xl border border-slate-200 text-center text-xs text-slate-500 animate-pulse">
              Đang tải danh sách banner...
            </div>
          ) : banners.length === 0 ? (
            <div className="bg-white p-8 rounded-2xl border border-slate-200 text-center space-y-4">
              <div className="w-12 h-12 rounded-full bg-slate-100 text-slate-400 flex items-center justify-center mx-auto">
                <ImageIcon className="w-6 h-6" />
              </div>
              <div>
                <h3 className="text-sm font-bold text-slate-700">Chưa có banner nào</h3>
                <p className="text-xs text-slate-500 mt-1 max-w-md mx-auto">
                  Cửa hàng hiện đang dùng banner mặc định. Bạn có thể thêm banner mới hoặc chọn mẫu tạo nhanh bên dưới!
                </p>
              </div>
              <div className="flex flex-wrap items-center justify-center gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => openBannerModal()}
                  className="px-4 py-2 rounded-xl bg-leaf text-white font-semibold text-xs hover:bg-leaf/90 shadow-2xs"
                >
                  Tạo Banner Mới
                </button>
                <button
                  type="button"
                  onClick={() => {
                    const preset = PRESET_BANNERS[0]
                    setBannerTitle(preset.title)
                    setBannerSubtitle(preset.subtitle)
                    setBannerImageKey(preset.imageKey)
                    setBannerLinkUrl(preset.linkUrl)
                    setBannerPlacement('HomeHero')
                    setBannerSortOrder(0)
                    setBannerIsActive(true)
                    setIsModalOpen(true)
                  }}
                  className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 font-semibold text-xs hover:bg-slate-200"
                >
                  Dùng Mẫu Nông Sản VietGAP
                </button>
              </div>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {banners.map((b) => (
                <div
                  key={b.id}
                  className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-2xs flex flex-col justify-between hover:border-slate-300 transition-all"
                >
                  <div className="relative aspect-[21/9] bg-slate-100 overflow-hidden border-b border-slate-100">
                    {b.imageUrl || b.imageKey ? (
                      <img
                        src={b.imageUrl || b.imageKey}
                        alt={b.title}
                        className="w-full h-full object-cover"
                        onError={(e) => {
                          ;(e.target as HTMLElement).style.display = 'none'
                        }}
                      />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center text-slate-300">
                        <ImageIcon className="w-8 h-8" />
                      </div>
                    )}
                    <div className="absolute top-2.5 right-2.5 flex items-center gap-1.5">
                      <span
                        className={`px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider backdrop-blur-xs ${
                          b.isActive
                            ? 'bg-emerald-500/90 text-white'
                            : 'bg-slate-800/70 text-slate-200'
                        }`}
                      >
                        {b.isActive ? 'Đang hiển thị' : 'Đã ẩn'}
                      </span>
                      <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-white/90 text-slate-700 backdrop-blur-xs">
                        {b.placement}
                      </span>
                    </div>
                  </div>

                  <div className="p-4 space-y-2 flex-1">
                    <h3 className="font-bold text-sm text-slate-800 leading-snug line-clamp-1">
                      {b.title}
                    </h3>
                    {b.subtitle && (
                      <p className="text-xs text-slate-500 line-clamp-2 leading-relaxed">
                        {b.subtitle}
                      </p>
                    )}
                    <div className="text-[11px] text-slate-400 flex items-center gap-3 pt-1">
                      <span>Thứ tự: {b.sortOrder}</span>
                      {b.linkUrl && (
                        <span className="truncate max-w-[200px]" title={b.linkUrl}>
                          Link: {b.linkUrl}
                        </span>
                      )}
                    </div>
                  </div>

                  <div className="p-3 bg-slate-50/80 border-t border-slate-100 flex items-center justify-between gap-2">
                    <button
                      type="button"
                      onClick={() => void handleToggleActive(b)}
                      className={`text-xs font-semibold px-2.5 py-1 rounded-lg transition-colors cursor-pointer ${
                        b.isActive
                          ? 'text-slate-600 hover:bg-slate-200'
                          : 'text-leaf hover:bg-leaf/10'
                      }`}
                    >
                      {b.isActive ? 'Tắt hiển thị' : 'Bật hiển thị'}
                    </button>

                    <div className="flex items-center gap-1">
                      <button
                        type="button"
                        onClick={() => openBannerModal(b)}
                        className="p-1.5 text-slate-600 hover:text-leaf hover:bg-white rounded-lg transition-colors"
                        title="Chỉnh sửa banner"
                      >
                        <Edit2 className="w-4 h-4" />
                      </button>
                      <button
                        type="button"
                        onClick={() => void handleDeleteBanner(b)}
                        className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-white rounded-lg transition-colors"
                        title="Xóa banner"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Tab 2: Marquee Announcements */}
      {activeTab === 'marquee' && (
        <div className="space-y-6 bg-white p-6 rounded-2xl border border-slate-200 shadow-2xs">
          <div>
            <h2 className="text-sm font-bold text-slate-800">Dòng chữ chạy liên tục (Marquee Header)</h2>
            <p className="text-xs text-slate-500 mt-0.5">
              Các thông điệp này sẽ di chuyển liên tục mượt mà ở thanh đầu trang chủ storefront.
            </p>
          </div>

          {/* Live Marquee Simulation Preview */}
          <div className="p-3 bg-paper-2 rounded-xl border border-rule overflow-hidden">
            <span className="text-[10px] font-bold uppercase tracking-wider text-leaf mb-1 block">
              Xem trước hiệu ứng chạy thực tế:
            </span>
            <div className="overflow-hidden py-1.5 bg-paper rounded-lg border border-rule/50">
              <div className="animate-marquee flex items-center">
                <div className="flex items-center shrink-0">
                  {announcements.map((text, i) => (
                    <div key={`prev1-${i}`} className="flex items-center gap-2 px-6">
                      <Sparkles className="w-3.5 h-3.5 text-leaf shrink-0" />
                      <span className="text-xs font-medium text-ink whitespace-nowrap">{text}</span>
                      <span className="text-leaf/30 ml-6 select-none">•</span>
                    </div>
                  ))}
                </div>
                <div className="flex items-center shrink-0" aria-hidden="true">
                  {announcements.map((text, i) => (
                    <div key={`prev2-${i}`} className="flex items-center gap-2 px-6">
                      <Sparkles className="w-3.5 h-3.5 text-leaf shrink-0" />
                      <span className="text-xs font-medium text-ink whitespace-nowrap">{text}</span>
                      <span className="text-leaf/30 ml-6 select-none">•</span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* List of Announcements */}
          <div className="space-y-2">
            <label className="text-xs font-semibold text-slate-700 block">Danh sách các thông báo:</label>
            <div className="space-y-2">
              {announcements.map((item, index) => (
                <div
                  key={index}
                  className="flex items-center justify-between gap-3 p-3 rounded-xl bg-slate-50 border border-slate-200"
                >
                  <div className="flex items-center gap-2.5 flex-1 min-w-0">
                    <span className="w-5 h-5 rounded-full bg-slate-200 text-slate-600 text-[10px] font-bold flex items-center justify-center shrink-0">
                      {index + 1}
                    </span>
                    <input
                      type="text"
                      value={item}
                      onChange={(e) => {
                        const updated = [...announcements]
                        updated[index] = e.target.value
                        setAnnouncements(updated)
                      }}
                      className="flex-1 bg-transparent text-xs font-medium text-slate-800 border-none outline-none focus:ring-1 focus:ring-leaf rounded px-2 py-1"
                    />
                  </div>

                  <div className="flex items-center gap-1 shrink-0">
                    <button
                      type="button"
                      disabled={index === 0}
                      onClick={() => handleMoveAnnouncement(index, 'up')}
                      className="p-1 text-slate-400 hover:text-slate-700 disabled:opacity-30"
                      title="Di chuyển lên"
                    >
                      <ArrowUp className="w-4 h-4" />
                    </button>
                    <button
                      type="button"
                      disabled={index === announcements.length - 1}
                      onClick={() => handleMoveAnnouncement(index, 'down')}
                      className="p-1 text-slate-400 hover:text-slate-700 disabled:opacity-30"
                      title="Di chuyển xuống"
                    >
                      <ArrowDown className="w-4 h-4" />
                    </button>
                    <button
                      type="button"
                      onClick={() => handleRemoveAnnouncement(index)}
                      className="p-1 text-slate-400 hover:text-rose-600 ml-1"
                      title="Xóa thông báo"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              ))}
            </div>

            {/* Add new announcement input */}
            <div className="flex items-center gap-2 pt-2">
              <input
                type="text"
                placeholder="Nhập thông báo mới (ví dụ: Giảm thêm 10% khi thanh toán VietQR)..."
                value={newAnnouncement}
                onChange={(e) => setNewAnnouncement(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    e.preventDefault()
                    handleAddAnnouncement()
                  }
                }}
                className="flex-1 px-3.5 py-2 text-xs rounded-xl border border-slate-200 outline-none focus:border-leaf focus:ring-1 focus:ring-leaf"
              />
              <button
                type="button"
                onClick={handleAddAnnouncement}
                className="px-4 py-2 rounded-xl bg-slate-800 text-white font-medium text-xs hover:bg-slate-700 transition-colors cursor-pointer shrink-0"
              >
                Thêm vào thanh chạy
              </button>
            </div>
          </div>

          <div className="pt-4 border-t border-slate-100 flex items-center justify-between">
            <button
              type="button"
              onClick={() => setAnnouncements(DEFAULT_UI_SETTINGS.announcements)}
              className="text-xs text-slate-500 hover:text-slate-800 underline"
            >
              Khôi phục thông báo mặc định
            </button>

            <button
              type="button"
              disabled={isSaving}
              onClick={() => void handleSaveUiSettings()}
              className="px-5 py-2.5 rounded-xl bg-leaf text-white font-bold text-xs hover:bg-leaf/90 transition-all shadow-2xs cursor-pointer flex items-center gap-2"
            >
              <CheckCircle2 className="w-4 h-4" />
              <span>{isSaving ? 'Đang lưu...' : 'Lưu Thay Đổi & Cập Nhật Lên Shop'}</span>
            </button>
          </div>
        </div>
      )}

      {/* Tab 3: Slogans & Badges */}
      {activeTab === 'slogans' && (
        <div className="space-y-6 bg-white p-6 rounded-2xl border border-slate-200 shadow-2xs">
          <div>
            <h2 className="text-sm font-bold text-slate-800">Slogan & Thông Điệp Cửa Hàng</h2>
            <p className="text-xs text-slate-500 mt-0.5">
              Tùy biến tiêu đề và khẩu hiệu xuất hiện trên storefront.
            </p>
          </div>

          <div className="space-y-4 max-w-2xl">
            <div>
              <label className="text-xs font-semibold text-slate-700 block mb-1">
                Badge nhãn trang chủ (Dòng chữ nhỏ góc trên):
              </label>
              <input
                type="text"
                value={heroBadge}
                onChange={(e) => setHeroBadge(e.target.value)}
                placeholder="Nông sản tươi sạch mỗi ngày"
                className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 outline-none focus:border-leaf focus:ring-1 focus:ring-leaf"
              />
            </div>

            <div>
              <label className="text-xs font-semibold text-slate-700 block mb-1">
                Tiêu đề chính Hero (Khi không có banner hình):
              </label>
              <input
                type="text"
                value={heroTitle}
                onChange={(e) => setHeroTitle(e.target.value)}
                placeholder="Nông Sản Tươi Ngon Chuẩn VietGAP"
                className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 outline-none focus:border-leaf focus:ring-1 focus:ring-leaf"
              />
            </div>

            <div>
              <label className="text-xs font-semibold text-slate-700 block mb-1">
                Phụ đề mô tả Hero:
              </label>
              <textarea
                rows={3}
                value={heroSubtitle}
                onChange={(e) => setHeroSubtitle(e.target.value)}
                placeholder="Hệ thống cung cấp thực phẩm an toàn, rõ nguồn gốc cho bữa ăn gia đình tràn đầy sức khỏe."
                className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 outline-none focus:border-leaf focus:ring-1 focus:ring-leaf leading-relaxed"
              />
            </div>
          </div>

          <div className="pt-4 border-t border-slate-100 flex items-center justify-end">
            <button
              type="button"
              disabled={isSaving}
              onClick={() => void handleSaveUiSettings()}
              className="px-5 py-2.5 rounded-xl bg-leaf text-white font-bold text-xs hover:bg-leaf/90 transition-all shadow-2xs cursor-pointer flex items-center gap-2"
            >
              <CheckCircle2 className="w-4 h-4" />
              <span>{isSaving ? 'Đang lưu...' : 'Lưu Thay Đổi & Cập Nhật Lên Shop'}</span>
            </button>
          </div>
        </div>
      )}

      {/* Tab 4: Holiday & Festival Effects */}
      {activeTab === 'holiday' && (
        <div className="space-y-6 bg-white p-6 rounded-2xl border border-slate-200 shadow-2xs">
          {previewEffect && (
            <HolidayEffectOverlay forceTheme={holidayTheme} forceIntensity={holidayIntensity} />
          )}

          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <h2 className="text-sm font-bold text-slate-800 flex items-center gap-2">
                <PartyPopper className="w-4 h-4 text-amber-500" />
                Hiệu Ứng Ngày Hội, Ngày Lễ & Mùa Sự Kiện
              </h2>
              <p className="text-xs text-slate-500 mt-0.5">
                Tạo không khí lễ hội vui tươi trên toàn bộ website với hiệu ứng hạt rơi nghệ thuật (hoa mai/đào Tết, tuyết rơi Giáng sinh, pháo giấy siêu hội sale).
              </p>
            </div>

            {/* Quick Preview Toggle */}
            <button
              type="button"
              onClick={() => setPreviewEffect(!previewEffect)}
              className={`px-3 py-1.5 rounded-xl text-xs font-semibold flex items-center gap-1.5 transition-colors cursor-pointer border ${
                previewEffect
                  ? 'bg-amber-100 text-amber-800 border-amber-300'
                  : 'bg-slate-100 text-slate-600 border-slate-200 hover:bg-slate-200'
              }`}
            >
              {previewEffect ? <EyeOff className="w-3.5 h-3.5" /> : <Eye className="w-3.5 h-3.5" />}
              <span>{previewEffect ? 'Đang bật xem thử (Tắt)' : 'Xem thử hiệu ứng ngay'}</span>
            </button>
          </div>

          {/* Master Enable/Disable Switch */}
          <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 flex items-center justify-between">
            <div>
              <span className="text-xs font-bold text-slate-800 block">Kích hoạt hiệu ứng trên cửa hàng</span>
              <span className="text-[11px] text-slate-500">
                {holidayEnabled
                  ? 'Hiệu ứng đang được bật và hiển thị cho toàn bộ khách hàng khi ghé thăm shop.'
                  : 'Hiệu ứng đang tạm tắt trên shop.'}
              </span>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                checked={holidayEnabled}
                onChange={(e) => setHolidayEnabled(e.target.checked)}
                className="sr-only peer"
              />
              <div className="w-11 h-6 bg-slate-300 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-leaf"></div>
            </label>
          </div>

          {/* Theme Selection Cards */}
          <div>
            <label className="text-xs font-bold text-slate-700 block mb-2">
              Chọn chủ đề ngày hội / ngày lễ:
            </label>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3.5">
              {/* Option 1: Tet */}
              <div
                onClick={() => setHolidayTheme('tet')}
                className={`p-4 rounded-2xl border-2 transition-all cursor-pointer relative overflow-hidden ${
                  holidayTheme === 'tet'
                    ? 'border-amber-500 bg-amber-50/50 shadow-xs'
                    : 'border-slate-200 hover:border-slate-300 bg-white'
                }`}
              >
                <div className="text-2xl mb-2">🌸</div>
                <div className="font-bold text-xs text-slate-800">Tết Nguyên Đán</div>
                <div className="text-[11px] text-slate-500 mt-1 leading-normal">
                  Hoa mai vàng & hoa đào hồng rơi rực rỡ, mang sắc xuân và may mắn tài lộc.
                </div>
                {holidayTheme === 'tet' && (
                  <span className="absolute top-2 right-2 text-xs font-bold text-amber-600">✓ Đang chọn</span>
                )}
              </div>

              {/* Option 2: Christmas */}
              <div
                onClick={() => setHolidayTheme('christmas')}
                className={`p-4 rounded-2xl border-2 transition-all cursor-pointer relative overflow-hidden ${
                  holidayTheme === 'christmas'
                    ? 'border-sky-500 bg-sky-50/50 shadow-xs'
                    : 'border-slate-200 hover:border-slate-300 bg-white'
                }`}
              >
                <div className="text-2xl mb-2">❄️</div>
                <div className="font-bold text-xs text-slate-800">Giáng Sinh & Năm Mới</div>
                <div className="text-[11px] text-slate-500 mt-1 leading-normal">
                  Bông tuyết trắng rơi êm dịu, không khí mùa đông ấm áp và tươi vui.
                </div>
                {holidayTheme === 'christmas' && (
                  <span className="absolute top-2 right-2 text-xs font-bold text-sky-600">✓ Đang chọn</span>
                )}
              </div>

              {/* Option 3: Sale */}
              <div
                onClick={() => setHolidayTheme('sale')}
                className={`p-4 rounded-2xl border-2 transition-all cursor-pointer relative overflow-hidden ${
                  holidayTheme === 'sale'
                    ? 'border-rose-500 bg-rose-50/50 shadow-xs'
                    : 'border-slate-200 hover:border-slate-300 bg-white'
                }`}
              >
                <div className="text-2xl mb-2">🎉</div>
                <div className="font-bold text-xs text-slate-800">Siêu Hội Mua Sắm / Sale</div>
                <div className="text-[11px] text-slate-500 mt-1 leading-normal">
                  Pháo giấy Confetti đa sắc màu & ngôi sao lấp lánh cho ngày sale lớn.
                </div>
                {holidayTheme === 'sale' && (
                  <span className="absolute top-2 right-2 text-xs font-bold text-rose-600">✓ Đang chọn</span>
                )}
              </div>

              {/* Option 4: Mid-Autumn */}
              <div
                onClick={() => setHolidayTheme('mid_autumn')}
                className={`p-4 rounded-2xl border-2 transition-all cursor-pointer relative overflow-hidden ${
                  holidayTheme === 'mid_autumn'
                    ? 'border-orange-500 bg-orange-50/50 shadow-xs'
                    : 'border-slate-200 hover:border-slate-300 bg-white'
                }`}
              >
                <div className="text-2xl mb-2">🍂</div>
                <div className="font-bold text-xs text-slate-800">Mùa Thu & Trung Thu</div>
                <div className="text-[11px] text-slate-500 mt-1 leading-normal">
                  Lá vàng phong rơi nhẹ nhàng, sắc màu ấm áp mùa trăng rằm.
                </div>
                {holidayTheme === 'mid_autumn' && (
                  <span className="absolute top-2 right-2 text-xs font-bold text-orange-600">✓ Đang chọn</span>
                )}
              </div>
            </div>
          </div>

          {/* Intensity Selection */}
          <div className="space-y-2">
            <label className="text-xs font-bold text-slate-700 block">
              Mật độ hạt rơi (Cường độ hiệu ứng):
            </label>
            <div className="grid grid-cols-3 gap-3 max-w-md">
              <button
                type="button"
                onClick={() => setHolidayIntensity('low')}
                className={`py-2 px-3 rounded-xl border text-xs font-semibold transition-colors cursor-pointer text-center ${
                  holidayIntensity === 'low'
                    ? 'border-leaf bg-leaf/10 text-leaf'
                    : 'border-slate-200 text-slate-600 hover:bg-slate-50'
                }`}
              >
                Nhẹ nhàng (Low)
              </button>
              <button
                type="button"
                onClick={() => setHolidayIntensity('medium')}
                className={`py-2 px-3 rounded-xl border text-xs font-semibold transition-colors cursor-pointer text-center ${
                  holidayIntensity === 'medium'
                    ? 'border-leaf bg-leaf/10 text-leaf'
                    : 'border-slate-200 text-slate-600 hover:bg-slate-50'
                }`}
              >
                Vừa phải (Medium)
              </button>
              <button
                type="button"
                onClick={() => setHolidayIntensity('high')}
                className={`py-2 px-3 rounded-xl border text-xs font-semibold transition-colors cursor-pointer text-center ${
                  holidayIntensity === 'high'
                    ? 'border-leaf bg-leaf/10 text-leaf'
                    : 'border-slate-200 text-slate-600 hover:bg-slate-50'
                }`}
              >
                Rộn ràng (High)
              </button>
            </div>
          </div>

          {/* Festive Banner Text (Optional) */}
          <div className="space-y-1.5 max-w-xl">
            <label className="text-xs font-bold text-slate-700 block">
              Khẩu hiệu ngày lễ (Banner text):
            </label>
            <input
              type="text"
              value={holidayBannerText}
              onChange={(e) => setHolidayBannerText(e.target.value)}
              placeholder="Ví dụ: Tết Nguyên Đán 2026 - Rộn Ràng Đón Xuân Tài Lộc"
              className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 outline-none focus:border-leaf focus:ring-1 focus:ring-leaf"
            />
          </div>

          {/* Action buttons */}
          <div className="pt-4 border-t border-slate-100 flex items-center justify-between">
            <button
              type="button"
              onClick={() => router.push('/')}
              className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-slate-100 text-slate-700 hover:bg-slate-200 text-xs font-semibold transition-colors cursor-pointer"
            >
              <Store className="w-3.5 h-3.5" />
              <span>Xem Shop thực tế</span>
            </button>

            <button
              type="button"
              disabled={isSaving}
              onClick={() => void handleSaveHolidaySettings()}
              className="px-5 py-2.5 rounded-xl bg-leaf text-white font-bold text-xs hover:bg-leaf/90 transition-all shadow-2xs cursor-pointer flex items-center gap-2"
            >
              <CheckCircle2 className="w-4 h-4" />
              <span>{isSaving ? 'Đang lưu...' : 'Lưu & Kích Hoạt Hiệu Ứng Lên Shop'}</span>
            </button>
          </div>
        </div>
      )}

      {/* Banner Create / Edit Dialog Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white w-full max-w-xl rounded-2xl shadow-xl border border-slate-200 overflow-hidden flex flex-col max-h-[90vh]">
            <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
              <h3 className="font-bold text-slate-800 text-sm">
                {editingBanner ? 'Chỉnh sửa Banner' : 'Thêm Banner Quảng Cáo Mới'}
              </h3>
              <button
                type="button"
                onClick={() => setIsModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 text-sm font-bold p-1"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleSaveBanner} className="p-6 space-y-4 overflow-y-auto">
              {/* Presets Button when creating */}
              {!editingBanner && (
                <div className="p-3 bg-slate-50 rounded-xl border border-slate-100 space-y-2">
                  <span className="text-[11px] font-semibold text-slate-600 block">
                    Gợi ý mẫu có sẵn (bấm để điền nhanh):
                  </span>
                  <div className="flex flex-wrap gap-2">
                    {PRESET_BANNERS.map((preset, idx) => (
                      <button
                        key={idx}
                        type="button"
                        onClick={() => {
                          setBannerTitle(preset.title)
                          setBannerSubtitle(preset.subtitle)
                          setBannerImageKey(preset.imageKey)
                          setBannerLinkUrl(preset.linkUrl)
                        }}
                        className="px-2.5 py-1 text-[11px] bg-white border border-slate-200 hover:border-leaf hover:text-leaf rounded-lg font-medium transition-colors"
                      >
                        {preset.title.split(' ')[0]} {preset.title.split(' ')[1]}
                      </button>
                    ))}
                  </div>
                </div>
              )}

              <div>
                <label className="text-xs font-semibold text-slate-700 block mb-1">
                  Tiêu đề Banner <span className="text-rose-500">*</span>:
                </label>
                <input
                  type="text"
                  required
                  value={bannerTitle}
                  onChange={(e) => setBannerTitle(e.target.value)}
                  placeholder="Ví dụ: Nông Sản Tươi Ngon Chuẩn VietGAP"
                  className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 outline-none focus:border-leaf focus:ring-1 focus:ring-leaf"
                />
              </div>

              <div>
                <label className="text-xs font-semibold text-slate-700 block mb-1">
                  Phụ đề mô tả:
                </label>
                <textarea
                  rows={2}
                  value={bannerSubtitle}
                  onChange={(e) => setBannerSubtitle(e.target.value)}
                  placeholder="Ví dụ: Giao nhanh 2h tại Hà Nội & TP.HCM. Miễn phí ship từ 299.000đ"
                  className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 outline-none focus:border-leaf focus:ring-1 focus:ring-leaf leading-relaxed"
                />
              </div>

              {/* Image Input & Upload */}
              <div className="space-y-2">
                <label className="text-xs font-semibold text-slate-700 block">
                  Hình ảnh Banner <span className="text-rose-500">*</span>:
                </label>
                <div className="flex items-center gap-2">
                  <input
                    type="text"
                    required
                    value={bannerImageKey}
                    onChange={(e) => setBannerImageKey(e.target.value)}
                    placeholder="URL ảnh hoặc tải file lên từ máy tính"
                    className="flex-1 px-3.5 py-2 text-xs rounded-xl border border-slate-200 outline-none focus:border-leaf focus:ring-1 focus:ring-leaf font-mono text-[11px]"
                  />
                  <label className="px-3 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-xl text-xs font-medium cursor-pointer flex items-center gap-1.5 shrink-0 transition-colors">
                    <Upload className="w-3.5 h-3.5" />
                    <span>{uploadingImage ? 'Đang tải...' : 'Tải file'}</span>
                    <input
                      type="file"
                      accept="image/*"
                      className="hidden"
                      onChange={handleFileUpload}
                      disabled={uploadingImage}
                    />
                  </label>
                </div>

                {bannerImageKey && (
                  <div className="relative aspect-[21/9] rounded-xl overflow-hidden border border-slate-200 bg-slate-50 mt-2">
                    <img
                      src={bannerImageKey}
                      alt="Xem trước banner"
                      className="w-full h-full object-cover"
                      onError={(e) => {
                        ;(e.target as HTMLElement).style.display = 'none'
                      }}
                    />
                  </div>
                )}
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs font-semibold text-slate-700 block mb-1">
                    Đường dẫn liên kết (khi bấm vào banner):
                  </label>
                  <input
                    type="text"
                    value={bannerLinkUrl}
                    onChange={(e) => setBannerLinkUrl(e.target.value)}
                    placeholder="/c/rau-cu-qua"
                    className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 outline-none focus:border-leaf focus:ring-1 focus:ring-leaf"
                  />
                </div>

                <div>
                  <label className="text-xs font-semibold text-slate-700 block mb-1">
                    Vị trí hiển thị:
                  </label>
                  <select
                    value={bannerPlacement}
                    onChange={(e) => setBannerPlacement(e.target.value)}
                    className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 outline-none focus:border-leaf focus:ring-1 focus:ring-leaf bg-white"
                  >
                    <option value="HomeHero">HomeHero (Đầu trang chủ)</option>
                    <option value="HomeMid">HomeMid (Giữa trang chủ)</option>
                    <option value="AisleTop">AisleTop (Đầu danh mục)</option>
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3 pt-1">
                <div>
                  <label className="text-xs font-semibold text-slate-700 block mb-1">
                    Thứ tự hiển thị (Sort Order):
                  </label>
                  <input
                    type="number"
                    min="0"
                    value={bannerSortOrder}
                    onChange={(e) => setBannerSortOrder(Number(e.target.value))}
                    className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 outline-none focus:border-leaf focus:ring-1 focus:ring-leaf"
                  />
                </div>

                <div className="flex items-center gap-2 pt-6">
                  <input
                    type="checkbox"
                    id="isActiveCheck"
                    checked={bannerIsActive}
                    onChange={(e) => setBannerIsActive(e.target.checked)}
                    className="w-4 h-4 text-leaf rounded border-slate-300 focus:ring-leaf cursor-pointer"
                  />
                  <label
                    htmlFor="isActiveCheck"
                    className="text-xs font-semibold text-slate-700 cursor-pointer select-none"
                  >
                    Kích hoạt hiển thị ngay
                  </label>
                </div>
              </div>

              <div className="pt-4 border-t border-slate-100 flex items-center justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="px-4 py-2 rounded-xl text-xs font-semibold text-slate-600 hover:bg-slate-100 transition-colors"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={isSaving}
                  className="px-5 py-2 rounded-xl bg-leaf text-white text-xs font-bold hover:bg-leaf/90 transition-colors shadow-2xs"
                >
                  {isSaving ? 'Đang lưu...' : editingBanner ? 'Cập nhật' : 'Tạo mới'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
