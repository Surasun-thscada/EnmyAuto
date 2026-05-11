import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private fb   = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  form = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  isLoading   = false;
  errorMsg    = '';
  showPassword = false;

  get emailCtrl()    { return this.form.get('email')!;    }
  get passwordCtrl() { return this.form.get('password')!; }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.isLoading = true;
    this.errorMsg  = '';

    this.auth.login({
      email:    this.emailCtrl.value!,
      password: this.passwordCtrl.value!,
    })
    .pipe(finalize(() => (this.isLoading = false)))
    .subscribe({
      next:  () => this.router.navigate(['/storyboard']),
      error: err => this.errorMsg = err.error?.error ?? 'Login failed. Please try again.',
    });
  }
}
