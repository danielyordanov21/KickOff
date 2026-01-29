import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { User } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = '/api/auth';

  constructor(private http: HttpClient) {}

  public getUser(id: string): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/${id}`);
  }

  public getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/me`);
  }
}

/*
fetch('/api/home')
  .then(response => response.json())
  .then(data => {
    console.log(data.message);
  }
  ).catch(error => {
    console.error('Error fetching greeting:', error);
  });
*/