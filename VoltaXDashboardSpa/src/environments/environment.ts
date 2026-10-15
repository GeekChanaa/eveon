// This file can be replaced during build by using the `fileReplacements` array.
// `ng build --prod` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.

export const environment = {
  production: false,
  apiUrl : "http://localhost:8000",
  apiStaticFilesUrl : "http://localhost:8000/StaticFiles/",
  // Public site (eveon-ui, `npm run dev`) that serves the sign in / sign up pages.
  authUrl : "http://localhost:5173",
  googleMapsApiKey : "AIzaSyCeZ5OSLki92cef4GtIJ82i2DlIGNOK1cU",
  googleGeocodingApiKey : "AIzaSyASK_Y37ctDVZfa9P7OqJ2QsFpC_XMgZBQ",
};

