import { NotificationType } from "./notification-type";
import { User } from "./user";


export interface NotificationSetting {
    id: number;
    email: boolean;
    user: User;
    userId: number;
    notificationTypeId: number;
    notificationType?: NotificationType;
    urgent: boolean;
    active: boolean;
}