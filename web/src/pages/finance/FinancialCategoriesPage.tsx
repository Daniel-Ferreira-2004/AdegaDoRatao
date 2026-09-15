import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus, Power } from 'lucide-react'
import {
  createFinancialCategory,
  getFinancialCategories,
  setFinancialCategoryActive,
} from '@/services/financial/financialService'
import { FinancialTransactionType, type FinancialCategoryResponse } from '@/types/api'
import { financialTypeLabel, financialTypeVariant } from '@/utils/enums'
import { useAuth } from '@/contexts/AuthContext'
import { useToast } from '@/components/feedback/toast'
import { PageHeader } from '@/components/common/page-header'
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

export default function FinancialCategoriesPage() {
  const { hasPermission } = useAuth()
  const canWrite = hasPermission('financial.write')
  const { toast } = useToast()
  const queryClient = useQueryClient()

  const [name, setName] = useState('')
  const [type, setType] = useState('')
  const [toggling, setToggling] = useState<FinancialCategoryResponse | null>(null)

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['financial-categories'],
    queryFn: getFinancialCategories,
  })

  const createMutation = useMutation({
    mutationFn: createFinancialCategory,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['financial-categories'] })
      toast({ variant: 'success', title: 'Categoria cadastrada com sucesso.' })
      setName('')
      setType('')
    },
    onError: (error) => {
      toast({ variant: 'error', title: 'Não foi possível cadastrar.', description: error.message })
    },
  })

  const toggleMutation = useMutation({
    mutationFn: (category: FinancialCategoryResponse) =>
      setFinancialCategoryActive(category.id, { isActive: !category.isActive }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['financial-categories'] })
      toast({ variant: 'success', title: 'Status atualizado.' })
      setToggling(null)
    },
    onError: (error) => {
      toast({ variant: 'error', title: 'Operação não concluída.', description: error.message })
      setToggling(null)
    },
  })

  return (
    <>
      <PageHeader title="Categorias financeiras" description="Classifique entradas e saídas" />

      <Card>
        <CardContent className="pt-6">
          {canWrite && (
            <form
              className="mb-4 flex flex-wrap gap-2"
              onSubmit={(e) => {
                e.preventDefault()
                if (name.trim() && type) {
                  createMutation.mutate({ name: name.trim(), type: Number(type) as 1 | 2 })
                }
              }}
            >
              <Input
                placeholder="Nova categoria..."
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="max-w-xs"
                aria-label="Nome da nova categoria"
              />
              <Select value={type} onChange={(e) => setType(e.target.value)} className="max-w-[160px]" aria-label="Tipo">
                <option value="">Tipo...</option>
                <option value={FinancialTransactionType.Entrada}>Entrada</option>
                <option value={FinancialTransactionType.Saida}>Saída</option>
              </Select>
              <Button type="submit" disabled={createMutation.isPending || !name.trim() || !type}>
                <Plus /> Adicionar
              </Button>
            </form>
          )}

          {isLoading ? (
            <LoadingState />
          ) : isError ? (
            <ErrorState onRetry={() => refetch()} />
          ) : !data || data.length === 0 ? (
            <EmptyState title="Nenhuma categoria cadastrada" />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome</TableHead>
                  <TableHead>Tipo</TableHead>
                  <TableHead>Status</TableHead>
                  {canWrite && <TableHead className="w-28">Ações</TableHead>}
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.map((category) => (
                  <TableRow key={category.id}>
                    <TableCell className="font-medium">{category.name}</TableCell>
                    <TableCell>
                      <Badge variant={financialTypeVariant[category.type]}>
                        {financialTypeLabel[category.type]}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={category.isActive ? 'success' : 'secondary'}>
                        {category.isActive ? 'Ativa' : 'Inativa'}
                      </Badge>
                    </TableCell>
                    {canWrite && (
                      <TableCell>
                        <Button variant="ghost" size="sm" onClick={() => setToggling(category)}>
                          <Power /> {category.isActive ? 'Desativar' : 'Ativar'}
                        </Button>
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <ConfirmDialog
        open={toggling !== null}
        onOpenChange={(open) => !open && setToggling(null)}
        title={toggling?.isActive ? 'Desativar categoria' : 'Ativar categoria'}
        description={
          toggling?.isActive
            ? `Tem certeza que deseja desativar "${toggling?.name}"?`
            : `Deseja reativar "${toggling?.name}"?`
        }
        confirmLabel={toggling?.isActive ? 'Desativar' : 'Ativar'}
        loading={toggleMutation.isPending}
        onConfirm={() => toggling && toggleMutation.mutate(toggling)}
      />
    </>
  )
}
