import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setAccessToken, getAccessToken, requestSilentRefresh, setOnAuthFailure } from '@/shared/api/client'

describe('Auth Client Security Invariant', () => {
  beforeEach(() => {
    setAccessToken(null)
    if (typeof localStorage !== 'undefined') {
      localStorage.clear()
    }
    if (typeof sessionStorage !== 'undefined') {
      sessionStorage.clear()
    }
    vi.restoreAllMocks()
  })

  it('stores access token in memory only', () => {
    const fakeToken = 'header.payload.signature-test-123'
    setAccessToken(fakeToken)

    expect(getAccessToken()).toBe(fakeToken)

    // Security invariant: must NEVER write JWT tokens into localStorage or sessionStorage
    if (typeof localStorage !== 'undefined') {
      expect(localStorage.getItem('token')).toBeNull()
      expect(localStorage.getItem('accessToken')).toBeNull()
      expect(localStorage.getItem('jwt')).toBeNull()
    }
    if (typeof sessionStorage !== 'undefined') {
      expect(sessionStorage.getItem('token')).toBeNull()
      expect(sessionStorage.getItem('accessToken')).toBeNull()
    }
  })

  it('clears in-memory token on logout', () => {
    setAccessToken('some-valid-token')
    expect(getAccessToken()).not.toBeNull()

    setAccessToken(null)
    expect(getAccessToken()).toBeNull()
  })

  it('requestSilentRefresh sends credentials and X-Requested-With and restores session on page load', async () => {
    const mockUser = {
      id: '00000000-0000-0000-0000-000000000001',
      email: 'admin@groceryhub.vn',
      fullName: 'Quản trị viên',
      phone: '0901234567',
      roles: ['Admin'],
      permissions: ['*'],
    }

    let capturedHeaders: Record<string, string> = {}
    let capturedCredentials = ''

    globalThis.fetch = vi.fn().mockImplementation(async (url: string, init?: RequestInit) => {
      if (url === '/api/v1/auth/refresh') {
        capturedCredentials = init?.credentials as string
        capturedHeaders = init?.headers as Record<string, string>
        return {
          ok: true,
          json: async () => ({
            accessToken: 'new-refreshed-jwt-token',
            expiresIn: 900,
            user: mockUser,
          }),
        }
      }
      return { ok: false, status: 404 }
    })

    const result = await requestSilentRefresh()

    expect(result).not.toBeNull()
    expect(result?.accessToken).toBe('new-refreshed-jwt-token')
    expect(result?.user?.email).toBe('admin@groceryhub.vn')
    expect(getAccessToken()).toBe('new-refreshed-jwt-token')
    expect(capturedCredentials).toBe('include')
    expect(capturedHeaders['X-Requested-With']).toBe('XMLHttpRequest')
  })

  it('requestSilentRefresh triggers onAuthFailure and clears token when cookie is invalid/expired', async () => {
    let failureTriggered = false
    setOnAuthFailure(() => {
      failureTriggered = true
    })
    setAccessToken('old-stale-token')

    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
      json: async () => ({ detail: 'Refresh token cookie is missing.' }),
    })

    const result = await requestSilentRefresh()

    expect(result).toBeNull()
    expect(getAccessToken()).toBeNull()
    expect(failureTriggered).toBe(true)
  })

  it('deduplicates simultaneous silent refresh calls', async () => {
    let callCount = 0
    globalThis.fetch = vi.fn().mockImplementation(async () => {
      callCount++
      // simulate network latency
      await new Promise((resolve) => setTimeout(resolve, 20))
      return {
        ok: true,
        json: async () => ({
          accessToken: 'single-flight-token',
          expiresIn: 900,
          user: { id: '1', email: 'test@groceryhub.vn', fullName: 'Test', phone: null, roles: [], permissions: [] },
        }),
      }
    })

    const [res1, res2, res3] = await Promise.all([
      requestSilentRefresh(),
      requestSilentRefresh(),
      requestSilentRefresh(),
    ])

    expect(callCount).toBe(1)
    expect(res1?.accessToken).toBe('single-flight-token')
    expect(res2?.accessToken).toBe('single-flight-token')
    expect(res3?.accessToken).toBe('single-flight-token')
  })
})
