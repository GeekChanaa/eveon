import { Component, Input } from '@angular/core';
import { CardChangeHistoryService } from 'src/_services/card-change-history.service';

@Component({
  selector: 'app-charging-card-history',
  templateUrl: './charging-card-history.component.html',
  styleUrls: ['./charging-card-history.component.sass']
})
export class ChargingCardHistoryComponent {
  @Input() cardID = 0;

  readonly fields = ['changedAtUtc', 'changedBy', 'propertyName', 'oldValue', 'newValue'];

  constructor(private historyService: CardChangeHistoryService) {}

  getHistoryObservable = (page?: number, itemsPerPage?: number, itemParams?: any) =>
    this.historyService.getForCard(this.cardID, page, itemsPerPage, itemParams);
}
