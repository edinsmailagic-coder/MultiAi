import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import { Observable,tap } from 'rxjs';

@Injectable({
    providedIn: 'root'

})
export class AuthService{
    private apiUrl = 'https://localhost:7090';
    constructor(private http:HttpClient){}
    
    login(credentials:any):Observable<any>{
        return this.http.post(`${this.apiUrl}/api/auth/login`,credentials).pipe(
            tap((response:any)=>{
                localStorage.setItem('token',response.accessToken);
            })
        );
    }

    register(data:any):Observable<any>{
        return this.http.post(`${this.apiUrl}/api/auth/register`,data);
    }
    logout():void{
        localStorage.removeItem('token');
    }
    isLoggedIn():boolean{
        return !!localStorage.getItem('token');
    }
}