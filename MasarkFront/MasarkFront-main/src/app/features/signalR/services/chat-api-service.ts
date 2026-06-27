import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ChatApiService {
  private http = inject(HttpClient);

  private apiUrl = 'https://localhost:49179/api/chat';

  getRooms() {
    return this.http.get<any[]>(`${this.apiUrl}/rooms`);
  }

  getMessages(roomId: number, page: number = 1, pageSize: number = 50) {
    return this.http.get<any>(
      `${this.apiUrl}/rooms/${roomId}/messages?page=${page}&pageSize=${pageSize}`,
    );
  }

  deleteMessage(messageId: number) {
    return this.http.delete(`${this.apiUrl}/messages/${messageId}`);
  }
}
