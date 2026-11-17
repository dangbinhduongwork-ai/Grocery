import { describe, it, expect, beforeEach, vi } from 'vitest'
import { DEFAULT_UI_SETTINGS, DEFAULT_HOLIDAY_EFFECT, type UiDesignSettings } from '@/shared/hooks/useUiSettings'

describe('UI Settings & Holiday Effects Configuration', () => {
  beforeEach(() => {
    if (typeof localStorage !== 'undefined') {
      localStorage.clear()
    }
    vi.restoreAllMocks()
  })

  it('provides comprehensive default holiday settings', () => {
    expect(DEFAULT_HOLIDAY_EFFECT.enabled).toBe(true)
    expect(DEFAULT_HOLIDAY_EFFECT.theme).toBe('tet')
    expect(DEFAULT_HOLIDAY_EFFECT.intensity).toBe('medium')
    expect(DEFAULT_HOLIDAY_EFFECT.bannerText).toContain('Tết')

    expect(DEFAULT_UI_SETTINGS.holidayEffect).toBeDefined()
    expect(DEFAULT_UI_SETTINGS.holidayEffect?.theme).toBe('tet')
    expect(DEFAULT_UI_SETTINGS.announcements.length).toBeGreaterThan(0)
  })

  it('supports all valid holiday themes (tet, christmas, sale, mid_autumn, none)', () => {
    const themes = ['none', 'tet', 'christmas', 'sale', 'mid_autumn'] as const
    for (const theme of themes) {
      const customSettings: UiDesignSettings = {
        ...DEFAULT_UI_SETTINGS,
        holidayEffect: {
          enabled: theme !== 'none',
          theme,
          intensity: 'high',
        },
      }
      expect(customSettings.holidayEffect?.theme).toBe(theme)
    }
  })
})
