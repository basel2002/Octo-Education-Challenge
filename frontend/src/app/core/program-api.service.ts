import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import type {
  CreateProgramRequest,
  ProgramResponse,
  ValidationResultResponse,
  SimulationRequest,
  SimulationResponse,
} from './api.models';

/**
 * Typed HTTP client for the ProgramDesigner backend API.
 * The base URL is read from the Angular environment file so it can be
 * switched between development (localhost:5173) and production without
 * touching this service.
 */
@Injectable({ providedIn: 'root' })
export class ProgramApiService {
  private readonly base = environment.apiBaseUrl;

  constructor(private http: HttpClient) {}

  /** POST /programs — create and store a new education program. Returns 201 on success. */
  createProgram(request: CreateProgramRequest): Observable<ProgramResponse> {
    return this.http.post<ProgramResponse>(`${this.base}/programs`, request);
  }

  /** GET /programs/{id} — retrieve a stored program by its server-assigned Guid. */
  getProgram(id: string): Observable<ProgramResponse> {
    return this.http.get<ProgramResponse>(`${this.base}/programs/${id}`);
  }

  /**
   * POST /programs/{id}/validate — run prerequisite and reachability checks.
   * Always returns 200; check `isValid` and the arrays in the response body.
   */
  validateProgram(id: string): Observable<ValidationResultResponse> {
    return this.http.post<ValidationResultResponse>(
      `${this.base}/programs/${id}/validate`,
      null
    );
  }

  /**
   * POST /programs/{id}/simulate — compute one participant's progress state
   * given their choice selections and completed steps.
   */
  simulateProgram(id: string, request: SimulationRequest): Observable<SimulationResponse> {
    return this.http.post<SimulationResponse>(
      `${this.base}/programs/${id}/simulate`,
      request
    );
  }
}
