import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { interval } from 'rxjs';
import { Reservation } from '../../models/reservation.model';
import { ReservationService } from '../../services/reservation.service';
import { cardNumberValidator, expiryValidator } from '../../authelpers/card.validators';

interface PaymentMethod {
  id: string;
  label: string;
  needsCard: boolean;
  hint: string;
}

@Component({
  selector: 'payment',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, DatePipe, DecimalPipe],
  templateUrl: './payment.component.html',
  styleUrls: ['./payment.component.css']
})
export class PaymentComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private formBuilder = inject(FormBuilder);
  private reservationService = inject(ReservationService);
  private destroyRef = inject(DestroyRef);

  readonly reservationId = Number(this.route.snapshot.paramMap.get('id'));

  readonly reservation = signal<Reservation | null>(null);
  readonly loadFailed = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly paying = signal(false);
  readonly secondsLeft = signal(0);

  readonly methods: PaymentMethod[] = [
    { id: 'Visa', label: 'Visa', needsCard: true, hint: 'Test number: 4242 4242 4242 4242' },
    { id: 'Mastercard', label: 'Mastercard', needsCard: true, hint: 'Test number: 5555 5555 5555 4444' },
    { id: 'Bancontact', label: 'Bancontact', needsCard: true, hint: 'Your Belgian debit card.' },
    { id: 'QrCode', label: 'QR code', needsCard: false, hint: 'Scan the code with your banking app.' }
  ];

  readonly selectedMethod = signal<PaymentMethod>(this.methods[0]);

  readonly form = this.formBuilder.nonNullable.group({
    holder: ['', [Validators.required, Validators.minLength(3)]],
    cardNumber: ['', [Validators.required, cardNumberValidator]],
    expiry: ['', [Validators.required, expiryValidator]],
    cvc: ['', [Validators.required, Validators.pattern(/^\d{3,4}$/)]]
  });

  /** Card fields only matter for the card methods. */
  readonly formState = signal(this.form.status);

  readonly canPay = computed(() => {
    if (this.paying() || this.reservation() === null) return false;
    if (!this.selectedMethod().needsCard) return true;

    return this.formState() === 'VALID';
  });

  readonly countdown = computed(() => {
    const seconds = Math.max(0, this.secondsLeft());
    const minutes = Math.floor(seconds / 60);
    const rest = seconds % 60;
    return `${minutes}:${rest.toString().padStart(2, '0')}`;
  });

  constructor() {
    this.reservationService.getById(this.reservationId).subscribe({
      next: reservation => {
        this.reservation.set(reservation);
        this.tick();
      },
      error: () => this.loadFailed.set(true)
    });

    // Keep the button in step with the form.
    this.form.statusChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(status => this.formState.set(status));

    interval(1000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.tick());
  }

  onSelectMethod(method: PaymentMethod): void {
    this.selectedMethod.set(method);
    this.errorMessage.set(null);
  }

  /** Formats as the user types: 4242 4242 4242 4242. */
  onCardNumberInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const digits = input.value.replace(/\D/g, '').slice(0, 16);
    const spaced = digits.replace(/(.{4})/g, '$1 ').trim();

    this.form.controls.cardNumber.setValue(spaced);
    input.value = spaced;
  }

  /** Inserts the slash: 09/28. */
  onExpiryInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const digits = input.value.replace(/\D/g, '').slice(0, 4);
    const value = digits.length > 2 ? `${digits.slice(0, 2)}/${digits.slice(2)}` : digits;

    this.form.controls.expiry.setValue(value);
    input.value = value;
  }

  onPay(): void {
    if (!this.canPay()) {
      this.form.markAllAsTouched();
      return;
    }

    this.paying.set(true);
    this.errorMessage.set(null);

    const method = this.selectedMethod();

    // Only the last four digits are sent: the rest never leaves the browser.
    const cardLastFour = method.needsCard
      ? this.form.controls.cardNumber.value.replace(/\s+/g, '').slice(-4)
      : null;

    this.reservationService.pay(this.reservationId, { method: method.id, cardLastFour }).subscribe({
      next: () => this.router.navigate(['/profile'], { queryParams: { paid: 1 } }),
      error: response => {
        this.paying.set(false);
        this.errorMessage.set(
          response.error?.message ?? 'The payment could not be completed. Try again.'
        );
      }
    });
  }

  private tick(): void {
    const current = this.reservation();
    if (!current || current.status !== 'Pending') return;

    const remaining = Math.floor(
      (new Date(current.expiresAt).getTime() - Date.now()) / 1000
    );

    this.secondsLeft.set(remaining);

    if (remaining <= 0) {
      this.router.navigate(['/events', current.eventId], { queryParams: { expired: 1 } });
    }
  }
}
