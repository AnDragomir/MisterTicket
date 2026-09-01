// Mirrors EventSeatDTO.
export interface EventSeatItem {
  id: number;                  // EventSeat id: what we send when booking
  rowLabel: string;
  number: number;
  status: 'Free' | 'Reserved' | 'Paid';
  price: number;
  pricingZoneId: number;
  pricingZoneName: string;
  pricingZoneColor: string;
  isMine: boolean;
}

// Mirrors SeatMapDTO.
export interface SeatMap {
  eventId: number;
  eventName: string;
  venueName: string;
  startsAt: string;
  seats: EventSeatItem[];
}

// Mirrors ReservationSeatDTO.
export interface ReservationSeat {
  eventSeatId: number;
  rowLabel: string;
  number: number;
  price: number;
  pricingZoneName: string;
}

// Mirrors ReservationDTO.
export interface Reservation {
  id: number;
  eventId: number;
  eventName: string;
  venueName: string;
  startsAt: string;
  status: 'Pending' | 'Paid' | 'Cancelled';
  createdAt: string;
  expiresAt: string;
  totalAmount: number;
  seats: ReservationSeat[];
}

// Mirrors ReservationCreateDTO.
export interface ReservationCreate {
  eventId: number;
  eventSeatIds: number[];
}

/** Mirrors SeatStatusChangeDTO: one seat whose status changed. */
export interface SeatStatusChange {
  eventSeatId: number;
  status: 'Free' | 'Reserved' | 'Paid';
}

/** Mirrors SeatsChangedDTO: what the hub pushes. */
export interface SeatsChanged {
  eventId: number;
  seats: SeatStatusChange[];
}

/** One line of the zone price list shown next to the map. */
export interface ZonePrice {
  zoneId: number;
  zoneName: string;
  price: number;
}

/** One line of the "x2 VIP, x3 Balcon" summary. */
export interface ZoneTally {
  zoneName: string;
  zoneColor: string;
  count: number;
  subtotal: number;
}

/** Mirrors PaymentCreateDTO: no real credential ever leaves the browser. */
export interface PaymentRequest {
  method: string;
  cardLastFour: string | null;
}
