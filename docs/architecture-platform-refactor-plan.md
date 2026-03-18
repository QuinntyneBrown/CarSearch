# Platform Refactor Plan

## Goal

Reduce dealer-by-dealer workflow duplication by extracting shared platform-level provider and parser abstractions while preserving current search behavior.

## Plan

1. Build a provider inventory and migration matrix.
   Record each provider, its platform family, known special cases, and migration status. Use the existing requirements in `docs/specs/L2.md` and the current provider implementations to classify providers into families such as D2C Media, LeadBox/WordPress, AutoTrader, and outliers.

2. Freeze current behavior before refactoring.
   Add a test project with parser fixture tests and a fake browser-session harness so platform extraction is verified against current behavior instead of relying on manual spot checks. Start with one or two representative providers per family.

3. Introduce a platform layer under `CarSearch.Core`.
   Create a shared platform area, for example `Providers/Platforms/`, with base workflows such as `D2cMediaProviderBase`, `LeadBoxProviderBase`, and `AutoTraderProviderBase`. These base classes should own the common open/filter/snapshot/close flow while dealer-specific providers keep only true overrides.

4. Extract parser helpers by platform.
   Move repeated regex and ref-finding logic into shared parser bases or helper classes. Dealer parsers should remain only where listing structure or filter discovery actually differs.

5. Pilot the abstraction one platform at a time.
   Migrate one low-risk provider for each platform family first, validate against fixtures, and only then migrate the rest of that family. Avoid a big-bang rewrite across all providers.

6. Simplify provider definitions after migration.
   For providers on a supported platform, reduce the provider class to metadata plus minimal overrides. Adding a new dealer on an existing platform should no longer require copying a full workflow implementation.

7. Clean up and enforce the new structure.
   Update documentation, remove dead helpers, and add lightweight guardrails so future providers follow the platform-first structure rather than reintroducing dealer-specific duplication.

## Success Criteria

- A new provider on an existing platform requires only configuration plus a minimal dealer-specific class.
- Shared click, wait, filter, and snapshot flow exists once per platform rather than once per dealer.
- Parser behavior is covered by fixtures for each platform family.
- Existing provider output remains stable aside from intentional fixes.

## Recommended First Slice

1. Add the test project and fixture coverage.
2. Extract `D2cMediaProviderBase` and shared D2C parser helpers.
3. Migrate two or three D2C providers.
4. Validate behavior, then migrate the rest of the D2C family.
