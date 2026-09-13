import { AbstractControl, ValidationErrors, ValidatorFn } from "@angular/forms";

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;

// Digits, separators and an optional leading "+". An email can never match, which is
// enough to tell what the user meant to type in the single login field.
const PHONE_SHAPE = /^\+?[\d\s\-.()]+$/;

// What the API stores: "+212" followed by the 9 national digits of a Moroccan number.
const NORMALIZED_PHONE = /^\+212[5-7]\d{8}$/;

export function looksLikePhoneNumber(value: string): boolean {
  return PHONE_SHAPE.test((value || '').trim());
}

/**
 * Mirrors PhoneHelper.Normalize on the API: "0610610614", "+212610610614" and
 * "212 610 610 614" all become "+212610610614".
 */
export function normalizePhoneNumber(value: string): string {
  let national = (value || '').replace(/\D/g, '');

  if (national.startsWith('00212')) national = national.substring(5);
  else if (national.startsWith('212')) national = national.substring(3);
  else if (national.startsWith('0')) national = national.substring(1);

  return `+212${national}`;
}

export function isValidPhoneNumber(value: string): boolean {
  return looksLikePhoneNumber(value) && NORMALIZED_PHONE.test(normalizePhoneNumber(value));
}

/**
 * The login field accepts either an email address or a Moroccan phone number.
 */
export function emailOrPhoneValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value || '').trim();

    // An empty field is Validators.required's business, not ours.
    if (!value) {
      return null;
    }

    if (looksLikePhoneNumber(value)) {
      return isValidPhoneNumber(value) ? null : { invalidPhone: true };
    }

    return EMAIL_PATTERN.test(value) ? null : { invalidEmail: true };
  };
}
