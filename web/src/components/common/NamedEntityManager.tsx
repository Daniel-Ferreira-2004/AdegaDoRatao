import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus, Power } from 'lucide-react'
import type { NamedEntityResponse } from '@/types/api'
import { useAuth } from '@/contexts/AuthContext'
import { useToast } from '@/components/feedback/toast'
import { EmptyState, ErrorState, LoadingState } from '@/components/feedback/states'
import { ConfirmDialog } from '@/components/feedback/confirm-dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { formatDate } from '@/utils/format'

interface NamedEntityManagerProps {
  queryKey: string
  entityLabel: string
  writePermission: string
  service: {
    list: () => Promise<NamedEntityResponse[]>
    create: (request: { name: string }) => Promise<NamedEntityResponse>
    setActive: (id: string, request: { isActive: boolean }) => Promise<void>
  }
}

/** Gerenciador genérico para entidades nomeadas (categorias, marcas, formas de pagamento). */
export function NamedEntityManager({ queryKey, entityLabel, writePermission, service }: NamedEntityManagerProps) {
  const { hasPermission } = useAuth()
  const canWrite = hasPermission(writePermission)
  const { toast } = useToast()
  const queryClient = useQueryClient()

  const [search, setSearch] = useState('')
  const [newName, setNewName] = useState('')
  const [toggling, setToggling] = useState<NamedEntityResponse | null>(null)

  const { data, isLoading, isError, refetch } = useQuery({ queryKey: [queryKey], queryFn: service.list })

  const createMutation = useMutation({
    mutationFn: service.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [queryKey] })
      toast({ variant: 'success', title: `${entityLabel} cadastrada com sucesso.` })
      setNewName('')
    },
    onError: (error) => {
      toast({ variant: 'error', title: `Não foi possível cadastrar.`, description: error.message })
    },
  })

  const toggleMutation = useMutation({
    mutationFn: (entity: NamedEntityResponse) => service.setActive(entity.id, { isActive: !entity.isActive }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [queryKey] })
      toast({ variant: 'success', title: 'Status atualizado.' })
      setToggling(null)
    },
    onError: (error) => {
      toast({ variant: 'error', title: 'Operação não concluída.', description: error.message })
      setToggling(null)
    },
  })

  const filtered = data?.filter((e) => e.name.toLowerCase().includes(search.toLowerCase()))

  return (
    <div>
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <Input
          placeholder="Buscar..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-xs"
          aria-label={`Buscar ${entityLabel.toLowerCase()}`}
        />
        {canWrite && (
          <form
            className="flex gap-2"
            onSubmit={(e) => {
              e.preventDefault()
              const name = newName.trim()
              if (name) createMutation.mutate({ name })
            }}
          >
            <Input
              placeholder={`Nova ${entityLabel.toLowerCase()}...`}
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              className="max-w-xs"
              aria-label={`Nome da nova ${entityLabel.toLowerCase()}`}
            />
            <Button type="submit" disabled={createMutation.isPending || !newName.trim()}>
              <Plus /> Adicionar
            </Button>
          </form>
        )}
      </div>

      {isLoading ? (
        <LoadingState />
      ) : isError ? (
        <ErrorState onRetry={() => refetch()} />
      ) : !filtered || filtered.length === 0 ? (
        <EmptyState title={`Nenhuma ${entityLabel.toLowerCase()} encontrada`} />
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Nome</TableHead>
              <TableHead>Criada em</TableHead>
              <TableHead>Status</TableHead>
              {canWrite && <TableHead className="w-28">Ações</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {filtered.map((entity) => (
              <TableRow key={entity.id}>
                <TableCell className="font-medium">{entity.name}</TableCell>
                <TableCell>{formatDate(entity.createdAt)}</TableCell>
                <TableCell>
                  <Badge variant={entity.isActive ? 'success' : 'secondary'}>
                    {entity.isActive ? 'Ativa' : 'Inativa'}
                  </Badge>
                </TableCell>
                {canWrite && (
                  <TableCell>
                    <Button variant="ghost" size="sm" onClick={() => setToggling(entity)}>
                      <Power /> {entity.isActive ? 'Desativar' : 'Ativar'}
                    </Button>
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <ConfirmDialog
        open={toggling !== null}
        onOpenChange={(open) => !open && setToggling(null)}
        title={toggling?.isActive ? `Desativar ${entityLabel.toLowerCase()}` : `Ativar ${entityLabel.toLowerCase()}`}
        description={
          toggling?.isActive
            ? `Tem certeza que deseja desativar "${toggling?.name}"?`
            : `Deseja reativar "${toggling?.name}"?`
        }
        confirmLabel={toggling?.isActive ? 'Desativar' : 'Ativar'}
        loading={toggleMutation.isPending}
        onConfirm={() => toggling && toggleMutation.mutate(toggling)}
      />
    </div>
  )
}
