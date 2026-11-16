import React, { useState } from 'react'

interface ZaloContactButtonProps {
  phone?: string
  className?: string
}

export const ZaloContactButton: React.FC<ZaloContactButtonProps> = ({
  phone = '0865087784',
  className = '',
}) => {
  const [isHovered, setIsHovered] = useState(false)
  const zaloUrl = `https://zalo.me/${phone}`

  return (
    <div
      className={`fixed bottom-36 md:bottom-24 right-4 sm:right-6 z-40 flex items-center select-none ${className}`}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      {/* Expandable Tooltip Bubble */}
      <div
        className={`mr-3 px-3.5 py-2 rounded-2xl bg-white border border-slate-200 shadow-lg text-xs font-semibold text-slate-800 flex items-center gap-2 transition-all duration-300 origin-right ${
          isHovered
            ? 'opacity-100 scale-100 translate-x-0'
            : 'opacity-0 scale-95 translate-x-2 pointer-events-none'
        }`}
      >
        <span className="w-2 h-2 rounded-full bg-emerald-500 animate-ping" />
        <span>Chat Zalo tư vấn:</span>
        <span className="font-mono text-[#0068FF] font-bold">0865 087 784</span>
      </div>

      {/* Floating Action Button */}
      <a
        href={zaloUrl}
        target="_blank"
        rel="noopener noreferrer"
        aria-label="Liên hệ Zalo tư vấn 0865087784"
        className="group relative w-13 h-13 rounded-full bg-[#0068FF] hover:bg-[#0057d6] text-white flex items-center justify-center shadow-xl hover:shadow-2xl transition-all duration-300 transform hover:scale-105"
      >
        {/* Soft Pulse Waves */}
        <span className="absolute inset-0 rounded-full bg-[#0068FF]/30 animate-ping -z-10" />
        <span className="absolute -inset-1 rounded-full border-2 border-[#0068FF]/40 animate-pulse -z-10" />

        {/* Zalo Icon */}
        <div className="flex flex-col items-center justify-center">
          <span className="font-black text-[13px] tracking-tighter leading-none font-sans uppercase">
            Zalo
          </span>
          <span className="text-[8px] font-bold opacity-80 leading-tight">24/7</span>
        </div>

        {/* Online Status Dot */}
        <span className="absolute top-0 right-0 w-3.5 h-3.5 bg-emerald-500 border-2 border-white rounded-full" />
      </a>
    </div>
  )
}
