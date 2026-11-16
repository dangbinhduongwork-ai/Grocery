import React, { useMemo } from 'react'
import { Truck, ShieldCheck, Clock, RotateCcw, Phone, Sparkles } from 'lucide-react'
import { cn } from '@/shared/utils/cn'
import { useUiSettings } from '@/shared/hooks/useUiSettings'

interface DeliveryStripProps {
  className?: string
  marquee?: boolean
}

const ICONS = [Truck, ShieldCheck, Clock, RotateCcw, Phone, Sparkles]

export const DeliveryStrip: React.FC<DeliveryStripProps> = ({
  className,
  marquee = true,
}) => {
  const { settings } = useUiSettings()

  const items = useMemo(() => {
    const list = settings.announcements && settings.announcements.length > 0
      ? settings.announcements
      : [
          'Giao hàng hỏa tốc trong 2 giờ tại nội thành',
          '100% Nông sản tươi sạch chuẩn VietGAP, rõ xuất xứ',
          'Miễn phí vận chuyển cho đơn hàng từ 300.000 ₫',
          'Cam kết đổi trả trong 24 giờ nếu hàng không tươi ngon',
          'Hotline / Zalo tư vấn: 0865 087 784 (7:00 - 22:00)',
        ]

    return list.map((text, i) => ({
      icon: ICONS[i % ICONS.length],
      text,
    }))
  }, [settings.announcements])

  if (!marquee) {
    return (
      <div
        className={cn(
          'grid grid-cols-1 sm:grid-cols-3 gap-2.5 sm:gap-3 border-y border-rule bg-paper-2 py-2.5 sm:py-3 px-3 sm:px-4 text-xs sm:text-sm text-ink-2',
          className
        )}
      >
        {items.slice(0, 3).map((item, index) => {
          const Icon = item.icon
          return (
            <div key={index} className="flex items-center gap-2">
              <Icon className="h-4 w-4 text-leaf shrink-0 stroke-[1.5]" />
              <span>{item.text}</span>
            </div>
          )
        })}
      </div>
    )
  }

  return (
    <aside
      aria-label="Thông báo giao hàng và cam kết dịch vụ"
      className={cn(
        'relative w-full overflow-hidden border-b border-rule bg-paper-2/95 py-2 text-xs sm:text-[13px] text-ink-2 select-none shadow-2xs',
        className
      )}
    >
      <div className="animate-marquee flex items-center">
        {/* First set of items */}
        <div className="flex items-center shrink-0">
          {items.map((item, index) => {
            const Icon = item.icon
            return (
              <div key={`set1-${index}`} className="flex items-center gap-2 px-6 sm:px-10">
                <Icon className="h-4 w-4 text-leaf shrink-0 stroke-[1.8]" />
                <span className="font-medium whitespace-nowrap">{item.text}</span>
                <span className="text-leaf/30 ml-6 sm:ml-10 select-none">•</span>
              </div>
            )
          })}
        </div>

        {/* Second set of identical items for continuous infinite loop */}
        <div className="flex items-center shrink-0" aria-hidden="true">
          {items.map((item, index) => {
            const Icon = item.icon
            return (
              <div key={`set2-${index}`} className="flex items-center gap-2 px-6 sm:px-10">
                <Icon className="h-4 w-4 text-leaf shrink-0 stroke-[1.8]" />
                <span className="font-medium whitespace-nowrap">{item.text}</span>
                <span className="text-leaf/30 ml-6 sm:ml-10 select-none">•</span>
              </div>
            )
          })}
        </div>
      </div>
    </aside>
  )
}
