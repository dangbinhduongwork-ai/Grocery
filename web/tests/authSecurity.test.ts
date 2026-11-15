import { describe, it, expect, beforeEach } from 'vitest'
import { setAccessToken, getAccessToken } from '@/shared/api/client'

describe('Auth Client Security Invariant', () => {
  beforeEach(() => {
    setAccessToken(null)
    if (typeof localStorage !== 'undefined') {
      localStorage.clear()
    }
    if (typeof sessionStorage !== 'undefined') {
      sessionStorage.clear()
    }
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
})
