import React from 'react'
import { Slot } from '@radix-ui/react-slot'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/shared/utils/cn'

export const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-[8px] text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-leaf disabled:pointer-events-none disabled:opacity-50 cursor-pointer select-none active:scale-[0.98]',
  {
    variants: {
      variant: {
        default:
          'bg-leaf text-paper hover:bg-leaf-2 shadow-sm font-semibold',
        primary:
          'bg-leaf text-paper hover:bg-leaf-2 shadow-sm font-semibold',
        secondary:
          'bg-paper-2 text-ink hover:bg-[#E8E2D5] border border-rule',
        outline:
          'border border-rule bg-transparent text-ink hover:bg-paper-2 hover:border-[#C4BCAB]',
        destructive:
          'bg-tomato text-white hover:bg-tomato-ink shadow-sm font-semibold',
        ghost:
          'hover:bg-paper-2 text-ink',
        link:
          'text-leaf underline-offset-4 hover:underline p-0 h-auto',
      },
      size: {
        default: 'h-10 px-4 py-2',
        sm: 'h-8 rounded-[6px] px-3 text-xs',
        lg: 'h-12 rounded-[10px] px-6 text-base',
        icon: 'h-9 w-9 p-0',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  }
)

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : 'button'
    return (
      <Comp
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        {...props}
      />
    )
  }
)
Button.displayName = 'Button'
