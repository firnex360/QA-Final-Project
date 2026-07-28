# 7 · Guía de Pruebas

Guía del requisito **Full Stack Testing**: dónde vive cada tipo de prueba, cómo ejecutarla y qué
cubre.

A diferencia de los documentos 01–06, este no es un índice de anclas. Las pruebas ya están
separadas por archivo y por proyecto, así que la unidad útil aquí es el proyecto, no la línea de
código. Lo que sí se mantiene es el criterio: explicar **qué hace y por qué**, no repetir lo que
el código ya dice.

## Índice rápido

| Área | Dónde | Pruebas | Necesita |
|---|---|---|---|
| 7.1 · Unit Testing | `test/unit-testing` | 31 | — |
| 7.2 · Integration Testing | `test/integration/Tests` | 16 | Docker |
| 7.3 · API / Contract Testing | `test/api-testing` | 6 peticiones · 24 aserciones | Pila levantada |
| 7.4 · E2E Testing | `test/e2e-testing` | 4 | Pila levantada + navegador |
| 7.5 · Security Testing | `test/integration/SecurityTests` + `.github/workflows/security.yml` | 10 + 2 escaneos | Docker / GitHub |
| 7.6 · Performance Testing | `test/performance-testing` | 3 escenarios | Pila levantada |
| 7.7 · Data Testing | `test/data-testing` | 5 | Docker |
| 7.8 · Exploratory Testing | — | pendiente | — |

Todas las rutas son relativas a `InventoryManagement/`.

---

## Antes de empezar

Las pruebas se dividen en dos grupos según lo que necesitan:

**No necesitan nada** — las unitarias. Se ejecutan sin base de datos y sin red.

**Necesitan Docker** — integración y datos. Levantan su propio contenedor de PostgreSQL con
Testcontainers, lo migran, lo usan y lo destruyen. No tocan la base de datos de desarrollo, así
que se pueden ejecutar en cualquier momento sin miedo a ensuciar datos.

**Necesitan la pila levantada** — API, E2E y rendimiento. Estas prueban el sistema **ya
desplegado y funcionando**, no una versión en memoria. Desde `InventoryManagement/src`:

```bash
docker compose up --build
```

Los usuarios de prueba (`api-test` y `e2e-testing`, ambos con contraseña `12345`) vienen
incluidos en `realm-export.json`, así que un clon limpio puede ejecutar todo sin crear cuentas a
mano. Keycloak solo importa el realm la **primera** vez: si el realm ya existe en el volumen, hay
que eliminar el volumen para que los cambios del archivo se apliquen.

---

## 7.1 · Unit Testing

**Dónde:** `test/unit-testing`
**Herramientas:** xUnit v3 + Moq

```bash
dotnet test InventoryManagement/test/unit-testing/unit-testing.csproj
```

Prueban los controladores de forma aislada: se sustituye `IProductService` por un doble de Moq,
de modo que lo que se verifica es la **traducción entre el resultado del servicio y la respuesta
HTTP**, no la base de datos. Es lo que permite que corran en un segundo y sin infraestructura.

| Archivo | Qué cubre |
|---|---|
| `CreateProductTests.cs` | Alta válida y cada validación de entrada (nombre, SKU, precio, ID asignado, cuerpo nulo) |
| `UpdateProductTests.cs` | Edición, producto inexistente, ID del cuerpo que no coincide con el de la URL |
| `DeleteProductTests.cs` | Borrado y 404 |
| `GetAllProductsTests.cs` | Listado, lista vacía, paso de filtros y paginación al servicio |
| `GetProductByIdTests.cs` | Consulta individual y 404 |
| `AdjustStockTests.cs` | Entrada, salida, `delta` cero, producto inexistente y stock insuficiente |
| `StockMovementTests.cs` | Derivación del historial desde la auditoría (usa una base en memoria) |
| `AuditControllerTests.cs` | Orden por fecha y tope de registros |
| `AuthControllerTests.cs` | Token correcto, credenciales inválidas y Keycloak inalcanzable (502) |

---

## 7.2 · Integration Testing

**Dónde:** `test/integration/Tests` · fixtures en `test/integration/Fixtures`
**Herramientas:** xUnit v3 + **Testcontainers** + `WebApplicationFactory`

```bash
dotnet test InventoryManagement/test/integration/integration.csproj
```

Levantan la API completa en memoria contra un **PostgreSQL real** en contenedor
(`InventoryApiFactory`), y hacen peticiones HTTP de verdad. A diferencia de las unitarias, aquí sí
se ejecutan las consultas, el mapeo de EF Core y la serialización.

Cubren el ciclo completo de productos, los movimientos de stock y el rastro de auditoría.

> **Limitación conocida:** la autenticación de Keycloak está sustituida por un doble
> (`FakeAuthHandler` y `FakeAuthorizationDecisionService`), para que la suite no dependa de un
> servidor de identidad en marcha. Es decir, se prueba la base de datos real pero **no** Keycloak
> real. La autorización de verdad se ejercita en las pruebas E2E y de API.

---

## 7.3 · API / Contract Testing

**Dónde:** `test/api-testing/PostmanCollection.json`
**Herramientas:** Postman / Newman

```bash
npx -y newman run InventoryManagement/test/api-testing/PostmanCollection.json \
  --env-var "baseUrl=http://localhost:8090"
```

Seis peticiones y 24 aserciones contra la API **desplegada**. Verifican códigos de estado,
`Content-Type`, la forma del cuerpo, los tipos de los campos y el tiempo de respuesta.

Dos detalles de diseño que conviene conocer antes de modificar la colección:

- **La autenticación es automática.** Un script previo a nivel de colección pide un token a
  `POST /api/auth/token` con el usuario `api-test`, lo guarda en una variable y lo reutiliza
  mientras no expire. No hay que pegar tokens a mano.
- **El orden importa.** La petición de alta va primero y guarda el id del producto creado en
  `{{createdProductId}}`; las de consulta, edición y borrado usan esa variable. Antes usaban un id
  fijo, lo que hacía fallar la suite en cualquier base de datos que no tuviera ese producto. Por
  la misma razón el SKU se genera con `{{$timestamp}}`: así una ejecución interrumpida no deja
  datos que rompan la siguiente.

---

## 7.4 · E2E Testing

**Dónde:** `test/e2e-testing`
**Herramientas:** Playwright + xUnit v3

Requiere la pila levantada y el navegador instalado una sola vez:

```bash
dotnet build InventoryManagement/test/e2e-testing/e2e-testing.csproj
```
```bash
pwsh InventoryManagement/test/e2e-testing/bin/Debug/net10.0/playwright.ps1 install chromium
```
```bash
dotnet test InventoryManagement/test/e2e-testing/e2e-testing.csproj
```

Manejan un navegador real contra el cliente Blazor desplegado. La URL se puede cambiar con la
variable de entorno `E2E_BASE_URL` (por defecto `http://localhost:9090`), que es como el pipeline
apunta a `host.docker.internal`.

| Archivo | Qué cubre |
|---|---|
| `LoginTests.cs` | Usuario sin autenticar ve "Access Denied"; el enlace lleva al formulario de Keycloak; un login válido termina en el dashboard |
| `ProductsCrudTests.cs` | Ciclo completo: crear, buscar en la lista, editar y eliminar |

Cada prueba guarda capturas de pantalla en `login-test-results/` y `CRUD-test-results/`, que son
la evidencia visual de la ejecución.

> **Dos cosas a tener en cuenta.** El proyecto **no** está incluido en `InventoryManagement.slnx`,
> así que `dotnet test` sobre la solución no lo ejecuta: hay que invocarlo por ruta. Y
> `UnitTest1.cs` es un archivo de plantilla que quedó del andamiaje inicial; no prueba nada.

---

## 7.5 · Security Testing

La seguridad se prueba en dos niveles distintos.

### Pruebas automatizadas de permisos

**Dónde:** `test/integration/SecurityTests` (se ejecutan junto con las de integración)

| Archivo | Qué cubre |
|---|---|
| `JwtValidationTests.cs` | Sin token, token corrupto y token vacío devuelven 401 |
| `PermissionValidationTests.cs` | Permiso concedido devuelve 200, denegado devuelve 403, y el cuerpo del 403 explica el motivo |
| `CorsValidationTests.cs` | Un origen permitido recibe las cabeceras CORS y uno no permitido no |
| `AuthenticationFlowTests.cs` | El endpoint de token es anónimo y responde correctamente a credenciales vacías |

### Escaneos OWASP

**Dónde:** `.github/workflows/security.yml` — se ejecutan en GitHub Actions al hacer push o PR
sobre `main`.

- **OWASP Dependency-Check (SCA)** revisa las dependencias del proyecto en busca de
  vulnerabilidades conocidas y publica un informe HTML como artefacto de la ejecución.
- **OWASP ZAP (DAST)** levanta la pila con Docker Compose y escanea la API **en caliente**,
  guiándose por el documento OpenAPI en lugar de rastrear a ciegas.

> El escaneo de ZAP se hace **sin autenticar**, por lo que solo alcanza `/openapi/v1.json`: el
> resto de endpoints le responden 401. Amplía poco la superficie, pero valida las cabeceras de
> seguridad y la configuración pública.

---

## 7.6 · Performance Testing

**Dónde:** `test/performance-testing` · informes en `test/performance-testing/reports`
**Herramientas:** NBomber

```bash
dotnet run --project InventoryManagement/test/performance-testing/PerformanceTests.csproj
```

Tres escenarios sobre `GET /api/product`:

| Escenario | Propósito |
|---|---|
| `load_test_get_products` | Carga sostenida: 50 usuarios concurrentes durante 40 s |
| `stress_test_get_products` | Estrés: rampa agresiva hasta 500 copias |
| `random_spike_get_products` | Picos aleatorios de tráfico entre 10 y 200 peticiones por segundo |

Genera informes en HTML, Markdown y texto dentro de `reports/`, con latencias, percentiles,
throughput y códigos de estado.

La ejecución **se autentica** al arrancar: pide un token con el usuario `api-test` y lo adjunta a
todas las peticiones. Si el login falla, el programa se detiene de inmediato en lugar de producir
un informe lleno de 401 que parecería un problema de rendimiento sin serlo.

---

## 7.7 · Data Testing

**Dónde:** `test/data-testing/DataTests.cs`
**Herramientas:** xUnit v3 + Testcontainers

```bash
dotnet test InventoryManagement/test/data-testing/data-testing.csproj
```

Una prueba por cada área que exige el requisito, contra un PostgreSQL real levantado con la misma
imagen que usa `docker-compose.yml`.

| Prueba | Área | Qué verifica |
|---|---|---|
| `Migrations_ApplyCleanlyToAnEmptyDatabase` | Migraciones | Todas las migraciones se aplican sobre una base vacía y no queda ninguna pendiente |
| `Seed_PopulatesTheProductCatalogue` | Seeds | El catálogo inicial existe con los valores esperados |
| `Constraint_ProductWithoutSkuIsRejected` | Constraints | La base rechaza un producto sin SKU (`NOT NULL`) |
| `Duplicate_ReusingAnExistingSkuIsRejected` | Datos duplicados | El índice único impide reutilizar un SKU existente |
| `Integrity_NewProductDoesNotCollideWithSeededIds` | Integridad | Un producto nuevo recibe un id por encima del rango sembrado |

El detalle que distingue a este proyecto: el esquema se construye ejecutando **las migraciones
reales** (`Migrate()`), no `EnsureCreated()` como hacen las pruebas de integración. Es el único
lugar del proyecto donde las migraciones quedan demostradas.

La última prueba parece trivial y no lo es. Sembrar con `HasData` escribe ids explícitos, algo
que **no** mueve el contador de identidad de PostgreSQL; sin el `setval` que añade la migración,
el primer producto creado por la aplicación recibiría el id 1 y chocaría con una fila sembrada.
Esa prueba es la que vigila esa corrección.

---

## 7.8 · Manual Exploratory Testing

Pendiente. Falta documentar las *charters* de exploración, los escenarios recorridos y los
defectos encontrados.

---

## Ejecutar todo

```bash
dotnet test InventoryManagement.slnx
```

Ejecuta unitarias, integración y datos — **62 pruebas**. Quedan fuera las E2E (el proyecto no está
en la solución) y las de rendimiento (es una aplicación de consola, no un proyecto de pruebas);
ambas se invocan por ruta como se indica arriba.

## En integración continua

| Dónde | Qué ejecuta |
|---|---|
| **GitHub Actions** — `dotnet.yml` | Compilación y `dotnet test` de la solución |
| **GitHub Actions** — `security.yml` | OWASP Dependency-Check y OWASP ZAP |
| **GitHub Actions** — `sonarcloud.yml` | Análisis de calidad y cobertura |
| **Jenkins** — `src/jenkins/Jenkinsfile` | Pipeline completo: unitarias → integración → API → E2E → escaneo → quality gate → build → despliegue |

La diferencia importante es que Jenkins **despliega los contenedores y luego ejecuta las pruebas
de API y E2E contra el sistema en marcha**, no solo durante la construcción de la imagen.
