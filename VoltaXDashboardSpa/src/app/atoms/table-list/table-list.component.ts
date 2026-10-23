import { AccessService, DASHBOARD_RESOURCES } from 'src/_services/access.service';
import { formatDate } from '@angular/common';
import { HttpResponse } from '@angular/common/http';
import { Component, ContentChild, DestroyRef, ElementRef, EventEmitter, HostListener, Input, OnInit, Output, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subscription } from 'rxjs';
import { AppTableCustomButtonDirective } from 'src/_directives/table-custom-button.directive';
import { saveDownload } from 'src/_helpers/download';
import { PaginatedResult, Pagination } from 'src/_models/pagination';
import { ConfirmService } from 'src/_services/confirm.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { LanguageService } from 'src/_services/language.service';
import { ToastService } from 'src/_services/toast.service';

/**
 * The dashboard's generic server-paged list: search, filters (projected), visible columns, sort, delete (confirmed),
 * optional CSV export of the filtered rows. Shows loading, empty and error (with retry) states.
 * Texts are translation keys: titleKey, filterTitleKey, searchByPlaceHolder, fieldLabelPrefix (+ field name).
 */
@Component({
  selector: 'app-table-list',
  templateUrl: './table-list.component.html',
  styleUrls: ['./table-list.component.sass']
})
export class TableListComponent implements OnInit {
  @ContentChild(AppTableCustomButtonDirective, { static: false })
  customButtonTemplate!: AppTableCustomButtonDirective;
  /** Identifies the list (enum formatting, row links); shown as title only without titleKey. */
  @Input() name: string = "";
  @Input() names: string = "";
  /** Translation key of the page title. */
  @Input() titleKey: string = "";
  /** Translation key of the filter panel title (default operator.table.filtering). */
  @Input() filterTitleKey: string = "operator.table.filtering";
  /** Column headers: translate(fieldLabelPrefix + '.' + field) when set, otherwise the field name spaced. */
  @Input() fieldLabelPrefix: string = "";
  @Input() fields: string[] = [];
  @Input() searchByAttributes: string[] = [];
  @Input() data: any[] = [];
  @Input() deletable: boolean = true;
  @Input() displayable: boolean = true;
  @Input() searchable: boolean = true;
  @Input() isLoading: boolean = true;
  @Input() breadcrumbs: { label: string, link?: string }[] = [];
  @Input() createLink: string = "/create";
  @Input() routeName : string = "";
  /** Translation key (or text) of the search box placeholder. */
  @Input() searchByPlaceHolder: string = "operator.table.searchPlaceholder";
  @Input() getItemsObservable! : (page?: number, itemsPerPage?: number, itemParams?: any, endpoint?: string) => Observable<PaginatedResult<any[]>>
  @Input() deleteItemObservable! : (id : number) => Observable<any>;
  @Input() updateItemObservable! : (id : number, model : any) => Observable<any>;
  /** When set, an "Export CSV" button downloads the rows matching the current search, filters and order (every page). */
  @Input() exportObservable?: (itemParams: any) => Observable<HttpResponse<Blob>>;
  @Input() exportFileName: string = "eveon-export.csv";
  @Output() applyFiltersEvent: EventEmitter<void> = new EventEmitter<void>();
  @Output() resetFiltersEvent: EventEmitter<void> = new EventEmitter<void>();
  @Output() displayEvent: EventEmitter<number> = new EventEmitter<number>();
  @Output() updateEvent: EventEmitter<number> = new EventEmitter<number>();
  @Output() sortEvent: EventEmitter<string> = new EventEmitter<string>();

  /** The last load failed: the error state with a retry replaces the table. */
  loadError = false;
  exporting = false;
  private loadSubscription?: Subscription;
  private readonly destroyRef = inject(DestroyRef);

  @HostListener('document:click', ['$event'])
  clickOutside(event: MouseEvent) {
    const columnsControl = this._eRef.nativeElement.querySelector('.list-columns-control');
    if (!columnsControl?.contains(event.target)) {
      this.displayMenu = false;
    }
  }
  constructor(
    public access: AccessService,
    private _enumMappingService : EnumMappingService,
    private _router : Router,
    private _eRef : ElementRef,
    private _confirm : ConfirmService,
    private _toast : ToastService,
    private _translate : TranslateService,
    private _language : LanguageService
  ) { }

  toggleMenu(event: MouseEvent) {
    event.stopPropagation();
    this.isActive = false;
    this.displayMenu = !this.displayMenu;
  }

  get canDelete(): boolean {
    return this.access.isAdmin || this.access.can('Delete' + (DASHBOARD_RESOURCES[this.routeName] || '__denied'));
  }

  delete(id : number){
    if (!this.canDelete) return;
    this.deleteItemObservable(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        // The last row of a page is gone: show the previous page.
        if (this.data?.length === 1 && this.currentPage > 1) this.currentPage--;
        this.getAll();
        this._toast.success('operator.table.deleted');
      },
      error: (error) => this._toast.apiError(error, 'operator.table.deleteFailed')
    });
  }

  paginationPages: any[] = [];

  sortedColumn : string= "";
  sortedDirection : string = "ASC";

  pagination: Pagination = {
    currentPage: 0,
    itemsPerPage: 0,
    totalItems: 0,
    totalPages: 0
  }

  itemParams : any = {};

  itemsPerPage: number = 20;
  currentPage: number = 1;

  searchValue: string = "";

  isActive: boolean = false;

  fieldShown: { [key: string]: Boolean } = {};

  displayMenu: Boolean = false;

  ngOnInit() {
    this.getAll();
    this.fields.forEach((field, i) => this.fieldShown[field] = i < 5);
  }

  getAll(){
    this.isLoading = true;
    this.loadError = false;
    // A newer request (search, page, sort) replaces one still running.
    this.loadSubscription?.unsubscribe();
    this.loadSubscription = this.getItemsObservable(this.currentPage, this.itemsPerPage, this.itemParams)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.isLoading = false;
          this.data = data?.result ?? [];
          if(data?.pagination){
            this.pagination = data.pagination;
            this.generatePaginationLinks();
          }
        },
        error: () => {
          this.isLoading = false;
          this.loadError = true;
        }
      });
  }

  /** Header of a column, in the interface language. */
  fieldLabel(field: string): string {
    if (this.fieldLabelPrefix) {
      const key = `${this.fieldLabelPrefix}.${field}`;
      const text = this._translate.instant(key);
      if (text !== key) return text;
    }
    return field.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/^./, c => c.toUpperCase());
  }

  nextPage() {
    if (this.currentPage >= this.pagination.totalPages) return;
    this.currentPage++;
    this.getAll();
  }

  previousPage() {
    if (this.currentPage <= 1) return;
    this.currentPage--;
    this.getAll();
  }

  search(){
    if(this.searchValue == null || this.searchValue == ""){
      this.itemParams.SearchBy = [];
      this.itemParams.SearchValue = [];
    }
    else{
      this.itemParams.SearchBy = this.searchByAttributes;
      this.itemParams.SearchValue = this.searchValue;
    }
    this.currentPage = 1;
    this.getAll();
  }

  applyFilters() {
    this.itemParams.FilterValue = [];
    this.itemParams.FilterBy = [];

    for (const key in this.filters) {
      if (this.filters[key] !== null && this.filters[key] !== undefined && this.filters[key] !== '') {
        this.itemParams.FilterValue.push(this.filters[key]);
        this.itemParams.FilterBy.push(this.capitalizeFirstLetter(key));
      }
    }
    this.currentPage = 1;
    this.getAll();
    this.removeActive();
  }

  capitalizeFirstLetter(str: string): string {
    return str.charAt(0).toUpperCase() + str.slice(1);
  }

  @Input() filters : any = {};

  update = (id: number) =>  this.updateEvent.emit(id);

  display(item: any){
    if(this.name == 'charging session'){
      this._router.navigateByUrl("dashboard/connector-realtime/"+item.chargePointID+"/charging-session/"+item.id);
      return;
    }
    if(this.name == 'transaction'){
      this._router.navigateByUrl("dashboard/transactions/"+item.uid);
      return;
    }
    this._router.navigateByUrl("/dashboard/"+this.routeName+"/"+item.id)
  }

  goToPage(page : number | string){
    if (typeof page !== 'number' || page === this.currentPage) return;
    this.currentPage = page;
    this.getAll();
  }

  sort(field : string){
    this.sortedColumn = field;
    if(this.itemParams.orderBy == field){
      if(this.itemParams.reverseOrder == 'y')
        this.itemParams.reverseOrder = 'n'
      else
        this.itemParams.reverseOrder = 'y'
    }
    else{
      this.itemParams.orderBy = field;
      this.itemParams.reverseOrder = 'n'
    }
    this.getAll();
  }

  toggleActive(event: Event): void {
      event.stopPropagation();
      this.displayMenu = false;
      this.isActive = !this.isActive;
  }

  removeActive(): void {
      this.isActive = false;
  }

  /** The rows matching the current search, filters and order, as a CSV file. */
  export(): void {
    if (!this.exportObservable || this.exporting) return;
    this.exporting = true;
    this.exportObservable({ ...this.itemParams }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: response => { this.exporting = false; saveDownload(response, this.exportFileName); },
      error: error => { this.exporting = false; this._toast.apiError(error); }
    });
  }

  formatValue(item: any, field: string): any {
    let value = item[field.charAt(0).toLowerCase() + field.slice(1)];
    if(this.isDateString(value)) {
      return formatDate(new Date(this.asUtc(value)), 'short', this._language.locale);
    } else if(field == 'status' && typeof value === 'number' && this.name == 'charging card') {
      return this._enumMappingService.label("CardStatus", value);
    } else if(field == 'cardType') {
      return this._enumMappingService.label("CardType", value);
    } else if(field == 'status' && typeof value === 'number' && this.name == 'charging station') {
      return this._enumMappingService.label("ChargingStationStatusEnum", value);
    } else if(field == 'category' && typeof value === 'number' && this.name == 'charging station') {
      return this._enumMappingService.label("ChargingStationCategoryEnum", value);
    } else if(field == 'parkingType' && typeof value === 'number' && this.name == 'charging station') {
      return this._enumMappingService.label("ParkingTypeEnum", value);
    } else if(field == 'status' && typeof value === 'number' && this.name == 'charging point') {
      return this._enumMappingService.label("ChargePointStatus", value);
    } else if(field == 'category' && typeof value === 'number' && this.name == 'charging point') {
      return this._enumMappingService.label("ChargePointCategory", value);
    } else if(typeof value === 'boolean') {
      return this._translate.instant(value ? 'common.state.yes' : 'common.state.no');
    } else if(typeof value === 'number' || this.isNumericString(value)) {
      // Integers as they are, decimals with two digits.
      const num = Number(value);
      return Number.isInteger(num) ? num.toString() : num.toFixed(2);
    }
    return value;
  }

  /** ISO date-times from the API (with or without fractional seconds / zone). */
  isDateString(value: any): boolean {
    return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:?\d{2})?$/.test(value);
  }

  isNumericString(value: any): boolean {
    return typeof value === 'string' && !isNaN(Number(value)) && value.includes(".");
  }

  generatePaginationLinks() {
    const currentPage = this.pagination.currentPage;
    const totalPages = this.pagination.totalPages;
    if (totalPages <= 3) {
      this.paginationPages = Array.from({ length: totalPages }, (_, i) => i + 1);
    } else if (currentPage === 1) {
      this.paginationPages = [1, 2, '...', totalPages];
    } else if (currentPage == totalPages) {
      this.paginationPages = [currentPage - 2, currentPage - 1, '...', totalPages];
    } else {
      this.paginationPages = [currentPage - 1, currentPage, '...', totalPages];
    }
  }

  resetFilters(){
    this.resetFiltersEvent.emit();
  }

  /** Deleting always asks first. */
  confirmDelete(itemId: any): void {
    if (!this.canDelete || itemId == null) return;
    this._confirm.ask({ title: 'operator.confirm.deleteTitle', message: 'operator.confirm.deleteMessage', tone: 'danger' })
      .subscribe(() => this.delete(itemId));
  }

  /** Stored date-times without a zone are UTC. */
  private asUtc(value: string): string {
    return /Z$|[+-]\d{2}:?\d{2}$/.test(value) ? value : value + 'Z';
  }
}
