import { Component, Input } from '@angular/core';
import { HDD } from '../../../models/userData';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-hdd',
  standalone: true,  
  imports: [CommonModule, FormsModule ],
  templateUrl: './hdd.component.html',
})
export class HDDComponent {
  @Input() data: HDD | undefined;
}