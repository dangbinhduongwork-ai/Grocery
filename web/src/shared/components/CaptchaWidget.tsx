import React, { useEffect, useRef, useState, useCallback } from 'react'
import { RefreshCw, Check, ShieldCheck } from 'lucide-react'

interface CaptchaWidgetProps {
  value: string
  onChange: (val: string) => void
  onValidate?: (isValid: boolean) => void
  error?: string | null
  className?: string
}

const CHARS = '23456789ABCDEFGHJKLMNPQRSTUVWXYZ'

function generateCode(length = 4): string {
  let result = ''
  for (let i = 0; i < length; i++) {
    result += CHARS.charAt(Math.floor(Math.random() * CHARS.length))
  }
  return result
}

export const CaptchaWidget: React.FC<CaptchaWidgetProps> = ({
  value,
  onChange,
  onValidate,
  error,
  className = '',
}) => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null)
  const [captchaCode, setCaptchaCode] = useState<string>('')

  const refreshCaptcha = useCallback(() => {
    const newCode = generateCode(4)
    setCaptchaCode(newCode)
    onChange('')
  }, [onChange])

  useEffect(() => {
    refreshCaptcha()
  }, [refreshCaptcha])

  // Draw captcha onto canvas with noise
  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas || !captchaCode) return

    const ctx = canvas.getContext('2d')
    if (!ctx) return

    const width = canvas.width
    const height = canvas.height

    // Background gradient
    const bgGrad = ctx.createLinearGradient(0, 0, width, height)
    bgGrad.addColorStop(0, '#f0fdf4') // Very light leaf
    bgGrad.addColorStop(1, '#fefce8') // Very light yellow
    ctx.fillStyle = bgGrad
    ctx.fillRect(0, 0, width, height)

    // Random noise lines
    for (let i = 0; i < 4; i++) {
      ctx.strokeStyle = `rgba(${Math.floor(Math.random() * 150)}, ${Math.floor(
        Math.random() * 180
      )}, ${Math.floor(Math.random() * 150)}, 0.35)`
      ctx.lineWidth = 1 + Math.random() * 1.5
      ctx.beginPath()
      ctx.moveTo(Math.random() * width, Math.random() * height)
      ctx.bezierCurveTo(
        Math.random() * width,
        Math.random() * height,
        Math.random() * width,
        Math.random() * height,
        Math.random() * width,
        Math.random() * height
      )
      ctx.stroke()
    }

    // Random dots
    for (let i = 0; i < 30; i++) {
      ctx.fillStyle = `rgba(${Math.floor(Math.random() * 180)}, ${Math.floor(
        Math.random() * 180
      )}, ${Math.floor(Math.random() * 180)}, 0.4)`
      ctx.beginPath()
      ctx.arc(Math.random() * width, Math.random() * height, Math.random() * 1.5, 0, Math.PI * 2)
      ctx.fill()
    }

    // Draw characters with random rotation & offset
    const charSpacing = width / (captchaCode.length + 1)
    const fonts = ['bold 20px "Courier New", monospace', 'bold 22px Georgia, serif', 'bold 20px Arial, sans-serif']

    for (let i = 0; i < captchaCode.length; i++) {
      ctx.save()
      const char = captchaCode[i]
      const x = charSpacing * (i + 0.8) + (Math.random() * 4 - 2)
      const y = height / 2 + 7 + (Math.random() * 4 - 2)
      const angle = (Math.random() * 30 - 15) * (Math.PI / 180)

      ctx.translate(x, y)
      ctx.rotate(angle)
      ctx.font = fonts[i % fonts.length]
      // Leaf green or dark slate color for contrast
      ctx.fillStyle = ['#166534', '#1e293b', '#0f766e', '#334155'][i % 4]
      ctx.fillText(char, 0, 0)
      ctx.restore()
    }
  }, [captchaCode])

  const isMatched = value.trim().toUpperCase() === captchaCode.toUpperCase() && captchaCode.length > 0

  useEffect(() => {
    onValidate?.(isMatched)
  }, [isMatched, onValidate])

  return (
    <div className={`space-y-1.5 ${className}`}>
      <label className="text-xs font-semibold text-ink flex items-center justify-between">
        <span className="flex items-center gap-1.5">
          <ShieldCheck className="w-3.5 h-3.5 text-leaf" strokeWidth={1.5} />
          Mã xác thực bảo mật (Captcha) <span className="text-tomato">*</span>
        </span>
        {isMatched && (
          <span className="text-[11px] text-leaf font-medium flex items-center gap-1">
            <Check className="w-3 h-3 stroke-[3]" /> Đã xác thực
          </span>
        )}
      </label>

      <div className="flex items-center gap-2">
        {/* Captcha Canvas Image */}
        <div className="relative border border-rule rounded-xl overflow-hidden shadow-2xs shrink-0 select-none">
          <canvas
            ref={canvasRef}
            width={120}
            height={40}
            className="block"
            title="Mã xác thực"
          />
        </div>

        {/* Refresh button */}
        <button
          type="button"
          onClick={refreshCaptcha}
          className="p-2.5 rounded-xl border border-rule text-ink-2 hover:text-ink hover:bg-paper-2 transition-colors shrink-0"
          title="Đổi mã xác thực khác"
        >
          <RefreshCw className="w-4 h-4" strokeWidth={1.5} />
        </button>

        {/* User Input */}
        <div className="relative flex-1">
          <input
            type="text"
            required
            maxLength={4}
            value={value}
            onChange={(e) => onChange(e.target.value.toUpperCase())}
            placeholder="Nhập 4 ký tự"
            className="w-full h-10 px-3 uppercase font-mono tracking-widest text-xs bg-paper border border-rule rounded-xl focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
            autoComplete="off"
          />
        </div>
      </div>

      {error && (
        <p className="text-[11px] text-tomato font-medium">{error}</p>
      )}
    </div>
  )
}

