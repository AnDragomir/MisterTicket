import { Routes } from '@angular/router';
import { EventListComponent } from './components/event-list/event.list.component';
import { EventDetailsComponent } from './components/event-details/event.details.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { ProfileComponent } from './components/profile/profile.component';
import { ReservationComponent } from './components/reservation/reservation.component';
import { PaymentComponent } from './components/payment/payment.component';
import { authGuard } from './authelpers/auth.guard';

export const routes: Routes = [
  { path: '', component: EventListComponent, title: 'MisterTicket — What\'s on stage' },
  { path: 'events/:id', component: EventDetailsComponent, title: 'MisterTicket — Performance' },

  { path: 'login', component: LoginComponent, title: 'MisterTicket — Sign in' },
  { path: 'register', component: RegisterComponent, title: 'MisterTicket — Create account' },

  // Signed-in only: the guard redirects to /login with a returnUrl.
  {
    path: 'events/:id/reserve',
    component: ReservationComponent,
    canActivate: [authGuard],
    title: 'MisterTicket — Choose your seats'
  },
  {
    path: 'reservations/:id/payment',
    component: PaymentComponent,
    canActivate: [authGuard],
    title: 'MisterTicket — Payment'
  },
  {
    path: 'profile',
    component: ProfileComponent,
    canActivate: [authGuard],
    title: 'MisterTicket — Your account'
  },

  { path: '**', redirectTo: '' }
];
