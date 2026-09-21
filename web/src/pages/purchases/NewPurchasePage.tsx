import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Minus, Plus, Trash2 } from 'lucide-react'
import { getProducts } from '@/services/products/productsService'
import { getSuppliers } from '@/services/suppliers/suppliersService'
import { paymentMethodsService } from '@/services/catalog/catalogService'
import { createPurchase, confirmPurchase } from '@/services/purchases/purchasesService'
import type { ProductResponse } from '@/types/api'
import { formatCurrency, toDateInputValue } from '@/utils/format'
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

interface PurchaseItem {
  product: ProductResponse
  quantity: number
  unitCost: number
}

export default function NewPurchasePage() {
  const { toast } = useToast()
  const queryClient = useQueryClient()

  const [supplierId, setSupplierId] = useState('')
  const [date, setDate] = useState(toDateInputValue(new Date()))
  const [paymentMethodId, setPaymentMethodId] = useState('')
  const [discount, setDiscount] = useState('')
  const [freight, setFreight] = useState('')
  const [search, setSearch] = useState('')
  const [items, setItems] = useState<PurchaseItem[]>([])
  const debouncedSearch = useDebounce(search, 300)

  const { data: suppliers } = useQuery({ queryKey: ['suppliers'], queryFn: getSuppliers })
  const { data: paymentMethods } = useQuery({ queryKey: ['payment-methods'], queryFn: paymentMethodsService.list })

  const searchResult = useQuery({
    queryKey: ['purchases', 'product-search', debouncedSearch],
    queryFn: () => getProducts({ name: debouncedSearch, isActive: true, pageSize: 8 }),
    enabled: debouncedSearch.length >= 2,
  })

  const itemsTotal = useMemo(
    () => items.reduce((sum, i) => sum + i.unitCost * i.quantity, 0),
    [items],
  )
  const discountValue = Math.max(0, Number(discount) || 0)
  const freightValue = Math.max(0, Number(freight) || 0)
  const total = Math.max(0, itemsTotal - discountValue + freightValue)

  function addItem(product: ProductResponse) {
    setItems((prev) => {
      if (prev.some((i) => i.product.id === product.id)) return prev
      return [...prev, { product, quantity: 1, unitCost: product.costPrice }]
    })
    setSearch('')
  }

  function updateItem(productId: string, patch: Partial<Omit<PurchaseItem, 'product'>>) {
    setItems((prev) => prev.map((i) => (i.product.id === productId ? { ...i, ...patch } : i)))
  }

  const mutation = useMutation({
    mutationFn: async (request: Parameters<typeof createPurchase>[0]) => {
      const purchase = await createPurchase(request)
      await confirmPurchase(purchase.id)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] })
      queryClient.invalidateQueries({ queryKey: ['financial'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      toast({ variant: 'success', title: 'Compra registrada e confirmada com sucesso.' })
      setItems([])
      setSupplierId('')
      setDiscount('')
      setFreight('')
    },
    onError: (error) => {
      toast({ variant: 'error', title: 'Não foi possível registrar a compra.', description: error.message })
    },
  })

  function submit() {
    if (!supplierId || !paymentMethodId || items.length === 0) return
    mutation.mutate({
      supplierId,
      date: new Date(date + 'T12:00:00').toISOString(),
      discount: discountValue,
      freight: freightValue,
      paymentMethodId,
      items: items.map((i) => ({ productId: i.product.id, quantity: i.quantity, unitCost: i.unitCost })),
    })
  }

  return (
    <>
      <PageHeader title="Nova compra" description="Registre uma compra de fornecedor" />

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="space-y-4 lg:col-span-2">
          <Card>
            <CardContent className="grid gap-4 pt-6 sm:grid-cols-2">
              <FormField label="Fornecedor" htmlFor="supplier" required>
                <Select id="supplier" value={supplierId} onChange={(e) => setSupplierId(e.target.value)}>
                  <option value="">Selecione...</option>
                  {suppliers?.filter((s) => s.isActive).map((s) => (
                    <option key={s.id} value={s.id}>{s.name}</option>
                  ))}
                </Select>
              </FormField>
              <FormField label="Data" htmlFor="date" required>
                <Input id="date" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
              </FormField>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <Input
                placeholder="Buscar produto (mín. 2 letras)..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                aria-label="Buscar produto"
              />
              {debouncedSearch.length >= 2 && searchResult.data && (
                <div className="mt-2 max-h-56 overflow-y-auto rounded-md border">
                  {searchResult.data.items.length === 0 ? (
                    <p className="p-3 text-sm text-muted-foreground">Nenhum produto encontrado.</p>
                  ) : (
                    searchResult.data.items.map((p) => (
                      <button
                        key={p.id}
                        className="flex w-full items-center justify-between px-3 py-2 text-left text-sm hover:bg-accent"
                        onClick={() => addItem(p)}
                      >
                        <span>{p.name}</span>
                        <span className="text-muted-foreground">custo: {formatCurrency(p.costPrice)}</span>
                      </button>
                    ))
                  )}
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Itens da compra</CardTitle>
            </CardHeader>
            <CardContent>
              {items.length === 0 ? (
                <p className="py-8 text-center text-sm text-muted-foreground">Nenhum item adicionado.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Produto</TableHead>
                      <TableHead className="w-32 text-center">Qtd.</TableHead>
                      <TableHead className="w-32 text-right">Custo unit.</TableHead>
                      <TableHead className="text-right">Subtotal</TableHead>
                      <TableHead className="w-12" />
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {items.map((item) => (
                      <TableRow key={item.product.id}>
                        <TableCell className="font-medium">{item.product.name}</TableCell>
                        <TableCell>
                          <div className="flex items-center justify-center gap-1">
                            <Button
                              variant="outline"
                              size="icon"
                              className="size-7"
                              onClick={() => updateItem(item.product.id, { quantity: Math.max(1, item.quantity - 1) })}
                              aria-label="Diminuir quantidade"
                            >
                              <Minus />
                            </Button>
                            <span className="w-8 text-center">{item.quantity}</span>
                            <Button
                              variant="outline"
                              size="icon"
                              className="size-7"
                              onClick={() => updateItem(item.product.id, { quantity: item.quantity + 1 })}
                              aria-label="Aumentar quantidade"
                            >
                              <Plus />
                            </Button>
                          </div>
                        </TableCell>
                        <TableCell>
                          <Input
                            type="number"
                            min="0"
                            step="0.01"
                            className="h-8 w-28 text-right"
                            value={item.unitCost}
                            onChange={(e) => updateItem(item.product.id, { unitCost: Math.max(0, Number(e.target.value) || 0) })}
                            aria-label="Custo unitário"
                          />
                        </TableCell>
                        <TableCell className="text-right font-medium">
                          {formatCurrency(item.unitCost * item.quantity)}
                        </TableCell>
                        <TableCell>
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => setItems((prev) => prev.filter((i) => i.product.id !== item.product.id))}
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
              <Select id="paymentMethod" value={paymentMethodId} onChange={(e) => setPaymentMethodId(e.target.value)}>
                <option value="">Selecione...</option>
                {paymentMethods?.filter((m) => m.isActive).map((m) => (
                  <option key={m.id} value={m.id}>{m.name}</option>
                ))}
              </Select>
            </FormField>
            <FormField label="Desconto (R$)" htmlFor="discount">
              <Input id="discount" type="number" min="0" step="0.01" value={discount} onChange={(e) => setDiscount(e.target.value)} />
            </FormField>
            <FormField label="Frete (R$)" htmlFor="freight">
              <Input id="freight" type="number" min="0" step="0.01" value={freight} onChange={(e) => setFreight(e.target.value)} />
            </FormField>
            <div className="space-y-1 border-t pt-4 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Itens</span>
                <span>{formatCurrency(itemsTotal)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Desconto</span>
                <span>- {formatCurrency(discountValue)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Frete</span>
                <span>+ {formatCurrency(freightValue)}</span>
              </div>
              <div className="flex justify-between text-lg font-bold">
                <span>Total</span>
                <span>{formatCurrency(total)}</span>
              </div>
            </div>
            <Button
              className="w-full"
              size="lg"
              disabled={!supplierId || !paymentMethodId || items.length === 0 || mutation.isPending}
              onClick={submit}
            >
              {mutation.isPending ? 'Registrando...' : 'Registrar compra'}
            </Button>
          </CardContent>
        </Card>
      </div>
    </>
  )
}
