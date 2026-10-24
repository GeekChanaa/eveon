/** Leaf keys of a translation table ("portal.live.stop"...). */
function leaves(node: Record<string, unknown>, prefix = ''): string[] {
  return Object.entries(node).flatMap(([key, value]) =>
    value && typeof value === 'object' ? leaves(value as Record<string, unknown>, `${prefix}${key}.`) : [`${prefix}${key}`]);
}

async function table(path: string): Promise<Record<string, unknown>> {
  const response = await fetch(path);
  expect(response.ok).withContext(path).toBeTrue();
  return response.json();
}

/** The portal and sign-in translations exist in French, English and Arabic alike, with the same placeholders. */
describe('Portal and sign-in translations', () => {
  for (const namespace of ['portal', 'auth']) {
    it(`has the same ${namespace} keys and placeholders in fr, en and ar`, async () => {
      const [fr, en, ar] = await Promise.all(['fr', 'en', 'ar'].map(lang => table(`/assets/i18n/${namespace}/${lang}.json`)));
      const keys = leaves(fr);

      expect(Object.keys(fr)).toEqual([namespace]);
      expect(keys.length).toBeGreaterThan(20);
      expect(leaves(en).sort()).toEqual([...keys].sort());
      expect(leaves(ar).sort()).toEqual([...keys].sort());

      const value = (t: Record<string, unknown>, key: string) => key.split('.').reduce((n: any, k) => n?.[k], t) as string;
      const placeholders = (s: string) => (s.match(/{{\s*\w+\s*}}/g) ?? []).map(p => p.replace(/\s/g, '')).sort();
      for (const key of keys) {
        expect(placeholders(value(en, key))).withContext(`en ${key}`).toEqual(placeholders(value(fr, key)));
        expect(placeholders(value(ar, key))).withContext(`ar ${key}`).toEqual(placeholders(value(fr, key)));
        expect(value(ar, key).trim()).withContext(`ar ${key}`).not.toBe('');
      }
    });
  }
});
