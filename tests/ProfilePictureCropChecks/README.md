# Profile picture crop checks

Run from the repository root after installing frontend dependencies:

```sh
node --test tests/ProfilePictureCropChecks/crop.test.mjs
```

Requires Node 24. Uses the installed Vue compiler and a headless Vue renderer
to check confirmation, conversion errors and cancellation. It does not exercise
the cropper's pointer/touch interactions or visual layout in a browser.
