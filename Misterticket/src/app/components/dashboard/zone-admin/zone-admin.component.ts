import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { PricingZone } from '../../../models/admin.model';
import { AdminService } from '../../../services/admin.service';

@Component({
  selector: 'zone-admin',
  standalone: true,
  imports: [ReactiveFormsModule, DecimalPipe],
  templateUrl: './zone-admin.component.html',
  styleUrls: ['./zone-admin.component.css']
})
export class ZoneAdminComponent {
  private formBuilder = inject(FormBuilder);
  private admin = inject(AdminService);

  readonly zones = signal<PricingZone[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly working = signal(false);

  /** Null when creating, the zone id when editing. */
  readonly editingId = signal<number | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(80)]],
    colorHex: ['#C9A227', [Validators.required, Validators.pattern(/^#[0-9a-fA-F]{6}$/)]],
    basePrice: [25, [Validators.required, Validators.min(0)]]
  });

  constructor() {
    this.load();
  }

  private load(): void {
    this.admin.getZones().subscribe({
      next: zones => this.zones.set(zones),
      error: () => this.errorMessage.set('The zones could not be loaded.')
    });
  }

  onEdit(zone: PricingZone): void {
    this.editingId.set(zone.id);
    this.form.setValue({
      name: zone.name,
      colorHex: zone.colorHex,
      basePrice: zone.basePrice
    });
  }

  onCancelEdit(): void {
    this.editingId.set(null);
    this.form.reset({ name: '', colorHex: '#C9A227', basePrice: 25 });
  }

  onSubmit(): void {
    if (this.form.invalid || this.working()) {
      this.form.markAllAsTouched();
      return;
    }

    this.working.set(true);
    this.errorMessage.set(null);

    const dto = this.form.getRawValue();
    const id = this.editingId();

    const request = id === null
      ? this.admin.createZone(dto)
      : this.admin.updateZone(id, dto);

    request.subscribe({
      next: () => {
        this.working.set(false);
        this.onCancelEdit();
        this.load();
      },
      error: response => {
        this.working.set(false);
        this.errorMessage.set(response.error?.message ?? 'The zone could not be saved.');
      }
    });
  }

  onDelete(zone: PricingZone): void {
    if (!confirm(`Delete the zone "${zone.name}"?`)) return;

    this.errorMessage.set(null);

    this.admin.deleteZone(zone.id).subscribe({
      next: () => this.load(),
      error: response => this.errorMessage.set(
        // 409: seats in some venue still use this zone.
        response.error?.message ?? 'The zone could not be deleted.'
      )
    });
  }
}
