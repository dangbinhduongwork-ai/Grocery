import React, { useState } from 'react'
import { colors } from '@/shared/tokens/colors'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'
import { Badge } from '@/shared/ui/Badge'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/shared/ui/Select'
import { PriceTag } from '@/shared/components/PriceTag'
import { StockBadge } from '@/shared/components/StockBadge'
import { DeliveryStrip } from '@/shared/components/DeliveryStrip'
import { AisleNav } from '@/shared/components/AisleNav'
import { ProductTile } from '@/shared/components/ProductTile'
import { ProductRow } from '@/shared/components/ProductRow'
import { ReceiptSummary } from '@/shared/components/ReceiptSummary'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/shared/ui/Dialog'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/ui/Tabs'

export const DesignRoute: React.FC = () => {
  const [selectedAisle, setSelectedAisle] = useState('rau-cu-qua')
  const [quantity, setQuantity] = useState(2)

  const sampleCategories = [
    { id: '1', name: 'Rau củ quả', slug: 'rau-cu-qua', productCount: 42 },
    { id: '2', name: 'Thịt & Hải sản', slug: 'thit-hai-san', productCount: 28 },
    { id: '3', name: 'Trái cây tươi', slug: 'trai-cay-tuoi', productCount: 35 },
    { id: '4', name: 'Gạo & Ngũ cốc', slug: 'gao-ngu-coc', productCount: 19 },
    { id: '5', name: 'Gia vị & Đồ khô', slug: 'gia-vi', productCount: 54 },
  ]

  const sampleProduct = {
    id: 'prod-01',
    name: 'Cà chua Beef Đà Lạt hữu cơ tươi sạch chuẩn VietGAP',
    slug: 'ca-chua-beef-da-lat',
    sku: 'SKU-TOMATO-01',
    unit: 'Kg',
    packSize: 'Túi 1kg (4-5 quả)',
    sellingPrice: 38000,
    listPrice: 48000,
    ratingAvg: 4.8,
    ratingCount: 124,
    inStock: true,
    lowStock: false,
    primaryImageUrl: 'https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=400&auto=format&fit=crop&q=80',
  }

  return (
    <div className="space-y-12 pb-16">
      {/* Header */}
      <div className="border-b border-rule pb-6">
        <span className="text-xs font-bold uppercase tracking-wider text-leaf bg-sprout/20 px-2.5 py-1 rounded-full">
          Hệ thống thiết kế GroceryHub
        </span>
        <h1 className="text-3xl sm:text-4xl font-bold font-display text-ink mt-3">
          Design Tokens & Components Guide
        </h1>
        <p className="text-sm sm:text-base text-ink-2 mt-2 max-w-2xl">
          Bảng quy chuẩn màu sắc, phông chữ tiếng Việt, thành phần giao diện và các signature components phục vụ toàn bộ storefront và admin.
        </p>
      </div>

      {/* 1. Color Palette */}
      <section className="space-y-4">
        <h2 className="text-xl font-bold font-display text-ink flex items-center gap-2">
          <span>1. Bảng màu chuẩn (Palette Tokens)</span>
        </h2>
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-3">
          {Object.entries(colors).map(([name, hex]) => (
            <div
              key={name}
              className="rounded-[8px] border border-rule bg-paper p-3 shadow-2xs space-y-2"
            >
              <div
                className="h-14 w-full rounded-[6px] border border-black/10"
                style={{ backgroundColor: hex }}
              />
              <div>
                <p className="text-xs font-bold text-ink truncate">--{name}</p>
                <p className="text-[11px] font-mono text-ink-2">{hex}</p>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* 2. Vietnamese Typography */}
      <section className="space-y-4">
        <h2 className="text-xl font-bold font-display text-ink">
          2. Quy chuẩn chữ tiếng Việt (Typography & Glyphs)
        </h2>
        <div className="rounded-[10px] border border-rule bg-paper p-5 space-y-4">
          <div className="border-b border-rule pb-3">
            <p className="text-xs font-semibold text-leaf uppercase">Phông hiển thị (Display - Fraunces)</p>
            <h1 className="text-2xl sm:text-3xl font-display font-bold text-ink mt-1">
              Rau củ quả tươi — Đường Ưu Đãi ẳ ỡ ợ ữ ế 25.000 ₫
            </h1>
          </div>
          <div>
            <p className="text-xs font-semibold text-leaf uppercase">Phông giao diện (UI/Body - Be Vietnam Pro)</p>
            <p className="text-base font-sans text-ink mt-1">
              Đảm bảo các dấu thanh điệu tiếng Việt: huyền, sắc, hỏi, ngã, nặng và các ký tự đặc biệt đ, Đ, ư, ơ hiển thị chuẩn xác không lỗi dấu.
            </p>
            <p className="text-sm tabular-nums text-ink-2 mt-2">
              Chữ số dạng Tabular Nums: 120.000 ₫ | 350.000 ₫ | 1.250.000 ₫
            </p>
          </div>
        </div>
      </section>

      {/* 3. Themed Primitives */}
      <section className="space-y-4">
        <h2 className="text-xl font-bold font-display text-ink">
          3. Nút & Thẻ trạng thái (Buttons & Badges)
        </h2>
        <div className="rounded-[10px] border border-rule bg-paper p-5 space-y-6">
          {/* Buttons */}
          <div className="space-y-2">
            <h3 className="text-sm font-semibold text-ink-2">Biến thể nút bấm (Button Variants)</h3>
            <div className="flex flex-wrap gap-3 items-center">
              <Button variant="default">Nút chính (Leaf)</Button>
              <Button variant="secondary">Nút phụ (Secondary)</Button>
              <Button variant="outline">Đường viền (Outline)</Button>
              <Button variant="destructive">Cảnh báo / Xóa (Tomato)</Button>
              <Button variant="ghost">Nút ẩn (Ghost)</Button>
              <Button disabled variant="default">Bị vô hiệu</Button>
            </div>
          </div>

          {/* Badges */}
          <div className="space-y-2">
            <h3 className="text-sm font-semibold text-ink-2">Thẻ trạng thái (Badges)</h3>
            <div className="flex flex-wrap gap-2.5 items-center">
              <Badge variant="default">Mặc định</Badge>
              <Badge variant="sprout">Nông sản sạch</Badge>
              <Badge variant="discount">-20%</Badge>
              <Badge variant="turmeric">Bán chạy</Badge>
              <Badge variant="ok">Đã giao hàng</Badge>
              <Badge variant="warn">Đang chuẩn bị</Badge>
              <Badge variant="info">Đang vận chuyển</Badge>
              <Badge variant="destructive">Đã hủy</Badge>
            </div>
          </div>

          {/* Stock Badges */}
          <div className="space-y-2">
            <h3 className="text-sm font-semibold text-ink-2">Trạng thái tồn kho (StockBadges)</h3>
            <div className="flex flex-wrap gap-3 items-center">
              <StockBadge inStock={true} />
              <StockBadge inStock={true} lowStock={true} />
              <StockBadge inStock={false} />
            </div>
          </div>

          {/* Inputs & Select */}
          <div className="space-y-2">
            <h3 className="text-sm font-semibold text-ink-2">Ô nhập liệu & Hộp chọn (Inputs & Select)</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 max-w-xl">
              <div>
                <label className="text-xs font-semibold text-ink block mb-1">Họ và tên</label>
                <Input placeholder="Nguyễn Văn A" />
              </div>
              <div>
                <label className="text-xs font-semibold text-ink block mb-1">Tỉnh / Thành phố</label>
                <Select defaultValue="01">
                  <SelectTrigger>
                    <SelectValue placeholder="Chọn Tỉnh / Thành" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="01">Thành phố Hà Nội</SelectItem>
                    <SelectItem value="79">Thành phố Hồ Chí Minh</SelectItem>
                    <SelectItem value="48">Thành phố Đà Nẵng</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* 4. Signature Components */}
      <section className="space-y-4">
        <h2 className="text-xl font-bold font-display text-ink">
          4. Thành phần đặc trưng (GroceryHub Signature Components)
        </h2>

        {/* Delivery Strip */}
        <div className="space-y-2">
          <h3 className="text-sm font-semibold text-ink-2">Băng thông tin giao hàng (DeliveryStrip)</h3>
          <DeliveryStrip />
        </div>

        {/* Aisle Nav */}
        <div className="space-y-2">
          <h3 className="text-sm font-semibold text-ink-2">Điều hướng gian hàng (AisleNav)</h3>
          <AisleNav
            categories={sampleCategories}
            selectedSlug={selectedAisle}
            onSelectCategory={(slug) => setSelectedAisle(slug)}
          />
        </div>

        {/* Price Tags */}
        <div className="space-y-2">
          <h3 className="text-sm font-semibold text-ink-2">Thẻ giá chợ (PriceTag Variants)</h3>
          <div className="flex flex-wrap gap-6 items-baseline p-4 rounded-[8px] bg-paper-2 border border-rule">
            <div>
              <p className="text-xs text-ink-2 mb-1">Cỡ nhỏ (Small):</p>
              <PriceTag sellingPrice={25000} listPrice={30000} unit="Bó" size="sm" />
            </div>
            <div>
              <p className="text-xs text-ink-2 mb-1">Cỡ vừa (Medium):</p>
              <PriceTag sellingPrice={120000} listPrice={150000} unit="Kg" size="md" />
            </div>
            <div>
              <p className="text-xs text-ink-2 mb-1">Cỡ lớn (Large PDP):</p>
              <PriceTag sellingPrice={280000} listPrice={350000} unit="Thùng" size="lg" />
            </div>
          </div>
        </div>

        {/* Product Grid & Row */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 items-start">
          <div className="space-y-2">
            <h3 className="text-sm font-semibold text-ink-2">Thẻ sản phẩm gian hàng (ProductTile)</h3>
            <div className="max-w-[280px]">
              <ProductTile
                product={sampleProduct}
                onSelect={(slug) => alert(`Xem chi tiết: ${slug}`)}
                onAddToCart={(p) => alert(`Đã thêm vào giỏ: ${p.name}`)}
              />
            </div>
          </div>

          <div className="space-y-4">
            <h3 className="text-sm font-semibold text-ink-2">Hàng sản phẩm giỏ hàng (ProductRow)</h3>
            <div className="rounded-[8px] border border-rule bg-paper p-4">
              <ProductRow
                item={{
                  id: 'item-1',
                  productId: sampleProduct.id,
                  name: sampleProduct.name,
                  sku: sampleProduct.sku,
                  unit: sampleProduct.unit,
                  packSize: sampleProduct.packSize,
                  unitPrice: sampleProduct.sellingPrice,
                  quantity: quantity,
                  lineSubtotal: sampleProduct.sellingPrice * quantity,
                  imageUrl: sampleProduct.primaryImageUrl,
                }}
                onUpdateQuantity={(_, q) => setQuantity(q)}
                onRemove={() => alert('Xóa sản phẩm')}
              />
            </div>

            <h3 className="text-sm font-semibold text-ink-2">Biên nhận thanh toán (ReceiptSummary)</h3>
            <ReceiptSummary
              subtotal={sampleProduct.sellingPrice * quantity}
              shippingFee={0}
              couponDiscount={10000}
              couponCode="FREESHIP30"
              total={sampleProduct.sellingPrice * quantity - 10000}
            />
          </div>
        </div>
      </section>

      {/* 5. Dialog & Interactive */}
      <section className="space-y-4">
        <h2 className="text-xl font-bold font-display text-ink">
          5. Hộp thoại & Tương tác (Dialog & Tabs)
        </h2>
        <div className="rounded-[10px] border border-rule bg-paper p-5 flex flex-wrap gap-4 items-center">
          <Dialog>
            <DialogTrigger asChild>
              <Button variant="default">Mở hộp thoại kiểm tra</Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Xác nhận đơn hàng</DialogTitle>
                <DialogDescription>
                  Đơn hàng của bạn sẽ được chuẩn bị và giao trong vòng 2 giờ. Bạn có chắc chắn muốn xác nhận?
                </DialogDescription>
              </DialogHeader>
              <div className="py-2 text-sm text-ink-2">
                Phương thức thanh toán: <strong className="text-ink">COD (Tiền mặt khi nhận hàng)</strong>
              </div>
              <DialogFooter>
                <Button variant="secondary">Hủy bỏ</Button>
                <Button variant="default">Đồng ý đặt hàng</Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>

          <Tabs defaultValue="desc" className="w-full max-w-md">
            <TabsList>
              <TabsTrigger value="desc">Mô tả sản phẩm</TabsTrigger>
              <TabsTrigger value="spec">Thông số</TabsTrigger>
              <TabsTrigger value="reviews">Đánh giá (124)</TabsTrigger>
            </TabsList>
            <TabsContent value="desc" className="p-3 text-sm text-ink-2 bg-paper-2 rounded-[6px]">
              Cà chua Beef Đà Lạt được trồng theo tiêu chuẩn hữu cơ, quả to mọng nước, vị ngọt thanh tự nhiên.
            </TabsContent>
            <TabsContent value="spec" className="p-3 text-sm text-ink-2 bg-paper-2 rounded-[6px]">
              Xuất xứ: Lâm Đồng | Đóng gói: 1kg | Hạn sử dụng: 5 ngày kể từ ngày thu hoạch.
            </TabsContent>
            <TabsContent value="reviews" className="p-3 text-sm text-ink-2 bg-paper-2 rounded-[6px]">
              Đánh giá trung bình 4.8/5 sao từ 124 khách hàng đã mua sản phẩm này.
            </TabsContent>
          </Tabs>
        </div>
      </section>
    </div>
  )
}
