import React, { useState, useEffect, useCallback, useMemo } from 'react'
import {
  CheckCircle2,
  Clock,
  Copy,
  Check,
  RefreshCw,
  AlertCircle,
  ArrowLeft,
  QrCode,
  ShieldCheck,
  Building2,
  Wallet,
} from 'lucide-react'
import { useRouter } from '@/app/router'
import { useAuth } from '@/shared/auth'
import { api } from '@/shared/api'
import { formatVnd } from '@/shared/utils/formatVnd'
import { Button } from '@/shared/ui/Button'
import { Badge } from '@/shared/ui/Badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/Card'
import { Skeleton } from '@/shared/ui/Skeleton'

interface PaymentPageProps {
  paymentCode: string
}

interface PaymentStatusData {
  paymentId: string
  orderId: string
  orderNumber: string
  paymentCode: string
  status: string
  method: string
  amount: number
  paidAt?: string | null
  attempt: {
    id: string
    paymentId: string
    paymentCode: string
    attemptNumber: number
    amount: number
    status: string
    expiresAt: string
    qr: {
      url: string
      bank: string
      accountNumber: string
      accountName: string
      amount: number
      content: string
      expiresAt: string
    }
  }
  orderStatus: string
}

export const PaymentPage: React.FC<PaymentPageProps> = ({ paymentCode }) => {
  const router = useRouter()
  const { isAuthenticated, isLoading: authLoading } = useAuth()

  const [paymentData, setPaymentData] = useState<PaymentStatusData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [checking, setChecking] = useState(false)
  const [copiedField, setCopiedField] = useState<string | null>(null)
  const [isGeneratingAttempt, setIsGeneratingAttempt] = useState(false)
  const [pollIntervalMs, setPollIntervalMs] = useState(3000)

  // Redirect to login if not authenticated once auth is checked
  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.navigate(`/login?returnUrl=/payment/${encodeURIComponent(paymentCode)}`)
    }
  }, [authLoading, isAuthenticated, paymentCode, router])

  // Fetch status from server
  const fetchStatus = useCallback(async (isManual = false) => {
    if (isManual) setChecking(true)
    try {
      const response = await api.GET('/api/v1/payments/{paymentCode}/status', {
        params: { path: { paymentCode } },
      })

      if (response.error || !response.data) {
        setError('Không tìm thấy thông tin thanh toán hoặc bạn không có quyền xem giao dịch này.')
        return
      }

      const data = response.data as unknown as PaymentStatusData
      setPaymentData(data)
      setError(null)

      // Stop polling if Paid or Cancelled
      if (
        data.status === 'Paid' ||
        data.orderStatus === 'Confirmed' ||
        data.status === 'Expired' ||
        data.orderStatus === 'Cancelled'
      ) {
        setPollIntervalMs(0)
      } else {
        // Backoff: after successful check, increase interval slightly up to 10s
        setPollIntervalMs((prev) => (prev < 10000 ? Math.min(prev + 1000, 10000) : prev))
      }
    } catch {
      setError('Lỗi khi kiểm tra trạng thái thanh toán. Đang thử lại...')
    } finally {
      setLoading(false)
      if (isManual) setChecking(false)
    }
  }, [paymentCode])

  // Initial load
  useEffect(() => {
    if (isAuthenticated) {
      void fetchStatus()
    }
  }, [isAuthenticated, fetchStatus])

  // Polling effect with backoff
  useEffect(() => {
    if (pollIntervalMs <= 0 || loading || !paymentData) return

    const timer = setInterval(() => {
      void fetchStatus()
    }, pollIntervalMs)

    return () => clearInterval(timer)
  }, [pollIntervalMs, loading, paymentData, fetchStatus])

  // Countdown timer for QR expiration
  const [timeLeft, setTimeLeft] = useState<{ minutes: number; seconds: number; isExpired: boolean }>({
    minutes: 0,
    seconds: 0,
    isExpired: false,
  })

  useEffect(() => {
    const rawExpiresAt = (paymentData as any)?.attempt?.expiresAt || (paymentData as any)?.expiresAt
    if (!rawExpiresAt) return

    const updateCountdown = () => {
      const expiresAt = new Date(rawExpiresAt).getTime()
      const now = Date.now()
      const diff = expiresAt - now

      if (diff <= 0) {
        setTimeLeft({ minutes: 0, seconds: 0, isExpired: true })
      } else {
        const minutes = Math.floor(diff / 60000)
        const seconds = Math.floor((diff % 60000) / 1000)
        setTimeLeft({ minutes, seconds, isExpired: false })
      }
    }

    updateCountdown()
    const interval = setInterval(updateCountdown, 1000)
    return () => clearInterval(interval)
  }, [(paymentData as any)?.attempt?.expiresAt, (paymentData as any)?.expiresAt])

  // Copy to clipboard helper
  const handleCopy = (text: string, fieldName: string) => {
    navigator.clipboard.writeText(text)
    setCopiedField(fieldName)
    setTimeout(() => {
      setCopiedField(null)
    }, 2000)
  }

  // Handle generating a new payment attempt
  const handleNewAttempt = async () => {
    if (!paymentData?.orderId) return
    setIsGeneratingAttempt(true)
    try {
      const response = await api.POST('/api/v1/me/orders/{id}/payment-attempts', {
        params: { path: { id: paymentData.orderId } },
      })

      if (response.data) {
        const attempt = response.data as unknown as { paymentCode: string }
        router.navigate(`/payment/${attempt.paymentCode}`)
      } else {
        setError('Không thể tạo mã QR mới. Vui lòng thử lại sau.')
      }
    } catch {
      setError('Lỗi kết nối khi tạo mã QR mới.')
    } finally {
      setIsGeneratingAttempt(false)
    }
  }

  const isCancelled = paymentData?.orderStatus === 'Cancelled'
  const isPaid = useMemo(() => {
    return paymentData?.status === 'Paid' || paymentData?.orderStatus === 'Confirmed'
  }, [paymentData])

  const isExpired = useMemo(() => {
    return (
      timeLeft.isExpired ||
      paymentData?.status === 'Expired' ||
      (paymentData as any)?.attempt?.status === 'Expired' ||
      isCancelled
    )
  }, [timeLeft.isExpired, paymentData, isCancelled])

  if (authLoading || (loading && !paymentData)) {
    return (
      <div className="max-w-2xl mx-auto py-12 px-4 space-y-6">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-96 w-full rounded-2xl" />
      </div>
    )
  }

  if (error && !paymentData) {
    return (
      <div className="max-w-md mx-auto py-16 px-4 text-center space-y-4">
        <AlertCircle className="w-12 h-12 text-tomato mx-auto stroke-[1.5]" />
        <h2 className="text-base font-bold text-ink">Không thể tải thông tin thanh toán</h2>
        <p className="text-xs text-ink-2">{error}</p>
        <Button variant="secondary" onClick={() => router.navigate('/orders')} className="mt-4">
          <ArrowLeft className="w-4 h-4 mr-2" /> Về danh sách đơn hàng
        </Button>
      </div>
    )
  }

  // Celebratory Success View
  if (isPaid) {
    return (
      <div className="max-w-md mx-auto py-16 px-4 text-center space-y-6 animate-fade-in">
        <div className="w-16 h-16 bg-leaf/10 text-leaf rounded-full flex items-center justify-center mx-auto ring-8 ring-leaf/5">
          <CheckCircle2 className="w-10 h-10 stroke-[2]" />
        </div>
        <div className="space-y-2">
          <Badge variant="ok" className="px-3 py-1 text-xs">
            Đã thanh toán thành công
          </Badge>
          <h1 className="text-xl font-bold text-ink">Thanh toán hoàn tất!</h1>
          <p className="text-xs text-ink-2">
            Đơn hàng <span className="font-mono font-bold text-ink">{paymentData?.orderNumber}</span> đã được ghi nhận thanh toán qua SePay VietQR. Cửa hàng đang tiến hành chuẩn bị đơn hàng cho bạn.
          </p>
        </div>

        <Card className="text-left bg-paper-2 border-rule">
          <CardContent className="p-4 space-y-2 text-xs">
            <div className="flex justify-between py-1 border-b border-rule/50">
              <span className="text-ink-2">Số tiền thanh toán</span>
              <span className="font-bold text-leaf font-mono">{formatVnd(paymentData?.amount ?? 0)}</span>
            </div>
            <div className="flex justify-between py-1 border-b border-rule/50">
              <span className="text-ink-2">Mã thanh toán</span>
              <span className="font-mono font-bold text-ink">{paymentData?.paymentCode}</span>
            </div>
            <div className="flex justify-between py-1">
              <span className="text-ink-2">Trạng thái đơn</span>
              <span className="font-bold text-ink">Đã xác nhận (Confirmed)</span>
            </div>
          </CardContent>
        </Card>

        <div className="space-y-2 pt-2">
          <Button
            className="w-full h-11 bg-leaf hover:bg-leaf/90 text-white font-semibold text-sm shadow-md"
            onClick={() => router.navigate(`/orders/${paymentData?.orderId}`)}
          >
            Xem chi tiết đơn hàng
          </Button>
          <Button
            variant="ghost"
            className="w-full text-xs text-ink-2 hover:text-ink"
            onClick={() => router.navigate('/')}
          >
            Tiếp tục mua sắm
          </Button>
        </div>
      </div>
    )
  }

  const fallbackBank = 'MBBank'
  const fallbackAccountNumber = '2222226062005'
  const fallbackAccountName = 'DANG BINH DUONG'
  const amount = paymentData?.amount ?? 0
  const paymentCodeVal = paymentData?.paymentCode ?? paymentCode ?? ''

  const rawQr = (paymentData as any)?.attempt?.qr || (paymentData as any)?.qr
  const qr = {
    bank: (rawQr?.bank && String(rawQr.bank).trim()) || fallbackBank,
    accountNumber: (rawQr?.accountNumber && String(rawQr.accountNumber).trim()) || fallbackAccountNumber,
    accountName: (rawQr?.accountName && String(rawQr.accountName).trim()) || fallbackAccountName,
    amount: rawQr?.amount || amount,
    content: rawQr?.content || paymentCodeVal,
    url: rawQr?.url || `https://qr.sepay.vn/img?acc=${encodeURIComponent((rawQr?.accountNumber && String(rawQr.accountNumber).trim()) || fallbackAccountNumber)}&bank=${encodeURIComponent((rawQr?.bank && String(rawQr.bank).trim()) || fallbackBank)}&amount=${rawQr?.amount || amount}&des=${encodeURIComponent(rawQr?.content || paymentCodeVal)}&template=compact&download=false`,
  }

  return (
    <div className="max-w-2xl mx-auto py-8 px-4 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between border-b border-rule pb-4">
        <button
          type="button"
          onClick={() => {
            const dest = (paymentData as any)?.orderId ? `/orders/${(paymentData as any).orderId}` : '/orders'
            router.navigate(dest)
          }}
          className="inline-flex items-center gap-1.5 text-xs text-ink-2 hover:text-ink font-medium transition-colors"
        >
          <ArrowLeft className="w-4 h-4" /> Quay lại đơn hàng
        </button>
        <div className="flex items-center gap-2">
          <ShieldCheck className="w-4 h-4 text-leaf" />
          <span className="text-xs font-semibold text-ink-2">Thanh toán bảo mật VietQR</span>
        </div>
      </div>

      <div className="text-center space-y-1">
        <h1 className="text-xl font-bold text-ink">Quét mã VietQR để thanh toán</h1>
        <p className="text-xs text-ink-2">
          Đơn hàng <span className="font-mono font-bold text-ink">{(paymentData as any)?.orderNumber || paymentData?.paymentCode}</span> • Tổng tiền:{' '}
          <span className="font-bold text-leaf font-mono">{formatVnd(paymentData?.amount ?? 0)}</span>
        </p>
      </div>

      {/* Countdown Timer or Expired Banner */}
      <div className="flex justify-center">
        {isCancelled ? (
          <div className="inline-flex items-center gap-2 px-3.5 py-1.5 bg-tomato/10 text-tomato border border-tomato/20 rounded-full text-xs font-semibold">
            <AlertCircle className="w-4 h-4 stroke-[2]" />
            Đơn hàng đã tự động hủy do quá hạn thanh toán (30 phút)
          </div>
        ) : isExpired ? (
          <div className="inline-flex items-center gap-2 px-3.5 py-1.5 bg-tomato/10 text-tomato border border-tomato/20 rounded-full text-xs font-semibold">
            <AlertCircle className="w-4 h-4 stroke-[2]" />
            Mã QR đã hết hạn thanh toán
          </div>
        ) : (
          <div className="inline-flex items-center gap-2 px-3.5 py-1.5 bg-leaf/10 text-leaf border border-leaf/20 rounded-full text-xs font-semibold">
            <Clock className="w-4 h-4 animate-spin-slow stroke-[2]" />
            Mã QR còn hiệu lực trong:{' '}
            <span className="font-mono font-bold text-sm">
              {String(timeLeft.minutes).padStart(2, '0')}:{String(timeLeft.seconds).padStart(2, '0')}
            </span>
          </div>
        )}
      </div>

      {/* Main Grid: QR Code & Transfer Details */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 items-start">
        {/* Left Column: QR Code Image */}
        <Card className="text-center border-rule shadow-sm overflow-hidden">
          <CardHeader className="bg-paper-2 py-3 border-b border-rule">
            <CardTitle className="text-xs font-bold text-ink flex items-center justify-center gap-1.5">
              <QrCode className="w-4 h-4 text-leaf" />
              Mã QR Chuẩn Napas 24/7
            </CardTitle>
          </CardHeader>
          <CardContent className="p-6 flex flex-col items-center justify-center space-y-4">
            <div className="relative p-2 bg-white rounded-xl border border-rule/80 shadow-inner flex items-center justify-center min-h-[220px]">
              <img
                src={qr.url}
                alt={`VietQR ${paymentCodeVal}`}
                className={`w-52 h-52 object-contain transition-opacity duration-300 ${
                  isExpired ? 'opacity-30 grayscale' : 'opacity-100'
                }`}
              />
              {isExpired && (
                <div className="absolute inset-0 flex flex-col items-center justify-center p-4 bg-paper/90 rounded-xl backdrop-blur-xs text-center">
                  <AlertCircle className="w-7 h-7 text-tomato mb-1 stroke-[1.5]" />
                  <p className="text-xs font-bold text-tomato mb-1">
                    {isCancelled ? 'Đơn hàng đã hủy' : 'Mã QR đã hết hạn'}
                  </p>
                  <p className="text-[11px] text-ink-2 max-w-[200px] mb-3">
                    {isCancelled
                      ? 'Đơn hàng bị hủy do quá 30 phút chưa thanh toán. Vui lòng đặt đơn hàng mới.'
                      : 'Quá thời hạn quét mã 30 phút. Bấm bên dưới để tạo mã mới.'}
                  </p>
                  {isCancelled ? (
                    <Button
                      size="sm"
                      onClick={() => router.navigate('/')}
                      className="text-xs bg-leaf hover:bg-leaf/90 text-white shadow-xs"
                    >
                      Tiếp tục mua sắm
                    </Button>
                  ) : (
                    <Button
                      size="sm"
                      onClick={handleNewAttempt}
                      disabled={isGeneratingAttempt}
                      className="text-xs bg-leaf hover:bg-leaf/90 text-white shadow-xs"
                    >
                      <RefreshCw className={`w-3.5 h-3.5 mr-1 ${isGeneratingAttempt ? 'animate-spin' : ''}`} />
                      Tạo lại mã QR
                    </Button>
                  )}
                </div>
              )}
            </div>

            <p className="text-[11px] text-ink-2 max-w-xs leading-relaxed">
              Mở App ngân hàng bất kỳ (Vietcombank, MB, Techcombank...) quét mã để chuyển khoản tức thì không cần nhập số tài khoản.
            </p>
          </CardContent>
        </Card>

        {/* Right Column: Transfer Info */}
        <div className="space-y-4">
          <Card className="border-rule shadow-sm">
            <CardHeader className="bg-paper-2 py-3 border-b border-rule">
              <CardTitle className="text-xs font-bold text-ink flex items-center gap-1.5">
                <Building2 className="w-4 h-4 text-leaf" />
                Thông tin chuyển khoản thủ công
              </CardTitle>
            </CardHeader>
            <CardContent className="p-4 space-y-3">
              {/* Ngân hàng */}
              <div>
                <label className="text-[11px] text-ink-2 block mb-0.5">Ngân hàng thụ hưởng</label>
                <div className="flex items-center justify-between p-2 rounded-lg bg-paper-2 border border-rule text-xs">
                  <span className="font-semibold text-ink">{qr?.bank ?? 'MBBank'}</span>
                  <button
                    type="button"
                    onClick={() => handleCopy(qr?.bank ?? 'MBBank', 'bank')}
                    className="text-ink-2 hover:text-leaf transition-colors p-1"
                    title="Sao chép"
                  >
                    {copiedField === 'bank' ? <Check className="w-3.5 h-3.5 text-leaf" /> : <Copy className="w-3.5 h-3.5" />}
                  </button>
                </div>
              </div>

              {/* Số tài khoản */}
              <div>
                <label className="text-[11px] text-ink-2 block mb-0.5">Số tài khoản</label>
                <div className="flex items-center justify-between p-2 rounded-lg bg-paper-2 border border-rule text-xs font-mono">
                  <span className="font-bold text-ink">{qr?.accountNumber}</span>
                  <button
                    type="button"
                    onClick={() => handleCopy(qr?.accountNumber ?? '', 'accountNumber')}
                    className="text-ink-2 hover:text-leaf transition-colors p-1"
                    title="Sao chép"
                  >
                    {copiedField === 'accountNumber' ? (
                      <Check className="w-3.5 h-3.5 text-leaf" />
                    ) : (
                      <Copy className="w-3.5 h-3.5" />
                    )}
                  </button>
                </div>
              </div>

              {/* Chủ tài khoản */}
              <div>
                <label className="text-[11px] text-ink-2 block mb-0.5">Chủ tài khoản</label>
                <div className="flex items-center justify-between p-2 rounded-lg bg-paper-2 border border-rule text-xs">
                  <span className="font-semibold text-ink uppercase">{qr?.accountName}</span>
                  <button
                    type="button"
                    onClick={() => handleCopy(qr?.accountName ?? '', 'accountName')}
                    className="text-ink-2 hover:text-leaf transition-colors p-1"
                    title="Sao chép"
                  >
                    {copiedField === 'accountName' ? (
                      <Check className="w-3.5 h-3.5 text-leaf" />
                    ) : (
                      <Copy className="w-3.5 h-3.5" />
                    )}
                  </button>
                </div>
              </div>

              {/* Số tiền */}
              <div>
                <label className="text-[11px] text-ink-2 block mb-0.5">Số tiền chính xác</label>
                <div className="flex items-center justify-between p-2 rounded-lg bg-leaf/5 border border-leaf/30 text-xs">
                  <span className="font-bold text-leaf font-mono text-sm">{formatVnd(qr?.amount ?? paymentData?.amount ?? 0)}</span>
                  <button
                    type="button"
                    onClick={() => handleCopy(String(qr?.amount ?? paymentData?.amount ?? 0), 'amount')}
                    className="text-leaf hover:text-leaf/80 transition-colors p-1"
                    title="Sao chép số tiền"
                  >
                    {copiedField === 'amount' ? <Check className="w-3.5 h-3.5 text-leaf" /> : <Copy className="w-3.5 h-3.5" />}
                  </button>
                </div>
              </div>

              {/* Nội dung chuyển khoản */}
              <div>
                <div className="flex items-center justify-between mb-0.5">
                  <label className="text-[11px] text-tomato font-bold">Nội dung chuyển khoản (BẮT BUỘC)</label>
                  <span className="text-[10px] text-ink-2">Giữ nguyên để khớp tự động</span>
                </div>
                <div className="flex items-center justify-between p-2 rounded-lg bg-tomato/5 border border-tomato/30 text-xs font-mono">
                  <span className="font-bold text-tomato text-sm">{qr?.content ?? paymentData?.paymentCode}</span>
                  <button
                    type="button"
                    onClick={() => handleCopy(qr?.content ?? paymentData?.paymentCode ?? '', 'content')}
                    className="text-tomato hover:text-tomato/80 transition-colors p-1"
                    title="Sao chép nội dung"
                  >
                    {copiedField === 'content' ? <Check className="w-3.5 h-3.5 text-leaf" /> : <Copy className="w-3.5 h-3.5" />}
                  </button>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Action Buttons */}
          <div className="space-y-2">
            {isCancelled ? (
              <div className="p-3 bg-tomato/5 border border-tomato/20 rounded-xl space-y-2">
                <div className="flex items-start gap-2 text-xs text-tomato">
                  <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
                  <p className="leading-relaxed">
                    <strong>Đơn hàng đã hết hạn:</strong> Quá 30 phút chưa thanh toán nên đơn hàng đã tự động hủy. Nếu quý khách đã chuyển khoản thành công từ tài khoản ngân hàng, vui lòng liên hệ hotline/Zalo <strong>0865 087 784</strong> để được nhân viên đối soát hoàn tiền hoặc tạo đơn ngay!
                  </p>
                </div>
                <div className="flex gap-2 pt-1">
                  <Button
                    className="flex-1 h-9 bg-leaf hover:bg-leaf/90 text-white font-semibold text-xs"
                    onClick={() => router.navigate('/')}
                  >
                    Mua sắm đơn mới
                  </Button>
                  <Button
                    variant="secondary"
                    className="flex-1 h-9 text-xs"
                    onClick={() => router.navigate('/orders')}
                  >
                    Xem đơn hàng
                  </Button>
                </div>
              </div>
            ) : (
              <>
                <Button
                  className="w-full h-11 bg-leaf hover:bg-leaf/90 text-white font-semibold text-xs shadow-sm flex items-center justify-center gap-2"
                  onClick={() => fetchStatus(true)}
                  disabled={checking}
                >
                  <RefreshCw className={`w-3.5 h-3.5 ${checking ? 'animate-spin' : ''}`} />
                  {checking ? 'Đang kiểm tra từ ngân hàng...' : 'Tôi đã chuyển khoản'}
                </Button>

                <div className="flex items-center justify-between text-[11px] text-ink-2 px-1">
                  <span>
                    Lần thử: {paymentData?.attempt?.attemptNumber ?? 1} / 5
                  </span>
                  {isExpired && (
                    <button
                      type="button"
                      onClick={handleNewAttempt}
                      disabled={isGeneratingAttempt}
                      className="text-leaf font-bold hover:underline"
                    >
                      Tạo mã QR mới
                    </button>
                  )}
                </div>
              </>
            )}
          </div>
        </div>
      </div>

      {/* Note footer */}
      <div className="p-3 bg-paper-2 rounded-xl border border-rule text-[11px] text-ink-2 space-y-1">
        <p className="font-semibold text-ink flex items-center gap-1.5">
          <Wallet className="w-3.5 h-3.5 text-leaf" /> Lưu ý quan trọng khi thanh toán:
        </p>
        <ul className="list-disc list-inside space-y-0.5 text-ink-2 pl-1">
          <li>Vui lòng chuyển <strong>chính xác số tiền</strong> và <strong>nội dung chuyển khoản</strong> được hiển thị ở trên.</li>
          <li>Hệ thống SePay tự động ghi nhận giao dịch trong vòng 5 - 30 giây kể từ khi ngân hàng báo trừ tiền thành công.</li>
          <li>Nếu bạn chuyển sai nội dung hoặc thiếu tiền, đội ngũ hỗ trợ GroceryHub sẽ kiểm tra và đối soát trong mục Hỗ trợ.</li>
        </ul>
      </div>
    </div>
  )
}
