# 5 · Roles, Permisos y Seguridad

Índice de anclas para **Roles y Niveles de Acceso (Modelo Granular Obligatorio)** y **Seguridad
Obligatoria**: Keycloak, OAuth2, JWT, roles, permisos, scopes y policies.

Para saber cómo usar las anclas, ver [01-gestion-de-productos.md](01-gestion-de-productos.md).

## Índice rápido

| Ancla | Qué es | Archivo |
|---|---|---|
| `#5.0-authz-registration` | Registro del modelo de autorización | Program.cs |
| `#5.1-jwt-auth` | Validación del JWT de Keycloak | Program.cs |
| `#5.2-policy-middleware` | Punto único de control de permisos | PolicyEnforcementMiddleware.cs |
| `#5.3-keycloak-decision` | Consulta de decisiones a Keycloak (UMA) | KeycloakDecisionService.cs |
| `#5.4-resources-scopes` | Nombres de recursos y scopes | AuthorizationConstants.cs |
| `#5.6-permissions-api` | Endpoints de permisos del usuario | PermissionsController.cs |
| `#5.7-permission-store` | Caché de permisos en el cliente | PermissionStore.cs |
| `#5.8-client-oidc` | Login OIDC del navegador | Client/Program.cs |
| `#5.9-cors` | Orígenes permitidos | Program.cs |
| `#5.10-keycloak-options` | Configuración de Keycloak | KeycloakAuthorizationOptions.cs |
| `#5.11-page-guards` | Protección de páginas por URL directa | RouteGuard.razor |
| `#5.12-permission-check-api` | `GET /api/permissions/check` (pre-vuelo) | PermissionsController.cs |
| `#5.12-scope-convention` | Regla verbo → scope compartida | AuthorizationConstants.cs |

---

## La idea central

El proyecto prohíbe validar el acceso por nombre de rol. Aquí **ningún endpoint pregunta por un
rol**: la autorización se delega por completo a Keycloak Authorization Services. Los recursos,
scopes, políticas y permisos viven en la consola de Keycloak, y la aplicación se limita a
preguntar "¿este token puede hacer *X* sobre *Y*?" en cada petición.

La consecuencia práctica es que **cambiar quién puede hacer qué no requiere tocar código ni
volver a desplegar**: se edita la política en Keycloak y surte efecto de inmediato, tanto en la
API como en los botones que dibuja la interfaz.

```
Navegador ──login OIDC (code + PKCE)──► Keycloak
    │  access token (JWT)
    ▼
API ──valida firma (#5.1)──► ¿permitido? ──UMA (#5.3)──► Keycloak ──► allow / deny
```

---

## 5.1 · Autenticación

### `#5.1-jwt-auth`
**Archivo:** [Program.cs](../InventoryManagement/src/InventorySystem.Server/Program.cs)

Configura la validación del JWT emitido por Keycloak en cada petición, cumpliendo el requisito
de OAuth2 + JWT. Tres ajustes merecen explicación. `Authority` y `MetadataAddress` difieren en
Docker porque el navegador ve Keycloak en `localhost:8080` mientras que la API debe usar el
nombre del contenedor. `MapInboundClaims = false` evita que .NET renombre los claims al esquema
antiguo de Microsoft, que es lo que hace que `roles` funcione tal como lo envía Keycloak.
`ValidateAudience = false` es correcto aquí porque el token se emite para el cliente, no para la
API; la API no confía en la audiencia sino que le pregunta a Keycloak si autoriza la operación.
La expiración de sesiones y los refresh tokens se configuran en el realm de Keycloak, no en el
código.

### `#5.8-client-oidc`
**Archivo:** [Program.cs (cliente)](../InventoryManagement/src/InventorySystem.Client/Program.cs)

El inicio de sesión desde el navegador, con OpenID Connect contra Keycloak usando **Authorization
Code + PKCE** (`ResponseType = "code"`), que es el flujo correcto para una aplicación WebAssembly
porque nunca se envía un secreto al navegador. La configuración del realm y del cliente se lee
de `wwwroot/appsettings.json`, así que no hay credenciales ni URLs incrustadas en el código. Los
tokens se adjuntan automáticamente a las llamadas a la API mediante un manejador de mensajes, y
la librería los renueva en silencio con el refresh token; cuando la renovación falla, el usuario
es enviado de vuelta a Keycloak a iniciar sesión, que es el comportamiento de expiración de
sesión que pide el proyecto.

Contiene además una pieza sutil: Keycloak envía el claim `roles` como un arreglo JSON, y el
convertidor por defecto de Blazor lo dejaría como **un solo claim** cuyo valor es el texto del
arreglo, rompiendo `IsInRole` y `AuthorizeView`. La fábrica de claims personalizada desempaqueta
ese arreglo en un claim por rol; si el valor viniera malformado, deja el claim original intacto
en lugar de lanzar una excepción, que se manifestaría como que el usuario aparece deslogueado.

---

## 5.2 · Autorización

### `#5.0-authz-registration`
**Archivo:** [Program.cs](../InventoryManagement/src/InventorySystem.Server/Program.cs)

Registra el modelo: la autorización se delega a Keycloak Authorization Services y el middleware
de políticas se encarga de consultarlo. Es el punto donde se hace explícito que la matriz de
permisos no vive en este repositorio.

### `#5.2-policy-middleware`
**Archivo:** [PolicyEnforcementMiddleware.cs](../InventoryManagement/src/InventorySystem.Server/Authorization/PolicyEnforcementMiddleware.cs)

El **único punto** donde se aplican los permisos. Para cada petición a `/api/` deduce el scope a
partir del verbo HTTP — GET/HEAD → `view`, DELETE → `delete`, el resto → `manage` — y pregunta a
Keycloak si ese token puede ejercer ese scope sobre esa ruta. Que `delete` sea un scope aparte
es deliberado: permite un rol que edite pero no borre.

Tres detalles importantes de implementación:

- **Normaliza la ruta a minúsculas** antes de consultar, porque Keycloak compara las URIs de sus
  recursos distinguiendo mayúsculas mientras que el enrutado de ASP.NET no; sin esto,
  `/api/Product` y `/api/product` serían dos recursos distintos siendo el mismo endpoint.
- **Falla cerrado**: si Keycloak no responde, si ningún recurso coincide con la URI o si la
  respuesta es ambigua, se deniega. Nunca se deja pasar una petición ante la duda.
- Distingue el caso `NoResourceDefined` y lo registra con un mensaje explícito, porque casi
  siempre significa que el recurso todavía no se ha creado en Keycloak — un problema de
  configuración que es fácil confundir con uno de permisos.

Quedan exentos `/metrics`, `/openapi`, `/scalar`, `/health` y `/api/permissions`; este último
porque solo informa los permisos del propio solicitante y exigirle un permiso sería circular.

### `#5.3-keycloak-decision`
**Archivo:** [KeycloakDecisionService.cs](../InventoryManagement/src/InventorySystem.Server/Authorization/KeycloakDecisionService.cs)

Habla con Keycloak mediante el *UMA ticket grant*. Para autorizar envía
`permission=<uri>#<scope>` con `response_mode=decision` y recibe un sí o un no; Keycloak compara
la URI contra sus recursos y evalúa las políticas asociadas. Para listar permisos omite el
parámetro `permission` y usa `response_mode=permissions`, obteniendo todo lo que el usuario
tiene concedido. Solo se reenvía el token del propio usuario: no hace falta ningún secreto de
cliente. Traduce además el error `invalid_resource` de Keycloak al caso `NoResourceDefined`
para que el middleware pueda distinguirlo.

### `#5.4-resources-scopes`
**Archivo:** [AuthorizationConstants.cs](../InventoryManagement/src/InventorySystem.Shared/Authorization/AuthorizationConstants.cs)

Los nombres de recursos (`Products`, `ProductStats`, `ProductStock`, `Audit`) y scopes (`view`,
`manage`, `delete`) que la interfaz necesita nombrar, en el proyecto Shared para que cliente y
servidor los escriban igual. Es explícitamente **no** un registro de permisos: crear un permiso
o una política nueva en Keycloak no requiere tocar este archivo; solo se agrega un nombre cuando
la interfaz debe dibujar algo en función de él.

### `#5.10-keycloak-options`
**Archivo:** [KeycloakAuthorizationOptions.cs](../InventoryManagement/src/InventorySystem.Server/Authorization/KeycloakAuthorizationOptions.cs)

Configuración enlazada desde la sección `Keycloak` (appsettings y variables de entorno del
compose). Distingue `Authority` (la URL del realm como la ve el navegador, que es el emisor del
token) de `InternalAuthority` (la URL para llamadas servidor a servidor, necesaria porque desde
su contenedor la API no puede resolver `localhost`). `EnforcementEnabled` permite apagar la
comprobación en pruebas para no depender de un Keycloak vivo.

---

## 5.3 · Permisos en la interfaz

### `#5.6-permissions-api`
**Archivo:** [PermissionsController.cs](../InventoryManagement/src/InventorySystem.Server/Controllers/PermissionsController.cs)

Expone las dos formas que tiene la interfaz de preguntar qué puede hacer. `GET
/api/permissions/me` devuelve todos los pares recurso:scope que Keycloak concede a quien
pregunta — por eso el cliente no necesita su propia lista de permisos. `GET
/api/permissions/check` (`#5.12-permission-check-api`) responde por una operación concreta.
Ambos están exentos de la evaluación de políticas porque solo informan del acceso del propio
solicitante; exigirles un permiso sería circular.

### `#5.7-permission-store`
**Archivo:** [PermissionStore.cs](../InventoryManagement/src/InventorySystem.Client/Authorization/PermissionStore.cs)

Caché en el cliente de lo que el usuario puede hacer, con dos formas de consultarla según la
granularidad:

- **`Has(recurso, scope)`** — para detalles dentro de una página (los botones de una tarjeta).
  Se resuelve contra la lista que se pidió una sola vez a `/api/permissions/me`, aplanada a
  cadenas `Recurso:scope`. Emite un evento para que los componentes se redibujen al llegar.
- **`CanAsync(ruta, método)`** — para el acceso a una página completa. Delega la decisión al
  servidor (`#5.12-permission-check-api`), de modo que la página nunca nombra un recurso ni un
  scope. Cada par ruta+método se pregunta una sola vez por sesión.

Si cualquiera de las dos llamadas falla, la respuesta es **denegar**: la interfaz oculta en
lugar de mostrar de forma optimista, igual que hace el servidor.

> **Ocultar un botón no es seguridad.** Es comodidad. La API vuelve a preguntar a Keycloak en
> cada petición, así que un usuario que llame al endpoint directamente recibe 403 igualmente.

### `#5.11-page-guards`
**Archivo:** [RouteGuard.razor](../InventoryManagement/src/InventorySystem.Client/Authorization/RouteGuard.razor)

Impide que una página se abra escribiendo su URL directamente. `@attribute [Authorize]` solo
comprueba que haya sesión iniciada, no qué permisos tiene el usuario: sin este guard, un usuario
de perfil *staff* podía entrar a `/create`, `/edit/5` o `/audit` y ver la pantalla, aunque la API
después rechazara sus acciones.

Lo importante es **cómo** lo declara. La página no dice qué permiso necesita, sino **qué llamada
hace**:

```razor
<RouteGuard Path="/api/product" Method="POST" Action="create products">
    ...contenido de la página...
</RouteGuard>
```

"Esta pantalla hace `POST /api/product`" es un hecho sobre la página, no una política. Quién
puede hacerlo lo decide Keycloak, así que en el cliente no aparece ningún nombre de recurso ni
de scope, y cambiar una política en la consola de Keycloak cambia el comportamiento sin
recompilar.

Tiene dos modos de uso: envolviendo el contenido (lo oculta y muestra el aviso), o suelto
(`<RouteGuard Path="..." />`) para páginas que ya tienen su propia cadena de condiciones, como
auditoría y movimientos. Mientras la respuesta no llega no dibuja ninguna de las dos ramas, para
evitar que a un administrador le parpadee un "acceso restringido" antes del contenido.

### `#5.12-permission-check-api`
**Archivo:** [PermissionsController.cs](../InventoryManagement/src/InventorySystem.Server/Controllers/PermissionsController.cs) · `GET /api/permissions/check?path=…&method=…`

El pre-vuelo que hace posible lo anterior: responde si el usuario **podría** realizar una llamada,
sin realizarla. Recibe la ruta y el verbo, deriva el scope con `#5.12-scope-convention` y se lo
pregunta a Keycloak con el mismo `EvaluateAsync` (`#5.3-keycloak-decision`) que usa el middleware
real. Por construcción, la respuesta no puede discrepar de lo que haría la petición verdadera.

Devuelve un booleano crudo (`true` / `false`), sin objeto de transferencia: la respuesta es un
solo bit y no justifica un tipo propio. Solo acepta rutas que empiecen por `/api/`.

### `#5.12-scope-convention`
**Archivo:** [AuthorizationConstants.cs](../InventoryManagement/src/InventorySystem.Shared/Authorization/AuthorizationConstants.cs)

La regla verbo → scope (GET/HEAD → `view`, DELETE → `delete`, el resto → `manage`) escrita **una
sola vez**. Antes vivía duplicada dentro del middleware; al extraerla, tanto la evaluación real
(`#5.2-policy-middleware`) como el pre-vuelo la comparten, de modo que la interfaz no puede
asumir una regla distinta de la que se aplica de verdad.

Si en el futuro algún endpoint necesitara un scope que su verbo no expresa (por ejemplo un `POST`
que aprueba en lugar de crear), la forma de resolverlo sería un atributo que anule esta
convención. No existe hoy porque ningún endpoint lo requiere.

### `#5.9-cors`
**Archivo:** [Program.cs](../InventoryManagement/src/InventorySystem.Server/Program.cs)

Lista blanca de orígenes autorizados a llamar la API desde un navegador: el cliente Blazor en
desarrollo (5167 y 7141) y en Docker (9090). Cualquier otro origen es bloqueado por el
navegador. Las peticiones `OPTIONS` de pre-vuelo quedan exentas del middleware de permisos
porque no llevan credenciales y bloquearlas rompería CORS.

---

## Matriz de permisos

Los roles se construyen en Keycloak combinando permisos; el realm incluye tres:

| Permiso | adminY | managerY | staffY |
|---|:--:|:--:|:--:|
| `product:view` | ✅ | ✅ | ✅ |
| `product:manage` | ✅ | ✅ | — |
| `stock:view` | ✅ | ✅ | ✅ |
| `stock:manage` | ✅ | ✅ | — |
| `report:view` | ✅ | ✅ | ✅ |
| `audit:view` | ✅ | ✅ | — |
| `user:manage` | ✅ | — | — |

La definición vive en [realm-export.json](../InventoryManagement/src/keycloak/realm-export.json)
(se importa al levantar el contenedor) y puede regenerarse con
`scripts/configure-keycloak-permissions.ps1`. Al ser un archivo JSON que Keycloak importa, no
lleva anclas: JSON no admite comentarios.

> **Nota:** el permiso `stock:view` está definido y asignado a los tres roles, pero la pantalla
> de movimientos se protege hoy con `audit:view`, porque los datos se derivan de la tabla de
> auditoría (ver `#2.3-movements-api`). Es una decisión consciente, no un olvido.
