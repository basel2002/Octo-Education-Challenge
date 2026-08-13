# ProgramDesigner

## Overview

ProgramDesigner is a .NET 10 Web API for modeling education programs as a recursive tree of steps and groups. A program can contain concrete activities such as sessions, tests, and submissions, plus nested groups that require either ordered completion or a choice among options. Nodes can depend on other nodes through prerequisites. The validation endpoint catches impossible prerequisites such as self-references and cycles, and also warns when a prerequisite may be unreachable because it lives inside a choice branch the participant might not pick.

## Setup Instructions

Prerequisite: install the .NET 10 SDK.

```powershell
git clone <repo-url>
cd ProgramDesigner
dotnet restore ProgramDesigner.slnx
dotnet build ProgramDesigner.slnx
dotnet run --project src\ProgramDesigner.Api\ProgramDesigner.Api.csproj --launch-profile http
```

The API runs at:

```text
http://localhost:5173
```

Run the test suite from the repository root:

```powershell
dotnet test ProgramDesigner.slnx
```

## Data Model

The core model is generic and recursive, so it can represent any program tree, not just the Computer Science example.

- `EducationProgram` is the aggregate root. It has an `id`, a `name`, and one `rootGroup`.
- `ProgramNode` is the shared base concept for anything in the tree.
- `StepNode` is a leaf activity with a free-form `stepType`, such as `session`, `test`, or `submission`.
- `GroupNode` contains child nodes and has a `groupRule`.
- `GroupRule.InOrder` means all children are completed in sequence.
- `GroupRule.Choice` means the participant picks `pickCount` children out of the available options.
- `prerequisiteId` is the stored Guid reference to another node. In create requests, clients use readable `key` and `prerequisiteRef` values, and the API resolves them to generated Guids.

Short nested create example:

```json
{
  "name": "Example Program",
  "rootGroup": {
    "name": "Root",
    "groupRule": "inOrder",
    "children": [
      {
        "type": "step",
        "key": "intro",
        "name": "Introduction",
        "stepType": "session"
      },
      {
        "type": "group",
        "name": "Track",
        "groupRule": "choice",
        "pickCount": 1,
        "prerequisiteRef": "intro",
        "children": [
          {
            "type": "step",
            "name": "AI Basics",
            "stepType": "session"
          },
          {
            "type": "step",
            "name": "IT Basics",
            "stepType": "session"
          }
        ]
      }
    ]
  }
}
```

## API Contract

### POST `/programs`

Creates and stores a program in memory.

Request:

```json
{
  "name": "Computer Science",
  "rootGroup": {
    "name": "Computer Science",
    "groupRule": "inOrder",
    "children": [
      {
        "type": "group",
        "key": "Foundations",
        "name": "Foundations",
        "groupRule": "inOrder",
        "children": [
          { "type": "step", "name": "Introduction to Computing", "stepType": "session" },
          { "type": "step", "name": "Mathematics for Computing", "stepType": "session" }
        ]
      },
      {
        "type": "group",
        "key": "Major",
        "name": "Major",
        "groupRule": "choice",
        "pickCount": 1,
        "prerequisiteRef": "Foundations",
        "children": [
          { "type": "step", "name": "AI", "stepType": "session" },
          { "type": "step", "name": "IT", "stepType": "session" },
          { "type": "step", "name": "Programming", "stepType": "session" }
        ]
      },
      {
        "type": "step",
        "name": "Final Capstone",
        "stepType": "submission",
        "prerequisiteRef": "Major"
      }
    ]
  }
}
```

Abbreviated response `201 Created`:

```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "name": "Computer Science",
  "rootGroup": {
    "id": "22222222-2222-2222-2222-222222222222",
    "name": "Computer Science",
    "prerequisiteId": null,
    "prerequisiteName": null,
    "groupRule": "inOrder",
    "pickCount": null,
    "children": [
      {
        "type": "group",
        "id": "33333333-3333-3333-3333-333333333333",
        "name": "Foundations",
        "prerequisiteId": null,
        "prerequisiteName": null,
        "groupRule": "inOrder",
        "pickCount": null,
        "children": []
      },
      {
        "type": "group",
        "id": "44444444-4444-4444-4444-444444444444",
        "name": "Major",
        "prerequisiteId": "33333333-3333-3333-3333-333333333333",
        "prerequisiteName": "Foundations",
        "groupRule": "choice",
        "pickCount": 1,
        "children": []
      }
    ]
  }
}
```

Status codes:

- `201 Created` when the program is stored.
- `400 Bad Request` when keys are duplicated, a `prerequisiteRef` cannot be resolved, or structural invariants fail, such as a choice group without `pickCount`.

### GET `/programs/{id}`

Returns a stored program by ID.

Response `200 OK`:

```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "name": "Computer Science",
  "rootGroup": {
    "id": "22222222-2222-2222-2222-222222222222",
    "name": "Computer Science",
    "prerequisiteId": null,
    "prerequisiteName": null,
    "groupRule": "inOrder",
    "pickCount": null,
    "children": []
  }
}
```

Status codes:

- `200 OK` when found.
- `404 Not Found` when no program exists for the supplied ID.

### POST `/programs/{id}/validate`

Validates a stored program. The request has no body.

Response `200 OK`:

```json
{
  "isValid": false,
  "impossiblePrerequisites": [
    {
      "nodeId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "nodeName": "A",
      "prerequisiteId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      "prerequisiteName": "B",
      "reason": "forwardReference",
      "description": "Node 'A' cannot depend on 'B' because 'B' appears later or alongside it in the required completion order."
    }
  ],
  "reachabilityWarnings": []
}
```

A valid program with advisory warnings still returns `isValid: true`:

```json
{
  "isValid": true,
  "impossiblePrerequisites": [],
  "reachabilityWarnings": [
    {
      "nodeId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "nodeName": "Final Capstone",
      "prerequisiteId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      "prerequisiteName": "AI Capstone",
      "riskyChoiceGroupId": "cccccccc-cccc-cccc-cccc-cccccccccccc",
      "riskyChoiceGroupName": "Major",
      "description": "The prerequisite on 'AI Capstone' is only guaranteed if the participant picks the specific option under the 'Major' choice group. Participants who choose other options can never satisfy it."
    }
  ]
}
```

Status codes:

- `200 OK` when validation runs.
- `404 Not Found` when no program exists for the supplied ID.

### POST `/programs/{id}/simulate` Optional Bonus

Computes one participant's current progress state from their selected choice paths and completed steps.

Request:

```json
{
  "choices": {
    "44444444-4444-4444-4444-444444444444": [
      "55555555-5555-5555-5555-555555555555"
    ]
  },
  "completedStepIds": [
    "66666666-6666-6666-6666-666666666666",
    "77777777-7777-7777-7777-777777777777"
  ]
}
```

`choices` maps a Choice group ID to the child node IDs the participant picked. If a Choice group has no entry yet, all of its children are treated as available to be picked. `completedStepIds` contains the Step IDs the participant has already finished.

Abbreviated response `200 OK`:

```json
{
  "rootNode": {
    "id": "22222222-2222-2222-2222-222222222222",
    "name": "Computer Science",
    "nodeType": "group",
    "status": "unlocked",
    "blockedReason": null,
    "children": [
      {
        "id": "33333333-3333-3333-3333-333333333333",
        "name": "Foundations",
        "nodeType": "group",
        "status": "complete",
        "blockedReason": null,
        "children": []
      },
      {
        "id": "88888888-8888-8888-8888-888888888888",
        "name": "IT",
        "nodeType": "group",
        "status": "blocked",
        "blockedReason": "Not part of the chosen path.",
        "children": []
      },
      {
        "id": "99999999-9999-9999-9999-999999999999",
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

Status codes:

- `200 OK` when simulation runs.
- `400 Bad Request` when `choices` references a missing Choice group, a non-Choice node, or a child ID that does not belong to that Choice group.
- `404 Not Found` when no program exists for the supplied ID.

## Validation Logic

Impossible prerequisites are hard validation errors. A node cannot depend on itself, cannot depend on one of its own descendants, and cannot depend on a node that appears later or alongside it in the required traversal order. These cases make the program impossible to complete reliably, so validation returns `isValid: false` and includes entries in `impossiblePrerequisites`.

A direct cycle such as `A` depends on `B` and `B` depends on `A` is rejected by the same ordering logic: one side of the pair must point forward. Self-reference is reported explicitly as `selfReference`.

Reachability warnings are advisory. They happen when a prerequisite target exists, and is not structurally impossible, but sits inside a `choice` branch the participant might skip. For example, `Final Capstone -> Major` is safe because completing the chosen major option satisfies the `Major` group. `Final Capstone -> AI Capstone` is risky because a participant who chooses IT or Programming can never complete the AI-only capstone, so the program remains valid but returns a warning.

## AI Tool Usage Note

Antigravity, an AI coding agent, was used to generate the implementation from structured prompts. It was used for project scaffolding, endpoint implementation, validator logic, and test coverage. The resulting code and tests are checked into this repository as normal source files.

## Known Limitations / What I Would Do With More Time

- Storage is in memory, so programs disappear when the API process restarts.
- There is no authentication, authorization, or multi-tenant separation.
- The simulation endpoint computes current state, but it does not persist participant progress.
- Validation returns useful reasons, but there is no dedicated problem-details schema beyond the current response DTOs.
- The API has no persistence migrations, observability, or production deployment configuration.
