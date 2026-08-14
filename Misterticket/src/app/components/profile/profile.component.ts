import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'profile',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  // The guard guarantees a user is signed in when this page renders.
  readonly user = this.authService.currentUser;

  onLogout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/');
  }
}
