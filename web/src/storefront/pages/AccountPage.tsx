import React, { useEffect, useState } from 'react'
import { api } from '@/shared/api/client'
import type { CustomerAddress, ProvinceItem, WardItem } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/AuthContext'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/shared/ui/Select'
import { Skeleton } from '@/shared/ui/Skeleton'
import { useRouter } from '@/app/router'
import {
  User,
  MapPin,
  Plus,
  Trash2,
  Mail,
  Phone,
  Shield,
  LogOut,
} from 'lucide-react'

export const AccountPage: React.FC = () => {
  const { navigate } = useRouter()
  const { user, isAuthenticated, isLoading: authLoading, logout } = useAuth()

  const [addresses, setAddresses] = useState<CustomerAddress[]>([])
  const [isLoadingAddresses, setIsLoadingAddresses] = useState<boolean>(true)
  const [showAddModal, setShowAddModal] = useState<boolean>(false)

  // 2-level VN Location State
  const [provinces, setProvinces] = useState<ProvinceItem[]>([])
  const [wards, setWards] = useState<WardItem[]>([])
  const [recipientName, setRecipientName] = useState('')
  const [phone, setPhone] = useState('')
  const [selectedProvinceCode, setSelectedProvinceCode] = useState('')
  const [selectedWardCode, setSelectedWardCode] = useState('')
  const [street, setStreet] = useState('')
  const [addressNote, setAddressNote] = useState('')
  const [isSaving, setIsSaving] = useState(false)
  const [addressError, setAddressError] = useState<string | null>(null)

  const fetchAddresses = async () => {
    setIsLoadingAddresses(true)
    try {
      const { data } = await api.GET('/api/v1/me/addresses')
      if (data) {
        setAddresses(data)
      }
    } catch {
      // silent
    } finally {
      setIsLoadingAddresses(false)
    }
  }

  useEffect(() => {
    if (isAuthenticated) {
      void fetchAddresses()
    }
  }, [isAuthenticated])

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

  const handleSetDefault = async (addressId: string) => {
    try {
      await api.PUT('/api/v1/me/addresses/{id}/default', {
        params: { path: { id: addressId } },
      })
      await fetchAddresses()
    } catch {
      // silent
    }
  }

  const handleDeleteAddress = async (addressId: string) => {
    try {
      await api.DELETE('/api/v1/me/addresses/{id}', {
        params: { path: { id: addressId } },
      })
      await fetchAddresses()
    } catch {
      // silent
    }
  }

  const handleCreateAddress = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!recipientName || !phone || !selectedProvinceCode || !selectedWardCode || !street) {
      setAddressError('Vui lòng điền đủ các thông tin bắt buộc.')
      return
    }

    setIsSaving(true)
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
        setAddressError('Không thể tạo địa chỉ mới.')
      } else {
        setShowAddModal(false)
        setRecipientName('')
        setPhone('')
        setStreet('')
        setAddressNote('')
        await fetchAddresses()
      }
    } catch {
      setAddressError('Lỗi kết nối khi lưu địa chỉ')
    } finally {
      setIsSaving(false)
    }
  }

  if (authLoading) {
    return (
      <div className="space-y-6 max-w-4xl mx-auto animate-pulse">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-48 w-full rounded-2xl" />
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="text-center py-20 bg-paper-2 rounded-2xl border border-rule max-w-lg mx-auto px-4">
        <User className="w-12 h-12 text-leaf mx-auto mb-3 stroke-[1.5]" />
        <h2 className="font-display font-semibold text-lg text-ink mb-1">
          Vui lòng đăng nhập
        </h2>
        <p className="text-xs text-ink-2 mb-6">
          Đăng nhập để xem thông tin cá nhân và quản lý sổ địa chỉ giao hàng.
        </p>
        <Button variant="primary" onClick={() => navigate('/login')}>
          Đăng nhập ngay
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-8 max-w-4xl mx-auto">
      {/* Page Title */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="font-display font-bold text-2xl sm:text-3xl text-ink">
            Tài khoản của tôi
          </h1>
          <p className="text-xs text-ink-2 mt-1">
            Quản lý thông tin hồ sơ và sổ địa chỉ giao hàng
          </p>
        </div>

        <Button
          variant="outline"
          size="sm"
          onClick={() => void logout()}
          className="text-xs text-tomato border-tomato hover:bg-tomato/10 flex items-center gap-1.5"
        >
          <LogOut className="w-3.5 h-3.5 stroke-[1.5]" />
          <span>Đăng xuất</span>
        </Button>
      </div>

      {/* User Info Overview Card */}
      <div className="bg-paper-2 p-6 rounded-2xl border border-rule space-y-4">
        <div className="flex items-center gap-4">
          <div className="w-16 h-16 rounded-2xl bg-leaf text-paper flex items-center justify-center font-display font-bold text-2xl shadow-xs">
            {user?.fullName?.charAt(0) || user?.email?.charAt(0) || 'U'}
          </div>
          <div className="space-y-1">
            <h2 className="font-display font-semibold text-lg text-ink">
              {user?.fullName || 'Khách hàng thân thiết'}
            </h2>
            <div className="flex flex-wrap items-center gap-3 text-xs text-ink-2">
              <span className="flex items-center gap-1">
                <Mail className="w-3.5 h-3.5 stroke-[1.5]" />
                {user?.email}
              </span>
              {user?.phone && (
                <span className="flex items-center gap-1 font-mono">
                  <Phone className="w-3.5 h-3.5 stroke-[1.5]" />
                  {user.phone}
                </span>
              )}
            </div>
          </div>
        </div>

        <div className="pt-2 border-t border-rule flex items-center gap-2 text-xs text-ink-2">
          <Shield className="w-3.5 h-3.5 text-leaf stroke-[1.5]" />
          <span>
            Vai trò: <strong className="text-ink">{user?.roles?.join(', ') || 'Customer'}</strong>
          </span>
        </div>
      </div>

      {/* Address Book Section */}
      <div className="bg-paper-2 p-6 rounded-2xl border border-rule space-y-5">
        <div className="flex items-center justify-between border-b border-rule pb-3">
          <div>
            <h3 className="font-display font-semibold text-base text-ink flex items-center gap-2">
              <MapPin className="w-5 h-5 text-leaf stroke-[1.5]" /> Sổ địa chỉ nhận hàng
            </h3>
            <p className="text-xs text-ink-2 mt-0.5">
              Hệ thống lưu tối đa 10 địa chỉ giao hàng thường dùng
            </p>
          </div>

          <Button
            variant="outline"
            size="sm"
            onClick={() => setShowAddModal(true)}
            className="text-xs flex items-center gap-1.5 text-leaf border-leaf hover:bg-leaf/10 font-semibold"
          >
            <Plus className="w-3.5 h-3.5 stroke-[1.5]" />
            <span>Thêm địa chỉ</span>
          </Button>
        </div>

        {/* Addresses List */}
        {isLoadingAddresses ? (
          <div className="space-y-3">
            <Skeleton className="h-20 w-full rounded-xl" />
            <Skeleton className="h-20 w-full rounded-xl" />
          </div>
        ) : addresses.length === 0 ? (
          <div className="text-center py-10 bg-paper rounded-xl border border-rule text-xs text-ink-2">
            <p className="mb-2">Bạn chưa có địa chỉ nhận hàng nào được lưu.</p>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowAddModal(true)}
              className="text-xs"
            >
              Thêm địa chỉ đầu tiên
            </Button>
          </div>
        ) : (
          <div className="space-y-3">
            {addresses.map((addr) => (
              <div
                key={addr.id}
                className="p-4 rounded-xl border border-rule bg-paper flex flex-col sm:flex-row sm:items-center justify-between gap-4 text-xs"
              >
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="font-semibold text-ink">{addr.recipientName}</span>
                    <span className="text-ink-2">|</span>
                    <span className="font-mono text-ink-2">{addr.phone}</span>
                    {addr.isDefault && (
                      <span className="text-[10px] bg-leaf/10 text-leaf font-bold px-2 py-0.5 rounded-full">
                        Mặc định
                      </span>
                    )}
                  </div>
                  <p className="text-ink-2 leading-relaxed">
                    {addr.street}, {addr.wardName}, {addr.provinceName}
                  </p>
                  {addr.note && (
                    <p className="text-[11px] text-ink-2/80 italic">Ghi chú: {addr.note}</p>
                  )}
                </div>

                <div className="flex items-center gap-2 shrink-0">
                  {!addr.isDefault && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => void handleSetDefault(addr.id)}
                      className="text-xs h-8"
                    >
                      Đặt làm mặc định
                    </Button>
                  )}
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => void handleDeleteAddress(addr.id)}
                    className="text-xs h-8 text-tomato border-tomato hover:bg-tomato/10"
                  >
                    <Trash2 className="w-3.5 h-3.5 stroke-[1.5]" />
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Add Address Modal */}
      {showAddModal && (
        <div className="fixed inset-0 z-50 bg-ink/40 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-paper-2 rounded-2xl border border-rule max-w-lg w-full p-6 shadow-xl space-y-4">
            <h3 className="font-display font-semibold text-base text-ink">
              Thêm địa chỉ giao hàng mới
            </h3>

            {addressError && (
              <p className="text-xs text-tomato bg-[#FDECE8] p-2.5 rounded-lg border border-[#FADCD5]">
                {addressError}
              </p>
            )}

            <form onSubmit={handleCreateAddress} className="space-y-3">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="text-xs font-semibold text-ink block mb-1">
                    Họ và tên người nhận <span className="text-tomato">*</span>
                  </label>
                  <Input
                    placeholder="Nguyễn Văn A"
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
                    placeholder="0912345678"
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
                    className="text-xs h-9 font-mono"
                    required
                  />
                </div>
              </div>

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
                    Phường / Xã <span className="text-tomato">*</span>
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
                  Địa chỉ cụ thể (Số nhà, đường phố) <span className="text-tomato">*</span>
                </label>
                <Input
                  placeholder="Ví dụ: 120 Hoàng Quốc Việt"
                  value={street}
                  onChange={(e) => setStreet(e.target.value)}
                  className="text-xs h-9"
                  required
                />
              </div>

              <div>
                <label className="text-xs font-semibold text-ink block mb-1">
                  Ghi chú giao hàng
                </label>
                <Input
                  placeholder="Ví dụ: Giao giờ hành chính"
                  value={addressNote}
                  onChange={(e) => setAddressNote(e.target.value)}
                  className="text-xs h-9"
                />
              </div>

              <div className="flex items-center justify-end gap-2 pt-3 border-t border-rule">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setShowAddModal(false)}
                  className="text-xs"
                >
                  Hủy
                </Button>
                <Button
                  type="submit"
                  variant="primary"
                  size="sm"
                  disabled={isSaving}
                  className="text-xs font-semibold"
                >
                  {isSaving ? 'Đang lưu...' : 'Lưu địa chỉ'}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
