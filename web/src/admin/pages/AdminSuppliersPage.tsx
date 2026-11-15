import React, { useEffect, useState, useCallback } from 'react'
import {
  Building2,
  Plus,
  RefreshCw,
  Phone,
  Mail,
  X,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { useAuth } from '@/shared/auth'
import { DataTable, Column } from '../components/DataTable'

interface SupplierItem {
  id: string
  code: string
  name: string
  phone?: string | null
  email?: string | null
  address?: string | null
  taxCode?: string | null
}

export const AdminSuppliersPage: React.FC = () => {
  const { hasPermission } = useAuth()
  const [suppliers, setSuppliers] = useState<SupplierItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isDialogOpen, setIsDialogOpen] = useState(false)

  // Form
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [phone, setPhone] = useState('')
  const [email, setEmail] = useState('')
  const [address, setAddress] = useState('')
  const [taxCode, setTaxCode] = useState('')

  const canWrite = hasPermission('purchasing.write')

  const fetchSuppliers = useCallback(async () => {
    setIsLoading(true)
    try {
      const res = await api.GET('/api/v1/admin/suppliers', {
        params: { query: { pageSize: 100 } },
      })
      if (res.data?.items) {
        setSuppliers(res.data.items as unknown as SupplierItem[])
      } else {
        setSuppliers([])
      }
    } catch {
      setSuppliers([])
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void fetchSuppliers()
  }, [fetchSuppliers])

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!code.trim() || !name.trim()) return

    try {
      await api.POST('/api/v1/admin/suppliers', {
        body: {
          code,
          name,
          contactPerson: null,
          phone: phone.trim() || '—',
          email: email.trim() || null,
          address: address.trim() || null,
          taxCode: taxCode.trim() || null,
          note: null,
        },
      })
      setIsDialogOpen(false)
      setCode('')
      setName('')
      setPhone('')
      setEmail('')
      setAddress('')
      setTaxCode('')
      await fetchSuppliers()
    } catch {
      alert('Không thể tạo nhà cung cấp.')
    }
  }

  const columns: Column<SupplierItem>[] = [
    {
      key: 'code',
      title: 'Mã NCC',
      sortable: true,
      render: s => <span className="font-mono text-xs font-semibold text-slate-700">{s.code}</span>,
    },
    {
      key: 'name',
      title: 'Tên nhà cung cấp',
      sortable: true,
      render: s => (
        <div className="font-semibold text-slate-800 flex items-center gap-2">
          <Building2 className="w-4 h-4 text-indigo-600 shrink-0" strokeWidth={1.5} />
          <span>{s.name}</span>
        </div>
      ),
    },
    {
      key: 'contact',
      title: 'Liên hệ',
      render: s => (
        <div className="text-xs space-y-0.5 text-slate-500">
          {s.phone && (
            <div className="flex items-center gap-1.5 font-mono">
              <Phone className="w-3 h-3 text-slate-400" /> {s.phone}
            </div>
          )}
          {s.email && (
            <div className="flex items-center gap-1.5">
              <Mail className="w-3 h-3 text-slate-400" /> {s.email}
            </div>
          )}
        </div>
      ),
    },
    {
      key: 'address',
      title: 'Địa chỉ & MST',
      render: s => (
        <div className="text-xs text-slate-500">
          {s.address && <div>{s.address}</div>}
          {s.taxCode && <div className="font-mono text-[11px] text-slate-400">MST: {s.taxCode}</div>}
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 font-display">Nhà cung cấp</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Quản lý đối tác phân phối, thông tin hợp đồng và mã số thuế.
          </p>
        </div>
        <div className="flex items-center gap-2">
          {canWrite && (
            <button
              type="button"
              onClick={() => setIsDialogOpen(true)}
              className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl bg-leaf text-white text-xs font-semibold hover:bg-leaf-2 transition-colors shadow-xs"
            >
              <Plus className="w-4 h-4" strokeWidth={1.5} />
              Thêm nhà cung cấp
            </button>
          )}
          <button
            type="button"
            onClick={() => void fetchSuppliers()}
            disabled={isLoading}
            className="p-2 rounded-xl border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 transition-colors shadow-2xs"
            title="Làm mới"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} strokeWidth={1.5} />
          </button>
        </div>
      </div>

      <DataTable
        data={suppliers}
        columns={columns}
        keyExtractor={s => s.id}
        searchPlaceholder="Tìm theo mã hoặc tên nhà cung cấp..."
        searchableKey={s => `${s.code} ${s.name} ${s.phone ?? ''}`}
        pageSize={15}
        isLoading={isLoading}
        emptyMessage="Chưa có nhà cung cấp nào"
      />

      {isDialogOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-xs">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-200 space-y-4">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <h2 className="text-base font-bold text-slate-900 font-display">Thêm nhà cung cấp mới</h2>
              <button type="button" onClick={() => setIsDialogOpen(false)} className="text-slate-400 hover:text-slate-600">
                <X className="w-5 h-5" strokeWidth={1.5} />
              </button>
            </div>
            <form onSubmit={handleCreate} className="space-y-3 text-xs">
              <div>
                <label className="block font-semibold text-slate-700 mb-1">Mã NCC *</label>
                <input
                  type="text"
                  required
                  value={code}
                  onChange={e => setCode(e.target.value)}
                  placeholder="NCC-CP, NCC-VINAMILK..."
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>
              <div>
                <label className="block font-semibold text-slate-700 mb-1">Tên nhà cung cấp *</label>
                <input
                  type="text"
                  required
                  value={name}
                  onChange={e => setName(e.target.value)}
                  placeholder="Công ty Cổ phần..."
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Số điện thoại</label>
                  <input
                    type="text"
                    value={phone}
                    onChange={e => setPhone(e.target.value)}
                    placeholder="028..."
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Mã số thuế</label>
                  <input
                    type="text"
                    value={taxCode}
                    onChange={e => setTaxCode(e.target.value)}
                    placeholder="030..."
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
              </div>
              <div>
                <label className="block font-semibold text-slate-700 mb-1">Email</label>
                <input
                  type="email"
                  value={email}
                  onChange={e => setEmail(e.target.value)}
                  placeholder="contact@ncc.vn"
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>
              <div>
                <label className="block font-semibold text-slate-700 mb-1">Địa chỉ trụ sở</label>
                <input
                  type="text"
                  value={address}
                  onChange={e => setAddress(e.target.value)}
                  placeholder="Số nhà, đường, quận/huyện..."
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>
              <div className="flex items-center justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setIsDialogOpen(false)}
                  className="px-4 py-2 rounded-xl border border-slate-200 text-slate-600 hover:bg-slate-50"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-5 py-2 rounded-xl bg-leaf text-white font-semibold hover:bg-leaf-2 shadow-xs"
                >
                  Lưu nhà cung cấp
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
