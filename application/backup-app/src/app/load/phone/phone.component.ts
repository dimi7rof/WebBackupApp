import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Phone } from '../../../models/userData';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-phone',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './phone.component.html',
})
export class PhoneComponent{
  @Input() phoneData: Phone | undefined;

  addNewPath(): void {
    if (this.phoneData) {
      this.phoneData.paths.sourcePaths.push('');
      this.phoneData.paths.destinationPaths.push('');
    }
  }

  onInputChange(index: number): void {
    if (this.phoneData) {
      const lastSource = this.phoneData.paths.sourcePaths[this.phoneData.paths.sourcePaths.length - 1];
      const lastDestination = this.phoneData.paths.destinationPaths[this.phoneData.paths.destinationPaths.length - 1];
      
      // If both the last source and destination paths are filled, add a new empty path
      if (lastSource && lastDestination) {
        this.addNewPath();
      }
    }
  }
}
