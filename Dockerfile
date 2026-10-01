# Imagen de producción de PSAcademyBack para Render (Web Service based en Docker).
#
# Render construye este Dockerfile con BuildKit en cada despliegue y enruta el
# tráfico al puerto $PORT. La aplicación debe escuchar en 0.0.0.0, nunca en
# localhost, o el balanceador no puede alcanzarla.

# ---------------------------------------------------------------- Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Se copia primero el .csproj para que la restauración de NuGet quede en una capa
# propia: mientras el proyecto no cambie de referencias, esa capa se reutiliza y
# los despliegues posteriores no vuelven a descargar el grafo de paquetes.
COPY PSAcademyBack.csproj ./
RUN dotnet restore PSAcademyBack.csproj

COPY . .

# "dotnet publish" no ejecuta la aplicación, así que no necesita ni cadena de
# conexión ni clave de JWT: ambas se validan al arrancar, en tiempo de ejecución.
RUN dotnet publish PSAcademyBack.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        /p:UseAppHost=false

# ---------------------------------------------------------------- Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Render espera el tráfico en $PORT (10000 por defecto) y solo publica la URL
# pública si el contenedor escucha en 0.0.0.0. render.yaml fija PORT=8080 para que
# el valor coincida con ASPNETCORE_HTTP_PORTS y ambos sean explícitos.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

COPY --from=build /app/publish .

# Las imágenes oficiales de .NET ya incluyen el usuario "app" sin privilegios
# (uid 1654). Ejecutar como root ampliaría el Radio de impacto de una escalada en
# el ejecutor de código, que es justo lo que ejecuta código de alumnos.
USER app

EXPOSE 8080

# Comprobación de vida por TCP. Render no la usa: ignora el HEALTHCHECK del
# Dockerfile y sondea la URL /health/live configurada en render.yaml. Esta existe
# para "docker run" en local, donde no hay balanceador que la reemplace.
#
# Se usa /dev/tcp en lugar de curl porque las imágenes aspnet son Debian minimo
# y no incluyen curl ni wget. Solo comprueba que el puerto acepta conexiones,
# que es exactamente lo que necesita para detectar un proceso colgado.
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD bash -c 'exec 3<>/dev/tcp/127.0.0.1/8080' || exit 1

ENTRYPOINT ["dotnet", "PSAcademyBack.dll"]
