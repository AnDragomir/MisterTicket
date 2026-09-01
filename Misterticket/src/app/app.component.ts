import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  private authService = inject(AuthService);

  readonly currentUser = this.authService.currentUser;

  /** The dashboard link only makes sense for staff. */
  readonly isStaff = computed(() => {
    const role = this.currentUser()?.role;
    return role === 'Admin' || role === 'Organizer';
  });
}
