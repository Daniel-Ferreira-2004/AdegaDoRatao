import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { Minus, Plus, Trash2 } from 'lucide-react'
import { getProducts } from '@/services/products/productsService'
import { paymentMethodsService } from '@/services/catalog/catalogService'
import { createSale } from '@/services/sales/salesService'
import type { ProductResponse } from '@/types/api'
import { formatCurrency } from '@/utils/format'
import { useDebounce } from '@/hooks/useDebounce'
import { useToast } from '@/components/feedback/toast'
import { PageHeader } from '@/components/common/page-header'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { FormField } from '@/components/forms/form-field'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

interface CartItem {
  product: ProductResponse
  quantity: number
}

export default function NewSalePage() {
  const { toast } = useToast()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [search, setSearch] = useState('')
  const [cart, setCart] = useState<CartItem[]>([])
  const [discount, setDiscount] = useState('')
  const [paymentMethodId, setPaymentMethodId] = useState('')
  const debouncedSearch = useDebounce(search, 300)

  const { data: paymentMethods } = useQuery({
    queryKey: ['payment-methods'],
    queryFn: paymentMethodsService.list,
  })

  const searchResult = useQuery({
    queryKey: ['sales', 'product-search', debouncedSearch],
    queryFn: () => getProducts({ name: debouncedSearch, isActive: true, pageSize: 8 }),
    enabled: debouncedSearch.length >= 2,
  })

  const subtotal = useMemo(
    () => cart.reduce((sum, item) => sum + item.product.salePrice * item.quantity, 0),
    [cart],
  )
  const discountValue = Math.max(0, Number(discount) || 0)
  const total = Math.max(0, subtotal - discountValue)

  function addToCart(product: ProductResponse) {
    setCart((prev) => {
      const existing = prev.find((i) => i.product.id === product.id)
      if (existing) {
        return prev.map((i) => (i.product.id === product.id ? { ...i, quantity: i.quantity + 1 } : i))
      }
      return [...prev, { product, quantity: 1 }]
    })
    setSearch('')
  }

  function changeQuantity(productId: string, delta: number) {
    setCart((prev) =>
      prev
        .map((i) => (i.product.id === productId ? { ...i, quantity: i.quantity + delta } : i))
        .filter((i) => i.quantity > 0),
    )
  }

  function removeItem(productId: string) {
    setCart((prev) => prev.filter((i) => i.product.id !== productId))
  }

  const mutation = useMutation({
    mutationFn: createSale,
    onSuccess: (sale) => {
      queryClient.invalidateQueries({ queryKey: ['sales'] })
      queryClient.invalidateQueries({ queryKey: ['products'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      toast({ variant: 'success', title: `Venda #${sale.saleNumber} registrada com sucesso.` })
      navigate('/vendas')
    },
    onError: (error) => {
      toast({ variant: 'error', title: 'Não foi possível registrar a venda.', description: error.message })
    },
  })

  function finalize() {
    if (cart.length === 0 || !paymentMethodId) return
    mutation.mutate({
      items: cart.map((i) => ({ productId: i.product.id, quantity: i.quantity })),
      discount: discountValue,
      paymentMethodId,
    })
  }

  return (
    <>
      <PageHeader title="Nova venda" description="Registre uma venda no caixa" />

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="space-y-4 lg:col-span-2">
          <Card>
            <CardContent className="pt-6">
              <Input
                placeholder="Buscar produto por nome (mín. 2 letras)..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                aria-label="Buscar produto"
                autoFocus
              />
              {debouncedSearch.length >= 2 && searchResult.data && (
                <div className="mt-2 max-h-64 overflow-y-auto rounded-md border">
                  {searchResult.data.items.length === 0 ? (
                    <p className="p-3 text-sm text-muted-foreground">Nenhum produto encontrado.</p>
                  ) : (
                    searchResult.data.items.map((p) => (
                      <button
                        key={p.id}
                        className="flex w-full items-center justify-between px-3 py-2 text-left text-sm hover:bg-accent"
                        onClick={() => addToCart(p)}
                      >
                        <span>
                          {p.name}
                          {p.currentStock === 0 && !p.allowNegativeStock && (
                            <span className="ml-2 text-xs text-destructive">(sem estoque)</span>
                          )}
                        </span>
                        <span className="font-medium">{formatCurrency(p.salePrice)}</span>
                      </button>
                    ))
                  )}
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Itens da venda</CardTitle>
            </CardHeader>
            <CardContent>
              {cart.length === 0 ? (
                <p className="py-8 text-center text-sm text-muted-foreground">
                  Nenhum item adicionado. Busque um produto acima.
                </p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Produto</TableHead>
                      <TableHead className="text-right">Preço</TableHead>
                      <TableHead className="w-36 text-center">Qtd.</TableHead>
                      <TableHead className="text-right">Subtotal</TableHead>
                      <TableHead className="w-12" />
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {cart.map((item) => (
                      <TableRow key={item.product.id}>
                        <TableCell className="font-medium">{item.product.name}</TableCell>
                        <TableCell className="text-right">{formatCurrency(item.product.salePrice)}</TableCell>
                        <TableCell>
                          <div className="flex items-center justify-center gap-1">
                            <Button
                              variant="outline"
                              size="icon"
                              className="size-7"
                              onClick={() => changeQuantity(item.product.id, -1)}
                              aria-label="Diminuir quantidade"
                            >
                              <Minus />
                            </Button>
                            <span className="w-8 text-center">{item.quantity}</span>
                            <Button
                              variant="outline"
                              size="icon"
                              className="size-7"
                              onClick={() => changeQuantity(item.product.id, 1)}
                              aria-label="Aumentar quantidade"
                            >
                              <Plus />
                            </Button>
                          </div>
                        </TableCell>
                        <TableCell className="text-right font-medium">
                          {formatCurrency(item.product.salePrice * item.quantity)}
                        </TableCell>
                        <TableCell>
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => removeItem(item.product.id)}
                            aria-label={`Remover ${item.product.name}`}
                          >
                            <Trash2 className="text-destructive" />
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </div>

        <Card className="h-fit">
          <CardHeader>
            <CardTitle>Resumo</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <FormField label="Forma de pagamento" htmlFor="paymentMethod" required>
              <Select
                id="paymentMethod"
                value={paymentMethodId}
                onChange={(e) => setPaymentMethodId(e.target.value)}
              >
                <option value="">Selecione...</option>
                {paymentMethods?.filter((m) => m.isActive).map((m) => (
                  <option key={m.id} value={m.id}>{m.name}</option>
                ))}
              </Select>
            </FormField>
            <FormField label="Desconto (R$)" htmlFor="discount">
              <Input
                id="discount"
                type="number"
                min="0"
                step="0.01"
                value={discount}
                onChange={(e) => setDiscount(e.target.value)}
              />
            </FormField>
            <div className="space-y-1 border-t pt-4 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Subtotal</span>
                <span>{formatCurrency(subtotal)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Desconto</span>
                <span>- {formatCurrency(discountValue)}</span>
              </div>
              <div className="flex justify-between text-lg font-bold">
                <span>Total</span>
                <span>{formatCurrency(total)}</span>
              </div>
            </div>
            <Button
              className="w-full"
              size="lg"
              disabled={cart.length === 0 || !paymentMethodId || mutation.isPending}
              onClick={finalize}
            >
              {mutation.isPending ? 'Finalizando...' : 'Finalizar venda'}
            </Button>
          </CardContent>
        </Card>
      </div>
    </>
  )
}
