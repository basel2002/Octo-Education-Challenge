import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ProgramApiService } from '../core/program-api.service';
import { 
  ProgramResponse, 
  ProgramNodeResponse, 
  GroupNodeResponse,
  StepNodeResponse,
  ValidationResultResponse,
  SimulationRequest,
  SimulationNodeResult
} from '../core/api.models';
import { ProgramHistoryService } from '../core/program-history.service';

@Component({
  selector: 'app-viewer-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="page">

      <!-- Header + navigation -->
      <div class="page-header">
        <h2>Program Viewer</h2>
        <a class="btn-secondary" routerLink="/builder">← New Program</a>
      </div>

      @if (loading()) {
        <div class="state-msg">Loading…</div>
      }

      @if (error()) {
        <div class="error-banner">{{ error() }}</div>
      }

      @if (program()) {
        <div class="actions-row">
           <button class="btn-primary" (click)="validate()" [disabled]="validating()">
             {{ validating() ? 'Validating...' : 'Validate Program' }}
           </button>
           <button class="btn-secondary" (click)="toggleSimulation()">
             {{ showSimulation() ? 'Hide Simulation' : 'Show Simulation' }}
           </button>
        </div>

        @if (validation()) {
          <div class="validation-panel" [class.is-valid]="validation()!.isValid" [class.is-invalid]="!validation()!.isValid">
            <h3>Validation Result: {{ validation()!.isValid ? 'Valid' : 'Invalid' }}</h3>
            
            @if (validation()!.impossiblePrerequisites.length === 0 && validation()!.reachabilityWarnings.length === 0) {
              <div class="success-msg">✓ No issues found. The program structure is completely valid.</div>
            }

            @if (validation()!.impossiblePrerequisites.length > 0) {
              <div class="issues-list errors-list">
                <h4>Impossible Prerequisites (Blocks Program)</h4>
                <ul>
                  @for (err of validation()!.impossiblePrerequisites; track err.nodeId) {
                    <li>
                      <strong>{{ err.nodeName }}</strong> cannot depend on <strong>{{ err.prerequisiteName }}</strong>: 
                      {{ err.description }}
                    </li>
                  }
                </ul>
              </div>
            }

            @if (validation()!.reachabilityWarnings.length > 0) {
              <div class="issues-list warnings-list">
                <h4>Reachability Warnings (Risky)</h4>
                <ul>
                  @for (warn of validation()!.reachabilityWarnings; track warn.nodeId) {
                    <li>
                      <strong>{{ warn.nodeName }}</strong> depending on <strong>{{ warn.prerequisiteName }}</strong> inside choice <strong>{{ warn.riskyChoiceGroupName }}</strong>: 
                      {{ warn.description }}
                    </li>
                  }
                </ul>
              </div>
            }
          </div>
        }

        <div class="layout-row">
          <div class="program-view">
            <div class="program-title">
              <span class="program-label">Program</span>
              <h3>{{ program()!.name }}</h3>
              <span class="program-id">{{ program()!.id }}</span>
            </div>
            <div class="tree-root">
              <ng-container *ngTemplateOutlet="nodeTree; context: { $implicit: asGroup(program()!.rootGroup), depth: 0 }"></ng-container>
            </div>
          </div>

          @if (showSimulation()) {
            <div class="simulation-panel">
              <h3>Simulation Panel</h3>
              <div class="sim-instructions">Check off steps and make choices, then simulate to see progress in the tree.</div>

              <div class="sim-section">
                <h4>Choices</h4>
                @if (allChoices().length === 0) { <div class="empty-msg">No choice groups in program.</div> }
                @for (choice of allChoices(); track choice.id) {
                  <div class="sim-choice-group">
                    <strong>{{ choice.name }}</strong> (pick {{ choice.pickCount }})
                    @for (child of choice.children; track child.id) {
                      <label class="sim-checkbox">
                        <input type="checkbox" 
                               [checked]="isChoiceSelected(choice.id, child.id)"
                               (change)="toggleChoice(choice.id, child.id, $event)">
                        {{ child.name }}
                      </label>
                    }
                  </div>
                }
              </div>

              <div class="sim-section">
                <h4>Completed Steps</h4>
                @if (allSteps().length === 0) { <div class="empty-msg">No steps in program.</div> }
                @for (step of allSteps(); track step.id) {
                  <label class="sim-checkbox">
                    <input type="checkbox" 
                           [checked]="completedSteps.has(step.id)"
                           (change)="toggleStep(step.id, $event)">
                    {{ step.name }}
                  </label>
                }
              </div>

              <button class="btn-primary w-full mt-10" (click)="simulate()" [disabled]="simulating()">
                {{ simulating() ? 'Simulating...' : 'Run Simulation' }}
              </button>
            </div>
          }
        </div>
      }
    </div>

    <!-- Recursive tree template -->
    <ng-template #nodeTree let-node let-depth="depth">
      <div class="tree-node" [style.margin-left.px]="depth * 20">

        <div class="node-row" [class.sim-complete]="getSimStatus(node.id) === 'complete'" 
                             [class.sim-blocked]="getSimStatus(node.id) === 'blocked'"
                             [class.sim-unlocked]="getSimStatus(node.id) === 'unlocked'">
          <!-- Type icon -->
          <span class="node-icon" [class.icon-step]="node.type === 'step'" [class.icon-group]="node.type === 'group' || !node.type">
            {{ (node.type === 'step') ? '▶' : '⬡' }}
          </span>

          <!-- Name -->
          <span class="node-name">{{ node.name }}</span>

          <!-- Step type badge -->
          @if (node.type === 'step') {
            <span class="badge badge-step">{{ asStep(node).stepType }}</span>
          }

          <!-- Group rule badge -->
          @if (node.type === 'group' || !node.type) {
            @if (asGroup(node).groupRule === 'inOrder') {
              <span class="badge badge-inorder">InOrder</span>
            } @else if (asGroup(node).groupRule === 'choice') {
              <span class="badge badge-choice">Choice · pick {{ asGroup(node).pickCount }}</span>
            }
          }
          
          <!-- Simulation Status Badge -->
          @if (getSimStatus(node.id); as status) {
            <span class="badge" [class.badge-sim-complete]="status === 'complete'"
                                [class.badge-sim-unlocked]="status === 'unlocked'"
                                [class.badge-sim-blocked]="status === 'blocked'">
              {{ status }}
            </span>
          }
        </div>

        <!-- Simulation Blocked Reason -->
        @if (getSimReason(node.id); as reason) {
          <div class="sim-reason">{{ reason }}</div>
        }

        <!-- Prerequisite -->
        @if (node.prerequisiteName) {
          <div class="prereq-row">
            <span class="prereq-label">Requires:</span>
            <span class="prereq-name">{{ node.prerequisiteName }}</span>
          </div>
        }

        <!-- Children (recursive) -->
        @if (node.children && node.children.length > 0) {
          @for (child of node.children; track child.id) {
            <ng-container *ngTemplateOutlet="nodeTree; context: { $implicit: child, depth: depth + 1 }"></ng-container>
          }
        }
      </div>
    </ng-template>
  `,
  styles: [`
    .page { max-width: 1000px; margin: 0 auto; }
    .page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 16px;
    }
    .page-header h2 { margin: 0; font-size: 1.4rem; color: #1a1a2e; }
    .id-row {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 20px;
    }
    .field-label { font-size: 0.85rem; color: #4b5563; min-width: 80px; }
    .id-input {
      flex: 1;
      padding: 6px 10px;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 0.87rem;
      font-family: monospace;
    }
    .btn-primary-sm {
      padding: 6px 16px;
      background: #1a1a2e;
      color: #fff;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      font-size: 0.87rem;
    }
    .btn-primary-sm:hover { background: #2a2a4e; }
    .btn-primary {
      padding: 8px 18px;
      background: #1a1a2e;
      color: #fff;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      font-size: 0.9rem;
    }
    .btn-primary:hover:not(:disabled) { background: #2a2a4e; }
    .btn-primary:disabled { opacity: 0.7; cursor: not-allowed; }
    .w-full { width: 100%; }
    .mt-10 { margin-top: 10px; }
    .btn-secondary {
      padding: 7px 14px;
      background: #fff;
      border: 1px solid #6c9fff;
      color: #1565c0;
      border-radius: 6px;
      font-size: 0.85rem;
      text-decoration: none;
      cursor: pointer;
    }
    .btn-secondary:hover { background: #e3f2fd; }
    .error-banner {
      background: #fdecea;
      border: 1px solid #f5c6c4;
      color: #b71c1c;
      padding: 10px 14px;
      border-radius: 6px;
      margin-bottom: 16px;
      font-size: 0.9rem;
    }
    .state-msg { color: #6b7280; font-size: 0.9rem; margin: 20px 0; }
    
    .actions-row {
      display: flex;
      gap: 10px;
      margin-bottom: 16px;
    }

    .validation-panel {
      padding: 16px;
      border-radius: 8px;
      margin-bottom: 20px;
    }
    .validation-panel.is-valid {
      background: #f0fdf4;
      border: 1px solid #bbf7d0;
    }
    .validation-panel.is-invalid {
      background: #fef2f2;
      border: 1px solid #fecaca;
    }
    .validation-panel h3 { margin: 0 0 10px 0; font-size: 1.1rem; }
    .success-msg { color: #166534; font-weight: 500; font-size: 0.95rem; }
    .issues-list h4 { margin: 0 0 8px 0; font-size: 0.95rem; }
    .issues-list ul { margin: 0; padding-left: 20px; font-size: 0.9rem; }
    .issues-list li { margin-bottom: 6px; }
    .errors-list { color: #991b1b; margin-bottom: 16px; }
    .warnings-list { color: #92400e; }
    
    .layout-row {
      display: flex;
      align-items: flex-start;
      gap: 20px;
    }
    .program-view {
      flex: 1;
      border: 1px solid #d1d5db;
      border-radius: 8px;
      overflow: hidden;
    }
    .simulation-panel {
      width: 300px;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 16px;
      flex-shrink: 0;
    }
    .simulation-panel h3 { margin: 0 0 8px 0; font-size: 1.1rem; }
    .sim-instructions { font-size: 0.8rem; color: #64748b; margin-bottom: 16px; }
    .sim-section { margin-bottom: 16px; }
    .sim-section h4 { margin: 0 0 8px 0; font-size: 0.9rem; color: #334155; border-bottom: 1px solid #e2e8f0; padding-bottom: 4px; }
    .empty-msg { font-size: 0.8rem; color: #94a3b8; font-style: italic; }
    .sim-checkbox { display: flex; align-items: center; gap: 6px; font-size: 0.85rem; margin-bottom: 6px; cursor: pointer; }
    .sim-choice-group { margin-bottom: 10px; font-size: 0.85rem; background: #fff; padding: 8px; border-radius: 4px; border: 1px solid #e2e8f0;}
    .sim-choice-group strong { color: #1e293b; }

    .program-title {
      background: #1a1a2e;
      color: #fff;
      padding: 14px 18px;
      display: flex;
      align-items: baseline;
      gap: 10px;
    }
    .program-label {
      font-size: 0.72rem;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      opacity: 0.6;
    }
    .program-title h3 { margin: 0; font-size: 1.1rem; }
    .program-id { font-size: 0.72rem; font-family: monospace; opacity: 0.5; margin-left: auto; }
    .tree-root { padding: 16px; background: #fff; }
    .tree-node { margin-bottom: 6px; }
    .node-row {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 6px 10px;
      border-radius: 5px;
      background: #f9fafb;
      border: 1px solid #e5e7eb;
      transition: background 0.2s, border-color 0.2s;
    }
    .node-row.sim-complete { background: #f0fdf4; border-color: #bbf7d0; }
    .node-row.sim-unlocked { background: #f0f9ff; border-color: #bae6fd; }
    .node-row.sim-blocked { background: #fef2f2; border-color: #fecaca; opacity: 0.7; }
    
    .node-icon { font-size: 0.85rem; }
    .icon-step { color: #2e7d32; }
    .icon-group { color: #1565c0; }
    .node-name { font-size: 0.9rem; font-weight: 500; color: #111827; }
    .badge {
      font-size: 0.72rem;
      padding: 2px 8px;
      border-radius: 10px;
      font-weight: 600;
      letter-spacing: 0.03em;
    }
    .badge-step { background: #e8f5e9; color: #2e7d32; }
    .badge-inorder { background: #f3e8ff; color: #6b21a8; }
    .badge-choice { background: #fff8e1; color: #b45309; }
    
    .badge-sim-complete { background: #16a34a; color: white; margin-left: auto; }
    .badge-sim-unlocked { background: #0284c7; color: white; margin-left: auto; }
    .badge-sim-blocked { background: #ef4444; color: white; margin-left: auto; }

    .sim-reason {
      font-size: 0.75rem;
      color: #b91c1c;
      padding: 2px 10px 4px 32px;
      font-style: italic;
    }

    .prereq-row {
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 2px 10px 6px 32px;
      font-size: 0.8rem;
    }
    .prereq-label { color: #9ca3af; }
    .prereq-name { color: #1565c0; font-weight: 500; }
  `]
})
export class ViewerPageComponent implements OnInit {
  program = signal<ProgramResponse | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  
  validation = signal<ValidationResultResponse | null>(null);
  validating = signal(false);

  showSimulation = signal(false);
  simulating = signal(false);
  
  allSteps = signal<StepNodeResponse[]>([]);
  allChoices = signal<GroupNodeResponse[]>([]);
  
  completedSteps = new Set<string>();
  choiceSelections = new Map<string, Set<string>>(); // groupId -> selected child ids
  
  simulationResults = signal<Map<string, SimulationNodeResult>>(new Map());

  constructor(
    private api: ProgramApiService,
    private route: ActivatedRoute,
    private router: Router,
    private historyService: ProgramHistoryService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.load(id);
      }
    });
  }

  private load(id: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.program.set(null);
    this.validation.set(null);
    this.showSimulation.set(false);
    this.simulationResults.set(new Map());

    this.api.getProgram(id).subscribe({
      next: (p) => {
        this.loading.set(false);
        this.program.set(p);
        this.historyService.recordProgram(p.id, p.name);
        this.extractNodes(p.rootGroup);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 404) {
          this.error.set(`Program "${id}" not found.`);
        } else {
          this.error.set('Failed to load program: ' + (err.message ?? 'unknown error'));
        }
      }
    });
  }

  validate(): void {
    const p = this.program();
    if (!p) return;
    
    this.validating.set(true);
    this.api.validateProgram(p.id).subscribe({
      next: (res) => {
        this.validating.set(false);
        this.validation.set(res);
      },
      error: (err) => {
        this.validating.set(false);
        this.error.set('Failed to validate program: ' + (err.message ?? 'unknown error'));
      }
    });
  }

  toggleSimulation(): void {
    this.showSimulation.set(!this.showSimulation());
    if (!this.showSimulation()) {
      this.simulationResults.set(new Map()); // clear results when hiding
    }
  }

  private extractNodes(node: ProgramNodeResponse): void {
    const steps: StepNodeResponse[] = [];
    const choices: GroupNodeResponse[] = [];
    
    const traverse = (n: ProgramNodeResponse) => {
      if (n.type === 'step') {
        steps.push(n as StepNodeResponse);
      } else if (n.type === 'group') {
        const group = n as GroupNodeResponse;
        if (group.groupRule === 'choice') {
          choices.push(group);
        }
        for (const child of group.children) {
          traverse(child);
        }
      } else if (!n.type) { // rootGroup has no type discriminator at top level
        const group = n as GroupNodeResponse;
        for (const child of group.children) {
          traverse(child);
        }
      }
    };
    
    traverse(node);
    this.allSteps.set(steps);
    this.allChoices.set(choices);
    this.completedSteps.clear();
    this.choiceSelections.clear();
  }

  toggleStep(stepId: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    if (checked) {
      this.completedSteps.add(stepId);
    } else {
      this.completedSteps.delete(stepId);
    }
  }

  toggleChoice(groupId: string, childId: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    if (!this.choiceSelections.has(groupId)) {
      this.choiceSelections.set(groupId, new Set());
    }
    const selections = this.choiceSelections.get(groupId)!;
    
    if (checked) {
      selections.add(childId);
    } else {
      selections.delete(childId);
    }
  }

  isChoiceSelected(groupId: string, childId: string): boolean {
    return this.choiceSelections.get(groupId)?.has(childId) ?? false;
  }

  simulate(): void {
    const p = this.program();
    if (!p) return;
    
    this.simulating.set(true);
    
    const choicesRecord: Record<string, string[]> = {};
    for (const [groupId, selected] of this.choiceSelections.entries()) {
      choicesRecord[groupId] = Array.from(selected);
    }
    
    const request: SimulationRequest = {
      choices: choicesRecord,
      completedStepIds: Array.from(this.completedSteps)
    };
    
    this.api.simulateProgram(p.id, request).subscribe({
      next: (res) => {
        this.simulating.set(false);
        const resultsMap = new Map<string, SimulationNodeResult>();
        
        const traverseResult = (n: SimulationNodeResult) => {
          resultsMap.set(n.id, n);
          for (const child of n.children) {
            traverseResult(child);
          }
        };
        
        traverseResult(res.rootNode);
        this.simulationResults.set(resultsMap);
      },
      error: (err) => {
        this.simulating.set(false);
        this.error.set('Failed to simulate program: ' + (err.message ?? 'unknown error'));
      }
    });
  }

  getSimStatus(id: string): string | null {
    return this.simulationResults().get(id)?.status ?? null;
  }

  getSimReason(id: string): string | null {
    return this.simulationResults().get(id)?.blockedReason ?? null;
  }

  /** Cast helper — rootGroup has no 'type' discriminator at top level */
  asGroup(node: ProgramNodeResponse): GroupNodeResponse {
    return { ...node, type: 'group' } as GroupNodeResponse;
  }
  
  asStep(node: ProgramNodeResponse): StepNodeResponse {
    return node as StepNodeResponse;
  }
}
