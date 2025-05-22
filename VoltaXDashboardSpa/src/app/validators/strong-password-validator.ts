import { ValidatorFn, AbstractControl, ValidationErrors } from "@angular/forms";

export function strongPasswordValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    
    if (!value) {
      return null;
    }

    const hasUpperCase = /[A-Z]/.test(value);
    const hasLowerCase = /[a-z]/.test(value);
    const hasNumeric = /[0-9]/.test(value);
    const hasSpecialChar = /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]+/.test(value);
    const validLength = value.length >= 8;
    
    const passwordValid = hasUpperCase && hasLowerCase && hasNumeric && hasSpecialChar && validLength;
    
    return !passwordValid ? {
      strongPassword: {
        hasUpperCase,
        hasLowerCase,
        hasNumeric,
        hasSpecialChar,
        validLength
      }
    } : null;
  };
}