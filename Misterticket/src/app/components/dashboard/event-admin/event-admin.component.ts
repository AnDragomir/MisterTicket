import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { VenueListItem } from '../../../models/admin.model';
import { EventListItem } from '../../../models/event.model';
import { AdminService } from '../../../services/admin.service';

@Component({
  selector: 'event-admin',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './event-admin.component.html',
  styleUrls: ['./event-admin.component.css']
})
export class EventAdminComponent {
  private formBuilder = inject(FormBuilder);
  private admin = inject(AdminService);

  readonly events = signal<EventListItem[]>([]);
  readonly venues = signal<VenueListItem[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly working = signal(false);

  readonly editingId = signal<number | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    description: [''],
    // datetime-local gives "2026-09-01T20:00"
    startsAt: ['', Validators.required],
    venueId: [0, [Validators.required, Validators.min(1)]]
  });

  constructor() {
    this.load();

    this.admin.getVenues().subscribe({
      next: venues => this.venues.set(venues.filter(v => v.seatCount > 0)),
      error: () => this.errorMessage.set('The venues could not be loaded.')
    });
  }

  private load(): void {
    this.admin.getEvents().subscribe({
      next: events => this.events.set(events),
      error: () => this.errorMessage.set('The events could not be loaded.')
    });
  }

  onEdit(event: EventListItem): void {
    this.editingId.set(event.id);
    this.form.patchValue({
      name: event.name,
      startsAt: event.startsAt.slice(0, 16),   // trim seconds and zone
      venueId: event.venueId
    });
    this.form.controls.venueId.disable();
  }

  onCancelEdit(): void {
    this.editingId.set(null);
    this.form.enable();
    this.form.reset({ name: '', description: '', startsAt: '', venueId: 0 });
  }

  onSubmit(): void {
    if (this.form.invalid || this.working()) {
      this.form.markAllAsTouched();
      return;
    }

    this.working.set(true);
    this.errorMessage.set(null);

    const value = this.form.getRawValue();
    const id = this.editingId();

    const payload = {
      name: value.name,
      description: value.description || null,
      startsAt: new Date(value.startsAt).toISOString()
    };

    const request = id === null
      // Creating also generates one EventSeat per seat of the venue.
      ? this.admin.createEvent({ ...payload, venueId: Number(value.venueId) })
      : this.admin.updateEvent(id, payload);

    request.subscribe({
      next: () => {
        this.working.set(false);
        this.onCancelEdit();
        this.load();
      },
      error: response => {
        this.working.set(false);
        this.errorMessage.set(response.error?.message ?? 'The event could not be saved.');
      }
    });
  }

  onDelete(event: EventListItem): void {
    if (!confirm(`Delete "${event.name}"?`)) return;

    this.errorMessage.set(null);

    this.admin.deleteEvent(event.id).subscribe({
      next: () => this.load(),
      // 409: the event already has reservations.
      error: response => this.errorMessage.set(
        response.error?.message ?? 'The event could not be deleted.'
      )
    });
  }
}
