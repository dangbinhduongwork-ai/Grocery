import React from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/shared/utils/cn'

export const badgeVariants = cva(
  'inline-flex items-center rounded-[4px] px-2 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-leaf focus:ring-offset-2 select-none',
  {
    variants: {
      variant: {
        default:
          'bg-leaf text-paper border-transparent',
        secondary:
          'bg-paper-2 text-ink border border-rule',
        sprout:
          'bg-[#E2EDE5] text-leaf border border-sprout/40',
        destructive:
          'bg-tomato text-white border-transparent',
        discount:
          'bg-tomato text-white font-bold',
        turmeric:
          'bg-turmeric text-ink font-semibold',
        outline:
          'text-ink border border-rule',
        ok:
          'bg-[#E8F5E9] text-ok border border-ok/30',
        warn:
          'bg-[#FFF8E1] text-warn border border-warn/30',
        info:
          'bg-[#E1F5FE] text-info border border-info/30',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  }
)

export interface BadgeProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {}

export function Badge({ className, variant, ...props }: BadgeProps) {
  return (
    <div className={cn(badgeVariants({ variant }), className)} {...props} />
  )
}
