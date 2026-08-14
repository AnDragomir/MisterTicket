import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { Reservation, ReservationCreate, SeatMap } from '../models/reservation.model';

@Injectable({ providedIn: 'root' })
export class ReservationService {
  private http = inject(HttpClient);
  private readonly baseUrl = API_BASE_URL;

  getSeatMap(eventId: number): Observable<SeatMap> {
    return this.http.get<SeatMap>(`${this.baseUrl}/events/${eventId}/seats`);
  }

  /** Holds the seats for 15 minutes. */
  hold(request: ReservationCreate): Observable<Reservation> {
    return this.http.post<Reservation>(`${this.baseUrl}/reservations`, request);
  }

  getById(reservationId: number): Observable<Reservation> {
    return this.http.get<Reservation>(`${this.baseUrl}/reservations/${reservationId}`);
  }

  getMine(): Observable<Reservation[]> {
    return this.http.get<Reservation[]>(`${this.baseUrl}/reservations/mine`);
  }

  /** Gives the seats back before payment. */
  cancel(reservationId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/reservations/${reservationId}`);
  }
}
