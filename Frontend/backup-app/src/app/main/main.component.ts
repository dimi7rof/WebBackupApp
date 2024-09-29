import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { PhoneComponent } from '../load/phone/phone.component';
import { HDDComponent } from '../load/hdd/hdd.component';
import { SDComponent } from '../load/sd/sd.component';
import { UserData, UserDataService } from '../../services/user-data.service';
import { SignalRService } from '../../services/signalr.service';

@Component({
  selector: 'main-component',
  standalone: true,
  imports: [RouterOutlet, CommonModule, PhoneComponent, HDDComponent, SDComponent],
  templateUrl: './main.component.html',
  styleUrl: './main.component.css'
})
export class MainComponent {
  items = ['Phone', 'HDD', 'SdCard'];
  selectedComponent: string = 'Phone';
  data: UserData | undefined;
  message: string | null = null;
  progressData: string[] = []; 

  constructor(
    private userDataService: UserDataService,
     private signalRService: SignalRService) {}

  ngOnInit(): void {
    this.signalRService.startConnection();

    // Listen for progress updates from the hub
    this.signalRService.addProgressListener((progress: any) => {
      console.log('Progress data received:', progress);
      this.progressData.push(progress); // Save the received progress data
    });

    this.userDataService.getUserData('id1').subscribe({
      next: (response: UserData) => {
        console.log('API response:', response);
        this.data = response; 
      },
      error: (err: any) => {
        console.error('Error fetching data:', err);
      },
      complete: () => {
        console.log('Request completed.');
      }
    });
  }

  selectComponent(component: string) {
    this.selectedComponent = component;
    console.log(`selected ${component}`);
  }

  save() {
    this.message = this.userDataService.save(this.data, '1')
  }

  execute() {
    this.userDataService.execute(this.data, this.selectedComponent);
  }
}