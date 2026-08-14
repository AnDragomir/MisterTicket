import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { interval } from 'rxjs';
import { EventSeatItem, Reservation, SeatMap, SeatsChanged, ZoneTally } from '../../models/reservation.model';
import { ReservationService } from '../../services/reservation.service';
import { SeatHubService } from '../../services/seat.hub.service';

interface SeatRow {
  label: string;
  seats: EventSeatItem[];
}

/** Consecutive rows that share a pricing zone, drawn as one band on the map. */
interface ZoneBand {
  zoneId: number;
  zoneName: string;
  price: number;
  rows: SeatRow[];
}

@Component({
  selector: 'reservation',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe],
  templateUrl: './reservation.component.html',
  styleUrls: ['./reservation.component.css']
})
export class ReservationComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private reservationService = inject(ReservationService);
  private seatHub = inject(SeatHubService);
  private destroyRef = inject(DestroyRef);

  readonly eventId = Number(this.route.snapshot.paramMap.get('id'));

  readonly seatMap = signal<SeatMap | null>(null);
  readonly loadFailed = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly working = signal(false);

  /** Seats picked but not yet held. */
  readonly selectedIds = signal<ReadonlySet<number>>(new Set());

  /** Set once the seats are held; drives the countdown. */
  readonly reservation = signal<Reservation | null>(null);
  readonly secondsLeft = signal(0);

  /** Rows in stage-to-back order, which is simply alphabetical. */
  readonly rows = computed<SeatRow[]>(() => {
    const map = this.seatMap();
    if (!map) return [];

    const byRow = new Map<string, EventSeatItem[]>();
    for (const seat of map.seats) {
      const row = byRow.get(seat.rowLabel) ?? [];
      row.push(seat);
      byRow.set(seat.rowLabel, row);
    }

    return [...byRow.entries()]
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([label, seats]) => ({
        label,
        seats: seats.sort((a, b) => a.number - b.number)
      }));
  });

  /**
   * The map, cut into price bands: rows are grouped as long as they keep the
   * same zone, so each band can be labelled with its price.
   */
  readonly zoneBands = computed<ZoneBand[]>(() => {
    const bands: ZoneBand[] = [];

    for (const row of this.rows()) {
      const first = row.seats[0];
      if (!first) continue;

      const last = bands[bands.length - 1];

      if (last && last.zoneId === first.pricingZoneId) {
        last.rows.push(row);
      } else {
        bands.push({
          zoneId: first.pricingZoneId,
          zoneName: first.pricingZoneName,
          price: first.price,
          rows: [row]
        });
      }
    }

    return bands;
  });

  /** "x2 VIP, x3 Balcon" with a subtotal per zone. */
  readonly tally = computed<ZoneTally[]>(() => {
    const picked = this.pickedSeats();
    const byZone = new Map<string, ZoneTally>();

    for (const seat of picked) {
      const line = byZone.get(seat.pricingZoneName) ?? {
        zoneName: seat.pricingZoneName,
        zoneColor: seat.pricingZoneColor,
        count: 0,
        subtotal: 0
      };
      line.count += 1;
      line.subtotal += seat.price;
      byZone.set(seat.pricingZoneName, line);
    }

    return [...byZone.values()].sort((a, b) => b.subtotal - a.subtotal);
  });

  readonly total = computed(() =>
    this.pickedSeats().reduce((sum, seat) => sum + seat.price, 0)
  );

  readonly countdown = computed(() => {
    const seconds = Math.max(0, this.secondsLeft());
    const minutes = Math.floor(seconds / 60);
    const rest = seconds % 60;
    return `${minutes}:${rest.toString().padStart(2, '0')}`;
  });

  constructor() {
    this.loadSeatMap();

    // One ticker for the whole page; it only does something while a hold is live.
    interval(1000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.tick());

    // Live updates: somebody else reserving, paying or losing their hold.
    this.seatHub.joinEvent(this.eventId);

    this.seatHub.seatsChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(update => this.applySeatUpdate(update));

    this.destroyRef.onDestroy(() => this.seatHub.leaveEvent());
  }

  /**
   * Patches the map in place instead of refetching it: the payload only carries
   * the seats that moved.
   */
  private applySeatUpdate(update: SeatsChanged): void {
    if (update.eventId !== this.eventId) return;

    const map = this.seatMap();
    if (!map) return;

    const changes = new Map(update.seats.map(seat => [seat.eventSeatId, seat.status]));

    this.seatMap.set({
      ...map,
      seats: map.seats.map(seat => {
        const status = changes.get(seat.id);
        return status ? { ...seat, status } : seat;
      })
    });

    // A seat we had picked but had not held yet may have just been taken.
    const stillFree = new Set(
      [...this.selectedIds()].filter(id => {
        const status = changes.get(id);
        return status === undefined || status === 'Free';
      })
    );

    if (stillFree.size !== this.selectedIds().size) {
      this.selectedIds.set(stillFree);
      this.errorMessage.set('One of your picked seats was just taken by someone else.');
    }
  }

  private pickedSeats(): EventSeatItem[] {
    const map = this.seatMap();
    const ids = this.selectedIds();
    return map ? map.seats.filter(seat => ids.has(seat.id)) : [];
  }

  private loadSeatMap(): void {
    this.reservationService.getSeatMap(this.eventId).subscribe({
      next: map => this.seatMap.set(map),
      error: () => this.loadFailed.set(true)
    });
  }

  isSelected(seat: EventSeatItem): boolean {
    return this.selectedIds().has(seat.id);
  }

  /** A seat can be picked only while nothing is held yet. */
  isSelectable(seat: EventSeatItem): boolean {
    return seat.status === 'Free' && this.reservation() === null;
  }

  onSeatClick(seat: EventSeatItem): void {
    if (!this.isSelectable(seat)) return;

    const next = new Set(this.selectedIds());
    next.has(seat.id) ? next.delete(seat.id) : next.add(seat.id);
    this.selectedIds.set(next);
  }

  /** Holds the seats, then sends the client straight to payment. */
  onProceedToPayment(): void {
    const ids = [...this.selectedIds()];
    if (ids.length === 0 || this.working()) return;

    this.working.set(true);
    this.errorMessage.set(null);

    this.reservationService.hold({ eventId: this.eventId, eventSeatIds: ids }).subscribe({
      next: reservation => {
        this.working.set(false);
        this.reservation.set(reservation);
        this.router.navigate(['/reservations', reservation.id, 'payment']);
      },
      error: response => {
        this.working.set(false);
        this.errorMessage.set(
          response.status === 409
            ? response.error?.message ?? 'Some of those seats were just taken. The map has been refreshed.'
            : 'The seats could not be held. Try again in a moment.'
        );

        // A 409 means somebody else got there first: show the truth.
        if (response.status === 409) {
          this.selectedIds.set(new Set());
          this.loadSeatMap();
        }
      }
    });
  }

  onRelease(): void {
    const current = this.reservation();
    if (!current || this.working()) return;

    this.working.set(true);

    this.reservationService.cancel(current.id).subscribe({
      next: () => this.resetToSelection(),
      error: () => {
        this.working.set(false);
        this.errorMessage.set('The seats could not be released.');
      }
    });
  }

  onPay(): void {
    const current = this.reservation();
    if (!current) return;

    this.router.navigate(['/reservations', current.id, 'payment']);
  }

  /** Recomputes the remaining time and reacts when it runs out. */
  private tick(): void {
    const current = this.reservation();
    if (!current) return;

    const remaining = Math.floor(
      (new Date(current.expiresAt).getTime() - Date.now()) / 1000
    );

    this.secondsLeft.set(remaining);

    if (remaining <= 0) {
      // The API has already released the seats; send the client back.
      this.router.navigate(['/events', this.eventId], {
        queryParams: { expired: 1 }
      });
    }
  }

  private resetToSelection(): void {
    this.working.set(false);
    this.reservation.set(null);
    this.secondsLeft.set(0);
    this.selectedIds.set(new Set());
    this.errorMessage.set(null);
    this.loadSeatMap();
  }
}
