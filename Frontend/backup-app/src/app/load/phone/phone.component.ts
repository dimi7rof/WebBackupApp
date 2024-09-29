import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Phone } from '../../../models/userData';

@Component({
  selector: 'app-phone',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './phone.component.html',
})
export class PhoneComponent{
  @Input() phoneData: Phone | undefined;
}
