import { HttpErrorResponse } from '@angular/common/http';

import { extractApiErrorDetails, extractApiErrorMessage } from './extract-api-error-message';

describe('extractApiErrorMessage', () => {
  it('joins ASP.NET Identity error arrays into a user-facing message', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: [
        { code: 'PasswordTooShort', description: 'Passwords must be at least 6 characters.' },
        { code: 'PasswordRequiresNonAlphanumeric', description: 'Passwords must have at least one non alphanumeric character.' }
      ]
    });

    expect(extractApiErrorMessage(error, 'fallback')).toBe(
      'Passwords must be at least 6 characters. Passwords must have at least one non alphanumeric character.'
    );
  });

  it('extracts validation-problem messages from ASP.NET bad requests', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred.',
        errors: {
          NewPassword: ['The NewPassword field is required.'],
          CurrentPassword: ['The CurrentPassword field is required.']
        }
      }
    });

    expect(extractApiErrorMessage(error, 'fallback')).toBe(
      'The NewPassword field is required. The CurrentPassword field is required.'
    );
  });

  it('falls back when there is no useful API message', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {}
    });

    expect(extractApiErrorMessage(error, 'fallback')).toBe('fallback');
  });

  it('extracts nested error payload messages', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        error: {
          detail: 'The supplied password does not meet policy.'
        }
      }
    });

    expect(extractApiErrorMessage(error, 'fallback')).toBe(
      'The supplied password does not meet policy.'
    );
  });

  it('keeps a friendly summary while preserving specific validation issues', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        code: 'validation_failed',
        message: 'We could not create your account.',
        errors: [
          'Email is already taken.',
          'Passwords must have at least one non alphanumeric character.'
        ]
      }
    });

    expect(extractApiErrorDetails(error, 'fallback')).toEqual({
      code: 'validation_failed',
      message: 'We could not create your account.',
      messages: [
        'Email is already taken.',
        'Passwords must have at least one non alphanumeric character.'
      ]
    });

    expect(extractApiErrorMessage(error, 'fallback')).toBe(
      'Email is already taken. Passwords must have at least one non alphanumeric character.'
    );
  });
});
