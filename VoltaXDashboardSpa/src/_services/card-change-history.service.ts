import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CardChangeHistoryDto } from 'src/_models/_dtos/card-change-history-dto';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';

@Injectable({ providedIn: 'root' })
export class CardChangeHistoryService extends AbstractService<CardChangeHistoryDto> {
  constructor(http: HttpClient) {
    super(http, environment.apiUrl + '/api/CardChangeHistory/');
  }

  getForCard(cardID: number, page?: number, itemsPerPage?: number, itemParams?: any) {
    return this.getAll(page, itemsPerPage, itemParams, `card/${cardID}`);
  }
}
