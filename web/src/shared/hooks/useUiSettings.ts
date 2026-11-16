import { useState, useEffect, useCallback } from 'react'
import { api } from '@/shared/api/client'

export interface UiDesignSettings {
  announcements: string[]
  heroBadge: string
  heroTitle: string
  heroSubtitle: string
}

export const DEFAULT_UI_SETTINGS: UiDesignSettings = {
  announcements: [
    'Giao hàng hỏa tốc trong 2 giờ tại nội thành',
    '100% Nông sản tươi sạch chuẩn VietGAP, rõ xuất xứ',
    'Miễn phí vận chuyển cho đơn hàng từ 300.000 ₫',
    'Cam kết đổi trả trong 24 giờ nếu hàng không tươi ngon',
    'Hotline / Zalo tư vấn: 0865 087 784 (7:00 - 22:00)',
  ],
  heroBadge: 'Nông sản tươi sạch mỗi ngày',
  heroTitle: 'Nông Sản Tươi Ngon Chuẩn VietGAP',
  heroSubtitle: 'Hệ thống cung cấp thực phẩm an toàn, rõ nguồn gốc cho bữa ăn gia đình tràn đầy sức khỏe.',
}

const STORAGE_KEY = 'groceryhub_ui_settings'
export const UI_SETTINGS_EVENT = 'groceryhub:ui-settings-updated'

export function useUiSettings() {
  const [settings, setSettings] = useState<UiDesignSettings>(() => {
    try {
      const cached = localStorage.getItem(STORAGE_KEY)
      if (cached) {
        return { ...DEFAULT_UI_SETTINGS, ...JSON.parse(cached) }
      }
    } catch {
      // ignore JSON parse error
    }
    return DEFAULT_UI_SETTINGS
  })
  const [loading, setLoading] = useState(false)

  const loadSettings = useCallback(async () => {
    try {
      setLoading(true)
      const res = await (api as any).GET('/api/v1/settings/ui')
      if (res && res.data) {
        const remoteData = res.data as Partial<UiDesignSettings>
        const merged: UiDesignSettings = {
          announcements:
            Array.isArray(remoteData.announcements) && remoteData.announcements.length > 0
              ? remoteData.announcements
              : DEFAULT_UI_SETTINGS.announcements,
          heroBadge: remoteData.heroBadge || DEFAULT_UI_SETTINGS.heroBadge,
          heroTitle: remoteData.heroTitle || DEFAULT_UI_SETTINGS.heroTitle,
          heroSubtitle: remoteData.heroSubtitle || DEFAULT_UI_SETTINGS.heroSubtitle,
        }
        setSettings(merged)
        localStorage.setItem(STORAGE_KEY, JSON.stringify(merged))
      }
    } catch (err) {
      console.warn('Failed to load UI settings from backend, using cache or defaults:', err)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadSettings()

    const handleUpdate = (e: Event) => {
      const customEvent = e as CustomEvent<UiDesignSettings>
      if (customEvent.detail) {
        setSettings(customEvent.detail)
      } else {
        void loadSettings()
      }
    }

    window.addEventListener(UI_SETTINGS_EVENT, handleUpdate)
    return () => {
      window.removeEventListener(UI_SETTINGS_EVENT, handleUpdate)
    }
  }, [loadSettings])

  const saveSettings = async (newSettings: UiDesignSettings): Promise<{ success: boolean; error?: string }> => {
    try {
      setSettings(newSettings)
      localStorage.setItem(STORAGE_KEY, JSON.stringify(newSettings))

      // Broadcast immediately across all components in the current window
      window.dispatchEvent(new CustomEvent(UI_SETTINGS_EVENT, { detail: newSettings }))

      // Persist to backend
      const res = await (api as any).PUT('/api/v1/admin/settings/ui', {
        body: newSettings,
      })

      if (res && res.error) {
        return { success: false, error: res.error.detail || 'Không thể lưu cài đặt giao diện' }
      }

      return { success: true }
    } catch (err: any) {
      return { success: false, error: err?.message || 'Lỗi kết nối khi lưu' }
    }
  }

  return {
    settings,
    loading,
    refresh: loadSettings,
    saveSettings,
  }
}
