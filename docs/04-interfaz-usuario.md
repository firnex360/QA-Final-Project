# 4 · Interfaz de Usuario y Dashboard

Índice de anclas para el **Alcance Funcional §4 — Interfaz de Usuario Amigable**: el tablero de
control con la visión general del inventario, los indicadores operacionales y la navegación.

Para saber cómo usar las anclas, ver [01-gestion-de-productos.md](01-gestion-de-productos.md).

## Índice rápido

| Ancla | Qué es | Archivo |
|---|---|---|
| `#4.1-dashboard-stats-api` | `GET /api/product/stats` | ProductController.cs |
| `#4.1-dashboard-stats` | Cálculo de todas las cifras del tablero | ProductService.cs |
| `#4.2-dashboard-ui` | Tablero de control | Home.razor |
| `#4.2-stock-health` | Serie "salud del stock" derivada | Home.razor |
| `#4.3-nav-menu` | Navegación según permisos | NavMenu.razor |

---

## 4.1 · Datos del tablero

### `#4.1-dashboard-stats-api`
**Archivo:** [ProductController.cs](../InventoryManagement/src/InventorySystem.Server/Controllers/ProductController.cs) · `GET /api/product/stats`

Entrega en **una sola llamada** todas las cifras que necesita el tablero, en vez de obligar al
cliente a pedir producto por producto. Además del dashboard, lo reutiliza la pantalla de
productos para llenar el desplegable de categorías y el contador de stock bajo del chip
(`#1.4.2-filters-ui`). Está protegido por su propio recurso de Keycloak, `ProductStats`, lo que
permite conceder acceso a los reportes a un rol que no necesariamente puede ver o gestionar el
listado de productos.

### `#4.1-dashboard-stats`
**Archivo:** [ProductService.cs](../InventoryManagement/src/InventorySystem.Server/Services/ProductService.cs)

Calcula todos los indicadores en una sola pasada sobre el catálogo:

- **Totales**: productos totales, activos e inactivos.
- **Alertas**: `LowStockCount` (cantidad menor o igual al mínimo) y `OutOfStockCount` (cantidad
  cero). Ojo con un detalle que importa al graficar: *el conteo de stock bajo incluye a los
  agotados*, porque cero siempre es menor o igual al mínimo.
- **Dinero**: valor total del inventario y valor por categoría, ambos como suma de precio ×
  cantidad.
- **Desgloses**: cantidad de productos por categoría.
- **Productos críticos**: los 8 más urgentes, ordenados por qué tan por debajo de su mínimo
  están (`Quantity - MinimumStockLevel`), de modo que primero aparece el que tiene el faltante
  más grave y no simplemente el que tiene menos unidades.

### `#4.2-dashboard-ui`
**Archivo:** [Home.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Home.razor) · ruta `/`

El tablero de control. Se compone de tres bloques: seis tarjetas KPI (total, activos, inactivos,
stock bajo, agotados y valor del inventario), tres gráficos (productos por categoría, salud del
stock y valor por categoría) y la tabla de **stock crítico**. Un detalle de usabilidad: las
tarjetas de stock bajo y agotados solo se ponen en rojo cuando el conteo es distinto de cero; si
todo está sano se muestran en verde, para que el rojo signifique siempre "hay que actuar". Si no
hay productos críticos, la tabla se sustituye por un mensaje en lugar de una tabla vacía.

### `#4.2-stock-health`
**Archivo:** [Home.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Home.razor)

Construye en el cliente la serie de tres porciones del gráfico de salud del stock (sanos, stock
bajo, agotados) a partir de los contadores que ya vienen del servidor, sin pedir otro endpoint.
Como `LowStockCount` incluye a los agotados, hay que **restarlos** para obtener la porción de
"solo stock bajo"; de lo contrario el gráfico contaría dos veces los mismos productos y las
porciones no sumarían el total del catálogo.

### `#4.3-nav-menu`
**Archivo:** [NavMenu.razor](../InventoryManagement/src/InventorySystem.Client/Layout/NavMenu.razor)

La navegación lateral. Cada enlace está envuelto en una comprobación de permisos, así que el
menú solo ofrece lo que el usuario realmente puede abrir: el tablero requiere
`ProductStats:view`, los movimientos de stock y la auditoría requieren `Audit:view`, crear
requiere `Products:manage` y el listado requiere `Products:view`. Ningún nombre de rol está
escrito en el código: los enlaces se dibujan a partir de lo que Keycloak conceda en tiempo de
ejecución (`#5.7-permission-store`), de modo que cambiar una política en la consola de Keycloak
cambia el menú sin recompilar nada.

---

## Cobertura del requisito §4.c

El proyecto pide que la interfaz incluya productos críticos, productos más vendidos, historial
reciente, métricas del sistema e indicadores operacionales:

| Elemento pedido | Dónde está | Ancla |
|---|---|---|
| Productos críticos | Tabla "Critical Stock" del tablero | `#4.1-dashboard-stats` |
| Productos más vendidos | Gráfico "Most Sold" en movimientos | `#2.3-movements-stats-api` |
| Historial reciente | Historial de movimientos y auditoría | `#2.3-movements-ui`, `#2.4-audit-ui` |
| Métricas del sistema | Dashboards de Grafana | `#6.8-grafana-dashboards` |
| Indicadores operacionales | Tarjetas KPI del tablero | `#4.2-dashboard-ui` |
