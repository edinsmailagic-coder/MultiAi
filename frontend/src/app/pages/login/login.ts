import { Component } from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {RouterModule, Router} from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {AuthService} from '../../core/services/auth';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-login',
  imports: [CommonModule,FormsModule,RouterModule,TranslatePipe],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
loginData = {email : '', password : ''};
errorMessage:string = '';
currentLanguage = 'bs';
constructor(private authService: AuthService,private router:Router,private translate: TranslateService,private toast:ToastService) {
  this.currentLanguage = localStorage.getItem('lang') || 'bs';
  this.translate.use(this.currentLanguage);
}
changeLanguage(lang: string) {
  this.currentLanguage = lang;
  localStorage.setItem('lang', lang);
  this.translate.use(lang);
}
onLogin(){
  this.errorMessage=''
  this.authService.login(this.loginData).subscribe({
    next:(response)=>{
      this.toast.showSuccess('toast.loginSuccess');
      this.router.navigate(['/dashboard']);
    },
    error : (err)=>{
      this.toast.error('toast.loginError');
    }
  });
}
}
