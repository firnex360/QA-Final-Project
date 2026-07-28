# 1 · Gestión de Productos

Índice de anclas para el **Alcance Funcional §1 — Gestión de Productos**: agregar, editar,
eliminar y visualizar productos (con paginación, búsqueda, filtros y ordenamiento).

## Cómo usar este índice

Cada entrada tiene un **id de ancla** como `#1.1-create-api`. Ese mismo texto existe como
comentario dentro del código fuente.

> Copie el id (incluyendo el `#`) y péguelo en la búsqueda global de su editor
> (`Ctrl+Shift+F` en VS Code). Obtendrá dos resultados: esta documentación y el código exacto.

En el código las anclas aparecen como `// #1.1-create-api` (C#) o `@* #1.1-create-ui *@` (Razor).
Los comentarios del código están en inglés y resumen los **valores concretos** (límites,
parámetros aceptados, códigos de estado); esta guía explica **qué hace** cada parte.

## Índice rápido

| Ancla | Qué es | Archivo |
|---|---|---|
| `#1.0-product-model` | Entidad Producto / columnas de la BD | Product.cs |
| `#1.0-query-params` | Contrato del query string del listado | ProductQueryParameters.cs |
| `#1.0-paged-response` | Envoltorio de resultados paginados | PagedResponse.cs |
| `#1.0-product-api` | Mapa de rutas del controlador | ProductController.cs |
| `#1.1-create-api` | `POST /api/product` | ProductController.cs |
| `#1.1-create-ui` | Formulario de alta | Create.razor |
| `#1.1-create-ui-validation` | Validación en el navegador | Create.razor |
| `#1.2-edit-api` | `PUT /api/product/{id}` | ProductController.cs |
| `#1.2-edit-ui` | Formulario de edición | Edit.razor |
| `#1.3-delete-api` | `DELETE /api/product/{id}` | ProductController.cs |
| `#1.3-delete-ui` | Botón eliminar + confirmación | Read.razor |
| `#1.4-list-api` | `GET /api/product` | ProductController.cs |
| `#1.4-get-by-id-api` | `GET /api/product/{id}` | ProductController.cs |
| `#1.4-list-service` | Pipeline de consulta | ProductService.cs |
| `#1.4.1-search` | Búsqueda | ProductService.cs |
| `#1.4.2-filters` | Filtros categoría + stock bajo | ProductService.cs |
| `#1.4.3-sorting` | Ordenamiento | ProductService.cs |
| `#1.4.4-pagination` | Paginación y límites | ProductService.cs |
| `#1.4-list-ui` | Pantalla de productos | Read.razor |
| `#1.4-list-ui-load` | Construcción de la URL / fetch | Read.razor |
| `#1.4.1-search-ui` | Caja de búsqueda | Read.razor |
| `#1.4.2-filters-ui` | Chip de stock bajo + categorías | Read.razor |
| `#1.4.3-sorting-ui` | Menú de ordenamiento | Read.razor |
| `#1.4.4-pagination-ui` | Barra de paginación | Read.razor |
| `#1.5-details-modal` | Ventana de detalle | Read.razor |
| `#1.6-product-permissions` | Botones según permisos | Read.razor |

---

## 1.0 · Contratos de datos

### `#1.0-product-model`
**Archivo:** [Product.cs](../InventoryManagement/src/InventorySystem.Shared/Models/Product.cs)

Define la entidad Producto con los 9 campos que exige el proyecto: `Id`, `Name`, `CodeSKU`,
`Description`, `Category`, `Price`, `Quantity`, `MinimumStockLevel` e `IsActive`. Esta única
clase cumple tres funciones a la vez: es la entidad de EF Core (se mapea directamente a la tabla
`Products` de PostgreSQL), es el cuerpo JSON que viaja en las peticiones y respuestas de la API,
y es el modelo al que se enlazan los formularios de Blazor. Al estar en el proyecto **Shared**,
cliente y servidor comparten exactamente la misma definición y nunca pueden desincronizarse.
Note que no tiene anotaciones de validación: las reglas viven en la API (`#1.1-create-api`), de
modo que se aplican en un solo lugar y ningún cliente puede saltárselas.

### `#1.0-query-params`
**Archivo:** [ProductQueryParameters.cs](../InventoryManagement/src/InventorySystem.Shared/Models/ProductQueryParameters.cs)

Es el contrato del query string del listado. ASP.NET Core enlaza automáticamente cada propiedad
desde la URL mediante `[FromQuery]`, así que `?pageNumber=2&sortBy=price` se convierte solo en un
objeto. Agrupa los seis parámetros que controlan el listado: `PageNumber`, `PageSize`,
`SearchTerm`, `Category`, `SortBy`, `SortDescending` y `LowStockOnly`. Todos son opcionales y
tienen valores por defecto (página 1, 10 elementos, orden por nombre ascendente, sin filtros),
por lo que una llamada simple a `GET /api/product` ya devuelve una primera página razonable.

### `#1.0-paged-response`
**Archivo:** [PagedResponse.cs](../InventoryManagement/src/InventorySystem.Shared/Models/PagedResponse.cs)

Envoltorio genérico que devuelve el listado. Además de `Items` (los productos de la página
actual), incluye tres metadatos: `TotalCount`, `TotalPages` y `CurrentPage`. Estos tres números
son imprescindibles para la interfaz: sin ellos el paginador no podría mostrar "Página 2 de 7"
ni saber cuándo deshabilitar el botón *Siguiente*. La barra de paginación (`#1.4.4-pagination-ui`)
es literalmente una representación visual de estos tres valores.

### `#1.0-product-api`
**Archivo:** [ProductController.cs](../InventoryManagement/src/InventorySystem.Server/Controllers/ProductController.cs)

Cabecera del controlador: contiene el mapa completo de rutas bajo `/api/product` para orientarse
rápido. Todas las acciones llevan `[Authorize]`, lo que exige un JWT válido, pero la verificación
del permiso concreto no ocurre aquí sino en `PolicyEnforcementMiddleware`, que deduce el scope a
partir del verbo HTTP: GET → `view`, DELETE → `delete`, y POST/PUT/PATCH → `manage`. Por eso los
métodos no repiten reglas de autorización (ver `#1.6-product-permissions`).

---

## 1.1 · Agregar Producto

### `#1.1-create-api`
**Archivo:** [ProductController.cs](../InventoryManagement/src/InventorySystem.Server/Controllers/ProductController.cs) · `POST /api/product`

Recibe un producto en JSON, lo valida y lo guarda. Devuelve **400** con el motivo cuando se envía
un `Id` (la base de datos asigna las llaves, no el cliente), cuando `Name`, `CodeSKU`,
`Description` o `Category` vienen vacíos, cuando el precio no es mayor que 0, o cuando la cantidad
o el stock mínimo son negativos. Si todo es válido responde **200** con `{ Message, ProductId,
ProductName }`. Un detalle importante: la auditoría ocurre sola, sin código en este método, porque
`ApplicationDbContext` hereda de `AuditDbContext` de Audit.NET y registra automáticamente el
`Insert` con el usuario que lo hizo y una copia JSON de los valores nuevos.

### `#1.1-create-ui`
**Archivo:** [Create.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Create.razor) · ruta `/create`

Formulario de alta. Enlaza todos los campos a una sola instancia `newProduct` y la envía a la API
con `PostAsJsonAsync`. La categoría es un desplegable fijo (*Electronics, Clothing, Home, Books*)
en lugar de texto libre, lo cual mantiene coherente el filtro por categoría (`#1.4.2-filters`):
si cada usuario escribiera la categoría a mano, la lista se fragmentaría. Mientras la petición
está en curso, la bandera `isSubmitting` deshabilita el botón para que un doble clic no cree dos
productos, y al terminar con éxito el formulario se reinicia para poder cargar varios seguidos.

### `#1.1-create-ui-validation`
**Archivo:** [Create.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Create.razor)

Repite en el navegador las mismas reglas del servidor, pero con una diferencia de diseño: en vez
de detenerse en el primer error, acumula **todos** los problemas en una lista y los muestra
juntos, para que el usuario corrija todo de una sola pasada en lugar de descubrir los errores uno
por uno. Si hay errores, corta la ejecución antes de enviar la petición. Esto es solo comodidad:
la API vuelve a validar siempre, sin importar lo que haya hecho el cliente.

---

## 1.2 · Editar Producto

### `#1.2-edit-api`
**Archivo:** [ProductController.cs](../InventoryManagement/src/InventorySystem.Server/Controllers/ProductController.cs) · `PUT /api/product/{id}`

Actualiza un producto completo. Aplica las mismas validaciones que la creación y añade una regla
extra: si el cuerpo trae un `Id` distinto de cero, debe coincidir con el de la URL; si no,
responde 400. Devuelve 404 cuando el producto no existe. El detalle clave de implementación es
que **carga primero la entidad existente y le copia campo por campo** los valores nuevos, en vez
de adjuntar el objeto recibido. Gracias a eso, el rastreo de cambios de EF Core genera un `UPDATE`
real conociendo el estado anterior, que es justamente lo que permite que la auditoría guarde los
valores viejos y nuevos — la misma información de la que se deriva la vista de movimientos de stock.

### `#1.2-edit-ui`
**Archivo:** [Edit.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Edit.razor) · ruta `/edit/{id}`

Formulario de edición. Al iniciarse, `OnInitializedAsync` busca el producto por id
(`#1.4-get-by-id-api`) y precarga el formulario con sus datos; si no lo encuentra, muestra
*"Product not found"* en lugar de un formulario vacío y confuso. Al guardar aplica las mismas
validaciones que el alta, envía un `PUT` y, si todo sale bien, navega de vuelta a `/read` para que
el usuario vea inmediatamente la lista ya actualizada en lugar de quedarse en un formulario viejo.

---

## 1.3 · Eliminar Producto

### `#1.3-delete-api`
**Archivo:** [ProductController.cs](../InventoryManagement/src/InventorySystem.Server/Controllers/ProductController.cs) · `DELETE /api/product/{id}`

Elimina el producto de forma definitiva: borra la fila, no la marca como inactiva. (El campo
`IsActive` sirve para el estado activo/inactivo del producto, que es un concepto distinto al
borrado.) Devuelve 404 si el id no existe y 200 con un mensaje de confirmación si lo elimina. Este
endpoint es la razón de que exista un scope **`delete` separado de `manage`**: como el middleware
traduce el verbo DELETE al scope `delete`, se puede configurar un rol que cree y edite productos
pero que no tenga permiso para borrarlos.

### `#1.3-delete-ui`
**Archivo:** [Read.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Read.razor)

Muestra primero un diálogo `confirm()` de confirmación y, tras eliminar, **recarga la página
actual desde el servidor** en lugar de simplemente quitar la tarjeta de la pantalla. Recargar es
lo correcto porque mantiene sinceros los contadores `TotalCount` y `TotalPages`; y como el
servidor ajusta el número de página al rango válido (`#1.4.4-pagination`), borrar el último
producto de la última página retrocede solo en vez de dejar al usuario viendo una página vacía.

---

## 1.4 · Visualizar Productos

### `#1.4-list-api`
**Archivo:** [ProductController.cs](../InventoryManagement/src/InventorySystem.Server/Controllers/ProductController.cs) · `GET /api/product`

Endpoint del listado. Es deliberadamente delgado: solo enlaza el query string a
`#1.0-query-params`, delega todo el trabajo al servicio y devuelve un `PagedResponse<Product>`.
Un ejemplo de llamada completa sería
`?pageNumber=1&pageSize=8&searchTerm=cable&category=Electronics&sortBy=price&sortDescending=true`.

### `#1.4-get-by-id-api`
**Archivo:** [ProductController.cs](../InventoryManagement/src/InventorySystem.Server/Controllers/ProductController.cs) · `GET /api/product/{id}`

Devuelve un único producto, o 404 si no existe. Lo consume la pantalla de edición
(`#1.2-edit-ui`) para precargar su formulario.

### `#1.4-list-service`
**Archivo:** [ProductService.cs](../InventoryManagement/src/InventorySystem.Server/Services/ProductService.cs)

El corazón de esta sección y donde ocurre de verdad la paginación, búsqueda, filtrado y
ordenamiento. Parte de un `IQueryable<Product>` y le va superponiendo condiciones, pero **nada se
ejecuta** hasta llegar a `CountAsync()` y `ToListAsync()`. Esto significa que todo el LINQ se
traduce en **una sola sentencia SQL** con `WHERE`, `ORDER BY`, `OFFSET` y `FETCH`, y que la base
de datos devuelve únicamente las filas de la página pedida — nunca se traen todos los productos a
memoria para filtrarlos después. El orden de composición es intencional: **primero se reduce
(búsqueda → filtros), luego se ordena y por último se pagina**; hacerlo en otro orden daría
resultados incorrectos.

### `#1.4.1-search`
**Archivo:** [ProductService.cs](../InventoryManagement/src/InventorySystem.Server/Services/ProductService.cs)

Implementa la búsqueda: un `contains` que se aplica simultáneamente sobre **Name, CodeSKU y
Description**, de modo que una sola caja encuentra "cable" aunque la palabra esté en el nombre, en
el SKU o en la descripción. Tanto el texto buscado como la columna se pasan a minúsculas, lo que
hace la comparación insensible a mayúsculas independientemente de la configuración regional
(*collation*) de la base de datos.

### `#1.4.2-filters`
**Archivo:** [ProductService.cs](../InventoryManagement/src/InventorySystem.Server/Services/ProductService.cs)

Dos filtros independientes que se combinan libremente entre sí y con la búsqueda: **Category**,
que compara la categoría exacta, y **LowStockOnly**, que aplica la condición
`Quantity <= MinimumStockLevel`. Este segundo filtro es el que materializa la *alerta por stock
mínimo* que pide el proyecto, ya que deja a la vista únicamente los productos que necesitan
reposición.

### `#1.4.3-sorting`
**Archivo:** [ProductService.cs](../InventoryManagement/src/InventorySystem.Server/Services/ProductService.cs)

Un `switch` sobre `SortBy` que acepta `price`, `quantity`, `category` y `codesku`; cualquier otro
valor —incluido `null` o un parámetro mal escrito— cae en el caso por defecto y ordena por
**Name**. Esto hace que una petición malformada degrade al orden por defecto en lugar de provocar
un error. La bandera `SortDescending` decide entre `OrderBy` y `OrderByDescending`.

### `#1.4.4-pagination`
**Archivo:** [ProductService.cs](../InventoryManagement/src/InventorySystem.Server/Services/ProductService.cs)

Calcula la paginación con tres protecciones importantes. Primero, `TotalCount` se cuenta
**después** de aplicar búsqueda y filtros, para que el paginador describa el conjunto filtrado y
no el catálogo completo. Segundo, `PageSize` se limita al rango **1–50**, de forma que alguien que
edite la URL a mano no pueda pedir 10.000 filas y tumbar el servidor. Tercero, `CurrentPage` se
ajusta al rango disponible, que es exactamente lo que hace seguro el caso de borrar el último
elemento de la última página (`#1.3-delete-ui`).

### `#1.4-list-ui`
**Archivo:** [Read.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Read.razor) · ruta `/read`

Pantalla principal de productos. Muestra **8 tarjetas por página** en una cuadrícula responsiva.
Cada tarjeta se colorea según el estado de su stock mediante `StockStateClass` (verde = en stock,
ámbar = stock bajo, rojo = agotado), lo que convierte la alerta de stock mínimo en algo visible de
un vistazo. La página solo guarda el estado de los filtros; cualquier cambio dispara una nueva
petición al servidor.

### `#1.4-list-ui-load`
**Archivo:** [Read.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Read.razor)

Construye la URL y trae los datos. `BuildQuery` añade **solo los valores que no son los por
defecto**, así la URL se mantiene legible, y escapa lo que escribe el usuario con
`Uri.EscapeDataString` para no romper el query string. `LoadProductsAsync` vuelca la respuesta en
las variables de la página y, si la petición falla, muestra un mensaje de error y una cuadrícula
vacía en lugar de dejar un "Loading..." infinito.

### `#1.4.1-search-ui`
**Archivo:** [Read.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Read.razor)

La caja de búsqueda. Aplica un *debounce* de **400 ms** con un `CancellationTokenSource`: cada
tecla cancela la espera anterior, de modo que la API se llama una sola vez cuando el usuario hace
una pausa y no una vez por carácter. Además, toda búsqueda nueva vuelve a la página 1; de lo
contrario el usuario podría quedar en la "página 5" de un resultado que ahora solo tiene 3.

### `#1.4.2-filters-ui`
**Archivo:** [Read.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Read.razor)

El chip de stock bajo y el menú de categorías. El chip funciona también como indicador, porque
muestra el conteo real de productos en stock bajo obtenido de `GET /api/product/stats`. La lista
de categorías no está escrita en la página: se deriva de ese mismo endpoint, por lo que siempre
refleja las categorías que existen realmente en los datos.

### `#1.4.3-sorting-ui`
**Archivo:** [Read.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Read.razor)

Menú emergente con las cinco claves de ordenamiento (Nombre, Precio, Cantidad, Categoría, SKU) y
la dirección ascendente/descendente. Solo puede haber un menú abierto a la vez (categorías u
orden) y un fondo transparente los cierra al hacer clic fuera.

### `#1.4.4-pagination-ui`
**Archivo:** [Read.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Read.razor)

La barra de paginación: representa directamente los metadatos de `#1.0-paged-response` como
"Página X de Y", deshabilitando *Anterior* y *Siguiente* en los extremos. Cada clic vuelve a
pedir la página al servidor.

---

## 1.5 · Ventana de detalle

### `#1.5-details-modal`
**Archivo:** [Read.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Read.razor)

Al hacer clic en cualquier parte de una tarjeta se abre una ventana modal de solo lectura con la
información completa que no cabe en la tarjeta — sobre todo la **descripción íntegra**, que en la
cuadrícula se recorta a dos líneas — junto con el precio, la existencia actual, el stock mínimo y
el **valor del inventario** de ese producto (precio × cantidad). Para que la interacción funcione
bien, la fila de acciones y el panel de ajuste de stock usan `@onclick:stopPropagation`, de modo
que pulsar +/−, Editar o Eliminar no abra además la ventana; y el fondo oscuro cierra el modal
mientras que el panel interior detiene la propagación para que los clics dentro no lo cierren. Es
estrictamente un visor: la única acción que modifica datos es un atajo de **Editar**, y ese solo
aparece si el usuario tiene `product:manage`.

---

## 1.6 · Permisos del módulo

### `#1.6-product-permissions`
**Archivo:** [Read.razor](../InventoryManagement/src/InventorySystem.Client/Pages/Read.razor)

Define qué botones se dibujan según los scopes que Keycloak conceda al usuario:

| Recurso : scope | Habilita |
|---|---|
| `Products : view` | Ver el listado (enlace del menú y `GET`) |
| `Products : manage` | Botones Crear y Editar (`POST` / `PUT`) |
| `Products : delete` | Botón Eliminar (`DELETE`) |
| `ProductStock : manage` | Botones +/− de stock |

El cliente consulta una sola vez `/api/permissions/me` (a través de `PermissionStore`) y dibuja
cada botón en consecuencia. Nada en el cliente tiene nombres de roles escritos en el código: si se
cambia una política en la consola de Keycloak, la interfaz se adapta sin recompilar. Es importante
entender que **ocultar un botón no es seguridad**: el servidor consulta a Keycloak en cada
petición y falla de forma cerrada si ningún recurso coincide con la URI.
