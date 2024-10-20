import { Component, OnChanges, SimpleChanges } from '@angular/core';
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
})
export class MainComponent {
  items = ['Phone', 'HDD', 'SdCard'];
  selectedComponent: string = 'Phone';
  data: UserData | undefined;
  progressData: string[] = []; 

  constructor(
    private userDataService: UserDataService,
     private signalRService: SignalRService) {}

  ngOnInit(): void {
    this.signalRService.startConnection();

    // Listen for progress updates from the hub
    this.signalRService.addProgressListener((progress: any) => {
      console.log('Progress data received:', progress);
      this.progressData.unshift(progress);
    });

    this.userDataService.getUserData('id1').subscribe({
      next: (response: UserData) => {
        console.log('API response:', response);
        this.data = response; 
      }
    });

    this.addNewPath();
  }

  selectComponent(component: string) {
    this.selectedComponent = component;
  }

  save() {
    this.userDataService.save(this.data!, 'id1')
  }

  execute() {
    this.userDataService.execute(this.data, this.selectedComponent);
  }

  addNewPath() {
    this.data?.hdd.paths.sourcePaths.push('');
    this.data?.hdd.paths.destinationPaths.push('');
    this.data?.phone.paths.sourcePaths.push('');
    this.data?.phone.paths.destinationPaths.push('');
    this.data?.sd.paths.sourcePaths.push('');
    this.data?.sd.paths.destinationPaths.push('');
  }
}