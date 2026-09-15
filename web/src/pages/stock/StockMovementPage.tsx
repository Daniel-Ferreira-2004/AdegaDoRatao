import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { registerMovement } from '@/services/stock/stockService'
import { getProducts } from '@/services/products/productsService'
import { StockMovementType, type ProductResponse } from '@/types/api'
import { useDebounce } from '@/hooks/useDebounce'
import { useToast } from '@/components/feedback/toast'
import { PageHeader } from '@/components/common/page-header'
import { Button } from '@/components/ui/button'
import { Input, Textarea } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Card, CardContent } from '@/components/ui/card'
import { FormField } from '@/components/forms/form-field'

const movementSchema = z.object({
  type: z.number('Selecione o tipo').refine((v) => [1, 2, 3].includes(v), 'Selecione o tipo'),
  quantity: z.number('Informe a quantidade').int('Quantidade deve ser inteira').min(1, 'Quantidade deve ser maior que zero'),
  reason: z.string().min(1, 'Informe o motivo').max(200),
})

type MovementFormData = z.infer<typeof movementSchema>

export default function StockMovementPage() {
  const { toast } = useToast()
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [product, setProduct] = useState<ProductResponse | null>(null)
  const debouncedSearch = useDebounce(search)

  const searchResult = useQuery({
    queryKey: ['stock', 'search', debouncedSearch],
    queryFn: () => getProducts({ name: debouncedSearch, pageSize: 10, isActive: true }),
    enabled: debouncedSearch.length >= 2 && product === null,
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<MovementFormData>({ resolver: zodResolver(movementSchema) })

  const mutation = useMutation({
    mutationFn: registerMovement,
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['stock'] })
      queryClient.invalidateQueries({ queryKey: ['products'] })
      toast({ variant: 'success', title: 'Movimentação registrada com sucesso.' })
      reset()
      setProduct(null)
      setSearch('')
      void variables
    },
    onError: (error) => {
      toast({ variant: 'error', title: 'Não foi possível registrar a movimentação.', description: error.message })
    },
  })

  function onSubmit(data: MovementFormData) {
    if (!product) return
    mutation.mutate({
      productId: product.id,
      type: data.type as 1 | 2 | 3,
      quantity: data.quantity,
      reason: data.reason,
    })
  }

  return (
    <>
      <PageHeader title="Nova movimentação" description="Registre entradas, saídas e ajustes de estoque" />
      <Card className="max-w-xl">
        <CardContent className="pt-6">
          <div className="mb-4 space-y-2">
            <FormField label="Produto" required>
              {product ? (
                <div className="flex items-center justify-between rounded-md border bg-muted p-3">
                  <div>
                    <p className="font-medium">{product.name}</p>
                    <p className="text-sm text-muted-foreground">Estoque atual: {product.currentStock}</p>
                  </div>
                  <Button variant="ghost" size="sm" onClick={() => setProduct(null)}>
                    Trocar
                  </Button>
                </div>
              ) : (
                <>
                  <Input
                    placeholder="Buscar produto (mín. 2 letras)..."
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                  />
                  {searchResult.data && debouncedSearch.length >= 2 && (
                    <div className="max-h-48 overflow-y-auto rounded-md border">
                      {searchResult.data.items.length === 0 ? (
                        <p className="p-3 text-sm text-muted-foreground">Nenhum produto encontrado.</p>
                      ) : (
                        searchResult.data.items.map((p) => (
                          <button
                            key={p.id}
                            className="flex w-full items-center justify-between px-3 py-2 text-left text-sm hover:bg-accent"
                            onClick={() => { setProduct(p); setSearch('') }}
                          >
                            <span>{p.name}</span>
                            <span className="text-muted-foreground">estoque: {p.currentStock}</span>
                          </button>
                        ))
                      )}
                    </div>
                  )}
                </>
              )}
            </FormField>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
            <FormField label="Tipo de movimentação" htmlFor="type" error={errors.type?.message} required>
              <Select id="type" defaultValue="" {...register('type', { valueAsNumber: true })}>
                <option value="" disabled>Selecione...</option>
                <option value={StockMovementType.Entrada}>Entrada</option>
                <option value={StockMovementType.Saida}>Saída</option>
                <option value={StockMovementType.Ajuste}>Ajuste</option>
              </Select>
            </FormField>
            <FormField label="Quantidade" htmlFor="quantity" error={errors.quantity?.message} required>
              <Input id="quantity" type="number" min="1" {...register('quantity', { valueAsNumber: true })} />
            </FormField>
            <FormField label="Motivo" htmlFor="reason" error={errors.reason?.message} required>
              <Textarea id="reason" placeholder="Ex: Compra do fornecedor X, produto danificado..." {...register('reason')} />
            </FormField>
            <Button type="submit" disabled={isSubmitting || !product} className="w-full">
              {isSubmitting ? 'Registrando...' : 'Registrar movimentação'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </>
  )
}
