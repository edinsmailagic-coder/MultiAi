import {Injectable} from '@angular/core';
import {MatSnackBar} from '@angular/material/snack-bar';
import {TranslateService} from '@ngx-translate/core';

@Injectable({
    providedIn: 'root'
})

export class ToastService {
    constructor(private snackBar: MatSnackBar, private translate: TranslateService) {}

    showSuccess(messageKey: string): void {
        const message = this.translate.instant(messageKey);
        this.snackBar.open(message, '', {
        duration: 3500,
        horizontalPosition: 'end',
        verticalPosition: 'top',
        panelClass: ['success-toast']
        });
    }
    error(key:string){
        this.translate.get(key).subscribe(message=>{
            this.snackBar.open(message, '', {
            duration: 3500,
            horizontalPosition: 'end',
            verticalPosition: 'top',
            panelClass: ['error-toast']
            });
        })
    }
    warning(key:string){
        this.translate.get(key).subscribe(message=>{
            this.snackBar.open(message, '', {
            duration: 3500,
            horizontalPosition: 'end',
            verticalPosition: 'top',
            panelClass: ['warning-toast']
            });
        })
    }
    
}