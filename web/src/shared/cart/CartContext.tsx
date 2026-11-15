import React, { createContext, useContext, useEffect, useState, useCallback } from 'react'
import { api } from '@/shared/api/client'
import type { CartData } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'

interface CartContextValue {
  cart: CartData | null
  itemCount: number
  isLoading: boolean
  addToCart: (productId: string, quantity?: number) => Promise<{ success: boolean; message?: string }>
  updateQuantity: (itemId: string, quantity: number) => Promise<boolean>
  removeItem: (itemId: string) => Promise<boolean>
  applyCoupon: (code: string) => Promise<{ success: boolean; message?: string }>
  removeCoupon: () => Promise<boolean>
  refreshCart: () => Promise<void>
}

const CartContext = createContext<CartContextValue | undefined>(undefined)

export const CartProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [cart, setCart] = useState<CartData | null>(null)
  const [isLoading, setIsLoading] = useState<boolean>(true)
  const { isAuthenticated } = useAuth()

  const refreshCart = useCallback(async (): Promise<void> => {
    try {
      const { data, error } = await api.GET('/api/v1/cart')
      if (data && !error) {
        setCart(data)
      } else {
        setCart(null)
      }
    } catch {
      setCart(null)
    } finally {
      setIsLoading(false)
    }
  }, [])

  // Refresh cart on initial load and when auth state changes (guest <-> user merge)
  useEffect(() => {
    void refreshCart()
  }, [isAuthenticated, refreshCart])

  const addToCart = async (
    productId: string,
    quantity = 1
  ): Promise<{ success: boolean; message?: string }> => {
    try {
      const { data, error } = await api.POST('/api/v1/cart/items', {
        body: { productId, quantity },
      })

      if (error || !data) {
        const errorDetail = (error as { detail?: string })?.detail ?? 'Không thể thêm vào giỏ hàng.'
        return { success: false, message: errorDetail }
      }

      setCart(data)
      return { success: true }
    } catch (err) {
      return { success: false, message: err instanceof Error ? err.message : 'Lỗi kết nối.' }
    }
  }

  const updateQuantity = async (itemId: string, quantity: number): Promise<boolean> => {
    try {
      const { data, error } = await api.PUT('/api/v1/cart/items/{id}', {
        params: { path: { id: itemId } },
        body: { quantity },
      })

      if (error || !data) {
        return false
      }

      setCart(data)
      return true
    } catch {
      return false
    }
  }

  const removeItem = async (itemId: string): Promise<boolean> => {
    try {
      const { data, error } = await api.DELETE('/api/v1/cart/items/{id}', {
        params: { path: { id: itemId } },
      })

      if (error || !data) {
        return false
      }

      setCart(data)
      return true
    } catch {
      return false
    }
  }

  const applyCoupon = async (
    code: string
  ): Promise<{ success: boolean; message?: string }> => {
    try {
      const { data, error } = await api.POST('/api/v1/cart/coupon', {
        body: { code },
      })

      if (error || !data) {
        const errorDetail = (error as { detail?: string })?.detail ?? 'Mã giảm giá không hợp lệ.'
        return { success: false, message: errorDetail }
      }

      setCart(data)
      return { success: true }
    } catch (err) {
      return { success: false, message: err instanceof Error ? err.message : 'Lỗi kết nối.' }
    }
  }

  const removeCoupon = async (): Promise<boolean> => {
    try {
      const { data, error } = await api.DELETE('/api/v1/cart/coupon')
      if (error || !data) {
        return false
      }
      setCart(data)
      return true
    } catch {
      return false
    }
  }

  const itemCount = cart?.items.reduce((sum, item) => sum + Number(item.quantity), 0) ?? 0

  const value: CartContextValue = {
    cart,
    itemCount,
    isLoading,
    addToCart,
    updateQuantity,
    removeItem,
    applyCoupon,
    removeCoupon,
    refreshCart,
  }

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>
}

export function useCart(): CartContextValue {
  const context = useContext(CartContext)
  if (!context) {
    throw new Error('useCart must be used within a CartProvider')
  }
  return context
}
