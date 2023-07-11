import { NotificationType } from "./notification-type";
import { User } from "./user";


export interface Notification {
    id: number;
    createdAt: Date;
    deleted: boolean;
    notificationType: NotificationType;
    notificationTypeId: number;
    read: boolean;
    url: string;
    sender?: User;
    senderId?: number;
    receiverId?: number;
    receiver?: User;
    action: string;
    actionOn: string;
    description: string;
    urgent: boolean;
}