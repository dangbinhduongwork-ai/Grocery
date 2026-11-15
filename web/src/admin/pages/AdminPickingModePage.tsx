import React, { useEffect, useState } from 'react'
import {
  Boxes,
  Check,
  CheckCircle2,
  ScanBarcode,
  ArrowLeft,
  PackageCheck,
} from 'lucide-react'
import { api } from '@/shared/api/client'
import { useRouter } from '@/app/router'

interface PickItem {
  sku: string
  barcode?: string
  productName: string
  unit: string
  packSize?: string
  quantity: number
  imageUrl?: string
}

interface PickGroup {
  categoryName: string
  items: PickItem[]
}

interface PickList {
  orderId: string
  orderNumber: string
  status: string
  customerNote?: string
  groups: PickGroup[]
}

interface PreparingOrder {
  id: string
  orderNumber: string
  status: string
  recipientName: string
}

export const AdminPickingModePage: React.FC = () => {
  const router = useRouter()
  const [orders, setOrders] = useState<PreparingOrder[]>([])
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null)
  const [pickList, setPickList] = useState<PickList | null>(null)
  const [pickedItems, setPickedItems] = useState<Set<string>>(new Set())
  const [barcodeInput, setBarcodeInput] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [scanFeedback, setScanFeedback] = useState<string | null>(null)

  // Fetch orders needing picking (Preparing or Confirmed)
  useEffect(() => {
    async function loadOrders() {
      try {
        const res = await api.GET('/api/v1/admin/orders', {
          params: { query: { status: 'Preparing', pageSize: 50 } },
        })
        if (res.data?.items) {
          const list = res.data.items as unknown as PreparingOrder[]
          setOrders(list)
          if (list.length > 0) {
            setSelectedOrderId(list[0].id)
          }
        }
      } catch {
        // Silently
      } finally {
        setIsLoading(false)
      }
    }
    void loadOrders()
  }, [])

  // Fetch pick list when selected order changes
  useEffect(() => {
    if (!selectedOrderId) {
      setPickList(null)
      return
    }

    async function loadPickList() {
      try {
        const res = await api.GET('/api/v1/admin/orders/{id}/pick-list', {
          params: { path: { id: selectedOrderId! } },
        })
        if (res.data) {
          setPickList(res.data as unknown as PickList)
          setPickedItems(new Set())
        }
      } catch {
        setPickList(null)
      }
    }
    void loadPickList()
  }, [selectedOrderId])

  const toggleItem = (sku: string) => {
    setPickedItems(prev => {
      const next = new Set(prev)
      if (next.has(sku)) {
        next.delete(sku)
      } else {
        next.add(sku)
      }
      return next
    })
  }

  const handleBarcodeSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!barcodeInput.trim() || !pickList) return

    const code = barcodeInput.trim()
    let found = false

    for (const group of pickList.groups) {
      for (const item of group.items) {
        if (item.barcode === code || item.sku === code) {
          setPickedItems(prev => new Set(prev).add(item.sku))
          setScanFeedback(`Đã quét thành công: ${item.productName}`)
          found = true
          break
        }
      }
      if (found) break
    }

    if (!found) {
      setScanFeedback(`Không tìm thấy mã: ${code}`)
    }

    setBarcodeInput('')
  }

  const handleCompletePicking = async () => {
    if (!pickList) return
    try {
      await api.POST('/api/v1/admin/orders/{id}/transition', {
        params: { path: { id: pickList.orderId } },
        body: { to: 'Packed', reason: null, codAmount: null },
      })
      alert(`Đã đóng gói thành công đơn ${pickList.orderNumber}!`)
      // Refresh list
      setOrders(prev => prev.filter(o => o.id !== pickList.orderId))
      setSelectedOrderId(null)
    } catch {
      alert('Không thể chuyển trạng thái sang Đã đóng gói.')
    }
  }

  const totalItemsCount = pickList?.groups.reduce((acc, g) => acc + g.items.length, 0) ?? 0
  const allPicked = totalItemsCount > 0 && pickedItems.size === totalItemsCount

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => router.push('/admin/orders')}
            className="p-2 rounded-xl border border-slate-200 bg-white text-slate-600 hover:bg-slate-50 transition-colors"
          >
            <ArrowLeft className="w-4 h-4" strokeWidth={1.5} />
          </button>
          <div>
            <h1 className="text-xl font-bold text-slate-900 font-display">
              Chế độ lấy hàng kho (Picking Mode)
            </h1>
            <p className="text-xs text-slate-500 mt-0.5">
              Giao diện tối ưu thao tác cảm ứng máy tính bảng và quét barcode tại quầy kho.
            </p>
          </div>
        </div>

        {/* Order Selector */}
        {orders.length > 0 && (
          <select
            value={selectedOrderId ?? ''}
            onChange={e => setSelectedOrderId(e.target.value)}
            className="px-3 py-2 bg-white border border-slate-200 rounded-xl text-xs font-semibold text-slate-800 shadow-2xs"
          >
            {orders.map(o => (
              <option key={o.id} value={o.id}>
                {o.orderNumber} &mdash; {o.recipientName}
              </option>
            ))}
          </select>
        )}
      </div>

      {isLoading ? (
        <div className="h-64 bg-white rounded-2xl animate-pulse" />
      ) : !pickList ? (
        <div className="p-12 text-center bg-white rounded-2xl border border-slate-200">
          <CheckCircle2 className="w-12 h-12 text-emerald-500 mx-auto mb-3" strokeWidth={1.5} />
          <h2 className="text-base font-bold text-slate-800">Không có đơn hàng nào chờ lấy hàng</h2>
          <p className="text-xs text-slate-400 mt-1">Toàn bộ đơn hàng đã được chuẩn bị và đóng gói.</p>
        </div>
      ) : (
        <div className="space-y-6">
          {/* Barcode Scanner Bar */}
          <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-2xs flex flex-col sm:flex-row items-center justify-between gap-4">
            <form onSubmit={handleBarcodeSubmit} className="relative w-full sm:w-96">
              <ScanBarcode className="w-5 h-5 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" strokeWidth={1.5} />
              <input
                type="text"
                value={barcodeInput}
                onChange={e => setBarcodeInput(e.target.value)}
                placeholder="Quét hoặc nhập mã Barcode / SKU..."
                className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-hidden focus:ring-2 focus:ring-leaf/20 focus:border-leaf font-mono"
              />
            </form>

            <div className="flex items-center gap-4 w-full sm:w-auto justify-between sm:justify-end">
              <div className="text-xs">
                Tiến độ:{' '}
                <span className="font-bold text-leaf font-mono">
                  {pickedItems.size} / {totalItemsCount}
                </span>{' '}
                mặt hàng
              </div>

              <button
                type="button"
                onClick={handleCompletePicking}
                disabled={!allPicked}
                className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-leaf text-white font-semibold text-xs hover:bg-leaf-2 transition-colors disabled:opacity-40 disabled:cursor-not-allowed shadow-xs"
              >
                <PackageCheck className="w-4 h-4" strokeWidth={1.5} />
                Hoàn thành đóng gói
              </button>
            </div>
          </div>

          {scanFeedback && (
            <div className="p-3 bg-indigo-50 border border-indigo-200 text-indigo-700 text-xs rounded-xl flex items-center gap-2">
              <ScanBarcode className="w-4 h-4" strokeWidth={1.5} />
              <span>{scanFeedback}</span>
            </div>
          )}

          {/* Grouped by Category / Aisle */}
          <div className="space-y-6">
            {pickList.groups.map(group => (
              <div key={group.categoryName} className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-2xs">
                <div className="p-4 bg-slate-50/80 border-b border-slate-200 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Boxes className="w-4 h-4 text-leaf" strokeWidth={1.5} />
                    <h2 className="text-xs font-bold uppercase tracking-wider text-slate-800">
                      Quầy hàng: {group.categoryName}
                    </h2>
                  </div>
                  <span className="text-xs text-slate-400 font-medium">{group.items.length} món</span>
                </div>

                <div className="divide-y divide-slate-100">
                  {group.items.map(item => {
                    const isPicked = pickedItems.has(item.sku)
                    return (
                      <div
                        key={item.sku}
                        onClick={() => toggleItem(item.sku)}
                        className={`p-4 flex items-center justify-between cursor-pointer transition-colors select-none ${
                          isPicked ? 'bg-emerald-50/60 text-slate-500' : 'hover:bg-slate-50 text-slate-900'
                        }`}
                      >
                        <div className="flex items-center gap-4">
                          {/* Large tap target checkbox */}
                          <div
                            className={`w-8 h-8 rounded-xl border flex items-center justify-center transition-colors ${
                              isPicked
                                ? 'bg-leaf border-leaf text-white shadow-2xs'
                                : 'border-slate-300 bg-white hover:border-leaf'
                            }`}
                          >
                            {isPicked && <Check className="w-5 h-5 stroke-[3]" />}
                          </div>

                          <div>
                            <div className={`font-semibold text-sm ${isPicked ? 'line-through text-slate-400' : ''}`}>
                              {item.productName}
                            </div>
                            <div className="text-xs text-slate-400 font-mono mt-0.5">
                              SKU: {item.sku} {item.barcode ? `• Barcode: ${item.barcode}` : ''}
                            </div>
                          </div>
                        </div>

                        <div className="text-right">
                          <span className="inline-block px-3 py-1 rounded-xl bg-slate-100 text-slate-800 font-bold text-base font-display">
                            &times; {item.quantity}
                          </span>
                          <span className="block text-xs text-slate-400 mt-0.5">{item.unit}</span>
                        </div>
                      </div>
                    )
                  })}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
