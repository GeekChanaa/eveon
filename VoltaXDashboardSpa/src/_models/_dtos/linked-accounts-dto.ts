export interface LinkedAccountsDto {
  googleLinked: boolean;
  googleEmail: string | null;
  hasPassword: boolean;
  authProvider: string;
}
