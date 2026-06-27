import { Component, OnInit, inject } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { ChatApiService } from '../../services/chat-api-service';
import { ChatSignalRService } from '../../../../core/services/signalr';
import { ChatStore } from '../../services/chat.store';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './chat.html',
})
export class Chat implements OnInit {
  private api = inject(ChatApiService);
  private signalr = inject(ChatSignalRService);
  store = inject(ChatStore);

  roomId = 13;

  message = '';

  async ngOnInit() {
    const token = localStorage.getItem('masarak_access_token')!;

    await this.signalr.startConnection(token);

    await this.signalr.joinRoom(this.roomId);

    this.loadMessages();

    this.signalr.messages$.subscribe((msg) => {
      if (!msg) return;

      this.store.addMessage(msg);
    });
  }

  loadMessages() {
    this.api.getMessages(this.roomId).subscribe({
      next: (res) => {
        console.log('MESSAGES', res);
        this.store.setMessages(res.items);
      },
      error: (err) => {
        console.error('LOAD ERROR', err);
      },
    });
  }

  send() {
    if (!this.message.trim()) return;

    this.signalr.sendMessage(this.roomId, this.message);

    this.message = '';
  }
}
