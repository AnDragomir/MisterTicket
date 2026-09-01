import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import {
  PricingZone, PricingZoneWrite, SeatBlock,
  VenueDetail, VenueListItem, VenueWrite, EventWrite
} from '../models/admin.model';
import { EventDetail, EventListItem } from '../models/event.model';

/** Everything only an Admin or an Organizer can call. */
@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);
  private readonly baseUrl = API_BASE_URL;

  // ---------------- pricing zones ----------------

  getZones(): Observable<PricingZone[]> {
    return this.http.get<PricingZone[]>(`${this.baseUrl}/pricingzones`);
  }

  createZone(dto: PricingZoneWrite): Observable<PricingZone> {
    return this.http.post<PricingZone>(`${this.baseUrl}/pricingzones`, dto);
  }

  updateZone(id: number, dto: PricingZoneWrite): Observable<PricingZone> {
    return this.http.put<PricingZone>(`${this.baseUrl}/pricingzones/${id}`, dto);
  }

  deleteZone(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/pricingzones/${id}`);
  }

  // ---------------- venues ----------------

  getVenues(): Observable<VenueListItem[]> {
    return this.http.get<VenueListItem[]>(`${this.baseUrl}/venues`);
  }

  getVenue(id: number): Observable<VenueDetail> {
    return this.http.get<VenueDetail>(`${this.baseUrl}/venues/${id}`);
  }

  createVenue(dto: VenueWrite): Observable<VenueDetail> {
    return this.http.post<VenueDetail>(`${this.baseUrl}/venues`, dto);
  }

  updateVenue(id: number, dto: VenueWrite): Observable<VenueDetail> {
    return this.http.put<VenueDetail>(`${this.baseUrl}/venues/${id}`, dto);
  }

  deleteVenue(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/venues/${id}`);
  }

  /** Generates a rectangular block of seats in one call. */
  addSeatBlock(venueId: number, block: SeatBlock): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/venues/${venueId}/seats`, block);
  }

  clearSeats(venueId: number): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/venues/${venueId}/seats`);
  }

  // ---------------- events ----------------

  getEvents(): Observable<EventListItem[]> {
    return this.http.get<EventListItem[]>(`${this.baseUrl}/events`);
  }

  createEvent(dto: EventWrite): Observable<EventDetail> {
    return this.http.post<EventDetail>(`${this.baseUrl}/events`, dto);
  }

  updateEvent(id: number, dto: EventWrite): Observable<EventDetail> {
    return this.http.put<EventDetail>(`${this.baseUrl}/events/${id}`, dto);
  }

  deleteEvent(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/events/${id}`);
  }
}
