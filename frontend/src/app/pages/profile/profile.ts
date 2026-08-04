import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin, switchMap } from 'rxjs';
import { AIProvider, ConnectedProvider } from '../../core/models/provider.model';
import { AuthService } from '../../core/services/auth.service';
import { ProviderService } from '../../core/services/provider.service';
import { ToastService } from '../../core/services/toast.service';
import { UserService } from '../../core/services/user.service';
@Component({ selector: 'app-profile', imports: [ReactiveFormsModule, TranslatePipe], templateUrl: './profile.html', styleUrl: './profile.scss', changeDetection: ChangeDetectionStrategy.OnPush })
export class Profile implements OnInit { readonly providers = signal<AIProvider[]>([]); readonly connected = signal<ConnectedProvider[]>([]); readonly profileForm; readonly passwordForm; readonly providerForm;
constructor(fb: FormBuilder, readonly auth: AuthService, private readonly userService: UserService, private readonly providerService: ProviderService, private readonly toast: ToastService) { this.profileForm = fb.nonNullable.group({ firstName: ['', Validators.required], lastName: ['', Validators.required] }); this.passwordForm = fb.nonNullable.group({ currentPassword: ['', Validators.required], newPassword: ['', [Validators.required, Validators.minLength(8)]] }); this.providerForm = fb.nonNullable.group({ providerId: [0, [Validators.required, Validators.min(1)]], apiKey: ['', Validators.required] }); }
ngOnInit(): void { const user = this.auth.currentUser(); if (user) this.profileForm.patchValue(user); else this.auth.loadCurrentUser().subscribe(value => this.profileForm.patchValue(value)); forkJoin({ providers: this.providerService.getProviders(), connected: this.providerService.getConnected() }).subscribe({ next: data => { this.providers.set(data.providers); this.connected.set(data.connected); }, error: () => this.toast.error('toast.loadError') }); }
saveProfile(): void { if (this.profileForm.invalid) return; this.userService.updateProfile(this.profileForm.getRawValue()).pipe(switchMap(() => this.auth.loadCurrentUser())).subscribe({ next: () => this.toast.showSuccess('toast.profileSaved'), error: () => this.toast.error('toast.saveError') }); }
changePassword(): void { if (this.passwordForm.invalid) { this.passwordForm.markAllAsTouched(); return; } this.userService.changePassword(this.passwordForm.getRawValue()).subscribe({ next: () => { this.passwordForm.reset(); this.toast.showSuccess('toast.passwordChanged'); }, error: () => this.toast.error('toast.passwordError') }); }
connect(): void {
    if (this.providerForm.invalid) {
      this.providerForm.markAllAsTouched();
      return;
    }
    const raw = this.providerForm.getRawValue();
    this.providerService
      .connect({ providerId: Number(raw.providerId), apiKey: raw.apiKey })
      .pipe(switchMap(() => this.providerService.getConnected()))
      .subscribe({
        next: (connected) => {
          this.connected.set(connected);
          this.providerForm.patchValue({ apiKey: '' });
          this.toast.showSuccess('toast.providerConnected');
        },
        error: () => this.toast.error('toast.providerError'),
      });
  }
}
