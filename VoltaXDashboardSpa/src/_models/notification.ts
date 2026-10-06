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

/** A notification as returned to its receiver by /api/notification and the notification hub. */
export interface DashboardNotification {
    id: number;
    type?: string;
    action?: string;
    description?: string;
    url: string;
    urgent: boolean;
    read: boolean;
    createdAt: string;
}
