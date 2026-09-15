import { paymentMethodsService } from '@/services/catalog/catalogService'
import { PageHeader } from '@/components/common/page-header'
import { NamedEntityManager } from '@/components/common/NamedEntityManager'
import { Card, CardContent } from '@/components/ui/card'

export default function PaymentMethodsPage() {
  return (
    <>
      <PageHeader title="Formas de pagamento" description="Gerencie as formas de pagamento aceitas" />
      <Card>
        <CardContent className="pt-6">
          <NamedEntityManager
            queryKey="payment-methods"
            entityLabel="Forma de pagamento"
            writePermission="sales.write"
            service={paymentMethodsService}
          />
        </CardContent>
      </Card>
    </>
  )
}
