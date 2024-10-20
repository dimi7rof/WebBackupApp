import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Injectable({
  providedIn: 'root',
})
export class SignalRService {
  private hubConnection: signalR.HubConnection | null = null;

  constructor() {}

  // Start the connection to SignalR hub
  public startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/progressHub') // URL to your SignalR hub
      .withAutomaticReconnect() // Automatically reconnect if the connection drops
      .build();

    // Start the connection
    this.hubConnection
      .start()
      .then(() => console.log('SignalR connection started'))
      .catch(err => console.error('Error while starting SignalR connection:', err));
  }

  // Subscribe to a message/event from the hub
  public addProgressListener(callback: (progress: any) => void): void {
    if (this.hubConnection) {
      this.hubConnection.on('ReceiveProgress', callback); // 'ReceiveProgress' is the event name
    }
  }
}
