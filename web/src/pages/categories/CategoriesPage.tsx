import { brandsService, categoriesService } from '@/services/catalog/catalogService'
import { PageHeader } from '@/components/common/page-header'
import { NamedEntityManager } from '@/components/common/NamedEntityManager'
import { Card, CardContent } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'

export default function CategoriesPage() {
  return (
    <>
      <PageHeader title="Categorias e Marcas" description="Organize o catálogo de produtos" />
      <Card>
        <CardContent className="pt-6">
          <Tabs defaultValue="categories">
            <TabsList>
              <TabsTrigger value="categories">Categorias</TabsTrigger>
              <TabsTrigger value="brands">Marcas</TabsTrigger>
            </TabsList>
            <TabsContent value="categories">
              <NamedEntityManager
                queryKey="categories"
                entityLabel="Categoria"
                writePermission="products.write"
                service={categoriesService}
              />
            </TabsContent>
            <TabsContent value="brands">
              <NamedEntityManager
                queryKey="brands"
                entityLabel="Marca"
                writePermission="products.write"
                service={brandsService}
              />
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>
    </>
  )
}
