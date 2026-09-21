using AdegaDoRatao.Infrastructure;
using AdegaDoRatao.Application;
using AdegaDoRatao.API.Middlewares;
using AdegaDoRatao.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

// Program.cs
//
// Ponto de entrada da aplicação. Nesta Etapa 1, o objetivo é apenas ter
// uma API mínima que compila e sobe, provando que a estrutura da solução
// está correta. A composição real (EF Core, JWT, Swagger, Serilog,
// middlewares) será adicionada nas próximas etapas, conforme o plano:
//   Etapa 4/5 -> Infrastructure + banco de dados
//   Etapa 6   -> Autenticação
//   Etapa 14  -> Tratamento global de erros e logging
//   Etapa 17  -> Swagger/OpenAPI

var builder = WebApplication.CreateBuilder(args);

// Serilog (RNF05): logging estruturado com enriquecimento de contexto de
// requisição (RequestId/TraceId) para correlação. Níveis e sinks podem ser
// ajustados via configuração (Serilog__MinimumLevel).
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/adega-.log", rollingInterval: RollingInterval.Day));

// Registra os adaptadores técnicos (JWT, hash de senha e usuário atual).
// A chave JWT permanece vazia até ser configurada em User Secrets/ambiente;
// ela só é exigida quando houver geração ou validação de token na Etapa 6.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddControllers();

// Health checks: /health executa um teste real contra o banco (SELECT 1 via
// EF Core). Se o banco estiver fora, o endpoint responde 503 — é isso que
// Docker/orquestradores usam para saber se o container está saudável.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AdegaDoRatao.Infrastructure.Persistence.AppDbContext>("database");

// Swagger/OpenAPI (RNF07): documentação interativa da API, com suporte a
// autenticação JWT direto pela UI (botão "Authorize").
builder.Services.AddEndpointsApiExplorer();

// Rate limiting (hardening): política "login" — janela fixa de 1 minuto,
// no máximo 5 tentativas por IP, para mitigar força bruta no login.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        // Resposta 429 em ProblemDetails, consistente com ErrorHandlingMiddleware.
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Muitas requisições",
            Detail = "Limite de tentativas excedido. Aguarde um minuto e tente novamente.",
            Instance = context.HttpContext.Request.Path,
            Extensions = { ["traceId"] = context.HttpContext.TraceIdentifier }
        };
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
    };
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Adega do Ratão API",
        Version = "v1",
        Description = "API de gestão da adega: produtos, estoque, compras, vendas, financeiro, dashboard e auditoria."
    });

    // Permite informar o token JWT uma vez e testar os endpoints protegidos.
    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Informe: Bearer {seu token JWT}",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var authentication = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
if (!string.IsNullOrWhiteSpace(jwt.Secret)) authentication.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateLifetime = true, ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
        NameClaimType = System.Security.Claims.ClaimTypes.Name,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("permission", p => p.RequireClaim("permission"));
    options.AddPolicy("products.read", p => p.RequireAssertion(c => c.User.IsInRole("ADMIN") || c.User.HasClaim("permission", "products.read") || c.User.HasClaim("permission", "products.write")));
    options.AddPolicy("products.write", p => p.RequireAssertion(c => c.User.IsInRole("ADMIN") || c.User.HasClaim("permission", "products.write")));
    options.AddPolicy("stock.read", p => p.RequireAssertion(c => c.User.IsInRole("ADMIN") || c.User.HasClaim("permission", "stock.read") || c.User.HasClaim("permission", "stock.write")));
    options.AddPolicy("stock.write", p => p.RequireAssertion(c => c.User.IsInRole("ADMIN") || c.User.HasClaim("permission", "stock.write")));
    options.AddPolicy("purchases.read", p => p.RequireAssertion(c => c.User.IsInRole("ADMIN") || c.User.HasClaim("permission", "purchases.read") || c.User.HasClaim("permission", "purchases.write")));
    options.AddPolicy("purchases.write", p => p.RequireAssertion(c => c.User.IsInRole("ADMIN") || c.User.HasClaim("permission", "purchases.write")));
    options.AddPolicy("sales.read", p => p.RequireAssertion(c => c.User.IsInRole("ADMIN") || c.User.HasClaim("permission", "sales.read") || c.User.HasClaim("permission", "sales.write")));
    options.AddPolicy("sales.write", p => p.RequireAssertion(c => c.User.IsInRole("ADMIN") || c.User.HasClaim("permission", "sales.write")));
    options.AddPolicy("financial.read", p => p.RequireAssertion(c => c.User.IsInRole("ADMIN") || c.User.HasClaim("permission", "financial.read") || c.User.HasClaim("permission", "financial.write")));
    options.AddPolicy("financial.write", p => p.RequireAssertion(c => c.User.IsInRole("ADMIN") || c.User.HasClaim("permission", "financial.write")));
    options.AddPolicy("dashboard.read", p => p.RequireAssertion(c => c.User.IsInRole("ADMIN") || c.User.HasClaim("permission", "dashboard.read")));
    options.AddPolicy("audit.read", p => p.RequireAssertion(c => c.User.IsInRole("ADMIN")));
});

// ATENÇÃO (produção): a configuração "AllowedOrigins" (ou as variáveis de
// ambiente AllowedOrigins__0, AllowedOrigins__1, ...) PRECISA estar definida
// com a URL real do frontend em produção. Se estiver vazia fora de
// Development, NENHUMA origem será liberada (fail-closed — comportamento
// correto e intencional; não "corrigir" abrindo CORS em produção).
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").GetChildren()
    .Select(origin => origin.Value).Where(origin => !string.IsNullOrWhiteSpace(origin)).Cast<string>().ToArray();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0) policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
        else if (builder.Environment.IsDevelopment()) policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// O middleware de erros deve ser o PRIMEIRO do pipeline: assim ele captura
// exceções de todos os middlewares seguintes (CORS, auth, controllers).
app.UseErrorHandling();

// Cabeçalhos de segurança HTTP em todas as respostas (hardening).
app.UseSecurityHeaders();

// Swagger exposto apenas em Development (hardening: não publicar a
// documentação interativa da API em produção).
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Adega do Ratão API v1");
        options.DocumentTitle = "Adega do Ratão — API";
    });
}

// Log de requisições HTTP (método, path, status, tempo) com TraceId
// automático — sem dados sensíveis (RNF05).
app.UseSerilogRequestLogging();

// CORS deve vir ANTES do HTTPS redirection: o preflight OPTIONS do navegador
// não segue redirects (307), então precisa ser respondido diretamente pelo
// middleware de CORS.
app.UseCors();

// Redireciona HTTP -> HTTPS apenas fora de Development. Em dev o frontend
// chama a API em HTTP direto, e o redirect 307 faz o navegador descartar o
// header Authorization (mudança de origem), causando 401 após o login.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Endpoint de verificação de saúde: 200 se a API e o banco estiverem OK,
// 503 caso contrário. Usado por Docker/orquestradores e monitoramento.
app.MapHealthChecks("/health");

app.Run();

// Classe parcial exposta propositalmente para permitir que os testes de
// integração (WebApplicationFactory<Program>) referenciem esta aplicação
// a partir do projeto AdegaDoRatao.IntegrationTests.
public partial class Program { }
