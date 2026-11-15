import createClient, { type Middleware } from 'openapi-fetch'
import type { paths } from './schema'

let inMemoryAccessToken: string | null = null
let refreshPromise: Promise<string | null> | null = null
let onAuthFailureCallback: (() => void) | null = null

export function getAccessToken(): string | null {
  return inMemoryAccessToken
}

export function setAccessToken(token: string | null): void {
  inMemoryAccessToken = token
}

export function setOnAuthFailure(callback: () => void): void {
  onAuthFailureCallback = callback
}

async function requestSilentRefresh(): Promise<string | null> {
  try {
    const response = await fetch('/api/v1/auth/refresh', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include', // sends httpOnly refresh token cookie
    })

    if (!response.ok) {
      setAccessToken(null)
      onAuthFailureCallback?.()
      return null
    }

    const data = await response.json() as { accessToken?: string }
    const newToken = data.accessToken ?? null
    setAccessToken(newToken)
    return newToken
  } catch {
    setAccessToken(null)
    onAuthFailureCallback?.()
    return null
  } finally {
    refreshPromise = null
  }
}

const authMiddleware: Middleware = {
  async onRequest({ request }) {
    if (inMemoryAccessToken) {
      request.headers.set('Authorization', `Bearer ${inMemoryAccessToken}`)
    }
    return request
  },
  async onResponse({ request, response }) {
    if (response.status === 401) {
      // Do not attempt refresh on auth endpoints themselves
      const url = new URL(request.url)
      if (url.pathname.includes('/auth/login') ||
          url.pathname.includes('/auth/register') ||
          url.pathname.includes('/auth/refresh-token')) {
        return response
      }

      // Deduplicate simultaneous refresh requests
      if (!refreshPromise) {
        refreshPromise = requestSilentRefresh()
      }
      const newToken = await refreshPromise

      if (newToken) {
        const retryHeaders = new Headers(request.headers)
        retryHeaders.set('Authorization', `Bearer ${newToken}`)

        return fetch(request.url, {
          method: request.method,
          headers: retryHeaders,
          body: request.body,
          credentials: 'include',
        })
      }
    }
    return response
  },
}

export const api = createClient<paths>({
  baseUrl: '',
})

api.use(authMiddleware)
