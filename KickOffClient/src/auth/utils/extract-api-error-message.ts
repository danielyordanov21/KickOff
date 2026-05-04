import { HttpErrorResponse } from '@angular/common/http';

export interface ApiErrorDetails {
  code: string | null;
  message: string;
  messages: string[];
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function normalizeMessage(value: unknown): string | null {
  return typeof value === 'string' && value.trim().length > 0
    ? value.trim()
    : null;
}

function uniqueMessages(messages: string[]): string[] {
  return Array.from(new Set(messages));
}

function extractSummaryMessage(payload: unknown, allowNestedError = true): string | null {
  if (typeof payload === 'string') {
    return normalizeMessage(payload);
  }

  if (Array.isArray(payload)) {
    return null;
  }

  if (!isRecord(payload)) {
    return null;
  }

  const summaryMessage = [
    payload['message'],
    payload['description'],
    payload['detail'],
    payload['title']
  ]
    .map(normalizeMessage)
    .find((value): value is string => value !== null);

  if (summaryMessage) {
    return summaryMessage;
  }

  if (allowNestedError && 'error' in payload) {
    return extractSummaryMessage(payload['error'], false);
  }

  return null;
}

function extractValidationMessages(payload: unknown): string[] {
  if (Array.isArray(payload)) {
    return uniqueMessages(payload.flatMap(item => extractMessages(item, false)));
  }

  if (!isRecord(payload)) {
    return [];
  }

  return uniqueMessages(Object.values(payload).flatMap(value => {
    if (Array.isArray(value)) {
      return value
        .map(normalizeMessage)
        .filter((entry): entry is string => entry !== null);
    }

    const normalizedValue = normalizeMessage(value);
    return normalizedValue ? [normalizedValue] : [];
  }));
}

function extractMessages(payload: unknown, allowNestedError = true): string[] {
  if (typeof payload === 'string') {
    const normalizedPayload = normalizeMessage(payload);
    return normalizedPayload ? [normalizedPayload] : [];
  }

  if (Array.isArray(payload)) {
    return uniqueMessages(payload.flatMap(item => extractMessages(item, false)));
  }

  if (!isRecord(payload)) {
    return [];
  }

  const validationMessages = extractValidationMessages(payload['errors']);
  if (validationMessages.length > 0) {
    return validationMessages;
  }

  const summaryMessage = extractSummaryMessage(payload, false);
  if (summaryMessage) {
    return [summaryMessage];
  }

  if (allowNestedError && 'error' in payload) {
    return extractMessages(payload['error'], false);
  }

  return [];
}

function extractErrorCode(payload: unknown, allowNestedError = true): string | null {
  if (!isRecord(payload)) {
    return null;
  }

  const errorCode = normalizeMessage(payload['code']);
  if (errorCode) {
    return errorCode;
  }

  if (allowNestedError && 'error' in payload) {
    return extractErrorCode(payload['error'], false);
  }

  return null;
}

function getErrorPayload(error: unknown): unknown {
  return error instanceof HttpErrorResponse
    ? error.error
    : error;
}

export function extractApiErrorDetails(error: unknown, fallbackMessage: string): ApiErrorDetails {
  const payload = getErrorPayload(error);
  const messages = extractMessages(payload);
  const summaryMessage = extractSummaryMessage(payload);

  return {
    code: extractErrorCode(payload),
    message: summaryMessage ?? (messages.length > 0 ? messages.join(' ') : fallbackMessage),
    messages,
  };
}

export function extractApiErrorMessage(error: unknown, fallbackMessage: string): string {
  const details = extractApiErrorDetails(error, fallbackMessage);

  return details.messages.length > 0
    ? details.messages.join(' ')
    : details.message;
}
