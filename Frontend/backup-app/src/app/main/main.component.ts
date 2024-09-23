import { Component, Type } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { LoadComponent } from "../load/load.component";
import { PhoneComponent } from '../load/phone/phone.component';
import { HDDComponent } from '../load/hdd/hdd.component';
import { SdCardComponent } from '../load/sd/sdcard.component';

@Component({
  selector: 'main-component',
  standalone: true,
  imports: [RouterOutlet, CommonModule, LoadComponent, PhoneComponent, HDDComponent, SdCardComponent],
  templateUrl: './main.component.html',
  styleUrl: './main.component.css'
})
export class MainComponent {
  items = ['Phone', 'HDD', 'Sd Card'];  // List of items
    // Variable to store the selected component
  selectedComponent: string | null = null;

  // Method to select a component and mark the button as active
  selectComponent(component: string) {
    this.selectedComponent = component;
  }
}