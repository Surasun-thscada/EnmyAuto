import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../services/auth.service';

function passwordMatchValidator(control: AbstractControl) {
  const password        = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return password === confirmPassword ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  private fb     = inject(FormBuilder);
  private auth   = inject(AuthService);
  private router = inject(Router);

  form = this.fb.group({
    name:            ['', [Validators.required, Validators.minLength(2)]],
    email:           ['', [Validators.required, Validators.email]],
    password:        ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
  }, { validators: passwordMatchValidator });

  isLoading    = false;
  errorMsg     = '';
  showPassword = false;

  get nameCtrl()            { return this.form.get('name')!;            }
  get emailCtrl()           { return this.form.get('email')!;           }
  get passwordCtrl()        { return this.form.get('password')!;        }
  get confirmPasswordCtrl() { return this.form.get('confirmPassword')!; }
  get passwordMismatch()    {
    return this.form.errors?.['passwordMismatch'] && this.confirmPasswordCtrl.touched;
  }

  getStrength(): number {
    const pw = this.passwordCtrl.value ?? '';
    let score = 0;
    if (pw.length >= 8)           score++;
    if (/[A-Z]/.test(pw))         score++;
    if (/[0-9]/.test(pw))         score++;
    if (/[^A-Za-z0-9]/.test(pw))  score++;
    return score;
  }

  getStrengthColor(bar: number): string {
    const s = this.getStrength();
    if (bar > s) return 'bg-slate-700';
    return ['', 'bg-red-500', 'bg-orange-500', 'bg-yellow-500', 'bg-green-500'][s];
  }

  getStrengthLabel(): string {
    return ['', 'Weak', 'Fair', 'Good', 'Strong'][this.getStrength()];
  }

  getStrengthLabelColor(): string {
    return ['', 'text-red-400', 'text-orange-400', 'text-yellow-400', 'text-green-400'][this.getStrength()];
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.isLoading = true;
    this.errorMsg  = '';

    this.auth.register({
      name:     this.nameCtrl.value!.trim(),
      email:    this.emailCtrl.value!.trim(),
      password: this.passwordCtrl.value!,
    })
    .pipe(finalize(() => (this.isLoading = false)))
    .subscribe({
      next:  () => this.router.navigate(['/storyboard']),
      error: err => this.errorMsg = err.error?.error ?? 'Registration failed.',
    });
  }
}
