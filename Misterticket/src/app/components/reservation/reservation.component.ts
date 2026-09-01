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

  /** The basket: created by the first click, gone when the last seat is given back. */
  readonly reservation = signal<Reservation | null>(null);
  readonly secondsLeft = signal(0);

  /** Seats with a request in flight, so a double click cannot fire twice. */
  readonly busySeatIds = signal<ReadonlySet<number>>(new Set());

  /** Ids currently in the basket. */
  readonly heldSeatIds = computed<ReadonlySet<number>>(() =>
    new Set(this.reservation()?.seats.map(seat => seat.eventSeatId) ?? [])
  );

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

  /** "x2 VIP, x3 Balcon", straight from the basket the API returned. */
  readonly tally = computed<ZoneTally[]>(() => {
    const seats = this.reservation()?.seats ?? [];
    const byZone = new Map<string, ZoneTally>();

    for (const seat of seats) {
      const line = byZone.get(seat.pricingZoneName) ?? {
        zoneName: seat.pricingZoneName,
        zoneColor: '',
        count: 0,
        subtotal: 0
      };
      line.count += 1;
      line.subtotal += seat.price;
      byZone.set(seat.pricingZoneName, line);
    }

    return [...byZone.values()].sort((a, b) => b.subtotal - a.subtotal);
  });

  /** The API keeps the total, so there is one source of truth for the price. */
  readonly total = computed(() => this.reservation()?.totalAmount ?? 0);

  readonly countdown = computed(() => {
    const seconds = Math.max(0, this.secondsLeft());
    const minutes = Math.floor(seconds / 60);
    const rest = seconds % 60;
    return `${minutes}:${rest.toString().padStart(2, '0')}`;
  });

  constructor() {
    this.loadSeatMap();

    // A basket may already exist: the client reloaded or came back to the page.
    this.reservationService.getActive(this.eventId).subscribe({
      next: active => {
        this.reservation.set(active);
        this.tick();
      },
      error: () => { /* no basket is not an error worth showing */ }
    });

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
    const mine = this.heldSeatIds();

    this.seatMap.set({
      ...map,
      seats: map.seats.map(seat => {
        const status = changes.get(seat.id);
        if (!status) return seat;

        // Our own seats keep their "mine" flag; a freed seat loses it.
        return { ...seat, status, isMine: status === 'Free' ? false : mine.has(seat.id) };
      })
    });
  }

  private loadSeatMap(): void {
    this.reservationService.getSeatMap(this.eventId).subscribe({
      next: map => this.seatMap.set(map),
      error: response => {
        console.error('Seat map failed:', response.status, response.error);
        this.loadFailed.set(true);
      }
    });
  }

  isSelected(seat: EventSeatItem): boolean {
    return this.heldSeatIds().has(seat.id);
  }

  isBusy(seat: EventSeatItem): boolean {
    return this.busySeatIds().has(seat.id);
  }

  /** Free seats can be taken; the ones already in the basket can be given back. */
  isSelectable(seat: EventSeatItem): boolean {
    return seat.status === 'Free' || this.isSelected(seat);
  }

  /**
   * One click, one server call: the seat changes status immediately for
   * everyone, not only for this client.
   */
  onSeatClick(seat: EventSeatItem): void {
    if (!this.isSelectable(seat) || this.isBusy(seat)) return;

    this.errorMessage.set(null);
    this.markBusy(seat.id, true);

    const done = () => this.markBusy(seat.id, false);

    if (this.isSelected(seat)) {
      this.reservationService.releaseSeat(this.eventId, seat.id).subscribe({
        next: reservation => {
          this.reservation.set(reservation);
          if (!reservation) this.secondsLeft.set(0);
          done();
        },
        error: response => {
          done();
          this.fail(response, 'That seat could not be released.');
        }
      });
      return;
    }

    this.reservationService.claimSeat(this.eventId, seat.id).subscribe({
      next: reservation => {
        this.reservation.set(reservation);
        this.tick();
        done();
      },
      error: response => {
        done();
        // 409: someone else won the race. SignalR has already turned it orange.
        this.fail(response, 'That seat could not be reserved.');
      }
    });
  }

  /** Gives the whole basket back. */
  onRelease(): void {
    const current = this.reservation();
    if (!current) return;

    this.reservationService.cancel(current.id).subscribe({
      next: () => {
        this.reservation.set(null);
        this.secondsLeft.set(0);
        this.errorMessage.set(null);
      },
      error: () => this.errorMessage.set('The seats could not be released.')
    });
  }

  onPay(): void {
    const current = this.reservation();
    if (!current) return;

    this.router.navigate(['/reservations', current.id, 'payment']);
  }

  private markBusy(seatId: number, busy: boolean): void {
    const next = new Set(this.busySeatIds());
    busy ? next.add(seatId) : next.delete(seatId);
    this.busySeatIds.set(next);
  }

  private fail(response: { status: number; error?: { message?: string } }, fallback: string): void {
    this.errorMessage.set(response.error?.message ?? fallback);
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
      this.reservation.set(null);
      this.router.navigate(['/events', this.eventId], {
        queryParams: { expired: 1 }
      });
    }
  }
}
