# ProgramDesigner

## Overview
ProgramDesigner is an education-management tool designed for education program designers. It allows users to visually compose, validate, and simulate complex program structures, such as a university curriculum or a corporate training track. Programs are built as a hierarchical tree of concrete steps (like sessions or tests) and logical groups (such as in-order sequences or pick-N choices), with support for strict prerequisite rules.

## Setup & Run Instructions
**Prerequisites:**
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (LTS recommended, e.g., v20+)
- [Angular CLI](https://angular.io/cli) (v19.2.27 or higher)

**1. Run the API:**
Open a terminal in the repository root and run:
```powershell
dotnet restore ProgramDesigner.slnx
dotnet run --project src\ProgramDesigner.Api\ProgramDesigner.Api.csproj
```
*The API will start and listen on `http://localhost:5173`.*

**2. Run the Test Suite:**
Open a separate terminal in the repository root and run:
```powershell
dotnet test ProgramDesigner.slnx
```
*Expect 23 tests to pass successfully.*

**3. Run the Frontend:**
Open a separate terminal, navigate to the `frontend` folder, and start the development server:
```powershell
cd frontend
npm install
npm start
```
*The frontend will be available at `http://localhost:4200` and is configured via `environment.development.ts` to automatically route API calls to `http://localhost:5173`.*

## Data Model Explanation
The domain model is built as a recursive, generic tree of components:
- **`ProgramNode`**: The abstract base class representing any node in the program.
- **`StepNode`**: A leaf node representing a concrete activity (e.g., a "session" or "test").
- **`GroupNode`**: A container node that holds other nodes and applies a completion rule (either `inOrder` or `choice`).
- **`EducationProgram`**: The aggregate root that manages the overall program and enforces structural invariants.

When parsing JSON, the API uses a discriminator field (`"type": "step"` or `"type": "group"`) to dynamically reconstruct the polymorphic `children` arrays.

**Example nested payload:**
```json
{
  "name": "Computer Science Degree",
  "rootGroup": {
    "name": "Core Curriculum",
    "groupRule": "inOrder",
    "children": [
      {
        "type": "step",
        "name": "Intro to Programming",
        "stepType": "session"
      },
      {
        "type": "group",
        "name": "Major Selection",
        "groupRule": "choice",
        "pickCount": 1,
        "children": [
          {
            "type": "step",
            "name": "AI Capstone",
            "stepType": "test"
          }
        ]
      }
    ]
  }
}
```

## API Contract

**1. Create a Program**
- **Method:** `POST /programs`
- **Request Body:** JSON representing the program. Clients use a temporary `key` to identify nodes for prerequisite references (`prerequisiteRef`) before the server generates GUIDs.
- **Status:** 201 Created (or 400 Bad Request for structural violations).
```json
{
  "name": "Computer Science Degree",
  "rootGroup": {
    "name": "Root",
    "groupRule": "inOrder",
    "children": [
      {
        "type": "group",
        "name": "Major",
        "groupRule": "choice",
        "pickCount": 1,
        "key": "major",
        "children": [
          {
            "type": "group",
            "name": "AI",
            "groupRule": "inOrder",
            "children": [
              { "type": "step", "name": "AI Capstone", "stepType": "test", "key": "aiCapstone" }
            ]
          }
        ]
      },
      { 
        "type": "step", 
        "name": "Final Capstone", 
        "stepType": "test", 
        "key": "finalCapstone", 
        "prerequisiteRef": "major" 
      }
    ]
  }
}
```

**2. Get a Program**
- **Method:** `GET /programs/{id}`
- **Response Body:** The stored program tree with resolved GUIDs.
- **Status:** 200 OK (or 404 Not Found).
```json
{
  "id": "e0b7a8a1-c052-4afc-a634-1188478d10ed",
  "name": "Computer Science Degree",
  "rootGroup": {
    "groupRule": "inOrder",
    "pickCount": null,
    "children": [
      {
        "type": "group",
        "groupRule": "choice",
        "pickCount": 1,
        "children": [
          {
            "type": "group",
            "groupRule": "inOrder",
            "pickCount": null,
            "children": [
              {
                "type": "step",
                "stepType": "test",
                "id": "7662c575-b873-455a-b9c1-487ad048f0e3",
                "name": "AI Capstone",
                "prerequisiteId": null,
                "prerequisiteName": null
              }
            ],
            "id": "27680bb0-54ec-4286-9a25-c637a77e2ffb",
            "name": "AI",
            "prerequisiteId": null,
            "prerequisiteName": null
          }
        ],
        "id": "f516a738-dbb4-4b53-a128-4ef9c5f8742b",
        "name": "Major",
        "prerequisiteId": null,
        "prerequisiteName": null
      },
      {
        "type": "step",
        "stepType": "test",
        "id": "e14b8a24-9b57-45eb-89da-2244243b9ce7",
        "name": "Final Capstone",
        "prerequisiteId": "f516a738-dbb4-4b53-a128-4ef9c5f8742b",
        "prerequisiteName": "Major"
      }
    ],
    "id": "4eec3b5e-04f8-4a92-be20-569611db844e",
    "name": "Root",
    "prerequisiteId": null,
    "prerequisiteName": null
  }
}
```

**3. Validate a Program**
- **Method:** `POST /programs/{id}/validate`
- **Response Body:** Analysis of the prerequisite structure.
- **Status:** 200 OK (or 404 Not Found).
```json
{
  "isValid": true,
  "impossiblePrerequisites": [],
  "reachabilityWarnings": []
}
```

**4. Simulate a Program**
- **Method:** `POST /programs/{id}/simulate`
- **Request Body:** `{ "choices": { "<choiceGroupId>": ["<pickedChildId>"] }, "completedStepIds": [] }`
- **Response Body:** The program tree annotated with real-time `status` (unlocked, blocked, complete).
- **Status:** 200 OK (or 400 Bad Request, 404 Not Found).
```json
{
  "rootNode": {
    "id": "4eec3b5e-04f8-4a92-be20-569611db844e",
    "name": "Root",
    "nodeType": "group",
    "status": "unlocked",
    "blockedReason": null,
    "children": [
      {
        "id": "e14b8a24-9b57-45eb-89da-2244243b9ce7",
        "name": "Final Capstone",
        "nodeType": "step",
        "status": "blocked",
        "blockedReason": "Blocked: prerequisite 'Major' not yet complete.",
        "children": []
      }
    ]
  }
}
```

## Validation Logic Explained
The API runs two levels of validation:
- **Impossible Prerequisites (Strictly Rejected):** Identifies cycles, self-references, or forward-references (e.g., trying to require a node that appears *after* the current node in a pre-order traversal). Programs with impossible prerequisites are completely rejected by the `isValid: false` flag.
- **Reachability Warnings (Advisory):** Identifies soft logic flaws. For example, if the `Final Capstone` requires `AI Capstone` (which is nested deep inside a specific `Major` choice branch), participants who choose the Web branch will be permanently locked out of the Final Capstone. This produces a *Reachability Warning*. Conversely, if `Final Capstone` simply requires the parent `Major` choice group itself, this is universally safe. The algorithm intelligently excludes siblings in the same branch (e.g., `AI Capstone` requiring `Electives` within the same AI branch correctly generates no warning).

## Frontend Guide
The Angular SPA provides a dynamic UI to visually interact with the backend API. 
- **Program Builder**: Build deep program trees visually using drag-and-drop principles and form controls. It includes a convenient "Load CS Example" button to quickly scaffold a complex curriculum.
- **Program Viewer & Validator**: Read-only display of a created program, accompanied by a validation panel that executes the `/validate` endpoint and highlights Reachability Warnings or Impossible Prerequisites. 

**Demo Walkthrough:**
1. Navigate to `http://localhost:4200`.
2. Click "Load CS Example", then "Create Program". 
3. The UI will jump to the Viewer. Click the "Validate Program" button. The panel should report a clean bill of health.
4. Go back to the Builder, modify the Final Capstone's prerequisite to point directly at "AI Capstone" instead of "Major", and save.
5. Re-run validation. You will now see a reachability warning appear!

*(Note: The frontend features a "Known Programs" sidebar search. This is a client-side UX convenience powered by `localStorage` to save you from copying and pasting GUIDs; the backend remains strictly ID-based).*

## AI Tool Usage
Antigravity (an AI coding agent) was used to implement both the backend and frontend from a structured set of story-by-story prompts covering the domain model, API endpoints, prerequisite/reachability validators, tests, and UI. A running `PROJECT_CONTEXT.md` file was continuously maintained to ensure implementation decisions and architectural constraints remained consistent across AI sessions.

## Design Decisions & Assumptions
- **Temporary Keys for Resolution:** Because GUIDs are generated exclusively by the server during creation, clients assign a temporary string `key` to nodes in the `POST /programs` payload. References (`prerequisiteRef`) are resolved against these keys by the mapper to establish structural links.
- **LCA (Lowest Common Ancestor) Validation:** Naively flagging any prerequisite inside a Choice branch causes false positives (e.g., a node requiring its immediate preceding sibling). The ReachabilityValidator uses an LCA walk-up algorithm to check if the prerequisite source and target strictly share the same path.
- **In-Memory Storage:** The challenge uses a lightweight, dependency-free in-memory repository to minimize setup friction. In a production environment, this would be swapped out for EF Core (SQL Server / PostgreSQL).
- **Future Enhancements:** If extended, this platform could support:
  1. *Program Versioning*: Creating immutable snapshots of a program when participants are actively enrolled.
  2. *Audit Trail*: Tracking the modification history of the program structure.
  3. *Participant Simulation Mode*: Expanding the simulation API into an interactive frontend view where designers can "play-test" the tree by actively clicking through choices.

## Known Limitations
- No Authentication or Authorization.
- Data is stored strictly in memory and will be lost if the API restarts.
- The "Known Programs" search is a client-side local cache; there is no backend search index.
- The simulate endpoint expects the user to correctly provide Choice branch selections.

## Screenshots

![Program Builder]
<img width="1073" height="913" alt="image" src="https://github.com/user-attachments/assets/5b9e81ee-189f-4816-9c9e-cc12392e855b" />

*Visual program tree builder.*

![Program Viewer]
<img width="1292" height="909" alt="image" src="https://github.com/user-attachments/assets/5188f3d5-9d5c-485e-860f-2944e7599b14" />

}
        ]
      }
    ]
  }
}
```

## API Contract

**1. Create a Program**
- **Method:** `POST /programs`
- **Request Body:** JSON representing the program. Clients use a temporary `key` to identify nodes for prerequisite references (`prerequisiteRef`) before the server generates GUIDs.
- **Status:** 201 Created (or 400 Bad Request for structural violations).
```json
{
  "name": "Computer Science Degree",
  "rootGroup": {
    "name": "Root",
    "groupRule": "inOrder",
    "children": [
      {
        "type": "group",
        "name": "Major",
        "groupRule": "choice",
        "pickCount": 1,
        "key": "major",
        "children": [
          {
            "type": "group",
            "name": "AI",
            "groupRule": "inOrder",
            "children": [
              { "type": "step", "name": "AI Capstone", "stepType": "test", "key": "aiCapstone" }
            ]
          }
        ]
      },
      { 
        "type": "step", 
        "name": "Final Capstone", 
        "stepType": "test", 
        "key": "finalCapstone", 
        "prerequisiteRef": "major" 
      }
    ]
  }
}
```

**2. Get a Program**
- **Method:** `GET /programs/{id}`
- **Response Body:** The stored program tree with resolved GUIDs.
- **Status:** 200 OK (or 404 Not Found).
```json
{
  "id": "e0b7a8a1-c052-4afc-a634-1188478d10ed",
  "name": "Computer Science Degree",
  "rootGroup": {
    "groupRule": "inOrder",
    "pickCount": null,
    "children": [
      {
        "type": "group",
        "groupRule": "choice",
        "pickCount": 1,
        "children": [
          {
            "type": "group",
            "groupRule": "inOrder",
            "pickCount": null,
            "children": [
              {
                "type": "step",
                "stepType": "test",
                "id": "7662c575-b873-455a-b9c1-487ad048f0e3",
                "name": "AI Capstone",
                "prerequisiteId": null,
                "prerequisiteName": null
              }
            ],
            "id": "27680bb0-54ec-4286-9a25-c637a77e2ffb",
            "name": "AI",
            "prerequisiteId": null,
            "prerequisiteName": null
          }
        ],
        "id": "f516a738-dbb4-4b53-a128-4ef9c5f8742b",
        "name": "Major",
        "prerequisiteId": null,
        "prerequisiteName": null
      },
      {
        "type": "step",
        "stepType": "test",
        "id": "e14b8a24-9b57-45eb-89da-2244243b9ce7",
        "name": "Final Capstone",
        "prerequisiteId": "f516a738-dbb4-4b53-a128-4ef9c5f8742b",
        "prerequisiteName": "Major"
      }
    ],
    "id": "4eec3b5e-04f8-4a92-be20-569611db844e",
    "name": "Root",
    "prerequisiteId": null,
    "prerequisiteName": null
  }
}
```

**3. Validate a Program**
- **Method:** `POST /programs/{id}/validate`
- **Response Body:** Analysis of the prerequisite structure.
- **Status:** 200 OK (or 404 Not Found).
```json
{
  "isValid": true,
  "impossiblePrerequisites": [],
  "reachabilityWarnings": []
}
```

**4. Simulate a Program**
- **Method:** `POST /programs/{id}/simulate`
- **Request Body:** `{ "choices": { "<choiceGroupId>": ["<pickedChildId>"] }, "completedStepIds": [] }`
- **Response Body:** The program tree annotated with real-time `status` (unlocked, blocked, complete).
- **Status:** 200 OK (or 400 Bad Request, 404 Not Found).
```json
{
  "rootNode": {
    "id": "4eec3b5e-04f8-4a92-be20-569611db844e",
    "name": "Root",
    "nodeType": "group",
    "status": "unlocked",
    "blockedReason": null,
    "children": [
      {
        "id": "e14b8a24-9b57-45eb-89da-2244243b9ce7",
        "name": "Final Capstone",
        "nodeType": "step",
        "status": "blocked",
        "blockedReason": "Blocked: prerequisite 'Major' not yet complete.",
        "children": []
      }
    ]
  }
}
```

## Validation Logic Explained
The API runs two levels of validation:
- **Impossible Prerequisites (Strictly Rejected):** Identifies cycles, self-references, or forward-references (e.g., trying to require a node that appears *after* the current node in a pre-order traversal). Programs with impossible prerequisites are completely rejected by the `isValid: false` flag.
- **Reachability Warnings (Advisory):** Identifies soft logic flaws. For example, if the `Final Capstone` requires `AI Capstone` (which is nested deep inside a specific `Major` choice branch), participants who choose the Web branch will be permanently locked out of the Final Capstone. This produces a *Reachability Warning*. Conversely, if `Final Capstone` simply requires the parent `Major` choice group itself, this is universally safe. The algorithm intelligently excludes siblings in the same branch (e.g., `AI Capstone` requiring `Electives` within the same AI branch correctly generates no warning).

## Frontend Guide
The Angular SPA provides a dynamic UI to visually interact with the backend API. 
- **Program Builder**: Build deep program trees visually using drag-and-drop principles and form controls. It includes a convenient "Load CS Example" button to quickly scaffold a complex curriculum.
- **Program Viewer & Validator**: Read-only display of a created program, accompanied by a validation panel that executes the `/validate` endpoint and highlights Reachability Warnings or Impossible Prerequisites. 

**Demo Walkthrough:**
1. Navigate to `http://localhost:4200`.
2. Click "Load CS Example", then "Create Program". 
3. The UI will jump to the Viewer. Click the "Validate Program" button. The panel should report a clean bill of health.
4. Go back to the Builder, modify the Final Capstone's prerequisite to point directly at "AI Capstone" instead of "Major", and save.
5. Re-run validation. You will now see a reachability warning appear!

*(Note: The frontend features a "Known Programs" sidebar search. This is a client-side UX convenience powered by `localStorage` to save you from copying and pasting GUIDs; the backend remains strictly ID-based).*

## AI Tool Usage
Antigravity (an AI coding agent) was used to implement both the backend and frontend from a structured set of story-by-story prompts covering the domain model, API endpoints, prerequisite/reachability validators, tests, and UI. A running `PROJECT_CONTEXT.md` file was continuously maintained to ensure implementation decisions and architectural constraints remained consistent across AI sessions.

## Design Decisions & Assumptions
- **Temporary Keys for Resolution:** Because GUIDs are generated exclusively by the server during creation, clients assign a temporary string `key` to nodes in the `POST /programs` payload. References (`prerequisiteRef`) are resolved against these keys by the mapper to establish structural links.
- **LCA (Lowest Common Ancestor) Validation:** Naively flagging any prerequisite inside a Choice branch causes false positives (e.g., a node requiring its immediate preceding sibling). The ReachabilityValidator uses an LCA walk-up algorithm to check if the prerequisite source and target strictly share the same path.
- **In-Memory Storage:** The challenge uses a lightweight, dependency-free in-memory repository to minimize setup friction. In a production environment, this would be swapped out for EF Core (SQL Server / PostgreSQL).
- **Future Enhancements:** If extended, this platform could support:
  1. *Program Versioning*: Creating immutable snapshots of a program when participants are actively enrolled.
  2. *Audit Trail*: Tracking the modification history of the program structure.
  3. *Participant Simulation Mode*: Expanding the simulation API into an interactive frontend view where designers can "play-test" the tree by actively clicking through choices.

## Known Limitations
- No Authentication or Authorization.
- Data is stored strictly in memory and will be lost if the API restarts.
- The "Known Programs" search is a client-side local cache; there is no backend search index.
- The simulate endpoint expects the user to correctly provide Choice branch selections.

## Screenshots

![Program Builder]
<img width="1073" height="913" alt="image" src="https://github.com/user-attachments/assets/5b9e81ee-189f-4816-9c9e-cc12392e855b" />

*Visual program tree builder.*

![Program Viewer]
<img width="1292" height="909" alt="image" src="https://github.com/user-attachments/assets/5188f3d5-9d5c-485e-860f-2944e7599b14" />

*Program tree viewer and simulation UI.*

![Validation Results]
<img width="1317" height="329" alt="image" src="https://github.com/user-attachments/assets/fd500135-31a7-44b5-a905-45fcc2008f75" />
*Reachability warning detection.*

## Postman Collection

We have provided a ready-to-use Postman Collection that maps directly to the challenge's required test scenarios and endpoints.

**1. Import Files into Postman:**
- Import `ProgramDesigner.postman_collection.json`
- Import `ProgramDesigner.postman_environment.json`

**2. Configure Environment:**
- Select the **ProgramDesigner Local** environment in the top right corner of Postman.
- The `baseUrl` variable defaults to `http://localhost:5173`. If your dev server uses a different port, update the variable in the environment settings.

**3. Run the Collection:**
- **Folder 1 (Core Endpoints):** Demonstrates realistic happy-path usage. Run the Create Program request first, which automatically saves the returned ID into a `programId` environment variable for the subsequent GET, Validate, and Simulate requests.
- **Folder 2 (Required Challenge Scenarios):** Maps 1:1 to the 4 required test cases from the challenge specification, plus an explicit LCA edge case. Each request is fully self-contained (creating its own program via a pre-request script) and includes automated tests. A reviewer can click **Run Collection** on this folder to instantly verify that all requirements are met without reading code.
- **Folder 3 (Error Handling):** Verifies that invalid inputs and missing IDs correctly return 400 and 404 status codes.
