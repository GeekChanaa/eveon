import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { RouterModule } from '@angular/router';
import { AtomsComponent } from './atoms.component';
import { CardComponent } from './card/card.component';

@NgModule({
  declarations: [
    AtomsComponent,
    CardComponent
   ],
  imports: [
    CommonModule,
    RouterModule,
  ],
  exports: [
    CardComponent
  ],
  providers: [],
})
export class AtomsModule { }
