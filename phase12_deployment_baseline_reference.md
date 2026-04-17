# Phase 12 Deployment Baseline Reference

## Implemented baseline
1. Dockerfile for API container build/publish/runtime.
2. `.dockerignore` to reduce build context and exclude large source books.
3. Production appsettings file.
4. Request logging middleware for basic latency/status observability.
5. README container launch steps.

## Runtime target
- API container listens on port `8080` with `ASPNETCORE_URLS=http://+:8080`.
