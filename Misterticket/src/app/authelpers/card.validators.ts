import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Luhn checksum: the algorithm every real card number satisfies. It rejects
 * typos and made-up numbers, so the form behaves like a real one.
 * 4242 4242 4242 4242 passes, which is the usual test number.
 */
export const cardNumberValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const digits = (control.value ?? '').replace(/\s+/g, '');

  if (!/^\d{16}$/.test(digits)) {
    return { cardNumber: 'Enter the 16 digits of the card.' };
  }

  let sum = 0;
  let double = false;

  // Walk the digits from right to left, doubling every second one.
  for (let i = digits.length - 1; i >= 0; i--) {
    let value = Number(digits[i]);

    if (double) {
      value *= 2;
      if (value > 9) value -= 9;
    }

    sum += value;
    double = !double;
  }

  return sum % 10 === 0 ? null : { cardNumber: 'That card number is not valid.' };
};

/** MM/YY, and the month must not be in the past. */
export const expiryValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = (control.value ?? '').trim();

  const match = /^(0[1-9]|1[0-2])\/(\d{2})$/.exec(value);
  if (!match) {
    return { expiry: 'Use the MM/YY format.' };
  }

  const month = Number(match[1]);
  const year = 2000 + Number(match[2]);

  // The card is valid through the last day of its month.
  const endOfMonth = new Date(year, month, 0, 23, 59, 59);

  return endOfMonth.getTime() > Date.now()
    ? null
    : { expiry: 'That card has expired.' };
};
