import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'payment',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './payment.component.html',
  styleUrls: ['./payment.component.css']
})
export class PaymentComponent {
  private route = inject(ActivatedRoute);

  // Placeholder: the fake payment form and the ticket PDF come next.
  readonly reservationId = Number(this.route.snapshot.paramMap.get('id'));
}
