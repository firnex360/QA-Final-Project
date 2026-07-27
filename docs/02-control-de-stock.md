# 2 · Control de Stock

Índice de anclas para el **Alcance Funcional §2 — Control de Stock**: actualización de
existencias, alertas por stock mínimo, historial de movimientos y auditoría.

Para saber cómo usar las anclas, ver [01-gestion-de-productos.md](01-gestion-de-productos.md).

## Índice rápido

| Ancla | Qué es | Archivo |
|---|---|---|
| `#2.1-adjust-api` | `PATCH /api/product/{id}/stock` | ProductController.cs |
| `#2.1-adjust-service` | Lógica de entrada/salida y validación | ProductService.cs |
| `#2.1-adjust-ui` | Botones +/− y panel de ajuste | Read.razor |
| `#2.2-low-stock-alerts` | Alertas por stock mínimo | Read.razor |
| `#2.3-movements-api` | `GET /api/audit/stock-movements` | AuditController.cs |
| `#2.3-movements-derive` | Derivación del historial desde la auditoría | AuditController.cs |
| `#2.3-movements-stats-api` | Totales y productos más vendidos | AuditController.cs |
| `#2.3-movements-ui` | Pantalla de historial de movimientos | StockMovements.razor |
| `#2.4-audit-entity` | Tabla de auditoría | AuditLog.cs |
| `#2.4-audit-dbcontext` | Contexto que activa la auditoría | ApplicationDbContext.cs |
| `#2.4-audit-config` | Configuración de Audit.NET | Program.cs |
| `#2.4-audit-api` | `GET /api/audit` | AuditController.cs |
| `#2.4-audit-stats-api` | `GET /api/audit/stats` | AuditController.cs |
| `#2.4-audit-ui` | Pantalla de auditoría | Audit.razor |

---

## 2.1 · Actualizar Stock

### `#2.1-adjust-api`
**Archivo:** [ProductController.cs](../InventoryManagement/src/InventorySystem.Server/Controllers/ProductController.cs) · `PATCH /api/product/{id}/stock?delta=N`

Endpoint único para entradas y salidas de mercancía. En lugar de tener dos rutas separadas usa
un solo parámetro con signo: un `delta` positivo suma existencias (entrada) y uno negativo las
resta (salida). Un `delta` de cero se rechaza con 400 porque no representa ningún movimiento
real. Se usa el verbo `PATCH` y no `PUT` porque solo modifica un campo del producto, no el
recurso completo. Devuelve 404 si el producto no existe y 400 si el movimiento dejaría el stock
en negativo.

### `#2.1-adjust-service`
**Archivo:** [ProductService.cs](../InventoryManagement/src/InventorySystem.Server/Services/ProductService.cs)

Aplica el movimiento sobre la cantidad actual del producto. Su regla de negocio principal es que
**el stock nunca puede quedar por debajo de cero**: si el resultado sería negativo lanza una
excepción con un mensaje que indica la cantidad actual y el cambio solicitado, y no guarda nada.
Es importante notar lo que **no** hace: no escribe en ninguna tabla de movimientos. Al llamar a
`SaveChangesAsync` se dispara el interceptor de auditoría (`#2.4-audit-config`), que registra el
cambio de cantidad; de ahí se deriva después todo el historial (`#2.3-movements-derive`).

### `#2.1-adjust-ui`
**Archivo:** [Read.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Read.razor)

Cada tarjeta de producto tiene dos botones, **−** y **+**, que abren un panel en línea donde el
usuario escribe la cantidad a mover. Al confirmar se envía el delta con el signo correspondiente
a la API y se muestra un aviso temporal con la transición ("Cable: 20 → 16"). Solo se dibujan si
el usuario tiene el scope `ProductStock:manage`. Si el filtro de stock bajo está activo, la
lista se recarga tras el movimiento, porque el producto puede haber dejado de cumplir el filtro.

---

## 2.2 · Alertas por stock mínimo

### `#2.2-low-stock-alerts`
**Archivo:** [Read.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Read.razor)

Traduce la cantidad de cada producto en uno de tres estados que se reflejan a la vez en el color
del borde de la tarjeta y en su etiqueta: **agotado** cuando la cantidad es 0 (rojo), **stock
bajo** cuando la cantidad es menor o igual al mínimo configurado (ámbar), y **en stock** en
cualquier otro caso (verde). Es importante entender que la misma regla existe en tres lugares
distintos con propósitos distintos: aquí como aviso visual, en el servidor como filtro
`LowStockOnly` (`#1.4.2-filters`), y en el dashboard como los contadores `LowStockCount` y la
lista de productos críticos (`#4.1-dashboard-stats`).

---

## 2.3 · Historial de Movimientos

> **Decisión de diseño:** no existe una tabla `StockMovements`. El historial se **deriva** de la
> tabla de auditoría, que ya registra cada cambio de cantidad con fecha, usuario y valores
> anterior/nuevo. Así no se duplica información ni puede desincronizarse de la auditoría.

### `#2.3-movements-api`
**Archivo:** [AuditController.cs](../InventoryManagement/src/InventorySystem.Server/Controllers/AuditController.cs) · `GET /api/audit/stock-movements`

Devuelve los 100 movimientos más recientes, del más nuevo al más viejo. Cada elemento trae
exactamente los campos que exige el proyecto: fecha, usuario, tipo de movimiento, cantidad
anterior y cantidad nueva. Como los datos viven en la auditoría, el permiso que lo protege es
`Audit:view`.

### `#2.3-movements-derive`
**Archivo:** [AuditController.cs](../InventoryManagement/src/InventorySystem.Server/Controllers/AuditController.cs)

Es la pieza clave del módulo: convierte filas de auditoría en movimientos de stock. Primero
filtra en la base de datos los registros cuya entidad sea `Product`, cuya acción sea `Update` y
cuyo campo `AffectedColumns` mencione `Quantity`. Ese último filtro es solo una búsqueda de
texto, así que cada candidato se **confirma** intentando extraer un entero `Quantity` de los
JSON `OldValues` y `NewValues` y exigiendo que ambos difieran; de ese modo un cambio que apenas
mencione la palabra no se cuela como movimiento falso. El signo de `Delta = nuevo - anterior`
determina si fue entrada o salida. Finalmente resuelve el nombre del producto contra la tabla
`Products` para que los reportes se lean por nombre, y si el producto fue eliminado usa
`Product #id` como respaldo en lugar de dejar el nombre vacío.

### `#2.3-movements-stats-api`
**Archivo:** [AuditController.cs](../InventoryManagement/src/InventorySystem.Server/Controllers/AuditController.cs) · `GET /api/audit/stock-movements/stats`

Agrega esos mismos movimientos derivados para alimentar los gráficos: unidades totales que
entraron, unidades que salieron, conteo por tipo, actividad de los últimos 7 días y —lo más
relevante para el requisito de reportes— **`TopProducts`, los productos más vendidos**,
ordenados por cantidad de unidades que salieron del inventario. Note que "más vendido" se define
como el que más unidades ha perdido por salidas, no el que más veces fue tocado.

### `#2.3-movements-ui`
**Archivo:** [StockMovements.razor](../InventoryManagement/src/InventorySystem.Client/Pages/StockMovements.razor) · ruta `/stock-movements`

Pantalla del historial, construida con el mismo lenguaje visual que la de auditoría. Arriba
cuatro tarjetas KPI (movimientos totales, unidades entradas, unidades salidas y actividad de la
semana), en medio tres gráficos (movimientos por tipo, actividad de 7 días y **productos más
vendidos**) y abajo una tabla paginada de 5 filas con la transición `anterior → nueva` y las
unidades con signo y color. Como todo proviene de la auditoría, la página se protege con
`audit:view`; si el usuario no lo tiene, muestra un aviso claro en vez de quedarse cargando.

---

## 2.4 · Auditoría

### `#2.4-audit-entity`
**Archivo:** [AuditLog.cs](../InventoryManagement/src/InventorySystem.Server/Models/AuditLog.cs)

La tabla donde vive todo el rastro de cambios. Guarda qué entidad cambió, su llave primaria, la
acción (`Insert`, `Update`, `Delete`), la fecha UTC y el usuario. Los tres campos que le dan
poder son `OldValues`, `NewValues` y `AffectedColumns`, que almacenan copias JSON del antes y el
después: gracias a ellos el historial de movimientos puede derivarse sin una segunda tabla. La
clase lleva `[AuditIgnore]` para que el sistema no audite sus propias inserciones, lo que
provocaría un bucle infinito.

### `#2.4-audit-dbcontext`
**Archivo:** [ApplicationDbContext.cs](../InventoryManagement/src/InventorySystem.Server/Data/ApplicationDbContext.cs)

Una sola decisión hace automática toda la auditoría: el contexto hereda de `AuditDbContext` de
Audit.NET en lugar del `DbContext` normal. A partir de ahí, cualquier `SaveChanges` sobre
cualquier entidad rastreada genera su registro de auditoría, sin que ningún servicio o
controlador tenga que acordarse de registrar nada. Esto cumple el requisito de auditoría
mediante "Hibernate Envers o mecanismo equivalente" — Audit.NET es el equivalente en .NET.

### `#2.4-audit-config`
**Archivo:** [Program.cs](../InventoryManagement/src/InventorySystem.Server/Program.cs)

Define cómo se rellena cada registro de auditoría. Toma el nombre de la entidad, su llave, la
acción y la fecha; obtiene el usuario del JWT de Keycloak (`preferred_username`, o `anonymous`
si no hay sesión); y serializa los valores según el tipo de operación: en un `Update` guarda
solo las columnas que cambiaron con su valor viejo y nuevo, en un `Insert` solo los valores
nuevos y en un `Delete` los valores que tenía la fila. Además incrementa el contador de
Prometheus que alimenta el dashboard de negocio (`#6.2-business-metric`).

### `#2.4-audit-api` y `#2.4-audit-stats-api`
**Archivo:** [AuditController.cs](../InventoryManagement/src/InventorySystem.Server/Controllers/AuditController.cs)

El primero devuelve los 100 registros más recientes; el tope evita enviar la tabla completa y el
cliente pagina lo que recibe. El segundo entrega las cifras agregadas: totales de hoy y de los
últimos 7 días, y desgloses por acción, por usuario (los 5 más activos) y por entidad. La serie
de 7 días se agrupa en memoria porque el conjunto es pequeño, lo que evita depender de funciones
SQL de truncado de fechas que varían entre motores.

### `#2.4-audit-ui`
**Archivo:** [Audit.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Audit.razor) · ruta `/audit`

Visor del rastro de auditoría. Lo interesante es cómo presenta los cambios: en vez de mostrar el
JSON crudo, lo convierte en "píldoras" con el formato `campo: viejo → nuevo`, coloreadas según
la acción, de modo que se entiende qué cambió sin leer JSON. Si algún registro trae JSON
malformado, simplemente no dibuja píldoras en lugar de romper la página.
