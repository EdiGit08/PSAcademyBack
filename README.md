# PSAcademy API — Backend .NET 9

API REST para la plataforma de aprendizaje de programación PSAcademy (PSeInt, Python, Java).

## Stack

| Capa | Tecnología | Versión |
|------|------------|---------|
| Runtime | .NET | 9.0 (LTS) |
| Framework | ASP.NET Core | 9.0 |
| Base de datos | PostgreSQL | 16+ (Neon serverless en producción) |
| ORM | EF Core | 9.0 |
| Auth | JWT (HMAC-SHA256) + refresh token con rotación | — |
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

- **Backend**: Render Web Service (Docker), una sola instancia, puerto 8080.
- **Base de datos**: Neon PostgreSQL serverless.
- **Piston**: **no corre en Render** (requiere `privileged: true`). Se despliega aparte (VPS, docker-compose) y se referencia con `Piston__BaseUrl`.

## Flujo de ejercicios y calificación

- **Tutorial** (categoría "Tutorial"): se autocorrige. Acertar la salida marca el paso como correcto; solo el último paso completa la lección.
- **Ejercicios normales**: `POST /api/exercises/{id}/execute` ejecuta el código y, si la salida es correcta, **crea la submission en la misma llamada** (`awaitingReview: true`). No hay un segundo botón de enviar.
- Mientras un ejercicio tiene un envío pendiente, **ese ejercicio** no admite otro envío; el resto de la plataforma funciona con normalidad.
- Un administrador califica con `PUT /api/admin/submissions/{id}/grade`: aprobado (el ejercicio queda `completed`) o devuelto con comentario (`incorrect` + feedback, y el alumno puede corregir y reenviar). Al calificar se crea una notificación para el alumno.

## Requisitos previos

- .NET 9 SDK (desarrollo local y migraciones)
- Docker Desktop (para Piston local y para construir la imagen)
- PostgreSQL local o un branch de Neon para desarrollo

## Configuración local (Windows / PowerShell)

```powershell
cd Back\PSAcademyBack

# 1. User secrets (nunca en appsettings.json ni en el repositorio)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=psacademy;Username=postgres;Password=<TU_CLAVE>"
dotnet user-secrets set "Jwt:SecretKey" ([Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 })))
dotnet user-secrets set "Piston:BaseUrl" "http://localhost:2000/api/v2/"

# 2. Piston local (requiere Docker; expone el puerto 2000)
docker compose up -d

# 3. Migraciones (en Development la API también las aplica al arrancar)
dotnet ef database update

# 4. Ejecutar
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Https__Enabled = "false"
dotnet run --urls http://localhost:5077
# API en http://localhost:5077
# Scalar UI: http://localhost:5077/scalar/v1 (solo Development)
```

Comprobar que la API está viva: `curl.exe -i http://localhost:5077/health/live` (debe dar 200).

> Detén siempre la API con **Ctrl+C** antes de recompilar. Un proceso `PSAcademyBack` huérfano bloquea el `.exe` (error `MSB3026`) y sigue sirviendo una compilación vieja. Con `dotnet watch run` no ocurre.

## Variables de entorno (producción)

Todas se configuran en **Render > Environment**. Las marcadas `sync: false` en `render.yaml` no se guardan en el repo; hay que pegarlas a mano en el dashboard.

| Variable | Descripción | Ejemplo / Nota |
|----------|-------------|----------------|
| `ASPNETCORE_ENVIRONMENT` | Entorno ASP.NET | `Production` |
| `PORT` | Puerto que Render expone (debe coincidir con `ASPNETCORE_HTTP_PORTS` del Dockerfile) | `8080` |
| `ConnectionStrings__DefaultConnection` | Cadena de Neon — *obligatoria*, `sync: false` | `postgresql://user:pass@ep-xxx.neon.tech/db?sslmode=require` |
| `Jwt__SecretKey` | Clave HMAC-SHA256 ≥ 32 caracteres, **distinta a la de desarrollo** — *obligatoria*, `sync: false` | generar una aleatoria |
| `Jwt__Issuer` | Emisor del token | `PSAcademyBack` |
| `Jwt__Audience` | Audiencia del token | `PSAcademyBackClient` |
| `Jwt__ExpirationMinutes` | Vida del access token | `60` |
| `Jwt__RefreshTokenExpirationDays` | Vida del refresh token | `14` |
| `Database__ApplyMigrationsOnStartup` | Aplicar migraciones al arrancar | `true` |
| `Database__SeedReferenceDataOnStartup` | Sembrar lenguajes y categorías | `true` |
| `Database__CommandTimeoutSeconds` | Timeout por comando SQL | `60` |
| `Database__MaxRetryCount` | Reintentos Npgsql (tráfico normal) | `6` |
| `Database__RetryDelaySeconds` | Delay máx. entre reintentos | `10` |
| `Database__MigrationMaxAttempts` | Intentos totales al migrar al arranque | `8` |
| `Cors__AllowedOrigins__0` | Dominio Vercel del frontend, sin barra final — `sync: false` | `https://psacademy.vercel.app` |
| `Https__Enabled` | HSTS + redirección HTTPS | `true` |
| `Https__HttpsPort` | Puerto público HTTPS (Render = 443) | `443` |
| `Piston__BaseUrl` | URL base del ejecutor — `sync: false` | `https://piston.midominio.com/api/v2/` |
| `Piston__ApiKey` | Clave si usa emkc.org público — `sync: false` | vacío si Piston propio |
| `Piston__TimeoutSeconds` | Timeout de la llamada a Piston | `15` |
| `Piston__MaxOutputLength` | Tope de stdout/stderr devuelto | `8000` |

### Neon: pooled vs direct

Para la aplicación se usa la **pooled connection** de Neon (botón "Pooled connection" en el dashboard). La cadena ya incluye `sslmode=require`. La API reintenta ante fallos transitorios (Neon reanudando tras la suspensión), tanto en las peticiones como al migrar.

Si las migraciones fallan al pasar por el pooler, aplícalas una vez con la **conexión directa** (sin `-pooler`) y deja la pooled para la app.

## Piston: opciones sin coste

Render **no admite contenedores `privileged`**, por lo que Piston no puede convivir con la API en el mismo Web Service.

1. **Piston autoalojado en VPS** (recomendado)
   - Un VPS Linux barato (Hetzner CX22 ~4 €/mes, Oracle Always Free, etc.).
   - Usar el `docker-compose.yml` de este repo (puerto 2000).
   - Exponerlo por HTTPS (Caddy/Traefik/nginx + Let's Encrypt).
   - `Piston__BaseUrl = https://piston.tu-dominio.com/api/v2/`

2. **Instancia pública emkc.org con clave autorizada**
   - Desde el 15/02/2026 exige clave (solo proyectos educativos no comerciales).
   - `Piston__BaseUrl = https://emkc.org/api/v2/piston/` + `Piston__ApiKey = <tu-clave>`

3. **Dejar `Piston__BaseUrl` pendiente y conectar después**
   - La API arranca y todo funciona **menos `/execute`** (devuelve 502 con mensaje claro).

> Sin Piston accesible, `POST /api/exercises/{id}/execute` **siempre responde 502**. El resto de la API (catálogo, borradores, auth, admin) funciona normal.

Lenguajes instalados en el Piston local de desarrollo: C, C++, D, Fortran, Java, C#, Basic, JavaScript y Python (3.10 y 3.12). PSeInt no existe en Piston: se traduce antes de ejecutar (`Tools/pseint-tool` / `PSeintTranslator`).

## Migraciones y esquema

```powershell
# Crear migración (solo desarrollo)
dotnet ef migrations add NombreMigracion

# Aplicar a la base configurada en user-secrets
dotnet ef database update
```

En producción, `Database__ApplyMigrationsOnStartup=true` ejecuta `MigrateAsync()` al arrancar con reintentos tolerantes a Neon. Es idempotente.

Tablas principales: `users`, `categories`, `languages`, `exercises`, `exercise_inputs`, `exercise_templates`, `tutorial_steps`, `user_progress`, `user_code_drafts`, `submissions`, `notifications`, `refresh_tokens`. El refresh token se guarda como hash SHA-256 (`token_hash`) y se rota de forma atómica.

### Sembrado de datos

| Entorno | Qué se siembra |
|---------|----------------|
| Development | Cuenta admin de desarrollo (credenciales en `DbSeeder.SeedDevelopmentAsync`) + lenguajes + categorías |
| Production | Solo lenguajes y categorías (idempotente, sin usuarios) |

> **Seguridad:** el primer usuario que se registre en producción recibe rol **Admin** automáticamente (`AuthController.Register`). Regístrate tú primero justo después del primer despliegue, antes de compartir la URL.

## Health checks

| Endpoint | Qué comprueba | Uso |
|----------|---------------|-----|
| `GET /health/live` | Proceso vivo (sin BD) | **Render healthCheckPath**: decide si la instancia recibe tráfico |
| `GET /health/ready` | Proceso + esquema BD accesible | Diagnóstico / despliegues con bloqueo |

## CORS

En producción **debe** declararse el dominio exacto de Vercel en `Cors__AllowedOrigins__0` (y `__1`, `__2`… si hay dominios propios). Sin barra final y en minúscula (la API normaliza ambos). Con la lista vacía la API responde `AllowAnyOrigin`: funciona, pero expone la API a cualquier origen. Al arrancar, el log imprime la lista que realmente se está usando.

La cabecera `X-Unread-Count` está expuesta por CORS: el front la lee para el contador de notificaciones.

## Rate limiting

Política `CodeExecution`: 20 peticiones/minuto por usuario (o por IP si no hay token), aplicada a `/execute` y `/submit`. Configurable en `Program.cs`.

## Estructura del proyecto

```
Back/PSAcademyBack/
├── Controllers/          # Auth, Categories, Languages, Exercises, Admin, AdminSubmissions, Notifications
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── DbSeeder.cs       # SeedDevelopmentAsync / SeedReferenceDataAsync
│   └── Migrations/       # Migraciones EF Core
├── Dtos/                 # Request/Response shapes (deben coincidir con el front)
├── Entities/             # Entidades EF Core
├── Enums/                # UserRole, Difficulty, ProgressStatus, SubmissionStatus, NotificationType
├── Extensions/           # Helpers de claims (GetUserId)
├── Services/
│   ├── JwtTokenService.cs
│   ├── RefreshTokenService.cs
│   ├── ExerciseGradingService.cs
│   ├── NotificationService.cs
│   ├── PistonExecutionService.cs
│   ├── JwtOptions.cs / PistonOptions.cs / HttpsOptions.cs / DatabaseStartupOptions.cs
├── Tools/                # content/ (generador de catálogo) y pseint-tool/ (traductor PSeInt)
├── Program.cs            # Composición raíz + pipeline
├── Dockerfile            # Build multi-stage para Render
├── render.yaml           # Blueprint de Render
├── appsettings.json
├── appsettings.Production.json
└── PSAcademyBack.csproj
```

## Endpoints principales

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/api/auth/login` | No | Login → JWT + refresh token |
| POST | `/api/auth/register` | No | Registro (1er usuario = Admin) |
| POST | `/api/auth/refresh` | Refresh token | Renueva el access token (rota el refresh token) |
| GET | `/api/categories` | No | Catálogo público |
| GET | `/api/categories/{id}/exercises` | No | Ejercicios de una categoría |
| GET | `/api/exercises/{id}` | Opcional | Detalle del ejercicio. Con JWT incluye estado, feedback y borradores del alumno |
| POST | `/api/exercises/{id}/execute` | Sí | Ejecuta el código (Piston); si es correcto en un ejercicio normal, lo envía a calificación |
| POST | `/api/exercises/{id}/submit` | Sí | Reenvío explícito a calificación (re-ejecuta y valida en el servidor) |
| PUT | `/api/exercises/{id}/draft` | Sí | Guarda el borrador del código |
| GET | `/api/languages` | No | Lenguajes activos |
| GET | `/api/notifications` | Sí | Notificaciones del alumno (cabecera `X-Unread-Count`) |
| GET | `/api/admin/submissions` | Admin | Bandeja de envíos |
| PUT | `/api/admin/submissions/{id}/grade` | Admin | Califica: `{ "correct": true }` o `{ "correct": false, "feedback": "..." }` |
| GET/POST/PUT/DELETE | `/api/admin/*` | Admin | Panel de administración |

## Desarrollo: añadir un lenguaje

1. Añadir la entrada en `PistonExecutionService.SupportedLanguages` (clave = slug, valor = runtime de Piston + nombre de archivo).
2. Si es PSeInt, extender `PSeintTranslator`.
3. Crear la migración para que exista en BD: `dotnet ef migrations add Add<Lang>Language`.
4. El seeder (`SeedLanguagesAsync`) lo activa o desactiva según la lista soportada.

## Seguridad

- Contraseñas: BCrypt.
- JWT: HMAC-SHA256, 60 min, `ClockSkew=30s`. Refresh token: hash SHA-256 en BD, rotación atómica, 14 días.
- `RequireHttpsMetadata=false` (clave simétrica → validación local; el TLS lo termina Render).
- CORS restringido a los dominios declarados.
- Rate limiting en `/execute` y `/submit`.
- `UseForwardedHeaders` va primero en el pipeline y **vacía** `KnownProxies` y `KnownNetworks` con `.Clear()`: sin eso, detrás de Render se ignora `X-Forwarded-Proto` y `UseHttpsRedirection` entra en un bucle de 308. (`KnownProxies = { }` no vacía la lista, solo la deja con los valores por defecto.)
- Ninguna credencial va en el repositorio: usar `dotnet user-secrets` en local y variables de entorno en Render.

## Logs y observabilidad

- Logging estructurado.
- Nivel `Information` por defecto, `Warning` para `Microsoft.AspNetCore*`.
- Al arrancar se registra a qué `Piston__BaseUrl` se está llamando y la lista de orígenes CORS.
- Render muestra los logs en tiempo real en el dashboard.

## Problemas conocidos / FAQ

| Síntoma | Causa | Solución |
|---------|-------|----------|
| `/execute` 502 "No se pudo contactar" | `Piston__BaseUrl` apunta a localhost o Piston caído | Verificar la URL y los logs de Piston |
| `/execute` 502 "El ejecutor remoto rechazó (HTTP 401)" | Instancia pública sin `Piston__ApiKey` | Añadir la clave o usar Piston propio |
| `/execute` 400 "Lenguaje no ejecutable" | El slug no está en `SupportedLanguages` | Añadirlo (ver "añadir un lenguaje") |
| 500 "Unable to resolve service for type ..." | Un constructor pide la clase concreta en vez de la interfaz, o hay una compilación vieja corriendo | Pedir `IExerciseGradingService` (no la clase) y reiniciar la API |
| `MSB3026` al compilar | Otra copia de la API sigue corriendo y bloquea el `.exe` | `Get-Process PSAcademyBack \| Stop-Process -Force` y recompilar |
| 308 en bucle / "CORS error" en Render | Forwarded headers sin `Clear()` en las listas de proxies | Ver sección Seguridad |
| 308 en local | Se ejecuta fuera de Development con `Https__Enabled=true` | `ASPNETCORE_ENVIRONMENT=Development` o `Https__Enabled=false` |
| Health check 503 en Render | Neon suspendido / cadena de conexión inválida | Revisar `ConnectionStrings__DefaultConnection` |
| CORS error en el navegador | Dominio de Vercel no está en `Cors__AllowedOrigins` | Añadirlo en Render y redesplegar |
| Migración falla "relation does not exist" | Base vacía + `ApplyMigrationsOnStartup=false` | Ponerlo en `true` o ejecutar `dotnet ef database update` |

## Licencia

Uso educativo interno. Sin licencia de distribución.