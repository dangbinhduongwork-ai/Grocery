import createClient, { type Middleware } from 'openapi-fetch'
import type { paths, components } from './schema'

export type AuthUser = components['schemas']['AuthUserDto']
export type AuthTokenResponse = components['schemas']['AuthTokenResponse']

let inMemoryAccessToken: string | null = null
let refreshPromise: Promise<AuthTokenResponse | null> | null = null
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

export async function requestSilentRefresh(): Promise<AuthTokenResponse | null> {
  if (refreshPromise) {
    return refreshPromise
  }

  refreshPromise = (async () => {
    try {
      const response = await fetch('/api/v1/auth/refresh', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Requested-With': 'XMLHttpRequest',
        },
        credentials: 'include', // sends httpOnly refresh token cookie
      })

      if (!response.ok) {
        setAccessToken(null)
        onAuthFailureCallback?.()
        return null
      }

      const data = (await response.json()) as AuthTokenResponse
      const newToken = data.accessToken ?? null
      setAccessToken(newToken)
      return data
    } catch {
      setAccessToken(null)
      onAuthFailureCallback?.()
      return null
    } finally {
      refreshPromise = null
    }
  })()

  return refreshPromise
}

const authMiddleware: Middleware = {
  async onRequest({ request }) {
    if (inMemoryAccessToken) {
      request.headers.set('Authorization', `Bearer ${inMemoryAccessToken}`)
    }
    request.headers.set('X-Requested-With', 'XMLHttpRequest')
    return request
  },
  async onResponse({ request, response }) {
    if (response.status === 401) {
      // Do not attempt refresh on auth endpoints themselves
      const url = new URL(request.url)
      if (
        url.pathname.includes('/auth/login') ||
        url.pathname.includes('/auth/register') ||
        url.pathname.includes('/auth/refresh')
      ) {
        return response
      }

      const refreshResult = await requestSilentRefresh()

      if (refreshResult?.accessToken) {
        const retryHeaders = new Headers(request.headers)
        retryHeaders.set('Authorization', `Bearer ${refreshResult.accessToken}`)

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
  credentials: 'include',
  headers: {
    'X-Requested-With': 'XMLHttpRequest',
  },
})

api.use(authMiddleware)
