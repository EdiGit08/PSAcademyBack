using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PSAcademyBack;
using PSAcademyBack.Data;
using PSAcademyBack.Entities;
using PSAcademyBack.Enums;
using PSAcademyBack.Extensions;
using PSAcademyBack.Services;
using Npgsql;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- Services

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Sin esto los enums viajan como numeros ("difficulty": 0) en lugar de
        // "easy", obligando a cada cliente a mantener su propia tabla de mapeo.
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

var databaseStartup = builder.Configuration.GetSection(DatabaseStartupOptions.SectionName).Get<DatabaseStartupOptions>()
                       ?? new DatabaseStartupOptions();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
    {
        // Neon es PostgreSQL serverless: la computation se suspende tras un periodo de
        // inactividad. El primer comando tras la reanudacion puede tardar, asi que el
        // timeout por comando se holga frente al valor por defecto de 30s.
        npgsql.CommandTimeout(databaseStartup.CommandTimeoutSeconds);

        // Las peticiones normales toleran fallos transitorios (Neon reanudando,
        // failover) sin que el alumno vea un error. Las migraciones del arranque
        // usan su propio reintento, porque alli interesa distinguir transitorio de
        // error de esquema. La lista de SqlState nula delega en los valores por
        // defecto de Npgsql, que ya cubren los casos de servidorless.
        npgsql.EnableRetryOnFailure(
            maxRetryCount: databaseStartup.MaxRetryCount,
            maxRetryDelay: TimeSpan.FromSeconds(databaseStartup.RetryDelaySeconds),
            errorCodesToAdd: null);
    }));

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
        // Con clave simétrica la validación es local y no se descarga metadata de
        // ningún proveedor, así que exigir HTTPS para obtenerla solo provocaría
        // fallos en despliegues sin TLS directo. La confidencialidad la aporta el
        // transporte, y en producción Render/Vercel terminan TLS siempre.
        options.RequireHttpsMetadata = false;
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
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IExerciseGradingService, ExerciseGradingService>();

// ----------------------------------------------------------------- Piston

builder.Services.Configure<PistonOptions>(builder.Configuration.GetSection(PistonOptions.SectionName));
builder.Services.AddHttpClient<IPistonExecutionService, PistonExecutionService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PistonOptions>>().Value;
    client.BaseAddress = new Uri(options.NormalizedBaseUrl, UriKind.Absolute);

    // La cancelación del servicio (TimeoutSeconds + 10s) debe dispararse antes que
    // esta, para devolver un 502 con mensaje en lugar de una TaskCanceledException.
    // Ambos quedan por encima del run_timeout de Piston, de modo que sea Piston quien
    // corte primero y el resultado viaje como un intento normal del alumno.
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds + 15);

    // La instancia pública de Piston exige autorización desde el 15/02/2026; sin
    // enviar la clave responde 401 y el alumno recibe un 502 sin más explicación.
    if (!string.IsNullOrWhiteSpace(options.ApiKey))
    {
        client.DefaultRequestHeaders.Add("Authorization", options.ApiKey);
    }
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

// El frontend se despliega en Vercel, un dominio distinto al de la API, asi que
// el navegador exige CORS. En produccion se declara la lista exacta de origenes
// ("Cors:AllowedOrigins"); si se deja vacia se cae en el comportamiento
// permisivo de desarrollo.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? Array.Empty<string>();

// ASP.NET compara los origenes con sensitivity a mayusculas y la barra final
// cuenta como otro origen, asi que se normaliza para que lo que se declara en el
// panel de Render coincida exactamente con lo que envia el navegador.
allowedOrigins = allowedOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/').ToLowerInvariant())
    .Distinct()
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        // `X-Unread-Count` va expuesta porque la SPA lee el contador de notificaciones no
        // leidas de esa cabecera: sin WithExposedHeaders el navegador la oculta y el
        // contador llega siempre a 0, con la campana en cero aunque haya avisos.
        var exposedHeaders = new[] { "Content-Disposition", "X-Unread-Count" };

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders(exposedHeaders);
        }
        else
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders(exposedHeaders);
        }
    });
});

// -------------------------------------------------------- Health Checks

// El puerto de redirección se fija de forma explícita porque, sin él,
// UseHttpsRedirection no puede elegir destino: registra "Failed to determine the
// https port for redirect" y deja servir la petición sin cifrar en lugar de
// redirigirla. Con 443 (el puerto público de Render y de cualquier proxy que
// termine TLS) una petición HTTP suelta se convierte en un 308 a HTTPS.
var httpsOptions = builder.Configuration.GetSection(HttpsOptions.SectionName).Get<HttpsOptions>()
                   ?? new HttpsOptions();

builder.Services.Configure<HttpsRedirectionOptions>(options =>
{
    options.HttpsPort = httpsOptions.HttpsPort;

    // 308 y no 307: en una API las peticiones son POST y deben conservar el
    // método. 308 además lo marca el navegador como permanente, así que un
    // usuario que escriba la URL con http:// no repite el intento tras el
    // primer 308.
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
});

// Render usa /health/live para decidir a que instancia enrutar trafico, asi que
// esa comprobacion no debe depender de la base de datos: un fallo transitorio de
// Neon sacaria el servicio de rotacion en lugar de degradarlo. /health/ready si
// comprueba el esquema y sirve para diagnostico y para despliegues con bloqueo.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" });

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

// La URL que realmente se usa, ya normalizada: si /execute empieza a fallar con
// 404 o 502, este log dice de inmediato contra qué host estaban saliendo las
// peticiones, sin tener que abrir el panel de Render a buscar la variable.
app.Logger.LogInformation(
    "Ejecutor de código (Piston): Piston__BaseUrl={Configured} -> {Effective}",
    string.IsNullOrWhiteSpace(pistonBaseUrl) ? "(vacío)" : pistonBaseUrl,
    new PSAcademyBack.Services.PistonOptions { BaseUrl = pistonBaseUrl ?? string.Empty }.NormalizedBaseUrl);

// Un origen mal escrito en Cors__AllowedOrigins no da error: la API sigue respondiendo
// 200, pero sin la cabecera Access-Control-Allow-Origin, y el navegador bloquea la
// llamada sin mensaje util ("CORS policy: No 'Access-Control-Allow-Origin' header").
// El error aparece en el frontend y a kilometros de su causa, asi que se imprime
// la lista exacta que el proceso esta usando.
app.Logger.LogInformation(
    "CORS: origenes permitidos = {Origins}",
    allowedOrigins.Length == 0
        ? "ninguno (política permisiva: responde Access-Control-Allow-Origin: * a cualquier origen)"
        : string.Join(", ", allowedOrigins));

// La instancia pública de Piston es compartida, sin SLA y sujeta a rate limiting.
// En producción conviene apuntar a un Piston autoalojado.
if (!app.Environment.IsDevelopment()
    && pistonBaseUrl?.Contains("emkc.org", StringComparison.OrdinalIgnoreCase) == true)
{
    app.Logger.LogWarning(
        "Piston está configurado contra la instancia pública ({BaseUrl}). Se recomienda un servicio autoalojado en producción.",
        pistonBaseUrl);
}

// Un Piston sin definir deja el catálogo, los ejercicios y los borradores funcionando,
// pero /execute devuelve 502 a todos los alumnos. No se aborta el arranque porque
// tiraría el servicio entero y dejaría el front sin nada; se deja constancia en el log
// para que se detecte en el primer despliegue.
if (!app.Environment.IsDevelopment() && IsLoopbackPiston(pistonBaseUrl))
{
    app.Logger.LogError(
        "Piston__BaseUrl sigue apuntando a {BaseUrl}, que en Render es el propio contenedor: " +
        "no existe ningún ejecutor dentro de este servicio y /execute responderá 502 a todos los alumnos. " +
        "Define Piston__BaseUrl con una instancia accesible (Piston autoalojado o la instancia pública con " +
        "Piston__ApiKey autorizada) y vuelve a desplegar. El resto de la API funciona mientras tanto.",
        pistonBaseUrl);
}

static bool IsLoopbackPiston(string? baseUrl)
{
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        return true;
    }

    return Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
           && (uri.IsLoopback
               || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
               || uri.Host.Equals("127.0.0.1", StringComparison.Ordinal)
               || uri.Host.Equals("0.0.0.0", StringComparison.Ordinal));
}

// Debe ir antes de todo lo demás: sin esto Request.IsHttps es false porque Render
// termina TLS y reenvía HTTP, y UseHttpsRedirection devolvería un 307 hacia una
// URL que el navegador vuelve a pedir por HTTP (bucle de redirección).
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = null
};
// Vaciar de verdad las listas: `= { }` no borra los valores por defecto (solo loopback).
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);
app.UseExceptionHandler();
app.UseStatusCodePages();

var appLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

await InitializeDatabaseAsync(app);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
        options.WithTitle("PSAcademy API")
            .WithTheme(ScalarTheme.BluePlanet));
}
else
{
    if (httpsOptions.Enabled)
    {
        // Con UseForwardedHeaders ya activo, IsHttps refleja el protocolo original
        // (https) y estas dos lineas no generan ninguna redireccion: solo anaden
        // HSTS y cubren el caso de que la peticion llegase por HTTP de verdad.
        app.UseHsts();
        app.UseHttpsRedirection();
    }
}

app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

// Debe ir despues de UseAuthentication: la clave de particion depende del usuario del token.
app.UseRateLimiter();

app.MapControllers();

// Sondas para Render y para monitorizacion externa. /health/live responde 200 en
// cuanto el proceso levanta; /health/ready ademas ejecuta un SELECT contra la BD.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
}).AllowAnonymous();

app.Run();

// ------------------------------------------------------------- Arranque BD
//
// En desarrollo el esquema y los datos se aplican siempre. Fuera de desarrollo
// solo si "Database:ApplyMigrationsOnStartup" / "Database:SeedReferenceDataOnStartup"
// lo permiten, de modo que un despliegue puede optar por aplicar las migraciones
// desde un comando previo de Render en lugar de al arrancar.

async Task InitializeDatabaseAsync(WebApplication app)
{
    var isDevelopment = app.Environment.IsDevelopment();
    var applyMigrations = isDevelopment || databaseStartup.ApplyMigrationsOnStartup;
    var seedReferenceData = isDevelopment || databaseStartup.SeedReferenceDataOnStartup;

    if (!applyMigrations && !seedReferenceData)
    {
        appLogger.LogInformation(
            "Arranque sin aplicar migraciones ni sembrar datos (Database:ApplyMigrationsOnStartup=false).");
        return;
    }

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");

    if (applyMigrations)
    {
        await MigrateWithRetryAsync(dbContext, logger);
    }

    if (seedReferenceData)
    {
        // Los datos de referencia (lenguajes y categorias) no contienen datos
        // sensibles y la siembra es idempotente, asi que es segura fuera de
        // desarrollo: es lo que permite que /execute no responda 400 por falta de
        // filas en "languages" y que el catalogo del alumno no arranque vacio.
        await DbSeeder.SeedReferenceDataAsync(dbContext, logger);
    }

    if (isDevelopment)
    {
        // La cuenta admin con contrasena conocida solo existe fuera de produccion.
        await DbSeeder.SeedDevelopmentAsync(dbContext, logger);
    }
}

/// <summary>
/// Aplica las migraciones reintentando ante fallos transitorios.
///
/// Neon suspende la computation tras un periodo de inactividad y el primer comando
/// posterior puede fallar o agotar el tiempo. Render tambien puede arrancar la
/// instancia mientras la base aun esta despertando. Sin reintento, un despliegue
/// perfectamente valido fallaria de forma intermitente y dejaria el esquema a medias.
/// </summary>
async Task MigrateWithRetryAsync(ApplicationDbContext dbContext, ILogger logger)
{
    var maxAttempts = Math.Max(1, databaseStartup.MigrationMaxAttempts);
    var delay = TimeSpan.FromSeconds(databaseStartup.RetryDelaySeconds);

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            // MigrateAsync es idempotente: si ya se aplicaron, no hace nada.
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Esquema de base de datos verificado (intento {Attempt}).", attempt);
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts && IsTransient(ex))
        {
            logger.LogWarning(
                ex,
                "No se pudo aplicar las migraciones en el intento {Attempt} de {MaxAttempts}: {Message}. Reintentando en {Delay}s.",
                attempt,
                maxAttempts,
                ex.Message,
                delay.TotalSeconds);

            await Task.Delay(delay);
        }
    }
}

/// <summary>
/// Decide si un fallo merece la pena reintentar. Se aceptan errores de red, timeouts
/// y los codigos de Postgres que son transitorios por definicion; un error de esquema
/// (SQL invalido, duplicados) no lo es y debe salir hacia Render para que el despliegue
/// falle de forma visible en lugar de quedarse reintentando indefinidamente.
/// </summary>
static bool IsTransient(Exception exception)
{
    switch (exception)
    {
        // PostgresException hereda de NpgsqlException, asi que debe evaluarse antes:
        // es el unico caso que distingue un SQLState transitorio de un fallo de red
        // opaco, y colocarlo despues lo haria inalcanzable.
        case PostgresException postgres:
            return postgres.SqlState is PostgresErrorCodes.ConnectionException
                or PostgresErrorCodes.ConnectionDoesNotExist
                or PostgresErrorCodes.ConnectionFailure
                or PostgresErrorCodes.TooManyConnections
                or PostgresErrorCodes.CannotConnectNow
                or PostgresErrorCodes.AdminShutdown
                or PostgresErrorCodes.CrashShutdown
                or PostgresErrorCodes.SerializationFailure
                or PostgresErrorCodes.DeadlockDetected;

        case NpgsqlException:
        case System.Net.Sockets.SocketException:
        case System.Net.Http.HttpRequestException:
        case TimeoutException:
            return true;

        case AggregateException aggregate:
            return aggregate.InnerExceptions.Any(IsTransient);

        default:
            return exception.InnerException is not null && IsTransient(exception.InnerException);
    }
}
