import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useLocation, useNavigate } from 'react-router-dom'
import { Wine } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { ApiError } from '@/services/api/apiClient'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { FormField } from '@/components/forms/form-field'

const loginSchema = z.object({
  email: z.string().min(1, 'Informe o e-mail').email('E-mail inválido'),
  password: z.string().min(1, 'Informe a senha'),
})

type LoginFormData = z.infer<typeof loginSchema>

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({ resolver: zodResolver(loginSchema) })

  const from = (location.state as { from?: string } | null)?.from ?? '/'

  async function onSubmit(data: LoginFormData) {
    setServerError(null)
    try {
      await login(data.email, data.password)
      navigate(from, { replace: true })
    } catch (error) {
      if (error instanceof ApiError && (error.status === 400 || error.status === 401)) {
        setServerError('E-mail ou senha inválidos.')
      } else if (error instanceof ApiError) {
        setServerError(error.message)
      } else {
        setServerError('Não foi possível entrar. Tente novamente.')
      }
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      <Card className="w-full max-w-sm">
        <CardHeader className="items-center text-center">
          <Wine className="mb-2 size-10 text-primary" />
          <CardTitle className="text-xl">Adega do Ratão</CardTitle>
          <CardDescription>Entre com suas credenciais para acessar o sistema</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
            <FormField label="E-mail" htmlFor="email" error={errors.email?.message} required>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                autoFocus
                placeholder="seu@email.com"
                {...register('email')}
              />
            </FormField>
            <FormField label="Senha" htmlFor="password" error={errors.password?.message} required>
              <Input id="password" type="password" autoComplete="current-password" {...register('password')} />
            </FormField>
            {serverError && (
              <p className="rounded-md bg-destructive/10 p-3 text-sm text-destructive" role="alert">
                {serverError}
              </p>
            )}
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? 'Entrando...' : 'Entrar'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
