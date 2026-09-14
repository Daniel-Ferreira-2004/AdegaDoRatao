using AdegaDoRatao.Infrastructure;
using AdegaDoRatao.Application;
using AdegaDoRatao.API.Middlewares;
using AdegaDoRatao.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

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

// Swagger/OpenAPI (RNF07): documentação interativa da API, com suporte a
// autenticação JWT direto pela UI (botão "Authorize").
builder.Services.AddEndpointsApiExplorer();
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

// Habilita CORS de forma aberta apenas nesta etapa inicial de esqueleto.
// Na Etapa 6/14 isso será restringido a uma lista de origens configurável
// (variável de ambiente "AllowedOrigins"), pensando também em clientes
// mobile/web que vão consumir esta API futuramente.
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

// Swagger disponível em /swagger (documentação interativa da API).
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Adega do Ratão API v1");
    options.DocumentTitle = "Adega do Ratão — API";
});

// Log de requisições HTTP (método, path, status, tempo) com TraceId
// automático — sem dados sensíveis (RNF05).
app.UseSerilogRequestLogging();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Endpoint de verificação de saúde (health check) simples, útil tanto
// para monitoramento em produção quanto para o frontend confirmar que
// a API está no ar antes de fazer chamadas.
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "AdegaDoRatao.API" }));

app.Run();

// Classe parcial exposta propositalmente para permitir que os testes de
// integração (WebApplicationFactory<Program>) referenciem esta aplicação
// a partir do projeto AdegaDoRatao.IntegrationTests.
public partial class Program { }
