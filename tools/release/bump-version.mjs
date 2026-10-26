#!/usr/bin/env node
// Bumps the version of the API or the dashboard and adds the release to the changelog the dashboard shows at
// /changelog (VoltaXDashboardSpa/src/assets/data/changelog.json). Run by .github/workflows/bump-version.yml;
// it only edits files, committing and tagging is left to the caller.
//
//   node tools/release/bump-version.mjs <api|dashboard> <patch|minor|major> [changes]
//
// changes: the release notes, separated by ";". Without them, the subjects of the commits that touched the
// component since its version last changed are used.
import { execFileSync } from 'node:child_process';
import { appendFileSync, readFileSync, writeFileSync } from 'node:fs';

const CHANGELOG = 'VoltaXDashboardSpa/src/assets/data/changelog.json';

const COMPONENTS = {
  api: {
    dir: 'VoltaXApi',
    versionFile: 'VoltaXApi/VoltaXApi.csproj',
    versionLine: '<Version>',
    read: () => {
      const match = readFileSync('VoltaXApi/VoltaXApi.csproj', 'utf8').match(/<Version>([^<]+)<\/Version>/);
      if (!match) fail('VoltaXApi/VoltaXApi.csproj has no <Version> element.');
      return match[1].trim();
    },
    write: (version) => {
      replaceIn('VoltaXApi/VoltaXApi.csproj', /<Version>[^<]+<\/Version>/, `<Version>${version}</Version>`);
    },
  },
  dashboard: {
    dir: 'VoltaXDashboardSpa',
    versionFile: 'VoltaXDashboardSpa/package.json',
    versionLine: '"version"',
    read: () => JSON.parse(readFileSync('VoltaXDashboardSpa/package.json', 'utf8')).version,
    write: (version) => {
      // Text replacements rather than JSON.stringify, so the files keep their formatting.
      replaceIn('VoltaXDashboardSpa/package.json', /("version":\s*")[^"]+/, `$1${version}`);
      replaceIn('VoltaXDashboardSpa/package-lock.json', /^(\s*"version":\s*")[^"]+/m, `$1${version}`);
      replaceIn('VoltaXDashboardSpa/package-lock.json', /("packages":\s*\{\s*"":\s*\{[^}]*?"version":\s*")[^"]+/, `$1${version}`);
    },
  },
};

const BUMPS = ['patch', 'minor', 'major'];

const [component, bump, notes = ''] = process.argv.slice(2);
const target = COMPONENTS[component];
if (!target) fail(`Unknown component "${component}", expected one of: ${Object.keys(COMPONENTS).join(', ')}.`);
if (!BUMPS.includes(bump)) fail(`Unknown bump "${bump}", expected one of: ${BUMPS.join(', ')}.`);

const current = target.read();
const next = nextVersion(current, bump);
const changelog = JSON.parse(readFileSync(CHANGELOG, 'utf8'));
const changes = notes.trim() ? splitNotes(notes) : commitSubjects(target);
if (changes.length === 0) fail(`No commits touched ${target.dir} since the last ${component} release; pass the changes instead.`);

target.write(next);
changelog.releases.unshift({ component, version: next, date: new Date().toISOString().slice(0, 10), changes });
writeFileSync(CHANGELOG, JSON.stringify(changelog, null, 2) + '\n');

console.log(`${component} ${current} -> ${next}`);
for (const change of changes) console.log(`  - ${change}`);
if (process.env.GITHUB_OUTPUT) appendFileSync(process.env.GITHUB_OUTPUT, `version=${next}\ntag=${component}-v${next}\n`);

function nextVersion(version, part) {
  const match = /^(\d+)\.(\d+)\.(\d+)$/.exec(version);
  if (!match) fail(`Current version "${version}" is not major.minor.patch.`);
  let [major, minor, patch] = match.slice(1).map(Number);
  if (part === 'major') [major, minor, patch] = [major + 1, 0, 0];
  else if (part === 'minor') [minor, patch] = [minor + 1, 0];
  else patch += 1;
  return `${major}.${minor}.${patch}`;
}

function splitNotes(text) {
  return text.split(/[;\n]/).map((line) => line.trim().replace(/^[-*]\s*/, '')).filter(Boolean);
}

function commitSubjects({ dir, versionFile, versionLine }) {
  // The last release is the last commit that changed the version line.
  const release = git('log', '-1', '--format=%H', `-G${versionLine}`, '--', versionFile);
  const range = release ? [`${release}..HEAD`] : [];
  const subjects = git('log', '--no-merges', '--format=%s', ...range, '--', dir) ?? '';
  return subjects.split('\n').map((s) => s.trim()).filter((s) => s && !/^Release (api|dashboard) /.test(s));
}

function git(...args) {
  try {
    return execFileSync('git', args, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] }).trim();
  } catch {
    return null;
  }
}

function replaceIn(file, pattern, replacement) {
  const text = readFileSync(file, 'utf8');
  if (!pattern.test(text)) fail(`Could not find the version in ${file}.`);
  writeFileSync(file, text.replace(pattern, replacement));
}

function fail(message) {
  console.error(message);
  process.exit(1);
}
