// Mirrors EventDTO from the API (list endpoint).
export interface EventListItem {
  id: number;
  name: string;
  startsAt: string;        // ISO date sent by .NET
  venueId: number;
  venueName: string;
  venueCity: string | null;
}

// Mirrors EventDetailDTO from the API (single event endpoint).
export interface EventDetail {
  id: number;
  name: string;
  description: string | null;
  startsAt: string;
  venueId: number;
  venueName: string;
  venueCity: string | null;
  organizerName: string;
  totalSeats: number;
  freeSeats: number;
}
