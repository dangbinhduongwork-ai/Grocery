import React, { useEffect, useState, useCallback } from 'react'
import {
  Plus,
  RefreshCw,
  Mail,
  Phone,
  X,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { useAuth } from '@/shared/auth'
import { DataTable, Column } from '../components/DataTable'

interface StaffItem {
  userId: string
  employeeCode: string
  email: string
  fullName: string
  phone?: string | null
  position?: string | null
  status: string
  roles: string[]
  extraPermissions: string[]
}

export const AdminStaffPage: React.FC = () => {
  const { hasPermission } = useAuth()
  const [staffList, setStaffList] = useState<StaffItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isDialogOpen, setIsDialogOpen] = useState(false)

  // Form
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fullName, setFullName] = useState('')
  const [phone, setPhone] = useState('')
  const [employeeCode, setEmployeeCode] = useState('')
  const [position, setPosition] = useState('')
  const [role, setRole] = useState<'Staff' | 'Manager'>('Staff')

  const canManage = hasPermission('staff.manage')
  const canCreate = hasPermission('staff.create') || canManage

  const fetchStaff = useCallback(async () => {
    setIsLoading(true)
    try {
      const res = await api.GET('/api/v1/admin/staff', {
        params: { query: { pageSize: 100 } },
      })
      if (res.data?.items) {
        setStaffList(res.data.items as unknown as StaffItem[])
      } else {
        setStaffList([])
      }
    } catch {
      setStaffList([])
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void fetchStaff()
  }, [fetchStaff])

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!email.trim() || !password.trim() || !fullName.trim() || !employeeCode.trim()) return

    try {
      const res = await api.POST('/api/v1/admin/staff', {
        body: {
          email,
          password,
          fullName,
          phone: phone.trim() || null,
          employeeCode,
          position: position.trim() || null,
          role,
          extraPermissions: null,
        },
      })

      if (res.error) {
        alert((res.error as { detail?: string })?.detail ?? 'Tạo tài khoản nhân viên thất bại.')
      } else {
        setIsDialogOpen(false)
        setEmail('')
        setPassword('')
        setFullName('')
        setPhone('')
        setEmployeeCode('')
        setPosition('')
        await fetchStaff()
      }
    } catch {
      alert('Lỗi kết nối máy chủ.')
    }
  }

  const columns: Column<StaffItem>[] = [
    {
      key: 'employeeCode',
      title: 'Mã NV',
      sortable: true,
      render: s => <span className="font-mono text-xs font-semibold text-slate-700">{s.employeeCode}</span>,
    },
    {
      key: 'fullName',
      title: 'Họ và tên',
      sortable: true,
      render: s => (
        <div>
          <div className="font-semibold text-slate-900">{s.fullName}</div>
          <div className="text-xs text-slate-400">{s.position ?? 'Nhân viên'}</div>
        </div>
      ),
    },
    {
      key: 'contact',
      title: 'Liên hệ',
      render: s => (
        <div className="text-xs space-y-0.5 text-slate-500">
          <div className="flex items-center gap-1.5 font-mono">
            <Mail className="w-3.5 h-3.5 text-slate-400" />
            <span>{s.email}</span>
          </div>
          {s.phone && (
            <div className="flex items-center gap-1.5 font-mono">
              <Phone className="w-3.5 h-3.5 text-slate-400" />
              <span>{s.phone}</span>
            </div>
          )}
        </div>
      ),
    },
    {
      key: 'roles',
      title: 'Vai trò & Nhóm',
      render: s => (
        <div className="flex items-center gap-1 flex-wrap">
          {s.roles.map(r => (
            <span
              key={r}
              className={`px-2 py-0.5 rounded-full text-xs font-semibold ${
                r === 'Admin'
                  ? 'bg-purple-50 text-purple-700 border border-purple-200'
                  : r === 'Manager'
                  ? 'bg-indigo-50 text-indigo-700 border border-indigo-200'
                  : 'bg-slate-100 text-slate-700'
              }`}
            >
              {r}
            </span>
          ))}
          {s.extraPermissions.length > 0 && (
            <span className="text-[10px] text-slate-400 font-mono">
              (+{s.extraPermissions.length} quyền)
            </span>
          )}
        </div>
      ),
    },
    {
      key: 'status',
      title: 'Trạng thái',
      render: s => (
        <span
          className={`px-2 py-0.5 rounded-full text-xs font-semibold ${
            s.status === 'Active'
              ? 'bg-emerald-50 text-emerald-700'
              : 'bg-rose-50 text-rose-700 border border-rose-200'
          }`}
        >
          {s.status === 'Active' ? 'Đang làm việc' : 'Khóa'}
        </span>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 font-display">Đội ngũ nhân sự</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Quản lý tài khoản vận hành, phân quyền vai trò (Staff, Manager, Admin) và gán quyền hạn bổ sung.
          </p>
        </div>
        <div className="flex items-center gap-2">
          {canCreate && (
            <button
              type="button"
              onClick={() => setIsDialogOpen(true)}
              className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl bg-leaf text-white text-xs font-semibold hover:bg-leaf-2 transition-colors shadow-xs"
            >
              <Plus className="w-4 h-4" strokeWidth={1.5} />
              Thêm nhân viên mới
            </button>
          )}
          <button
            type="button"
            onClick={() => void fetchStaff()}
            disabled={isLoading}
            className="p-2 rounded-xl border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 transition-colors shadow-2xs"
            title="Làm mới"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} strokeWidth={1.5} />
          </button>
        </div>
      </div>

      <DataTable
        data={staffList}
        columns={columns}
        keyExtractor={s => s.userId}
        searchPlaceholder="Tìm theo mã nhân viên, tên hoặc email..."
        searchableKey={s => `${s.employeeCode} ${s.fullName} ${s.email}`}
        pageSize={15}
        isLoading={isLoading}
        emptyMessage="Chưa có nhân viên nào"
      />

      {isDialogOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-xs">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-200 space-y-4">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <h2 className="text-base font-bold text-slate-900 font-display">Thêm nhân viên mới</h2>
              <button type="button" onClick={() => setIsDialogOpen(false)} className="text-slate-400 hover:text-slate-600">
                <X className="w-5 h-5" strokeWidth={1.5} />
              </button>
            </div>
            <form onSubmit={handleCreate} className="space-y-3 text-xs">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Mã nhân viên *</label>
                  <input
                    type="text"
                    required
                    value={employeeCode}
                    onChange={e => setEmployeeCode(e.target.value)}
                    placeholder="NV-001"
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Chức danh</label>
                  <input
                    type="text"
                    value={position}
                    onChange={e => setPosition(e.target.value)}
                    placeholder="Kho, Thu ngân..."
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
              </div>

              <div>
                <label className="block font-semibold text-slate-700 mb-1">Họ và tên *</label>
                <input
                  type="text"
                  required
                  value={fullName}
                  onChange={e => setFullName(e.target.value)}
                  placeholder="Nguyễn Văn A"
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>

              <div>
                <label className="block font-semibold text-slate-700 mb-1">Email đăng nhập *</label>
                <input
                  type="email"
                  required
                  value={email}
                  onChange={e => setEmail(e.target.value)}
                  placeholder="nv.a@groceryhub.vn"
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>

              <div>
                <label className="block font-semibold text-slate-700 mb-1">Mật khẩu khởi tạo *</label>
                <input
                  type="password"
                  required
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Số điện thoại</label>
                  <input
                    type="text"
                    value={phone}
                    onChange={e => setPhone(e.target.value)}
                    placeholder="090..."
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl font-mono focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  />
                </div>
                <div>
                  <label className="block font-semibold text-slate-700 mb-1">Vai trò</label>
                  <select
                    value={role}
                    onChange={e => setRole(e.target.value as 'Staff' | 'Manager')}
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
                  >
                    <option value="Staff">Nhân viên (Staff)</option>
                    {canManage && <option value="Manager">Quản lý (Manager)</option>}
                  </select>
                </div>
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
                  Tạo tài khoản
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
