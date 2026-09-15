import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { changeProductPrices } from '@/services/products/productsService'
import type { ProductResponse } from '@/types/api'
import { useToast } from '@/components/feedback/toast'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { FormField } from '@/components/forms/form-field'

const pricesSchema = z.object({
  costPrice: z.number('Informe um preço válido').min(0, 'Preço inválido'),
  salePrice: z.number('Informe um preço válido').min(0, 'Preço inválido'),
})

type PricesFormData = z.infer<typeof pricesSchema>

interface PricesDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  product: ProductResponse | null
}

export function PricesDialog({ open, onOpenChange, product }: PricesDialogProps) {
  const queryClient = useQueryClient()
  const { toast } = useToast()

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<PricesFormData>({ resolver: zodResolver(pricesSchema) })

  useEffect(() => {
    if (open && product) {
      reset({ costPrice: product.costPrice, salePrice: product.salePrice })
    }
  }, [open, product, reset])

  const mutation = useMutation({
    mutationFn: (data: PricesFormData) => changeProductPrices(product!.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] })
      toast({ variant: 'success', title: 'Preços atualizados com sucesso.' })
      onOpenChange(false)
    },
    onError: (error) => {
      toast({ variant: 'error', title: 'Não foi possível atualizar os preços.', description: error.message })
    },
  })

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Alterar preços</DialogTitle>
          <DialogDescription>{product?.name}</DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit((data) => mutation.mutate(data))} className="space-y-4" noValidate>
          <FormField label="Preço de custo" htmlFor="costPrice" error={errors.costPrice?.message} required>
            <Input id="costPrice" type="number" step="0.01" min="0" {...register('costPrice', { valueAsNumber: true })} />
          </FormField>
          <FormField label="Preço de venda" htmlFor="salePrice" error={errors.salePrice?.message} required>
            <Input id="salePrice" type="number" step="0.01" min="0" {...register('salePrice', { valueAsNumber: true })} />
          </FormField>
          <DialogFooter>
            <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isSubmitting}>
              Cancelar
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Salvando...' : 'Salvar'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
