// ---- pricing zones (shared catalogue) ----

export interface PricingZone {
  id: number;
  name: string;
  colorHex: string;
  basePrice: number;
}

export interface PricingZoneWrite {
  name: string;
  colorHex: string;
  basePrice: number;
}

// ---- venues ----

export interface VenueListItem {
  id: number;
  name: string;
  city: string | null;
  capacity: number;
  seatCount: number;
}

export interface VenueZone extends PricingZone {
  seatCount: number;
}

export interface VenueDetail {
  id: number;
  name: string;
  city: string | null;
  capacity: number;
  seatCount: number;
  pricingZones: VenueZone[];
}

export interface VenueWrite {
  name: string;
  city: string | null;
  capacity: number;
}

/** Mirrors SeatBulkCreateDTO: generates rows x seats in one call. */
export interface SeatBlock {
  pricingZoneId: number;
  firstRow: string;
  rowCount: number;
  seatsPerRow: number;
  firstSeatNumber: number;
}

// ---- events ----

export interface EventWrite {
  name: string;
  description: string | null;
  startsAt: string;
  venueId?: number;      // create only: an event cannot change venue
}
