import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { interval } from 'rxjs';
import { Reservation } from '../../models/reservation.model';
import { AuthService } from '../../services/auth.service';
import { ReservationService } from '../../services/reservation.service';

@Component({
  selector: 'profile',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent {
  private authService = inject(AuthService);
  private reservationService = inject(ReservationService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  // The guard guarantees a user is signed in when this page renders.
  readonly user = this.authService.currentUser;

  readonly reservations = signal<Reservation[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly showCancelled = signal(false);

  /** Reservation whose PDF is being fetched, so the button can say so. */
  readonly downloadingId = signal<number | null>(null);

  /** Set when the client just came back from a successful payment. */
  readonly justPaid = signal(
    inject(ActivatedRoute).snapshot.queryParamMap.get('paid') === '1'
  );

  /** Ticks every second so the countdowns stay honest. */
  private readonly now = signal(Date.now());

  /** Still holding seats and still in time: these can be paid or cancelled. */
  readonly pending = computed(() =>
    this.reservations().filter(r =>
      r.status === 'Pending' && new Date(r.expiresAt).getTime() > this.now()
    )
  );

  readonly paid = computed(() =>
    this.reservations().filter(r => r.status === 'Paid')
  );

  /**
   * Cancelled by the client, or released when the hold ran out. A pending
   * reservation past its deadline belongs here too: the sweeper has not run yet.
   */
  readonly cancelled = computed(() =>
    this.reservations().filter(r =>
      r.status === 'Cancelled' ||
      (r.status === 'Pending' && new Date(r.expiresAt).getTime() <= this.now())
    )
  );

  constructor() {
    this.load();

    interval(1000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.now.set(Date.now()));
  }

  private load(): void {
    this.loading.set(true);

    this.reservationService.getMine().subscribe({
      next: reservations => {
        this.reservations.set(reservations);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('Your reservations could not be loaded.');
      }
    });
  }

  /** "12:04" left on a pending hold. */
  countdown(reservation: Reservation): string {
    const seconds = Math.max(
      0,
      Math.floor((new Date(reservation.expiresAt).getTime() - this.now()) / 1000)
    );

    const minutes = Math.floor(seconds / 60);
    const rest = seconds % 60;
    return `${minutes}:${rest.toString().padStart(2, '0')}`;
  }

  isUrgent(reservation: Reservation): boolean {
    return new Date(reservation.expiresAt).getTime() - this.now() < 120_000;
  }

  /** "A3, A4, B7" for the seat line. */
  seatLabels(reservation: Reservation): string {
    return reservation.seats
      .map(seat => `${seat.rowLabel}${seat.number}`)
      .join(', ');
  }

  onPay(reservation: Reservation): void {
    this.router.navigate(['/reservations', reservation.id, 'payment']);
  }

  onCancel(reservation: Reservation): void {
    if (!confirm(`Cancel this reservation and release ${reservation.seats.length} seat(s)?`)) {
      return;
    }

    this.errorMessage.set(null);

    this.reservationService.cancel(reservation.id).subscribe({
      next: () => this.load(),
      error: response => this.errorMessage.set(
        // 409: already paid, so it cannot be cancelled here.
        response.error?.message ?? 'The reservation could not be cancelled.'
      )
    });
  }

  /**
   * Fetches the PDF as a blob and hands it to the browser. It cannot be a plain
   * link: the endpoint needs the Authorization header the interceptor adds.
   */
  onDownloadTicket(reservation: Reservation): void {
    this.downloadingId.set(reservation.id);
    this.errorMessage.set(null);

    this.reservationService.downloadTicket(reservation.id).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');

        link.href = url;
        link.download = `misterticket-${reservation.id}.pdf`;
        link.click();

        // Let go of the blob once the browser has taken it.
        URL.revokeObjectURL(url);
        this.downloadingId.set(null);
      },
      error: () => {
        this.downloadingId.set(null);
        this.errorMessage.set('The ticket could not be downloaded.');
      }
    });
  }

  onToggleCancelled(): void {
    this.showCancelled.update(shown => !shown);
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/');
  }
}
