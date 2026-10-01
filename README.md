# PSAcademy API — Backend .NET 9

API REST para la plataforma de aprendizaje de programación PSAcademy.

## Stack

| Capa | Tecnología | Versión |
|------|------------|---------|
| Runtime | .NET | 9.0 (LTS) |
| Framework | ASP.NET Core | 9.0 |
| Base de datos | PostgreSQL | 16+ (Neon serverless) |
| ORM | EF Core | 9.0 |
| Auth | JWT (HMAC-SHA256) | — |
| Ejecutor código | Piston | autoalojado / emkc.org con clave |
| Rate limiting | ASP.NET Core RateLimiter | 9.0 |
| Docs API | Scalar (OpenAPI) | 2.17 |

## Arquitectura de despliegue objetivo

```
┌─────────────────┐     HTTPS      ┌─────────────────┐
│   Frontend      │ ─────────────▶ │   Backend       │
│   (Vercel)      │  /api/*        │   (Render)      │
└─────────────────┘                └────────┬────────┘
                                            │
                           ┌────────────────┼────────────────┐
                           ▼                ▼                ▼
                      ┌─────────┐      ┌─────────┐      ┌─────────┐
                      │  Neon   │      │ Piston  │      │ Render  │
                      │PostgreSQL│     │ (código) │      │ Logs    │
                      └─────────┘      └─────────┘      └─────────┘
```

- **Backend**: Render Web Service (Docker) — una sola instancia, puerto 8080
- **Base de datos**: Neon PostgreSQL serverless — cadena *pooled*
- **Piston**: **no corre en Render** (requiere `privileged: true`). Se despliega aparte (VPS, docker-compose) y se referencia vía `Piston__BaseUrl`

## Requisitos previos

- .NET 9 SDK (para desarrollo local y migraciones)
- Docker (para build local de la imagen)
- PostgreSQL local o Neon branch para desarrollo
- Una instancia de Piston accesible (ver sección Piston)

## Configuración local

```bash
# 1. Clonar y entrar
cd Back/PSAcademyBack

# 2. User secrets (nunca en appsettings.json)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=psacademy;Username=postgres;Password=..."
dotnet user-secrets set "Jwt:SecretKey" "$(openssl rand -base64 48)"
dotnet user-secrets set "Piston:BaseUrl" "http://localhost:2000/api/v2/"

# 3. Levantar Piston local (requiere Docker + privileged)
cd ../../Back/PSAcademyBack
docker compose up -d

# 4. Ejecutar
dotnet run
# API en http://localhost:5077 (ver launchSettings.json)
# Scalar UI: http://localhost:5077/scalar/v1 (solo Development)
```

## Variables de entorno (producción)

Todas se configuran en **Render > Environment**. Las marcadas `sync: false` en `render.yaml` **no** se guardan en el repo; hay que pegarlas a mano en el dashboard.

| Variable | Descripción | Ejemplo / Nota |
|----------|-------------|----------------|
| `ASPNETCORE_ENVIRONMENT` | Entorno ASP.NET | `Production` |
| `PORT` | Puerto que Render expone (debe coincidir con `ASPNETCORE_HTTP_PORTS` del Dockerfile) | `8080` |
| `ConnectionStrings__DefaultConnection` | **Neon pooled connection string** — *obligatoria*, `sync: false` | `postgresql://user:pass@ep-xxx.neon.tech/db?sslmode=require` |
| `Jwt__SecretKey` | Clave HMAC-SHA256 ≥ 32 chars — *obligatoria*, `sync: false` | `openssl rand -base64 48` |
| `Jwt__Issuer` | Emisor del token | `PSAcademyBack` |
| `Jwt__Audience` | Audiencia del token | `PSAcademyBackClient` |
| `Jwt__ExpirationMinutes` | Vida del access token | `60` |
| `Database__ApplyMigrationsOnStartup` | Aplicar migraciones al arrancar | `true` |
| `Database__SeedReferenceDataOnStartup` | Sembrar idiomas + categorías | `true` |
| `Database__CommandTimeoutSeconds` | Timeout por comando SQL | `60` |
| `Database__MaxRetryCount` | Reintentos Npgsql (tráfico normal) | `6` |
| `Database__RetryDelaySeconds` | Delay máx entre reintentos | `10` |
| `Database__MigrationMaxAttempts` | Intentos totales al migrar al arranque | `8` |
| `Cors__AllowedOrigins__0` | Dominio Vercel del frontend — `sync: false` | `https://psacademy.vercel.app` |
| `Https__Enabled` | HSTS + redirección HTTPS | `true` |
| `Https__HttpsPort` | Puerto público HTTPS (Render = 443) | `443` |
| `Piston__BaseUrl` | URL base del ejecutor — `sync: false` | `https://piston.midominio.com/api/v2/` |
| `Piston__ApiKey` | Clave si usa emkc.org público — `sync: false` | (vacío si Piston propio) |
| `Piston__TimeoutSeconds` | Timeout llamada a Piston | `15` |
| `Piston__MaxOutputLength` | Tope stdout/stderr devuelto | `8000` |

### Neon: pooled vs direct connection

**Usar siempre la *pooled connection* de Neon** (botón "Pooled connection" en el dashboard). El pooler externo (PgBouncer) mantiene conexiones calientes y sobrevive a la suspensión de la *computation* serverless. La conexión directa se cierra al suspender y la primera query falla.

La cadena pooled ya incluye `sslmode=require`. Añadir `Pooling=true` para que Npgsql use su pool interno sobre el pooler externo.

## Piston: opciones sin coste

Render **no admite contenedores `privileged`**, por lo que Piston **no puede convivir** con la API en el mismo Web Service. Tres caminos a coste cero:

1. **Piston autoalojado en VPS** (recomendado)  
   - Un VPS Linux barato (Hetzner CX22 ~4 €/mes, Oracle Always Free, etc.)  
   - Usar el `docker-compose.yml` de este repo (puerto 2000, `PISTON_LIMIT_OVERRIDES` ya ajustado)  
   - Exponer por HTTPS (Caddy/Traefik/nginx + Let's Encrypt)  
   - `Piston__BaseUrl = https://piston.tu-dominio.com/api/v2/`

2. **Instancia pública emkc.org con clave autorizada**  
   - Desde 15/02/2026 exige clave (solo proyectos educativos no comerciales)  
   - Solicitar en Discord a EngineerMan  
   - `Piston__BaseUrl = https://emkc.org/api/v2/piston/` + `Piston__ApiKey = <tu-clave>`

3. **Dejar `Piston__BaseUrl` pendiente y conectar luego**  
   - La API arranca y todo funciona **menos `/execute`** (devuelve 502 con mensaje claro)  
   - Útil para validar el resto del despliegue antes de montar Piston

> ⚠️ Sin Piston accesible, el endpoint `POST /api/exercises/{id}/execute` **siempre responde 502**. El resto de la API (catálogo, borradores, auth, admin) funciona normal.

## Migraciones y esquema

```bash
# Crear migración (solo desarrollo)
dotnet ef migrations add NombreMigracion

# Aplicar a base de datos destino (Neon pooled)
dotnet ef database update \
  --connection "ConnectionStrings__DefaultConnection=..."
```

En producción, `Database__ApplyMigrationsOnStartup=true` ejecuta `MigrateAsync()` al arrancar con reintentos tolerantes a Neon (suspensión, failover). Es idempotente: si ya están aplicadas, no hace nada.

### Sembrado de datos

| Entorno | Qué se siembra |
|---------|----------------|
| Development | Admin (`admin@psacademy.com` / `Admin123!`) + idiomas + categorías |
| Production  | Solo idiomas + categorías (idempotente, sin usuarios) |

El primer usuario que se registre en producción recibe rol **Admin** automáticamente (ver `AuthController.Register`). Así la plataforma queda operable sin intervención manual.

## Health checks

| Endpoint | Qué comprueba | Uso |
|----------|---------------|-----|
| `GET /health/live` | Proceso vivo (sin BD) | **Render healthCheckPath** — decide si la instancia recibe tráfico |
| `GET /health/ready` | Proceso + esquema BD accesible | Diagnóstico / despliegues con bloqueo |

Render usa `/health/live`; un Neon suspendido momentáneamente **no** saca el servicio de rotación.

## CORS

En producción **debe** declararse el dominio exacto de Vercel en `Cors__AllowedOrigins__0` (y `__1`, `__2`… si hay dominios personalizados). Con la lista vacía la API responde `AllowAnyOrigin` (funciona pero expone la API a cualquier origen).

## Rate limiting

Política `CodeExecution`: 20 peticiones/minuto por usuario (o IP si no autenticado). Partition key: `user:{id}` o `ip:{remoteIp}`. Configurable en `Program.cs`.

## Estructura del proyecto

```
Back/PSAcademyBack/
├── Controllers/          # Auth, Categories, Languages, Exercises, Admin
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── DbSeeder.cs       # SeedDevelopmentAsync / SeedReferenceDataAsync
│   └── Migrations/       # EF Core migrations
├── Dtos/                 # Request/Response shapes
├── Entities/             # EF Core entities
├── Enums/                # UserRole, Difficulty, ProgressStatus, etc.
├── Extensions/           # Claim helpers (GetUserId)
├── Services/
│   ├── JwtTokenService.cs
│   ├── PistonExecutionService.cs
│   ├── JwtOptions.cs
│   ├── PistonOptions.cs
│   ├── HttpsOptions.cs
│   └── DatabaseStartupOptions.cs
├── Program.cs            # Composition root + pipeline
├── Dockerfile            # Multi-stage build para Render
├── .dockerignore
├── render.yaml           # Blueprint Render
├── appsettings.json
├── appsettings.Production.json
└── PSAcademyBack.csproj
```

## Endpoints principales

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/api/auth/login` | ❌ | Login → JWT |
| POST | `/api/auth/register` | ❌ | Registro (1er usuario = Admin) |
| GET | `/api/categories` | ❌ | Catálogo público |
| GET | `/api/categories/{id}/exercises` | ❌ | Ejercicios de una categoría |
| GET | `/api/exercises/{id}` | ❌ | Detalle ejercicio (plantillas, inputs) |
| POST | `/api/exercises/{id}/execute` | ✅ | Ejecutar código (Piston) |
| PUT | `/api/exercises/{id}/draft` | ✅ | Guardar borrador autoguardado |
| GET | `/api/languages` | ❌ | Idiomas activos |
| GET/POST/PUT/DELETE | `/api/admin/*` | ✅ Admin | Panel administración |

## Desarrollo: añadir un idioma

1. Añadir entrada en `PistonExecutionService.SupportedLanguages` (clave = slug, valor = runtime Piston + filename)
2. Si es PSeint, extender `PSeintTranslator`
3. Ejecutar migración para que exista en BD: `dotnet ef migrations add Add<Lang>Language`
4. El seeder (`SeedLanguagesAsync`) lo activará/desactivará según el array `supported`

## Seguridad

- Contraseñas: BCrypt work factor 11
- JWT: HMAC-SHA256, 60 min, `ClockSkew=30s`
- `RequireHttpsMetadata=false` (clave simétrica → validación local)
- CORS restringido a dominios declarados
- Rate limiting en `/execute`
- Headers de seguridad vía `HttpsOptions` + `UseForwardedHeaders` detrás de Render

## Logs y observabilidad

- Structured logging (JSON en contenedor)
- Nivel `Information` por defecto, `Warning` para `Microsoft.AspNetCore*`
- Render muestra logs en tiempo real en el dashboard
- Para APM externo: exportar a Seq/Grafana Loki/etc. añadiendo proveedor en `Program.cs`

## Problemas conocidos / FAQ

| Síntoma | Causa | Solución |
|---------|-------|----------|
| `/execute` 502 "No se pudo contactar" | `Piston__BaseUrl` apunta a localhost o Piston caído | Verificar URL + logs de Piston |
| `/execute` 502 "El ejecutor remoto rechazó (HTTP 401)" | Instancia pública sin `Piston__ApiKey` | Añadir clave o usar Piston propio |
| Health check 503 en Render | Neon suspendido / connection string inválida | Revisar `ConnectionStrings__DefaultConnection` |
| CORS error en navegador | Dominio Vercel no está en `Cors__AllowedOrigins` | Añadirlo en Render y redespelgar |
| Migración falla "relation does not exist" | Base vacía + `ApplyMigrationsOnStartup=false` | Poner a `true` o correr `dotnet ef database update` |

## Licencia

Uso educativo interno. Sin licencia de distribución.