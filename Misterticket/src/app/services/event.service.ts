import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { EventDetail, EventListItem } from '../models/event.model';
import { API_BASE_URL } from '../api.config';

@Injectable({ providedIn: 'root' })
export class EventService {
  private http = inject(HttpClient);

  // Change this once, instead of in every component.
  private readonly baseUrl = API_BASE_URL;

  getAll(): Observable<EventListItem[]> {
    return this.http.get<EventListItem[]>(`${this.baseUrl}/events`);
  }

  getById(id: number): Observable<EventDetail> {
    return this.http.get<EventDetail>(`${this.baseUrl}/events/${id}`);
  }
}
