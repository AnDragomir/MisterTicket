import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { HUB_BASE_URL } from '../api.config';
import { SeatsChanged } from '../models/reservation.model';

/**
 * Keeps one SignalR connection to the seat hub and turns the pushes into an
 * observable the components can subscribe to.
 */
@Injectable({ providedIn: 'root' })
export class SeatHubService {
  private connection?: HubConnection;
  private joinedEventId?: number;

  /** Emits every time the server reports seats moving. */
  readonly seatsChanged$ = new Subject<SeatsChanged>();

  /** Opens the connection if needed, then joins the group of one event. */
  async joinEvent(eventId: number): Promise<void> {
    await this.ensureConnected();

    if (this.joinedEventId === eventId) return;

    if (this.joinedEventId !== undefined) {
      await this.connection!.invoke('LeaveEvent', this.joinedEventId);
    }

    await this.connection!.invoke('JoinEvent', eventId);
    this.joinedEventId = eventId;
  }

  async leaveEvent(): Promise<void> {
    if (!this.connection || this.joinedEventId === undefined) return;

    try {
      await this.connection.invoke('LeaveEvent', this.joinedEventId);
    } catch {
      // The connection may already be gone; nothing to clean up then.
    }

    this.joinedEventId = undefined;
  }

  private async ensureConnected(): Promise<void> {
    if (this.connection) {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(`${HUB_BASE_URL}/hubs/seats`)
      .withAutomaticReconnect()          // survives a short network drop
      .configureLogging(LogLevel.Warning)
      .build();

    // The name must match SendAsync("SeatsChanged", ...) on the server.
    this.connection.on('SeatsChanged', (payload: SeatsChanged) => {
      this.seatsChanged$.next(payload);
    });

    // After a reconnect the server has forgotten our group: join again.
    this.connection.onreconnected(async () => {
      if (this.joinedEventId !== undefined) {
        await this.connection!.invoke('JoinEvent', this.joinedEventId);
      }
    });

    await this.connection.start();
  }
}
