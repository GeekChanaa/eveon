import { Component, OnInit } from '@angular/core';
import { AccessService } from 'src/_services/access.service';

interface QuickLink {
  title: string;
  description: string;
  icon: string;
  url: string;
}

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.sass']
})
export class HomeComponent implements OnInit {

  readonly quickLinks: QuickLink[] = [
    { title: 'Charging stations', description: 'Manage sites, addresses and station availability.', icon: 'icon-charging-station', url: '/dashboard/charging-stations' },
    { title: 'Charge points', description: 'Configure charge points and their connectors.', icon: 'icon-type2-charger', url: '/dashboard/charging-points' },
    { title: 'Live connectors', description: 'See connector status and active charging activity.', icon: 'icon-charging-cable', url: '/dashboard/connector-realtime' },
    { title: 'Charging sessions', description: 'Review active and completed charging sessions.', icon: 'icon-arrows-up-down', url: '/dashboard/charging-sessions' },
    { title: 'Recharge orders', description: 'Track recharge-card orders and fulfilment.', icon: 'icon-basket', url: '/dashboard/recharge-orders' },
    { title: 'Transactions', description: 'Review payment and energy transactions.', icon: 'icon-payment', url: '/dashboard/transactions' },
    { title: 'Users', description: 'Manage user accounts and access.', icon: 'icon-profile-circle', url: '/dashboard/users' },
    { title: 'Reports', description: 'Review reported issues and system reports.', icon: 'icon-warning', url: '/dashboard/reports' }
  ];

  constructor(
    public access: AccessService
  ) { }

  ngOnInit() {
  }
  

 

}
