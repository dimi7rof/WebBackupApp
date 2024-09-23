import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LoadComponent } from './load/load.component';
import { MainComponent } from './main/main.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, LoadComponent, MainComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'] // Corrected "styleUrls" (plural)
})
export class AppComponent {
  title = 'backup-app';
  page = 'set1';
}
