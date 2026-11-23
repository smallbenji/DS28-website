# Formularskabeloner

## Status

Implementeret som udkast. Skabeloner kan defineres i kode og vises, men svar
gemmes ikke. Der findes kun en testskabelon.

## Nuværende adfærd

- Skabeloner er kode i `src/DS.Application/FormTemplates`. Konkrete skabeloner
  registreres som `FormTemplate` i DI og findes via `FormTemplateService` ud fra
  et unikt id. Dublerede id'er afvises, når servicen oprettes.
- `GET /api/v1/forms/{id}` returnerer definitionen eller 404.
  `/api/v1/forms/test` er alias for `test-form-template`. Endpointet er
  offentligt.
- Felttyper sendes som enum-navne. Tekst-, tal- og datofelter findes. Tal er
  tal eller `null`; datoer er datostrenge (`YYYY-MM-DD`).
- `/forms/:id?` (uden id: testskabelonen) bruger gæstelayoutet og viser
  sektionskort, indlæsning og fejl med genforsøg, rydning af svar og en foldbar
  JSON-visning. `TemplateForm.vue` renderer felterne med Nuxt UI og modtager
  svar med `v-model`. Siden siger tydeligt, at intet gemmes.
- Frontenden har `FormService` og Pinia-storen `useFormStore`.

## Beslutninger

- 2026-10-22: Kode-definerede skabeloner registreres i DI og hentes efter id.
  Controlleren opretter ikke selv skabeloner.
- 2026-10-23: Visningen er et udspil, ikke et vedtaget domæneflow.

## Åbne spørgsmål

- Hvem skal have adgang til fremtidige domænespecifikke skabeloner? Det skal
  afklares, før de registreres.
- Indsendelse, lagring og servervalidering er ikke besluttet.

## Kodehenvisninger

- `src/DS.Application/FormTemplates/FormTemplate.cs`
- `src/DS.Application/FormTemplates/FormTemplateService.cs`
- `src/DS.Application/FormTemplates/TestFormTemplate.cs`
- `src/DS.Backend/Controllers/ApiControllers/FormApiController.cs`
- `src/DS.Frontend/src/Views/FormPreview.vue`
- `src/DS.Frontend/src/Components/Forms/TemplateForm.vue`
- `src/DS.Frontend/src/Services/FormService.ts`
