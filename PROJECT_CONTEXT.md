# Project Context - ProgramDesigner (octo education challenge)

> This file is the memory for AI coding sessions on this project. At the start of every story prompt, read this file first. At the end of every story, update it. Replace stale sections so this stays a snapshot of where things actually are, not a log.

## 1. What this project is

ProgramDesigner is an education-management tool composed of a .NET 10 Web API backend and an Angular 19 SPA frontend. Designers compose a Program as a recursive tree of nodes: `StepNode` is a leaf representing one concrete participant activity such as attending a session, passing a test, or submitting work; `GroupNode` is a container holding Steps and/or other Groups. A `GroupNode` applies either an `InOrder` rule, where every child must be completed in sequence, or a `Choice` rule, where the participant picks N out of M children. Any node can declare a `PrerequisiteId`, a reference to another node's Id, that gates access to that node and its subtree. The frontend provides a UI to build, view, validate, and simulate these program structures.

## 2. Solution structure

```text
ProgramDesigner/
|-- ProgramDesigner.slnx
|-- README.md
|-- PROJECT_CONTEXT.md
|-- src/
|   |-- ProgramDesigner.Api/
|   |   |-- Controllers/ProgramsController.cs
|   |   |-- Dto/
|   |   |-- Mapping/ProgramMapper.cs
|   |   |-- Program.cs
|   |   |-- appsettings.Development.json  ← Cors:AllowedOrigins config
|   |   `-- ProgramDesigner.Api.csproj
|   `-- ProgramDesigner.Core/
|       |-- Domain/
|       |-- Repositories/
|       |-- Services/
|       |-- Validators/
|       `-- ProgramDesigner.Core.csproj
|-- tests/
|   `-- ProgramDesigner.Tests/
|       |-- Domain/
|       |-- Integration/
|       |-- Validators/
|       |-- RequiredScenariosTests.cs
|       `-- ProgramDesigner.Tests.csproj
`-- frontend/                             ← Angular 19 SPA (Story 10)
    |-- src/
    |   |-- app/
    |   |   |-- core/
    |   |   |   |-- api.models.ts          ← TypeScript interfaces mirroring backend DTOs
    |   |   |   `-- program-api.service.ts ← Typed HttpClient service
    |   |   |-- builder/
    |   |   |   |-- builder.model.ts        ← Client-side form tree model (separate from API DTOs)
    |   |   |   |-- builder-page.component.ts ← Program builder UI (Story 11)
    |   |   |   `-- node-editor.component.ts  ← Recursive node editor sub-component
    |   |   |-- viewer/
    |   |   |   `-- viewer-page.component.ts  ← Read-only program tree viewer (Story 11)
    |   |   |-- app.component.*            ← Shell: header + router-outlet + nav
    |   |   |-- app.config.ts              ← providers: HttpClient, Router
    |   |   `-- app.routes.ts              ← Routes: / → /builder, /programs/:id
    |   |-- environments/
    |   |   |-- environment.ts             ← Production placeholder
    |   |   `-- environment.development.ts ← apiBaseUrl = http://localhost:5173
    |   `-- styles.css
    |-- angular.json
    |-- package.json
    `-- README.md
```

Project references:
- `ProgramDesigner.Api` -> `ProgramDesigner.Core`
- `ProgramDesigner.Tests` -> `ProgramDesigner.Core` and `ProgramDesigner.Api`

## 3. Domain model - key decisions

### Type names

| Concept | C# type name | Namespace |
|---|---|---|
| Base node | `ProgramNode` | `ProgramDesigner.Core.Domain` |
| Leaf node | `StepNode` | `ProgramDesigner.Core.Domain` |
| Container node | `GroupNode` | `ProgramDesigner.Core.Domain` |
| Aggregate root | `EducationProgram` | `ProgramDesigner.Core.Domain` |
| Node kind enum | `NodeType` | `ProgramDesigner.Core.Domain` |
| Group rule enum | `GroupRule` | `ProgramDesigner.Core.Domain` |
| Simulation status enum | `SimulationStatus` | `ProgramDesigner.Core.Domain` |

### Fields

**ProgramNode (abstract base class)**
- `Id` - `Guid`, `init`, defaults to `Guid.NewGuid()`
- `Name` - `required string`, `init`
- `NodeType` - `abstract NodeType`, `[JsonIgnore]`
- `PrerequisiteId` - `Guid?`, `init`, nullable

**StepNode : ProgramNode**
- `StepType` - `required string`, `init`; conventional values are `"session"`, `"test"`, and `"submission"`
- `NodeType` override -> `NodeType.Step`

**GroupNode : ProgramNode**
- `GroupRule` - required `GroupRule`
- `PickCount` - `int?`; only meaningful for `Choice`, must be 1..Children.Count
- `Children` - `List<ProgramNode>`, defaults to `[]`
- `NodeType` override -> `NodeType.Group`
- `ValidateInvariants()` throws on structural violations

**EducationProgram**
- `Id` - `Guid`, `init`, defaults to `Guid.NewGuid()`
- `Name` - `required string`
- `RootGroup` - required `GroupNode`
- `ValidateInvariants()` recursively validates all groups

### JSON and API conventions

- API DTOs use camelCase JSON.
- Create request child nodes use `type` as discriminator: `"step"` or `"group"`.
- Clients can provide `key` on any create-request node and refer to it through `prerequisiteRef`; the mapper resolves this to an internal `PrerequisiteId`.
- API responses include generated `id`, `prerequisiteId`, and `prerequisiteName` where applicable.
- The domain model itself uses System.Text.Json polymorphism with `nodeType`, but API create/response DTOs intentionally use `type` for external payloads.

### Design decisions

- Nodes are classes, not records, because identity is by `Id` and `GroupNode.Children` is a mutable tree list.
- Core has no ASP.NET dependency. Controllers, DTOs, and mapping live in `ProgramDesigner.Api`; domain, repositories, services, and validators live in `ProgramDesigner.Core`.
- Storage is an in-memory singleton repository.

## 4. API contract - endpoints implemented

- `POST /programs` - DONE.
  **Request**: JSON representing the program tree. Uses `type` (`"step"` or `"group"`) as the child-node discriminator. Clients can provide `key` on any node and use `prerequisiteRef` on other nodes.
  **Response**: 201 Created. Body is the mapped program tree with generated `id`s and resolved `prerequisiteId`s.
  **Validation**: Returns 400 Bad Request with an `Errors` list if duplicate keys exist, prerequisite refs cannot be resolved, or structural invariants fail.

- `GET /programs/{id}` - DONE.
  **Request**: None.
  **Response**: 200 OK. Body is the mapped program tree, including `prerequisiteName` where applicable.
  **Errors**: Returns 404 Not Found if the program ID does not exist.

- `POST /programs/{id}/validate` - DONE.
  **Request**: No body.
  **Response**: 200 OK with:
  ```json
  {
    "isValid": true,
    "impossiblePrerequisites": [],
    "reachabilityWarnings": []
  }
  ```
  `isValid` is `true` iff `impossiblePrerequisites` is empty. Reachability warnings are advisory only.
  **Errors**: Returns 404 Not Found if the program ID does not exist.

- `POST /programs/{id}/simulate` - DONE (optional bonus).
  **Request**: `{ "choices": { "<choiceGroupId>": ["<pickedChildId>", ...] }, "completedStepIds": ["<stepId>", ...] }`.
  **Response**: 200 OK with `{ "rootNode": ... }`, where every node in the tree has `id`, `name`, `nodeType`, `status` (`complete`, `unlocked`, or `blocked`), optional `blockedReason`, and recursive `children`.
  **Computation**: Steps are complete when their ID is in `completedStepIds`. Groups are complete when their rule is satisfied. Choice children not selected are blocked as not part of the chosen path; if a Choice group has no selection yet, all children are available to be picked. Nodes with incomplete prerequisites are blocked. InOrder children after an incomplete sibling are blocked by the previous node.
  **Errors**: Returns 400 Bad Request with an `Errors` list if `choices` references a missing node, a non-Choice node, or a child ID that is not a direct child of the referenced Choice group. Returns 404 Not Found if the program ID does not exist.

## 5. Validation logic - key decisions

**Impossible Prerequisites**
- `PrerequisiteValidator` walks the tree to assign each node a pre-order index and descendant set, then checks each `PrerequisiteId`.
- Reasons:
  - `SelfReference`: The node's prerequisite points to itself.
  - `DescendantReference`: The node's prerequisite points to one of its descendants.
  - `ForwardReference`: The prerequisite's pre-order index is greater than or equal to the dependent node's index.
- Direct prerequisite cycles are rejected because at least one side of the cycle must be a forward reference.

**Reachability Warnings**
- `ReachabilityValidator` maps parent-child relationships and checks each prerequisite target by walking up its ancestor chain.
- If the target is inside a `Choice` branch that the source does not share, the prerequisite may be unreachable for participants who choose another branch.
- These warnings do not make the program invalid.

## 6. Test scenarios covered so far

**Required challenge scenarios**: `tests/ProgramDesigner.Tests/RequiredScenariosTests.cs`

1. `FullComputerScienceScenario_ValidatesWithoutErrorsOrWarnings`
2. `DirectPrerequisiteCycle_IsRejected`
3. `PrerequisiteInUnchosenChoicePath_GeneratesWarningNotRejection`
4. `SelfReferencingPrerequisite_IsRejected`

**Domain model / JSON**: `tests/ProgramDesigner.Tests/Domain/ProgramTreeJsonTests.cs`

5. `RoundTrip_PreservesEntireProgramStructure`
6. `Serialise_EmitsNodeTypeDiscriminator`
7. `ValidateInvariants_DoesNotThrow_ForWellFormedProgram`
8. `ValidateInvariants_Throws_WhenChoicePickCountExceedsChildCount`
9. `ValidateInvariants_Throws_WhenChoiceGroupHasNullPickCount`
10. `ValidateInvariants_Throws_WhenInOrderGroupHasPickCount`

**API endpoints**: `tests/ProgramDesigner.Tests/Integration/ProgramsControllerTests.cs`

11. `PostProgram_ValidComputerScienceScenario_Returns201Created`
12. `GetProgram_ValidId_Returns200WithCorrectTree`
13. `GetProgram_InvalidId_Returns404NotFound`
14. `ValidateProgram_InvalidId_Returns404NotFound`

**Prerequisite validator regressions**: `tests/ProgramDesigner.Tests/Validators/PrerequisiteValidatorTests.cs`

15. `FindImpossiblePrerequisites_DescendantReference_ReturnsDescendantReferenceError`
16. `FindImpossiblePrerequisites_ForwardReference_ReturnsForwardReferenceError`
17. `FindImpossiblePrerequisites_ValidBackwardReference_ReturnsEmpty`

**Reachability validator regressions**: `tests/ProgramDesigner.Tests/Validators/ReachabilityValidatorTests.cs`

18. `FindReachabilityWarnings_TargetIsChoiceGroup_ReturnsEmpty`
19. `FindReachabilityWarnings_TargetInsideChoiceGroup_SourceOutside_ReturnsWarning`
20. `FindReachabilityWarnings_TargetInsideInOrderGroup_ReturnsEmpty`
21. `FindReachabilityWarnings_TargetInsideNestedChoice_SourceSharesOuterChoice_ReturnsWarningForInnerChoice`

**Simulation endpoint**: `tests/ProgramDesigner.Tests/Integration/ProgramSimulationTests.cs`

22. `SimulateProgram_ComputerScienceAiChoice_ReturnsExpectedProgressTree`
23. `SimulateProgram_ChoiceReferencesNonChild_Returns400BadRequest`

## 7. Open decisions / things later stories must respect

- **ID generation**: The server generates Guids for programs and nodes. Clients use temporary `key` values only during creation.
- **PrerequisiteRef resolution**: Clients send `prerequisiteRef` strings that map to another node's `key` in the same request payload.
- **JSON casing**: API is configured with camelCase globally for property names and enums.
- **No `nodeType` discriminator on RootGroup**: API responses use the DTO model; child nodes include `type` for polymorphic API responses, while simulation responses use `nodeType`.
- **Solution format**: `.slnx`, not `.sln`. Use `ProgramDesigner.slnx` in `dotnet` commands.
- **CORS**: The API uses a named `"DevFrontend"` policy. Allowed origins are read from `Cors:AllowedOrigins` in `appsettings.Development.json` (default: `["http://localhost:4200"]`). This is dev-only and must be tightened for production.
- **Frontend API base URL**: Configured in `frontend/src/environments/environment.development.ts` → `apiBaseUrl: 'http://localhost:5173'`. Must match the `applicationUrl` in `launchSettings.json`.

## 8. Status

- [x] Story 1 - Domain Model (6/6 tests pass, 0 build warnings)
- [x] Story 2 - POST /programs
- [x] Story 3 - GET /programs/:id
- [x] Story 4 - Impossible Prerequisite Detection
- [x] Story 5 - Reachability Warning Detection
- [x] Story 6 - Validate Endpoint (22/22 tests pass at completion of Story 6)
- [x] Story 7 - Test Suite Consolidation (21/21 tests pass)
- [x] Story 8 - README & Documentation (README added, 21/21 tests pass)
- [x] Story 9 - Optional Simulate Endpoint (23/23 tests pass)
- [x] Story 10 - Frontend Scaffold, CORS & API Client ✅ (Angular 19, builds clean, `ProgramApiService` typed against real DTOs)
- [x] Story 11 - Program Builder & Viewer UI ✅ (BuilderPageComponent + NodeEditorComponent + ViewerPageComponent, builds clean, 23/23 .NET tests still pass)
- [x] Story 12 - Validation Results UI ✅ (Validate button, Simulation panel, issues display)
