import { ValidatorFn, AbstractControl, ValidationErrors } from "@angular/forms";


export function passwordMatchValidator(passwordField: string, confirmPasswordField: string): ValidatorFn {
  return (formGroup: AbstractControl): ValidationErrors | null => {
    const password = formGroup.get(passwordField);
    const confirmPassword = formGroup.get(confirmPasswordField);

    if (!password || !confirmPassword) {
      return null;
    }

    return password.value === confirmPassword.value ? null : { passwordMismatch: true };
  };
}