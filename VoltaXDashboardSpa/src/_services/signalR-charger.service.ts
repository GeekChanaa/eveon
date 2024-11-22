import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SignalRChargerService {
  private hubConnection: signalR.HubConnection | undefined;

  constructor() { }

  // Start the connection to SignalR hub
  public startConnection(chargePointID : string): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.apiUrl+'/chargerHub')
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR Connection started');
        this.joinChargerGroup(chargePointID);
      }).catch(
        (err : any) => console.log('Error while starting connection: ' + err)
      );
  }

  // Join a specific charger group
  public joinChargerGroup(chargerId: string): void {
    this.hubConnection?.invoke('JoinChargerGroup', chargerId)
      .then(() => console.log(`Joined Charger ${chargerId} group`))
      .catch((err : any) => console.error('Error joining charger group: ', err));
  }

  // Send message to a specific charger
  public sendMessageToCharger(chargerId: string, message: string): void {
    this.hubConnection?.invoke('SendMessageToCharger', chargerId, message)
      .catch((err : any) => console.error(err));
  }

  // Listen for messages
  public addMessageListener(messageSentCallback? : any, messageReceivedCallback? : any): void {
    this.hubConnection?.on('ReceiveMessage', (message : any) => {
      messageReceivedCallback(message);
    });
    this.hubConnection?.on('SentMessage', (message : any) => {
      messageSentCallback(message);
    })
  }
}
