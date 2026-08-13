# ProgramDesigner — Frontend (Angular)

This directory contains the Angular 19 web application for ProgramDesigner.
It is a standalone-component app with TypeScript strict mode enabled.

## Prerequisites

| Tool | Required version |
|---|---|
| Node.js | v22.x or v24.x |
| npm | ≥ 8 |
| .NET 10 SDK | (for the backend API) |

## Install dependencies

```bash
cd frontend
npm install
```

## Run the dev server

```bash
npm start
# or
npx ng serve
```

The app will be available at **http://localhost:4200**.
Angular's dev server proxies nothing — it calls the .NET API directly using
the URL configured in `src/environments/environment.development.ts`.

## API connection

The Angular environment file at
`src/environments/environment.development.ts` sets `apiBaseUrl` to
`http://localhost:5173` (the .NET API's default `http` launch profile port).

If you change the API port in
`src/ProgramDesigner.Api/Properties/launchSettings.json`, update
`environment.development.ts` accordingly.

The .NET API must be running before the frontend can make API calls:

```bash
# From the repo root
dotnet run --project src/ProgramDesigner.Api/ProgramDesigner.Api.csproj --launch-profile http
```

CORS is enabled on the API for `http://localhost:4200` via
`appsettings.Development.json` (`Cors:AllowedOrigins`). If you change
the frontend dev-server port, update that value too.

## Build for production

```bash
npm run build
# output goes to dist/program-designer-ui/
```

Replace `src/environments/environment.ts` → `apiBaseUrl` with your deployed
API origin before building for production.

## How to demo this

1. Start both the .NET API and the Angular dev server.
2. Open the UI at `http://localhost:4200` (which redirects to the Builder).
3. Click the **"⭐ Load Computer Science Example"** button. This pre-fills the form with a valid, somewhat complex recursive structure and prerequisites.
4. Click **Create Program**. You will be redirected to the Viewer page for your new program.
5. On the Viewer page, click **Validate Program**. It will show "Valid" and "No issues found."
6. Open the **Simulation Panel**, tick a few steps, select a major, and click **Run Simulation** to see dynamic `unlocked`, `complete`, and `blocked` statuses appear on the tree nodes.
7. To see a reachability warning in action: go back to the Builder, reload the example, and change the prerequisite of **Final Capstone** to be **Machine Learning** (which is inside the AI choice group). Create the program, and validate it again. It will still be Valid, but will surface a Reachability Warning because a student picking the IT or Programming major can never unlock the Final Capstone.

## Project structure

```text
frontend/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── api.models.ts          ← TypeScript interfaces mirroring the API DTOs
│   │   │   └── program-api.service.ts ← Injectable HttpClient service
│   │   ├── app.component.*            ← App shell (header + router-outlet)
│   │   ├── app.config.ts              ← Angular providers (HttpClient, Router)
│   │   └── app.routes.ts              ← Route definitions
│   ├── environments/
│   │   ├── environment.ts             ← Production environment (placeholder)
│   │   └── environment.development.ts ← Development environment (API URL)
│   ├── index.html
│   └── styles.css
├── angular.json
├── package.json
└── tsconfig.json
```
