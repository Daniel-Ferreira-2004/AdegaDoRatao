import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { brandsService, categoriesService } from '@/services/catalog/catalogService'
import { createProduct, updateProduct } from '@/services/products/productsService'
import { ApiError } from '@/services/api/apiClient'
import type { ProductResponse } from '@/types/api'
import { useToast } from '@/components/feedback/toast'
import { Button } from '@/components/ui/button'
import { Input, Textarea } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { FormField } from '@/components/forms/form-field'

const productSchema = z.object({
  name: z.string().min(1, 'Informe o nome').max(200),
  sku: z.string().min(1, 'Informe o SKU').max(50),
  barcode: z.string().max(50).optional().or(z.literal('')),
  description: z.string().max(500).optional().or(z.literal('')),
  categoryId: z.string().min(1, 'Selecione a categoria'),
  brandId: z.string().min(1, 'Selecione a marca'),
  unitOfMeasure: z.string().min(1, 'Informe a unidade').max(10),
  costPrice: z.number('Informe um preço válido').min(0, 'Preço inválido'),
  salePrice: z.number('Informe um preço válido').min(0, 'Preço inválido'),
  minStock: z.number('Informe um valor válido').int('Deve ser inteiro').min(0, 'Estoque mínimo inválido'),
  maxStock: z.union([z.nan(), z.number().int('Deve ser inteiro').min(0, 'Estoque máximo inválido')]),
  allowNegativeStock: z.boolean(),
})

type ProductFormData = z.infer<typeof productSchema>

interface ProductFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  product?: ProductResponse | null
}

export function ProductFormDialog({ open, onOpenChange, product }: ProductFormDialogProps) {
  const isEditing = Boolean(product)
  const queryClient = useQueryClient()
  const { toast } = useToast()

  const { data: categories } = useQuery({ queryKey: ['categories'], queryFn: categoriesService.list })
  const { data: brands } = useQuery({ queryKey: ['brands'], queryFn: brandsService.list })

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ProductFormData>({
    resolver: zodResolver(productSchema),
    defaultValues: { allowNegativeStock: false, unitOfMeasure: 'UN' },
  })

  useEffect(() => {
    if (open) {
      reset(
        product
          ? {
              name: product.name,
              sku: product.sku,
              barcode: product.barcode ?? '',
              description: product.description ?? '',
              categoryId: product.categoryId,
              brandId: product.brandId,
              unitOfMeasure: product.unitOfMeasure,
              costPrice: product.costPrice,
              salePrice: product.salePrice,
              minStock: product.minStock,
              maxStock: product.maxStock ?? Number.NaN,
              allowNegativeStock: product.allowNegativeStock,
            }
          : { allowNegativeStock: false, unitOfMeasure: 'UN', name: '', sku: '', categoryId: '', brandId: '', maxStock: Number.NaN },
      )
    }
  }, [open, product, reset])

  const mutation = useMutation({
    mutationFn: async (data: ProductFormData) => {
      const payload = {
        name: data.name,
        description: data.description || null,
        categoryId: data.categoryId,
        brandId: data.brandId,
        unitOfMeasure: data.unitOfMeasure,
        minStock: data.minStock,
        maxStock: Number.isNaN(data.maxStock) ? null : data.maxStock,
        allowNegativeStock: data.allowNegativeStock,
      }
      if (product) {
        return updateProduct(product.id, payload)
      }
      return createProduct({
        ...payload,
        sku: data.sku,
        barcode: data.barcode || null,
        costPrice: data.costPrice,
        salePrice: data.salePrice,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] })
      toast({
        variant: 'success',
        title: isEditing ? 'Produto atualizado com sucesso.' : 'Produto cadastrado com sucesso.',
      })
      onOpenChange(false)
    },
    onError: (error) => {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        for (const [field, messages] of Object.entries(error.fieldErrors)) {
          const fieldName = field.charAt(0).toLowerCase() + field.slice(1)
          setError(fieldName as keyof ProductFormData, { message: messages[0] })
        }
      }
      toast({ variant: 'error', title: 'Não foi possível salvar o produto.', description: error.message })
    },
  })

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Editar produto' : 'Novo produto'}</DialogTitle>
          <DialogDescription>
            {isEditing
              ? 'Atualize as informações do produto. Preços são alterados na tela de listagem.'
              : 'Preencha os dados para cadastrar um novo produto.'}
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit((data) => mutation.mutate(data))} className="space-y-4" noValidate>
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label="Nome" htmlFor="name" error={errors.name?.message} required>
              <Input id="name" {...register('name')} />
            </FormField>
            <FormField label="SKU" htmlFor="sku" error={errors.sku?.message} required>
              <Input id="sku" disabled={isEditing} {...register('sku')} />
            </FormField>
            <FormField label="Código de barras" htmlFor="barcode" error={errors.barcode?.message}>
              <Input id="barcode" disabled={isEditing} {...register('barcode')} />
            </FormField>
            <FormField label="Unidade" htmlFor="unitOfMeasure" error={errors.unitOfMeasure?.message} required>
              <Input id="unitOfMeasure" placeholder="UN, CX, LT..." {...register('unitOfMeasure')} />
            </FormField>
            <FormField label="Categoria" htmlFor="categoryId" error={errors.categoryId?.message} required>
              <Select id="categoryId" {...register('categoryId')}>
                <option value="">Selecione...</option>
                {categories?.filter((c) => c.isActive).map((c) => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </Select>
            </FormField>
            <FormField label="Marca" htmlFor="brandId" error={errors.brandId?.message} required>
              <Select id="brandId" {...register('brandId')}>
                <option value="">Selecione...</option>
                {brands?.filter((b) => b.isActive).map((b) => (
                  <option key={b.id} value={b.id}>{b.name}</option>
                ))}
              </Select>
            </FormField>
            {!isEditing && (
              <>
                <FormField label="Preço de custo" htmlFor="costPrice" error={errors.costPrice?.message} required>
                  <Input id="costPrice" type="number" step="0.01" min="0" {...register('costPrice', { valueAsNumber: true })} />
                </FormField>
                <FormField label="Preço de venda" htmlFor="salePrice" error={errors.salePrice?.message} required>
                  <Input id="salePrice" type="number" step="0.01" min="0" {...register('salePrice', { valueAsNumber: true })} />
                </FormField>
              </>
            )}
            <FormField label="Estoque mínimo" htmlFor="minStock" error={errors.minStock?.message} required>
              <Input id="minStock" type="number" min="0" {...register('minStock', { valueAsNumber: true })} />
            </FormField>
            <FormField label="Estoque máximo" htmlFor="maxStock" error={errors.maxStock?.message}>
              <Input id="maxStock" type="number" min="0" {...register('maxStock', { valueAsNumber: true })} />
            </FormField>
          </div>
          <FormField label="Descrição" htmlFor="description" error={errors.description?.message}>
            <Textarea id="description" {...register('description')} />
          </FormField>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" className="size-4 accent-primary" {...register('allowNegativeStock')} />
            Permitir estoque negativo
          </label>
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
