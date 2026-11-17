import React, { useEffect, useRef } from 'react'
import { useUiSettings, type HolidayTheme, type HolidayIntensity } from '@/shared/hooks/useUiSettings'

interface Particle {
  x: number
  y: number
  size: number
  speedX: number
  speedY: number
  angle: number
  angleSpeed: number
  color: string
  shapeType: number
  opacity: number
}

const TET_COLORS = ['#FBBF24', '#F59E0B', '#F472B6', '#FB7185', '#E11D48']
const XMAS_COLORS = ['#FFFFFF', '#E0F2FE', '#BAE6FD', '#F1F5F9']
const SALE_COLORS = ['#EF4444', '#F59E0B', '#10B981', '#3B82F6', '#8B5CF6', '#EC4899']
const AUTUMN_COLORS = ['#D97706', '#EA580C', '#B45309', '#F59E0B', '#9A3412']

function getParticleCount(intensity: HolidayIntensity): number {
  switch (intensity) {
    case 'low':
      return 18
    case 'high':
      return 55
    case 'medium':
    default:
      return 35
  }
}

export const HolidayEffectOverlay: React.FC<{ forceTheme?: HolidayTheme; forceIntensity?: HolidayIntensity }> = ({
  forceTheme,
  forceIntensity,
}) => {
  const { settings } = useUiSettings()
  const canvasRef = useRef<HTMLCanvasElement | null>(null)

  const holiday = settings.holidayEffect
  const enabled = forceTheme ? true : (holiday?.enabled ?? true)
  const theme: HolidayTheme = forceTheme || holiday?.theme || 'tet'
  const intensity: HolidayIntensity = forceIntensity || holiday?.intensity || 'medium'

  useEffect(() => {
    if (!enabled || theme === 'none') {
      return
    }

    // Check reduced motion preference
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      return
    }

    const canvas = canvasRef.current
    if (!canvas) return
    const ctx = canvas.getContext('2d')
    if (!ctx) return

    let animationFrameId: number
    let isRunning = true

    const updateCanvasSize = () => {
      if (!canvas) return
      canvas.width = window.innerWidth
      canvas.height = window.innerHeight
    }

    updateCanvasSize()
    window.addEventListener('resize', updateCanvasSize)

    // Palette selection
    let colorPalette: string[]
    if (theme === 'tet') colorPalette = TET_COLORS
    else if (theme === 'christmas') colorPalette = XMAS_COLORS
    else if (theme === 'sale') colorPalette = SALE_COLORS
    else colorPalette = AUTUMN_COLORS

    const count = getParticleCount(intensity)
    const particles: Particle[] = []

    for (let i = 0; i < count; i++) {
      particles.push({
        x: Math.random() * window.innerWidth,
        y: Math.random() * window.innerHeight,
        size: Math.random() * (theme === 'sale' ? 8 : 12) + (theme === 'christmas' ? 3 : 7),
        speedX: (Math.random() - 0.5) * 1.2 + (theme === 'christmas' ? 0.4 : 0),
        speedY: Math.random() * 1.2 + 0.8,
        angle: Math.random() * Math.PI * 2,
        angleSpeed: (Math.random() - 0.5) * 0.04,
        color: colorPalette[Math.floor(Math.random() * colorPalette.length)] || '#FBBF24',
        shapeType: Math.floor(Math.random() * 3),
        opacity: Math.random() * 0.4 + 0.6,
      })
    }

    // Draw functions for different festive shapes
    const drawFlower = (p: Particle) => {
      ctx.save()
      ctx.translate(p.x, p.y)
      ctx.rotate(p.angle)
      ctx.globalAlpha = p.opacity

      // Draw 5 flower petals (Vietnamese Hoa Mai / Hoa Đào)
      ctx.fillStyle = p.color
      const petalRadius = p.size * 0.45
      for (let j = 0; j < 5; j++) {
        ctx.save()
        ctx.rotate((j * Math.PI * 2) / 5)
        ctx.beginPath()
        ctx.ellipse(0, petalRadius, petalRadius * 0.7, petalRadius * 1.1, 0, 0, Math.PI * 2)
        ctx.fill()
        ctx.restore()
      }

      // Flower center pistil
      ctx.fillStyle = '#78350F'
      ctx.beginPath()
      ctx.arc(0, 0, petalRadius * 0.35, 0, Math.PI * 2)
      ctx.fill()

      ctx.restore()
    }

    const drawSnowflake = (p: Particle) => {
      ctx.save()
      ctx.translate(p.x, p.y)
      ctx.rotate(p.angle)
      ctx.globalAlpha = p.opacity

      // Soft circular snowflake with outer glow
      const grad = ctx.createRadialGradient(0, 0, 0, 0, 0, p.size)
      grad.addColorStop(0, 'rgba(255, 255, 255, 0.95)')
      grad.addColorStop(0.6, p.color)
      grad.addColorStop(1, 'rgba(255, 255, 255, 0)')

      ctx.fillStyle = grad
      ctx.beginPath()
      ctx.arc(0, 0, p.size, 0, Math.PI * 2)
      ctx.fill()

      ctx.restore()
    }

    const drawConfetti = (p: Particle) => {
      ctx.save()
      ctx.translate(p.x, p.y)
      ctx.rotate(p.angle)
      ctx.globalAlpha = p.opacity
      ctx.fillStyle = p.color

      if (p.shapeType === 0) {
        // Tumbling rectangular ribbon
        ctx.fillRect(-p.size / 2, -p.size, p.size, p.size * 1.8)
      } else if (p.shapeType === 1) {
        // Circle confetti dot
        ctx.beginPath()
        ctx.arc(0, 0, p.size * 0.6, 0, Math.PI * 2)
        ctx.fill()
      } else {
        // 4-point star sparkle
        ctx.beginPath()
        ctx.moveTo(0, -p.size)
        ctx.lineTo(p.size * 0.3, 0)
        ctx.lineTo(0, p.size)
        ctx.lineTo(-p.size * 0.3, 0)
        ctx.closePath()
        ctx.fill()
      }

      ctx.restore()
    }

    const drawAutumnLeaf = (p: Particle) => {
      ctx.save()
      ctx.translate(p.x, p.y)
      ctx.rotate(p.angle)
      ctx.globalAlpha = p.opacity
      ctx.fillStyle = p.color

      // Gently curved leaf
      ctx.beginPath()
      ctx.moveTo(0, -p.size)
      ctx.quadraticCurveTo(p.size * 0.8, 0, 0, p.size)
      ctx.quadraticCurveTo(-p.size * 0.8, 0, 0, -p.size)
      ctx.fill()

      ctx.restore()
    }

    const render = () => {
      if (!isRunning) return
      ctx.clearRect(0, 0, canvas.width, canvas.height)

      for (let i = 0; i < particles.length; i++) {
        const p = particles[i]
        if (!p) continue

        // Update position
        p.y += p.speedY
        p.x += Math.sin(p.angle * 2) * 0.8 + p.speedX
        p.angle += p.angleSpeed

        // Wrap around bottom/sides
        if (p.y > canvas.height + 20) {
          p.y = -20
          p.x = Math.random() * canvas.width
        }
        if (p.x > canvas.width + 20) {
          p.x = -20
        } else if (p.x < -20) {
          p.x = canvas.width + 20
        }

        // Render shape
        if (theme === 'tet') {
          drawFlower(p)
        } else if (theme === 'christmas') {
          drawSnowflake(p)
        } else if (theme === 'sale') {
          drawConfetti(p)
        } else if (theme === 'mid_autumn') {
          drawAutumnLeaf(p)
        }
      }

      animationFrameId = requestAnimationFrame(render)
    }

    const handleVisibilityChange = () => {
      if (document.hidden) {
        isRunning = false
        cancelAnimationFrame(animationFrameId)
      } else {
        isRunning = true
        animationFrameId = requestAnimationFrame(render)
      }
    }

    document.addEventListener('visibilitychange', handleVisibilityChange)
    animationFrameId = requestAnimationFrame(render)

    return () => {
      isRunning = false
      cancelAnimationFrame(animationFrameId)
      window.removeEventListener('resize', updateCanvasSize)
      document.removeEventListener('visibilitychange', handleVisibilityChange)
    }
  }, [enabled, theme, intensity])

  if (!enabled || theme === 'none') {
    return null
  }

  return (
    <canvas
      ref={canvasRef}
      className="pointer-events-none fixed inset-0 z-40 w-full h-full"
      aria-hidden="true"
    />
  )
}
