import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ChatSignalRService {

  private hubConnection?: signalR.HubConnection;

  messages$ = new BehaviorSubject<any | null>(null);

  userJoined$ = new BehaviorSubject<string | null>(null);

  userLeft$ = new BehaviorSubject<string | null>(null);

  startConnection(token: string) {

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(
        `${environment.apiUrl.replace('/api', '')}/hubs/chat`,
        {
          accessTokenFactory: () => token
        }
      )
      .withAutomaticReconnect()
      .build();

    this.registerEvents();

    return this.hubConnection.start();
  }

  private registerEvents() {

    this.hubConnection?.on(
      'ReceiveMessage',
      message => {
        this.messages$.next(message);
      }
    );

    this.hubConnection?.on(
      'UserJoined',
      username => {
        this.userJoined$.next(username);
      }
    );

    this.hubConnection?.on(
      'UserLeft',
      username => {
        this.userLeft$.next(username);
      }
    );
  }

  joinRoom(roomId: number) {
    return this.hubConnection?.invoke(
      'JoinRoom',
      roomId
    );
  }

  leaveRoom(roomId: number) {
    return this.hubConnection?.invoke(
      'LeaveRoom',
      roomId
    );
  }

  sendMessage(
    roomId: number,
    content: string
  ) {
    return this.hubConnection?.invoke(
      'SendMessage',
      roomId,
      content
    );
  }

  stopConnection() {
    return this.hubConnection?.stop();
  }
}