# Client

The Angular 22 front end of the Todo application. Setup, run and test instructions for the whole project — including
this client — are in the [root README](../README.md).

Quick reference (from this folder, with the API running on `http://localhost:5055`):

```bash
npm install
npm start                  # http://localhost:4200, /api proxied to the API (proxy.conf.json)
npm test -- --watch=false  # unit tests (Vitest)
npm run build              # production build in dist/client
```
