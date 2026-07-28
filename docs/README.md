# Documentación técnica — Sistema de Gestión de Inventarios

Documentación por requisito del proyecto. Cada documento es un **índice de anclas**: una lista de
identificadores que existen a la vez aquí y como comentario dentro del código.

## Cómo funciona

Cada apartado tiene un id como `#1.1-create-api`. Ese mismo texto está escrito en el archivo
fuente correspondiente.

> Copie el id (con el `#`) y péguelo en la búsqueda global del editor (`Ctrl+Shift+F`).
> Obtendrá dos resultados: la explicación aquí y el código exacto.

En el código las anclas aparecen como `// #1.1-create-api` (C#), `@* #1.1-create-ui *@` (Razor),
`# #6.5-alert-rules` (YAML) o en el campo `description` (JSON de los tableros de Grafana). Los
comentarios del código están en **inglés** y resumen los valores concretos (límites, parámetros,
códigos de estado); estos documentos están en **español** y explican qué hace y por qué.

## Documentos

| Doc | Requisito del proyecto | Anclas |
|---|---|---|
| [01 · Gestión de Productos](01-gestion-de-productos.md) | Alcance Funcional §1 — alta, edición, borrado y visualización con paginación, búsqueda, filtros y ordenamiento | `#1.x` |
| [02 · Control de Stock](02-control-de-stock.md) | Alcance Funcional §2 — entradas/salidas, alertas de stock mínimo, historial de movimientos y auditoría | `#2.x` |
| [03 · API Empresarial](03-api-empresarial.md) | Alcance Funcional §3 — API REST documentada con OpenAPI y Swagger UI | `#3.x` |
| [04 · Interfaz y Dashboard](04-interfaz-usuario.md) | Alcance Funcional §4 — tablero de control, indicadores y usabilidad | `#4.x` |
| [05 · Roles y Seguridad](05-roles-y-seguridad.md) | Modelo granular obligatorio y Seguridad — Keycloak, OAuth2, JWT, scopes y policies | `#5.x` |
| [06 · Observabilidad y Telemetría](06-observabilidad-telemetria.md) | Observabilidad — OpenTelemetry, Prometheus, Tempo, Loki, Alloy, Grafana y Alertmanager | `#6.x` |
| [07 · Guía de Pruebas](07-guia-de-pruebas.md) | Full Stack Testing — dónde está cada tipo de prueba, cómo ejecutarla y qué cubre | — |

> El documento 07 es una guía, no un índice de anclas: las pruebas ya están separadas por proyecto
> y por archivo, así que la unidad útil ahí es el proyecto y no la línea de código.

## Arranque rápido

```bash
docker compose up --build
```

Desde `InventoryManagement/src`. Luego entre al cliente en `http://localhost:9090`, inicie sesión
y navegue un poco para generar telemetría; después revise los tableros en
`http://localhost:3000`.
