import React, { useState, useMemo } from 'react'
import {
  ChevronLeft,
  ChevronRight,
  ArrowUpDown,
  Search,
  FilterX,
} from 'lucide-react'

export interface Column<T> {
  key: string
  title: string
  sortable?: boolean
  render?: (item: T) => React.ReactNode
  className?: string
}

export interface DataTableProps<T> {
  data: T[]
  columns: Column<T>[]
  keyExtractor: (item: T) => string
  searchPlaceholder?: string
  searchableKey?: (item: T) => string
  pageSize?: number
  isLoading?: boolean
  emptyMessage?: string
  actions?: React.ReactNode
}

export function DataTable<T>({
  data,
  columns,
  keyExtractor,
  searchPlaceholder = 'Tìm kiếm...',
  searchableKey,
  pageSize = 10,
  isLoading = false,
  emptyMessage = 'Không tìm thấy dữ liệu',
  actions,
}: DataTableProps<T>) {
  const [search, setSearch] = useState('')
  const [currentPage, setCurrentPage] = useState(1)
  const [sortKey, setSortKey] = useState<string | null>(null)
  const [sortAsc, setSortAsc] = useState(true)

  const handleSort = (key: string) => {
    if (sortKey === key) {
      setSortAsc(!sortAsc)
    } else {
      setSortKey(key)
      setSortAsc(true)
    }
    setCurrentPage(1)
  }

  const filteredData = useMemo(() => {
    if (!search.trim()) return data
    const query = search.toLowerCase()
    return data.filter(item => {
      if (searchableKey) {
        return searchableKey(item).toLowerCase().includes(query)
      }
      return JSON.stringify(item).toLowerCase().includes(query)
    })
  }, [data, search, searchableKey])

  const sortedData = useMemo(() => {
    if (!sortKey) return filteredData
    return [...filteredData].sort((a, b) => {
      const valA = (a as Record<string, unknown>)[sortKey]
      const valB = (b as Record<string, unknown>)[sortKey]

      if (valA === valB) return 0
      if (valA === null || valA === undefined) return 1
      if (valB === null || valB === undefined) return -1

      if (typeof valA === 'number' && typeof valB === 'number') {
        return sortAsc ? valA - valB : valB - valA
      }

      const strA = String(valA).toLowerCase()
      const strB = String(valB).toLowerCase()
      return sortAsc ? strA.localeCompare(strB) : strB.localeCompare(strA)
    })
  }, [filteredData, sortKey, sortAsc])

  const totalPages = Math.ceil(sortedData.length / pageSize) || 1
  const paginatedData = useMemo(() => {
    const start = (currentPage - 1) * pageSize
    return sortedData.slice(start, start + pageSize)
  }, [sortedData, currentPage, pageSize])

  return (
    <div className="bg-white rounded-xl border border-slate-200 overflow-hidden shadow-xs">
      {/* Table Toolbar */}
      <div className="p-4 flex flex-col sm:flex-row items-center justify-between gap-3 border-b border-slate-100 bg-slate-50/50">
        <div className="relative w-full sm:w-72">
          <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" strokeWidth={1.5} />
          <input
            type="text"
            value={search}
            onChange={e => {
              setSearch(e.target.value)
              setCurrentPage(1)
            }}
            placeholder={searchPlaceholder}
            className="w-full pl-9 pr-4 py-2 bg-white border border-slate-200 rounded-lg text-sm text-slate-800 placeholder-slate-400 focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf"
          />
        </div>
        {actions && <div className="flex items-center gap-2 self-end sm:self-auto">{actions}</div>}
      </div>

      {/* Table Content */}
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm text-slate-600">
          <thead className="bg-slate-50 text-xs uppercase font-semibold text-slate-500 border-b border-slate-200">
            <tr>
              {columns.map(col => (
                <th
                  key={col.key}
                  className={`px-4 py-3 select-none ${col.className ?? ''}`}
                  onClick={() => col.sortable && handleSort(col.key)}
                >
                  <div className={`flex items-center gap-1.5 ${col.sortable ? 'cursor-pointer hover:text-slate-800' : ''}`}>
                    <span>{col.title}</span>
                    {col.sortable && (
                      <ArrowUpDown className="w-3.5 h-3.5 text-slate-400" strokeWidth={1.5} />
                    )}
                  </div>
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <tr key={i} className="animate-pulse">
                  {columns.map(col => (
                    <td key={col.key} className="px-4 py-3">
                      <div className="h-4 bg-slate-200 rounded-sm w-3/4" />
                    </td>
                  ))}
                </tr>
              ))
            ) : paginatedData.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="px-4 py-12 text-center text-slate-400">
                  <div className="flex flex-col items-center justify-center gap-2">
                    <FilterX className="w-8 h-8 text-slate-300" strokeWidth={1.5} />
                    <p className="text-sm font-medium text-slate-600">{emptyMessage}</p>
                    {search && (
                      <button
                        type="button"
                        onClick={() => setSearch('')}
                        className="text-xs text-leaf hover:underline font-medium mt-1"
                      >
                        Xóa bộ lọc tìm kiếm
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ) : (
              paginatedData.map(item => (
                <tr key={keyExtractor(item)} className="hover:bg-slate-50/80 transition-colors">
                  {columns.map(col => (
                    <td key={col.key} className={`px-4 py-3 ${col.className ?? ''}`}>
                      {col.render
                        ? col.render(item)
                        : String((item as Record<string, unknown>)[col.key] ?? '')}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination Footer */}
      <div className="p-4 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500 bg-white">
        <div>
          Hiển thị{' '}
          <span className="font-semibold text-slate-700">
            {sortedData.length > 0 ? (currentPage - 1) * pageSize + 1 : 0}
          </span>{' '}
          đến{' '}
          <span className="font-semibold text-slate-700">
            {Math.min(currentPage * pageSize, sortedData.length)}
          </span>{' '}
          trong tổng số <span className="font-semibold text-slate-700">{sortedData.length}</span> bản ghi
        </div>
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
            disabled={currentPage === 1}
            className="p-1.5 rounded-md border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed"
          >
            <ChevronLeft className="w-4 h-4" strokeWidth={1.5} />
          </button>
          <span className="px-2 font-medium text-slate-700">
            {currentPage} / {totalPages}
          </span>
          <button
            type="button"
            onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
            disabled={currentPage === totalPages || totalPages === 0}
            className="p-1.5 rounded-md border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed"
          >
            <ChevronRight className="w-4 h-4" strokeWidth={1.5} />
          </button>
        </div>
      </div>
    </div>
  )
}
