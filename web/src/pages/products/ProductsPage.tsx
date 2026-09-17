import { useState } from 'react'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { MoreHorizontal, Pencil, Plus, DollarSign, Power, Store, Trash2 } from 'lucide-react'
import { deleteProduct, getProducts, setProductActive } from '@/services/products/productsService'
import { categoriesService } from '@/services/catalog/catalogService'
import type { ProductResponse } from '@/types/api'
import { formatCurrency } from '@/utils/format'
import { useDebounce } from '@/hooks/useDebounce'
import { useAuth } from '@/contexts/AuthContext'
import { useToast } from '@/components/feedback/toast'
import { PageHeader } from '@/components/common/page-header'
import { Pagination } from '@/components/common/pagination'
import { EmptyState, ErrorState, LoadingState } from '@/components/feedback/states'
import { ConfirmDialog } from '@/components/feedback/confirm-dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { ProductFormDialog } from './ProductFormDialog'
import { PricesDialog } from './PricesDialog'
import { MarketPricesDialog } from './MarketPricesDialog'

export default function ProductsPage() {
  const { hasPermission } = useAuth()
  const canWrite = hasPermission('products.write')
  const { toast } = useToast()
  const queryClient = useQueryClient()

  const [search, setSearch] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [page, setPage] = useState(1)
  const debouncedSearch = useDebounce(search)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<ProductResponse | null>(null)
  const [pricesProduct, setPricesProduct] = useState<ProductResponse | null>(null)
  const [marketPricesProduct, setMarketPricesProduct] = useState<ProductResponse | null>(null)
  const [toggling, setToggling] = useState<ProductResponse | null>(null)
  const [deleting, setDeleting] = useState<ProductResponse | null>(null)

  const { data: categories } = useQuery({ queryKey: ['categories'], queryFn: categoriesService.list })

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['products', { search: debouncedSearch, categoryId, statusFilter, page }],
    queryFn: () =>
      getProducts({
        name: debouncedSearch || undefined,
        categoryId: categoryId || undefined,
        isActive: statusFilter === '' ? undefined : statusFilter === 'active',
        page,
        pageSize: 20,
      }),
    placeholderData: keepPreviousData,
  })

  const toggleMutation = useMutation({
    mutationFn: (product: ProductResponse) => setProductActive(product.id, { isActive: !product.isActive }),
    onSuccess: (_, product) => {
      queryClient.invalidateQueries({ queryKey: ['products'] })
      toast({
        variant: 'success',
        title: product.isActive ? 'Produto desativado.' : 'Produto ativado.',
      })
      setToggling(null)
    },
    onError: (error) => {
      toast({ variant: 'error', title: 'Operação não concluída.', description: error.message })
      setToggling(null)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (product: ProductResponse) => deleteProduct(product.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] })
      toast({ variant: 'success', title: 'Produto excluído com sucesso.' })
      setDeleting(null)
    },
    onError: (error) => {
      toast({ variant: 'error', title: 'Não foi possível excluir o produto.', description: error.message })
      setDeleting(null)
    },
  })

  function stockBadge(product: ProductResponse) {
    if (product.currentStock === 0) return <Badge variant="destructive">Sem estoque</Badge>
    if (product.currentStock <= product.minStock) return <Badge variant="warning">Baixo</Badge>
    return <Badge variant="success">Normal</Badge>
  }

  return (
    <>
      <PageHeader
        title="Produtos"
        description="Gerencie o catálogo de produtos"
        actions={
          canWrite && (
            <Button onClick={() => { setEditing(null); setFormOpen(true) }}>
              <Plus /> Novo produto
            </Button>
          )
        }
      />

      <Card>
        <CardContent className="pt-6">
          <div className="mb-4 flex flex-wrap gap-3">
            <Input
              placeholder="Buscar por nome..."
              value={search}
              onChange={(e) => { setSearch(e.target.value); setPage(1) }}
              className="max-w-xs"
              aria-label="Buscar produto"
            />
            <Select
              value={categoryId}
              onChange={(e) => { setCategoryId(e.target.value); setPage(1) }}
              className="max-w-[200px]"
              aria-label="Filtrar por categoria"
            >
              <option value="">Todas as categorias</option>
              {categories?.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </Select>
            <Select
              value={statusFilter}
              onChange={(e) => { setStatusFilter(e.target.value); setPage(1) }}
              className="max-w-[160px]"
              aria-label="Filtrar por status"
            >
              <option value="">Todos</option>
              <option value="active">Ativos</option>
              <option value="inactive">Inativos</option>
            </Select>
          </div>

          {isLoading ? (
            <LoadingState message="Carregando produtos..." />
          ) : isError ? (
            <ErrorState title="Não foi possível carregar os produtos." onRetry={() => refetch()} />
          ) : !data || data.items.length === 0 ? (
            <EmptyState title="Nenhum produto encontrado" description="Ajuste os filtros ou cadastre um novo produto." />
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>SKU</TableHead>
                    <TableHead>Nome</TableHead>
                    <TableHead className="text-right">Custo</TableHead>
                    <TableHead className="text-right">Venda</TableHead>
                    <TableHead className="text-right">Estoque</TableHead>
                    <TableHead>Situação</TableHead>
                    <TableHead>Status</TableHead>
                    {canWrite && <TableHead className="w-12" />}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((product) => (
                    <TableRow key={product.id}>
                      <TableCell className="font-mono text-xs">{product.sku}</TableCell>
                      <TableCell className="font-medium">{product.name}</TableCell>
                      <TableCell className="text-right">{formatCurrency(product.costPrice)}</TableCell>
                      <TableCell className="text-right">{formatCurrency(product.salePrice)}</TableCell>
                      <TableCell className="text-right">
                        {product.currentStock} {product.unitOfMeasure}
                      </TableCell>
                      <TableCell>{stockBadge(product)}</TableCell>
                      <TableCell>
                        <Badge variant={product.isActive ? 'success' : 'secondary'}>
                          {product.isActive ? 'Ativo' : 'Inativo'}
                        </Badge>
                      </TableCell>
                      {canWrite && (
                        <TableCell>
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <Button variant="ghost" size="icon" aria-label={`Ações de ${product.name}`}>
                                <MoreHorizontal />
                              </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="end">
                              <DropdownMenuItem onClick={() => { setEditing(product); setFormOpen(true) }}>
                                <Pencil /> Editar
                              </DropdownMenuItem>
                              <DropdownMenuItem onClick={() => setPricesProduct(product)}>
                                <DollarSign /> Alterar preços
                              </DropdownMenuItem>
                              <DropdownMenuItem
                                onClick={() => setMarketPricesProduct(product)}
                                disabled={!product.barcode}
                              >
                                <Store /> Comparar preços
                              </DropdownMenuItem>
                              <DropdownMenuItem onClick={() => setToggling(product)}>
                                <Power /> {product.isActive ? 'Desativar' : 'Ativar'}
                              </DropdownMenuItem>
                              <DropdownMenuItem
                                onClick={() => setDeleting(product)}
                                className="text-destructive focus:text-destructive"
                              >
                                <Trash2 /> Excluir
                              </DropdownMenuItem>
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </TableCell>
                      )}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              <Pagination result={data} onPageChange={setPage} />
            </>
          )}
        </CardContent>
      </Card>

      <ProductFormDialog open={formOpen} onOpenChange={setFormOpen} product={editing} />
      <PricesDialog open={pricesProduct !== null} onOpenChange={(open) => !open && setPricesProduct(null)} product={pricesProduct} />
      <MarketPricesDialog open={marketPricesProduct !== null} onOpenChange={(open) => !open && setMarketPricesProduct(null)} product={marketPricesProduct} />
      <ConfirmDialog
        open={toggling !== null}
        onOpenChange={(open) => !open && setToggling(null)}
        title={toggling?.isActive ? 'Desativar produto' : 'Ativar produto'}
        description={
          toggling?.isActive
            ? `Tem certeza que deseja desativar "${toggling?.name}"? Ele não aparecerá para novas vendas.`
            : `Deseja reativar "${toggling?.name}"?`
        }
        confirmLabel={toggling?.isActive ? 'Desativar' : 'Ativar'}
        loading={toggleMutation.isPending}
        onConfirm={() => toggling && toggleMutation.mutate(toggling)}
      />
      <ConfirmDialog
        open={deleting !== null}
        onOpenChange={(open) => !open && setDeleting(null)}
        title="Excluir produto"
        description={`Tem certeza que deseja excluir "${deleting?.name}"? Esta ação não pode ser desfeita. Produtos com histórico de movimentações, compras ou vendas não podem ser excluídos — nesse caso, desative o produto.`}
        confirmLabel="Excluir"
        loading={deleteMutation.isPending}
        onConfirm={() => deleting && deleteMutation.mutate(deleting)}
      />
    </>
  )
}
