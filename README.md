# ProgramDesigner

## 1. Overview
ProgramDesigner is an education-management tool designed for education program designers. It allows users to build, view, validate, and simulate complex, recursive program structures. Programs consist of sequential or choice-based groups of steps (like sessions, tests, or submissions), and include prerequisite definitions to map out the required learning paths. 

## 2. Setup & Run Instructions

### Prerequisites
- **.NET SDK**: `10.0` or higher
- **Node.js**: `v18` or `v20` (LTS)
- **Angular CLI**: `19.2+` (or you can use the locally installed version via `npm`)

### Running the API Backend
From the repository root, restore dependencies and start the API:
```bash
# Restore solution packages
dotnet restore ProgramDesigner.slnx

# Run the API
dotnet run --project src/ProgramDesigner.Api/ProgramDesigner.Api.csproj
```
The API will run on `http://localhost:5173` (as configured in `launchSettings.json`).

### Running the Test Suite
To verify the application behaves correctly, run the full test suite from the repository root:
```bash
dotnet test ProgramDesigner.slnx
```
**Expected output:** 23 tests should pass, covering the domain model, validators, API integration, and required scenarios.

### Running the Frontend
The Angular SPA is located in the `frontend` directory. It is pre-configured in `environment.development.ts` to reach the API at `http://localhost:5173`.
```bash
cd frontend
npm install
npm start
```
The development server will open the application at `http://localhost:4200`. Cross-Origin Resource Sharing (CORS) is configured on the backend to allow this origin during development.

### API Testing with Postman
A pre-configured Postman collection is included in the repository root for testing the API independently of the frontend:
- Import `ProgramDesigner.postman_collection.json` and `ProgramDesigner.postman_environment.json` into Postman.
- Ensure the **Local** environment is active so the `{{baseUrl}}` variable correctly points to `http://localhost:5173`.
- The collection contains pre-built requests for creating programs (including the Computer Science scenario), viewing, validating, and simulating them.

## 3. Data Model Explanation
The domain model uses a recursive composite pattern built around `ProgramNode`. A `StepNode` represents a concrete leaf activity (e.g., a "session" or "test"), while a `GroupNode` acts as a container applying a specific rule: either `InOrder` (all children must be completed sequentially) or `Choice` (pick N children). 

Because any node can be a prerequisite for any other node, the model uses `NodeId` references. To support polymorphism in API requests, JSON payloads use a discriminator field (`"type": "group"` or `"type": "step"`). During program creation, clients use temporary `key` properties to define internal references (`prerequisiteRef`), which the backend resolves into strictly typed `NodeId` GUIDs.

**Example nested structure:**
```json
{
  "name": "Computer Science Degree",
  "rootGroup": {
    "type": "group",
    "key": "root",
    "groupRule": "InOrder",
    "children": [
      {
        "type": "step",
        "key": "major",
        "name": "Major Core",
        "stepType": "session"
      },
      {
        "type": "group",
        "key": "specialization",
        "name": "Specialization Track",
        "groupRule": "Choice",
        "pickCount": 1,
        "children": [
          { "type": "step", "key": "ai", "name": "AI Capstone", "stepType": "submission" },
          { "type": "step", "key": "electives", "name": "Electives", "stepType": "session" }
        ]
      },
      {
        "type": "step",
        "key": "final",
        "name": "Final Capstone",
        "stepType": "submission",
        "prerequisiteRef": "major"
      }
    ]
  }
}
```

## 4. API Contract

### `POST /programs`
Creates a new program, resolving temporary keys to generated GUIDs.
**Request:**
```json
{
  "name": "CS Program",
  "rootGroup": {
    "type": "group",
    "key": "g1",
    "groupRule": "InOrder",
    "children": [
      { "type": "step", "key": "s1", "name": "Major", "stepType": "session" },
      { "type": "step", "key": "s2", "name": "Final Capstone", "stepType": "submission", "prerequisiteRef": "s1" }
    ]
  }
}
```
**Response (201 Created):**
```json
{
  "id": "e44d32a0-4f51-40c2-901c-7fc8db5d0b98",
  "name": "CS Program",
  "rootGroup": {
    "id": "18f971c2-559d-472e-bce1-fbc387063c1a",
    "name": "",
    "type": "group",
    "groupRule": "InOrder",
    "children": [
      { "id": "40df0353-83eb-46f9-ad6a-54320db9e27c", "name": "Major", "type": "step", "stepType": "session" },
      { "id": "2f42be23-5e92-4f32-8419-f9c322b7a374", "name": "Final Capstone", "type": "step", "stepType": "submission", "prerequisiteId": "40df0353-83eb-46f9-ad6a-54320db9e27c", "prerequisiteName": "Major" }
    ]
  }
}
```

### `GET /programs/{id}`
Retrieves a saved program by ID.
**Response (200 OK):** Identical structure to the `POST` response. (Returns `404 Not Found` if ID does not exist).

### `POST /programs/{id}/validate`
Evaluates the structural integrity and reachability of prerequisites.
**Response (200 OK):**
```json
{
  "isValid": true,
  "impossiblePrerequisites": [],
  "reachabilityWarnings": []
}
```

### `POST /programs/{id}/simulate`
Simulates a participant's progress given their chosen paths and completed steps.
**Request:**
```json
{
  "choices": {
    "0c1c8a14-8a43-455b-80df-561b369622d9": ["a1b2c3d4-e5f6-7890-1234-567890abcdef"]
  },
  "completedStepIds": ["40df0353-83eb-46f9-ad6a-54320db9e27c"]
}
```
**Response (200 OK):**
```json
{
  "rootNode": {
    "id": "18f971c2-559d-472e-bce1-fbc387063c1a",
    "name": "",
    "nodeType": "Group",
    "status": "Unlocked",
    "children": [
      { "id": "40df0353-83eb-46f9-ad6a-54320db9e27c", "name": "Major", "nodeType": "Step", "status": "Complete" }
    ]
  }
}
```

## 5. Validation Logic Explained

The backend enforces two distinct classes of prerequisite validation:

**Impossible Prerequisites (Strict Errors)**
These render a program invalid (`isValid: false`) and reject creation. Examples include self-references, dependency cycles, and forward-references (where a node requires a prerequisite that appears structurally later in the tree).

**Reachability Warnings (Advisory)**
These flag paths that *might* become dead ends depending on a participant's choices. These warnings do **not** invalidate the program, as they represent risk, not structural impossibility.
- **Safe Example**: `Final Capstone` requires `Major`. Because both are in the primary sequence, this is completely safe.
- **Safe Example**: `AI Capstone` requires `Electives`, and both live inside the *same* `Choice` branch. Since selecting the branch includes both, this is perfectly fine and generates no warnings.
- **Risky Example**: `Final Capstone` (a required global node) requires `AI Capstone` (an optional choice node). If the student selects `Electives` instead of `AI Capstone`, they will forever be locked out of the `Final Capstone`. The validator will issue a reachability warning.

## 6. Frontend Application
The Angular SPA provides a visual interface for creating and reviewing programs.
- **Builder View**: Visually constructs nested node trees using a dynamic form, mapping directly to the API's creation schema.
- **Viewer & Simulator**: Renders saved programs as an interactive tree.
- **Walkthrough Demo**: 
  1. Go to the Builder page and load the "Computer Science" template.
  2. Create the program. The viewer page will load automatically.
  3. Click **Validate**. The response will show no errors and no warnings.
  4. Modify the `Final Capstone` node so its prerequisite points to `AI Capstone` instead of `Major Core`.
  5. Save and click **Validate** again. A reachability warning will now clearly display, warning that the Capstone depends on an optional choice.
- **Search Box**: The UI includes a convenient name-search bar in the header. Note that this feature is client-side only (caching program IDs in `localStorage`) as a UX enhancement; the actual API remains exclusively GUID-based.

## 7. AI Tool Usage
Antigravity (an AI coding agent) was utilized to construct both the backend and frontend. Development followed a structured, story-by-story sequence covering the core domain model, API endpoints, the complex prerequisite/reachability validators, extensive unit testing, and the Angular UI. To maintain strict architectural consistency across disjointed sessions, an internal `PROJECT_CONTEXT.md` document served as persistent memory for the AI.

## 8. Assumptions Made Beyond the Spec
Because the challenge required concrete implementation decisions, the following assumptions were made:
- **Prerequisite Resolution**: Clients supply temporary string `key` and `prerequisiteRef` properties. The server converts these into permanent `NodeId` references during creation, avoiding the need for clients to guess GUIDs.
- **`isValid` Definition**: A program is only deemed invalid if it contains impossible prerequisites. Reachability warnings are considered advisory only, per spec guidelines.
- **Forward-Reference Rules**: "Appearing later" is formally defined mathematically using a node's pre-order traversal index in the tree structure.
- **LCA-Exclusion Reachability**: A prerequisite located inside a `Choice` branch is only flagged as risky if the dependent node does *not* share that same branch. Without this "Lowest Common Ancestor" logic, the CS example's `AI Capstone -> Electives` valid path would trigger false positives.
- **In-Memory Storage**: The repository pattern uses a singleton dictionary. Data will reset upon API restart. This scopes the take-home to domain logic rather than database management.
- **No Authorization**: The API is completely open, devoid of authentication middleware or ownership concepts.
- **Client-side Search**: The name-search feature was intentionally kept strictly in the frontend cache to prevent scope creep on the backend API requirements.

## 9. Business Improvements & Extension Ideas
To transition this concept into a production-grade platform, several extensions would be vital:
- **Program Versioning**: Programs should become immutable once participants enroll. Editing a live program could corrupt progress tracking; updates should require publishing a new version.
- **"Preview as Participant"**: Adding a dedicated sandbox mode to let designers run through their own program layout as a student, seeing exactly when steps unlock.
- **Analytics Engine**: Tracking aggregate completion rates to identify where students actually stall. This would contextualize reachability warnings with real-world dropout data.
- **Persistent Storage & Multi-Tenancy**: Replacing the in-memory cache with an EF Core relational database (e.g., PostgreSQL/SQL Server) with tenant segregation, allowing multiple institutions to manage independent catalogs.
- **Draft States**: Permitting programs to be saved incrementally in a "Draft" status before validating and "Publishing", removing the friction of needing a perfectly valid tree on the first save.

## 10. Known Limitations
- **Data Persistence**: All programs are lost when the API process stops.
- **Security**: No authentication or authorization is implemented.
- **Search**: Searching by program name works entirely via frontend local storage; if a program is created via Postman, the UI search bar won't "know" about it.

## 11. Screenshots

### Program Builder
![Program Builder](docs/screenshots/builder.png)

### Viewer & Simulation
![Program Viewer](docs/screenshots/viewer.png)

### Validation Warnings
![Validation Warnings](docs/screenshots/validation.png)
