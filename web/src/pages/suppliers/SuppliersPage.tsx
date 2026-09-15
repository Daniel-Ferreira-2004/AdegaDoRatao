import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { MoreHorizontal, Pencil, Plus, Power } from 'lucide-react'
import {
  createSupplier,
  getSuppliers,
  setSupplierActive,
  updateSupplier,
} from '@/services/suppliers/suppliersService'
import type { SupplierResponse } from '@/types/api'
import { useAuth } from '@/contexts/AuthContext'
import { useToast } from '@/components/feedback/toast'
import { PageHeader } from '@/components/common/page-header'
import { EmptyState, ErrorState, LoadingState } from '@/components/feedback/states'
import { ConfirmDialog } from '@/components/feedback/confirm-dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { FormField } from '@/components/forms/form-field'

const supplierSchema = z.object({
  name: z.string().min(1, 'Informe o nome').max(200),
  document: z.string().max(20).optional().or(z.literal('')),
  phone: z.string().max(20).optional().or(z.literal('')),
  email: z.string().email('E-mail inválido').optional().or(z.literal('')),
})

type SupplierFormData = z.infer<typeof supplierSchema>

export default function SuppliersPage() {
  const { hasPermission } = useAuth()
  const canWrite = hasPermission('purchases.write')
  const { toast } = useToast()
  const queryClient = useQueryClient()

  const [search, setSearch] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<SupplierResponse | null>(null)
  const [toggling, setToggling] = useState<SupplierResponse | null>(null)

  const { data, isLoading, isError, refetch } = useQuery({ queryKey: ['suppliers'], queryFn: getSuppliers })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<SupplierFormData>({ resolver: zodResolver(supplierSchema) })

  useEffect(() => {
    if (formOpen) {
      reset(
        editing
          ? {
              name: editing.name,
              document: editing.document ?? '',
              phone: editing.phone ?? '',
              email: editing.email ?? '',
            }
          : { name: '', document: '', phone: '', email: '' },
      )
    }
  }, [formOpen, editing, reset])

  const saveMutation = useMutation({
    mutationFn: (form: SupplierFormData) => {
      const payload = {
        name: form.name,
        document: form.document || null,
        phone: form.phone || null,
        email: form.email || null,
      }
      return editing ? updateSupplier(editing.id, payload) : createSupplier(payload)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['suppliers'] })
      toast({ variant: 'success', title: editing ? 'Fornecedor atualizado.' : 'Fornecedor cadastrado com sucesso.' })
      setFormOpen(false)
    },
    onError: (error) => {
      toast({ variant: 'error', title: 'Não foi possível salvar o fornecedor.', description: error.message })
    },
  })

  const toggleMutation = useMutation({
    mutationFn: (supplier: SupplierResponse) => setSupplierActive(supplier.id, { isActive: !supplier.isActive }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['suppliers'] })
      toast({ variant: 'success', title: 'Status atualizado.' })
      setToggling(null)
    },
    onError: (error) => {
      toast({ variant: 'error', title: 'Operação não concluída.', description: error.message })
      setToggling(null)
    },
  })

  const filtered = data?.filter((s) => s.name.toLowerCase().includes(search.toLowerCase()))

  return (
    <>
      <PageHeader
        title="Fornecedores"
        description="Gerencie os fornecedores"
        actions={
          canWrite && (
            <Button onClick={() => { setEditing(null); setFormOpen(true) }}>
              <Plus /> Novo fornecedor
            </Button>
          )
        }
      />

      <Card>
        <CardContent className="pt-6">
          <Input
            placeholder="Buscar fornecedor..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="mb-4 max-w-xs"
            aria-label="Buscar fornecedor"
          />

          {isLoading ? (
            <LoadingState message="Carregando fornecedores..." />
          ) : isError ? (
            <ErrorState title="Não foi possível carregar os fornecedores." onRetry={() => refetch()} />
          ) : !filtered || filtered.length === 0 ? (
            <EmptyState title="Nenhum fornecedor encontrado" />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome</TableHead>
                  <TableHead>Documento</TableHead>
                  <TableHead>Telefone</TableHead>
                  <TableHead>E-mail</TableHead>
                  <TableHead>Status</TableHead>
                  {canWrite && <TableHead className="w-12" />}
                </TableRow>
              </TableHeader>
              <TableBody>
                {filtered.map((supplier) => (
                  <TableRow key={supplier.id}>
                    <TableCell className="font-medium">{supplier.name}</TableCell>
                    <TableCell>{supplier.document ?? '—'}</TableCell>
                    <TableCell>{supplier.phone ?? '—'}</TableCell>
                    <TableCell>{supplier.email ?? '—'}</TableCell>
                    <TableCell>
                      <Badge variant={supplier.isActive ? 'success' : 'secondary'}>
                        {supplier.isActive ? 'Ativo' : 'Inativo'}
                      </Badge>
                    </TableCell>
                    {canWrite && (
                      <TableCell>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="icon" aria-label={`Ações de ${supplier.name}`}>
                              <MoreHorizontal />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={() => { setEditing(supplier); setFormOpen(true) }}>
                              <Pencil /> Editar
                            </DropdownMenuItem>
                            <DropdownMenuItem onClick={() => setToggling(supplier)}>
                              <Power /> {supplier.isActive ? 'Desativar' : 'Ativar'}
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Dialog open={formOpen} onOpenChange={setFormOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editing ? 'Editar fornecedor' : 'Novo fornecedor'}</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit((form) => saveMutation.mutate(form))} className="space-y-4" noValidate>
            <FormField label="Nome" htmlFor="name" error={errors.name?.message} required>
              <Input id="name" {...register('name')} />
            </FormField>
            <FormField label="CNPJ/CPF" htmlFor="document" error={errors.document?.message}>
              <Input id="document" {...register('document')} />
            </FormField>
            <FormField label="Telefone" htmlFor="phone" error={errors.phone?.message}>
              <Input id="phone" {...register('phone')} />
            </FormField>
            <FormField label="E-mail" htmlFor="email" error={errors.email?.message}>
              <Input id="email" type="email" {...register('email')} />
            </FormField>
            <DialogFooter>
              <Button variant="outline" onClick={() => setFormOpen(false)} disabled={isSubmitting}>
                Cancelar
              </Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? 'Salvando...' : 'Salvar'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={toggling !== null}
        onOpenChange={(open) => !open && setToggling(null)}
        title={toggling?.isActive ? 'Desativar fornecedor' : 'Ativar fornecedor'}
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
