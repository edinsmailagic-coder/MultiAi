import { Component } from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {RouterModule, Router} from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {AuthService} from '../../core/services/auth';
import { ToastService } from '../../core/services/toast.service';
@Component({
  selector: 'app-dashboard',
  imports: [CommonModule,FormsModule,RouterModule,TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
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

}
