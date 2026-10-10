# 🧭 Commit Convention – Courier Project

This document defines the commit message standard for the **Courier Project**,  
following the **Conventional Commits** specification.  
It ensures all commits are **consistent**, **meaningful**, and **easy to track** throughout development.

## 📦 Commit Message Format

```bash
<type>(<scope>): <short summary>

[optional body]

[optional footer]
```

**✨ Example**:
```bash
feat(api): add API workflow execution

- support chained requests
- add retry logic
- improve error handling

Closes #12
```

## 🧩 Commit Types

| Type | Description | Example |
|------|--------------|----------|
| **feat** | Add a new feature | `feat(api): add request runner` |
| **fix** | Fix a bug | `fix(auth): resolve token refresh issue` |
| **chore** | Tooling, dependencies, configs | `chore: setup Turborepo + project base` |
| **docs** | Documentation only | `docs: update README setup guide` |
| **style** | Code style or formatting | `style: apply Prettier formatting` |
| **refactor** | Code refactor without behavior change | `refactor(db): simplify query logic` |
| **perf** | Performance improvement | `perf(api): optimize request execution` |
| **test** | Add or update tests | `test(api): add integration tests` |
| **ci** | CI/CD configuration | `ci: add GitHub Actions pipeline` |
| **build** | Build system changes | `build: update pnpm lockfile` |
| **revert** | Revert a previous commit | `revert: feat(api): remove workflow module` |
| **feat! / fix!** | Breaking change | `feat!: change API response structure` |

---

## 📍 Common Scopes
- Use scopes to clarify where the change happens:
- auth – authentication
- api – backend / API logic
- db – database / Prisma
- ui – UI components
- router – routing (TanStack Router)
- query – data fetching (React Query)
- config – configuration files
- build – tooling / bundler
- test – testing

---



## ✍️ Commit Best Practices

✅ Keep messages **short and clear** (≤ 72 chars)  
✅ Use **imperative mood** (e.g., “add”, not “added”)  
✅ Each commit should be **atomic** — one purpose only  
✅ Include `scope` when possible for clarity  
✅ Avoid vague messages like update code  
✅ Reference issues or PRs if relevant (e.g., `fix: resolve #42`)

---

## 🚀 Example Commit Flow

| Step | Example commit message |
|------|-------------------------|
| Initialize repo | `chore: initialize Turborepo + base structure` |
| Setup frontend | `feat(web): setup base frontend` |
| Setup backend | `feat(api): setup NestJS server` |
| Setup database | `feat(db): configure Prisma with PostgreSQL` |
| Add auth | `feat(auth): implement JWT authentication` |
| Add feature | `feat(api): add API request runner` |
| Fix bug | `fix(api): handle timeout error` |
| Improve performance | `perf(api): optimize request batching` |
| Update docs | `docs: add installation guide` |
| Deploy setup | `ci: configure GitHub Actions pipeline` |

---


## 🧱 Commit Size & File Guidelines

### 🎯 1. One Commit = One Purpose
Each commit should represent a **single logical change**:
- ✅ Good: `feat(api): add request validation middleware`
- ❌ Bad: `feat: add validation and fix auth bug`

---

### 📁 2. Recommended File Change Limits

| Commit Type | Typical File Count | Notes |
|--------------|--------------------|-------|
| **fix** | 1–5 files | Small bug fixes only |
| **feat/refactor** | 5–15 files | Moderate new feature or update, Internal improvements or restructuring |
| **chore/docs/style** | 1–5 files | Configs, documentation, or formatting |

> ⚠️ Try to keep commits focused — typically **10–15 files is healthy**.  
> If a commit touches more than **20 files**, consider splitting it into smaller, meaningful commits.

### 💥 Breaking Changes
When a commit introduces a breaking change:

Option 1: Use **!**
```bash 
feat!: redesign API response format
```

Option 2: Use footer
```bash
feat(api): update response structure 

BREAKING CHANGE: response format has changed
```

## 🧠 References

- [Conventional Commits Spec](https://www.conventionalcommits.org/)
- [Commitlint Guide](https://commitlint.js.org/)
- [Semantic Release](https://semantic-release.gitbook.io/semantic-release/)