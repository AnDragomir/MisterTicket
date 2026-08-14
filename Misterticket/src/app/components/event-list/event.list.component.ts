import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AsyncPipe, DatePipe } from '@angular/common';
import { Observable, catchError, of, tap } from 'rxjs';
import { EventListItem } from '../../models/event.model';
import { EventService } from '../../services/event.service';

@Component({
  selector: 'event-list',
  standalone: true,
  imports: [RouterLink, AsyncPipe, DatePipe],
  templateUrl: './event.list.component.html',
  styleUrls: ['./event.list.component.css']
})
export class EventListComponent {
  private eventService = inject(EventService);

  events$: Observable<EventListItem[]> = this.eventService.getAll().pipe(
    tap(events => console.log('Events received:', events)),
    catchError(error => {
      console.error('Error loading events:', error);
      return of([]);
    })
  );
}
