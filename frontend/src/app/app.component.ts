import { Component, computed, signal, HostListener, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProgramHistoryService, KnownProgram } from './core/program-history.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  private historyService = inject(ProgramHistoryService);
  private router = inject(Router);

  searchTerm = signal('');
  isSearchFocused = signal(false);

  knownPrograms = this.historyService.knownPrograms;

  filteredPrograms = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    const list = this.knownPrograms();
    if (!term) return list;
    return list.filter(p => p.name.toLowerCase().includes(term));
  });

  onSearchFocus(): void {
    this.isSearchFocused.set(true);
  }

  onSearchBlur(): void {
    // delay hiding so clicks on the dropdown can register
    setTimeout(() => this.isSearchFocused.set(false), 200);
  }

  selectProgram(p: KnownProgram): void {
    this.searchTerm.set('');
    this.isSearchFocused.set(false);
    this.router.navigate(['/programs', p.id]);
  }
}

