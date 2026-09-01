import { Component, computed, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin, of, switchMap } from 'rxjs';
import { PricingZone, SeatBlock, VenueListItem } from '../../../models/admin.model';
import { AdminService } from '../../../services/admin.service';

/** The part of a seat block the layout check looks at. */
interface BlockValue {
  pricingZoneId: number;
  firstRow: string;
  rowCount: number;
  seatsPerRow: number;
}

@Component({
  selector: 'venue-admin',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './venue-admin.component.html',
  styleUrls: ['./venue-admin.component.css']
})
export class VenueAdminComponent {
  private formBuilder = inject(FormBuilder);
  private admin = inject(AdminService);

  readonly venues = signal<VenueListItem[]>([]);
  readonly zones = signal<PricingZone[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly working = signal(false);

  /** Null when creating. Editing only changes name and city: seats stay put. */
  readonly editingId = signal<number | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    city: [''],
    blocks: this.formBuilder.array<ReturnType<VenueAdminComponent['newBlock']>>([])
  });

  get blocks(): FormArray {
    return this.form.controls.blocks as unknown as FormArray;
  }

  /**
   * A mirror of the block values. computed() cannot track a FormArray, so the
   * values are copied into a signal every time the form changes.
   */
  readonly blockValues = signal<BlockValue[]>([]);

  /** Live capacity: the sum of every block, so the number always matches reality. */
  readonly plannedCapacity = computed(() =>
    this.blockValues().reduce(
      (sum, block) => sum + (Number(block.rowCount) || 0) * (Number(block.seatsPerRow) || 0),
      0
    )
  );

  /** Null when the layout is fine, otherwise why it is not. */
  readonly layoutProblem = computed<string | null>(() => {
    if (this.editingId() !== null) return null;

    const used = new Set<string>();

    for (const block of this.blockValues()) {
      if (!Number(block.pricingZoneId)) {
        return 'Pick a pricing zone for every block.';
      }

      const start = (block.firstRow ?? '').charCodeAt(0);
      const rows = Number(block.rowCount) || 0;

      if (start + rows - 1 > 'Z'.charCodeAt(0)) {
        return 'A block runs past row Z. Use fewer rows or start it earlier.';
      }

      for (let r = 0; r < rows; r++) {
        const letter = String.fromCharCode(start + r);

        if (used.has(letter)) {
          return 'Two blocks use the same rows. Press "Auto-letter rows" to lay them out end to end.';
        }

        used.add(letter);
      }
    }

    return null;
  });

  readonly canSubmit = computed(() =>
    this.editingId() !== null ||
    (this.plannedCapacity() > 0 && this.layoutProblem() === null)
  );

  constructor() {
    this.loadVenues();

    this.admin.getZones().subscribe({
      next: zones => this.zones.set(zones),
      error: () => this.errorMessage.set('The pricing zones could not be loaded.')
    });

    // Keep the mirror in step with the form.
    this.blocks.valueChanges.subscribe(() => this.syncBlockValues());

    this.onAddBlock();
  }

  private newBlock() {
    return this.formBuilder.nonNullable.group({
      pricingZoneId: [0, [Validators.required, Validators.min(1)]],
      firstRow: ['A', [Validators.required, Validators.pattern(/^[A-Z]$/)]],
      rowCount: [5, [Validators.required, Validators.min(1), Validators.max(26)]],
      seatsPerRow: [10, [Validators.required, Validators.min(1), Validators.max(100)]]
    });
  }

  private syncBlockValues(): void {
    this.blockValues.set(this.blocks.controls.map(control => control.value as BlockValue));
  }

  private loadVenues(): void {
    this.admin.getVenues().subscribe({
      next: venues => this.venues.set(venues),
      error: () => this.errorMessage.set('The venues could not be loaded.')
    });
  }

  onAddBlock(): void {
    this.blocks.push(this.newBlock());
    this.syncBlockValues();
  }

  onRemoveBlock(index: number): void {
    this.blocks.removeAt(index);
    this.syncBlockValues();
  }

  /** Rows are laid out one block after another: A-E, then F-J, and so on. */
  onSuggestRows(): void {
    let next = 'A'.charCodeAt(0);

    for (const control of this.blocks.controls) {
      control.patchValue({ firstRow: String.fromCharCode(next) }, { emitEvent: false });
      next += Number(control.value.rowCount) || 0;
    }

    this.syncBlockValues();
  }

  onEdit(venue: VenueListItem): void {
    this.editingId.set(venue.id);
    this.form.patchValue({ name: venue.name, city: venue.city ?? '' });
    this.blocks.clear();
    this.syncBlockValues();
  }

  onCancelEdit(): void {
    this.editingId.set(null);
    this.form.reset({ name: '', city: '' });
    this.blocks.clear();
    this.onAddBlock();
  }

  onSubmit(): void {
    if (this.form.invalid || this.working() || !this.canSubmit()) {
      this.form.markAllAsTouched();
      return;
    }

    this.working.set(true);
    this.errorMessage.set(null);

    const { name, city } = this.form.getRawValue();
    const editingId = this.editingId();

    if (editingId !== null) {
      // Editing keeps the existing seats, so capacity stays as it is.
      const venue = this.venues().find(v => v.id === editingId);

      this.admin.updateVenue(editingId, {
        name,
        city: city || null,
        capacity: venue?.seatCount || 1
      }).subscribe({
        next: () => this.finish(),
        error: response => this.fail(response, 'The venue could not be saved.')
      });
      return;
    }

    const blocks: SeatBlock[] = this.blockValues().map(block => ({
      ...block,
      firstSeatNumber: 1
    }));

    // Create the venue, then generate its seat blocks one call each.
    this.admin.createVenue({ name, city: city || null, capacity: this.plannedCapacity() })
      .pipe(
        switchMap(venue =>
          blocks.length === 0
            ? of(null)
            : forkJoin(blocks.map(block => this.admin.addSeatBlock(venue.id, block)))
        )
      )
      .subscribe({
        next: () => this.finish(),
        error: response => this.fail(response, 'The venue could not be created.')
      });
  }

  onDelete(venue: VenueListItem): void {
    if (!confirm(`Delete "${venue.name}" and all its seats?`)) return;

    this.errorMessage.set(null);

    this.admin.deleteVenue(venue.id).subscribe({
      next: () => this.loadVenues(),
      // 409: an event still uses this venue.
      error: response => this.errorMessage.set(
        response.error?.message ?? 'The venue could not be deleted.'
      )
    });
  }

  private finish(): void {
    this.working.set(false);
    this.onCancelEdit();
    this.loadVenues();
  }

  private fail(response: { error?: { message?: string } }, fallback: string): void {
    this.working.set(false);
    this.errorMessage.set(response.error?.message ?? fallback);
  }
}
