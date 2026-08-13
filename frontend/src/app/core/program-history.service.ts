import { Injectable, signal } from '@angular/core';

export interface KnownProgram {
  id: string;
  name: string;
  createdAt: string;
}

const STORAGE_KEY = 'programDesigner.knownPrograms';

@Injectable({ providedIn: 'root' })
export class ProgramHistoryService {
  private _knownPrograms = signal<KnownProgram[]>([]);

  readonly knownPrograms = this._knownPrograms.asReadonly();

  constructor() {
    this.loadFromStorage();
  }

  recordProgram(id: string, name: string): void {
    const current = this._knownPrograms();
    // Remove if already exists to deduplicate (and push to top)
    const filtered = current.filter(p => p.id !== id);
    const newEntry: KnownProgram = {
      id,
      name,
      createdAt: new Date().toISOString()
    };
    
    const updated = [newEntry, ...filtered];
    this._knownPrograms.set(updated);
    this.saveToStorage(updated);
  }

  private loadFromStorage(): void {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        this._knownPrograms.set(JSON.parse(stored));
      }
    } catch (e) {
      console.warn('Could not load program history from localStorage', e);
    }
  }

  private saveToStorage(programs: KnownProgram[]): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(programs));
    } catch (e) {
      console.warn('Could not save program history to localStorage', e);
    }
  }
}
