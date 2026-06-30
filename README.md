# HRDocs – HR Document Receiving and Tracking System

Full-stack Blazor WebAssembly + ASP.NET Core Web API application with PostgreSQL, EF Core, Radzen, QuestPDF, JWT role authorization, and SignalR notifications.

## Projects

- `HRDocs.Client` – standalone Blazor WebAssembly UI
- `HRDocs.Api` – REST API, JWT auth, SignalR hub, uploads, PDF/report endpoints
- `HRDocs.Shared` – request/response contracts and shared enums
- `HRDocs.Infrastructure` – EF Core entities, PostgreSQL context, seed data

## Run locally

1. Create PostgreSQL database `hrdocs` and update `HRDocs.Api/appsettings.json` if needed.
2. From the repository root run `dotnet ef database update --project HRDocs.Infrastructure --startup-project HRDocs.Api`.
3. Run API: `dotnet run --project HRDocs.Api --launch-profile https`.
4. Run client: `dotnet run --project HRDocs.Client --launch-profile https`.
5. Sign in as `admin@hrdocs.local` / `ChangeMe123!` and change production secrets immediately.

The API applies pending migrations and seeds roles, departments, document types, and the first admin at startup. Uploaded files are stored under `HRDocs.Api/uploads`; use persistent object storage for horizontally scaled production deployments.

## Neon

Set `DATABASE_URL` to the Neon pooled PostgreSQL URI. URI-form connection strings are normalized automatically with required SSL. Keep the connection in Render environment variables rather than source control.

## Render

The root `render.yaml` and `HRDocs.Api/Dockerfile` deploy the API as a Docker web service. Set `DATABASE_URL`, `SEED_ADMIN_PASSWORD`, and a strong `JWT_KEY`; configure `AllowedOrigins` for the deployed client URL. The health check is `/health`.

The Blueprint creates both `hrdocs-api` and the static `hrdocs-client` service. If either Render service name changes, update the client production API URL and the API allowed origin to match the generated `onrender.com` hostnames.
