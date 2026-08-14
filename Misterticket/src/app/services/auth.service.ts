import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, UserAccount } from '../models/auth.model';
import { API_BASE_URL } from '../api.config';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private readonly baseUrl = API_BASE_URL;

  private readonly tokenKey = 'misterticket.token';
  private readonly userKey = 'misterticket.user';
  private readonly expiryKey = 'misterticket.expiresAt';

  /** Current user, or null when nobody is signed in. */
  readonly currentUser = signal<UserAccount | null>(this.restoreSession());

  readonly isLoggedIn = computed(() => this.currentUser() !== null);

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/auth/login`, request)
      .pipe(tap(response => this.startSession(response)));
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/auth/register`, request)
      .pipe(tap(response => this.startSession(response)));
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    localStorage.removeItem(this.expiryKey);
    this.currentUser.set(null);
  }

  /** Read by the interceptor to add the Authorization header. */
  get token(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  private startSession(response: AuthResponse): void {
    localStorage.setItem(this.tokenKey, response.token);
    localStorage.setItem(this.userKey, JSON.stringify(response.user));
    localStorage.setItem(this.expiryKey, response.expiresAt);
    this.currentUser.set(response.user);
  }

  /**
   * Called once at startup so a page reload keeps the user signed in.
   * An expired token is dropped straight away instead of waiting for a 401.
   */
  private restoreSession(): UserAccount | null {
    const token = localStorage.getItem(this.tokenKey);
    const rawUser = localStorage.getItem(this.userKey);
    const expiresAt = localStorage.getItem(this.expiryKey);

    if (!token || !rawUser || !expiresAt) {
      return null;
    }

    if (new Date(expiresAt) <= new Date()) {
      this.logout();
      return null;
    }

    try {
      return JSON.parse(rawUser) as UserAccount;
    } catch {
      this.logout();
      return null;
    }
  }
}
