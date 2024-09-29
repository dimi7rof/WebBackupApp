import { Component, Input } from '@angular/core';
import { SD } from '../../../models/userData';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-sd',
  standalone: true,  
  imports: [CommonModule],
  templateUrl: './sd.component.html',
})
export class SDComponent {
  @Input() data: SD | undefined;
}