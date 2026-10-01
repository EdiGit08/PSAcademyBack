using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PSAcademyBack;
using PSAcademyBack.Data;
using PSAcademyBack.Entities;
using PSAcademyBack.Enums;
using PSAcademyBack.Extensions;
using PSAcademyBack.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- Services

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Sin esto los enums viajan como numeros ("difficulty": 0) en lugar de
        // "Easy", obligando a cada cliente a mantener su propia tabla de mapeo.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    // La cadena con la contraseña nunca se versiona: vive en user-secrets o en la
    // variable de entorno ConnectionStrings__DefaultConnection.
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings:DefaultConnection'. Configúrala con: " +
        "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=...;Password=...\"");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// El hashing de contraseñas usa BCrypt de forma estática (BCrypt.Net.BCrypt),
// por lo que no requiere registro en DI.

// ------------------------------------------------------------------ JWT

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.SecretKey),
        "Falta 'Jwt:SecretKey'. Configúrala con: dotnet user-secrets set \"Jwt:SecretKey\" \"<clave-de-32-caracteres-o-mas>\"")
    .Validate(o => o.SecretKey!.Length >= 32,
        "'Jwt:SecretKey' debe tener al menos 32 caracteres (256 bits) para HMAC-SHA256.")
    .ValidateOnStart();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Falta la sección de configuración 'Jwt'.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole(nameof(UserRole.Admin)));
});
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// ----------------------------------------------------------------- Piston

builder.Services.Configure<PistonOptions>(builder.Configuration.GetSection(PistonOptions.SectionName));
builder.Services.AddHttpClient<IPistonExecutionService, PistonExecutionService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PistonOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);

    // La cancelación del servicio (TimeoutSeconds + 10s) debe dispararse antes que
    // esta, para devolver un 502 con mensaje en lugar de una TaskCanceledException.
    // Ambos quedan por encima del run_timeout de Piston, de modo que sea Piston quien
    // corte primero y el resultado viaje como un intento normal del alumno.
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds + 15);
});

// ------------------------------------------------------------ Rate Limiting

// Sin este límite, cualquier usuario autenticado puede usar la API para martillear
// el ejecutor de código y agotar su cuota.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimitingPolicies.CodeExecution, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ResolvePartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

static string ResolvePartitionKey(HttpContext httpContext)
{
    var userId = httpContext.User.GetUserId();

    // PartitionKey exige un valor no vacío: si el token no es utilizable,
    // se agrupa por IP para no compartir cuota entre desconocidos.
    return userId.HasValue
        ? $"user:{userId.Value}"
        : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}

var pistonBaseUrl = builder.Configuration[$"{PistonOptions.SectionName}:BaseUrl"];

// ------------------------------------------------------------------- CORS

// AllowAnyOrigin y AllowCredentials son mutuamente excluyentes: combinarlos
// hace que la política sea rechazada en tiempo de ejecución. Con JWT en cabecera
// Authorization no se necesitan credenciales.
const string CorsPolicy = "CorsAllowAnyOrigin";

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("Content-Disposition"));
});

// -------------------------------------------------------------- OpenAPI

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "PSAcademy API",
            Version = "v1",
            Description = "API de la plataforma de aprendizaje de programación."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Escribe solo el token JWT, sin el prefijo 'Bearer '."
        };

        return Task.CompletedTask;
    });
});

// ---------------------------------------------------------------- Pipeline

var app = builder.Build();

// La instancia pública de Piston es compartida, sin SLA y sujeta a rate limiting.
// En producción conviene apuntar a un Piston autoalojado.
if (!app.Environment.IsDevelopment()
    && pistonBaseUrl?.Contains("emkc.org", StringComparison.OrdinalIgnoreCase) == true)
{
    app.Logger.LogWarning(
        "Piston está configurado contra la instancia pública ({BaseUrl}). Se recomienda un servicio autoalojado en producción.",
        pistonBaseUrl);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // Aplica las migraciones pendientes y siembra datos iniciales.
    // Bloqueado a Development a proposito: la semilla crea un admin con
    // contrasena conocida y no debe ejecutarse nunca en produccion.
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await dbContext.Database.MigrateAsync();
        await DbSeeder.SeedAsync(dbContext, logger);
    }

    app.MapOpenApi();
    app.MapScalarApiReference(options =>
        options.WithTitle("PSAcademy API")
            .WithTheme(ScalarTheme.BluePlanet));
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

// Debe ir despues de UseAuthentication: la clave de particion depende del usuario del token.
app.UseRateLimiter();

app.MapControllers();

app.Run();
