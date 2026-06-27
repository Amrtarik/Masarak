import { Injectable, signal } from '@angular/core';
import { ChatMessage } from '../models/message';

@Injectable({
  providedIn: 'root'
})
export class ChatStore {

  messages = signal<ChatMessage[]>([]);

  setMessages(messages: ChatMessage[]) {
    this.messages.set(messages);
  }

  addMessage(message: ChatMessage) {

    this.messages.update(
      msgs => [...msgs, message]
    );
  }

  clear() {
    this.messages.set([]);
  }
}