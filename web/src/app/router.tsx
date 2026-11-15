import React, { createContext, useContext, useEffect, useState, useCallback, useMemo } from 'react'

interface RouterContextValue {
  path: string
  pathname: string
  searchParams: URLSearchParams
  navigate: (to: string, options?: { replace?: boolean }) => void
}

const RouterContext = createContext<RouterContextValue | undefined>(undefined)

export const RouterProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [currentUrl, setCurrentUrl] = useState<string>(() => window.location.pathname + window.location.search)

  useEffect(() => {
    const handlePopState = () => {
      setCurrentUrl(window.location.pathname + window.location.search)
    }
    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [])

  const navigate = useCallback((to: string, options?: { replace?: boolean }) => {
    if (options?.replace) {
      window.history.replaceState({}, '', to)
    } else {
      window.history.pushState({}, '', to)
    }
    setCurrentUrl(to)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }, [])

  const { pathname, searchParams } = useMemo(() => {
    const [pathPart, queryPart] = currentUrl.split('?')
    return {
      pathname: pathPart || '/',
      searchParams: new URLSearchParams(queryPart || ''),
    }
  }, [currentUrl])

  const value = useMemo(
    () => ({
      path: currentUrl,
      pathname,
      searchParams,
      navigate,
    }),
    [currentUrl, pathname, searchParams, navigate],
  )

  return <RouterContext.Provider value={value}>{children}</RouterContext.Provider>
}

export function useRouter(): RouterContextValue {
  const context = useContext(RouterContext)
  if (!context) {
    throw new Error('useRouter must be used within a RouterProvider')
  }
  return context
}

export interface LinkProps extends React.AnchorHTMLAttributes<HTMLAnchorElement> {
  to: string
  replace?: boolean
}

export const Link: React.FC<LinkProps> = ({ to, replace, onClick, children, ...props }) => {
  const { navigate } = useRouter()

  const handleClick = (e: React.MouseEvent<HTMLAnchorElement>) => {
    if (onClick) onClick(e)
    if (!e.defaultPrevented && e.button === 0 && !e.metaKey && !e.ctrlKey && !e.altKey && !e.shiftKey) {
      e.preventDefault()
      navigate(to, { replace })
    }
  }

  return (
    <a href={to} onClick={handleClick} {...props}>
      {children}
    </a>
  )
}

export function matchPath(pattern: string, pathname: string): { matched: boolean; params: Record<string, string> } {
  const patternParts = pattern.split('/').filter(Boolean)
  const pathParts = pathname.split('/').filter(Boolean)

  if (patternParts.length !== pathParts.length) {
    return { matched: false, params: {} }
  }

  const params: Record<string, string> = {}
  for (let i = 0; i < patternParts.length; i++) {
    const patternPart = patternParts[i]
    const pathPart = pathParts[i]
    if (!patternPart || !pathPart) return { matched: false, params: {} }

    if (patternPart.startsWith(':')) {
      const paramName = patternPart.slice(1)
      params[paramName] = decodeURIComponent(pathPart)
    } else if (patternPart !== pathPart) {
      return { matched: false, params: {} }
    }
  }

  return { matched: true, params }
}
