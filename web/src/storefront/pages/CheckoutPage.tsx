import React, { useEffect, useState } from 'react'
import { api } from '@/shared/api/client'
import type { CustomerAddress, ProvinceItem, WardItem } from '@/shared/api/types'
import { useCart } from '@/shared/cart/CartContext'
import { useAuth } from '@/shared/auth/AuthContext'
import { ReceiptSummary } from '@/shared/components/ReceiptSummary'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/shared/ui/Select'
import { Skeleton } from '@/shared/ui/Skeleton'
import { vi } from '@/shared/i18n/vi'
import { formatVnd } from '@/shared/utils/formatVnd'
import { useRouter } from '@/app/router'
import {
  MapPin,
  Plus,
  CreditCard,
  Banknote,
  QrCode,
  ShieldCheck,
  Truck,
  CheckCircle2,
  AlertCircle,
  Lock,
  LogIn,
} from 'lucide-react'

export const CheckoutPage: React.FC = () => {
  const { navigate } = useRouter()
  const { cart, refreshCart } = useCart()
  const { isAuthenticated, isLoading: authLoading } = useAuth()

  // Addresses State
  const [addresses, setAddresses] = useState<CustomerAddress[]>([])
  const [selectedAddressId, setSelectedAddressId] = useState<string>('')
  const [showNewAddressForm, setShowNewAddressForm] = useState<boolean>(false)

  // 2-level VN Location State for new address
  const [provinces, setProvinces] = useState<ProvinceItem[]>([])
  const [wards, setWards] = useState<WardItem[]>([])
  const [recipientName, setRecipientName] = useState('')
  const [phone, setPhone] = useState('')
  const [selectedProvinceCode, setSelectedProvinceCode] = useState('')
  const [selectedWardCode, setSelectedWardCode] = useState('')
  const [street, setStreet] = useState('')
  const [addressNote, setAddressNote] = useState('')
  const [isSavingAddress, setIsSavingAddress] = useState(false)
  const [addressError, setAddressError] = useState<string | null>(null)

  // Order Options State
  const [paymentMethod, setPaymentMethod] = useState<'COD' | 'SEPAY_QR'>('COD')
  const [orderNote, setOrderNote] = useState('')

  // Quote State
  const [shippingFee, setShippingFee] = useState<number>(0)

  // Order Placement State
  const [isPlacingOrder, setIsPlacingOrder] = useState<boolean>(false)
  const [orderError, setOrderError] = useState<string | null>(null)
  const [idempotencyKey] = useState<string>(() => crypto.randomUUID())

  // Load Saved Addresses
  useEffect(() => {
    async function loadAddresses() {
      if (!isAuthenticated) return
      try {
        const { data } = await api.GET('/api/v1/me/addresses')
        if (data && data.length > 0) {
          setAddresses(data)
          const def = data.find((a) => a.isDefault) || data[0]
          if (def) {
            setSelectedAddressId(def.id)
          }
        } else {
          setShowNewAddressForm(true)
        }
      } catch {
        // silent fail
      }
    }
    void loadAddresses()
  }, [isAuthenticated])

  // Load Provinces
  useEffect(() => {
    async function loadProvinces() {
      try {
        const { data } = await api.GET('/api/v1/locations/provinces')
        if (data?.items) {
          setProvinces(data.items)
        }
      } catch {
        // silent
      }
    }
    void loadProvinces()
  }, [])

  // Load Wards when Province changes
  useEffect(() => {
    async function loadWards() {
      if (!selectedProvinceCode) {
        setWards([])
        setSelectedWardCode('')
        return
      }
      try {
        const { data } = await api.GET('/api/v1/locations/provinces/{code}/wards', {
          params: { path: { code: selectedProvinceCode } },
        })
        if (data?.items) {
          setWards(data.items)
          if (data.items[0]) {
            setSelectedWardCode(data.items[0].code)
          }
        }
      } catch {
        setWards([])
      }
    }
    void loadWards()
  }, [selectedProvinceCode])

  // Fetch Checkout Quote when selectedAddress changes
  useEffect(() => {
    async function fetchQuote() {
      if (!selectedAddressId) return
      try {
        const { data } = await api.POST('/api/v1/checkout/quote', {
          body: {
            addressId: selectedAddressId,
            couponCode: cart?.couponCode ?? null,
            paymentMethod,
          },
        })
        if (data) {
          setShippingFee(Number(data.shippingFee || 0))
        }
      } catch {
        setShippingFee(30000) // Fallback standard shipping
      }
    }
    void fetchQuote()
  }, [selectedAddressId, cart?.couponCode, paymentMethod])

  // Handle Save New Address
  const handleSaveAddress = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!recipientName || !phone || !selectedProvinceCode || !selectedWardCode || !street) {
      setAddressError('Vui lòng điền đầy đủ các thông tin địa chỉ bắt buộc.')
      return
    }

    setIsSavingAddress(true)
    setAddressError(null)

    try {
      const { data, error } = await api.POST('/api/v1/me/addresses', {
        body: {
          recipientName: recipientName.trim(),
          phone: phone.trim(),
          provinceCode: selectedProvinceCode,
          wardCode: selectedWardCode,
          street: street.trim(),
          note: addressNote.trim() || null,
          isDefault: addresses.length === 0,
        },
      })

      if (error || !data) {
        setAddressError('Không thể lưu địa chỉ. Vui lòng kiểm tra lại số điện thoại.')
      } else {
        setAddresses((prev) => [...prev, data])
        setSelectedAddressId(data.id)
        setShowNewAddressForm(false)
        setRecipientName('')
        setPhone('')
        setStreet('')
        setAddressNote('')
      }
    } catch {
      setAddressError('Lỗi kết nối khi lưu địa chỉ')
    } finally {
      setIsSavingAddress(false)
    }
  }

  // Handle Place Order
  const handlePlaceOrder = async () => {
    if (!selectedAddressId) {
      setOrderError('Vui lòng chọn hoặc thêm địa chỉ nhận hàng')
      return
    }

    setIsPlacingOrder(true)
    setOrderError(null)

    try {
      const { data, error } = await api.POST('/api/v1/checkout/orders', {
        headers: {
          'Idempotency-Key': idempotencyKey,
        },
        body: {
          addressId: selectedAddressId,
          paymentMethod: paymentMethod,
          couponCode: cart?.couponCode ?? null,
          note: orderNote.trim() || null,
        },
      })

      if (error || !data) {
        const msg = (error as any)?.detail || (error as any)?.title || 'Không thể tạo đơn hàng'
        setOrderError(msg)
      } else if (data.order?.id) {
        await refreshCart()
        if (paymentMethod === 'SEPAY_QR' && data.payment?.paymentCode) {
          navigate(`/payment/${data.payment.paymentCode}`)
        } else {
          navigate(`/orders/${data.order.id}/confirmed`)
        }
      }
    } catch {
      setOrderError('Lỗi kết nối khi gửi đơn hàng')
    } finally {
      setIsPlacingOrder(false)
    }
  }

  if (authLoading) {
    return (
      <div className="space-y-6 animate-pulse max-w-3xl mx-auto">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-48 w-full rounded-2xl" />
        <Skeleton className="h-48 w-full rounded-2xl" />
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="text-center py-20 bg-paper-2 rounded-2xl border border-rule max-w-lg mx-auto px-6 space-y-4">
        <div className="w-16 h-16 rounded-full bg-paper border border-rule flex items-center justify-center mx-auto text-leaf">
          <LogIn className="w-8 h-8 stroke-[1.5]" />
        </div>
        <h2 className="font-display font-semibold text-xl text-ink">
          Vui lòng đăng nhập để tiếp tục thanh toán
        </h2>
        <p className="text-xs sm:text-sm text-ink-2 max-w-md mx-auto">
          Đăng nhập để sử dụng địa chỉ đã lưu, tích lũy điểm thưởng và theo dõi trạng thái đơn hàng.
        </p>
        <div className="flex flex-col sm:flex-row justify-center gap-3 pt-2">
          <Button variant="primary" size="lg" onClick={() => navigate('/login')}>
            {vi.auth.loginTitle}
          </Button>
          <Button variant="outline" size="lg" onClick={() => navigate('/register')}>
            {vi.auth.registerTitle}
          </Button>
        </div>
      </div>
    )
  }

  const subtotal = Number(cart?.subtotal ?? 0)
  const discount = Number(cart?.couponDiscount ?? 0)
  const finalTotal = Math.max(0, subtotal - discount + shippingFee)

  return (
    <div className="space-y-8 max-w-5xl mx-auto">
      {/* Title */}
      <div>
        <h1 className="font-display font-bold text-2xl sm:text-3xl text-ink">
          {vi.checkout.title}
        </h1>
        <p className="text-xs text-ink-2 mt-1">
          Hoàn tất thông tin nhận hàng và chọn phương thức thanh toán
        </p>
      </div>

      {orderError && (
        <div className="p-4 rounded-xl bg-[#FDECE8] border border-[#FADCD5] text-xs text-tomato flex items-center gap-2">
          <AlertCircle className="w-4 h-4 shrink-0 stroke-[1.5]" />
          <span>{orderError}</span>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 items-start">
        {/* Left Column: Delivery Address & Payment Method */}
        <div className="lg:col-span-2 space-y-6">
          {/* Step 1: Delivery Address */}
          <section className="bg-paper-2 p-5 sm:p-6 rounded-2xl border border-rule space-y-4">
            <div className="flex items-center justify-between border-b border-rule pb-3">
              <h2 className="font-display font-semibold text-base text-ink flex items-center gap-2">
                <MapPin className="w-5 h-5 text-leaf stroke-[1.5]" />
                {vi.checkout.deliveryAddress}
              </h2>

              {!showNewAddressForm && addresses.length > 0 && (
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setShowNewAddressForm(true)}
                  className="text-xs h-8 flex items-center gap-1 text-leaf border-leaf"
                >
                  <Plus className="w-3.5 h-3.5 stroke-[1.5]" />
                  <span>Thêm địa chỉ mới</span>
                </Button>
              )}
            </div>

            {/* Saved Address Cards List */}
            {!showNewAddressForm && addresses.length > 0 && (
              <div className="space-y-3">
                {addresses.map((addr) => {
                  const isSelected = selectedAddressId === addr.id
                  return (
                    <div
                      key={addr.id}
                      onClick={() => setSelectedAddressId(addr.id)}
                      className={`p-4 rounded-xl border transition-all cursor-pointer ${
                        isSelected
                          ? 'border-leaf bg-paper ring-2 ring-leaf/20'
                          : 'border-rule bg-paper/60 hover:bg-paper'
                      }`}
                    >
                      <div className="flex items-start justify-between gap-2">
                        <div className="space-y-1">
                          <div className="flex items-center gap-2">
                            <span className="font-semibold text-xs text-ink">
                              {addr.recipientName}
                            </span>
                            <span className="text-xs text-ink-2">|</span>
                            <span className="text-xs font-mono text-ink-2">{addr.phone}</span>
                            {addr.isDefault && (
                              <span className="text-[10px] bg-leaf/10 text-leaf font-bold px-1.5 py-0.5 rounded-full">
                                Mặc định
                              </span>
                            )}
                          </div>
                          <p className="text-xs text-ink-2 leading-relaxed">
                            {addr.street}, {addr.wardName}, {addr.provinceName}
                          </p>
                          {addr.note && (
                            <p className="text-[11px] text-ink-2/80 italic">Ghi chú: {addr.note}</p>
                          )}
                        </div>

                        <div className="pt-0.5">
                          <div
                            className={`w-4 h-4 rounded-full border flex items-center justify-center ${
                              isSelected ? 'border-leaf bg-leaf text-paper' : 'border-rule'
                            }`}
                          >
                            {isSelected && <CheckCircle2 className="w-3.5 h-3.5 stroke-[2]" />}
                          </div>
                        </div>
                      </div>
                    </div>
                  )
                })}
              </div>
            )}

            {/* New Address Form (2-level VN dropdowns) */}
            {showNewAddressForm && (
              <form onSubmit={handleSaveAddress} className="space-y-4 pt-2">
                <h3 className="text-xs font-semibold text-ink uppercase tracking-wider">
                  Nhập địa chỉ giao hàng mới (Chuẩn hành chính 2 cấp)
                </h3>

                {addressError && (
                  <p className="text-xs text-tomato bg-[#FDECE8] p-2.5 rounded-lg border border-[#FADCD5]">
                    {addressError}
                  </p>
                )}

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <label className="text-xs font-semibold text-ink block mb-1">
                      Họ và tên người nhận <span className="text-tomato">*</span>
                    </label>
                    <Input
                      placeholder="Ví dụ: Nguyễn Văn A"
                      value={recipientName}
                      onChange={(e) => setRecipientName(e.target.value)}
                      className="text-xs h-9"
                      required
                    />
                  </div>
                  <div>
                    <label className="text-xs font-semibold text-ink block mb-1">
                      Số điện thoại <span className="text-tomato">*</span>
                    </label>
                    <Input
                      type="tel"
                      placeholder="Ví dụ: 0912345678"
                      value={phone}
                      onChange={(e) => setPhone(e.target.value)}
                      className="text-xs h-9 font-mono"
                      required
                    />
                  </div>
                </div>

                {/* 2-level administrative dropdowns */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <label className="text-xs font-semibold text-ink block mb-1">
                      Tỉnh / Thành phố <span className="text-tomato">*</span>
                    </label>
                    <Select
                      value={selectedProvinceCode}
                      onValueChange={(val) => {
                        setSelectedProvinceCode(val)
                      }}
                    >
                      <SelectTrigger className="h-9 text-xs">
                        <SelectValue placeholder="Chọn Tỉnh / Thành" />
                      </SelectTrigger>
                      <SelectContent>
                        {provinces.map((p) => (
                          <SelectItem key={p.code} value={p.code}>
                            {p.fullName || p.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  <div>
                    <label className="text-xs font-semibold text-ink block mb-1">
                      Phường / Xã / Thị trấn <span className="text-tomato">*</span>
                    </label>
                    <Select
                      value={selectedWardCode}
                      onValueChange={setSelectedWardCode}
                      disabled={!selectedProvinceCode || wards.length === 0}
                    >
                      <SelectTrigger className="h-9 text-xs">
                        <SelectValue placeholder="Chọn Phường / Xã" />
                      </SelectTrigger>
                      <SelectContent>
                        {wards.map((w) => (
                          <SelectItem key={w.code} value={w.code}>
                            {w.fullName || w.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div>
                  <label className="text-xs font-semibold text-ink block mb-1">
                    Địa chỉ cụ thể (Số nhà, ngõ ngách, tên đường){' '}
                    <span className="text-tomato">*</span>
                  </label>
                  <Input
                    placeholder="Ví dụ: Số 25 ngõ 120 Hoàng Quốc Việt"
                    value={street}
                    onChange={(e) => setStreet(e.target.value)}
                    className="text-xs h-9"
                    required
                  />
                </div>

                <div>
                  <label className="text-xs font-semibold text-ink block mb-1">
                    Ghi chú chỉ đường (Tùy chọn)
                  </label>
                  <Input
                    placeholder="Ví dụ: Gọi trước khi giao, giao giờ hành chính"
                    value={addressNote}
                    onChange={(e) => setAddressNote(e.target.value)}
                    className="text-xs h-9"
                  />
                </div>

                <div className="flex items-center justify-end gap-2 pt-2">
                  {addresses.length > 0 && (
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => setShowNewAddressForm(false)}
                      className="text-xs"
                    >
                      Hủy bỏ
                    </Button>
                  )}
                  <Button
                    type="submit"
                    variant="primary"
                    size="sm"
                    disabled={isSavingAddress}
                    className="text-xs font-semibold"
                  >
                    {isSavingAddress ? 'Đang lưu...' : 'Lưu địa chỉ'}
                  </Button>
                </div>
              </form>
            )}
          </section>

          {/* Step 2: Payment Method Selection */}
          <section className="bg-paper-2 p-5 sm:p-6 rounded-2xl border border-rule space-y-4">
            <h2 className="font-display font-semibold text-base text-ink flex items-center gap-2 border-b border-rule pb-3">
              <CreditCard className="w-5 h-5 text-leaf stroke-[1.5]" />
              {vi.checkout.paymentMethod}
            </h2>

            <div className="space-y-3">
              {/* Option 1: COD (Enabled) */}
              <label
                className={`flex items-start gap-4 p-4 rounded-xl border transition-all cursor-pointer ${
                  paymentMethod === 'COD'
                    ? 'border-leaf bg-paper ring-2 ring-leaf/20'
                    : 'border-rule bg-paper/60'
                }`}
              >
                <input
                  type="radio"
                  name="paymentMethod"
                  value="COD"
                  checked={paymentMethod === 'COD'}
                  onChange={() => setPaymentMethod('COD')}
                  className="mt-1 text-leaf focus:ring-leaf w-4 h-4 cursor-pointer"
                />
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <Banknote className="w-4 h-4 text-leaf stroke-[1.5]" />
                    <span className="font-semibold text-xs text-ink">
                      {vi.checkout.codTitle} (Thanh toán tiền mặt khi nhận hàng)
                    </span>
                    <span className="text-[10px] bg-leaf/10 text-leaf font-bold px-1.5 py-0.5 rounded-full">
                      Khuyên dùng
                    </span>
                  </div>
                  <p className="text-xs text-ink-2 leading-relaxed">
                    {vi.checkout.codDescription}
                  </p>
                </div>
              </label>

              {/* Option 2: VietQR / SePay */}
              <label
                className={`flex items-start gap-4 p-4 rounded-xl border transition-all cursor-pointer ${
                  paymentMethod === 'SEPAY_QR'
                    ? 'border-leaf bg-paper ring-2 ring-leaf/20'
                    : 'border-rule bg-paper/60'
                }`}
              >
                <input
                  type="radio"
                  name="paymentMethod"
                  value="SEPAY_QR"
                  checked={paymentMethod === 'SEPAY_QR'}
                  onChange={() => setPaymentMethod('SEPAY_QR')}
                  className="mt-1 text-leaf focus:ring-leaf w-4 h-4 cursor-pointer"
                />
                <div className="space-y-1.5 flex-1">
                  <div className="flex items-center gap-2">
                    <QrCode className="w-4 h-4 text-leaf stroke-[1.5]" />
                    <span className="font-semibold text-xs text-ink">
                      {vi.checkout.vietqrTitle} (Chuyển khoản SePay VietQR)
                    </span>
                    <span className="text-[10px] bg-leaf/10 text-leaf font-bold px-1.5 py-0.5 rounded-full">
                      Tự động xác nhận 24/7
                    </span>
                  </div>
                  <p className="text-xs text-ink-2 leading-relaxed">
                    Quét mã VietQR chuẩn Napas 24/7 qua bất kỳ ứng dụng ngân hàng nào. Đơn hàng tự động xác nhận tức thì mà không cần nhân viên gọi lại.
                  </p>
                </div>
              </label>
            </div>
          </section>

          {/* Delivery Note */}
          <section className="bg-paper-2 p-5 rounded-2xl border border-rule space-y-2">
            <label className="text-xs font-semibold text-ink block">
              Ghi chú cho đơn hàng (Tùy chọn)
            </label>
            <textarea
              rows={2}
              placeholder="Ví dụ: Giao trước 11h trưa, để tại quầy lễ tân nếu không có nhà..."
              value={orderNote}
              onChange={(e) => setOrderNote(e.target.value)}
              className="w-full rounded-md border border-rule bg-paper p-3 text-xs text-ink focus:outline-none focus:ring-2 focus:ring-leaf"
            />
          </section>
        </div>

        {/* Right Column: Order Summary Preview & Confirm Button */}
        <div className="space-y-6">
          <div className="bg-paper-2 p-5 rounded-2xl border border-rule space-y-4">
            <h3 className="font-display font-semibold text-base text-ink border-b border-rule pb-3">
              Đơn hàng ({cart?.items?.length ?? 0} món)
            </h3>

            {/* Quick items list */}
            <div className="space-y-2.5 max-h-60 overflow-y-auto pr-1">
              {cart?.items?.map((item) => (
                <div key={item.id} className="flex justify-between items-center text-xs gap-2">
                  <div className="truncate flex-1">
                    <span className="font-semibold text-ink">{item.name}</span>
                    <span className="text-ink-2 block text-[11px] tabular-nums">
                      x {item.quantity} {item.unit}
                    </span>
                  </div>
                  <span className="font-bold text-ink tabular-nums shrink-0">
                    {formatVnd(Number(item.lineSubtotal))}
                  </span>
                </div>
              ))}
            </div>

            <div className="border-t border-rule pt-3">
              <ReceiptSummary
                subtotal={subtotal}
                shippingFee={shippingFee}
                couponDiscount={discount > 0 ? discount : undefined}
                total={finalTotal}
              />
            </div>

            <Button
              variant="primary"
              size="lg"
              disabled={isPlacingOrder || !selectedAddressId || !cart?.items?.length}
              onClick={() => void handlePlaceOrder()}
              className="w-full font-semibold shadow-xs flex items-center justify-center gap-2 h-12 text-sm"
            >
              <Lock className="w-4 h-4 stroke-[1.5]" />
              <span>{isPlacingOrder ? 'Đang tạo đơn hàng...' : vi.checkout.placeOrder}</span>
            </Button>

            <div className="space-y-1.5 pt-1 text-[11px] text-ink-2">
              <p className="flex items-center gap-1.5">
                <Truck className="w-3.5 h-3.5 text-leaf stroke-[1.5]" />
                <span>Giao hàng tận nơi trong 2 giờ tại nội thành</span>
              </p>
              <p className="flex items-center gap-1.5">
                <ShieldCheck className="w-3.5 h-3.5 text-leaf stroke-[1.5]" />
                <span>Kiểm tra hàng trước khi thanh toán tiền mặt</span>
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
