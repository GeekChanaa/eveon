import { Component } from '@angular/core';
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'VoltaXDashboardSpa';
  constructor() { }

  // Loading Assets
  ngOnInit() {
    this.loadCSS();
    this.loadScripts();
  }

  

  // Loading CSS Files
  loadCSS() {
    // css files$
    const cssFiles = [
      "https://netdna.bootstrapcdn.com/font-awesome/4.7.0/css/font-awesome.min.css",
      "/assets/css/app.min.css",


    ];
    for (let i = 0; i < cssFiles.length; i++) {
      const node = document.createElement('link');
      node.href = cssFiles[i];
      node.rel = "stylesheet";
      document.getElementsByTagName('head')[0].appendChild(node);
    }
  }

  // Loading Javascript scripts
  loadScripts() {
    // files / cdns
    const dynamicScripts = [
      "assets/js/lib/jquery.min.js",
      "assets/js/lib/slick.min.js",
      "assets/js/lib/jquery.nice-select.min.js",
      "assets/js/lib/tooltipster.bundle.min.js",
      "assets/js/lib/apexcharts.min.js",
      "assets/js/lib/jquery.richtext.min.js",
      "assets/js/lib/jquery.fancybox.min.js",
      "assets/js/lib/jQuery.tagify.min.js",
      "assets/js/lib/moment.min.js",
      "assets/js/lib/jquery.daterangepicker.min.js",
      "assets/js/lib/nouislider.min.js",
      "assets/js/lib/wNumb.js",
      "assets/js/charts.js",
      "assets/js/demo.js",
      "assets/js/app.js",
    ];
    for (let i = 0; i < dynamicScripts.length; i++) {
      const node = document.createElement('script');
      node.src = dynamicScripts[i];
      node.type = 'text/javascript';
      node.async = false;
      document.getElementsByTagName('head')[0].appendChild(node);
    }
  }
}
