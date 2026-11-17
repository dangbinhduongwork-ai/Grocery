import React, { useState, useEffect, useRef } from 'react'
import {
  X,
  Send,
  Sparkles,
  Bot,
  User,
  Settings,
  RotateCcw,
  Lock,
  LogIn,
  Sliders,
  Eye,
  EyeOff,
} from 'lucide-react'
import { useAuth } from '@/shared/auth/AuthContext'
import { getAccessToken } from '@/shared/api/client'
import { useRouter } from '@/app/router'

function FormattedMessage({ text, onNavigate }: { text: string; onNavigate: (url: string) => void }) {
  const lines = text.split('\n')

  return (
    <div className="space-y-1">
      {lines.map((line, lIdx) => {
        if (!line.trim()) {
          return <div key={lIdx} className="h-1.5" />
        }

        const parts: React.ReactNode[] = []
        const regex = /(\[([^\]]+)\]\(([^)]+)\)|\*\*([^*]+)\*\*|`([^`]+)`)/g
        let lastIndex = 0
        let match: RegExpExecArray | null

        while ((match = regex.exec(line)) !== null) {
          if (match.index > lastIndex) {
            parts.push(line.substring(lastIndex, match.index))
          }

          if (match[2] && match[3]) {
            const linkText = match[2]
            const linkUrl = match[3]
            parts.push(
              <a
                key={match.index}
                href={linkUrl}
                onClick={(e) => {
                  e.preventDefault()
                  if (linkUrl.startsWith('/')) {
                    onNavigate(linkUrl)
                  } else {
                    window.open(linkUrl, '_blank', 'noopener,noreferrer')
                  }
                }}
                className="text-leaf underline font-semibold hover:opacity-80 inline-flex items-center gap-0.5 cursor-pointer"
              >
                {linkText}
              </a>
            )
          } else if (match[4]) {
            parts.push(
              <strong key={match.index} className="font-semibold text-ink">
                {match[4]}
              </strong>
            )
          } else if (match[5]) {
            parts.push(
              <code key={match.index} className="bg-paper px-1 py-0.2 rounded font-mono text-[10.5px]">
                {match[5]}
              </code>
            )
          }

          lastIndex = regex.lastIndex
        }

        if (lastIndex < line.length) {
          parts.push(line.substring(lastIndex))
        }

        return (
          <p key={lIdx} className="leading-relaxed">
            {parts}
          </p>
        )
      })}
    </div>
  )
}

interface ChatMessage {
  id: string
  sender: 'user' | 'bot'
  text: string
  timestamp: string
}

export interface AIConfig {
  provider: 'gemini' | 'openai' | 'custom'
  endpointUrl: string
  model: string
  apiKey: string
}

const DEFAULT_SUGGESTIONS = [
  'Gợi ý thực đơn tối nay cho gia đình',
  'Địa chỉ cửa hàng và chính sách giao hàng',
  'Cách thanh toán quét mã VietQR',
  'Rau củ tươi sạch VietGAP',
  'Hotline liên hệ chăm sóc khách hàng',
]

function getSmartFallbackReply(input: string): string {
  const lower = input.toLowerCase()

  if (lower.includes('thực đơn') || lower.includes('món ngon') || lower.includes('nấu gì') || lower.includes('ăn gì')) {
    return '**Gợi ý thực đơn hôm nay cho gia đình:**\n\n1. **Canh sườn hầm củ quả VietGAP** (cà rốt, khoai tây, bắp ngọt).\n2. **Cá basa áp chảo sốt cà chua** hoặc **Thịt ba chỉ luộc chấm mắm tỏi**.\n3. **Rau muống non xào tỏi** giòn ngọt.\n4. Tráng miệng: **Dưa hấu tươi Long An** mát lạnh.\n\nTất cả nguyên liệu tươi ngon đều đang có sẵn tại các quầy hàng của GroceryHub, giao hỏa tốc 2 giờ đến tận bếp nhà bạn!'
  }

  if (lower.includes('freeship') || lower.includes('vận chuyển') || lower.includes('giao hàng') || lower.includes('phí ship') || lower.includes('địa chỉ')) {
    return '**Chính sách giao hàng & Địa chỉ GroceryHub:**\n\n• **Địa chỉ cửa hàng:** Thôn 9, Xã Suối Hai, Huyện Ba Vì, Hà Nội.\n• **Phạm vi giao hàng:** Toàn quốc — giao hàng tận nơi tới bất kỳ địa chỉ nào trên toàn lãnh thổ Việt Nam!\n• **Giao siêu tốc 2 giờ** cho khu vực nội thành.\n• **Miễn phí vận chuyển (Freeship 100%)** cho đơn hàng từ **300.000 ₫**.\n• Nhân viên giao hàng có thùng giữ lạnh chuyên dụng, đảm bảo rau thịt luôn tươi rói!'
  }

  if (lower.includes('thanh toán') || lower.includes('vietqr') || lower.includes('sepay') || lower.includes('chuyển khoản') || lower.includes('cod')) {
    return '**Phương thức thanh toán tiện lợi:**\n\n1. **Chuyển khoản SePay VietQR (Khuyên dùng):** Quét mã QR Napas 24/7 qua bất kỳ app ngân hàng nào (MBBank, Vietcombank, Techcombank...). Hệ thống tự động xác nhận trong 3 giây không cần chụp biên lai!\n2. **Thanh toán tiền mặt khi nhận hàng (COD):** Bạn kiểm tra thực phẩm tươi ngon đúng yêu cầu rồi mới thanh toán cho shipper.'
  }

  if (lower.includes('liên hệ') || lower.includes('hotline') || lower.includes('sđt') || lower.includes('email') || lower.includes('hỗ trợ')) {
    return '**Thông tin hỗ trợ khách hàng GroceryHub:**\n\n• **Địa chỉ shop:** Thôn 9, Xã Suối Hai, Huyện Ba Vì, Hà Nội\n• **Hotline / Zalo:** 0865 087 784 (Hỗ trợ 24/7)\n• **Email:** dangbinhduong.work@gmail.com\n• **Thời gian phục vụ:** 7:00 - 22:00 tất cả các ngày trong tuần (kể cả Thứ 7, Chủ nhật và ngày Lễ).'
  }

  return 'Chào bạn! Em là Trợ lý AI của GroceryHub. Em có thể hỗ trợ bạn tìm kiếm thực phẩm sạch, gợi ý thực đơn món ngon mỗi ngày, kiểm tra tình trạng đơn hàng hoặc hướng dẫn đặt hàng. Bạn cần em tư vấn điều gì ạ?'
}

const AI_CONFIG_LOCAL_STORE = 'groceryhub_ai_config'

function getInitialAIConfig(): AIConfig {
  const envKey = (import.meta.env.VITE_AI_API_KEY as string) || (import.meta.env.VITE_GEMINI_API_KEY as string) || ''
  const isSkKey = envKey.startsWith('sk-')
  const envProvider = (import.meta.env.VITE_AI_PROVIDER as string) || 'openai'
  const envEndpoint = (import.meta.env.VITE_AI_ENDPOINT as string) || 'https://api.xingchiapi.com'
  const envModel = (import.meta.env.VITE_AI_MODEL as string) || 'gpt-6.1-sol'

  try {
    const saved = localStorage.getItem(AI_CONFIG_LOCAL_STORE)
    if (saved) {
      const parsed = JSON.parse(saved)
      const key = parsed.apiKey || envKey
      // Auto-migrate if key is OpenAI/XingChi sk- format
      if (key.startsWith('sk-') && (parsed.provider === 'gemini' || !parsed.endpointUrl || parsed.endpointUrl.includes('openai.com'))) {
        parsed.provider = 'openai'
        parsed.model = 'gpt-6.1-sol'
        parsed.endpointUrl = envEndpoint
      }
      return {
        provider: parsed.provider || (isSkKey ? 'openai' : envProvider),
        endpointUrl: parsed.endpointUrl || envEndpoint,
        model: parsed.model || envModel,
        apiKey: key,
      }
    }
  } catch {
    // Ignore parse error
  }

  return {
    provider: (isSkKey ? 'openai' : (envProvider as 'gemini' | 'openai' | 'custom')),
    endpointUrl: envEndpoint,
    model: envModel,
    apiKey: envKey,
  }
}

export const AIChatBot: React.FC = () => {
  const { navigate } = useRouter()
  const { isAuthenticated, isAdmin } = useAuth()

  const [isOpen, setIsOpen] = useState(false)
  const [isSettingsOpen, setIsSettingsOpen] = useState(false)
  const [showApiKey, setShowApiKey] = useState(false)

  // AI Configuration State
  const [aiConfig, setAiConfig] = useState<AIConfig>(getInitialAIConfig)
  const [tempConfig, setTempConfig] = useState<AIConfig>(aiConfig)

  const [inputMessage, setInputMessage] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [hasUnread, setHasUnread] = useState(false)

  const [messages, setMessages] = useState<ChatMessage[]>(() => {
    return [
      {
        id: 'welcome',
        sender: 'bot',
        text: 'Xin chào! Em là Trợ lý AI của GroceryHub (Thôn 9, Suối Hai, Ba Vì, Hà Nội). Em có thể giúp gì cho bạn hôm nay: gợi ý thực đơn món ngon, kiểm tra giao hàng toàn quốc hay tư vấn chọn thực phẩm sạch?',
        timestamp: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
      },
    ]
  })

  const messagesEndRef = useRef<HTMLDivElement | null>(null)

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }

  useEffect(() => {
    if (isOpen) {
      scrollToBottom()
      setHasUnread(false)
    }
  }, [isOpen, messages])

  // Sync tempConfig when isSettingsOpen opens
  useEffect(() => {
    if (isSettingsOpen) {
      setTempConfig(aiConfig)
    }
  }, [isSettingsOpen, aiConfig])

  const handleSaveConfig = (e: React.FormEvent) => {
    e.preventDefault()
    const cleanConfig: AIConfig = {
      provider: tempConfig.provider,
      endpointUrl: tempConfig.endpointUrl.trim(),
      model: tempConfig.model.trim(),
      apiKey: tempConfig.apiKey.trim(),
    }
    setAiConfig(cleanConfig)
    localStorage.setItem(AI_CONFIG_LOCAL_STORE, JSON.stringify(cleanConfig))
    setIsSettingsOpen(false)

    setMessages((prev) => [
      ...prev,
      {
        id: String(Date.now()),
        sender: 'bot',
        text: cleanConfig.apiKey
          ? `Đã lưu cấu hình AI (${cleanConfig.provider.toUpperCase()} - ${cleanConfig.model || 'Default'}). Trợ lý ảo sẵn sàng giải đáp và tư vấn thông minh cho khách hàng!`
          : 'Đã xóa API Key. Trợ lý ảo sẽ tiếp tục hoạt động với kịch bản trả lời tự động thông minh của GroceryHub.',
        timestamp: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
      },
    ])
  }

  const handleClearHistory = () => {
    setMessages([
      {
        id: 'welcome',
        sender: 'bot',
        text: 'Lịch sử trò chuyện đã được làm mới. Em sẵn sàng hỗ trợ bạn chọn thực phẩm tươi sạch hôm nay!',
        timestamp: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
      },
    ])
  }

  const handleSend = async (textToSend?: string) => {
    const text = (textToSend || inputMessage).trim()
    if (!text || isLoading) return

    // Require authentication
    if (!isAuthenticated) {
      return
    }

    const nowStr = new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
    const userMsg: ChatMessage = {
      id: String(Date.now()),
      sender: 'user',
      text,
      timestamp: nowStr,
    }

    setMessages((prev) => [...prev, userMsg])
    setInputMessage('')
    setIsLoading(true)

    // Call AI API via backend proxy (/api/v1/ai/chat)
    let apiErrorMsg = ''
    try {
      const token = getAccessToken()
      const response = await fetch('/api/v1/ai/chat', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        credentials: 'include',
        body: JSON.stringify({
          message: text,
          model: aiConfig.model || 'gpt-6.1-sol',
          endpoint: aiConfig.endpointUrl || 'https://api.xingchiapi.com',
          apiKey: aiConfig.apiKey,
          provider: aiConfig.provider,
        }),
      })

      if (response.ok) {
        const data = await response.json()
        if (data?.reply) {
          setMessages((prev) => [
            ...prev,
            {
              id: String(Date.now() + 1),
              sender: 'bot',
              text: data.reply,
              timestamp: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
            },
          ])
          setIsLoading(false)
          return
        }
      } else {
        try {
          const errData = await response.json()
          apiErrorMsg = errData?.error || `HTTP ${response.status} ${response.statusText}`
        } catch {
          apiErrorMsg = `HTTP ${response.status} ${response.statusText}`
        }
      }
    } catch (ex) {
      apiErrorMsg = ex instanceof Error ? ex.message : 'Lỗi kết nối máy chủ'
    }

    // Fallback response with smart offline engine
    setTimeout(() => {
      const fallbackReply = getSmartFallbackReply(text)
      const botText = (isAdmin && apiErrorMsg)
        ? `[Lưu ý Quản trị viên]: ${apiErrorMsg}.\n*Vui lòng kiểm tra lại cấu hình Endpoint URL và API Key trong Cài đặt!*\n\n---\n\n${fallbackReply}`
        : fallbackReply

      setMessages((prev) => [
        ...prev,
        {
          id: String(Date.now() + 1),
          sender: 'bot',
          text: botText,
          timestamp: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
        },
      ])
      setIsLoading(false)
    }, 400)
  }

  return (
    <>
      {/* Floating Launcher Button */}
      <div className="fixed bottom-20 md:bottom-6 right-4 sm:right-6 z-40">
        <button
          type="button"
          onClick={() => setIsOpen(!isOpen)}
          aria-label="Mở Trợ lý ảo AI GroceryHub"
          className="relative w-13 h-13 rounded-full bg-leaf hover:bg-leaf/90 text-white flex items-center justify-center shadow-xl hover:shadow-2xl transition-all duration-300 transform hover:scale-105 cursor-pointer"
        >
          {isOpen ? (
            <X className="w-6 h-6 stroke-[2]" />
          ) : (
            <>
              <Sparkles className="w-6 h-6 stroke-[1.8]" />
              {hasUnread && (
                <span className="absolute -top-1 -right-1 w-4 h-4 bg-tomato text-white text-[10px] font-bold rounded-full flex items-center justify-center ring-2 ring-white">
                  1
                </span>
              )}
            </>
          )}
        </button>
      </div>

      {/* Chat Window */}
      {isOpen && (
        <div className="fixed bottom-20 md:bottom-22 right-2 sm:right-6 z-50 w-[calc(100vw-16px)] sm:w-[400px] max-w-[420px] h-[78vh] sm:h-[550px] bg-paper rounded-3xl border border-rule shadow-2xl flex flex-col overflow-hidden animate-fade-in">
          {/* Header */}
          <div className="bg-leaf text-white px-4 py-3.5 flex items-center justify-between shadow-xs">
            <div className="flex items-center gap-2.5">
              <div className="relative w-9 h-9 rounded-full bg-white/15 backdrop-blur-xs flex items-center justify-center">
                <Bot className="w-5 h-5 text-white stroke-[2]" />
                <span className="absolute bottom-0 right-0 w-2.5 h-2.5 bg-emerald-400 border-2 border-leaf rounded-full" />
              </div>
              <div>
                <h3 className="font-display font-bold text-sm leading-tight flex items-center gap-1.5">
                  GroceryHub AI
                  {aiConfig.apiKey && (
                    <span className="text-[10px] bg-white/20 font-sans font-semibold px-1.5 py-0.2 rounded-md uppercase">
                      {aiConfig.provider}
                    </span>
                  )}
                </h3>
                <p className="text-[11px] text-white/80 leading-tight">Thôn 9, Suối Hai, Ba Vì • Trợ lý 24/7</p>
              </div>
            </div>

            <div className="flex items-center gap-1">
              {/* Only Admin can see and open Settings */}
              {isAdmin && (
                <button
                  type="button"
                  onClick={() => setIsSettingsOpen(!isSettingsOpen)}
                  className={`p-1.5 rounded-lg text-white/80 hover:text-white hover:bg-white/10 transition-colors ${
                    isSettingsOpen ? 'bg-white/20 text-white' : ''
                  }`}
                  title="Cấu hình API AI (Dành riêng cho Quản trị viên)"
                >
                  <Settings className="w-4 h-4" />
                </button>
              )}
              <button
                type="button"
                onClick={handleClearHistory}
                className="p-1.5 rounded-lg text-white/80 hover:text-white hover:bg-white/10 transition-colors"
                title="Làm mới lịch sử chat"
              >
                <RotateCcw className="w-4 h-4" />
              </button>
              <button
                type="button"
                onClick={() => setIsOpen(false)}
                className="p-1.5 rounded-lg text-white/80 hover:text-white hover:bg-white/10 transition-colors ml-0.5"
                title="Đóng chat"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
          </div>

          {/* Admin-Only Settings Panel (Universal AI Configuration) */}
          {isAdmin && isSettingsOpen && (
            <div className="p-4 bg-paper-2 border-b border-rule space-y-3 animate-fade-in text-xs max-h-[340px] overflow-y-auto">
              <div className="flex items-center justify-between font-semibold text-ink">
                <span className="flex items-center gap-1.5">
                  <Sliders className="w-4 h-4 text-leaf" strokeWidth={1.5} />
                  Cấu hình AI Chatbot (Admin Only)
                </span>
                <span className="text-[10px] text-ink-2 font-normal">Linh hoạt mọi AI</span>
              </div>

              <form onSubmit={handleSaveConfig} className="space-y-2.5">
                {/* Provider Selector */}
                <div>
                  <label className="text-[11px] font-semibold text-ink block mb-1">
                    Nhà cung cấp AI (Provider)
                  </label>
                  <select
                    value={tempConfig.provider}
                    onChange={(e) => {
                      const p = e.target.value as 'gemini' | 'openai' | 'custom'
                      setTempConfig((prev) => ({
                        ...prev,
                        provider: p,
                        endpointUrl:
                          p === 'openai'
                            ? 'https://api.xingchiapi.com'
                            : p === 'gemini'
                            ? 'https://generativelanguage.googleapis.com/v1beta/models'
                            : prev.endpointUrl,
                        model:
                          p === 'openai'
                            ? 'gpt-6.1-sol'
                            : p === 'gemini'
                            ? 'gemini-2.5-flash'
                            : prev.model,
                      }))
                    }}
                    className="w-full h-8 px-2.5 bg-paper border border-rule rounded-lg text-xs font-medium text-ink focus:outline-hidden focus:border-leaf"
                  >
                    <option value="openai">OpenAI / Proxy tương thích (Mặc định: gpt-6.1-sol)</option>
                    <option value="gemini">Google Gemini (Model: gemini-2.5-flash)</option>
                    <option value="custom">Tùy chỉnh Endpoint bất kỳ (Custom)</option>
                  </select>
                </div>

                {/* Endpoint URL (shown for OpenAI & Custom) */}
                {tempConfig.provider !== 'gemini' && (
                  <div>
                    <label className="text-[11px] font-semibold text-ink block mb-1">
                      Endpoint URL
                    </label>
                    <input
                      type="text"
                      value={tempConfig.endpointUrl}
                      onChange={(e) => setTempConfig({ ...tempConfig, endpointUrl: e.target.value })}
                      placeholder="https://api.xingchiapi.com"
                      className="w-full px-2.5 py-1.5 bg-paper border border-rule rounded-lg font-mono text-[11px] focus:outline-hidden focus:border-leaf"
                    />
                    <span className="text-[10px] text-ink-2 mt-0.5 block">
                      Ví dụ: https://api.xingchiapi.com hoặc https://api.openai.com/v1/chat/completions
                    </span>
                  </div>
                )}

                {/* Model Name */}
                <div>
                  <label className="text-[11px] font-semibold text-ink block mb-1">
                    Tên mô hình (Model Name)
                  </label>
                  <input
                    type="text"
                    value={tempConfig.model}
                    onChange={(e) => setTempConfig({ ...tempConfig, model: e.target.value })}
                    placeholder={tempConfig.provider === 'gemini' ? 'gemini-2.5-flash' : 'gpt-6.1-sol'}
                    className="w-full px-2.5 py-1.5 bg-paper border border-rule rounded-lg font-mono text-[11px] focus:outline-hidden focus:border-leaf"
                  />
                </div>

                {/* API Key */}
                <div>
                  <div className="flex items-center justify-between mb-1">
                    <label className="text-[11px] font-semibold text-ink">
                      Mã API Key của bạn (Tự điền mã sk-...)
                    </label>
                    <button
                      type="button"
                      onClick={() => setShowApiKey(!showApiKey)}
                      className="text-[10px] text-leaf hover:underline flex items-center gap-1 cursor-pointer"
                    >
                      {showApiKey ? (
                        <>
                          <EyeOff className="w-3 h-3" /> Ẩn key
                        </>
                      ) : (
                        <>
                          <Eye className="w-3 h-3" /> Xem key
                        </>
                      )}
                    </button>
                  </div>
                  <input
                    type={showApiKey ? 'text' : 'password'}
                    value={tempConfig.apiKey}
                    onChange={(e) => setTempConfig({ ...tempConfig, apiKey: e.target.value })}
                    placeholder="Dán mã API Key của bạn vào đây (sk-...)"
                    className="w-full px-2.5 py-1.5 bg-paper border border-rule rounded-lg font-mono text-[11px] focus:outline-hidden focus:border-leaf"
                  />
                  <span className="text-[10px] text-ink-2 mt-0.5 block">
                    Khóa này được lưu an toàn trên máy và đồng bộ với cấu hình hệ thống.
                  </span>
                </div>

                <div className="flex items-center justify-between pt-1">
                  <span className="text-[10px] text-ink-2">Chỉ Quản trị viên mới thấy menu này</span>
                  <button
                    type="submit"
                    className="px-3.5 py-1.5 rounded-lg bg-leaf text-white font-semibold text-xs hover:bg-leaf/90 transition-colors shadow-2xs cursor-pointer"
                  >
                    Lưu cấu hình
                  </button>
                </div>
              </form>
            </div>
          )}

          {/* Main Body: Guest Locked State vs Message List */}
          {!isAuthenticated ? (
            /* Locked View for Unauthenticated Guests */
            <div className="flex-1 flex flex-col items-center justify-center p-6 text-center space-y-4 bg-paper">
              <div className="w-14 h-14 rounded-2xl bg-leaf/10 border border-leaf/20 text-leaf flex items-center justify-center mx-auto shadow-xs">
                <Lock className="w-7 h-7 stroke-[1.5]" />
              </div>
              <div className="space-y-1.5">
                <h4 className="font-display font-bold text-base text-ink">Đăng nhập để trò chuyện</h4>
                <p className="text-xs text-ink-2 leading-relaxed max-w-[270px]">
                  Trợ lý ảo AI của GroceryHub luôn sẵn sàng tư vấn thực đơn ngon, thực phẩm sạch và hỗ trợ giao hàng tận nơi. Quý khách vui lòng đăng nhập tài khoản để bắt đầu!
                </p>
              </div>

              <div className="w-full max-w-[220px] space-y-2 pt-1">
                <a
                  href="/login"
                  className="w-full h-10 rounded-xl bg-leaf hover:bg-leaf/90 text-white text-xs font-semibold shadow-xs flex items-center justify-center gap-2 transition-colors cursor-pointer"
                >
                  <LogIn className="w-4 h-4 stroke-[2]" />
                  <span>Đăng nhập ngay</span>
                </a>
                <p className="text-[11px] text-ink-2">
                  Chưa có tài khoản?{' '}
                  <a href="/register" className="text-leaf font-semibold hover:underline cursor-pointer">
                    Đăng ký miễn phí
                  </a>
                </p>
              </div>
            </div>
          ) : (
            /* Authenticated User: Message List & Input */
            <>
              {/* Message List */}
              <div className="flex-1 overflow-y-auto p-4 space-y-3 text-xs">
                {messages.map((m) => (
                  <div
                    key={m.id}
                    className={`flex gap-2 ${m.sender === 'user' ? 'justify-end' : 'justify-start'}`}
                  >
                    {m.sender === 'bot' && (
                      <div className="w-7 h-7 rounded-full bg-leaf/15 text-leaf flex items-center justify-center shrink-0 mt-0.5">
                        <Bot className="w-4 h-4 stroke-[2]" />
                      </div>
                    )}
                    <div
                      className={`max-w-[80%] rounded-2xl px-3.5 py-2.5 leading-relaxed shadow-2xs whitespace-pre-wrap ${
                        m.sender === 'user'
                          ? 'bg-leaf text-white rounded-br-xs'
                          : 'bg-paper-2 text-ink border border-rule rounded-bl-xs'
                      }`}
                    >
                      {m.sender === 'bot' ? (
                        <FormattedMessage text={m.text} onNavigate={navigate} />
                      ) : (
                        <p className="leading-relaxed">{m.text}</p>
                      )}
                      <span
                        className={`text-[9px] block text-right mt-1 opacity-70 ${
                          m.sender === 'user' ? 'text-white/80' : 'text-ink-2'
                        }`}
                      >
                        {m.timestamp}
                      </span>
                    </div>
                    {m.sender === 'user' && (
                      <div className="w-7 h-7 rounded-full bg-slate-200 text-slate-600 flex items-center justify-center shrink-0 mt-0.5">
                        <User className="w-4 h-4" />
                      </div>
                    )}
                  </div>
                ))}

                {isLoading && (
                  <div className="flex gap-2 items-center text-ink-2 text-xs">
                    <div className="w-7 h-7 rounded-full bg-leaf/15 text-leaf flex items-center justify-center shrink-0">
                      <Bot className="w-4 h-4 animate-spin" />
                    </div>
                    <div className="bg-paper-2 px-3.5 py-2 rounded-2xl border border-rule flex items-center gap-1.5 text-[11px]">
                      <span className="w-1.5 h-1.5 rounded-full bg-leaf animate-bounce" />
                      <span className="w-1.5 h-1.5 rounded-full bg-leaf animate-bounce delay-100" />
                      <span className="w-1.5 h-1.5 rounded-full bg-leaf animate-bounce delay-200" />
                      <span className="ml-1 text-ink-2">GroceryHub AI đang trả lời...</span>
                    </div>
                  </div>
                )}
                <div ref={messagesEndRef} />
              </div>

              {/* Quick Suggestions */}
              <div className="px-3 py-1.5 bg-paper-2 border-t border-rule overflow-x-auto flex gap-1.5 no-scrollbar">
                {DEFAULT_SUGGESTIONS.map((s, idx) => (
                  <button
                    key={idx}
                    type="button"
                    onClick={() => handleSend(s)}
                    className="shrink-0 px-2.5 py-1 rounded-full bg-white border border-rule text-[11px] text-ink-2 hover:text-leaf hover:border-leaf/50 transition-colors select-none cursor-pointer"
                  >
                    {s}
                  </button>
                ))}
              </div>

              {/* Input Bar */}
              <div className="p-3 bg-paper border-t border-rule">
                <form
                  onSubmit={(e) => {
                    e.preventDefault()
                    void handleSend()
                  }}
                  className="flex items-center gap-2"
                >
                  <input
                    type="text"
                    value={inputMessage}
                    onChange={(e) => setInputMessage(e.target.value)}
                    placeholder="Hỏi về món ăn, địa chỉ shop, đặt hàng..."
                    className="flex-1 h-10 px-3.5 bg-paper-2 border border-rule rounded-xl text-xs focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                  <button
                    type="submit"
                    disabled={isLoading || !inputMessage.trim()}
                    className="w-10 h-10 rounded-xl bg-leaf hover:bg-leaf/90 disabled:opacity-40 text-white flex items-center justify-center shadow-2xs transition-colors shrink-0 cursor-pointer"
                  >
                    <Send className="w-4 h-4 stroke-[2]" />
                  </button>
                </form>
              </div>
            </>
          )}
        </div>
      )}
    </>
  )
}
