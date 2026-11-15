import React from 'react'
import { Truck, ShieldCheck, Clock } from 'lucide-react'
import { cn } from '@/shared/utils/cn'

interface DeliveryStripProps {
  className?: string
}

export const DeliveryStrip: React.FC<DeliveryStripProps> = ({ className }) => {
  return (
    <div
      className={cn(
        'grid grid-cols-1 sm:grid-cols-3 gap-3 border-y border-rule bg-paper-2 py-3 px-4 text-xs sm:text-sm text-ink-2',
        className
      )}
    >
      <div className="flex items-center gap-2">
        <Truck className="h-4 w-4 text-leaf shrink-0 stroke-[1.5]" />
        <span>Giao hàng nhanh trong 2 giờ tại nội thành</span>
      </div>
      <div className="flex items-center gap-2">
        <ShieldCheck className="h-4 w-4 text-leaf shrink-0 stroke-[1.5]" />
        <span>100% tươi sạch, nguồn gốc xuất xứ rõ ràng</span>
      </div>
      <div className="flex items-center gap-2">
        <Clock className="h-4 w-4 text-leaf shrink-0 stroke-[1.5]" />
        <span>Miễn phí vận chuyển cho đơn từ 300.000 ₫</span>
      </div>
    </div>
  )
}
