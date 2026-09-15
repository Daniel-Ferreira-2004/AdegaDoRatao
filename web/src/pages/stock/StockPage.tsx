import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getLowStock, getProductMovements } from '@/services/stock/stockService'
import { getProducts } from '@/services/products/productsService'
import type { ProductResponse } from '@/types/api'
import { formatDateTime } from '@/utils/format'
import { stockMovementTypeLabel, stockMovementTypeVariant } from '@/utils/enums'
import { useDebounce } from '@/hooks/useDebounce'
import { PageHeader } from '@/components/common/page-header'
import { EmptyState, ErrorState, LoadingState } from '@/components/feedback/states'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

function stockStatus(product: ProductResponse) {
  if (product.currentStock === 0) return <Badge variant="destructive">Sem estoque</Badge>
  if (product.currentStock <= product.minStock) return <Badge variant="warning">Baixo</Badge>
  return <Badge variant="success">Normal</Badge>
}

export default function StockPage() {
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<ProductResponse | null>(null)
  const debouncedSearch = useDebounce(search)

  const lowStock = useQuery({ queryKey: ['stock', 'low'], queryFn: () => getLowStock(true) })

  const searchResult = useQuery({
    queryKey: ['stock', 'search', debouncedSearch],
    queryFn: () => getProducts({ name: debouncedSearch, pageSize: 10 }),
    enabled: debouncedSearch.length >= 2,
  })

  const movements = useQuery({
    queryKey: ['stock', 'movements', selected?.id],
    queryFn: () => getProductMovements(selected!.id),
    enabled: selected !== null,
  })

  return (
    <>
      <PageHeader title="Estoque" description="Posição de estoque e movimentações por produto" />

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Estoque baixo / sem estoque</CardTitle>
          </CardHeader>
          <CardContent>
            {lowStock.isLoading ? (
              <LoadingState />
            ) : lowStock.isError ? (
              <ErrorState onRetry={() => lowStock.refetch()} />
            ) : !lowStock.data || lowStock.data.length === 0 ? (
              <EmptyState title="Nenhum produto com estoque baixo" description="Tudo sob controle por aqui." />
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Produto</TableHead>
                    <TableHead className="text-right">Atual</TableHead>
                    <TableHead className="text-right">Mínimo</TableHead>
                    <TableHead>Situação</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {lowStock.data.map((product) => (
                    <TableRow
                      key={product.id}
                      className="cursor-pointer"
                      onClick={() => setSelected(product)}
                    >
                      <TableCell className="font-medium">{product.name}</TableCell>
                      <TableCell className="text-right">{product.currentStock}</TableCell>
                      <TableCell className="text-right">{product.minStock}</TableCell>
                      <TableCell>{stockStatus(product)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Movimentações por produto</CardTitle>
          </CardHeader>
          <CardContent>
            <Input
              placeholder="Buscar produto (mín. 2 letras)..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="mb-3"
              aria-label="Buscar produto"
            />
            {searchResult.data && debouncedSearch.length >= 2 && (
              <div className="mb-4 max-h-48 overflow-y-auto rounded-md border">
                {searchResult.data.items.length === 0 ? (
                  <p className="p-3 text-sm text-muted-foreground">Nenhum produto encontrado.</p>
                ) : (
                  searchResult.data.items.map((p) => (
                    <button
                      key={p.id}
                      className="flex w-full items-center justify-between px-3 py-2 text-left text-sm hover:bg-accent"
                      onClick={() => { setSelected(p); setSearch('') }}
                    >
                      <span>{p.name}</span>
                      <span className="text-muted-foreground">estoque: {p.currentStock}</span>
                    </button>
                  ))
                )}
              </div>
            )}

            {selected === null ? (
              <EmptyState title="Selecione um produto" description="Busque acima ou clique em um item da lista de estoque baixo." />
            ) : (
              <>
                <div className="mb-3 flex items-center justify-between rounded-md bg-muted p-3">
                  <div>
                    <p className="font-medium">{selected.name}</p>
                    <p className="text-sm text-muted-foreground">Estoque atual: {selected.currentStock}</p>
                  </div>
                  {stockStatus(selected)}
                </div>
                {movements.isLoading ? (
                  <LoadingState />
                ) : movements.isError ? (
                  <ErrorState onRetry={() => movements.refetch()} />
                ) : !movements.data || movements.data.length === 0 ? (
                  <EmptyState title="Nenhuma movimentação registrada" />
                ) : (
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Data</TableHead>
                        <TableHead>Tipo</TableHead>
                        <TableHead className="text-right">Qtd.</TableHead>
                        <TableHead className="text-right">Saldo</TableHead>
                        <TableHead>Motivo</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {movements.data.map((mov) => (
                        <TableRow key={mov.id}>
                          <TableCell>{formatDateTime(mov.createdAt)}</TableCell>
                          <TableCell>
                            <Badge variant={stockMovementTypeVariant[mov.type]}>
                              {stockMovementTypeLabel[mov.type]}
                            </Badge>
                          </TableCell>
                          <TableCell className="text-right">{mov.quantity}</TableCell>
                          <TableCell className="text-right">
                            {mov.previousStock} → {mov.newStock}
                          </TableCell>
                          <TableCell className="max-w-[200px] truncate">{mov.reason}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </>
            )}
          </CardContent>
        </Card>
      </div>
    </>
  )
}
