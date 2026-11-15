import React from 'react'
import { Badge } from '@/shared/ui/Badge'
import { CheckCircle2, AlertCircle, XCircle } from 'lucide-react'

interface StockBadgeProps {
  inStock: boolean
  lowStock?: boolean
  className?: string
}

export const StockBadge: React.FC<StockBadgeProps> = ({ inStock, lowStock, className }) => {
  if (!inStock) {
    return (
      <Badge variant="destructive" className={className}>
        <XCircle className="w-3 h-3 mr-1 stroke-[1.5]" />
        Hết hàng
      </Badge>
    )
  }

  if (lowStock) {
    return (
      <Badge variant="warn" className={className}>
        <AlertCircle className="w-3 h-3 mr-1 stroke-[1.5]" />
        Sắp hết hàng
      </Badge>
    )
  }

  return (
    <Badge variant="ok" className={className}>
      <CheckCircle2 className="w-3 h-3 mr-1 stroke-[1.5]" />
      Còn hàng
    </Badge>
  )
}
