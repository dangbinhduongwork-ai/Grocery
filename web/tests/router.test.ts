import { describe, it, expect } from 'vitest'
import { matchPath } from '@/app/router'

describe('Router matchPath', () => {
  it('matches exact root path', () => {
    const res = matchPath('/', '/')
    expect(res.matched).toBe(true)
    expect(res.params).toEqual({})
  })

  it('matches parameterized category path', () => {
    const res = matchPath('/c/:slug', '/c/rau-cu-qua')
    expect(res.matched).toBe(true)
    expect(res.params.slug).toBe('rau-cu-qua')
  })

  it('matches parameterized product path', () => {
    const res = matchPath('/p/:slug', '/p/ca-chua-beef-dalat')
    expect(res.matched).toBe(true)
    expect(res.params.slug).toBe('ca-chua-beef-dalat')
  })

  it('matches order confirmation with nested segments', () => {
    const res = matchPath('/orders/:id/confirmed', '/orders/98f12a83-bf9c-4f10-b98d-a4e9dc012ef3/confirmed')
    expect(res.matched).toBe(true)
    expect(res.params.id).toBe('98f12a83-bf9c-4f10-b98d-a4e9dc012ef3')
  })

  it('returns false when segment counts do not match', () => {
    const res = matchPath('/c/:slug', '/c/rau-cu-qua/sub-item')
    expect(res.matched).toBe(false)
  })

  it('returns false when static segments do not match', () => {
    const res = matchPath('/orders/:id', '/products/123')
    expect(res.matched).toBe(false)
  })
})
