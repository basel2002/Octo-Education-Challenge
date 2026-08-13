// ---------------------------------------------------------------------------
// API DTO interfaces — field names match the camelCase JSON the API serializes.
// Keep these in sync with the backend response shapes documented in PROJECT_CONTEXT.md.
// ---------------------------------------------------------------------------

// ---- Program tree ----

export type NodeType = 'step' | 'group';
export type GroupRule = 'inOrder' | 'choice';

export interface ProgramNodeResponse {
  id: string;
  name: string;
  /** Discriminator emitted on every item inside a `children` array. */
  type: NodeType;
  prerequisiteId: string | null;
  prerequisiteName: string | null;
}

export interface StepNodeResponse extends ProgramNodeResponse {
  type: 'step';
  stepType: string;
}

export interface GroupNodeResponse extends ProgramNodeResponse {
  type: 'group';
  groupRule: GroupRule;
  pickCount: number | null;
  children: ProgramNodeResponse[];
}

/** Top-level response from POST /programs and GET /programs/{id}. */
export interface ProgramResponse {
  id: string;
  name: string;
  /** rootGroup is always a GroupNode but has no 'type' discriminator at the top level. */
  rootGroup: GroupNodeResponse;
}

// ---- Validation ----

export type ImpossiblePrerequisiteReason = 'selfReference' | 'descendantReference' | 'forwardReference';

export interface ImpossiblePrerequisiteResponse {
  nodeId: string;
  nodeName: string;
  prerequisiteId: string;
  prerequisiteName: string;
  reason: ImpossiblePrerequisiteReason;
  description: string;
}

export interface ReachabilityWarningResponse {
  nodeId: string;
  nodeName: string;
  prerequisiteId: string;
  prerequisiteName: string;
  riskyChoiceGroupId: string;
  riskyChoiceGroupName: string;
  description: string;
}

export interface ValidationResultResponse {
  isValid: boolean;
  impossiblePrerequisites: ImpossiblePrerequisiteResponse[];
  reachabilityWarnings: ReachabilityWarningResponse[];
}

// ---- Simulation ----

export type SimulationStatus = 'complete' | 'unlocked' | 'blocked';

export interface SimulationNodeResult {
  id: string;
  name: string;
  nodeType: NodeType;
  status: SimulationStatus;
  blockedReason: string | null;
  children: SimulationNodeResult[];
}

export interface SimulationResponse {
  rootNode: SimulationNodeResult;
}

/** Request body for POST /programs/{id}/simulate. */
export interface SimulationRequest {
  /** Maps Choice group IDs to the array of selected child IDs. */
  choices: Record<string, string[]>;
  /** IDs of steps the participant has already completed. */
  completedStepIds: string[];
}

// ---- Create request ----

export type CreateNodeRequest = CreateStepNodeRequest | CreateGroupNodeRequest;

export interface CreateStepNodeRequest {
  type: 'step';
  /** Temporary client-only key; the server generates the real Guid. */
  key?: string;
  name: string;
  stepType: string;
  prerequisiteRef?: string;
}

export interface CreateGroupNodeRequest {
  type: 'group';
  key?: string;
  name: string;
  groupRule: GroupRule;
  pickCount?: number;
  prerequisiteRef?: string;
  children?: CreateNodeRequest[];
}

export interface CreateProgramRequest {
  name: string;
  rootGroup: Omit<CreateGroupNodeRequest, 'type'>;
}
