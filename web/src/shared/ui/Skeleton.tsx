import React from 'react'
import { cn } from '@/shared/utils/cn'

export function Skeleton({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn('animate-pulse rounded-[4px] bg-[#E8E2D5]', className)}
      {...props}
    />
  )
}
