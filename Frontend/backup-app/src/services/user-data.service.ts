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
    // Clean up the source and destination paths by removing any empty values
    const cleanedData = { ...data };
    
    if (cleanedData.paths) {
      cleanedData.paths.sourcePaths = cleanedData.paths.sourcePaths.filter((path: string) => path.trim() !== '');
      cleanedData.paths.destinationPaths = cleanedData.paths.destinationPaths.filter((path: string) => path.trim() !== '');
    }

    // Proceed with the cleaned data
    this.http.post(`http://localhost:5000/execute/${component}`, cleanedData, {
      headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
    }).subscribe({
      next: (response) => {
        console.log('Post request successful. Response:', response);
      },
      error: (err) => {
        console.error('Error executing request:', err);
      },
      complete: () => {
        console.log('Post request completed.');
      },
    });
  }

  save(data: UserData, id: string): void {
    const url = `http://localhost:5000/saveuserdata/${id}`;
    const headers = {
      'Content-Type': 'application/json',
    };
  
    fetch(url, {
      method: 'POST',
      headers: headers,
      body: JSON.stringify(data),
    })
      .then((response) => {
        if (!response.ok) {
          throw new Error(`HTTP error! Status: ${response.status}`);
        }
        return response.json(); // Parse the JSON response
      })
      .then((responseData) => {
        console.log('Post request successful. Response:', responseData);
        alert(responseData.message);
      })
      .catch((error) => {
        console.error('Error executing request:', error);
      })
      .finally(() => {
        console.log('Post request completed.');
      });
  }
  

  clearCache() {
    this.userData = undefined;
  }
}
export { UserData };
