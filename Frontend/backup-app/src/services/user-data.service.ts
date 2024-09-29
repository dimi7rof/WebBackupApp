import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { UserData } from '../models/userData';

@Injectable({
  providedIn: 'root',
})
export class UserDataService {
  private userData: UserData | undefined;

  constructor(private http: HttpClient) {}

  getUserData(userId: string): Observable<UserData> {
    if (this.userData) {
      return new Observable((observer) => {
        observer.next(this.userData);
        observer.complete();
      });
    } else {
      return this.http.get<UserData>(`http://localhost:5000/loaduserdata/${userId}`).pipe(
        tap((data) => {
          this.userData = data; // Cache the data
        })
      );
    }
  }

  execute(data: any, component: string) {
    this.http.post(`http://localhost:5000/execute/${component}`, data, {
      headers: new HttpHeaders({ 'Content-Type': 'application/json' })
    }).subscribe({
      next: (response) => {
        console.log('Post request successful. Response:', response);
      },
      error: (err) => {
        console.error('Error executing request:', err);
      },
      complete: () => {
        console.log('Post request completed.');
      }
    });
  }

  save(data: any, id: string): string {
    let message = '';
    this.http.post<{ Message: string }>(`http://localhost:5000/saveuserdata/${id}`, data, {
      headers: new HttpHeaders({ 'Content-Type': 'application/json' })
    }).subscribe({
      next: (response) => {
        console.log('Post request successful. Response:', response);
        message = response.Message;
      },
      error: (err) => {
        console.error('Error executing request:', err);
      },
      complete: () => {
        console.log('Post request completed.');
      }
    });

    return message
  }

  clearCache() {
    this.userData = undefined;
  }
}
export { UserData };

