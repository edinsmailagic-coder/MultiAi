import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
@Injectable({ providedIn: 'root' })
export class ToastService { constructor(private readonly snackBar: MatSnackBar, private readonly translate: TranslateService) {} showSuccess(key: string): void { this.open(key, 'success-toast'); } error(key: string): void { this.open(key, 'error-toast'); } warning(key: string): void { this.open(key, 'warning-toast'); } private open(key: string, panelClass: string): void { this.snackBar.open(this.translate.instant(key), '', { duration: 3500, horizontalPosition: 'end', verticalPosition: 'top', panelClass: [panelClass] }); } }
