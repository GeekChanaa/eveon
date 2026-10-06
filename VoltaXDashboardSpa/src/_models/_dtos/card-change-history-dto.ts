export interface CardChangeHistoryDto {
  id: number;
  changeSetID: string;
  cardID: number;
  changedByUserID?: number;
  changedBy: string;
  source: 'User' | 'System';
  propertyName: string;
  oldValue?: string;
  newValue?: string;
  changedAtUtc: string;
}
