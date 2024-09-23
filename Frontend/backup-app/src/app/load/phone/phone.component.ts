import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';

interface PhoneData {
    sourcePaths: string[],
    destinationPaths: string[]
}
@Component({
  selector: 'app-phone',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './phone.component.html',
})
export class PhoneComponent implements OnInit {
  data: PhoneData | undefined;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.http.get('http://localhost:5000/load/set1').subscribe({
        next: (response) => {
          this.data = this.ParseData(response);
          console.log('Data from API:', this.data);
        },
        error: (err) => {
          console.error('Error fetching data:', err);
        },
        complete: () => {
          console.log('Request completed.');
        }
      });
  }

  private ParseData(data: any): PhoneData {
    return data as PhoneData;
  }
}