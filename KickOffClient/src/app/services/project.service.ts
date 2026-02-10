import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
    providedIn: 'root'
})
export class ProjectService {
    private apiUrl = '/api/projects';

    constructor(private http: HttpClient) { }

    public getCatalogue() {
        return this.http.get(`${this.apiUrl}/catalogue`);
    }
}