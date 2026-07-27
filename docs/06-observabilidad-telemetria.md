# 6 · Observabilidad y Telemetría

Índice de anclas para **Observabilidad y Telemetría (Obligatorio)**: métricas con Prometheus,
trazas con Tempo, logs con Loki, recolección con Alloy, tableros en Grafana, alertas con
Alertmanager e instrumentación con OpenTelemetry.

Para saber cómo usar las anclas, ver [01-gestion-de-productos.md](01-gestion-de-productos.md).

## Índice rápido

| Ancla | Qué es | Archivo |
|---|---|---|
| `#6.1-otel-config` | Instrumentación OpenTelemetry | Program.cs |
| `#6.2-business-metric` | Contador de negocio para Prometheus | Program.cs |
| `#6.2-metrics-endpoint` | Endpoint `/metrics` y métricas HTTP | Program.cs |
| `#6.3-alloy-pipeline` | Recolector: reparte todas las señales | alloy-config.alloy |
| `#6.4-prometheus-config` | Almacén de métricas y reglas | prometheus-config.yml |
| `#6.5-alert-rules` | Las cinco alertas obligatorias | alert-rules.yml |
| `#6.6-alertmanager` | Agrupación y envío de alertas | alertmanager-config.yml |
| `#6.7-grafana-datasources` | Datasources y correlación traza↔log | grafana-datasources.yml |
| `#6.8-grafana-dashboards` | Aprovisionamiento de tableros | grafana-dashboards.yml |
| `#6.8-dash-infra` | Tablero de Infraestructura | 01-infrastructure.json |
| `#6.8-dash-app` | Tablero de Aplicación | 02-application.json |
| `#6.8-dash-business` | Tablero de Negocio | 03-business.json |
| `#6.8-dash-security` | Tablero de Seguridad | 04-security.json |
| `#6.9-loki-config` | Almacén de logs | loki-config.yml |
| `#6.10-tempo-config` | Almacén de trazas | tempo-config.yml |
| `#6.11-compose-stack` | Toda la pila y sus puertos | docker-compose.yml |

---

## Arquitectura

Regla de oro de esta pila: **la aplicación no habla con Prometheus, Tempo ni Loki**. Solo emite
OTLP hacia Alloy, y Alloy reparte. Así el recolector es el único punto que hay que cambiar si
mañana se sustituye un backend.

```
                          ┌──► Tempo       (trazas)
app ──OTLP :4317──► Alloy ├──► Loki        (logs)
app /metrics ◄─scrape──┘  └──► Prometheus  (métricas, remote-write) ──► Grafana
                                    │
                                    └──► Alertmanager
```

---

## 6.1 · Instrumentación

### `#6.1-otel-config`
**Archivo:** [Program.cs](../InventoryManagement/src/InventorySystem.Server/Program.cs)

Configura OpenTelemetry para las tres señales, todas exportadas por OTLP hacia Alloy:

- **Trazas**: peticiones ASP.NET Core (registrando excepciones), llamadas salientes con
  `HttpClient` y consultas de Entity Framework Core **incluyendo el texto SQL**, que es lo que
  cumple el requisito de *database tracing*.
- **Métricas**: ASP.NET Core, HttpClient, runtime de .NET (CPU, GC, hilos) y el medidor de
  **Npgsql**, que aporta las cifras del *pool* de conexiones que el proyecto exige.
- **Logs**: con mensaje formateado y scopes, de modo que `traceId` y `spanId` viajan en cada
  registro y permiten saltar del log a la traza.

Todas las señales se identifican con el servicio `InventoryServer` v1.0.0, que es el nombre por
el que se filtra en Grafana.

### `#6.2-business-metric` y `#6.2-metrics-endpoint`
**Archivo:** [Program.cs](../InventoryManagement/src/InventorySystem.Server/Program.cs)

El primero es un contador propio, `inventory_audit_events_total`, que se incrementa con cada
cambio auditado y lleva etiquetas de acción y entidad: es la métrica **de negocio** que alimenta
el tablero correspondiente. El segundo expone `/metrics` en formato Prometheus y registra las
métricas HTTP por petición (tasa, histograma de duración y código de estado) bajo
`http_request_duration_seconds_*`, que son las que sostienen los paneles de throughput, latencia
y error rate y las reglas de alerta.

> Detalle importante: **Prometheus no raspa este endpoint directamente**. Lo hace Alloy y lo
> reenvía por remote-write, para que toda la telemetría pase por el recolector.

---

## 6.2 · Recolección y almacenamiento

### `#6.3-alloy-pipeline`
**Archivo:** [alloy-config.alloy](../InventoryManagement/src/monitoring/alloy-config.alloy)

El recolector por el que pasa todo. Recibe OTLP en 4317 (gRPC) y 4318 (HTTP) y reparte: las
trazas a Tempo por OTLP, los logs a Loki convirtiéndolos a su formato nativo, y las métricas a
Prometheus por remote-write. Además **raspa él mismo** el `/metrics` de la aplicación y lo
reenvía por el mismo camino, que es la razón por la que Prometheus no tiene ningún objetivo de
scrape propio.

### `#6.4-prometheus-config`
**Archivo:** [prometheus-config.yml](../InventoryManagement/src/monitoring/prometheus-config.yml)

El almacén de métricas. Escucha en **:3300** (no en el 9090 por defecto) y su lista de objetivos
está vacía a propósito: todo llega por remote-write desde Alloy. Su otra función es evaluar las
reglas de alerta en cada intervalo y enviar a Alertmanager lo que se dispare.

### `#6.9-loki-config` y `#6.10-tempo-config`
**Archivos:** [loki-config.yml](../InventoryManagement/src/monitoring/loki-config.yml) ·
[tempo-config.yml](../InventoryManagement/src/monitoring/tempo-config.yml)

Loki guarda los logs en modo binario único sobre el sistema de archivos, con anillo en memoria;
cada registro llega etiquetado con `service_name`, `level` y el `trace_id` que permite volver a
la traza. Tempo guarda las trazas en bloques locales con retención de 1 hora. Ambas
configuraciones son adecuadas para una pila de demostración, no para producción: sin
almacenamiento de objetos ni replicación, los datos se pierden al recrear los volúmenes.

---

## 6.3 · Visualización y alertas

### `#6.7-grafana-datasources`
**Archivo:** [grafana-datasources.yml](../InventoryManagement/src/monitoring/grafana/provisioning/datasources/grafana-datasources.yml)

Aprovisiona los tres datasources al arrancar, sin configurarlos a mano. Lo más valioso aquí es
la **correlación**: Tempo está enlazado a Loki (`tracesToLogsV2`, emparejando por id de traza) y
Loki enlazado de vuelta a Tempo (`derivedFields` sobre `trace_id`), de forma que desde una traza
lenta se puede saltar a sus logs y desde una línea de log a la traza completa. Las URLs usan
nombres de contenedor, no `localhost`, porque se resuelven dentro de la red de Docker.

### `#6.8-grafana-dashboards`
**Archivo:** [grafana-dashboards.yml](../InventoryManagement/src/monitoring/grafana/provisioning/dashboards/grafana-dashboards.yml)

Indica a Grafana que cargue todos los tableros JSON montados en el contenedor. Al estar
aprovisionados desde archivos, los tableros quedan versionados en Git y se recrean solos en una
pila nueva, en vez de tener que rehacerlos a mano.

Los cuatro tableros que exige el proyecto (infraestructura, aplicación, negocio y seguridad):

| Ancla | Tablero | Contenido |
|---|---|---|
| `#6.8-dash-infra` | 1 · Infraestructura | CPU, memoria y heap, GC, thread pool, conexiones Kestrel, excepciones, contención de bloqueos |
| `#6.8-dash-app` | 2 · Aplicación | Throughput, latencia p50/p95/p99, error rate, códigos de estado, endpoints más usados, **pool de base de datos** y panel de logs en vivo |
| `#6.8-dash-business` | 3 · Negocio | Cambios auditados, productos creados, actualizaciones de stock, actividad por acción y entidad |
| `#6.8-dash-security` | 4 · Seguridad | Tasas de 401 y 403, intentos de autorización por resultado, latencia de autenticación, denegaciones por endpoint, logs de advertencia y error |

### `#6.5-alert-rules`
**Archivo:** [alert-rules.yml](../InventoryManagement/src/monitoring/alert-rules.yml)

Las cinco alertas obligatorias. Cada una debe mantenerse cierta durante su ventana `for:` antes
de dispararse, lo que evita que un pico momentáneo genere ruido:

| Alerta | Severidad | Condición | Espera |
|---|---|---|---|
| `ApiServiceDown` | critical | El objetivo no responde | 1 min |
| `HighErrorRate` | warning | Más del 5 % de respuestas son 5xx | 5 min |
| `HighRequestLatency` | warning | p95 por encima de 1 s | 5 min |
| `HighCpuUsage` | warning | CPU por encima del 85 % | 5 min |
| `AuthFailureSpike` | warning | Más de 1 respuesta 401/403 por segundo | 5 min |

Un detalle que costó descubrir: las métricas que llegan por OTLP se exportan **cada 60
segundos**, así que una ventana `rate(...[1m])` no tiene suficientes muestras y evalúa a vacío.
Por eso las expresiones sobre CPU usan ventanas de `[5m]`.

### `#6.6-alertmanager`
**Archivo:** [alertmanager-config.yml](../InventoryManagement/src/monitoring/alertmanager-config.yml)

Recibe las alertas que dispara Prometheus, las agrupa por nombre y las muestra en su interfaz.
Incluye una regla de inhibición para que una alerta crítica silencie las de severidad menor con
el mismo nombre. El receptor está intencionalmente vacío: la pila no envía nada al exterior. Para
recibir notificaciones hay que añadir un receptor de correo, Slack o webhook.

---

## 6.4 · Cómo verlo funcionando

### `#6.11-compose-stack`
**Archivo:** [docker-compose.yml](../InventoryManagement/src/docker-compose.yml)

Levanta toda la pila. Puertos en el host:

| Servicio | URL |
|---|---|
| Cliente Blazor | `http://localhost:9090` |
| API | `http://localhost:8090` (docs en `/scalar`) |
| Keycloak | `http://localhost:8080` |
| **Grafana** | `http://localhost:3000` (admin/admin) |
| **Prometheus** | `http://localhost:3300` |
| **Alertmanager** | `http://localhost:9093` |
| Loki / Tempo / Alloy | `3100` / `3200` / `3800` |

Los servicios se comunican entre sí por nombre de contenedor, que es la razón por la que las
configuraciones dicen `prometheus:3300` y no `localhost`.

```bash
docker compose up --build
```

Un punto que suele confundir: **los tableros solo muestran datos si hay tráfico que observar**.
Después de levantar la pila hay que entrar al cliente en `localhost:9090`, iniciar sesión y
navegar —listar productos, mover stock, abrir la auditoría— para que se generen métricas, trazas
y logs. Sin actividad, los paneles aparecen vacíos aunque todo esté correctamente configurado.
El panel del *pool* de base de datos, en particular, solo reporta mientras hay conexiones
abiertas.
