import { Component, Input } from '@angular/core';
import { SD } from '../../../models/userData';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-sd',
  standalone: true,  
  imports: [CommonModule, FormsModule],
  templateUrl: './sd.component.html',
})
export class SDComponent {
  @Input() data: SD | undefined;
}