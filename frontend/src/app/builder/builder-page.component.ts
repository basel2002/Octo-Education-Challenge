import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProgramApiService } from '../core/program-api.service';
import { NodeEditorComponent } from './node-editor.component';
import {
  FormNode,
  createGroupNode,
  collectAll,
  nextKey,
} from './builder.model';
import { CreateNodeRequest, CreateGroupNodeRequest } from '../core/api.models';

@Component({
  selector: 'app-builder-page',
  standalone: true,
  imports: [CommonModule, FormsModule, NodeEditorComponent],
  template: `
    <div class="page">
      <div class="page-header">
        <h2>Program Builder</h2>
        <button class="btn-secondary" (click)="loadExample()">⭐ Load Computer Science Example</button>
      </div>

      @if (error()) {
        <div class="error-banner">{{ error() }}</div>
      }

      <!-- Program name -->
      <div class="program-name-row">
        <label class="field-label-lg">Program name</label>
        <input class="field-input-lg" [(ngModel)]="programName" placeholder="Enter program name" />
      </div>

      <!-- Root group config -->
      <div class="root-group-config">
        <span class="section-label">Root group</span>
        <div class="field-row-inline">
          <label class="field-label">Name</label>
          <input class="field-input" [(ngModel)]="rootGroup.name" placeholder="Root group name" />
        </div>
        <div class="field-row-inline">
          <label class="field-label">Rule</label>
          <select class="field-input" [(ngModel)]="rootGroup.groupRule">
            <option value="inOrder">InOrder (all required)</option>
            <option value="choice">Choice (pick N)</option>
          </select>
        </div>
        @if (rootGroup.groupRule === 'choice') {
          <div class="field-row-inline">
            <label class="field-label">Pick count</label>
            <input class="field-input field-narrow" type="number" [(ngModel)]="rootGroup.pickCount" min="1" />
          </div>
        }
      </div>

      <!-- Children -->
      <div class="children-area">
        @for (child of rootGroup.children; track child.key; let i = $index) {
          <app-node-editor
            [node]="child"
            [allNodes]="allNodes()"
            (remove)="removeChild(i)"
          />
        }
        <div class="add-buttons">
          <button class="btn-add" (click)="addStep()">+ Step</button>
          <button class="btn-add" (click)="addGroup()">+ Group</button>
        </div>
      </div>

      <!-- Submit -->
      <div class="submit-row">
        <button class="btn-primary" [disabled]="submitting()" (click)="submit()">
          {{ submitting() ? 'Creating…' : 'Create Program' }}
        </button>
      </div>
    </div>
  `,
  styles: [`
    .page { max-width: 820px; margin: 0 auto; }
    .page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 20px;
    }
    .page-header h2 { margin: 0; font-size: 1.4rem; color: #1a1a2e; }
    .error-banner {
      background: #fdecea;
      border: 1px solid #f5c6c4;
      color: #b71c1c;
      padding: 10px 14px;
      border-radius: 6px;
      margin-bottom: 16px;
      font-size: 0.9rem;
    }
    .program-name-row {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 16px;
    }
    .field-label-lg { font-weight: 600; font-size: 0.95rem; min-width: 120px; }
    .field-input-lg {
      flex: 1;
      padding: 8px 12px;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 1rem;
    }
    .root-group-config {
      background: #f0f4ff;
      border: 1px solid #b8cbff;
      border-radius: 8px;
      padding: 14px 16px;
      margin-bottom: 16px;
    }
    .section-label {
      font-size: 0.78rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: #1565c0;
      display: block;
      margin-bottom: 10px;
    }
    .field-row-inline {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 8px;
    }
    .field-label { width: 80px; font-size: 0.85rem; color: #4b5563; flex-shrink: 0; }
    .field-input {
      flex: 1;
      padding: 5px 8px;
      border: 1px solid #d1d5db;
      border-radius: 4px;
      font-size: 0.87rem;
    }
    .field-narrow { max-width: 80px; }
    .children-area {
      padding-left: 4px;
      margin-bottom: 20px;
    }
    .add-buttons { display: flex; gap: 8px; margin-top: 10px; }
    .btn-add {
      font-size: 0.82rem;
      padding: 5px 14px;
      border: 1px dashed #6c9fff;
      border-radius: 4px;
      background: transparent;
      color: #1565c0;
      cursor: pointer;
    }
    .btn-add:hover { background: #e3f2fd; }
    .submit-row { display: flex; justify-content: flex-end; }
    .btn-primary {
      padding: 10px 28px;
      background: #1a1a2e;
      color: #fff;
      border: none;
      border-radius: 6px;
      font-size: 0.95rem;
      cursor: pointer;
    }
    .btn-primary:disabled { opacity: 0.5; cursor: default; }
    .btn-primary:not(:disabled):hover { background: #2a2a4e; }
    .btn-secondary {
      padding: 7px 16px;
      background: #fff;
      border: 1px solid #6c9fff;
      color: #1565c0;
      border-radius: 6px;
      font-size: 0.85rem;
      cursor: pointer;
    }
    .btn-secondary:hover { background: #e3f2fd; }
  `]
})
export class BuilderPageComponent {
  programName = '';
  rootGroup: FormNode = this.makeRootGroup();
  error = signal<string | null>(null);
  submitting = signal(false);

  constructor(private api: ProgramApiService, private router: Router) {}

  /** Reactive flat list of all nodes — used for prerequisite dropdowns */
  allNodes = signal<FormNode[]>([]);

  private refreshAllNodes(): void {
    this.allNodes.set(collectAll(this.rootGroup));
  }

  addStep(): void {
    const n = { key: nextKey(), type: 'step' as const, name: '', stepType: 'session', groupRule: 'inOrder' as const, pickCount: 1, prerequisiteRef: '', children: [] };
    this.rootGroup.children.push(n);
    this.refreshAllNodes();
  }

  addGroup(): void {
    const n = { key: nextKey(), type: 'group' as const, name: '', stepType: '', groupRule: 'inOrder' as const, pickCount: 1, prerequisiteRef: '', children: [] };
    this.rootGroup.children.push(n);
    this.refreshAllNodes();
  }

  removeChild(i: number): void {
    this.rootGroup.children.splice(i, 1);
    this.refreshAllNodes();
  }

  loadExample(): void {
    this.programName = 'Computer Science';
    this.rootGroup = {
      key: nextKey(), type: 'group', name: 'Computer Science',
      groupRule: 'inOrder', pickCount: 1, prerequisiteRef: '', stepType: '',
      children: [
        {
          key: 'Foundations', type: 'group', name: 'Foundations',
          groupRule: 'inOrder', pickCount: 1, prerequisiteRef: '', stepType: '',
          children: [
            { key: nextKey(), type: 'step', name: 'Introduction to Computing', stepType: 'session', groupRule: 'inOrder', pickCount: 1, prerequisiteRef: '', children: [] },
            { key: nextKey(), type: 'step', name: 'Mathematics for Computing', stepType: 'session', groupRule: 'inOrder', pickCount: 1, prerequisiteRef: '', children: [] },
          ]
        },
        {
          key: 'Major', type: 'group', name: 'Major',
          groupRule: 'choice', pickCount: 1, prerequisiteRef: 'Foundations', stepType: '',
          children: [
            {
              key: nextKey(), type: 'group', name: 'AI',
              groupRule: 'choice', pickCount: 2, prerequisiteRef: '', stepType: '',
              children: [
                { key: nextKey(), type: 'step', name: 'Machine Learning', stepType: 'session', groupRule: 'inOrder', pickCount: 1, prerequisiteRef: '', children: [] },
                { key: nextKey(), type: 'step', name: 'Neural Networks', stepType: 'session', groupRule: 'inOrder', pickCount: 1, prerequisiteRef: '', children: [] },
                { key: nextKey(), type: 'step', name: 'Computer Vision', stepType: 'session', groupRule: 'inOrder', pickCount: 1, prerequisiteRef: '', children: [] },
              ]
            },
            { key: nextKey(), type: 'group', name: 'IT', groupRule: 'inOrder', pickCount: 1, prerequisiteRef: '', stepType: '', children: [{ key: nextKey(), type: 'step', name: 'Networking Basics', stepType: 'session', groupRule: 'inOrder', pickCount: 1, prerequisiteRef: '', children: [] }] },
            { key: nextKey(), type: 'group', name: 'Programming', groupRule: 'inOrder', pickCount: 1, prerequisiteRef: '', stepType: '', children: [{ key: nextKey(), type: 'step', name: 'Algorithms', stepType: 'session', groupRule: 'inOrder', pickCount: 1, prerequisiteRef: '', children: [] }] },
          ]
        },
        { key: nextKey(), type: 'step', name: 'Final Capstone', stepType: 'submission', groupRule: 'inOrder', pickCount: 1, prerequisiteRef: 'Major', children: [] },
      ]
    };
    this.refreshAllNodes();
    this.error.set(null);
  }

  submit(): void {
    if (!this.programName.trim()) {
      this.error.set('Program name is required.');
      return;
    }
    this.error.set(null);
    this.submitting.set(true);

    const request = {
      name: this.programName.trim(),
      rootGroup: this.mapNode(this.rootGroup) as Omit<CreateGroupNodeRequest, 'type'>,
    };

    this.api.createProgram(request).subscribe({
      next: (program) => {
        this.submitting.set(false);
        this.router.navigate(['/programs', program.id]);
      },
      error: (err) => {
        this.submitting.set(false);
        if (err.error?.errors) {
          this.error.set('Validation errors: ' + (err.error.errors as string[]).join(', '));
        } else if (err.status === 400) {
          this.error.set('The program structure has errors: ' + JSON.stringify(err.error));
        } else {
          this.error.set('Unexpected error: ' + (err.message ?? 'unknown'));
        }
      }
    });
  }

  private mapNode(node: FormNode): CreateNodeRequest | Omit<CreateGroupNodeRequest, 'type'> {
    if (node.type === 'step') {
      return {
        type: 'step',
        key: node.key,
        name: node.name,
        stepType: node.stepType,
        ...(node.prerequisiteRef ? { prerequisiteRef: node.prerequisiteRef } : {}),
      };
    } else {
      return {
        type: 'group',
        key: node.key,
        name: node.name,
        groupRule: node.groupRule,
        ...(node.groupRule === 'choice' ? { pickCount: node.pickCount } : {}),
        ...(node.prerequisiteRef ? { prerequisiteRef: node.prerequisiteRef } : {}),
        children: node.children.map(c => this.mapNode(c) as CreateNodeRequest),
      };
    }
  }

  private makeRootGroup(): FormNode {
    const g = createGroupNode();
    g.name = 'Root';
    return g;
  }
}
