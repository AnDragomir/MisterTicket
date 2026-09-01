import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { PaymentRequest, Reservation, SeatMap } from '../models/reservation.model';

@Injectable({ providedIn: 'root' })
export class ReservationService {
  private http = inject(HttpClient);
  private readonly baseUrl = API_BASE_URL;

  getSeatMap(eventId: number): Observable<SeatMap> {
    return this.http.get<SeatMap>(`${this.baseUrl}/events/${eventId}/seats`);
  }

  /** The basket being filled for this event, or null. */
  getActive(eventId: number): Observable<Reservation | null> {
    return this.http.get<Reservation | null>(
      `${this.baseUrl}/reservations/events/${eventId}/active`
    );
  }

  /** Reserves one seat straight away. */
  claimSeat(eventId: number, eventSeatId: number): Observable<Reservation> {
    return this.http.post<Reservation>(
      `${this.baseUrl}/reservations/events/${eventId}/seats/${eventSeatId}`,
      {}
    );
  }

  /** Gives one seat back. Null when it was the last one. */
  releaseSeat(eventId: number, eventSeatId: number): Observable<Reservation | null> {
    return this.http.delete<Reservation | null>(
      `${this.baseUrl}/reservations/events/${eventId}/seats/${eventSeatId}`
    );
  }

  getById(reservationId: number): Observable<Reservation> {
    return this.http.get<Reservation>(`${this.baseUrl}/reservations/${reservationId}`);
  }

  getMine(): Observable<Reservation[]> {
    return this.http.get<Reservation[]>(`${this.baseUrl}/reservations/mine`);
  }

  /** Simulated payment: turns the reservation into a ticket. */
  pay(reservationId: number, request: PaymentRequest): Observable<Reservation> {
    return this.http.post<Reservation>(
      `${this.baseUrl}/reservations/${reservationId}/payment`,
      request
    );
  }

  /** The PDF ticket, as a blob the browser can save. */
  downloadTicket(reservationId: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/reservations/${reservationId}/ticket`, {
      responseType: 'blob'
    });
  }

  /** Gives every seat of the basket back. */
  cancel(reservationId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/reservations/${reservationId}`);
  }
}
