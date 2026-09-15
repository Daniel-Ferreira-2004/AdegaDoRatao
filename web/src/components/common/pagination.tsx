import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import type { PagedResult } from '@/types/api'

interface PaginationProps {
  result: PagedResult<unknown>
  onPageChange: (page: number) => void
}

export function Pagination({ result, onPageChange }: PaginationProps) {
  const { page, totalPages, totalCount, pageSize } = result
  if (totalCount === 0) return null

  const from = (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, totalCount)

  return (
    <div className="flex items-center justify-between gap-4 pt-4 text-sm text-muted-foreground">
      <span>
        {from}–{to} de {totalCount}
      </span>
      <div className="flex items-center gap-2">
        <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => onPageChange(page - 1)} aria-label="Página anterior">
          <ChevronLeft />
        </Button>
        <span>
          Página {page} de {totalPages}
        </span>
        <Button
          variant="outline"
          size="sm"
          disabled={!result.hasNextPage}
          onClick={() => onPageChange(page + 1)}
          aria-label="Próxima página"
        >
          <ChevronRight />
        </Button>
      </div>
    </div>
  )
}
