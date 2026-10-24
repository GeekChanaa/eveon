import { NO_ERRORS_SCHEMA, NgModule, SchemaMetadata, Type } from '@angular/core';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { TestBed, TestModuleMetadata } from '@angular/core/testing';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { RouterTestingModule } from '@angular/router/testing';
import { TranslateModule } from '@ngx-translate/core';
import { VxI18nModule } from 'src/app/shared/i18n/vx-i18n.module';
import { CamelToKebabPipe } from 'src/_pipes/camel-to-kebab.pipe';
import { CamelCaseToSpacePipe } from 'src/pipes/camel-case-to-space-case.pipe';
import { SafeUrlPipe } from 'src/pipes/safe-url.pipe';
import { TokenStorageService } from 'src/_services/token-storage.service';
import { HUB_CONNECTION_FACTORY } from 'src/_services/realtime-hub.service';
import { fakeHubFactory } from './fake-hub-connection';

/** Claims of the signed-in user in unit tests (an administrator, no partner). */
export const TEST_CLAIMS = { nameid: '1', role: 'Admin', unique_name: 'tester@eveon.test', exp: 4102444800 };

/** An unsigned JWT carrying the claims (the SPA only decodes tokens; the API checks signatures). */
export function testToken(claims: object = TEST_CLAIMS): string {
  const part = (value: object) => btoa(JSON.stringify(value)).replace(/=+$/, '').replace(/\+/g, '-').replace(/\//g, '_');
  return `${part({ alg: 'none', typ: 'JWT' })}.${part(claims)}.test`;
}

/**
 * What the application module provides to every page, for unit tests: HTTP (testing backend, nothing leaves the
 * browser), router, animations off, forms, menus, snack bars, translations (keys are shown as is), the app's pipes
 * and a signed-in administrator (see TEST_CLAIMS).
 */
@NgModule({
  declarations: [CamelCaseToSpacePipe, CamelToKebabPipe, SafeUrlPipe],
  imports: [
    HttpClientTestingModule,
    RouterTestingModule,
    NoopAnimationsModule,
    FormsModule,
    ReactiveFormsModule,
    MatSnackBarModule,
    MatMenuModule,
    TranslateModule.forRoot(),
    VxI18nModule
  ],
  // Realtime hubs never open a socket in unit tests.
  providers: [{ provide: HUB_CONNECTION_FACTORY, useFactory: () => fakeHubFactory() }],
  exports: [
    HttpClientTestingModule,
    RouterTestingModule,
    NoopAnimationsModule,
    FormsModule,
    ReactiveFormsModule,
    MatSnackBarModule,
    MatMenuModule,
    TranslateModule,
    VxI18nModule,
    CamelCaseToSpacePipe,
    CamelToKebabPipe,
    SafeUrlPipe
  ]
})
export class VxTestingModule {
  constructor(tokens: TokenStorageService) {
    // Pages read the signed-in user from the access token.
    if (!tokens.accessToken) tokens.storeAccessToken(testToken());
  }
}

/** Child components of the component under test are not declared: their tags and inputs are ignored. */
export const VX_TEST_SCHEMAS: SchemaMetadata[] = [NO_ERRORS_SCHEMA];

/** Configures TestBed for a component (or several) with the shared testing module; returns compileComponents(). */
export function configureVxTest(declarations: Type<unknown>[], extra: TestModuleMetadata = {}): Promise<void> {
  return TestBed.configureTestingModule({
    ...extra,
    imports: [VxTestingModule, ...(extra.imports ?? [])],
    declarations: [...declarations, ...(extra.declarations ?? [])],
    schemas: [...VX_TEST_SCHEMAS, ...(extra.schemas ?? [])]
  }).compileComponents();
}
