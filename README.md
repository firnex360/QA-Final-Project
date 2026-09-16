# QA-Final-Project

This is a repo for version control of the final project of the class "Quality Assurance". This project has as objective learning devops, testing code, metrics, build and deploying using actions, better use of commits and pull request and using better GitHub.

For the project we would be using [.NET Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) and .NET version: 10.0.300 download [here](https://dotnet.microsoft.com/es-es/download).

## Quick start

docker compose up --build

From InventoryManagement/src run `docker compose up --build`. Then access the client at http://localhost:9090, log in,
and browse around a bit to generate telemetry; then check the dashboards at
http://localhost:3000.

# Technical documentation — Inventory Management System

Documentation by project requirement. Each document is an **anchor index**: a list of
identifiers that exist both here and as a comment inside the code.

## How it works

Each section has an id like `#1.1-create-api`. That same text is written in the corresponding
source file.

> Copy the id (with the `#`) and paste it into the editor's global search (`Ctrl+Shift+F`).
> You will get two results: the explanation here and the exact code.

In the code, anchors appear as `// #1.1-create-api` (C#), `@* #1.1-create-ui *@` (Razor),
`# #6.5-alert-rules` (YAML), or in the `description` field (JSON of Grafana dashboards). The
code comments are in **English** and summarize the concrete values (limits, parameters,
status codes); these documents are in **Spanish** and explain what it does and why.

## Documents

| Doc | Project requirement | Anchors |
|---|---|---|
| [01 · Product Management](01-gestion-de-productos.md) | Functional Scope §1 — creation, editing, deletion, and display with pagination, search, filters, and sorting | `#1.x` |
| [02 · Stock Control](02-control-de-stock.md) | Functional Scope §2 — inbound/outbound, minimum stock alerts, movement history, and audit | `#2.x` |
| [03 · Enterprise API](03-api-empresarial.md) | Functional Scope §3 — REST API documented with OpenAPI and Swagger UI | `#3.x` |
| [04 · Interface and Dashboard](04-interfaz-usuario.md) | Functional Scope §4 — control panel, indicators, and usability | `#4.x` |
| [05 · Roles and Security](05-roles-y-seguridad.md) | Mandatory granular model and Security — Keycloak, OAuth2, JWT, scopes, and policies | `#5.x` |
| [06 · Observability and Telemetry](06-observabilidad-telemetria.md) | Observability — OpenTelemetry, Prometheus, Tempo, Loki, Alloy, Grafana, and Alertmanager | `#6.x` |
| [07 · Testing Guide](07-guia-de-pruebas.md) | Full Stack Testing — where each test type is, how to run it, and what it covers | — |

> Document 07 is a guide, not an anchor index: tests are already separated by project
> and by file, so the useful unit there is the project and not the line of code.


