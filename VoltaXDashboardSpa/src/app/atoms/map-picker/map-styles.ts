// Google Maps styles matching the dashboard palette (navy). Applied to the roadmap
// only; satellite imagery is left untouched.

export const LIGHT_MAP_STYLES = [
  { elementType: 'geometry', stylers: [{ color: '#F3F5F9' }] },
  { elementType: 'labels.text.fill', stylers: [{ color: '#4A5878' }] },
  { elementType: 'labels.text.stroke', stylers: [{ color: '#FFFFFF' }, { weight: 3 }] },
  { featureType: 'administrative', elementType: 'geometry.stroke', stylers: [{ color: '#C9D2E3' }] },
  { featureType: 'administrative.land_parcel', elementType: 'labels', stylers: [{ visibility: 'off' }] },
  { featureType: 'administrative.locality', elementType: 'labels.text.fill', stylers: [{ color: '#1F3461' }] },
  { featureType: 'landscape.man_made', elementType: 'geometry', stylers: [{ color: '#EBEFF6' }] },
  { featureType: 'poi', elementType: 'geometry', stylers: [{ color: '#E4E9F2' }] },
  { featureType: 'poi', elementType: 'labels.text.fill', stylers: [{ color: '#7D89A3' }] },
  { featureType: 'poi.business', stylers: [{ visibility: 'off' }] },
  { featureType: 'poi.park', elementType: 'geometry', stylers: [{ color: '#DCEFE0' }] },
  { featureType: 'poi.park', elementType: 'labels.text.fill', stylers: [{ color: '#5E8A6A' }] },
  { featureType: 'road', elementType: 'geometry', stylers: [{ color: '#FFFFFF' }] },
  { featureType: 'road', elementType: 'geometry.stroke', stylers: [{ color: '#DFE5F0' }] },
  { featureType: 'road.arterial', elementType: 'labels.text.fill', stylers: [{ color: '#4A5878' }] },
  { featureType: 'road.highway', elementType: 'geometry', stylers: [{ color: '#DCE4F4' }] },
  { featureType: 'road.highway', elementType: 'geometry.stroke', stylers: [{ color: '#C3CFE6' }] },
  { featureType: 'road.highway', elementType: 'labels.text.fill', stylers: [{ color: '#1F3461' }] },
  { featureType: 'road.local', elementType: 'labels.text.fill', stylers: [{ color: '#7D89A3' }] },
  { featureType: 'transit', elementType: 'geometry', stylers: [{ color: '#E4E9F2' }] },
  { featureType: 'transit.station', elementType: 'labels.text.fill', stylers: [{ color: '#4A5878' }] },
  { featureType: 'water', elementType: 'geometry', stylers: [{ color: '#C7DAF5' }] },
  { featureType: 'water', elementType: 'labels.text.fill', stylers: [{ color: '#5B85E0' }] }
];

export const DARK_MAP_STYLES = [
  { elementType: 'geometry', stylers: [{ color: '#0E1E3F' }] },
  { elementType: 'labels.text.fill', stylers: [{ color: '#A8B3CB' }] },
  { elementType: 'labels.text.stroke', stylers: [{ color: '#0A1A3A' }, { weight: 3 }] },
  { featureType: 'administrative', elementType: 'geometry.stroke', stylers: [{ color: '#2A4277' }] },
  { featureType: 'administrative.land_parcel', elementType: 'labels', stylers: [{ visibility: 'off' }] },
  { featureType: 'administrative.locality', elementType: 'labels.text.fill', stylers: [{ color: '#D4DEF5' }] },
  { featureType: 'landscape.man_made', elementType: 'geometry', stylers: [{ color: '#12264D' }] },
  { featureType: 'poi', elementType: 'geometry', stylers: [{ color: '#132A55' }] },
  { featureType: 'poi', elementType: 'labels.text.fill', stylers: [{ color: '#7D89A3' }] },
  { featureType: 'poi.business', stylers: [{ visibility: 'off' }] },
  { featureType: 'poi.park', elementType: 'geometry', stylers: [{ color: '#123A3A' }] },
  { featureType: 'poi.park', elementType: 'labels.text.fill', stylers: [{ color: '#6FAF8C' }] },
  { featureType: 'road', elementType: 'geometry', stylers: [{ color: '#1F3461' }] },
  { featureType: 'road', elementType: 'geometry.stroke', stylers: [{ color: '#0A1A3A' }] },
  { featureType: 'road.arterial', elementType: 'labels.text.fill', stylers: [{ color: '#A8B3CB' }] },
  { featureType: 'road.highway', elementType: 'geometry', stylers: [{ color: '#2D4C8A' }] },
  { featureType: 'road.highway', elementType: 'geometry.stroke', stylers: [{ color: '#0A1A3A' }] },
  { featureType: 'road.highway', elementType: 'labels.text.fill', stylers: [{ color: '#D4DEF5' }] },
  { featureType: 'road.local', elementType: 'labels.text.fill', stylers: [{ color: '#7D89A3' }] },
  { featureType: 'transit', elementType: 'geometry', stylers: [{ color: '#162E5C' }] },
  { featureType: 'transit.station', elementType: 'labels.text.fill', stylers: [{ color: '#A8B3CB' }] },
  { featureType: 'water', elementType: 'geometry', stylers: [{ color: '#040C1F' }] },
  { featureType: 'water', elementType: 'labels.text.fill', stylers: [{ color: '#4A5878' }] }
];

/** Charging-station pin (map marker with a bolt), as a data URI. */
export function stationPinUrl(dark: boolean): string {
  const fill = dark ? '#7FA2EE' : '#0A1A3A';
  const stroke = dark ? '#0A1A3A' : '#FFFFFF';
  const bolt = dark ? '#0A1A3A' : '#FFD34D';
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="40" height="52" viewBox="0 0 40 52">`
    + `<path d="M20 1.5C9.8 1.5 1.5 9.7 1.5 19.8 1.5 33.3 20 50.5 20 50.5S38.5 33.3 38.5 19.8C38.5 9.7 30.2 1.5 20 1.5z" fill="${fill}" stroke="${stroke}" stroke-width="2.5"/>`
    + `<path d="M22.5 9.5 13 22.5h6l-2 10.5 9.5-13h-6z" fill="${bolt}"/></svg>`;
  return 'data:image/svg+xml;charset=UTF-8,' + encodeURIComponent(svg);
}
