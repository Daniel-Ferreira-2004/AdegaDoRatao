# 12 — Etapa 6: Autenticação e autorização

`POST /api/auth/login` recebe e-mail e senha e devolve o token JWT, sua data
de expiração, perfil e permissões. Senhas são verificadas com BCrypt e nunca
retornam na resposta.

Antes de usar login, configure uma chave aleatória de no mínimo 32 caracteres:

```bash
dotnet user-secrets set "Jwt:Secret" "coloque-aqui-uma-chave-aleatoria-com-mais-de-32-caracteres"
```

O token carrega as claims de usuário, perfil e permissões. Endpoints futuros
podem exigir a policy `permission`; a lista efetiva é carregada do perfil no
banco no momento do login. CORS permanece preparado para receber origens
configuradas em `AllowedOrigins` nas próximas configurações da API.
