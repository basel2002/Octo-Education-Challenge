/**
 * Internal form-model types used only by the builder.
 * These are separate from the API DTOs — they hold UI state like
 * client-side keys, derived flat lists for prerequisite dropdowns, etc.
 */
export type FormNodeType = 'step' | 'group';
export type FormGroupRule = 'inOrder' | 'choice';

let _keyCounter = 1;
export function nextKey(): string {
  return `node_${_keyCounter++}`;
}

export interface FormNode {
  key: string;
  type: FormNodeType;
  name: string;
  stepType: string;       // only used when type === 'step'
  groupRule: FormGroupRule; // only used when type === 'group'
  pickCount: number;      // only used when type === 'group' && rule === 'choice'
  prerequisiteRef: string; // '' means none
  children: FormNode[];   // only used when type === 'group'
}

export function createStepNode(): FormNode {
  return {
    key: nextKey(),
    type: 'step',
    name: '',
    stepType: 'session',
    groupRule: 'inOrder',
    pickCount: 1,
    prerequisiteRef: '',
    children: [],
  };
}

export function createGroupNode(): FormNode {
  return {
    key: nextKey(),
    type: 'group',
    name: '',
    stepType: '',
    groupRule: 'inOrder',
    pickCount: 1,
    prerequisiteRef: '',
    children: [],
  };
}

/** Collect all nodes in pre-order (for the prerequisite dropdown). */
export function collectAll(node: FormNode): FormNode[] {
  return [node, ...node.children.flatMap(collectAll)];
}
