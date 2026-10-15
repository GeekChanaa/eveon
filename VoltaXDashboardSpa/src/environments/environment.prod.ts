// This file can be replaced during build by using the `fileReplacements` array.
// `ng build --prod` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.

export const environment = {
  production: true,
  // HTTPS only: tokens and personal data must never travel in clear text.
  apiUrl : "https://api-v2.eveon.ma",
  apiStaticFilesUrl : "https://api-v2.eveon.ma/StaticFiles/",
  // Public site (eveon-ui) that serves the sign in / sign up pages, no trailing slash.
  authUrl : "https://eveon.ma",
  // Browser keys are public by nature: restrict them by HTTP referrer and API in Google Cloud.
  googleMapsApiKey : "AIzaSyCeZ5OSLki92cef4GtIJ82i2DlIGNOK1cU",
  googleGeocodingApiKey : "AIzaSyASK_Y37ctDVZfa9P7OqJ2QsFpC_XMgZBQ",

};

/*
 * For easier debugging in development mode, you can import the following file
 * to ignore zone related error stack frames such as `zone.run`, `zoneDelegate.invokeTask`.
 *
 * This import should be commented out in production mode because it will have a negative impact
 * on performance if an error is thrown.
 */
// import 'zone.js/plugins/zone-error';  // Included with Angular CLI.
