import React, { useEffect, useState, useCallback } from 'react'
import {
  Shield,
  RefreshCw,
  Save,
  Check,
  AlertCircle,
  CheckCircle2,
} from 'lucide-react'
import { api } from '@/shared/api/client'

interface RoleMatrixItem {
  roleId: string
  name: string
  isImmutableAdmin: boolean
  permissions: string[]
}

interface PermissionItem {
  code: string
  group: string
  description: string
  allowUserGrant: boolean
}

export const AdminRolesPage: React.FC = () => {
  const [roles, setRoles] = useState<RoleMatrixItem[]>([])
  const [permissions, setPermissions] = useState<PermissionItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null)
  const [rolePermissions, setRolePermissions] = useState<Record<string, Set<string>>>({})
  const [successMsg, setSuccessMsg] = useState<string | null>(null)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  const fetchData = useCallback(async () => {
    setIsLoading(true)
    try {
      const [rolesRes, permsRes] = await Promise.all([
        api.GET('/api/v1/admin/roles'),
        api.GET('/api/v1/admin/permissions'),
      ])

      if (rolesRes.data && permsRes.data) {
        const rList = rolesRes.data as unknown as RoleMatrixItem[]
        const pList = permsRes.data as unknown as PermissionItem[]
        setRoles(rList)
        setPermissions(pList)

        const map: Record<string, Set<string>> = {}
        for (const r of rList) {
          map[r.roleId] = new Set(r.permissions)
        }
        setRolePermissions(map)

        if (rList.length > 0 && !selectedRoleId) {
          // Select Manager or first non-immutable role
          const target = rList.find(r => r.name === 'Manager') ?? rList[0]
          setSelectedRoleId(target.roleId)
        }
      }
    } catch {
      setErrorMsg('Không thể tải ma trận phân quyền.')
    } finally {
      setIsLoading(false)
    }
  }, [selectedRoleId])

  useEffect(() => {
    void fetchData()
  }, [fetchData])

  const activeRole = roles.find(r => r.roleId === selectedRoleId)

  const togglePermission = (permCode: string) => {
    if (!selectedRoleId || activeRole?.isImmutableAdmin) return

    setRolePermissions(prev => {
      const currentSet = new Set(prev[selectedRoleId] ?? [])
      if (currentSet.has(permCode)) {
        currentSet.delete(permCode)
      } else {
        currentSet.add(permCode)
      }
      return {
        ...prev,
        [selectedRoleId]: currentSet,
      }
    })
  }

  const handleSave = async () => {
    if (!selectedRoleId || !activeRole) return
    setIsSaving(true)
    setErrorMsg(null)
    setSuccessMsg(null)

    try {
      const permsArray = Array.from(rolePermissions[selectedRoleId] ?? [])
      const res = await api.PUT('/api/v1/admin/roles/{id}/permissions', {
        params: { path: { id: selectedRoleId } },
        body: { permissions: permsArray },
      })

      if (res.error) {
        setErrorMsg((res.error as { detail?: string })?.detail ?? 'Cập nhật phân quyền thất bại.')
      } else {
        setSuccessMsg(`Đã cập nhật phân quyền cho vai trò ${activeRole.name}!`)
        await fetchData()
      }
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : 'Lỗi hệ thống.')
    } finally {
      setIsSaving(false)
    }
  }

  // Group permissions by group
  const groupedPermissions = permissions.reduce((acc, p) => {
    if (!acc[p.group]) acc[p.group] = []
    acc[p.group].push(p)
    return acc
  }, {} as Record<string, PermissionItem[]>)

  if (isLoading) {
    return <div className="h-64 bg-white rounded-2xl animate-pulse" />
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 font-display">Ma trận phân quyền (RBAC)</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Quản trị quyền hạn chi tiết theo vai trò (Role-Permission Matrix) với kiểm soát ghi nhật ký kiểm toán tự động.
          </p>
        </div>
        <div className="flex items-center gap-2">
          {activeRole && !activeRole.isImmutableAdmin && (
            <button
              type="button"
              disabled={isSaving}
              onClick={handleSave}
              className="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-leaf text-white text-xs font-semibold hover:bg-leaf-2 transition-colors shadow-xs disabled:opacity-50"
            >
              <Save className="w-4 h-4" strokeWidth={1.5} />
              {isSaving ? 'Đang lưu...' : 'Lưu quyền hạn'}
            </button>
          )}
          <button
            type="button"
            onClick={() => void fetchData()}
            className="p-2 rounded-xl border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 transition-colors shadow-2xs"
            title="Làm mới"
          >
            <RefreshCw className="w-4 h-4" strokeWidth={1.5} />
          </button>
        </div>
      </div>

      {errorMsg && (
        <div className="p-3 bg-rose-50 border border-rose-200 text-rose-700 text-xs rounded-xl flex items-center gap-2">
          <AlertCircle className="w-4 h-4 shrink-0" strokeWidth={1.5} />
          <span>{errorMsg}</span>
        </div>
      )}

      {successMsg && (
        <div className="p-3 bg-emerald-50 border border-emerald-200 text-emerald-700 text-xs rounded-xl flex items-center gap-2">
          <CheckCircle2 className="w-4 h-4 shrink-0" strokeWidth={1.5} />
          <span>{successMsg}</span>
        </div>
      )}

      {/* Role Selector Tabs */}
      <div className="flex items-center gap-2 border-b border-slate-200">
        {roles.map(r => (
          <button
            key={r.roleId}
            type="button"
            onClick={() => setSelectedRoleId(r.roleId)}
            className={`flex items-center gap-2 px-4 py-2.5 border-b-2 text-xs font-bold transition-colors ${
              selectedRoleId === r.roleId
                ? 'border-leaf text-leaf'
                : 'border-transparent text-slate-500 hover:text-slate-800'
            }`}
          >
            <Shield className="w-4 h-4" strokeWidth={1.5} />
            <span>Vai trò: {r.name}</span>
            {r.isImmutableAdmin && (
              <span className="text-[10px] px-1.5 py-0.2 rounded-sm bg-purple-50 text-purple-700 border border-purple-200 font-mono">
                Bất biến
              </span>
            )}
          </button>
        ))}
      </div>

      {activeRole?.isImmutableAdmin && (
        <div className="p-3 bg-purple-50 border border-purple-200 text-purple-800 text-xs rounded-xl flex items-center gap-2">
          <Shield className="w-4 h-4 shrink-0" strokeWidth={1.5} />
          <span>
            Vai trò <b>Admin (Toàn quyền)</b> sở hữu toàn bộ 29 quyền hệ thống và không được phép loại bỏ bất kỳ quyền nào (Quy tắc bảo mật T-ID-03).
          </span>
        </div>
      )}

      {/* Permissions Groups Accordion / Table */}
      <div className="space-y-6">
        {Object.entries(groupedPermissions).map(([groupName, perms]) => (
          <div key={groupName} className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-2xs">
            <div className="p-3.5 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
              <h2 className="text-xs font-bold uppercase tracking-wider text-slate-800 font-display">
                Phân hệ: {groupName}
              </h2>
              <span className="text-xs text-slate-400 font-medium">{perms.length} quyền</span>
            </div>

            <div className="divide-y divide-slate-100">
              {perms.map(p => {
                const isChecked = Boolean(
                  selectedRoleId && rolePermissions[selectedRoleId]?.has(p.code)
                )

                return (
                  <div
                    key={p.code}
                    onClick={() => !activeRole?.isImmutableAdmin && togglePermission(p.code)}
                    className={`p-3.5 flex items-center justify-between transition-colors ${
                      activeRole?.isImmutableAdmin
                        ? 'cursor-default opacity-85'
                        : 'cursor-pointer hover:bg-slate-50'
                    }`}
                  >
                    <div className="flex items-center gap-3">
                      <div
                        className={`w-5 h-5 rounded-md border flex items-center justify-center transition-colors ${
                          isChecked
                            ? 'bg-leaf border-leaf text-white'
                            : 'border-slate-300 bg-white'
                        }`}
                      >
                        {isChecked && <Check className="w-3.5 h-3.5 stroke-[3]" />}
                      </div>
                      <div>
                        <div className="text-xs font-semibold text-slate-800 font-mono">{p.code}</div>
                        <div className="text-xs text-slate-500 mt-0.5">{p.description}</div>
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
