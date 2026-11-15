import React, { useEffect, useState, useCallback } from 'react'
import {
  RefreshCw,
  Eye,
  X,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { formatDateTime } from '@/shared/utils/dateTime'
import { DataTable, Column } from '../components/DataTable'

interface AuditLogItem {
  id: number
  at: string
  actorUserId?: string | null
  actorRole?: string | null
  action: string
  entityType: string
  entityId: string
  beforeJson?: string | null
  afterJson?: string | null
  ip?: string | null
  correlationId?: string | null
}

export const AdminAuditLogsPage: React.FC = () => {
  const [logs, setLogs] = useState<AuditLogItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [selectedLog, setSelectedLog] = useState<AuditLogItem | null>(null)

  const fetchLogs = useCallback(async () => {
    setIsLoading(true)
    try {
      const res = await api.GET('/api/v1/admin/audit-logs', {
        params: { query: { pageSize: 100 } },
      })
      const data = res.data as { items?: AuditLogItem[] } | undefined
      if (data?.items) {
        setLogs(data.items)
      } else {
        setLogs([])
      }
    } catch {
      setLogs([])
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void fetchLogs()
  }, [fetchLogs])

  const columns: Column<AuditLogItem>[] = [
    {
      key: 'at',
      title: 'Thời gian',
      sortable: true,
      render: l => (
        <span className="font-mono text-xs text-slate-500 whitespace-nowrap">
          {formatDateTime(l.at)}
        </span>
      ),
    },
    {
      key: 'actor',
      title: 'Tác tử (Actor)',
      render: l => (
        <div className="text-xs">
          <div className="font-semibold text-slate-800">{l.actorRole ?? 'Hệ thống'}</div>
          {l.actorUserId && (
            <div className="text-[10px] text-slate-400 font-mono truncate max-w-[120px]">
              {l.actorUserId}
            </div>
          )}
        </div>
      ),
    },
    {
      key: 'action',
      title: 'Hành động',
      sortable: true,
      render: l => (
        <span className="font-mono text-xs font-bold text-slate-800 bg-slate-100 px-2 py-0.5 rounded-md">
          {l.action}
        </span>
      ),
    },
    {
      key: 'entityType',
      title: 'Thực thể',
      sortable: true,
      render: l => (
        <div className="text-xs">
          <span className="font-semibold text-slate-700">{l.entityType}</span>
          <span className="block text-[10px] font-mono text-slate-400 truncate max-w-[140px]">
            {l.entityId}
          </span>
        </div>
      ),
    },
    {
      key: 'correlationId',
      title: 'Correlation ID',
      render: l => (
        <span className="font-mono text-[11px] text-slate-400">
          {l.correlationId ? l.correlationId.slice(0, 8) + '...' : '—'}
        </span>
      ),
    },
    {
      key: 'actions',
      title: 'Chi tiết',
      className: 'text-right',
      render: l => (
        <button
          type="button"
          onClick={() => setSelectedLog(l)}
          className="p-1.5 rounded-lg border border-slate-200 text-slate-600 hover:text-leaf hover:bg-leaf/5 transition-colors"
          title="Xem chi tiết Before / After JSON"
        >
          <Eye className="w-4 h-4" strokeWidth={1.5} />
        </button>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 font-display">Nhật ký kiểm toán (Audit Logs)</h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Truy vết toàn bộ biến động dữ liệu nhạy cảm: thay đổi giá, xuất nhập kho, phân quyền và thao tác bảo mật.
          </p>
        </div>
        <button
          type="button"
          onClick={() => void fetchLogs()}
          disabled={isLoading}
          className="p-2 rounded-xl border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 transition-colors shadow-2xs"
          title="Làm mới"
        >
          <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} strokeWidth={1.5} />
        </button>
      </div>

      <DataTable
        data={logs}
        columns={columns}
        keyExtractor={l => String(l.id)}
        searchPlaceholder="Tìm theo hành động, loại thực thể hoặc ID..."
        searchableKey={l => `${l.action} ${l.entityType} ${l.entityId} ${l.actorRole ?? ''}`}
        pageSize={15}
        isLoading={isLoading}
        emptyMessage="Chưa có bản ghi nhật ký kiểm toán nào"
      />

      {selectedLog && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-xs">
          <div className="bg-white rounded-2xl max-w-2xl w-full p-6 shadow-2xl border border-slate-200 space-y-4 max-h-[85vh] flex flex-col">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <div>
                <h2 className="text-base font-bold text-slate-900 font-display">
                  Chi tiết bản ghi kiểm toán #{selectedLog.id}
                </h2>
                <p className="text-xs text-slate-400">
                  {selectedLog.action} &bull; {selectedLog.entityType} ({selectedLog.entityId})
                </p>
              </div>
              <button
                type="button"
                onClick={() => setSelectedLog(null)}
                className="text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" strokeWidth={1.5} />
              </button>
            </div>

            <div className="flex-1 overflow-y-auto space-y-4 text-xs">
              <div className="grid grid-cols-2 gap-3 p-3 bg-slate-50 rounded-xl font-mono text-[11px]">
                <div>Thời gian: {formatDateTime(selectedLog.at)}</div>
                <div>IP: {selectedLog.ip ?? 'N/A'}</div>
                <div>Tác tử: {selectedLog.actorRole} ({selectedLog.actorUserId})</div>
                <div>Trace ID: {selectedLog.correlationId ?? 'N/A'}</div>
              </div>

              {selectedLog.beforeJson && (
                <div>
                  <h3 className="font-semibold text-slate-700 mb-1">Dữ liệu trước thay đổi (Before JSON):</h3>
                  <pre className="p-3 bg-slate-900 text-slate-100 rounded-xl overflow-x-auto font-mono text-[11px]">
                    {selectedLog.beforeJson}
                  </pre>
                </div>
              )}

              {selectedLog.afterJson && (
                <div>
                  <h3 className="font-semibold text-slate-700 mb-1">Dữ liệu sau thay đổi (After JSON):</h3>
                  <pre className="p-3 bg-slate-900 text-emerald-300 rounded-xl overflow-x-auto font-mono text-[11px]">
                    {selectedLog.afterJson}
                  </pre>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
