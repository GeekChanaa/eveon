import { HttpErrorResponse } from '@angular/common/http';

/** What the API answers for a command sent to a charger: { message, status, response } (OcppCommandResult). */
export interface CommandAnswer {
  message: string;
  status: string | null;
  response?: unknown;
}

export type FeedbackTone = 'success' | 'warning' | 'error';

/** A translated message (portal.command.*) and its tone, for the result of a remote command. */
export interface CommandFeedback {
  tone: FeedbackTone;
  key: string;
  params: Record<string, unknown>;
}

const DONE = ['Accepted', 'Unlocked'];

/**
 * Turns a charger's answer, or the error of the call, into what the portal tells the user.
 * action: reset, refresh, unlock or stop (portal.command.{action}.done / .scheduled).
 */
export function commandFeedback(result: CommandAnswer | HttpErrorResponse, action: string): CommandFeedback {
  if (!(result instanceof HttpErrorResponse)) {
    const status = result.status ?? '';
    if (DONE.includes(status)) return { tone: 'success', key: `portal.command.${action}.done`, params: {} };
    if (status === 'Scheduled') return { tone: 'success', key: `portal.command.${action}.scheduled`, params: {} };
    return { tone: 'warning', key: 'portal.command.refused', params: { status } };
  }
  const code = result.error?.status as string | undefined;
  switch (result.status) {
    case 409: return code === 'NotRunning'
      ? { tone: 'warning', key: 'portal.command.notRunning', params: {} }
      : { tone: 'error', key: 'portal.command.offline', params: {} };
    case 404: return { tone: 'error', key: 'portal.command.notFound', params: {} };
    case 504: return { tone: 'error', key: 'portal.command.timeout', params: {} };
    case 502: return { tone: 'error', key: 'portal.command.callError', params: { code: result.error?.errorCode ?? '' } };
    case 503: return { tone: 'error', key: 'portal.command.unavailable', params: {} };
    case 400: return code === 'NotSupportedByProtocol'
      ? { tone: 'error', key: 'portal.command.notSupported', params: {} }
      : { tone: 'error', key: 'portal.command.failed', params: {} };
    default: return { tone: 'error', key: 'portal.command.failed', params: {} };
  }
}

/** Alert class of a feedback tone. */
export function feedbackClass(tone: FeedbackTone): string {
  return tone === 'success' ? 'vx-alert--success' : tone === 'warning' ? 'vx-alert--warning' : 'vx-alert--error';
}
