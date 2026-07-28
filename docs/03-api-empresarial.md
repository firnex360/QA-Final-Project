# 3 · API Empresarial

Índice de anclas para el **Alcance Funcional §3 — API Empresarial**: una API REST documentada
con OpenAPI y una interfaz interactiva tipo Swagger UI.

Para saber cómo usar las anclas, ver [01-gestion-de-productos.md](01-gestion-de-productos.md).

## Índice rápido

| Ancla | Qué es | Archivo |
|---|---|---|
| `#3.1-openapi-config` | Generación del documento OpenAPI | Program.cs |
| `#3.2-openapi-ui` | Interfaz interactiva (Scalar) | Program.cs |
| `#3.3-bearer-scheme` | Esquema de seguridad JWT en la documentación | BearerSecuritySchemeTransformer.cs |

---

## 3.1 · Documentación OpenAPI

### `#3.1-openapi-config`
**Archivo:** [Program.cs](../InventoryManagement/src/InventorySystem.Server/Program.cs)

Registra la generación del documento OpenAPI. ASP.NET Core recorre los controladores y produce
la especificación automáticamente a partir de las rutas, los verbos, los tipos de los parámetros
y los modelos, por lo que la documentación no se escribe a mano y no puede quedar desfasada
respecto al código. Al registrarse se le añade el transformador del esquema de seguridad
(`#3.3-bearer-scheme`), sin el cual la documentación existiría pero no permitiría autenticarse.

### `#3.2-openapi-ui`
**Archivo:** [Program.cs](../InventoryManagement/src/InventorySystem.Server/Program.cs)

Expone dos cosas, **solo en el entorno de desarrollo**: `/openapi/v1.json`, que es el documento
OpenAPI en crudo (el que consumirían herramientas de pruebas de contrato), y `/scalar`, que es
la interfaz navegable donde cada endpoint se puede ejecutar desde el navegador. Scalar cumple el
mismo papel que Swagger UI. Que esté limitado a desarrollo es intencional: en producción no
conviene publicar la superficie completa de la API.

Con la pila levantada, la interfaz está en `http://localhost:8090/scalar`. Para probar
endpoints protegidos hay que pegar un token de Keycloak en el botón *Authorize*.

### `#3.3-bearer-scheme`
**Archivo:** [BearerSecuritySchemeTransformer.cs](../InventoryManagement/src/InventorySystem.Server/OpenApi/BearerSecuritySchemeTransformer.cs)

Declara el esquema de seguridad `Bearer` (JWT) dentro del documento OpenAPI y lo aplica a todas
las operaciones. Sin esta pieza, la interfaz no mostraría el botón *Authorize* y nunca enviaría
la cabecera `Authorization`, por lo que cada endpoint protegido respondería 401 y la
documentación sería inútil para probar. La descripción del esquema aclara que debe pegarse
únicamente el JWT en crudo, porque la interfaz ya añade el prefijo `Bearer `.

---

## Superficie de la API

El requisito pide que la API permita CRUD de productos, consulta de inventario, movimientos de
stock y reportes. Cada endpoint está documentado en su sección correspondiente:

| Endpoint | Propósito | Ancla |
|---|---|---|
| `POST /api/product` | Crear producto | `#1.1-create-api` |
| `GET /api/product` | Consultar inventario (paginado/filtrado) | `#1.4-list-api` |
| `GET /api/product/{id}` | Consultar un producto | `#1.4-get-by-id-api` |
| `PUT /api/product/{id}` | Editar producto | `#1.2-edit-api` |
| `DELETE /api/product/{id}` | Eliminar producto | `#1.3-delete-api` |
| `PATCH /api/product/{id}/stock` | Movimiento de stock | `#2.1-adjust-api` |
| `GET /api/product/stats` | Reporte del inventario | `#4.1-dashboard-stats-api` |
| `GET /api/audit/stock-movements` | Historial de movimientos | `#2.3-movements-api` |
| `GET /api/audit/stock-movements/stats` | Reporte de movimientos / más vendidos | `#2.3-movements-stats-api` |
| `GET /api/audit` | Consulta de auditoría | `#2.4-audit-api` |
| `GET /api/audit/stats` | Reporte de auditoría | `#2.4-audit-stats-api` |
| `GET /api/permissions/me` | Permisos del usuario actual | `#5.6-permissions-api` |
| `GET /api/permissions/check` | ¿Puedo realizar esta llamada? (pre-vuelo) | `#5.12-permission-check-api` |

Ninguno de estos endpoints comprueba roles por su cuenta: todos pasan por el mismo middleware de
autorización (`#5.2-policy-middleware`), que consulta a Keycloak.
