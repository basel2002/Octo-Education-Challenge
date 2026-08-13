import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FormNode, FormGroupRule, createStepNode, createGroupNode, collectAll } from './builder.model';

@Component({
  selector: 'app-node-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="node-card" [class.group-card]="node.type === 'group'">

      <!-- Node type toggle -->
      <div class="node-header">
        <span class="node-type-badge" [class.badge-step]="node.type === 'step'" [class.badge-group]="node.type === 'group'">
          {{ node.type === 'step' ? '▶ Step' : '⬡ Group' }}
        </span>
        <div class="type-toggle">
          <label>
            <input type="radio" [(ngModel)]="node.type" value="step" (change)="onTypeChange()"> Step
          </label>
          <label>
            <input type="radio" [(ngModel)]="node.type" value="group" (change)="onTypeChange()"> Group
          </label>
        </div>
        <button class="btn-remove" (click)="remove.emit()" title="Remove node">✕</button>
      </div>

      <!-- Name -->
      <div class="field-row">
        <label class="field-label">Name</label>
        <input class="field-input" [(ngModel)]="node.name" placeholder="Node name" />
      </div>

      <!-- Step-specific: step type -->
      @if (node.type === 'step') {
        <div class="field-row">
          <label class="field-label">Step type</label>
          <select class="field-input" [(ngModel)]="node.stepType">
            <option value="session">session</option>
            <option value="test">test</option>
            <option value="submission">submission</option>
          </select>
        </div>
      }

      <!-- Group-specific: rule + pick count -->
      @if (node.type === 'group') {
        <div class="field-row">
          <label class="field-label">Rule</label>
          <select class="field-input" [(ngModel)]="node.groupRule">
            <option value="inOrder">InOrder (all required)</option>
            <option value="choice">Choice (pick N)</option>
          </select>
        </div>
        @if (node.groupRule === 'choice') {
          <div class="field-row">
            <label class="field-label">Pick count</label>
            <input class="field-input field-narrow" type="number" [(ngModel)]="node.pickCount" min="1" />
          </div>
        }
      }

      <!-- Prerequisite picker -->
      @if (availablePrereqs.length > 0) {
        <div class="field-row">
          <label class="field-label">Prerequisite</label>
          <select class="field-input" [(ngModel)]="node.prerequisiteRef">
            <option value="">(none)</option>
            @for (n of availablePrereqs; track n.key) {
              <option [value]="n.key">{{ n.name || '(unnamed)' }}</option>
            }
          </select>
        </div>
      }

      <!-- Children (recursive) -->
      @if (node.type === 'group') {
        <div class="children-area">
          @for (child of node.children; track child.key; let i = $index) {
            <app-node-editor
              [node]="child"
              [allNodes]="allNodes"
              (remove)="removeChild(i)"
            />
          }
          <div class="add-buttons">
            <button class="btn-add" (click)="addStep()">+ Step</button>
            <button class="btn-add" (click)="addGroup()">+ Group</button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .node-card {
      border: 1px solid #d1d5db;
      border-radius: 6px;
      padding: 12px 14px;
      margin-bottom: 10px;
      background: #fff;
    }
    .group-card {
      border-color: #6c9fff;
      background: #f8fbff;
    }
    .node-header {
      display: flex;
      align-items: center;
      gap: 10px;
      margin-bottom: 10px;
    }
    .node-type-badge {
      font-size: 0.72rem;
      font-weight: 600;
      padding: 2px 8px;
      border-radius: 12px;
      letter-spacing: 0.03em;
    }
    .badge-step { background: #e8f5e9; color: #2e7d32; }
    .badge-group { background: #e3f2fd; color: #1565c0; }
    .type-toggle { display: flex; gap: 10px; font-size: 0.85rem; }
    .type-toggle label { display: flex; align-items: center; gap: 4px; cursor: pointer; }
    .btn-remove {
      margin-left: auto;
      background: none;
      border: none;
      color: #ef5350;
      cursor: pointer;
      font-size: 1rem;
      padding: 0 4px;
    }
    .field-row {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 8px;
    }
    .field-label {
      width: 90px;
      font-size: 0.82rem;
      color: #4b5563;
      flex-shrink: 0;
    }
    .field-input {
      flex: 1;
      padding: 5px 8px;
      border: 1px solid #d1d5db;
      border-radius: 4px;
      font-size: 0.87rem;
    }
    .field-narrow { max-width: 80px; }
    .children-area {
      margin-top: 12px;
      padding-left: 16px;
      border-left: 3px solid #6c9fff33;
    }
    .add-buttons { display: flex; gap: 8px; margin-top: 8px; }
    .btn-add {
      font-size: 0.8rem;
      padding: 4px 12px;
      border: 1px dashed #6c9fff;
      border-radius: 4px;
      background: transparent;
      color: #1565c0;
      cursor: pointer;
    }
    .btn-add:hover { background: #e3f2fd; }
  `]
})
export class NodeEditorComponent {
  @Input({ required: true }) node!: FormNode;
  @Input() allNodes: FormNode[] = [];
  @Output() remove = new EventEmitter<void>();

  /** Nodes that can be selected as a prerequisite (all except self and own descendants) */
  get availablePrereqs(): FormNode[] {
    const selfAndDescendants = new Set(collectAll(this.node).map(n => n.key));
    return this.allNodes.filter(n => !selfAndDescendants.has(n.key));
  }

  onTypeChange(): void {
    if (this.node.type === 'step') this.node.children = [];
  }

  addStep(): void { this.node.children.push(createStepNode()); }
  addGroup(): void { this.node.children.push(createGroupNode()); }
  removeChild(i: number): void { this.node.children.splice(i, 1); }
}
