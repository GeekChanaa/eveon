// Loaded as an external file: the CSP (script-src 'self') blocks inline scripts.
// Must stay the first element of <body> so the dark class is applied before render.
if (localStorage.getItem('darkMode') === "on") {
  document.body.classList.add("dark");
  document.addEventListener("DOMContentLoaded", function() {
    document.querySelector('.js-theme input').checked = true;
  });
}
