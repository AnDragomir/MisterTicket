import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AsyncPipe, DatePipe } from '@angular/common';
import { Observable, catchError, map, of, switchMap } from 'rxjs';
import { EventDetail } from '../../models/event.model';
import { EventService } from '../../services/event.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'event-details',
  standalone: true,
  imports: [RouterLink, AsyncPipe, DatePipe],
  templateUrl: './event.details.component.html',
  styleUrls: ['./event.details.component.css']
})
export class EventDetailsComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private eventService = inject(EventService);
  private authService = inject(AuthService);

  /** Set when the reservation page sent the client back after the hold expired. */
  readonly holdExpired = signal(this.route.snapshot.queryParamMap.get('expired') === '1');

  event$: Observable<EventDetail | null> = this.route.paramMap.pipe(
    map(params => Number(params.get('id'))),
    switchMap(id => this.eventService.getById(id).pipe(
      catchError(() => of(null))
    ))
  );

  /**
   * Signed in: straight to the seat map.
   * Anonymous: to the login page, which sends them back here afterwards.
   */
  onBuyTickets(eventId: number): void {
    const target = `/events/${eventId}/reserve`;

    if (this.authService.isLoggedIn()) {
      this.router.navigateByUrl(target);
      return;
    }

    this.router.navigate(['/login'], { queryParams: { returnUrl: target } });
  }
}
