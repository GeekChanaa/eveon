/// <reference types="@angular/localize" />

import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';

import { AppModule } from './app/app.module';
import { environment } from './environments/environment';

// Production builds stay silent: debug output (ours or a library's) must not reach the browser console.
// Warnings and errors are kept so real failures can still be diagnosed.
if (environment.production) {
  const noop = () => {};
  console.log = noop;
  console.debug = noop;
  console.info = noop;
  console.trace = noop;
  console.table = noop;
}

// The Maps key comes from the environment file instead of being hardcoded in index.html.
const mapsScript = document.createElement('script');
mapsScript.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(environment.googleMapsApiKey)}`;
mapsScript.async = true;
mapsScript.defer = true;
document.body.appendChild(mapsScript);


platformBrowserDynamic().bootstrapModule(AppModule)
  .catch(err => console.error(err));
