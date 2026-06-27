export interface ChatRoom {
  chatRoomId: number;
  name: string;
  roomType: string;
  gradeId?: number;
  messageCount: number;
}