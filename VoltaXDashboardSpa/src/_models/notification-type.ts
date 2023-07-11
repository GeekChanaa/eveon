export interface NotificationType {
    id: number;
    description: string;
    name: string;
    forAdmins : boolean;
    forCustomers : boolean;
    forPartners : boolean;
}