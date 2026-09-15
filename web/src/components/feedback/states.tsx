import { AlertCircle, Inbox, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'

export function LoadingState({ message = 'Carregando...' }: { message?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-12 text-muted-foreground" role="status">
      <Loader2 className="size-8 animate-spin" />
      <p className="text-sm">{message}</p>
    </div>
  )
}

export function EmptyState({ title, description }: { title: string; description?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 py-12 text-center">
      <Inbox className="size-10 text-muted-foreground/50" />
      <p className="font-medium">{title}</p>
      {description && <p className="text-sm text-muted-foreground">{description}</p>}
    </div>
  )
}

export function ErrorState({
  title = 'Não foi possível carregar os dados.',
  onRetry,
}: {
  title?: string
  onRetry?: () => void
}) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-12 text-center">
      <AlertCircle className="size-10 text-destructive/70" />
      <p className="font-medium">{title}</p>
      {onRetry && (
        <Button variant="outline" size="sm" onClick={onRetry}>
          Tentar novamente
        </Button>
      )}
    </div>
  )
}
