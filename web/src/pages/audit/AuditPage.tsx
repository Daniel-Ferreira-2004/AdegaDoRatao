import { useState } from 'react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { getAuditLogs } from '@/services/audit/auditService'
import { formatDateTime } from '@/utils/format'
import { PageHeader } from '@/components/common/page-header'
import { Pagination } from '@/components/common/pagination'
import { EmptyState, ErrorState, LoadingState } from '@/components/feedback/states'
import { Input } from '@/components/ui/input'
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

const actionVariant: Record<string, 'default' | 'success' | 'warning' | 'destructive' | 'secondary'> = {
  Create: 'success',
  Update: 'warning',
  Delete: 'destructive',
}

export default function AuditPage() {
  const [entityName, setEntityName] = useState('')
  const [page, setPage] = useState(1)

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['audit', { entityName, page }],
    queryFn: () => getAuditLogs({ entityName: entityName || undefined, page, pageSize: 20 }),
    placeholderData: keepPreviousData,
  })

  return (
    <>
      <PageHeader title="Auditoria" description="Histórico de alterações no sistema (somente administradores)" />

      <Card>
        <CardContent className="pt-6">
          <Input
            placeholder="Filtrar por entidade (ex: Product, Sale)..."
            value={entityName}
            onChange={(e) => { setEntityName(e.target.value); setPage(1) }}
            className="mb-4 max-w-xs"
            aria-label="Filtrar por entidade"
          />

          {isLoading ? (
            <LoadingState message="Carregando registros..." />
          ) : isError ? (
            <ErrorState title="Não foi possível carregar a auditoria." onRetry={() => refetch()} />
          ) : !data || data.items.length === 0 ? (
            <EmptyState title="Nenhum registro encontrado" />
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Data</TableHead>
                    <TableHead>Usuário</TableHead>
                    <TableHead>Ação</TableHead>
                    <TableHead>Entidade</TableHead>
                    <TableHead>ID</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((log) => (
                    <TableRow key={log.id}>
                      <TableCell>{formatDateTime(log.createdAt)}</TableCell>
                      <TableCell>{log.userEmail ?? '—'}</TableCell>
                      <TableCell>
                        <Badge variant={actionVariant[log.action] ?? 'secondary'}>{log.action}</Badge>
                      </TableCell>
                      <TableCell>{log.entityName}</TableCell>
                      <TableCell className="font-mono text-xs">{log.entityId.slice(0, 8)}…</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              <Pagination result={data} onPageChange={setPage} />
            </>
          )}
        </CardContent>
      </Card>
    </>
  )
}
