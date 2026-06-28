export interface ChatMessage {
  messageId: number;
  chatRoomId: number;
  senderUserId: number;
  senderName: string;
  content: string;
  sentAt: string;
  isDeleted: boolean;
}
