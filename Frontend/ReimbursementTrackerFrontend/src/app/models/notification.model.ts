export interface CreateNotificationResponseDto {
  notificationId: string;
  userId:         string;
  senderId:       string;
  message:        string;
  description:    string;
  readStatus:     'Unread' | 'Read';
  senderRole:     string;
  reply?:         string;
  createdAt:      string;
}
